﻿using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using RandomizerCore.Asm;

[assembly: SupportedOSPlatform("browser")]

namespace CrossPlatformUI.Browser;

internal sealed class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .WithInterFont()
        .UseReactiveUI()
        .AfterSetup(async _ =>
        {
            App.ServiceContainer ??= new ();
            App.ServiceContainer.AddSingleton<IAsmEngine>(x => new BrowserJsEngine());
        })
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}