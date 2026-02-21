using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace RA2Installer
{
    /// <summary>
    /// 调色板条目结构体
    /// </summary>
    internal struct PaletEntry
    {
        public byte R, G, B;
    }

    /// <summary>
    /// SHP(TS) 文件头结构体
    /// </summary>
    internal struct ShpTsHeader
    {
        public short Zero;      // 应该是0
        public short Width;      // 宽度
        public short Height;     // 高度
        public short FrameCount; // 图像数量
    }

    /// <summary>
    /// SHP(TS) 图像头结构体
    /// </summary>
    internal struct ShpTsImageHeader
    {
        public short X;           // 图像X偏移
        public short Y;           // 图像Y偏移
        public short Width;        // 图像宽度
        public short Height;       // 图像高度
        public int Compression;    // 压缩方式
        public int Unknown;        // 未知
        public int Zero;           // 应该是0
        public int Offset;         // 图像数据偏移量
    }

    /// <summary>
    /// 用于处理 SHP(TS) 文件格式
    /// </summary>
    public class ShpFile
    {
        private byte[] _shpData;
        private List<BitmapSource> _frames;
        private int _frameCount;
        private int _width;
        private int _height;
        private PaletEntry[] _palet; // 调色板

        /// <summary>
        /// 初始化 ShpFile 实例
        /// </summary>
        /// <param name="shpData">SHP 文件的字节数组</param>
        /// <exception cref="ArgumentException">当没有提供调色板时抛出</exception>
        public ShpFile(byte[] shpData)
        {
            throw new ArgumentException("Palette is required, please use the constructor that accepts palData");
        }

        /// <summary>
        /// 初始化 ShpFile 实例，使用指定的调色板
        /// </summary>
        /// <param name="shpData">SHP 文件的字节数组</param>
        /// <param name="palData">PAL 文件的字节数组</param>
        public ShpFile(byte[] shpData, byte[] palData)
        {
            _shpData = shpData;
            _frames = new List<BitmapSource>();
            _palet = LoadPaletFromData(palData); // 从 PAL 文件加载调色板
            ParseShpFile();
        }

        /// <summary>
        /// 从 PAL 文件数据加载调色板
        /// </summary>
        /// <param name="palData">PAL 文件的字节数组</param>
        /// <returns>加载的调色板</returns>
        /// <exception cref="Exception">当调色板加载失败时抛出</exception>
        private PaletEntry[] LoadPaletFromData(byte[] palData)
        {
            PaletEntry[] palet = new PaletEntry[256];
            
            // 检查 PAL 文件数据长度
            if (palData.Length < 768) // 256 个颜色 × 3 字节/颜色
            {
                throw new Exception("Invalid PAL file size");
            }
            
            // 检查是否有 PAL0 文件头
            int colorDataStart = 0;
            if (palData.Length >= 4 && palData[0] == 0x50 && palData[1] == 0x41 && palData[2] == 0x4C && palData[3] == 0x30)
            {
                // 有文件头，跳过
                colorDataStart = 4;
            }
            
            // 从 PAL 文件数据加载调色板
            // 注意：在 RA2 的 PAL 文件中，颜色通道顺序是 R、G、B
            // 并且每个通道值需要乘以 4 以匹配 OS-SHP-Builder 的处理方式
            for (int i = 0; i < 256 && (colorDataStart + i * 3) < palData.Length; i++)
            {
                palet[i] = new PaletEntry
                {
                    R = (byte)(palData[colorDataStart + i * 3] * 4),
                    G = (byte)(palData[colorDataStart + i * 3 + 1] * 4),
                    B = (byte)(palData[colorDataStart + i * 3 + 2] * 4)
                };
            }
            
            return palet;
        }

        /// <summary>
        /// 解析 SHP 文件
        /// </summary>
        private void ParseShpFile()
        {
            try
            {
                string logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.AppendAllText(logFile, "Parsing SHP file using TS format\n");
                File.AppendAllText(logFile, "SHP data length: " + _shpData.Length + " bytes\n");
                
                using (var stream = new MemoryStream(_shpData))
                using (var reader = new BinaryReader(stream))
                {
                    // 检查文件大小是否足够读取头部
                    if (stream.Length < 8) // SHP(TS) 头部大小为8字节
                    {
                        File.AppendAllText(logFile, "SHP file too small to read header\n");
                        return;
                    }
                    
                    // 读取文件头
                    ShpTsHeader header = new ShpTsHeader
                    {
                        Zero = reader.ReadInt16(),
                        Width = reader.ReadInt16(),
                        Height = reader.ReadInt16(),
                        FrameCount = reader.ReadInt16()
                    };
                    
                    _frameCount = header.FrameCount;
                    _width = header.Width;
                    _height = header.Height;
                    
                    File.AppendAllText(logFile, "Header zero: " + header.Zero + "\n");
                    File.AppendAllText(logFile, "Frame count: " + _frameCount + "\n");
                    File.AppendAllText(logFile, "Width: " + _width + "\n");
                    File.AppendAllText(logFile, "Height: " + _height + "\n");
                    
                    // 检查头部信息是否合理
                    if (_frameCount <= 0 || _frameCount > 10000 || // 限制最大帧数为10000
                        _width <= 0 || _width > 10000 || // 限制最大宽度为10000
                        _height <= 0 || _height > 10000) // 限制最大高度为10000
                    {
                        File.AppendAllText(logFile, "Invalid header information\n");
                        return;
                    }

                    // 读取图像头部
                    ShpTsImageHeader[] imageHeaders = new ShpTsImageHeader[_frameCount];
                    int imageHeaderSize = 24; // 每个图像头部大小为24字节
                    
                    if (stream.Length < 8 + (_frameCount * imageHeaderSize))
                    {
                        File.AppendAllText(logFile, "Not enough data to read all image headers\n");
                        return;
                    }
                    
                    File.AppendAllText(logFile, "Reading image headers\n");
                    for (int i = 0; i < _frameCount; i++)
                    {
                        imageHeaders[i] = new ShpTsImageHeader
                        {
                            X = reader.ReadInt16(),
                            Y = reader.ReadInt16(),
                            Width = reader.ReadInt16(),
                            Height = reader.ReadInt16(),
                            Compression = reader.ReadInt32(),
                            Unknown = reader.ReadInt32(),
                            Zero = reader.ReadInt32(),
                            Offset = reader.ReadInt32()
                        };
                        
                        File.AppendAllText(logFile, "Image " + i + " - Offset: " + imageHeaders[i].Offset + ", Size: " + imageHeaders[i].Width + "x" + imageHeaders[i].Height + ", Compression: " + imageHeaders[i].Compression + "\n");
                    }

                    // 读取每个帧
                    File.AppendAllText(logFile, "Starting to read frames\n");
                    for (int i = 0; i < _frameCount; i++)
                    {
                        ShpTsImageHeader imgHeader = imageHeaders[i];
                        
                        // 检查偏移量是否有效
                        if (imgHeader.Offset < 0 || imgHeader.Offset >= _shpData.Length)
                        {
                            File.AppendAllText(logFile, "Invalid offset for frame " + i + ", skipping\n");
                            continue;
                        }
                        
                        // 计算帧数据大小
                        int frameSize;
                        if (i < _frameCount - 1)
                        {
                            frameSize = imageHeaders[i + 1].Offset - imgHeader.Offset;
                        }
                        else
                        {
                            frameSize = _shpData.Length - imgHeader.Offset;
                        }
                        
                        if (frameSize <= 0)
                        {
                            File.AppendAllText(logFile, "Invalid frame size for frame " + i + ", skipping\n");
                            continue;
                        }
                        
                        // 读取帧数据
                        byte[] frameData = new byte[frameSize];
                        Array.Copy(_shpData, imgHeader.Offset, frameData, 0, frameSize);
                        File.AppendAllText(logFile, "Frame " + i + " data read, size: " + frameData.Length + " bytes\n");
                        
                        // 检查是否需要解压缩
                        byte[] decodedData = frameData;
                        if ((imgHeader.Compression & 2) != 0) // 检查压缩标志
                        {
                            File.AppendAllText(logFile, "Frame " + i + " is compressed, decoding\n");
                            decodedData = Decode3(frameData, imgHeader.Width, imgHeader.Height);
                            if (decodedData == null)
                            {
                                File.AppendAllText(logFile, "Failed to decode frame " + i + ", skipping\n");
                                continue;
                            }
                            File.AppendAllText(logFile, "Frame " + i + " decoded, size: " + decodedData.Length + " bytes\n");
                        }
                        
                        // 转换帧数据为 BitmapSource
                        BitmapSource frame = ConvertFrameDataToBitmap(decodedData, imgHeader.Width, imgHeader.Height);
                        if (frame != null)
                        {
                            _frames.Add(frame);
                            File.AppendAllText(logFile, "Frame " + i + " added to frames list\n");
                        }
                        else
                        {
                            File.AppendAllText(logFile, "Failed to convert frame " + i + " data to bitmap\n");
                        }
                    }
                    
                    File.AppendAllText(logFile, "Finished reading frames, total frames: " + _frames.Count + "\n");
                }
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.AppendAllText(logFile, "Error parsing SHP file: " + ex.Message + "\n");
                File.AppendAllText(logFile, "Stack trace: " + ex.StackTrace + "\n");
            }
        }

        /// <summary>
        /// 解码压缩的 SHP 图像数据
        /// </summary>
        /// <param name="compressedData">压缩的图像数据</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns>解码后的图像数据</returns>
        private byte[] Decode3(byte[] compressedData, int width, int height)
        {
            try
            {
                int totalSize = width * height;
                byte[] decodedData = new byte[totalSize];
                int inPos = 0;
                int outPos = 0;
                
                while (outPos < totalSize && inPos < compressedData.Length)
                {
                    byte b = compressedData[inPos++];
                    
                    if (b < 0x80)
                    {
                        // 字面量运行
                        int count = b + 1;
                        Array.Copy(compressedData, inPos, decodedData, outPos, count);
                        inPos += count;
                        outPos += count;
                    }
                    else if (b < 0xC0)
                    {
                        // 短重复运行
                        int count = (b & 0x3F) + 3;
                        byte value = compressedData[inPos++];
                        for (int i = 0; i < count && outPos < totalSize; i++)
                        {
                            decodedData[outPos++] = value;
                        }
                    }
                    else if (b < 0xE0)
                    {
                        // 中重复运行
                        int count = (b & 0x1F) + 9;
                        byte value = compressedData[inPos++];
                        for (int i = 0; i < count && outPos < totalSize; i++)
                        {
                            decodedData[outPos++] = value;
                        }
                    }
                    else if (b < 0xF0)
                    {
                        // 长重复运行
                        int count = ((b & 0x0F) << 8) | compressedData[inPos++];
                        count += 17;
                        byte value = compressedData[inPos++];
                        for (int i = 0; i < count && outPos < totalSize; i++)
                        {
                            decodedData[outPos++] = value;
                        }
                    }
                    else
                    {
                        // 非常长的重复运行
                        int count = ((b & 0x07) << 16) | (compressedData[inPos++] << 8) | compressedData[inPos++];
                        count += 273;
                        byte value = compressedData[inPos++];
                        for (int i = 0; i < count && outPos < totalSize; i++)
                        {
                            decodedData[outPos++] = value;
                        }
                    }
                }
                
                return decodedData;
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.AppendAllText(logFile, "Error decoding frame: " + ex.Message + "\n");
                return null;
            }
        }

        /// <summary>
        /// 将帧数据转换为 BitmapSource
        /// </summary>
        /// <param name="frameData">帧数据</param>
        /// <param name="width">帧宽度</param>
        /// <param name="height">帧高度</param>
        /// <returns>BitmapSource 实例</returns>
        private BitmapSource ConvertFrameDataToBitmap(byte[] frameData, int width, int height)
        {
            try
            {
                if (width <= 0 || height <= 0)
                {
                    return null;
                }
                
                // 创建一个 32 位的像素数组
                int[] pixels = new int[width * height];
                
                // 使用调色板将 8 位索引颜色转换为 32 位 BGRA
                // 注意：在 WPF 的 Bgra32 格式中，颜色通道顺序是蓝色、绿色、红色、alpha
                // 我们在 LoadPaletFromData 方法中按 R、G、B 顺序加载了 PAL 文件，所以需要转换为 B、G、R 顺序
                for (int i = 0; i < frameData.Length && i < width * height; i++)
                {
                    byte colorIndex = frameData[i];
                    if (colorIndex < _palet.Length)
                    {
                        // 检查是否为透明色（通常索引 0 是透明色）
                        if (colorIndex == 0)
                        {
                            // 透明色
                            pixels[i] = 0x00000000; // BGRA: 0,0,0,0
                        }
                        else
                        {
                            PaletEntry color = _palet[colorIndex];
                            // 在 WPF 的 Bgra32 格式中，像素值的存储顺序是蓝色、绿色、红色、alpha（小端序）
                            // 我们在 LoadPaletFromData 方法中按 R、G、B 顺序加载了 PAL 文件，所以需要转换为 B、G、R 顺序
                            pixels[i] = (color.B) | (color.G << 8) | (color.R << 16) | (255 << 24);
                        }
                    }
                    else
                    {
                        // 未知颜色索引，使用黑色
                        pixels[i] = 0x000000 | (255 << 24); // BGRA: 0,0,0,255
                    }
                }
                
                // 创建 BitmapSource
                return BitmapSource.Create(
                    width,
                    height,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    width * 4);
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.AppendAllText(logFile, "Error converting frame data to bitmap: " + ex.Message + "\n");
                return null;
            }
        }

        /// <summary>
        /// 获取所有帧
        /// </summary>
        /// <returns>帧列表</returns>
        public List<BitmapSource> GetFrames()
        {
            return _frames;
        }

        /// <summary>
        /// 获取帧数量
        /// </summary>
        public int FrameCount
        {
            get { return _frameCount; }
        }

        /// <summary>
        /// 获取宽度
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        /// <summary>
        /// 获取高度
        /// </summary>
        public int Height
        {
            get { return _height; }
        }
    }

    /// <summary>
    /// 用于播放 SHP 动画的辅助类
    /// </summary>
    public class ShpAnimationPlayer
    {
        private ShpFile _shpFile;
        private System.Windows.Controls.Image _imageControl;
        private int _currentFrameIndex;
        private System.Windows.Threading.DispatcherTimer _timer;

        /// <summary>
        /// 动画播放完成事件
        /// </summary>
        public event EventHandler AnimationCompleted;

        /// <summary>
        /// 初始化 ShpAnimationPlayer 实例
        /// </summary>
        /// <param name="shpFile">ShpFile 实例</param>
        /// <param name="imageControl">用于显示动画的 Image 控件</param>
        public ShpAnimationPlayer(ShpFile shpFile, System.Windows.Controls.Image imageControl)
        {
            _shpFile = shpFile;
            _imageControl = imageControl;
            _currentFrameIndex = 0;

            // 初始化定时器
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
            _timer.Tick += Timer_Tick;

            // 显示第一帧
            var frames = _shpFile.GetFrames();
            if (frames.Count > 0)
            {
                _imageControl.Source = frames[_currentFrameIndex];
            }
        }

        /// <summary>
        /// 开始播放动画
        /// </summary>
        public void Play()
        {
            _timer.Start();
        }

        /// <summary>
        /// 停止播放动画
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        /// <summary>
        /// 重置动画到第一帧
        /// </summary>
        public void Reset()
        {
            _currentFrameIndex = 0;
            // 显示第一帧
            var frames = _shpFile.GetFrames();
            if (frames.Count > 0)
            {
                _imageControl.Source = frames[_currentFrameIndex];
            }
        }

        /// <summary>
        /// 定时器触发事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            var frames = _shpFile.GetFrames();
            if (frames.Count > 0)
            {
                // 显示当前帧
                _imageControl.Source = frames[_currentFrameIndex];
                
                // 检查是否是最后一帧
                if (_currentFrameIndex < frames.Count - 1)
                {
                    // 不是最后一帧，切换到下一帧
                    _currentFrameIndex++;
                }
                else
                {
                    // 是最后一帧，停止播放
                    _timer.Stop();
                    // 触发动画播放完成事件
                    OnAnimationCompleted(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 触发动画播放完成事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnAnimationCompleted(EventArgs e)
        {
            AnimationCompleted?.Invoke(this, e);
        }
    }
}
