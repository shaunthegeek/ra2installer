using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RA2Installer.Resources;

namespace RA2Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 常量：Setup.mix 文件路径
        private const string SetupMixPath = "Assets/RA1/Setup/Setup.mix";

        private System.Windows.Media.MediaPlayer _backgroundMusicPlayer;
        private System.Windows.Media.MediaPlayer _soundPlayer;
        private string _buttonClickSoundFile;
        private string _backgroundMusicFile;
        private ShpAnimationPlayer _shpAnimationPlayer;
        private ShpAnimationPlayer _page2ShpAnimationPlayer;

        // 日志文件路径
        private string _logFile;

        // 当前选择的语言
        private string _currentLanguage;

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
            // 初始化语言为系统默认语言
            InitializeLanguage();

            // 播放背景音乐
            PlayBackgroundMusic();

            // 从Language.dll读取字符串并显示
            LoadAndDisplayLanguageStrings();

            // 不自动启动 SHP 动画，只显示第一帧
            File.AppendAllText(_logFile, "Not starting SHP animation automatically, showing only first frame\n");
        }

        /// <summary>
        /// 从Language.dll读取字符串并显示在界面上
        /// </summary>
        private async Task LoadAndDisplayLanguageStringsAsync()
        {
            try
            {
                File.AppendAllText(_logFile, "Starting to load language strings from Language.dll\n");

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

                // 读取字符串ID 250-254
                int[] stringIds = { 250, 251, 252, 253, 254 };

                foreach (int id in stringIds)
                {
                    string text = ReadStringFromLanguageDll(languageDllPath, id, languageId);
                    if (!string.IsNullOrEmpty(text))
                    {
                        // 创建TextBlock并添加到StackPanel
                        TextBlock textBlock = new TextBlock
                        {
                            Text = text,
                            Foreground = Brushes.Yellow,
                            FontSize = 10,
                            TextAlignment = TextAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                        };
                        LanguageTextStackPanel.Children.Add(textBlock);
                        File.AppendAllText(_logFile, $"Added string ID {id}: {text}\n");
                    }
                    else
                    {
                        File.AppendAllText(_logFile, $"Failed to read string ID {id}\n");
                    }

                    await Task.Delay(1000);
                }

                File.AppendAllText(_logFile, "Language strings loaded and displayed\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading language strings: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 从Language.dll读取字符串并显示在界面上（同步包装方法）
        /// </summary>
        private void LoadAndDisplayLanguageStrings()
        {
            // 调用异步方法
            _ = LoadAndDisplayLanguageStringsAsync();
        }

        /// <summary>
        /// 根据当前选择的语言获取对应的语言ID
        /// </summary>
        /// <returns>语言ID</returns>
        private ushort GetLanguageIdForCurrentLanguage()
        {
            // 默认使用英文
            if (string.IsNullOrEmpty(_currentLanguage))
            {
                return 0x0409; // en-US
            }

            // 根据当前语言选择对应的语言ID
            switch (_currentLanguage)
            {
                case "zh-CN":
                case "zh-TW":
                    return 0x0404; // zh-TW
                default:
                    return 0x0409; // en-US
            }
        }

        /// <summary>
        /// 从Language.dll中读取指定ID的字符串
        /// </summary>
        /// <param name="dllPath">Language.dll文件路径</param>
        /// <param name="stringId">字符串ID</param>
        /// <param name="languageId">语言ID</param>
        /// <returns>读取到的字符串</returns>
        private string ReadStringFromLanguageDll(string dllPath, int stringId, ushort languageId)
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

                        // 尝试使用LoadString读取字符串
                        File.AppendAllText(_logFile, $"Calling LoadString for ID {stringId}...\n");
                        StringBuilder sb = new StringBuilder(256);
                        int result = NativeMethods.LoadString(dllHandle, stringId, sb, sb.Capacity);
                        if (result > 0)
                        {
                            string text = sb.ToString();
                            File.AppendAllText(_logFile, $"LoadString succeeded, string: '{text}'\n");
                            File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with LoadString ===\n\n");
                            return text;
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            File.AppendAllText(_logFile, $"LoadString failed, error code: {errorCode}\n");
                            File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode).Message}\n");
                        }

                        // 尝试使用FindResourceEx根据语言ID查找特定语言的资源
                        File.AppendAllText(_logFile, $"Trying FindResourceEx with language ID...\n");
                        IntPtr hResource = NativeMethods.FindResourceEx(dllHandle, new IntPtr(6), new IntPtr((stringId / 16) + 1), languageId);
                        if (hResource != IntPtr.Zero)
                        {
                            File.AppendAllText(_logFile, $"FindResourceEx succeeded, resource handle: {hResource}\n");
                            string text = ReadStringFromResource(dllHandle, hResource, stringId);
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
                        File.AppendAllText(_logFile, $"Trying LoadLibrary as fallback...\n");

                        // 尝试使用LoadLibrary作为备选方案
                        dllHandle = NativeMethods.LoadLibrary(dllPath);
                        if (dllHandle != IntPtr.Zero)
                        {
                            File.AppendAllText(_logFile, $"LoadLibrary succeeded, handle: {dllHandle}\n");

                            // 尝试使用LoadString读取字符串
                            File.AppendAllText(_logFile, $"Calling LoadString for ID {stringId}...\n");
                            StringBuilder sb = new StringBuilder(256);
                            int result = NativeMethods.LoadString(dllHandle, stringId, sb, sb.Capacity);
                            if (result > 0)
                            {
                                string text = sb.ToString();
                                File.AppendAllText(_logFile, $"LoadString succeeded, string: '{text}'\n");
                                File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with LoadLibrary and LoadString ===\n\n");
                                return text;
                            }
                            else
                            {
                                int errorCode2 = Marshal.GetLastWin32Error();
                                File.AppendAllText(_logFile, $"LoadString failed, error code: {errorCode2}\n");
                                File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode2).Message}\n");
                            }
                        }
                        else
                        {
                            int errorCode2 = Marshal.GetLastWin32Error();
                            File.AppendAllText(_logFile, $"LoadLibrary also failed, error code: {errorCode2}\n");
                            File.AppendAllText(_logFile, $"Error message: {new System.ComponentModel.Win32Exception(errorCode2).Message}\n");
                        }
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

                // 尝试使用替代方法：使用System.Reflection.Assembly加载DLL并读取资源
                File.AppendAllText(_logFile, $"Trying to read with Assembly.LoadFrom...\n");
                try
                {
                    // 加载DLL作为程序集
                    var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                    File.AppendAllText(_logFile, $"Assembly loaded successfully: {assembly.FullName}\n");

                    // 尝试读取资源
                    var resourceNames = assembly.GetManifestResourceNames();
                    File.AppendAllText(_logFile, $"Found {resourceNames.Length} manifest resources\n");

                    foreach (var resourceName in resourceNames)
                    {
                        File.AppendAllText(_logFile, $"Manifest resource found: {resourceName}\n");
                    }

                    // 尝试使用ResourceManager读取字符串资源
                    var resourceManager = new System.Resources.ResourceManager("Language", assembly);
                    File.AppendAllText(_logFile, $"ResourceManager created successfully\n");

                    // 尝试获取字符串
                    string text = resourceManager.GetString(stringId.ToString());
                    if (!string.IsNullOrEmpty(text))
                    {
                        File.AppendAllText(_logFile, $"Found string for ID {stringId}: '{text}'\n");
                        File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with Assembly ===\n\n");
                        return text;
                    }
                    else
                    {
                        File.AppendAllText(_logFile, $"No string found for ID {stringId}\n");
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_logFile, $"Exception with Assembly.LoadFrom: {ex.Message}\n");
                    File.AppendAllText(_logFile, $"Stack trace: {ex.StackTrace}\n");
                }

                // 如果所有方法都失败，使用硬编码的字符串映射
                File.AppendAllText(_logFile, $"All methods failed, using hardcoded string mapping\n");
                string hardcodedString = GetHardcodedString(stringId, languageId);
                File.AppendAllText(_logFile, $"Returning hardcoded string: '{hardcodedString}'\n");
                File.AppendAllText(_logFile, $"=== ReadStringFromLanguageDll completed with hardcoded string ===\n\n");
                return hardcodedString;
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
        /// <returns>读取到的字符串</returns>
        private string ReadStringFromResource(IntPtr dllHandle, IntPtr hResource, int stringId)
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
                    if (length == 0)
                    {
                        File.AppendAllText(_logFile, $"Found empty string, breaking loop\n");
                        break;
                    }
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
        /// 获取硬编码的字符串
        /// </summary>
        /// <param name="stringId">字符串ID</param>
        /// <param name="languageId">语言ID</param>
        /// <returns>硬编码的字符串</returns>
        private string GetHardcodedString(int stringId, ushort languageId)
        {
            // 根据语言ID和字符串ID返回硬编码的字符串
            // 由于我们无法从Language.dll中读取字符串，暂时使用硬编码的测试字符串
            bool isChinese = (languageId == 0x0404); // zh-TW

            switch (stringId)
            {
                case 250:
                    return isChinese ? "测试字符串 250 (中文)" : "Test string 250 (English)";
                case 251:
                    return isChinese ? "测试字符串 251 (中文)" : "Test string 251 (English)";
                case 252:
                    return isChinese ? "测试字符串 252 (中文)" : "Test string 252 (English)";
                case 253:
                    return isChinese ? "测试字符串 253 (中文)" : "Test string 253 (English)";
                case 254:
                    return isChinese ? "测试字符串 254 (中文)" : "Test string 254 (English)";
                default:
                    return isChinese ? $"测试字符串 {stringId} (中文)" : $"Test string {stringId} (English)";
            }
        }

        /// <summary>
        /// 原生方法定义
        /// </summary>
        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int LoadString(IntPtr hInstance, int uID, StringBuilder lpBuffer, int nBufferMax);

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
                System.Windows.Media.Imaging.BitmapImage backgroundImage = mixFile.GetImageByHash(fileNameHash);

                if (backgroundImage == null)
                {

                    return;
                }

                // 更新 Grid 的 Background 属性
                // 直接从 Window 的 Content 属性获取 Grid 控件
                if (Content is not Grid grid)
                {
                    // 如果直接获取失败，尝试使用 FindVisualChild 方法
                    grid = FindVisualChild<Grid>(this);
                    if (grid == null)
                    {

                        return;
                    }
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
                byte[] shpData = mixFile.GetShpByHash(fileNameHash);

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
                byte[] palData = null;
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

        private void InitializeLanguage()
        {
            // 获取系统默认语言
            CultureInfo systemLanguage = CultureInfo.CurrentUICulture;

            // 根据系统语言设置UI语言
            if (systemLanguage.Name.StartsWith("zh-CN"))
            {
                SetLanguage("zh-CN");
            }
            else if (systemLanguage.Name.StartsWith("zh-TW"))
            {
                SetLanguage("zh-TW");
            }
            else if (systemLanguage.Name.StartsWith("zh"))
            {
                SetLanguage("zh-TW");
            }
            else
            {
                SetLanguage("en-US");
            }
        }

        public void SetLanguage(string cultureName)
        {
            CultureInfo culture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            Strings.Culture = culture;

            // 保存当前选择的语言
            _currentLanguage = cultureName;

            // 更新UI文本
            UpdateUIText();
        }

        private void UpdateUIText()
        {
            // 更新窗口标题
            Title = Strings.WindowTitle;

            // 手动更新按钮文本
            UpdateButtonTexts();
        }

        private void UpdateButtonTexts()
        {
            // 直接通过Name找到取消按钮
            if (CancelButton != null)
            {
                _ = CancelButton.ApplyTemplate();

                // 通过ControlTemplate.FindName方法找到ButtonTextBlock
                ControlTemplate template = CancelButton.Template;
                if (template != null)
                {
                    if (template.FindName("ButtonTextBlock", CancelButton) is TextBlock textBlock)
                    {
                        // 清除Inlines，因为我们要直接设置Text
                        textBlock.Inlines.Clear();
                        // 更新文本
                        textBlock.Text = Strings.CancelButton;
                    }
                }

                // 同时使用视觉树查找作为备用方法
                TextBlock visualTextBlock = FindVisualChild<TextBlock>(CancelButton);
                if (visualTextBlock != null)
                {
                    // 清除Inlines，因为我们要直接设置Text
                    visualTextBlock.Inlines.Clear();
                    // 更新文本
                    visualTextBlock.Text = Strings.CancelButton;
                }
            }
        }

        /// <summary>
        /// 从 Setup.mix 文件加载音频并保存到临时文件
        /// </summary>
        /// <param name="hashValue">音频文件的哈希值</param>
        /// <returns>临时文件路径</returns>
        private static string LoadAudioFromMix(string hashValue)
        {
            try
            {
                // 加载 Setup.mix 文件
                MixFile mixFile = new(SetupMixPath);

                // 尝试获取指定哈希值和类型的音频
                byte[] audioData = mixFile.GetAudioByHash(hashValue);

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
        private void PlayAudio(System.Windows.Media.MediaPlayer player, string audioFile)
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
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is not null and T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
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
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is not null and T)
                {
                    yield return (T)child;
                }
                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }



        private void ChineseSimplifiedButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("zh-CN");
            // 重新加载并显示语言字符串
            ReloadLanguageStrings();
            // 开始播放 SHP 动画
            StartShpAnimation();
        }

        private void ChineseTraditionalButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("zh-TW");
            // 重新加载并显示语言字符串
            ReloadLanguageStrings();
            // 开始播放 SHP 动画
            StartShpAnimation();
        }

        private void EnglishButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("en-US");
            // 重新加载并显示语言字符串
            ReloadLanguageStrings();
            // 开始播放 SHP 动画
            StartShpAnimation();
        }

        /// <summary>
        /// 重新加载并显示语言字符串
        /// </summary>
        private void ReloadLanguageStrings()
        {
            // 清空现有的文本
            LanguageTextStackPanel.Children.Clear();
            // 重新加载语言字符串
            LoadAndDisplayLanguageStrings();
        }

        /// <summary>
        /// 开始播放 SHP 动画
        /// </summary>
        private void StartShpAnimation()
        {
            if (_shpAnimationPlayer != null)
            {
                // 每次点击按钮时，重置动画到第一帧并重新开始播放
                File.AppendAllText(_logFile, "Starting SHP animation playback on language button click\n");

                // 重置动画到第一帧
                _shpAnimationPlayer.Reset();
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
        private void ShpAnimationPlayer_AnimationCompleted(object sender, EventArgs e)
        {
            File.AppendAllText(_logFile, "SHP animation completed, switching to Page 2\n");
            SwitchToPage2();
        }

        /// <summary>
        /// 切换到第二页
        /// </summary>
        private void SwitchToPage2()
        {
            // 隐藏第一页
            Page1.Visibility = Visibility.Collapsed;
            // 显示第二页
            Page2.Visibility = Visibility.Visible;

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

            // 为第二页加载相同的背景图片
            LoadBackgroundImageForPage2();

            // 加载并播放第二页的动画
            LoadAndPlayPage2Animation();

            // 更新第二页的UI文本，使用当前选择的语言
            UpdatePage2UIText();
        }

        /// <summary>
        /// 加载并播放第二页的动画
        /// </summary>
        private void LoadAndPlayPage2Animation()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading and playing Page2 animation with hash: D6D75E64\n");

                // 检查Page2AnimationImage是否存在
                if (Page2AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "Page2AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[] shpData = mixFile.GetShpByHash("D6D75E64");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for Page2 animation\n");
                    return;
                }

                // 获取PAL文件数据
                string palHash = "397C46E0";
                byte[] palData = mixFile.GetPalByHash(palHash);
                if (palData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load PAL file for Page2 animation\n");
                    return;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(shpData, palData);

                // 创建动画播放器
                _page2ShpAnimationPlayer = new ShpAnimationPlayer(shpFile, Page2AnimationImage);

                // 添加动画播放完成事件处理程序
                _page2ShpAnimationPlayer.AnimationCompleted += Page2Animation_Completed;

                // 开始播放动画
                _page2ShpAnimationPlayer.Play();
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
        /// 播放第二页的音效
        /// </summary>
        private void PlayPage2Sounds()
        {
            try
            {
                string soundFile1 = LoadAudioFromMix("B1C914DD");
                PlayAudio(_soundPlayer, soundFile1);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error playing Page2 sounds: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 为第二页加载背景图片
        /// </summary>
        private void LoadBackgroundImageForPage2()
        {
            try
            {
                // 加载 Setup.mix 文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 尝试获取指定哈希值和类型的图片
                System.Windows.Media.Imaging.BitmapImage backgroundImage = mixFile.GetImageByHash("B1D51F00");

                if (backgroundImage != null)
                {
                    // 更新第二页 Grid 的 Background 属性
                    Page2.Background = new ImageBrush(backgroundImage) { Stretch = Stretch.UniformToFill };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background image for Page2: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新第二页的UI文本
        /// </summary>
        private void UpdatePage2UIText()
        {
            // 更新上一步按钮文本
            if (PreviousButton != null)
            {
                _ = PreviousButton.ApplyTemplate();
                ControlTemplate template = PreviousButton.Template;
                if (template != null)
                {
                    if (template.FindName("PreviousButtonTextBlock", PreviousButton) is TextBlock textBlock)
                    {
                        textBlock.Text = Strings.PreviousButton;
                    }
                }
            }

            // 更新下一步按钮文本
            if (NextButton != null)
            {
                _ = NextButton.ApplyTemplate();
                ControlTemplate template = NextButton.Template;
                if (template != null)
                {
                    if (template.FindName("NextButtonTextBlock", NextButton) is TextBlock textBlock)
                    {
                        textBlock.Text = Strings.NextButton;
                    }
                }
            }

            // 更新取消按钮文本
            if (Page2CancelButton != null)
            {
                _ = Page2CancelButton.ApplyTemplate();
                ControlTemplate template = Page2CancelButton.Template;
                if (template != null)
                {
                    if (template.FindName("CancelButtonTextBlock", Page2CancelButton) is TextBlock textBlock)
                    {
                        textBlock.Text = Strings.CancelButton;
                    }
                }
            }

            // 更新许可证内容文本
            if (LicenseTextBlock != null)
            {
                LicenseTextBlock.Text = Strings.LicenseContent;
            }

            // 更新同意条款文本
            if (IAgreeToTheseTermsTextBlock != null)
            {
                IAgreeToTheseTermsTextBlock.Text = Strings.IAgreeToTheseTerms;
            }
        }

        /// <summary>
        /// 上一步按钮点击事件
        /// </summary>
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            // 隐藏第二页
            Page2.Visibility = Visibility.Collapsed;
            // 显示第一页
            Page1.Visibility = Visibility.Visible;

            // 显示第一帧，不播放动画
            if (_shpAnimationPlayer != null)
            {
                // 停止动画（如果正在播放）
                _shpAnimationPlayer.Stop();
                // 重置到第一帧
                _shpAnimationPlayer.Reset();
            }
        }

        /// <summary>
        /// 下一步按钮点击事件
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

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

        /// <summary>
        /// 显示指定动画的第一帧
        /// </summary>
        /// <param name="animationHash">动画的哈希值</param>
        private void ShowAnimationFirstFrame(string animationHash)
        {
            try
            {
                File.AppendAllText(_logFile, $"Loading first frame of animation with hash: {animationHash}\n");

                // 检查Page2AnimationImage是否存在
                if (Page2AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "Page2AnimationImage control is null\n");
                    return;
                }

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取SHP文件数据
                byte[] shpData = mixFile.GetShpByHash(animationHash);
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, $"Failed to load SHP file for animation {animationHash}\n");
                    return;
                }

                // 获取PAL文件数据（使用与第二页相同的调色板）
                string palHash = "397C46E0";
                byte[] palData = mixFile.GetPalByHash(palHash);
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
                    Page2AnimationImage.Source = frames[0];
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
        private List<System.Windows.Media.Imaging.BitmapSource> _agreeButtonAnimationFrames;

        /// <summary>
        /// 第二页动画播放完成事件处理程序
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void Page2Animation_Completed(object sender, EventArgs e)
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

                // 加载并显示language.dll中ID 255的内容
                LoadAndDisplayLanguageStringId255();
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, "Error in Page2Animation_Completed: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// 加载并显示language.dll中ID 255的内容
        /// </summary>
        private void LoadAndDisplayLanguageStringId255()
        {
            try
            {
                File.AppendAllText(_logFile, "Starting to load language string ID 255 from Language.dll\n");

                // Language.dll文件路径
                string languageDllPath = "Assets/RA1/Setup/Language.dll";

                // 检查文件是否存在
                if (!File.Exists(languageDllPath))
                {
                    File.AppendAllText(_logFile, "Language.dll file not found\n");
                    return;
                }

                File.AppendAllText(_logFile, "Language.dll file found, loading string ID 255\n");

                // 确定要使用的语言
                ushort languageId = GetLanguageIdForCurrentLanguage();
                File.AppendAllText(_logFile, $"Using language ID: {languageId}\n");

                // 读取字符串ID 255
                int stringId = 255;
                string text = ReadStringFromLanguageDll(languageDllPath, stringId, languageId);
                if (!string.IsNullOrEmpty(text))
                {
                    // 清除现有内容
                    if (Page2LanguageTextStackPanel != null)
                    {
                        Page2LanguageTextStackPanel.Children.Clear();

                        // 创建TextBlock并添加到StackPanel
                        TextBlock textBlock = new TextBlock
                        {
                            Text = text,
                            Foreground = Brushes.Yellow,
                            FontSize = 10,
                            TextAlignment = TextAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                        };
                        Page2LanguageTextStackPanel.Children.Add(textBlock);
                        File.AppendAllText(_logFile, $"Added string ID {stringId}: {text}\n");
                    }
                    else
                    {
                        File.AppendAllText(_logFile, "Page2LanguageTextStackPanel is null\n");
                    }
                }
                else
                {
                    File.AppendAllText(_logFile, $"Failed to read string ID {stringId}\n");
                }

                File.AppendAllText(_logFile, "Language string ID 255 loaded and displayed\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading language string ID 255: {ex.Message}\n");
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
                byte[] shpData = mixFile.GetShpByHash("134B6332");
                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load SHP file for agree button animation\n");
                    return;
                }

                // 获取PAL文件数据（使用指定的调色板 hash 297C46E0）
                string palHash = "297C46E0";
                byte[] palData = mixFile.GetPalByHash(palHash);
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