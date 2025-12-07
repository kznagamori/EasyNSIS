using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace EasyNSIS.Helpers;

public static class ThemeHelper
{
    public static bool IsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 1;
        }
        catch
        {
            return false; // Default to dark theme on error
        }
    }

    public static void ApplyTheme(bool isLight)
    {
        var resources = Application.Current.Resources;

        if (isLight)
        {
            // Light Theme Colors
            resources["BackgroundColor"] = ColorFromHex("#F3F3F3");
            resources["CardBackgroundColor"] = ColorFromHex("#FFFFFF");
            resources["CardHeaderColor"] = ColorFromHex("#E8E8E8");
            resources["SidebarBackgroundColor"] = ColorFromHex("#E5E5E5");
            resources["TextPrimaryColor"] = ColorFromHex("#1A1A1A");
            resources["TextSecondaryColor"] = ColorFromHex("#666666");
            resources["BorderColor"] = ColorFromHex("#D0D0D0");
            resources["InputBackgroundColor"] = ColorFromHex("#FFFFFF");

            // Update brushes
            resources["BackgroundBrush"] = new SolidColorBrush(ColorFromHex("#F3F3F3"));
            resources["CardBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#FFFFFF"));
            resources["CardHeaderBrush"] = new SolidColorBrush(ColorFromHex("#E8E8E8"));
            resources["SidebarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#E5E5E5"));
            resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#1A1A1A"));
            resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#666666"));
            resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#D0D0D0"));
            resources["InputBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#FFFFFF"));

            // NavItem hover color for light theme
            resources["NavItemHoverColor"] = ColorFromHex("#D0D0D0");
            resources["NavItemHoverBrush"] = new SolidColorBrush(ColorFromHex("#D0D0D0"));

            // Log area colors for light theme
            resources["LogBackgroundColor"] = ColorFromHex("#F5F5F5");
            resources["LogBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#F5F5F5"));
            resources["LogTextColor"] = ColorFromHex("#1A1A1A");
            resources["LogTextBrush"] = new SolidColorBrush(ColorFromHex("#1A1A1A"));
            resources["LogOverlayBrush"] = new SolidColorBrush(Color.FromArgb(0xCC, 0xF5, 0xF5, 0xF5));

            // Splitter color for light theme
            resources["SplitterColor"] = ColorFromHex("#C0C0C0");
            resources["SplitterBrush"] = new SolidColorBrush(ColorFromHex("#C0C0C0"));
        }
        else
        {
            // Dark Theme Colors (original)
            resources["BackgroundColor"] = ColorFromHex("#1E1E1E");
            resources["CardBackgroundColor"] = ColorFromHex("#2D2D2D");
            resources["CardHeaderColor"] = ColorFromHex("#363636");
            resources["SidebarBackgroundColor"] = ColorFromHex("#252526");
            resources["TextPrimaryColor"] = ColorFromHex("#E0E0E0");
            resources["TextSecondaryColor"] = ColorFromHex("#A0A0A0");
            resources["BorderColor"] = ColorFromHex("#3F3F3F");
            resources["InputBackgroundColor"] = ColorFromHex("#3C3C3C");

            // Update brushes
            resources["BackgroundBrush"] = new SolidColorBrush(ColorFromHex("#1E1E1E"));
            resources["CardBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#2D2D2D"));
            resources["CardHeaderBrush"] = new SolidColorBrush(ColorFromHex("#363636"));
            resources["SidebarBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#252526"));
            resources["TextPrimaryBrush"] = new SolidColorBrush(ColorFromHex("#E0E0E0"));
            resources["TextSecondaryBrush"] = new SolidColorBrush(ColorFromHex("#A0A0A0"));
            resources["BorderBrush"] = new SolidColorBrush(ColorFromHex("#3F3F3F"));
            resources["InputBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#3C3C3C"));

            // NavItem hover color for dark theme
            resources["NavItemHoverColor"] = ColorFromHex("#3D3D3D");
            resources["NavItemHoverBrush"] = new SolidColorBrush(ColorFromHex("#3D3D3D"));

            // Log area colors for dark theme
            resources["LogBackgroundColor"] = ColorFromHex("#1E1E1E");
            resources["LogBackgroundBrush"] = new SolidColorBrush(ColorFromHex("#1E1E1E"));
            resources["LogTextColor"] = ColorFromHex("#D4D4D4");
            resources["LogTextBrush"] = new SolidColorBrush(ColorFromHex("#D4D4D4"));
            resources["LogOverlayBrush"] = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E));

            // Splitter color for dark theme
            resources["SplitterColor"] = ColorFromHex("#4F4F4F");
            resources["SplitterBrush"] = new SolidColorBrush(ColorFromHex("#4F4F4F"));
        }
    }

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }
}
