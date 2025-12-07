using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Strings = EasyNSIS.Resources.Strings;

namespace EasyNSIS.Views;

public partial class BuildResultDialog : Window
{
    private readonly string? _outputPath;

    public BuildResultDialog(bool success, string message, string? outputPath = null)
    {
        InitializeComponent();

        _outputPath = outputPath;

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

        // Set title
        Title = success ? Strings.Msg_BuildCompleted : Strings.AppTitle;
        TitleText.Text = success ? Strings.Msg_BuildCompleted : Strings.AppTitle;
        MessageText.Text = message;

        // Set icon and colors based on success (dark theme)
        if (success)
        {
            IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x3D, 0x1F)); // Dark green background
            IconText.Text = "\u2713"; // Checkmark
            IconText.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x59)); // Green
        }
        else
        {
            IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x4D, 0x1F, 0x1F)); // Dark red background
            IconText.Text = "\u2717"; // X mark
            IconText.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49)); // Red
        }

        // Set output path
        if (!string.IsNullOrEmpty(outputPath))
        {
            OutputPathText.Text = outputPath;
            OutputPathPanel.Visibility = Visibility.Visible;
            OpenFolderButton.Visibility = success ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            OutputPathPanel.Visibility = Visibility.Collapsed;
            OpenFolderButton.Visibility = Visibility.Collapsed;
        }

        // Localize button text
        OpenFolderButtonText.Text = Strings.Btn_OpenFolder;
        CloseButton.Content = Strings.Btn_Close;
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_outputPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _outputPath,
                UseShellExecute = true
            });
        }
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
