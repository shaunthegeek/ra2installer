using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RA2Installer.Resources;

namespace RA2Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Run(new MainWindow());
        }

        // 常量：Setup.mix 文件路径
        private const string SetupMixPath = "Assets/RA1/Setup/Setup.mix";

        private MediaPlayer _backgroundMusicPlayer;
        private MediaPlayer _soundPlayer;
        private string? _buttonClickSoundFile;
        private string? _backgroundMusicFile;
        private ShpAnimationPlayer? _shpAnimationPlayer;

        // 日志文件路径
        private string _logFile = string.Empty;

        // 当前页码
        private int _currentPage = 1;

        // 存储每一页的雷达文案IDs
        private readonly Dictionary<int, int[]> _pageRadarStringIds = new Dictionary<int, int[]> {
            { 1, new int[] { 250, 251, 252, 253, 254 } },
            { 2, new int[] { 255 } },
            { 3, new int[] { 256, 257, 258, 259, 260, 261 } }
        };

        // 存储每一页的底部文字ID和显示时长（毫秒）
        private readonly Dictionary<int, (int StringId, int DisplayDurationMs)> _pageBottomTextConfig = new Dictionary<int, (int, int)> {
            { 1, (144, 1000) }, // 第一页：ID 144，显示1秒
            { 2, (145, 1000) }, // 第二页：ID 145，显示1秒
            { 3, (146, 1000) }  // 第三页：ID 146，显示1秒
        };

        // 用于控制底部文字显示时长的定时器
        private System.Timers.Timer? _bottomTextTimer;

        // 用于取消异步加载任务的令牌源
        private CancellationTokenSource? _loadStringsCancellationTokenSource;



        public MainWindow()
        {
            try
            {
                // 创建日志文件
                _logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.WriteAllText(_logFile, "Starting MainWindow initialization\n");

                // 首先初始化组件，这样 Grid 控件就会被创建
                InitializeComponent();

                File.AppendAllText(_logFile, "Components initialized, checking AnimationImage\n");

                // 检查 AnimationImage 是否存在
                if (AnimationImage != null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is available\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                }

                File.AppendAllText(_logFile, "Loading background image\n");
                // 然后加载背景图片
                LoadBackgroundImageFromMix(SetupMixPath, "B1D51F00");

                File.AppendAllText(_logFile, "Loading SHP animation data\n");
                // 加载 SHP 动画数据（不播放）
                try
                {
                    File.AppendAllText(_logFile, "Calling LoadShpAnimationData with parameters: " + SetupMixPath + ", 2012EC16\n");
                    LoadShpAnimationData(SetupMixPath, "2012EC16");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_logFile, "Error calling LoadShpAnimationData: " + ex.Message + "\n");
                }

                // 初始化 MediaPlayer
                File.AppendAllText(_logFile, "Initializing MediaPlayer\n");
                _backgroundMusicPlayer = new System.Windows.Media.MediaPlayer();
                _soundPlayer = new System.Windows.Media.MediaPlayer();

                // 加载按钮点击音效
                File.AppendAllText(_logFile, "Loading button click sound\n");
                LoadButtonClickSound();

                // 加载背景音乐
                File.AppendAllText(_logFile, "Loading background music\n");
                LoadBackgroundMusic();

                File.AppendAllText(_logFile, "Adding Loaded event handler\n");
                Loaded += MainWindow_Loaded;

                File.AppendAllText(_logFile, "MainWindow initialization completed\n");
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.AppendAllText(logFile, $"Error during MainWindow initialization: {ex.Message}\n");
                File.AppendAllText(logFile, $"Stack trace: {ex.StackTrace}\n");
                // 不退出程序，继续初始化
                InitializeComponent();
                // 初始化所有MediaPlayers
                _backgroundMusicPlayer = new System.Windows.Media.MediaPlayer();
                _soundPlayer = new System.Windows.Media.MediaPlayer();
                Loaded += MainWindow_Loaded;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 播放背景音乐
            PlayBackgroundMusic();

            // 从Language.dll读取字符串并显示
            LoadAndDisplayRadarStrings();

            // 加载并显示底部文本
            LoadBottomText();

            // 确保AnimationImage可见
            if (AnimationImage != null)
            {
                AnimationImage.Visibility = Visibility.Visible;
                File.AppendAllText(_logFile, "AnimationImage visibility set to Visible\n");
            }

            // 更新导航按钮状态
            UpdateNavigationButtons();

            // 第1页启动时，自动倒放动画
            if (_shpAnimationPlayer != null)
            {
                File.AppendAllText(_logFile, "MainWindow loaded, starting reverse animation for Page 1\n");
                _shpAnimationPlayer.IsReverse = true;
                _shpAnimationPlayer.ResetToLastFrame();
                _shpAnimationPlayer.Play();
            }
        }

        /// <summary>
        /// 从Language.dll读取字符串并显示在界面上
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task LoadAndDisplayRadarStringsAsync(int pageNumber, CancellationToken cancellationToken)
        {
            try
            {
                File.AppendAllText(_logFile, $"Starting to load language strings from Language.dll for page {pageNumber}\n");

                // Language.dll文件路径
                string languageDllPath = "Assets/RA1/Setup/Language.dll";

                // 检查文件是否存在
                if (!File.Exists(languageDllPath))
                {
                    File.AppendAllText(_logFile, "Language.dll file not found\n");
                    return;
                }

                File.AppendAllText(_logFile, "Language.dll file found, loading strings\n");

                // 确定要使用的语言
                ushort languageId = GetLanguageIdForCurrentLanguage();
                File.AppendAllText(_logFile, $"Using language ID: {languageId}\n");

                // 根据页码获取对应的字符串IDs
                if (!_pageRadarStringIds.TryGetValue(pageNumber, out int[]? stringIds) || stringIds == null)
                {
                    File.AppendAllText(_logFile, $"No string IDs defined for page {pageNumber}\n");
                    return;
                }

                File.AppendAllText(_logFile, $"Loading string IDs: {string.Join(", ", stringIds)}\n");

                foreach (int id in stringIds)
                {
                    // 检查是否取消
                    if (cancellationToken.IsCancellationRequested)
                    {
                        File.AppendAllText(_logFile, "Loading cancelled\n");
                        return;
                    }

                    string? text = ReadStringFromLanguageDll(languageDllPath, id, languageId);
                    if (!string.IsNullOrEmpty(text))
                    {
                        // 检查是否取消
                        if (cancellationToken.IsCancellationRequested)
                        {
                            File.AppendAllText(_logFile, "Loading cancelled\n");
                            return;
                        }

                        // 创建TextBlock并添加到StackPanel
                        TextBlock textBlock = new TextBlock
                        {
                            Text = text,
                            Foreground = Brushes.Yellow,
                            FontSize = 10,
                            TextAlignment = TextAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                        };
                        RadarTextStackPanel.Children.Add(textBlock);
                        File.AppendAllText(_logFile, $"Added string ID {id}: {text}\n");
                    }
                    else
                    {
                        File.AppendAllText(_logFile, $"Failed to read string ID {id}\n");
                    }

                    // 检查是否取消
                    if (cancellationToken.IsCancellationRequested)
                    {
                        File.AppendAllText(_logFile, "Loading cancelled\n");
                        return;
                    }

                    await Task.Delay(1000, cancellationToken);
                }

                File.AppendAllText(_logFile, "Language strings loaded and displayed\n");
            }
            catch (OperationCanceledException)
            {
                File.AppendAllText(_logFile, "Loading operation cancelled\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading language strings: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 从Language.dll读取字符串并显示在界面上（同步包装方法）
        /// </summary>
        /// <param name="pageNumber">页码</param>
        private void LoadAndDisplayRadarStrings(int pageNumber)
        {
            // 取消之前的加载任务
            CancelLoadStringsTask();

            // 创建新的令牌源
            _loadStringsCancellationTokenSource = new CancellationTokenSource();

            // 调用异步方法
            _ = LoadAndDisplayRadarStringsAsync(pageNumber, _loadStringsCancellationTokenSource.Token);
        }

        /// <summary>
        /// 从Language.dll读取字符串并显示在界面上（默认使用当前页码）
        /// </summary>
        private void LoadAndDisplayRadarStrings()
        {
            // 取消之前的加载任务
            CancelLoadStringsTask();

            // 创建新的令牌源
            _loadStringsCancellationTokenSource = new CancellationTokenSource();

            // 调用异步方法，使用当前页码
            _ = LoadAndDisplayRadarStringsAsync(_currentPage, _loadStringsCancellationTokenSource.Token);
        }

        /// <summary>
        /// 取消当前的字符串加载任务
        /// </summary>
        private void CancelLoadStringsTask()
        {
            if (_loadStringsCancellationTokenSource != null)
            {
                _loadStringsCancellationTokenSource.Cancel();
                _loadStringsCancellationTokenSource.Dispose();
                _loadStringsCancellationTokenSource = null;
                File.AppendAllText(_logFile, "Cancelled previous string loading task\n");
            }
        }

        /// <summary>
        /// 根据当前选择的语言获取对应的语言ID
        /// </summary>
        /// <returns>语言ID</returns>
        private ushort GetLanguageIdForCurrentLanguage()
        {
            // 使用当前的 UI 文化来确定语言 ID
            string currentLanguageName = CultureInfo.CurrentUICulture.Name;

            // 根据当前语言选择对应的语言ID
            if (currentLanguageName.StartsWith("zh"))
            {
                return 0x0404; // zh-TW
            }
            else
            {
                return 0x0409; // en-US
            }
        }

        /// <summary>
        /// 从Language.dll中读取指定ID的字符串
        /// </summary>
        /// <param name="dllPath">Language.dll文件路径</param>
        /// <param name="stringId">字符串ID</param>
        /// <param name="languageId">语言ID</param>
        /// <returns>读取到的字符串或 null</returns>
        private string? ReadStringFromLanguageDll(string dllPath, int stringId, ushort languageId)
        {
            try
            {
                File.AppendAllText(_logFile, $"=== Starting ReadStringFromLanguageDll ===\n");
                File.AppendAllText(_logFile, $"DLL Path: {dllPath}\n");
                File.AppendAllText(_logFile, $"String ID: {stringId}\n");
                File.AppendAllText(_logFile, $"Language ID: {languageId:X4}\n");

                // 检查文件是否存在
                if (!File.Exists(dllPath))
                {
                    File.AppendAllText(_logFile, $"File does not exist: {dllPath}\n");
                    return null;
                }
                File.AppendAllText(_logFile, $"File exists: {dllPath}\n");

                // 获取文件大小
                FileInfo fileInfo = new FileInfo(dllPath);
                File.AppendAllText(_logFile, $"File size: {fileInfo.Length} bytes\n");

                // 尝试使用Windows API读取DLL中的字符串
                IntPtr dllHandle = IntPtr.Zero;
                try
                {
                    // 使用LoadLibraryEx加载DLL，指定LOAD_LIBRARY_AS_DATAFILE标志
                    File.AppendAllText(_logFile, $"Calling LoadLibraryEx with LOAD_LIBRARY_AS_DATAFILE...\n");
                    const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
                    dllHandle = NativeMethods.LoadLibraryEx(dllPath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
                    if (dllHandle != IntPtr.Zero)
                    {
                        File.AppendAllText(_logFile, $"LoadLibraryEx succeeded, handle: {dllHandle}\n");

                        // 使用FindResourceEx根据语言ID查找特定语言的资源
                        File.AppendAllText(_logFile, $"Using FindResourceEx with language ID...\n");
                        IntPtr hResource = NativeMethods.FindResourceEx(dllHandle, new IntPtr(6), new IntPtr((stringId / 16) + 1), languageId);
                        if (hResource != IntPtr.Zero)
                        {
                            File.AppendAllText(_logFile, $"FindResourceEx succeeded, resource handle: {hResource}\n");
                            string? text = ReadStringFromResource(dllHandle, hResource, stringId);
                            if (!string.IsNullOrEmpty(text))
                            {
                                File.AppendAllText(_logFile, $"ReadStringFromResource succeeded, string: '{text}'\n");
                                File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with FindResourceEx ===\n\n");
                                return text;
                            }
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            File.AppendAllText(_logFile, $"FindResourceEx failed, error code: {errorCode}\n");
                            File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode).Message}\n");
                        }
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        File.AppendAllText(_logFile, $"LoadLibraryEx failed, error code: {errorCode}\n");
                        File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode).Message}\n");
                    }
                }
                finally
                {
                    if (dllHandle != IntPtr.Zero)
                    {
                        File.AppendAllText(_logFile, $"Calling FreeLibrary for handle: {dllHandle}\n");
                        bool freed = NativeMethods.FreeLibrary(dllHandle);
                        File.AppendAllText(_logFile, $"FreeLibrary result: {freed}\n");
                    }
                }

                File.AppendAllText(_logFile, $"FindResourceEx method failed\n");
                File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed ===\n\n");
                return null;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Exception in ReadStringFromLanguageDll: {ex.Message}\n");
                File.AppendAllText(_logFile, $"Stack trace: {ex.StackTrace}\n");
                File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with exception ===\n\n");
                return null;
            }
        }

        /// <summary>
        /// 从资源中读取字符串
        /// </summary>
        /// <param name="dllHandle">DLL句柄</param>
        /// <param name="hResource">资源句柄</param>
        /// <param name="stringId">字符串ID</param>
        /// <returns>读取到的字符串或 null</returns>
        private string? ReadStringFromResource(IntPtr dllHandle, IntPtr hResource, int stringId)
        {
            try
            {
                // 加载资源
                File.AppendAllText(_logFile, $"Calling LoadResource...\n");
                IntPtr hGlobal = NativeMethods.LoadResource(dllHandle, hResource);
                if (hGlobal == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    File.AppendAllText(_logFile, $"Failed to load resource, error code: {errorCode}\n");
                    File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode).Message}\n");
                    return null;
                }
                File.AppendAllText(_logFile, $"LoadResource succeeded, handle: {hGlobal}\n");

                // 锁定资源
                File.AppendAllText(_logFile, $"Calling LockResource...\n");
                IntPtr lpBuffer = NativeMethods.LockResource(hGlobal);
                if (lpBuffer == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    File.AppendAllText(_logFile, $"Failed to lock resource, error code: {errorCode}\n");
                    File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode).Message}\n");
                    return null;
                }
                File.AppendAllText(_logFile, $"LockResource succeeded, buffer address: {lpBuffer}\n");

                // 查找字符串
                int index = stringId % 16;
                File.AppendAllText(_logFile, $"String index in table: {index}\n");

                IntPtr currentPtr = lpBuffer;
                File.AppendAllText(_logFile, $"Starting buffer address: {currentPtr}\n");

                // 跳过前面的字符串
                for (int i = 0; i < index; i++)
                {
                    // 读取字符串长度
                    short length = Marshal.ReadInt16(currentPtr);
                    File.AppendAllText(_logFile, $"String {i} length: {length}\n");
                    // 即使是空字符串也要继续移动指针
                    // 移动到下一个字符串
                    currentPtr = currentPtr + 2 + length * 2;
                    File.AppendAllText(_logFile, $"Moved to next string address: {currentPtr}\n");
                }

                // 读取目标字符串
                short targetLength = Marshal.ReadInt16(currentPtr);
                File.AppendAllText(_logFile, $"Target string length: {targetLength}\n");
                if (targetLength > 0)
                {
                    currentPtr += 2;
                    File.AppendAllText(_logFile, $"Target string address: {currentPtr}\n");
                    string text = Marshal.PtrToStringUni(currentPtr, targetLength);
                    File.AppendAllText(_logFile, $"Successfully read string: '{text}'\n");
                    File.AppendAllText(_logFile, $"=== ReadStringFromResource completed successfully ===\n");
                    return text;
                }
                else
                {
                    File.AppendAllText(_logFile, $"Empty string for ID {stringId}\n");
                    File.AppendAllText(_logFile, $"=== ReadStringFromResource completed with empty string ===\n");
                    return null;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Exception in ReadStringFromResource: {ex.Message}\n");
                File.AppendAllText(_logFile, $"Stack trace: {ex.StackTrace}\n");
                return null;
            }
        }

        /// <summary>
        /// 原生方法定义
        /// </summary>
        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LockResource(IntPtr hResData);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);
        }



        /// <summary>
        /// 从指定路径的 Setup.mix 中加载指定哈希值和类型的图片作为背景
        /// 如果加载失败则输出日志但不退出程序
        /// </summary>
        /// <param name="setupMixPath">Setup.mix 文件的路径</param>
        /// <param name="fileNameHash">文件名哈希值</param>
        private void LoadBackgroundImageFromMix(string setupMixPath, string fileNameHash)
        {
            try
            {
                // 加载 Setup.mix 文件
                MixFile mixFile = new MixFile(setupMixPath);

                // 尝试获取指定哈希值和类型的图片
                System.Windows.Media.Imaging.BitmapImage? backgroundImage = mixFile.GetImageByHash(fileNameHash);

                if (backgroundImage == null)
                {

                    return;
                }

                // 更新 Grid 的 Background 属性
                // 直接从 Window 的 Content 属性获取 Grid 控件
                if (Content is not Grid grid)
                {
                    // 如果直接获取失败，尝试使用 FindVisualChild 方法
                    Grid? foundGrid = FindVisualChild<Grid>(this);
                    if (foundGrid == null)
                    {

                        return;
                    }
                    grid = foundGrid;
                }

                grid.Background = new ImageBrush(backgroundImage) { Stretch = Stretch.UniformToFill };
            }
            catch (Exception ex)
            {
                // 记录错误但不退出程序
                Console.WriteLine($"Error loading background image from Setup.mix: {ex.Message}");
            }
        }

        /// <summary>
        /// 从指定路径的 Setup.mix 中加载指定哈希值和类型的 SHP 文件并准备动画
        /// 如果加载失败则输出日志但不退出程序
        /// </summary>
        /// <param name="setupMixPath">Setup.mix 文件的路径</param>
        /// <param name="fileNameHash">文件名哈希值</param>
        private void LoadShpAnimationData(string setupMixPath, string fileNameHash)
        {
            try
            {
                // 简单的日志写入，避免格式化字符串可能的问题
                File.AppendAllText(_logFile, "Starting to load SHP animation\n");
                File.AppendAllText(_logFile, "Hash: " + fileNameHash + "\n");

                // 检查 AnimationImage 是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    return;
                }
                else
                {
                    File.AppendAllText(_logFile, "AnimationImage control is available\n");
                }

                // 加载 Setup.mix 文件
                File.AppendAllText(_logFile, "Loading mix file\n");
                File.AppendAllText(_logFile, "Path: " + setupMixPath + "\n");

                // 检查文件是否存在
                if (!File.Exists(setupMixPath))
                {
                    File.AppendAllText(_logFile, "Mix file does not exist\n");
                    return;
                }
                else
                {
                    File.AppendAllText(_logFile, "Mix file exists\n");
                }

                MixFile mixFile = new MixFile(setupMixPath);
                File.AppendAllText(_logFile, "Mix file loaded\n");

                // 尝试获取指定哈希值和类型的 SHP 文件
                File.AppendAllText(_logFile, "Attempting to get SHP file\n");
                byte[]? shpData = mixFile.GetShpByHash(fileNameHash);

                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file\n");
                    return;
                }
                else
                {
                    File.AppendAllText(_logFile, "Successfully loaded SHP file\n");
                    File.AppendAllText(_logFile, "Size: " + shpData.Length + " bytes\n");
                }

                ShpFile shpFile;

                // 使用用户指定的 PAL 文件
                byte[]? palData = null;
                string userSpecifiedPalHash = "397C46E0";
                palData = mixFile.GetPalByHash(userSpecifiedPalHash);
                if (palData != null)
                {
                    File.AppendAllText(_logFile, "Successfully loaded user specified PAL file with hash: " + userSpecifiedPalHash + "\n");
                    File.AppendAllText(_logFile, "Size: " + palData.Length + " bytes\n");

                    // 使用找到的 PAL 文件解析 SHP 文件
                    File.AppendAllText(_logFile, "Parsing SHP file with PAL\n");
                    shpFile = new ShpFile(shpData, palData);
                }
                else
                {
                    File.AppendAllText(_logFile, "Failed to load user specified PAL file with hash: " + userSpecifiedPalHash + "\n");
                    throw new Exception($"Failed to load specified PAL file with hash: {userSpecifiedPalHash}");
                }

                File.AppendAllText(_logFile, "SHP file parsed successfully\n");
                File.AppendAllText(_logFile, "Frame count: " + shpFile.FrameCount + "\n");
                File.AppendAllText(_logFile, "Width: " + shpFile.Width + "\n");
                File.AppendAllText(_logFile, "Height: " + shpFile.Height + "\n");

                // 创建动画播放器但不开始播放（将在 Loaded 事件中播放）
                File.AppendAllText(_logFile, "Creating animation player\n");
                _shpAnimationPlayer = new ShpAnimationPlayer(shpFile, AnimationImage);
                // 订阅动画完成事件
                _shpAnimationPlayer.AnimationCompleted += ShpAnimationPlayer_AnimationCompleted;
                File.AppendAllText(_logFile, "Animation player created, will start playback in Loaded event\n");
                File.AppendAllText(_logFile, "AnimationCompleted event subscribed\n");
            }
            catch (Exception ex)
            {
                // 记录错误但不退出程序
                File.AppendAllText(_logFile, "Error loading SHP animation\n");
                File.AppendAllText(_logFile, "Message: " + ex.Message + "\n");
                File.AppendAllText(_logFile, "Stack trace: " + ex.StackTrace + "\n");
            }
        }



        /// <summary>
        /// 从 Setup.mix 文件加载音频并保存到临时文件
        /// </summary>
        /// <param name="hashValue">音频文件的哈希值</param>
        /// <returns>临时文件路径或 null</returns>
        private static string? LoadAudioFromMix(string hashValue)
        {
            try
            {
                // 加载 Setup.mix 文件
                MixFile mixFile = new(SetupMixPath);

                // 尝试获取指定哈希值和类型的音频
                byte[]? audioData = mixFile.GetAudioByHash(hashValue);

                if (audioData != null)
                {
                    // 保存音频数据到临时文件，使用哈希值命名
                    string tempFile = Path.Combine(Path.GetTempPath(), $"{hashValue}.wav");
                    File.WriteAllBytes(tempFile, audioData);
                    return tempFile;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载按钮点击音效
        /// </summary>
        private void LoadButtonClickSound()
        {
            _buttonClickSoundFile = LoadAudioFromMix("C7A23518");
        }

        /// <summary>
        /// 加载背景音乐
        /// </summary>
        private void LoadBackgroundMusic()
        {
            _backgroundMusicFile = LoadAudioFromMix("D6A1C973");
        }

        /// <summary>
        /// 播放音频文件
        /// </summary>
        /// <param name="player">MediaPlayer 实例</param>
        /// <param name="audioFile">音频文件路径</param>
        private void PlayAudio(System.Windows.Media.MediaPlayer player, string? audioFile)
        {
            try
            {
                if (!string.IsNullOrEmpty(audioFile) && File.Exists(audioFile))
                {
                    // 停止当前播放
                    player.Stop();

                    // 重新打开音频文件并播放
                    player.Open(new Uri(audioFile));
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        private void PlayBackgroundMusic()
        {
            PlayAudio(_backgroundMusicPlayer, _backgroundMusicFile);
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        private void PlayButtonClickSound()
        {
            try
            {
                if (!string.IsNullOrEmpty(_buttonClickSoundFile) && File.Exists(_buttonClickSoundFile))
                {
                    // 创建一个临时的MediaPlayer实例来播放音效
                    // 这样可以避免每次都重置主MediaPlayer实例
                    var tempPlayer = new System.Windows.Media.MediaPlayer();
                    tempPlayer.Open(new Uri(_buttonClickSoundFile));
                    tempPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing button click sound: {ex.Message}");
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is not null and T)
                {
                    return (T)child;
                }
                else if (child != null)
                {
                    T? childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is not null and T)
                {
                    yield return (T)child;
                }
                if (child != null)
                {
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }



        /// <summary>
        /// 重新加载并显示语言字符串
        /// </summary>
        private void ReloadLanguageStrings()
        {
            // 清空现有的文本
            RadarTextStackPanel.Children.Clear();
            // 重新加载语言字符串
            LoadAndDisplayRadarStrings();
        }

        /// <summary>
        /// 加载并显示底部文本（根据当前页码从配置中获取）
        /// </summary>
        private void LoadBottomText()
        {
            try
            {
                File.AppendAllText(_logFile, $"Starting to load bottom text for page {_currentPage}\n");

                // 获取当前页面的底部文字配置
                if (!_pageBottomTextConfig.TryGetValue(_currentPage, out var config))
                {
                    File.AppendAllText(_logFile, $"No bottom text config defined for page {_currentPage}\n");
                    return;
                }

                int stringId = config.StringId;
                int displayDurationMs = config.DisplayDurationMs;
                File.AppendAllText(_logFile, $"Using string ID: {stringId}, display duration: {displayDurationMs}ms\n");

                // Language.dll文件路径
                string languageDllPath = "Assets/RA1/Setup/Language.dll";

                // 检查文件是否存在
                if (!File.Exists(languageDllPath))
                {
                    File.AppendAllText(_logFile, "Language.dll file not found\n");
                    return;
                }

                // 确定要使用的语言
                ushort languageId = GetLanguageIdForCurrentLanguage();
                File.AppendAllText(_logFile, $"Using language ID: {languageId}\n");

                // 读取字符串
                string? text = ReadStringFromLanguageDll(languageDllPath, stringId, languageId);
                if (!string.IsNullOrEmpty(text))
                {
                    // 显示文本
                    if (BottomTextBlock != null)
                    {
                        BottomTextBlock.Text = text;
                        BottomTextBlock.Visibility = Visibility.Visible;
                        File.AppendAllText(_logFile, $"Bottom text loaded and displayed: '{text}'\n");
                    }

                    // 设置定时器，在指定时长后隐藏文本
                    StartBottomTextTimer(displayDurationMs);
                }
                else
                {
                    File.AppendAllText(_logFile, $"Failed to read string ID {stringId}\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading bottom text: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 启动底部文字显示定时器
        /// </summary>
        /// <param name="durationMs">显示时长（毫秒）</param>
        private void StartBottomTextTimer(int durationMs)
        {
            // 停止之前的定时器
            StopBottomTextTimer();

            // 创建新的定时器
            _bottomTextTimer = new System.Timers.Timer(durationMs);
            _bottomTextTimer.AutoReset = false;
            _bottomTextTimer.Elapsed += (sender, e) =>
            {
                // 在UI线程上执行隐藏操作
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (BottomTextBlock != null)
                    {
                        BottomTextBlock.Visibility = Visibility.Collapsed;
                        File.AppendAllText(_logFile, $"Bottom text hidden after {durationMs}ms\n");
                    }
                    StopBottomTextTimer();
                });
            };

            // 启动定时器
            _bottomTextTimer.Start();
            File.AppendAllText(_logFile, $"Bottom text timer started with duration: {durationMs}ms\n");
        }

        /// <summary>
        /// 停止底部文字显示定时器
        /// </summary>
        private void StopBottomTextTimer()
        {
            if (_bottomTextTimer != null)
            {
                _bottomTextTimer.Stop();
                _bottomTextTimer.Dispose();
                _bottomTextTimer = null;
                File.AppendAllText(_logFile, "Bottom text timer stopped\n");
            }
        }

        /// <summary>
        /// 析构函数，用于释放资源
        /// </summary>
        ~MainWindow()
        {
            // 停止底部文字定时器
            StopBottomTextTimer();
        }

        /// <summary>
        /// 开始播放 SHP 动画
        /// </summary>
        private void StartShpAnimation()
        {
            if (_shpAnimationPlayer != null)
            {
                // 每次点击按钮时，根据当前页面决定播放模式
                File.AppendAllText(_logFile, "Starting SHP animation playback on language button click\n");

                // 先停止当前动画
                _shpAnimationPlayer.Stop();

                // 确保AnimationImage可见
                if (AnimationImage != null)
                {
                    AnimationImage.Visibility = Visibility.Visible;
                    File.AppendAllText(_logFile, "AnimationImage visibility set to Visible\n");
                }

                // 检查是否在第1页（使用当前页码变量）
                bool isPage1 = _currentPage == 1;

                if (isPage1)
                {
                    // 第1页：正序播放动画，从第一帧开始
                    File.AppendAllText(_logFile, "Page 1 detected, starting forward animation\n");
                    _shpAnimationPlayer.IsReverse = false;
                    _shpAnimationPlayer.Reset();
                }
                else
                {
                    // 其他页：正常播放
                    File.AppendAllText(_logFile, "Other page detected, starting normal animation\n");
                    _shpAnimationPlayer.IsReverse = false;
                    _shpAnimationPlayer.Reset();
                }

                // 开始播放
                _shpAnimationPlayer.Play();
                File.AppendAllText(_logFile, "SHP animation playback started\n");
            }
            else
            {
                File.AppendAllText(_logFile, "_shpAnimationPlayer is null, cannot start playback\n");
            }
        }

        /// <summary>
        /// 动画播放完成事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void ShpAnimationPlayer_AnimationCompleted(object? sender, EventArgs e)
        {
            File.AppendAllText(_logFile, "SHP animation completed\n");
            
            // 检查是否在第1页（使用当前页码变量）
            bool isPage1 = _currentPage == 1;

            if (isPage1)
            {
                // 检查是否是倒放模式
                bool isReverse = _shpAnimationPlayer != null && _shpAnimationPlayer.IsReverse;
                
                if (isReverse)
                {
                    // 倒放模式（从第2页返回或开场）：保持动画可见并停留在第一帧，不跳转页面
                    File.AppendAllText(_logFile, "Page 1 reverse animation completed, keeping animation visible at first frame\n");
                    if (AnimationImage != null)
                    {
                        AnimationImage.Visibility = Visibility.Visible;
                        File.AppendAllText(_logFile, "AnimationImage visibility set to Visible\n");
                    }
                }
                else
                {
                    // 正常模式（下一步按钮触发）：跳转到第二页
                    File.AppendAllText(_logFile, "Page 1 normal animation completed, switching to Page 2\n");
                    SwitchToPage(2);
                }
            }
            else
            {
                // 非第1页：动画播放完毕后不跳转页面
                File.AppendAllText(_logFile, "Non-Page 1 detected, animation completed without page switch\n");
            }
        }

        /// <summary>
        /// 切换到指定页码
        /// </summary>
        /// <param name="pageNumber">目标页码</param>
        private void SwitchToPage(int pageNumber)
        {
            _currentPage = pageNumber;
            File.AppendAllText(_logFile, $"Switching to page {pageNumber}\n");

            // 取消之前的加载任务
            CancelLoadStringsTask();

            // 停止当前的底部文字定时器
            StopBottomTextTimer();

            // 更新按钮状态
            UpdateNavigationButtons();

            // 清空现有的文本
            RadarTextStackPanel.Children.Clear();

            if (pageNumber == 1)
            {
                // 第一页
                // 隐藏第二页特有元素
                LicenseBorder.Visibility = Visibility.Collapsed;
                IAgreeToTheseTermsTextBlock.Visibility = Visibility.Collapsed;
                AgreeButtonImage.Visibility = Visibility.Collapsed;
                // 隐藏第三页特有元素
                InputFieldsStackPanel.Visibility = Visibility.Collapsed;

                // 调整动画控件位置为第一页位置
                if (AnimationImage != null)
                {
                    AnimationImage.Margin = new Thickness(0, 75, 0, 0);
                    AnimationImage.Width = 470;
                    File.AppendAllText(_logFile, "AnimationImage margin set to (0,75,0,0) and width set to 470 for Page 1\n");
                }

                // 显示底部文本
                LoadBottomText();

                // 加载并播放第一页的动画
                LoadAndPlayPage1Animation();

                // 从Language.dll读取字符串并显示
                LoadAndDisplayRadarStrings();


            }
            else if (pageNumber == 2)
            {
                // 第二页
                // 隐藏第一页特有元素
                
                // 调整动画控件位置为第二页位置（紧贴顶部）
                if (AnimationImage != null)
                {
                    AnimationImage.Margin = new Thickness(0, 0, 0, 0);
                    AnimationImage.Width = 472;
                    File.AppendAllText(_logFile, "AnimationImage margin set to (0,0,0,0) and width set to 472 for Page 2\n");
                }

                // 确保许可证边框初始状态为隐藏
                if (LicenseBorder != null)
                {
                    LicenseBorder.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "LicenseBorder visibility reset to Collapsed\n");
                }

                // 确保同意条款文本初始状态为隐藏
                if (IAgreeToTheseTermsTextBlock != null)
                {
                    IAgreeToTheseTermsTextBlock.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "IAgreeToTheseTermsTextBlock visibility reset to Collapsed\n");
                }

                // 确保同意按钮初始状态为隐藏
                if (AgreeButtonImage != null)
                {
                    AgreeButtonImage.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "AgreeButtonImage visibility reset to Collapsed\n");
                }

                // 确保第三页特有元素初始状态为隐藏
                if (LicenseStackPanel != null)
                {
                    LicenseStackPanel.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "LicenseStackPanel visibility reset to Collapsed\n");
                }
                // 隐藏输入框区域
                InputFieldsStackPanel.Visibility = Visibility.Collapsed;

                // 显示底部文本
                LoadBottomText();

                // 加载并播放第二页的动画
                LoadAndPlayPage2Animation();


            }
            else if (pageNumber == 3)
            {
                // 第三页
                // 隐藏其他页面特有元素
                if (LicenseBorder != null)
                {
                    LicenseBorder.Visibility = Visibility.Collapsed;
                }
                if (IAgreeToTheseTermsTextBlock != null)
                {
                    IAgreeToTheseTermsTextBlock.Visibility = Visibility.Collapsed;
                }
                if (AgreeButtonImage != null)
                {
                    AgreeButtonImage.Visibility = Visibility.Collapsed;
                }

                // 调整动画控件位置为第三页位置（紧贴顶部）
                if (AnimationImage != null)
                {
                    AnimationImage.Margin = new Thickness(0, 0, 0, 0);
                    AnimationImage.Width = 472;
                    File.AppendAllText(_logFile, "AnimationImage margin set to (0,0,0,0) and width set to 472 for Page 3\n");
                }

                // 显示底部文本
                LoadBottomText();

                // 显示第三页特有元素（无边框许可证内容区域）
                if (LicenseStackPanel != null)
                {
                    LicenseStackPanel.Visibility = Visibility.Visible;
                    File.AppendAllText(_logFile, "LicenseStackPanel visibility set to Visible\n");
                }

                // 从Language.dll ID 210读取许可证内容并显示
                LoadLicenseContentFromLanguageDll();

                // 直接沿用第二页的动画，不再重新加载
                File.AppendAllText(_logFile, "Using existing animation from Page 2 for Page 3\n");

                // 从Language.dll读取字符串并显示
                LoadAndDisplayRadarStrings();

                // 显示输入框区域
                InputFieldsStackPanel.Visibility = Visibility.Visible;
                // 默认选中第一个输入框
                InputField1.Focus();

            }
        }

        /// <summary>
        /// 输入框文本输入验证，只允许输入数字
        /// </summary>
        private void InputField_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // 检查输入是否为数字
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 输入框获得焦点事件处理
        /// </summary>
        private void InputField_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// 输入框失去焦点事件处理
        /// </summary>
        private void InputField_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// 输入框文本变化时的处理，用于自动切换到下一个输入框
        /// </summary>
        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 检查输入框是否已满
                if (textBox.Text.Length == textBox.MaxLength)
                {
                    // 根据当前输入框切换到下一个
                    switch (textBox.Name)
                    {
                        case "InputField1":
                            InputField2.Focus();
                            break;
                        case "InputField2":
                            InputField3.Focus();
                            break;
                        case "InputField3":
                            InputField4.Focus();
                            break;
                        case "InputField4":
                            // 最后一个输入框，不需要切换
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 更新导航按钮状态
        /// </summary>
        private void UpdateNavigationButtons()
        {
            // 显示导航按钮容器
            NavigationButtonsStackPanel.Visibility = Visibility.Visible;

            // 第一页隐藏后退按钮
            if (BackButton != null)
            {
                BackButton.Visibility = _currentPage == 1 ? Visibility.Collapsed : Visibility.Visible;
                File.AppendAllText(_logFile, $"BackButton visibility set to {(BackButton.Visibility == Visibility.Visible ? "Visible" : "Collapsed")} for Page {_currentPage}\n");
            }
        }

        /// <summary>
        /// 加载并播放第一页的动画
        /// </summary>
        private void LoadAndPlayPage1Animation()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading and playing Page1 animation with hash: 2012EC16\n");

                // 检查AnimationImage是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[]? shpData = mixFile.GetShpByHash("2012EC16");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for Page1 animation\n");
                    return;
                }

                // 获取PAL文件数据
                string palHash = "397C46E0";
                byte[]? palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for Page1 animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 创建动画播放器
                _shpAnimationPlayer = new ShpAnimationPlayer(shpFile, AnimationImage);

                // 添加动画播放完成事件处理程序
                _shpAnimationPlayer.AnimationCompleted += ShpAnimationPlayer_AnimationCompleted;

                // 第1页启动时，自动倒放动画
                _shpAnimationPlayer.IsReverse = true;
                _shpAnimationPlayer.ResetToLastFrame();
                _shpAnimationPlayer.Play();
                File.AppendAllText(_logFile, "Page1 animation playback started in reverse\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error loading Page1 animation: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 更新第一页的UI文本
        /// </summary>


        /// <summary>
        /// 从Language.dll ID 210读取许可证内容并显示在第三页
        /// </summary>
        private void LoadLicenseContentFromLanguageDll()
        {
            try
            {
                File.AppendAllText(_logFile, "Starting to load license content from Language.dll ID 210\n");

                // Language.dll文件路径
                string languageDllPath = "Assets/RA1/Setup/Language.dll";

                // 检查文件是否存在
                if (!File.Exists(languageDllPath))
                {
                    File.AppendAllText(_logFile, "Language.dll file not found\n");
                    return;
                }

                // 确定要使用的语言
                ushort languageId = GetLanguageIdForCurrentLanguage();
                File.AppendAllText(_logFile, $"Using language ID: {languageId}\n");

                // 读取字符串（ID 210）
                string? text = ReadStringFromLanguageDll(languageDllPath, 210, languageId);
                if (!string.IsNullOrEmpty(text))
                {
                    // 检查是否包含 %s 占位符
                    if (text.Contains("%s"))
                    {
                        // 读取 ID 18 的文本用于替换 %s
                        string? replacementText = ReadStringFromLanguageDll(languageDllPath, 18, languageId);
                        if (!string.IsNullOrEmpty(replacementText))
                        {
                            // 替换 %s 占位符
                            text = text.Replace("%s", replacementText);
                            File.AppendAllText(_logFile, "Replaced %s with text from ID 18\n");
                        }
                        else
                        {
                            File.AppendAllText(_logFile, "Failed to read string ID 18 for replacement\n");
                        }
                    }

                    // 显示文本
                    if (LicenseTextBlockPage3 != null)
                    {
                        LicenseTextBlockPage3.Text = text;
                        File.AppendAllText(_logFile, "License content loaded and displayed from ID 210\n");
                    }
                }
                else
                {
                    File.AppendAllText(_logFile, "Failed to read string ID 210\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading license content: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 加载并播放第二页的动画
        /// </summary>
        private void LoadAndPlayPage2Animation()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading and playing Page2 animation with hash: D6D75E64\n");

                // 检查AnimationImage是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[]? shpData = mixFile.GetShpByHash("D6D75E64");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for Page2 animation\n");
                    return;
                }

                // 获取PAL文件数据
                string palHash = "397C46E0";
                byte[]? palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for Page2 animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 创建动画播放器
                _shpAnimationPlayer = new ShpAnimationPlayer(shpFile, AnimationImage);

                // 添加动画播放完成事件处理程序
                _shpAnimationPlayer.AnimationCompleted += Page2Animation_Completed;

                // 开始播放动画
                _shpAnimationPlayer.Play();
                File.AppendAllText(_logFile, "Page2 animation playback started\n");

                // 播放第二页的音效
                PlayPage2Sounds();
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error loading Page2 animation: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 加载并播放第三页的动画
        /// </summary>
        private void LoadAndPlayPage3Animation()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading and playing Page3 animation with hash: D6D75E64\n");

                // 检查AnimationImage是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[]? shpData = mixFile.GetShpByHash("D6D75E64");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for Page3 animation\n");
                    return;
                }

                // 获取PAL文件数据
                string palHash = "397C46E0";
                byte[]? palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for Page3 animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 创建动画播放器
                _shpAnimationPlayer = new ShpAnimationPlayer(shpFile, AnimationImage);

                // 添加动画播放完成事件处理程序
                _shpAnimationPlayer.AnimationCompleted += Page3Animation_Completed;

                // 开始播放动画
                _shpAnimationPlayer.Play();
                File.AppendAllText(_logFile, "Page3 animation playback started\n");

                // 播放第三页的音效
                PlayPage2Sounds();
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error loading Page3 animation: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 第三页动画播放完成事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Page3Animation_Completed(object? sender, EventArgs e)
        {
            File.AppendAllText(_logFile, "Page3 animation completed\n");
            // 第三页动画播放完毕后不跳转页面
        }

        /// <summary>
        /// 播放第二页的音效
        /// </summary>
        private void PlayPage2Sounds()
        {
            try
            {
                string? soundFile1 = LoadAudioFromMix("B1C914DD");
                PlayAudio(_soundPlayer, soundFile1);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error playing Page2 sounds: " + ex.Message + "\n");
            }
        }





        /// <summary>
        /// 上一步按钮点击事件
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            // 切换到上一页
            if (_currentPage > 1)
            {
                SwitchToPage(_currentPage - 1);
            }
        }

        /// <summary>
        /// 下一步按钮点击事件
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            if (_currentPage == 1)
            {
                // 第1页：执行与原来语言按钮相同的逻辑，只是不需要切换语言
                // 重新加载并显示语言字符串
                ReloadLanguageStrings();
                // 开始播放 SHP 动画
                StartShpAnimation();
            }
            else if (_currentPage == 2)
            {
                // 第2页：跳转到第三页
                SwitchToPage(3);
            }
            else
            {
                // 其他页：执行原来的逻辑
                // 隐藏许可证内容和同意条款文本
                if (LicenseBorder != null)
                {
                    LicenseBorder.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "LicenseBorder visibility set to Collapsed\n");
                }
                if (IAgreeToTheseTermsTextBlock != null)
                {
                    IAgreeToTheseTermsTextBlock.Visibility = Visibility.Collapsed;
                    File.AppendAllText(_logFile, "IAgreeToTheseTermsTextBlock visibility set to Collapsed\n");
                }

                // 显示动画 hash 134B6332 的第一帧
                ShowAnimationFirstFrame("134B6332");
            }
        }

        /// <summary>
        /// 显示指定动画的第一帧
        /// </summary>
        /// <param name="animationHash">动画的哈希值</param>
        private void ShowAnimationFirstFrame(string animationHash)
        {
            try
            {
                File.AppendAllText(_logFile, $"Loading first frame of animation with hash: {animationHash}\n");

                // 检查AnimationImage是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[]? shpData = mixFile.GetShpByHash(animationHash);
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, $"Failed to load SHP file for animation {animationHash}\n");
                    return;
                }

                // 获取PAL文件数据（使用与第二页相同的调色板）
                string palHash = "397C46E0";
                byte[]? palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 获取第一帧图像并显示
                var frames = shpFile.GetFrames();
                if (frames.Count > 0)
                {
                    AnimationImage.Source = frames[0];
                    File.AppendAllText(_logFile, $"First frame of animation {animationHash} displayed\n");
                }
                else
                {
                    File.AppendAllText(_logFile, $"No frames found in SHP file for animation {animationHash}\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error displaying first frame of animation: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            Application.Current.Shutdown();
        }

        // 存储动画帧用于同意按钮
        private List<System.Windows.Media.Imaging.BitmapSource>? _agreeButtonAnimationFrames;

        /// <summary>
        /// 第二页动画播放完成事件处理程序
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Page2Animation_Completed(object? sender, EventArgs e)
        {
            try
            {
                File.AppendAllText(_logFile, "Page2 animation completed, showing license agreement\n");

                // 显示许可证内容
                if (LicenseBorder != null)
                {
                    LicenseBorder.Visibility = Visibility.Visible;
                    File.AppendAllText(_logFile, "LicenseBorder visibility set to Visible\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "LicenseBorder is null\n");
                }

                // 显示同意条款文本
                if (IAgreeToTheseTermsTextBlock != null)
                {
                    IAgreeToTheseTermsTextBlock.Visibility = Visibility.Visible;
                    File.AppendAllText(_logFile, "IAgreeToTheseTermsTextBlock visibility set to Visible\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "IAgreeToTheseTermsTextBlock is null\n");
                }

                // 加载并显示同意按钮动画的第一帧
                LoadAgreeButtonAnimation();

                // 清空现有内容并加载第二页的雷达文案
                RadarTextStackPanel.Children.Clear();
                LoadAndDisplayRadarStrings(_currentPage);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error in Page2Animation_Completed: " + ex.Message + "\n");
            }
        }



        /// <summary>
        /// 加载同意按钮的动画帧
        /// </summary>
        private void LoadAgreeButtonAnimation()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading agree button animation with hash: 134B6332\n");

                // 检查AgreeButtonImage是否存在
                if (AgreeButtonImage == null)
                {
                    File.AppendAllText(_logFile, "AgreeButtonImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[]? shpData = mixFile.GetShpByHash("134B6332");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for agree button animation\n");
                    return;
                }

                // 获取PAL文件数据（使用指定的调色板 hash 297C46E0）
                string palHash = "297C46E0";
                byte[]? palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for agree button animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 存储动画帧
                _agreeButtonAnimationFrames = shpFile.GetFrames();

                // 显示第一帧
                if (_agreeButtonAnimationFrames.Count > 0)
                {
                    AgreeButtonImage.Source = _agreeButtonAnimationFrames[0];
                    AgreeButtonImage.Visibility = Visibility.Visible;
                    File.AppendAllText(_logFile, "Agree button animation first frame displayed\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "No frames found in SHP file for agree button animation\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error loading agree button animation: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 同意按钮点击事件处理程序
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void AgreeButtonImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PlayButtonClickSound();

            // 显示第二帧
            if (_agreeButtonAnimationFrames != null && _agreeButtonAnimationFrames.Count > 1)
            {
                AgreeButtonImage.Source = _agreeButtonAnimationFrames[1];
                File.AppendAllText(_logFile, "Agree button animation second frame displayed\n");
            }

            // 这里可以添加同意后的逻辑
        }
    }
}