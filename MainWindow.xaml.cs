using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RA2Installer.Resources;

namespace RA2Installer
{
    /// <summary>
    /// 页面属性类，用于定义元素的页码属性
    /// </summary>
    public static class PageProperties
    {
        /// <summary>
        /// PageNumbers附加属性
        /// </summary>
        public static readonly DependencyProperty PageNumbersProperty = DependencyProperty.RegisterAttached(
            "PageNumbers",
            typeof(string),
            typeof(PageProperties),
            new PropertyMetadata(string.Empty)
        );

        /// <summary>
        /// 设置PageNumbers属性
        /// </summary>
        public static void SetPageNumbers(DependencyObject element, string value)
        {
            element.SetValue(PageNumbersProperty, value);
        }

        /// <summary>
        /// 获取PageNumbers属性
        /// </summary>
        public static string GetPageNumbers(DependencyObject element)
        {
            return (string)element.GetValue(PageNumbersProperty);
        }

        /// <summary>
        /// 获取元素的页码列表
        /// </summary>
        public static List<int> GetPageNumbersList(DependencyObject element)
        {
            string pageNumbersStr = GetPageNumbers(element);
            if (string.IsNullOrEmpty(pageNumbersStr))
                return new List<int>();

            return pageNumbersStr.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int page) ? page : -1)
                .Where(page => page > 0)
                .ToList();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [STAThread]
        public static void Main()
        {
            System.Windows.Application app = new System.Windows.Application();
            app.Run(new MainWindow());
        }

        // 常量：Setup.mix 文件路径
        private const string SetupMixPath = "Assets/RA1/Setup/Setup.mix";

        private MediaPlayer _backgroundMusicPlayer;
        private MediaPlayer _soundPlayer;
        private string? _buttonClickSoundFile;
        private string? _backgroundMusicFile;
        private ShpAnimationPlayer? _shpAnimationPlayer;
        private ShpAnimationPlayer? _radarShpAnimationPlayer;

        // 日志文件路径
        private string _logFile = string.Empty;

        // 当前页码
        private int _currentPage = 1;

        // 动画播放完成后的行为枚举
        private enum AnimationEndBehavior
        {
            Disappear,
            StayAtLastFrame,
            StayAtFirstFrame
        }

        // 单个动画配置
        private class AnimationConfig
        {
            public string ShpHash { get; set; } = string.Empty;
            public string PalHash { get; set; } = "397C46E0"; // 默认色卡
            public bool IsRadarAnimation { get; set; } = false; // 是否为雷达区动画
            public bool IsReverse { get; set; }
            public AnimationEndBehavior EndBehavior { get; set; }
            public string SoundHash { get; set; } = string.Empty;
            public int SoundDelay { get; set; } = 0;
        }

        // 页面动画配置
        private class PageAnimationConfig
        {
            public List<AnimationConfig> IntroAnimations { get; set; } = new List<AnimationConfig>();
            public List<AnimationConfig> ExitAnimations { get; set; } = new List<AnimationConfig>();
        }

        // 存储每一页的雷达文案IDs
        private readonly Dictionary<int, int[]> _pageRadarStringIds = new Dictionary<int, int[]> {
            { 1, new int[] { 250, 251, 252, 253, 254 } },
            { 2, new int[] { 255 } },
            { 3, new int[] { 256, 257, 258, 259, 260, 261 } }
        };

        // 存储每一页的底部文字ID和显示时长（毫秒）
        private readonly Dictionary<int, (int StringId, int DisplayDurationMs)> _pageBottomTextConfig = new Dictionary<int, (int, int)> {
            { 1, (144, 1000) } // 第一页：ID 144，显示1秒
        };

