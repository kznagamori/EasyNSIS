using System.ComponentModel;
using System.Windows;
using EasyNSIS.ViewModels;
using EasyNSIS.Views;
using AboutWindow = EasyNSIS.Views.AboutWindow;

namespace EasyNSIS;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.BuildResultReady -= OnBuildResultReady;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.BuildResultReady += OnBuildResultReady;
        }
    }

    private void OnBuildResultReady(object? sender, BuildResultEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var dialog = new BuildResultDialog(e.Success, e.Message, e.OutputPath)
            {
                Owner = this
            };
            dialog.ShowDialog();
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (!vm.CanClose())
            {
                e.Cancel = true;
            }
        }
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }
}
