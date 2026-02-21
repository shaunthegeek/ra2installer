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
        private System.Windows.Media.MediaPlayer _backgroundMusicPlayer;
        private string _buttonClickSoundFile;
        private string _backgroundMusicFile;
        private ShpAnimationPlayer _shpAnimationPlayer;

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
                _mediaPlayer = new System.Windows.Media.MediaPlayer();
                _backgroundMusicPlayer = new System.Windows.Media.MediaPlayer();

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
                Loaded += MainWindow_Loaded;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化语言为系统默认语言
            InitializeLanguage();

            // 播放背景音乐
            PlayBackgroundMusic();

            // 不自动启动 SHP 动画，只显示第一帧
            File.AppendAllText(_logFile, "Not starting SHP animation automatically, showing only first frame\n");
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
                File.AppendAllText(_logFile, "Animation player created, will start playback in Loaded event\n");
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
            
            // 切换到第二页
            SwitchToPage2();
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

        private void ApplyCharacterSpacing()
        {
            // 查找窗口中的所有按钮
            foreach (Button button in FindVisualChildren<Button>(this))
            {
                _ = button.ApplyTemplate();
                TextBlock textBlock = FindVisualChild<TextBlock>(button);
                if (textBlock != null)
                {
                    // 对于取消按钮，使用资源文件中的原始文本
                    string originalText = "";
                    if (button.Name == "CancelButton")
                    {
                        originalText = Strings.CancelButton;
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
                    // 使用 Uri 播放音频
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
            PlayAudio(_mediaPlayer, _buttonClickSoundFile);
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
            // 开始播放 SHP 动画
            StartShpAnimation();
        }

        private void ChineseTraditionalButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("zh-TW");
            // 开始播放 SHP 动画
            StartShpAnimation();
        }

        private void EnglishButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            SetLanguage("en-US");
            // 开始播放 SHP 动画
            StartShpAnimation();
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
        /// 切换到第二页
        /// </summary>
        private void SwitchToPage2()
        {
            // 隐藏第一页
            Page1.Visibility = Visibility.Collapsed;
            // 显示第二页
            Page2.Visibility = Visibility.Visible;
            
            // 为第二页加载相同的背景图片
            LoadBackgroundImageForPage2();
            
            // 更新第二页的UI文本，使用当前选择的语言
            UpdatePage2UIText();
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
            
            // 重新应用字符间距，确保和第一页相同
            ApplyCharacterSpacing();
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
        }
        
        /// <summary>
        /// 下一步按钮点击事件
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            // 这里可以添加下一步的逻辑
        }
        
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButtonClickSound();
            Application.Current.Shutdown();
        }
    }
}