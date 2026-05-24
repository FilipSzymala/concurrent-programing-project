using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;
using Data.Diagnostics;
using Logic;
using Microsoft.Extensions.DependencyInjection;
using Presentation.ViewModels;
using Presentation.Views;

namespace Presentation;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<BallDiagnosticsLogger>(_ =>
        {
            string path = Path.Combine(
                ResolveLogDirectory(),
                $"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            return BallDiagnosticsLogger.Create(path, queueCapacity: 1024);
        });
        services.AddSingleton<BallLogicApi>(sp =>
            BallLogicApi.CreateApi(600, 600, sp.GetRequiredService<BallDiagnosticsLogger>()));

        services.AddSingleton<BoardViewModel>();
        services.AddTransient<MainWindowViewModel>();

        var serviceProvider = services.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            
            var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private static string ResolveLogDirectory()
    {
        const string solutionMarker = "ConcurrentProgramming.slnx";
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, solutionMarker)))
                return Path.Combine(dir, "logs");
            string parent = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(parent) || parent == dir) break;
            dir = parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "logs");
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}