using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using RA2Installer.Resources;

namespace RA2Installer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 获取系统默认语言
        CultureInfo systemLanguage = CultureInfo.CurrentUICulture;

        // 根据系统语言设置UI语言
        if (systemLanguage.Name.StartsWith("zh"))
        {
            SetLanguage("zh-TW");
        }
        else
        {
            SetLanguage("en-US");
        }
    }

    private void SetLanguage(string cultureName)
    {
        CultureInfo culture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        Strings.Culture = culture;
    }
}

