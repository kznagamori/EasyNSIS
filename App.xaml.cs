using System.IO;
using System.Threading;
using System.Windows;
using EasyNSIS.Helpers;
using EasyNSIS.Services;
using EasyNSIS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Strings = EasyNSIS.Resources.Strings;

namespace EasyNSIS;

public partial class App : Application
{
    private static Mutex? _mutex;
    private ServiceProvider? _serviceProvider;

    public static IServiceProvider Services { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // 多重起動防止
        const string mutexName = "EasyNSIS_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                Strings.Msg_AlreadyRunning,
                Strings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // assetsフォルダーの存在確認
        var basePath = AppContext.BaseDirectory;
        var assetsPath = Path.Combine(basePath, "assets");
        var toolsPath = Path.Combine(basePath, "tools", "nsis-3.11");

        if (!Directory.Exists(assetsPath) || !Directory.Exists(toolsPath))
        {
            MessageBox.Show(
                $"Required folders not found:\n- {assetsPath}\n- {toolsPath}\n\nPlease ensure 'assets' and 'tools/nsis-3.11' folders exist.",
                Strings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // DIコンテナの設定
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        // グローバル例外ハンドラー
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // Windowsテーマに合わせた色の適用
        ApplyWindowsTheme();

        // テーマ変更の監視
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        // メインウィンドウの表示
        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<INsisService, NsisService>();
        services.AddSingleton<ILogService, LogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logService = _serviceProvider?.GetService<ILogService>();
        logService?.LogError("Unhandled exception", e.Exception);

        MessageBox.Show(
            $"An unexpected error occurred:\n{e.Exception.Message}",
            Strings.AppTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logService = _serviceProvider?.GetService<ILogService>();
        if (e.ExceptionObject is Exception ex)
        {
            logService?.LogError("Unhandled domain exception", ex);
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _serviceProvider?.Dispose();
    }

    private static void ApplyWindowsTheme()
    {
        var isLight = ThemeHelper.IsLightTheme();
        ThemeHelper.ApplyTheme(isLight);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.Invoke(ApplyWindowsTheme);
        }
    }
}
