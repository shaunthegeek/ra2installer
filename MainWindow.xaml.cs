using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RA2Installer.Resources;

namespace RA2Installer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化语言为系统默认语言
        InitializeLanguage();
    }

    private void InitializeLanguage()
    {
        // 获取系统默认语言
        var systemLanguage = CultureInfo.CurrentUICulture;
        
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
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        Strings.Culture = culture;
        
        // 更新UI文本
        UpdateUIText();
    }

    private void UpdateUIText()
    {
        // 更新窗口标题
        this.Title = Strings.WindowTitle;
        
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
            ExitButton.ApplyTemplate();
            
            // 通过ControlTemplate.FindName方法找到ButtonTextBlock
            var template = ExitButton.Template;
            if (template != null)
            {
                var textBlock = template.FindName("ButtonTextBlock", ExitButton) as TextBlock;
                if (textBlock != null)
                {
                    // 清除Inlines，因为我们要直接设置Text
                    textBlock.Inlines.Clear();
                    // 更新文本
                    textBlock.Text = Strings.ExitButton;
                }
            }
            
            // 同时使用视觉树查找作为备用方法
            var visualTextBlock = FindVisualChild<TextBlock>(ExitButton);
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
        foreach (var button in FindVisualChildren<Button>(this))
        {
            button.ApplyTemplate();
            var textBlock = FindVisualChild<TextBlock>(button);
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

    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child != null && child is T)
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
            if (child != null && child is T)
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
        Application.Current.Shutdown();
    }

    private void ChineseSimplifiedButton_Click(object sender, RoutedEventArgs e)
    {
        SetLanguage("zh-CN");
    }

    private void ChineseTraditionalButton_Click(object sender, RoutedEventArgs e)
    {
        SetLanguage("zh-TW");
    }

    private void EnglishButton_Click(object sender, RoutedEventArgs e)
    {
        SetLanguage("en-US");
    }
}