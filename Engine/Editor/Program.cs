﻿using System;
using Avalonia;
using Friflo.Fliox.Editor.Avalonia;

namespace Friflo.Fliox.Editor;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        AppBuilder builder = BuildAvaloniaApp();
        builder.StartWithClassicDesktopLifetime(args);
        
        var editor = new Editor();
        editor.Init(args).Wait();
        editor.Run();
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}

