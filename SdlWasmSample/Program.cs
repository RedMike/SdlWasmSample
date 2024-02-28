using System;
using System.Runtime.InteropServices.JavaScript;

public static partial class Program
{
    /// <summary>
    /// If this is set to true: JS will set the main loop in Emscripten (when JS decides to do it).
    /// If this is set to false: C# will set the main loop in Emscripten (when C# does it, maybe too early for JS to have finished)
    /// </summary>
    private const bool SetMainLoopFromJs = true;
    
    internal static void Main()
    {
        Console.WriteLine("Initial setup of C# side");

        if (!SetMainLoopFromJs)
        {
            Console.WriteLine("Setting up main loop from C#");
            SetMainLoop(MainLoop);
        }
    }

    private static bool _firstRun = true;
    private static DateTime _lastLog = DateTime.UnixEpoch;

    [JSExport]
    internal static void MainLoop()
    {
        if (_firstRun)
        {
            Console.WriteLine("First run of the main loop");
            _firstRun = false;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastLog).TotalSeconds > 1.0)
        {
            _lastLog = now;
            Console.WriteLine($"Main loop still running at: {now}");
        }
    }
    
    [JSExport]
    internal static bool ShouldSetMainLoopFromJs()
    {
        //if we want MainLoop to be set by JS instead of C#
        return SetMainLoopFromJs;
    }

    [JSImport("setMainLoop", "main.js")]
    internal static partial void SetMainLoop([JSMarshalAs<JSType.Function>] Action cb);
}