using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace RA2Installer
{
    /// <summary>
    /// 用于处理 Red Alert 2 的 mix 文件格式
    /// </summary>
    public class MixFile
    {
        private string _filePath;
        private Dictionary<string, byte[]> _fileEntries;

        /// <summary>
        /// 初始化 MixFile 实例
        /// </summary>
        /// <param name="filePath">mix 文件的路径</param>
        public MixFile(string filePath)
        {
            _filePath = filePath;
            _fileEntries = new Dictionary<string, byte[]>();
            LoadMixFile();
        }

        /// <summary>
        /// 加载 mix 文件并解析其中的文件条目
        /// </summary>
        private void LoadMixFile()
        {
            using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                try
                {
                    // 读取 mix 文件头
                    var header = reader.ReadBytes(4);

                    // 检查是否是加密的 mix 文件
                    var isEncrypted = header[0] == 0x56 && header[1] == 0x49 && header[2] == 0x4D && header[3] == 0x58;

                    if (isEncrypted)
                    {
                        // 处理加密的 mix 文件
                        ProcessEncryptedMixFile(stream, reader);
                    }
                    else
                    {
                        // 处理未加密的 mix 文件
                        ProcessUnencryptedMixFile(stream, reader);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Mix file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理未加密的 mix 文件
        /// </summary>
        private void ProcessUnencryptedMixFile(FileStream stream, BinaryReader reader)
        {
            try
            {
                // 重新定位到文件开头
                stream.Position = 0;
                
                // 读取 mix 文件头（根据 XCC 代码，这是一个 union）
                // 首先读取 2 字节的文件计数
                short fileCountShort = reader.ReadInt16();
                
                // 读取 4 字节的总大小
                int totalSize = reader.ReadInt32();
                
                // 检查文件计数是否合理
                int fileCount = fileCountShort;
                if (fileCount <= 0 || fileCount > 10000)
                {
                    return;
                }
                
                // 计算索引大小
                int indexSize = fileCount * 12; // 每个条目 12 字节
                
                // 计算文件数据开始位置
                long dataStartOffset = 6 + indexSize; // 2字节文件计数 + 4字节总大小 + 索引大小
                
                // 检查文件大小是否合理
                long expectedFileSize = dataStartOffset + totalSize;
                
                // 读取文件条目
                for (int i = 0; i < fileCount; i++)
                {
                    try
                    {
                        var fileNameHash = reader.ReadInt32();
                        var fileOffset = reader.ReadInt32();
                        var fileSize = reader.ReadInt32();
                        
                        // 计算实际的文件偏移量
                        long actualOffset = dataStartOffset + fileOffset;
                        
                        // 检查值是否合理
                        if (actualOffset < 0 || actualOffset >= stream.Length || fileSize <= 0 || fileSize > stream.Length - actualOffset)
                        {
                            continue;
                        }
                        
                        // 保存文件条目信息
                        _fileInfos[fileNameHash] = new FileInfo { Offset = actualOffset, Size = fileSize };
                    }
                    catch
                    {
                        // 跳过无效的文件条目
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
        }

        /// <summary>
        /// 处理加密的 mix 文件
        /// </summary>
        private void ProcessEncryptedMixFile(FileStream stream, BinaryReader reader)
        {
            try
            {
                // 读取标志
                var flags = reader.ReadInt32();
                
                // 读取密钥源
                var keySource = reader.ReadBytes(80);
                
                // 计算 Blowfish 密钥
                var key = CalculateBlowfishKey(keySource);
                
                // 读取加密的文件头
                var encryptedHeader = reader.ReadBytes(8);
                
                // 使用 Blowfish 解密文件头
                var decryptedHeader = DecryptWithBlowfish(encryptedHeader, key);
                
                // 从解密后的文件头中读取文件计数
                using (var headerReader = new BinaryReader(new MemoryStream(decryptedHeader)))
                {
                    var fileCount = headerReader.ReadInt32();
                    
                    // 检查文件计数是否合理
                    if (fileCount <= 0 || fileCount > 10000)
                    {
                        return;
                    }
                    
                    // 计算文件条目大小（每个条目 12 字节，对齐到 8 字节）
                    int entrySize = fileCount * 12;
                    int paddedEntrySize = (entrySize + 7) & ~7; // 对齐到 8 字节
                    
                    // 读取加密的文件条目
                    var encryptedEntries = reader.ReadBytes(paddedEntrySize);
                    
                    // 解密文件条目
                    var decryptedEntries = DecryptWithBlowfish(encryptedEntries, key);
                    
                    // 解析文件条目
                    using (var entryReader = new BinaryReader(new MemoryStream(decryptedEntries)))
                    {
                        // 跳过前 2 字节（文件计数已经读取）
                        entryReader.ReadBytes(2);
                        
                        for (int i = 0; i < fileCount; i++)
                        {
                            try
                            {
                                var fileNameHash = entryReader.ReadInt32();
                                var fileOffset = entryReader.ReadInt32();
                                var fileSize = entryReader.ReadInt32();
                                
                                // 计算实际的文件偏移量（加密的 mix 文件，文件数据从 92 + 加密条目大小后开始）
                                long actualOffset = 92 + paddedEntrySize + fileOffset;
                                
                                // 检查值是否合理
                                if (actualOffset < 0 || actualOffset >= stream.Length || fileSize <= 0 || fileSize > stream.Length - actualOffset)
                                {
                                    continue;
                                }
                                
                                // 保存文件条目信息
                                _fileInfos[fileNameHash] = new FileInfo { Offset = actualOffset, Size = fileSize };
                            }
                            catch
                            {
                                // 跳过无效的文件条目
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
        }

        /// <summary>
        /// 计算 Blowfish 密钥
        /// </summary>
        private byte[] CalculateBlowfishKey(byte[] keySource)
        {
            // 参考 XCC 的实现，计算 Blowfish 密钥
            byte[] key = new byte[56];
            for (int i = 0; i < 56; i++)
            {
                key[i] = keySource[i % keySource.Length];
            }
            return key;
        }

        /// <summary>
        /// 使用 BouncyCastle.Cryptography 库的 Blowfish 算法解密数据
        /// </summary>
        private byte[] DecryptWithBlowfish(byte[] encryptedData, byte[] key)
        {
            try
            {
                // 创建 BouncyCastle.Cryptography 的 Blowfish 引擎
                var engine = new Org.BouncyCastle.Crypto.Engines.BlowfishEngine();
                
                // 初始化引擎为解密模式
                engine.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
                
                // 解密数据（8 字节为一组）
                byte[] decryptedData = new byte[encryptedData.Length];
                for (int i = 0; i < encryptedData.Length; i += 8)
                {
                    if (i + 8 <= encryptedData.Length)
                    {
                        // 读取 8 字节为一组
                        byte[] block = new byte[8];
                        Array.Copy(encryptedData, i, block, 0, 8);
                        
                        // 解密
                        engine.ProcessBlock(block, 0, block, 0);
                        
                        // 写入解密后的数据
                        Array.Copy(block, 0, decryptedData, i, 8);
                    }
                    else
                    {
                        // 处理不足 8 字节的部分
                        Array.Copy(encryptedData, i, decryptedData, i, encryptedData.Length - i);
                    }
                }
                
                return decryptedData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting data with Blowfish: {ex.Message}");
                return encryptedData; // 出错时返回原始数据
            }
        }

        /// <summary>
        /// 文件信息类，用于存储文件的偏移量和大小
        /// </summary>
        private class FileInfo
        {
            public long Offset { get; set; }
            public int Size { get; set; }
        }

        // 存储文件信息的字典，key 是文件名哈希值，value 是文件信息
        private Dictionary<int, FileInfo> _fileInfos = new Dictionary<int, FileInfo>();

        /// <summary>
        /// 根据哈希值读取文件内容
        /// </summary>
        /// <param name="fileNameHash">文件名哈希值</param>
        /// <returns>文件内容</returns>
        private byte[] ReadFileByHash(int fileNameHash)
        {
            if (_fileInfos.TryGetValue(fileNameHash, out var fileInfo))
            {
                using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = fileInfo.Offset;
                    using (var reader = new BinaryReader(stream))
                    {
                        return reader.ReadBytes(fileInfo.Size);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 解密整数（用于处理加密的 mix 文件）
        /// </summary>
        /// <param name="value">要解密的整数</param>
        /// <returns>解密后的整数</returns>
        private int DecryptInt(int value)
        {
            // 这里使用简单的解密算法，实际算法可能更复杂
            return unchecked(value ^ (int)0xDEADBEEF);
        }

        /// <summary>
        /// 尝试获取指定文件名的文件内容
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="fileContent">文件内容</param>
        /// <returns>是否找到文件</returns>
        public bool TryGetFile(string fileName, out byte[] fileContent)
        {
            return _fileEntries.TryGetValue(fileName, out fileContent);
        }

        /// <summary>
        /// 获取所有文件条目
        /// </summary>
        /// <returns>文件条目字典</returns>
        public Dictionary<string, byte[]> GetAllFiles()
        {
            return _fileEntries;
        }

        /// <summary>
        /// 从 mix 文件中提取图片并转换为 BitmapImage
        /// </summary>
        /// <param name="fileName">图片文件名</param>
        /// <returns>BitmapImage 实例</returns>
        public BitmapImage GetImage(string fileName)
        {
            if (TryGetFile(fileName, out var fileContent))
            {
                using (var stream = new MemoryStream(fileContent))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            return null;
        }

        /// <summary>
        /// 尝试从 mix 文件中查找背景图片
        /// </summary>
        /// <returns>背景图片的 BitmapImage 实例</returns>
        public BitmapImage GetBackgroundImage()
        {
            // 尝试查找可能的背景图片文件
            foreach (var entry in _fileEntries)
            {
                try
                {
                    using (var stream = new MemoryStream(entry.Value))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch
                {
                    // 跳过不是图片的文件
                }
            }
            return null;
        }

        /// <summary>
        /// 根据文件名哈希值和类型从 mix 文件中查找图片
        /// </summary>
        /// <param name="fileNameHash">文件名哈希值</param>
        /// <param name="fileType">文件类型（如 "bmp"）</param>
        /// <returns>找到的图片的 BitmapImage 实例</returns>
        public BitmapImage GetImageByHash(string fileNameHash, string fileType)
        {
            try
            {
                // 将字符串哈希值转换为整数
                int hashValue = Convert.ToInt32(fileNameHash, 16);
                
                // 检查哈希值是否存在于存储的哈希值中
                if (!_fileInfos.ContainsKey(hashValue))
                {
                    // 尝试不同的哈希值表示方式
                    // 尝试将哈希值转换为无符号整数
                    uint uintHashValue = Convert.ToUInt32(fileNameHash, 16);
                    
                    // 尝试将无符号整数转换为有符号整数
                    int signedHashValue = unchecked((int)uintHashValue);
                    
                    if (_fileInfos.ContainsKey(signedHashValue))
                    {
                        hashValue = signedHashValue;
                    }
                    else
                    {
                        return null;
                    }
                }
                
                // 使用哈希值读取文件内容
                var fileContent = ReadFileByHash(hashValue);
                if (fileContent != null)
                {
                    try
                    {
                        using (var stream = new MemoryStream(fileContent))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading image from hash lookup: {ex.Message}");
                        // 跳过不是图片的文件
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetImageByHash: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// 直接从文件中提取第一个 BMP 图片
        /// </summary>
        /// <returns>找到的 BMP 图片的 BitmapImage 实例</returns>
        public BitmapImage ExtractFirstBmpImage()
        {
            try
            {


                using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                {
                    // BMP 文件头签名
                    byte[] bmpSignature = new byte[] { 0x42, 0x4D }; // "BM"

                    // 搜索文件中的 BMP 签名
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    long position = 0;

                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < bytesRead - 1; i++)
                        {
                            if (buffer[i] == bmpSignature[0] && buffer[i + 1] == bmpSignature[1])
                            {
                                // 找到 BMP 签名，计算位置
                                long bmpPosition = position + i;


                                // 重置流位置到 BMP 开始
                                stream.Position = bmpPosition;

                                // 读取 BMP 文件
                                using (var memoryStream = new MemoryStream())
                                {
                                    // 读取剩余的文件内容
                                    stream.CopyTo(memoryStream);
                                    memoryStream.Position = 0;

                                    try
                                    {
                                        var bitmap = new BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.StreamSource = memoryStream;
                                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmap.EndInit();
                                        bitmap.Freeze();

                                        return bitmap;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error loading BMP image: {ex.Message}");
                                    }
                                }
                            }
                        }
                        position += bytesRead;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting BMP image: {ex.Message}");
            }

            return null;
        }
    }
}

namespace RA2Installer.Helpers
{
    /// <summary>
    /// 用于查找和处理 Setup.mix 文件的辅助类
    /// </summary>
    public static class MixFileHelper
    {
        /// <summary>
        /// 查找 Setup.mix 文件
        /// </summary>
        /// <returns>Setup.mix 文件的路径，如果未找到则返回 null</returns>
        public static string FindSetupMixFile()
        {
            // 首先检查当前目录
            var currentDir = Directory.GetCurrentDirectory();
            var setupMixPath = Path.Combine(currentDir, "Setup.mix");
            if (File.Exists(setupMixPath))
            {
                return setupMixPath;
            }

            // 检查父目录
            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir != null)
            {
                setupMixPath = Path.Combine(parentDir, "Setup.mix");
                if (File.Exists(setupMixPath))
                {
                    return setupMixPath;
                }
            }

            // 检查常见的游戏目录
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "Command & Conquer Red Alert II", "Setup.mix"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EA Games", "Command & Conquer Red Alert II", "Setup.mix"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Red Alert 2", "Setup.mix")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}