        // 存储每一页的动画配置
        private readonly Dictionary<int, PageAnimationConfig> _pageAnimationConfigs = new Dictionary<int, PageAnimationConfig> {
            {
                1,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "2012EC16",
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.StayAtFirstFrame
                        }
                    },
                    ExitAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "2012EC16",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.Disappear
                        }
                    }
                }
            },
            {
                2,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "D6D75E64",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.StayAtLastFrame,
                            SoundHash = "B1C914DD"
                        }
                    }
                }
            },
            {
                3,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "D6D75E64",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.StayAtLastFrame
                        }
                    },
                    ExitAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "D6D75E64",
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        }
                    }
                }
            },
            {
                4,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.StayAtLastFrame,
                            SoundHash = "C7918F4A",
                            SoundDelay = 1000
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.StayAtFirstFrame
                        }
                    },
                    ExitAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        }
                    }
                }
            },
            {
                5,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.StayAtLastFrame,
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.StayAtFirstFrame
                        }
                    },
                    ExitAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        }
                    }
                }
            },
            {
                6,
                new PageAnimationConfig {
                    IntroAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = false,
                            EndBehavior = AnimationEndBehavior.StayAtLastFrame,
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.StayAtFirstFrame
                        }
                    },
                    ExitAnimations = new List<AnimationConfig> {
                        new AnimationConfig {
                            ShpHash = "EA92E578",
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        },
                        new AnimationConfig {
                            ShpHash = "E9490E87",
                            PalHash = "297C46E0",
                            IsRadarAnimation = true,
                            IsReverse = true,
                            EndBehavior = AnimationEndBehavior.Disappear
                        }
                    }
                }
            }
        };

        // 用于控制底部文字显示时长的定时器
        private System.Timers.Timer? _bottomTextTimer;

        // 用于取消异步加载任务的令牌源
        private CancellationTokenSource? _loadStringsCancellationTokenSource;

        // 存储用户输入的序列号
        private string _serialNumber = string.Empty;

        // 存储安装路径
        private string _installationPath = string.Empty;

        // 存储开始菜单文件夹名称
        private string _startMenuFolderName = string.Empty;

        // 存储多选框状态
        private Dictionary<int, bool> _checkBoxStates = new Dictionary<int, bool> {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true }
        };

        // 多选框SHP文件和PAL文件数据
        private byte[]? _checkBoxShpData;
        private byte[]? _checkBoxPalData;

        public MainWindow()
        {
            try
            {
                // 创建日志文件
                _logFile = Path.Combine(Path.GetTempPath(), "ra2installer.log");
                File.WriteAllText(_logFile, "Starting MainWindow initialization\n");

                // 首先初始化组件，这样 Grid 控件就会被创建
                File.AppendAllText(_logFile, "Calling InitializeComponent()\n");
                InitializeComponent();
                File.AppendAllText(_logFile, "InitializeComponent() completed\n");

                string cursorPath = "Assets/3D_red_normalselect.cur";
                this.Cursor = new System.Windows.Input.Cursor(cursorPath);
                File.AppendAllText(_logFile, "Cursor set\n");

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

                // 检查 Page6 控件是否存在
                if (Page6ContentStackPanel != null)
                {
                    File.AppendAllText(_logFile, "Page6ContentStackPanel control is available\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "Page6ContentStackPanel control is null\n");
                }

                if (Page6Line1TextBlock != null)
                {
                    File.AppendAllText(_logFile, "Page6Line1TextBlock control is available\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "Page6Line1TextBlock control is null\n");
                }

                if (Page6Line2TextBlock != null)
                {
                    File.AppendAllText(_logFile, "Page6Line2TextBlock control is available\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "Page6Line2TextBlock control is null\n");
                }

                if (Page6InputTextBox != null)
                {
                    File.AppendAllText(_logFile, "Page6InputTextBox control is available\n");
                }
                else
                {
                    File.AppendAllText(_logFile, "Page6InputTextBox control is null\n");
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
                            Foreground = System.Windows.Media.Brushes.Yellow,
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

                // 使用默认的 PAL 文件
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
        /// 从指定路径的 Setup.mix 中加载指定哈希值和类型的 SHP 文件并准备雷达区动画
        /// 如果加载失败则输出日志但不退出程序
        /// </summary>
        /// <param name="setupMixPath">Setup.mix 文件的路径</param>
        /// <param name="fileNameHash">文件名哈希值</param>
        private void LoadRadarShpAnimationData(string setupMixPath, string fileNameHash)
        {
            try
            {
                // 简单的日志写入，避免格式化字符串可能的问题
                File.AppendAllText(_logFile, "Starting to load radar SHP animation\n");
                File.AppendAllText(_logFile, "Hash: " + fileNameHash + "\n");

                // 检查 RadarAnimationImage 是否存在
                if (RadarAnimationImage == null)
                {
                    File.AppendAllText(_logFile, "RadarAnimationImage control is null\n");
                    return;
                }
                else
                {
                    File.AppendAllText(_logFile, "RadarAnimationImage control is available\n");
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
                File.AppendAllText(_logFile, "Attempting to get radar SHP file\n");
                byte[]? shpData = mixFile.GetShpByHash(fileNameHash);

                if (shpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load radar SHP file\n");
                    return;
                }
                else
                {
                    File.AppendAllText(_logFile, "Successfully loaded radar SHP file\n");
                    File.AppendAllText(_logFile, "Size: " + shpData.Length + " bytes\n");
                }

                ShpFile shpFile;

                // 使用默认的 PAL 文件
                byte[]? palData = null;
                string userSpecifiedPalHash = "397C46E0";
                palData = mixFile.GetPalByHash(userSpecifiedPalHash);
                if (palData != null)
                {
                    File.AppendAllText(_logFile, "Successfully loaded user specified PAL file with hash: " + userSpecifiedPalHash + "\n");
                    File.AppendAllText(_logFile, "Size: " + palData.Length + " bytes\n");

                    // 使用找到的 PAL 文件解析 SHP 文件
                    File.AppendAllText(_logFile, "Parsing radar SHP file with PAL\n");
                    shpFile = new ShpFile(shpData, palData);
                }
                else
                {
                    File.AppendAllText(_logFile, "Failed to load user specified PAL file with hash: " + userSpecifiedPalHash + "\n");
                    throw new Exception($"Failed to load specified PAL file with hash: {userSpecifiedPalHash}");
                }

                File.AppendAllText(_logFile, "Radar SHP file parsed successfully\n");
                File.AppendAllText(_logFile, "Frame count: " + shpFile.FrameCount + "\n");
                File.AppendAllText(_logFile, "Width: " + shpFile.Width + "\n");
                File.AppendAllText(_logFile, "Height: " + shpFile.Height + "\n");

                // 创建雷达区动画播放器但不开始播放
                File.AppendAllText(_logFile, "Creating radar animation player\n");
                _radarShpAnimationPlayer = new ShpAnimationPlayer(shpFile, RadarAnimationImage);
            }
            catch (Exception ex)
            {
                // 记录错误但不退出程序
                File.AppendAllText(_logFile, "Error loading radar SHP animation\n");
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
                    
                    // 如果临时文件已存在，先删除它
                    if (File.Exists(tempFile))
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                            // 忽略删除失败的异常
                        }
                    }
                    
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

        /// <summary>
        /// 加载多选框图片数据
        /// </summary>
        private void LoadCheckBoxImages()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading checkbox images\n");

                // 加载Setup.mix文件
                MixFile mixFile = new MixFile(SetupMixPath);

                // 获取多选框SHP文件数据（hash DAB17B12）
                _checkBoxShpData = mixFile.GetShpByHash("DAB17B12");
                if (_checkBoxShpData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load checkbox SHP file\n");
                    return;
                }

                // 获取多选框PAL文件数据（色卡：317C46E0.pal）
                _checkBoxPalData = mixFile.GetPalByHash("317C46E0");
                if (_checkBoxPalData == null)
                {
                    File.AppendAllText(_logFile, "Failed to load checkbox PAL file\n");
                    return;
                }

                File.AppendAllText(_logFile, "Checkbox images loaded successfully\n");

                // 初始化所有多选框为未选中状态
                UpdateCheckBoxVisualState(CheckBox1, 1);
                UpdateCheckBoxVisualState(CheckBox2, 2);
                UpdateCheckBoxVisualState(CheckBox3, 3);
                UpdateCheckBoxVisualState(CheckBox4, 4);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading checkbox images: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 更新多选框视觉状态
        /// </summary>
        /// <param name="checkBoxImage">多选框Image控件</param>
        /// <param name="checkBoxId">多选框ID</param>
        private void UpdateCheckBoxVisualState(System.Windows.Controls.Image checkBoxImage, int checkBoxId)
        {
            try
            {
                if (_checkBoxShpData == null || _checkBoxPalData == null)
                {
                    File.AppendAllText(_logFile, "Checkbox SHP or PAL data not loaded\n");
                    return;
                }

                bool isChecked = _checkBoxStates.ContainsKey(checkBoxId) ? _checkBoxStates[checkBoxId] : false;
                int frameIndex = isChecked ? 0 : 1; // 选中使用第1帧（索引0），取消选中使用第2帧（索引1）

                // 获取对应帧的图片
                System.Windows.Media.Imaging.BitmapImage? frameImage = GetCheckBoxFrame(frameIndex);
                if (frameImage != null)
                {
                    checkBoxImage.Source = frameImage;
                    File.AppendAllText(_logFile, $"Updated checkbox {checkBoxId} visual state to {(isChecked ? "checked" : "unchecked")} based on state: {isChecked}\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error updating checkbox visual state: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 获取多选框指定帧的图片
        /// </summary>
        /// <param name="frameIndex">帧索引（0为选中状态，1为未选中状态）</param>
        /// <returns>BitmapImage对象或null</returns>
        private System.Windows.Media.Imaging.BitmapImage? GetCheckBoxFrame(int frameIndex)
        {
            try
            {
                if (_checkBoxShpData == null || _checkBoxPalData == null)
                {
                    File.AppendAllText(_logFile, "Checkbox SHP or PAL data not loaded\n");
                    return null;
                }

                // 解析SHP文件
                ShpFile shpFile = new ShpFile(_checkBoxShpData, _checkBoxPalData);

                // 检查帧索引是否有效
                if (frameIndex < 0 || frameIndex >= shpFile.FrameCount)
                {
                    File.AppendAllText(_logFile, $"Invalid frame index: {frameIndex}, total frames: {shpFile.FrameCount}\n");
                    return null;
                }

                // 获取指定帧的图片
                System.Windows.Media.Imaging.BitmapImage frameImage = shpFile.GetFrame(frameIndex);
                return frameImage;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error getting checkbox frame: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// 多选框点击事件处理程序
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void CheckBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Image checkBoxImage && checkBoxImage.Tag is string tag)
                {
                    int checkBoxId = int.Parse(tag);
                    
                    // 切换多选框状态
                    if (_checkBoxStates.ContainsKey(checkBoxId))
                    {
                        _checkBoxStates[checkBoxId] = !_checkBoxStates[checkBoxId];
                        File.AppendAllText(_logFile, $"Toggled checkbox {checkBoxId} to state: {_checkBoxStates[checkBoxId]}\n");
                    }
                    else
                    {
                        _checkBoxStates[checkBoxId] = true;
                        File.AppendAllText(_logFile, $"Set checkbox {checkBoxId} to state: true\n");
                    }

                    // 更新多选框视觉状态
                    UpdateCheckBoxVisualState(checkBoxImage, checkBoxId);

                    // 播放按钮点击音效
                    PlayButtonClickSound();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error handling checkbox click: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 加载多选框标题文本
        /// </summary>
        private void LoadCheckBoxesTitle()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading checkboxes title text from Language.dll ID 197\n");

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

                // 读取字符串（ID 197）
                string? text = ReadStringFromLanguageDll(languageDllPath, 197, languageId);
                if (!string.IsNullOrEmpty(text))
                {
                    // 显示文本
                    if (CheckBoxesTitleTextBlock != null)
                    {
                        CheckBoxesTitleTextBlock.Text = text;
                        File.AppendAllText(_logFile, "Checkboxes title loaded and displayed from ID 197\n");
                    }
                }
                else
                {
                    File.AppendAllText(_logFile, "Failed to read string ID 197\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading checkboxes title: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 通用函数：读取Language.dll中的字符串并替换其中的%s占位符
        /// </summary>
        /// <param name="stringId">字符串ID</param>
        /// <param name="replacementId">替换字符串的ID</param>
        /// <returns>替换后的字符串或null</returns>
        private string? GetStringWithReplacement(int stringId, int replacementId)
        {
            try
            {
                // Language.dll文件路径
                string languageDllPath = "Assets/RA1/Setup/Language.dll";

                // 检查文件是否存在
                if (!File.Exists(languageDllPath))
                {
                    File.AppendAllText(_logFile, "Language.dll file not found\n");
                    return null;
                }

                // 确定要使用的语言
                ushort languageId = GetLanguageIdForCurrentLanguage();

                // 读取原始字符串
                string? originalText = ReadStringFromLanguageDll(languageDllPath, stringId, languageId);
                if (string.IsNullOrEmpty(originalText))
                {
                    File.AppendAllText(_logFile, $"Failed to read string ID {stringId}\n");
                    return null;
                }

                // 读取替换字符串
                string? replacementText = ReadStringFromLanguageDll(languageDllPath, replacementId, languageId);
                if (string.IsNullOrEmpty(replacementText))
                {
                    File.AppendAllText(_logFile, $"Failed to read replacement string ID {replacementId}\n");
                    return originalText; // 如果替换字符串读取失败，返回原始字符串
                }

                // 替换%s占位符
                string result = originalText.Replace("%s", replacementText);
                File.AppendAllText(_logFile, $"String ID {stringId} with replacement ID {replacementId}: '{originalText}' -> '{result}'\n");
                return result;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error getting string with replacement: {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// 加载多选框选项文本
        /// </summary>
        private void LoadCheckBoxesItems()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading checkboxes items text from Language.dll\n");

                // 加载每个选项文本，使用ID 18作为替换文本
                string? item1Text = GetStringWithReplacement(199, 18);
                string? item2Text = GetStringWithReplacement(200, 18);
                string? item3Text = GetStringWithReplacement(203, 18);
                string? item4Text = GetStringWithReplacement(204, 18);

                // 显示文本
                if (CheckBoxText1 != null && !string.IsNullOrEmpty(item1Text))
                {
                    CheckBoxText1.Text = item1Text;
                    File.AppendAllText(_logFile, "Checkbox item 1 loaded and displayed from ID 199\n");
                }

                if (CheckBoxText2 != null && !string.IsNullOrEmpty(item2Text))
                {
                    CheckBoxText2.Text = item2Text;
                    File.AppendAllText(_logFile, "Checkbox item 2 loaded and displayed from ID 200\n");
                }

                if (CheckBoxText3 != null && !string.IsNullOrEmpty(item3Text))
                {
                    CheckBoxText3.Text = item3Text;
                    File.AppendAllText(_logFile, "Checkbox item 3 loaded and displayed from ID 203\n");
                }

                if (CheckBoxText4 != null && !string.IsNullOrEmpty(item4Text))
                {
                    CheckBoxText4.Text = item4Text;
                    File.AppendAllText(_logFile, "Checkbox item 4 loaded and displayed from ID 204\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading checkboxes items: {ex.Message}\n");
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (BottomTextBlock != null)
                    {
                        BottomTextBlock.Text = "";
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
        /// 递归更新元素及其子元素的可见性
        /// </summary>
        /// <param name="element">要更新的元素</param>
        /// <param name="pageNumber">当前页码</param>
        /// <param name="showMatching">是否显示匹配的元素（true）或隐藏不匹配的元素（false）</param>
        private void UpdateElementsVisibility(DependencyObject element, int pageNumber, bool showMatching)
        {
            if (element == null)
                return;

            // 检查元素是否有PageNumbers属性
            string pageNumbersStr = PageProperties.GetPageNumbers(element);
            if (!string.IsNullOrEmpty(pageNumbersStr))
            {
                List<int> pageNumbers = PageProperties.GetPageNumbersList(element);
                bool shouldBeVisible = pageNumbers.Contains(pageNumber);
                bool shouldUpdateVisibility = showMatching ? shouldBeVisible : !shouldBeVisible;
                Visibility visibility = showMatching ? Visibility.Visible : Visibility.Collapsed;
                string visibilityStr = showMatching ? "Visible" : "Collapsed";

                // 只处理具有Visibility属性的元素
                if (element is UIElement uiElement && shouldUpdateVisibility)
                {
                    uiElement.Visibility = visibility;
                    File.AppendAllText(_logFile, $"Element {element.GetType().Name} (Name: {(element is FrameworkElement fe ? fe.Name : "N/A")}) visibility set to {visibilityStr} for Page {pageNumber}\n");
                }
            }

            // 递归处理子元素
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(element, i);
                UpdateElementsVisibility(child, pageNumber, showMatching);
            }
        }

        /// <summary>
        /// 隐藏所有不匹配的元素
        /// </summary>
        /// <param name="pageNumber">当前页码</param>
        private void HideNonMatchingElements(int pageNumber)
        {
            // 遍历所有子元素并更新可见性
            UpdateElementsVisibility(LayoutRoot, pageNumber, false);
        }

        /// <summary>
        /// 显示所有匹配的元素
        /// </summary>
        /// <param name="pageNumber">当前页码</param>
        private void ShowMatchingElements(int pageNumber)
        {
            // 遍历所有子元素并更新可见性
            UpdateElementsVisibility(LayoutRoot, pageNumber, true);
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

            // 清空雷达文本
            RadarTextStackPanel.Children.Clear();

            // 调整主区域动画控件位置
            if (AnimationImage != null)
            {
                if (pageNumber == 1)
                {
                    AnimationImage.Margin = new Thickness(0, 75, 0, 0);
                    AnimationImage.Width = 470;
                    File.AppendAllText(_logFile, "AnimationImage margin set to (0,75,0,0) and width set to 470 for Page 1\n");
                }
                else
                {
                    AnimationImage.Margin = new Thickness(0, 0, 0, 0);
                    AnimationImage.Width = 472;
                    File.AppendAllText(_logFile, $"AnimationImage margin set to (0,0,0,0) and width set to 472 for Page {pageNumber}\n");
                }
            }

            // 隐藏不匹配的元素
            HideNonMatchingElements(pageNumber);

            if (pageNumber == 1)
            {
                // 加载并播放第一页的动画
                PlayPageAnimations(pageNumber, true, () =>
                {
                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                    LoadAndDisplayRadarStrings(_currentPage);
                });
            }
            else if (pageNumber == 2)
            {
                // 加载并播放第二页的动画，动画完成后显示许可证内容
                PlayPageAnimations(pageNumber, true, () =>
                {
                    // 加载并显示同意按钮动画的第一帧
                    LoadAgreeButtonAnimation();

                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                    // 加载第二页的雷达文案
                    LoadAndDisplayRadarStrings(_currentPage);
                });
            }
            else if (pageNumber == 3)
            {

                // 加载并播放第三页的动画
                PlayPageAnimations(_currentPage, true, () =>
                {
                    // 读取许可证内容
                    LoadLicenseContentFromLanguageDll();

                    // 加载同意按钮动画的第一帧
                    LoadAgreeButtonAnimation();
                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                    LoadAndDisplayRadarStrings(_currentPage);
                });

                // 默认选中第一个输入框
                InputField1.Focus();
            }
            else if (pageNumber == 4)
            {
                // 加载并播放第四页的动画
                PlayPageAnimations(pageNumber, true, () =>
                {

                    // 加载并初始化多选框图片
                    LoadCheckBoxImages();

                    // 加载多选框标题文本
                    LoadCheckBoxesTitle();

                    // 加载多选框选项文本
                    LoadCheckBoxesItems();

                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                });
            }
            else if (pageNumber == 5)
            {
                // 加载并播放第五页的动画
                PlayPageAnimations(pageNumber, true, () =>
                {
                    // 加载第五页的文本内容
                    LoadPage5Content();

                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                });
            }
            else if (pageNumber == 6)
            {
                // 加载并播放第六页的动画
                PlayPageAnimations(pageNumber, true, () =>
                {
                    // 加载第六页的文本内容
                    LoadPage6Content();

                    // 显示匹配的元素
                    ShowMatchingElements(pageNumber);
                });
            }
        }

        /// <summary>
        /// 加载第五页的文本内容
        /// </summary>
        private void LoadPage5Content()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading page 5 content from Language.dll\n");

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

                // 加载第一行文本（ID 124，使用ID 18替换）
                string? line1Text = GetStringWithReplacement(124, 18);
                if (!string.IsNullOrEmpty(line1Text) && Page5Line1TextBlock != null)
                {
                    Page5Line1TextBlock.Text = line1Text;
                    File.AppendAllText(_logFile, $"Page 5 line 1 loaded from ID 124 with replacement ID 18: '{line1Text}'\n");
                }

                // 加载第二行文本框内容（ID 5）
                string? line2Text = ReadStringFromLanguageDll(languageDllPath, 5, languageId);
                if (!string.IsNullOrEmpty(line2Text) && Page5PathTextBox != null)
                {
                    Page5PathTextBox.Text = line2Text;
                    _installationPath = line2Text;
                    File.AppendAllText(_logFile, $"Page 5 line 2 loaded from ID 5: '{line2Text}'\n");
                    File.AppendAllText(_logFile, $"Installation path saved: {_installationPath}\n");
                    // 更新第四行的可用空间
                    UpdatePage5FreeSpace(line2Text);
                }

                // 加载第三行左对齐文本（ID 120）
                string? line3LeftText = ReadStringFromLanguageDll(languageDllPath, 120, languageId);
                if (!string.IsNullOrEmpty(line3LeftText) && Page5Line3LeftTextBlock != null)
                {
                    Page5Line3LeftTextBlock.Text = line3LeftText;
                    File.AppendAllText(_logFile, $"Page 5 line 3 left loaded from ID 120: '{line3LeftText}'\n");
                }

                // 加载第四行左对齐文本（ID 119）
                string? line4LeftText = ReadStringFromLanguageDll(languageDllPath, 119, languageId);
                if (!string.IsNullOrEmpty(line4LeftText) && Page5Line4LeftTextBlock != null)
                {
                    Page5Line4LeftTextBlock.Text = line4LeftText;
                    File.AppendAllText(_logFile, $"Page 5 line 4 left loaded from ID 119: '{line4LeftText}'\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading page 5 content: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 加载第六页的文本内容
        /// </summary>
        private void LoadPage6Content()
        {
            try
            {
                File.AppendAllText(_logFile, "Loading page 6 content from Language.dll\n");

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

                // 加载第一行文本（ID 123，使用ID 18替换）
                string? line1Text = GetStringWithReplacement(123, 18);
                if (!string.IsNullOrEmpty(line1Text) && Page6Line1TextBlock != null)
                {
                    Page6Line1TextBlock.Text = line1Text;
                    File.AppendAllText(_logFile, $"Page 6 line 1 loaded from ID 123 with replacement ID 18: '{line1Text}'\n");
                }

                // 加载第二行文本（ID 121）
                string? line2Text = ReadStringFromLanguageDll(languageDllPath, 121, languageId);
                if (!string.IsNullOrEmpty(line2Text) && Page6Line2TextBlock != null)
                {
                    Page6Line2TextBlock.Text = line2Text;
                    File.AppendAllText(_logFile, $"Page 6 line 2 loaded from ID 121: '{line2Text}'\n");
                }

                // 加载第三行文本框内容（ID 7）
                string? line3Text = ReadStringFromLanguageDll(languageDllPath, 7, languageId);
                if (!string.IsNullOrEmpty(line3Text) && Page6InputTextBox != null)
                {
                    Page6InputTextBox.Text = line3Text;
                    File.AppendAllText(_logFile, $"Page 6 input box loaded from ID 7: '{line3Text}'\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error loading page 6 content: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 更新第五页第四行的可用空间
        /// </summary>
        /// <param name="path">目录路径</param>
        private void UpdatePage5FreeSpace(string path)
        {
            try
            {
                // 提取盘符
                string driveLetter = string.Empty;
                if (!string.IsNullOrEmpty(path) && path.Length >= 2 && path[1] == ':')
                {
                    driveLetter = path.Substring(0, 2);
                    File.AppendAllText(_logFile, $"Extracted drive letter: {driveLetter}\n");
                }

                if (!string.IsNullOrEmpty(driveLetter))
                {
                    // 获取磁盘信息
                    DriveInfo driveInfo = new DriveInfo(driveLetter);
                    if (driveInfo.IsReady)
                    {
                        // 计算可用空间（单位：K）
                        long freeSpaceK = driveInfo.AvailableFreeSpace / 1024;
                        string freeSpaceText = $"{freeSpaceK} K";
                        
                        if (Page5Line4RightTextBlock != null)
                        {
                            Page5Line4RightTextBlock.Text = freeSpaceText;
                            File.AppendAllText(_logFile, $"Updated page 5 free space: {freeSpaceText}\n");
                        }
                    }
                    else
                    {
                        File.AppendAllText(_logFile, $"Drive {driveLetter} is not ready\n");
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error updating page 5 free space: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 浏览按钮点击事件处理程序
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 播放按钮点击音效
                PlayButtonClickSound();

                // 创建文件夹选择对话框
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "选择安装目录";
                
                // 如果当前文本框有内容，设置为初始目录
                if (!string.IsNullOrEmpty(Page5PathTextBox?.Text))
                {
                    string currentPath = Page5PathTextBox.Text;
                    if (Directory.Exists(currentPath))
                    {
                        dialog.SelectedPath = currentPath;
                    }
                }

                // 显示对话框
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    if (Page5PathTextBox != null)
                    {
                        Page5PathTextBox.Text = selectedPath;
                        _installationPath = selectedPath;
                        File.AppendAllText(_logFile, $"Selected folder: {selectedPath}\n");
                        File.AppendAllText(_logFile, $"Installation path saved: {_installationPath}\n");
                        // 更新第四行的可用空间
                        UpdatePage5FreeSpace(selectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error in BrowseButton_Click: {ex.Message}\n");
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
            if (sender is System.Windows.Controls.TextBox textBox)
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

            // 浏览按钮只在第5页显示
            if (BrowseButton != null)
            {
                BrowseButton.Visibility = _currentPage == 5 ? Visibility.Visible : Visibility.Collapsed;
                File.AppendAllText(_logFile, $"BrowseButton visibility set to {(BrowseButton.Visibility == Visibility.Visible ? "Visible" : "Collapsed")} for Page {_currentPage}\n");
            }
        }

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

                // 读取字符串（ID 210），使用ID 18作为替换文本
                string? text = GetStringWithReplacement(210, 18);
                if (!string.IsNullOrEmpty(text))
                {
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
        /// 上一步按钮点击事件
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();

            // 切换到上一页，播放退出动画
            if (_currentPage > 1)
            {
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(_currentPage - 1));
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
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(2));
            }
            else if (_currentPage == 2)
            {
                // 第2页：播放退出动画，然后跳转到第三页
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(3));
            }
            else if (_currentPage == 3)
            {
                // 第3页：获取并存储序列号
                _serialNumber = InputField1.Text + InputField2.Text + InputField3.Text + InputField4.Text;
                
                // 第3页：播放退出动画，然后跳转到第四页
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(4));
            }
            else if (_currentPage == 4)
            {
                // 第4页：播放退出动画，然后跳转到第五页
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(5));
            }
            else if (_currentPage == 5)
            {
                // 第5页：播放退出动画，然后跳转到第六页
                PlayPageAnimations(_currentPage, false, () => SwitchToPage(6));
            }
            else if (_currentPage == 6)
            {
                // 第6页：保存开始菜单文件夹名称
                if (Page6InputTextBox != null)
                {
                    _startMenuFolderName = Page6InputTextBox.Text;
                    File.AppendAllText(_logFile, $"Start menu folder name saved: {_startMenuFolderName}\n");
                }
                
                // 第6页：播放退出动画，然后可以跳转到下一页（如果有）
                // 这里暂时只做动画播放，不跳转
                PlayPageAnimations(_currentPage, false, null);
            }
        }

        /// <summary>
        /// 播放页面的动画
        /// </summary>
        /// <param name="pageNumber">页面编号</param>
        /// <param name="isIntro">是否是开场动画（true表示开场动画，false表示退出动画）</param>
        /// <param name="callback">动画播放完成后的回调函数</param>
        private void PlayPageAnimations(int pageNumber, bool isIntro, Action? callback = null)
        {
            try
            {
                File.AppendAllText(_logFile, $"Starting to play {((isIntro) ? "intro" : "exit")} animations for page {pageNumber}\n");

                // 检查AnimationImage是否存在
                if (AnimationImage == null)
                {
                    File.AppendAllText(_logFile, "AnimationImage control is null\n");
                    callback?.Invoke();
                    return;
                }

                // 获取页面的动画配置
                if (!_pageAnimationConfigs.TryGetValue(pageNumber, out var pageConfig))
                {
                    File.AppendAllText(_logFile, $"No animation config defined for page {pageNumber}\n");
                    callback?.Invoke();
                    return;
                }

                // 选择要播放的动画列表
                var animations = isIntro ? pageConfig.IntroAnimations : pageConfig.ExitAnimations;
                if (animations == null || animations.Count == 0)
                {
                    File.AppendAllText(_logFile, $"No {((isIntro) ? "intro" : "exit")} animations defined for page {pageNumber}\n");
                    callback?.Invoke();
                    return;
                }

                // 开始顺序播放动画
                PlayAnimations(animations, 0, callback);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error playing page animations: {ex.Message}\n");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 并行播放多个动画
        /// </summary>
        /// <param name="animations">动画配置列表</param>
        /// <param name="index">当前要播放的动画索引</param>
        /// <param name="callback">所有动画播放完成后的回调函数</param>
        private async void PlayAnimations(List<AnimationConfig> animations, int index, Action? callback = null)
        {
            try
            {
                // 检查是否所有动画都已处理
                if (index >= animations.Count)
                {
                    File.AppendAllText(_logFile, "All animations processed\n");
                    return;
                }

                // 并行播放所有动画
                for (int i = 0; i < animations.Count; i++)
                {
                    var currentAnimation = animations[i];
                    File.AppendAllText(_logFile, $"Playing animation {i + 1}/{animations.Count} with hash: {currentAnimation.ShpHash}, reverse: {currentAnimation.IsReverse}, end behavior: {currentAnimation.EndBehavior}, sound: {currentAnimation.SoundHash}, delay: {currentAnimation.SoundDelay}ms\n");

                    // 加载Setup.mix文件
                    MixFile mixFile = new MixFile(SetupMixPath);

                    // 获取SHP文件数据
                    byte[]? shpData = mixFile.GetShpByHash(currentAnimation.ShpHash);
                    if (shpData == null)
                    {
                        File.AppendAllText(_logFile, $"Failed to load SHP file for animation {currentAnimation.ShpHash}\n");
                        continue;
                    }

                    // 获取PAL文件数据
                    string palHash = currentAnimation.PalHash;
                    byte[]? palData = mixFile.GetPalByHash(palHash);
                    if (palData == null)
                    {
                        File.AppendAllText(_logFile, "Failed to load PAL file for animation\n");
                        continue;
                    }

                    // 解析SHP文件
                    ShpFile shpFile = new ShpFile(shpData, palData);

                    // 根据动画配置判断是主动画区还是雷达区动画
                    bool isRadarAnimation = currentAnimation.IsRadarAnimation;

                    // 保存当前动画的配置和索引
                    int currentIndex = i;
                    AnimationConfig config = currentAnimation;

                    // 根据是否为雷达动画选择对应的播放器和图像控件
                    ShpAnimationPlayer animationPlayer;
                    System.Windows.Controls.Image animationImage;
                    
                    if (isRadarAnimation)
                    {
                        _radarShpAnimationPlayer = new ShpAnimationPlayer(shpFile, RadarAnimationImage);
                        animationPlayer = _radarShpAnimationPlayer;
                        animationImage = RadarAnimationImage;
                    }
                    else
                    {
                        _shpAnimationPlayer = new ShpAnimationPlayer(shpFile, AnimationImage);
                        animationPlayer = _shpAnimationPlayer;
                        animationImage = AnimationImage;
                    }

                    // 设置是否倒序播放
                    animationPlayer.IsReverse = currentAnimation.IsReverse;

                    // 根据是否倒序，设置初始帧
                    if (currentAnimation.IsReverse)
                    {
                        animationPlayer.ResetToLastFrame();
                    }
                    else
                    {
                        animationPlayer.Reset();
                    }

                    // 确保动画图像可见
                    animationImage.Visibility = Visibility.Visible;

                    // 添加动画播放完成事件处理程序
                    animationPlayer.AnimationCompleted += (sender, e) =>
                    {
                        try
                        {
                            File.AppendAllText(_logFile, $"{((isRadarAnimation) ? "Radar " : "")}Animation {currentIndex + 1}/{animations.Count} completed\n");

                            // 根据结束行为处理
                            switch (config.EndBehavior)
                            {
                                case AnimationEndBehavior.Disappear:
                                    // 隐藏动画
                                    if (animationImage != null)
                                    {
                                        animationImage.Visibility = Visibility.Collapsed;
                                        File.AppendAllText(_logFile, $"{((isRadarAnimation) ? "Radar " : "")}Animation disappeared as per end behavior\n");
                                    }
                                    break;
                                case AnimationEndBehavior.StayAtLastFrame:
                                    // 停留在最后一帧（不需要额外操作，动画播放完成后会自动停留在最后一帧）
                                    File.AppendAllText(_logFile, $"{((isRadarAnimation) ? "Radar " : "")}Animation stayed at last frame as per end behavior\n");
                                    break;
                                case AnimationEndBehavior.StayAtFirstFrame:
                                    // 停留在第一帧
                                    if (animationPlayer != null)
                                    {
                                        animationPlayer.Reset();
                                        File.AppendAllText(_logFile, $"{((isRadarAnimation) ? "Radar " : "")}Animation stayed at first frame as per end behavior\n");
                                    }
                                    break;
                            }

                            // 只有第一个动画完成时调用callback
                            if (currentIndex == 0)
                            {
                                File.AppendAllText(_logFile, "First animation completed, calling callback\n");
                                callback?.Invoke();
                            }
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllText(_logFile, $"Error in {((isRadarAnimation) ? "radar " : "")}animation completed handler: {ex.Message}\n");
                        }
                    };

                    // 开始播放动画
                    animationPlayer.Play();
                    File.AppendAllText(_logFile, $"{((isRadarAnimation) ? "Radar " : "")}Animation {currentIndex + 1}/{animations.Count} started\n");

                    // 播放音效（带延迟）
                    if (!string.IsNullOrEmpty(currentAnimation.SoundHash))
                    {
                        if (currentAnimation.SoundDelay > 0)
                        {
                            File.AppendAllText(_logFile, $"Waiting for {currentAnimation.SoundDelay}ms before playing sound\n");
                            await Task.Delay(currentAnimation.SoundDelay);
                        }
                        File.AppendAllText(_logFile, $"Playing sound effect: {currentAnimation.SoundHash}\n");
                        string? soundFile = LoadAudioFromMix(currentAnimation.SoundHash);
                        PlayAudio(_soundPlayer, soundFile);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFile, $"Error playing animations sequentially: {ex.Message}\n");
                // 发生错误时仍然调用callback
                if (index == 0)
                {
                    callback?.Invoke();
                }
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
            System.Windows.Application.Current.Shutdown();
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
            // 此方法已被PlayAnimations中的事件处理程序替代
            // 保留此方法以确保兼容性
            File.AppendAllText(_logFile, "Legacy Page2 animation completed event handler called\n");
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