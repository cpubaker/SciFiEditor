using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SciFiEditor.App.ViewModels;
using SciFiEditor.Core.DependencyInjection;
using Serilog;
using ILogger = Serilog.ILogger;

namespace SciFiEditor.App;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SciFiEditor",
            "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "scifieditor-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ILogger>(Log.Logger);
                services.AddSciFiEditorCore();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _host.Start();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        Log.Information("SciFiEditor started");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("SciFiEditor exiting");
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
