using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using SdlWasmSample;

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

        //redirect Debug.WriteLine to Console (Mono uses it for errors)
        Trace.Listeners.Add(new ConsoleTraceListener(true));
        
        if (!SetMainLoopFromJs)
        {
            Console.WriteLine("Setting up main loop from C#");
            SetMainLoop(MainLoop);
        }
    }

    private static bool _firstRun = true;
    private static DateTime _lastLog = DateTime.UnixEpoch;
    // private static SampleSdl? _sample = null;
    private static SampleGame? _sample = null;
    
    [JSExport]
    private static void MainLoop()
    {
        try
        {
            if (_firstRun)
            {
                Console.WriteLine("First run of the main loop");
                _firstRun = false;

                //_sample = new SampleSdl($"Test Window {DateTime.UtcNow:s}", 600, 400);
                _sample = new SampleGame();
            }

            var now = DateTime.UtcNow;
            if ((now - _lastLog).TotalSeconds > 1.0)
            {
                _lastLog = now;
                Console.WriteLine($"Main loop still running at: {now}");
            }

            if (_sample != null)
            {
                _sample.RunOneFrame();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }
    
    [JSExport]
    private static bool ShouldSetMainLoopFromJs()
    {
        //if we want MainLoop to be set by JS instead of C#
        return SetMainLoopFromJs;
    }

    [JSImport("setMainLoop", "main.js")]
    private static partial void SetMainLoop([JSMarshalAs<JSType.Function>] Action cb);
}