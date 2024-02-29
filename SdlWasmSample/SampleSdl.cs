using System;
using SDL2;

namespace SdlWasmSample;

//not used for MonoGame
public class SampleSdl : IDisposable
{
    private const int MaxEventsInOneTick = 100;
    
    static SampleSdl()
    {
        try
        {
            InitSdl();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
    
    static void InitSdl()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            var err = "Unable to init SDL at all: ";
            try
            {
                err = SDL.SDL_GetError();
            }
            catch (Exception e)
            {
                err += $"{e}";
            }
            throw new Exception($"Unable to init SDL: {err}");
        }
    }

    public static void QuitSdl()
    {
        try
        {
            SDL.SDL_Quit();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error while quitting SDL: {e}");
        }
    }

    private IntPtr _window;
    private IntPtr _renderer;
    private bool _shouldQuit;

    public SampleSdl(string title, int w, int h)
    {
        try
        {
            InitWindowAndRenderer(title, w, h);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    public bool MainLoop()
    {
        try
        {
            if (_shouldQuit)
            {
                return true;
            }
            CheckEvents();
            if (_shouldQuit)
            {
                return true;
            }
            Render();
            if (_shouldQuit)
            {
                return true;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return false;
    }

    private void InitWindowAndRenderer(string title, int w, int h)
    {
        _window = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, w, h, 
            SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | //correctly render toolbar if not WASM
            SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL //correctly use GL
        );
        if (_window == IntPtr.Zero)
        {
            var err = SDL.SDL_GetError();
            throw new Exception($"Failed to create window: {err}");
        }

        _renderer = SDL.SDL_CreateRenderer(_window, -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | //correctly use GL
            SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC //changes how the main loop is bound in Emscripten SDL
        );
        if (_renderer == IntPtr.Zero)
        {
            var err = SDL.SDL_GetError();
            throw new Exception($"Failed to create renderer: {err}");
        }
    }

    private void DeleteWindowAndRenderer()
    {
        if (_renderer != IntPtr.Zero)
        {
            SDL.SDL_DestroyRenderer(_renderer);
        }
        
        if (_window != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(_window);
        }
    }

    private void CheckEvents()
    {
        var i = 0;
        while (SDL.SDL_PollEvent(out var ev) != 0 && i < MaxEventsInOneTick)
        {
            i++;
            switch (ev.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    _shouldQuit = true;
                    break;
                default:
                    Console.WriteLine($"Unhandled SDL event: {ev.type}");
                    break;
            }
        }

        if (i >= MaxEventsInOneTick)
        {
            Console.WriteLine($"Hit maximum number of SDL events in one tick: {i}");
        }
    }

    private void Render()
    {
        SDL.SDL_SetRenderDrawColor(_renderer, 0x3F, 0x3F, 0xFF, 0xFF);
        SDL.SDL_RenderClear(_renderer);

        SDL.SDL_RenderPresent(_renderer);
    }

    public void Dispose()
    {
        try
        {
            DeleteWindowAndRenderer();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error while deleting window and renderer: {e}");
        }
    }
}