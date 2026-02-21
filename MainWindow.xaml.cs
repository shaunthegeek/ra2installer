using System.Globalization;
using System.IO;
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
        
        private System.Windows.Media.MediaPlayer _mediaPlayer;
        private byte[] _buttonClickSound;

        public MainWindow()
        {
            try
            {
                Console.WriteLine("Starting MainWindow initialization");

                // 首先初始化组件，这样 Grid 控件就会被创建
                InitializeComponent();

                Console.WriteLine("Components initialized, loading background image");

                // 然后加载背景图片
                LoadBackgroundImageFromMix(SetupMixPath, "B1D71F00", "bmp");

                // 初始化 MediaPlayer
                _mediaPlayer = new System.Windows.Media.MediaPlayer();

                // 加载按钮点击音效
                LoadButtonClickSound();

                Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MainWindow initialization: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // 不退出程序，继续初始化
                InitializeComponent();
                Loaded += MainWindow_Loaded;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化语言为系统默认语言
            InitializeLanguage();
        }



        /// <summary>
        /// 从指定路径的 Setup.mix 中加载指定哈希值和类型的图片作为背景
        /// 如果加载失败则输出日志但不退出程序
        /// </summary>
        /// <param name="setupMixPath">Setup.mix 文件的路径</param>
        /// <param name="fileNameHash">文件名哈希值</param>
        /// <param name="fileType">文件类型（如 "bmp"）</param>
        private void LoadBackgroundImageFromMix(string setupMixPath, string fileNameHash, string fileType)
        {
            try
            {
                // 检查文件是否存在
                if (setupMixPath == null)
                {

                    return;
                }

                if (!System.IO.File.Exists(setupMixPath))
                {

                    return;
                }

                // 加载 Setup.mix 文件
                MixFile mixFile = new MixFile(setupMixPath);

                // 尝试获取指定哈希值和类型的图片
                System.Windows.Media.Imaging.BitmapImage backgroundImage = mixFile.GetImageByHash(fileNameHash, fileType);

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

            // 更新UI文本
            UpdateUIText();
        }

        private void UpdateUIText()
        {
            // 更新窗口标题
            Title = Strings.WindowTitle;

            // 手动更新按钮文本
            UpdateButtonTexts();

            // 重新应用字符间距
            ApplyCharacterSpacing();
        }

        private void UpdateButtonTexts()
        {
            // 直接通过Name找到退出按钮
            if (ExitButton != null)
            {
                _ = ExitButton.ApplyTemplate();

                // 通过ControlTemplate.FindName方法找到ButtonTextBlock
                ControlTemplate template = ExitButton.Template;
                if (template != null)
                {
                    if (template.FindName("ButtonTextBlock", ExitButton) is TextBlock textBlock)
                    {
                        // 清除Inlines，因为我们要直接设置Text
                        textBlock.Inlines.Clear();
                        // 更新文本
                        textBlock.Text = Strings.ExitButton;
                    }
                }

                // 同时使用视觉树查找作为备用方法
                TextBlock visualTextBlock = FindVisualChild<TextBlock>(ExitButton);
                if (visualTextBlock != null)
                {
                    // 清除Inlines，因为我们要直接设置Text
                    visualTextBlock.Inlines.Clear();
                    // 更新文本
                    visualTextBlock.Text = Strings.ExitButton;
                }
            }
        }

        private void ApplyCharacterSpacing()
        {
            // 查找窗口中的所有按钮
            foreach (Button button in FindVisualChildren<Button>(this))
            {
                _ = button.ApplyTemplate();
                TextBlock textBlock = FindVisualChild<TextBlock>(button);
                if (textBlock != null)
                {
                    // 对于退出按钮，使用资源文件中的原始文本
                    string originalText = "";
                    if (button.Name == "ExitButton")
                    {
                        originalText = Strings.ExitButton;
                    }
                    else if (textBlock.Text != null)
                    {
                        // 对于其他按钮，使用当前文本，但移除可能存在的多余空格
                        originalText = textBlock.Text.Replace(" ", "");
                    }

                    if (!string.IsNullOrEmpty(originalText))
                    {
                        textBlock.Inlines.Clear();

                        // 为每个字符添加Run元素，并在字符之间添加空格
                        for (int i = 0; i < originalText.Length; i++)
                        {
                            textBlock.Inlines.Add(new Run(originalText[i].ToString()));
                            if (i < originalText.Length - 1)
                            {
                                textBlock.Inlines.Add(new Run(" "));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 加载按钮点击音效
        /// </summary>
        private void LoadButtonClickSound()
        {
            try
            {
                // 使用确定的路径加载 Setup.mix 文件
                if (File.Exists(SetupMixPath))
                {
                    // 加载 Setup.mix 文件
                    MixFile mixFile = new MixFile(SetupMixPath);

                    // 尝试获取指定哈希值和类型的音频
                    _buttonClickSound = mixFile.GetAudioByHash("C7A23518", "aud");

                    if (_buttonClickSound != null)
                    {
                        Console.WriteLine("Button click sound loaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to load button click sound.");
                    }
                }
                else
                {
                    Console.WriteLine($"Setup.mix file not found at: {SetupMixPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading button click sound: {ex.Message}");
            }
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        private void PlayButtonClickSound()
        {
            try
            {
                if (_buttonClickSound != null)
                {
                    // 保存音频数据到临时文件
                    string tempFile = Path.GetTempFileName() + ".wav";
                    File.WriteAllBytes(tempFile, _buttonClickSound);
                    
                    // 使用 Uri 播放音频
                    _mediaPlayer.Open(new Uri(tempFile));
                    _mediaPlayer.Play();
                    
                    // 播放完成后删除临时文件
                    _mediaPlayer.MediaEnded += (sender, e) =>
                    {
                        try
                        {
                            if (File.Exists(tempFile))
                            {
                                File.Delete(tempFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting temp audio file: {ex.Message}");
                        }
                    };
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

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            Application.Current.Shutdown();
        }

        private void ChineseSimplifiedButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("zh-CN");
        }

        private void ChineseTraditionalButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("zh-TW");
        }

        private void EnglishButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("en-US");
        }
    }
}