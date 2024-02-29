# C# SDL WebAssembly Sample

This is a sample showing how with the current .NET SDK (8 at time of writing), it
is possible to use the `browser-wasm` template (in `wasm-tools` workload) to 
show direct graphics via WebGL (using SDL2 as a library) **without using Blazor**.

There are still a number of problems with .NET's WebAssembly support for this, which
have to be worked around in each program/library, but it is now feasible to do
this without requiring a custom build of the runtime/SDK to allow it to work.
See [Remaining .NET WebAssembly Issues](#remaining-net-webassembly-issues) for more info on this.

To see the difference from the templates, look at the list of commits in the 
`initial-net8` branch, which contains just the basic setup described below.
For other functionality, look at subsequent branches and their commits (e.g. 
full AOT compilation).

If you're trying to build your project that uses a library that uses SDL internally
(like MonoGame, FNA, etc), while SDL itself is easy to use effectively as-is,
these other libraries are not as simple to use: see 
[MonoGame/Library Usage of SDL](#monogamelibrary-usage-of-sdl).
There is a `monogame-net8` branch that demonstrates this, but also involves
forking MonoGame to apply some small changes. See the section mentioned for info.

## Basic Setup

Requirements to build/follow along with this repository:
* Install .NET 8 (or newer, **but some commands/config options may change**!)
* Install the `wasm-tools` workload: `dotnet workload install wasm-tools`

This project is effectively just:

* New project with `browser-wasm` template,
* Import SDL C# binding (*see [Native Library Name](#native-library-name) section below*),
* Import SDL native library built with Emscripten (*see [Emscripten Frozen Cache/Ports](#emscripten-frozen-cacheports) section below*),
* Set up main loop binding for Emscripten (*see [Emscripten Main Loop](#emscripten-main-loop) section below*),
* Set up a basic window class that just clears the screen,   

With other branches containing follow-up commits that add various
bits of functionality (AOT compilation, etc).

## Remaining .NET WebAssembly Issues

While a lot of the setup now works out of the box, there are a number of 
issues that are either stuck waiting on the dotnet team to change
in the runtime/SDK, or they've decided against fixing and are probably
going to be long-term issues.

### Emscripten Interaction API Changes

This is not so much an issue as a consequence of the WebAssembly support
still being early in development. The MSBuild parameters used to configure
the Emscripten environment/build process are still in flux and likely
to change either in name or behaviour over time. 

Hopefully, most changes will be for the better, but it is possible 
some changes will break existing functionality without offering alternatives, 
if the dotnet team consider the functionality irrelevant or problematic.

### Emscripten Frozen Cache/Ports

Emscripten is used internally by the dotnet SDK as part of the build
process for projects of SDK type `Microsoft.NET.Sdk.WebAssembly`. When building
the project, MSBuild will use a package called `Microsoft.NET.Runtime.Emscripten`
(on Windows, downloaded by default to `C:\Program Files\dotnet\packs\`) to
download and store a copy of Emscripten specifically for compiling these projects.
At time of writing, this uses version `3.1.34`.

Emscripten has functionality called "ports" which are libraries provided
automatically to Emscripten-built projects, to cover common library requirements
(and provide functional WebAssembly versions of them). This includes both
SDL 1.x and SDL 2.x (depending on build flags). So there's no need for your
project to include a binary of SDL, because Emscripten will provide it.

As part of the Emscripten build process, Emscripten would download and store a
cache of these ports in its install folder... which breaks on Windows because
the Emscripten install is in `C:\Program Files\` (which acts oddly when non-Admin).

To avoid this, dotnet team grabbed only the ports they thought would be required
(which does not include SDL 1 or 2), set up the cache within the 
`Microsoft.NET.Runtime.Emscripten.Cache` package, and made MSBuild forcibly set 
a `FROZEN_CACHE` environment variable as part of the build process, to 
tell Emscripten that it's **not allowed to download any new ports**. 

Therefore, if you try to send Emscripten the build flags to add SDL to the 
build (as part of ports), which is `-lSDL -s USE_SDL=2` for `3.1.34`, it will
try to add the port, realise the cache is frozen, and fail with the error:
`Exception: Attempt to lock the cache but FROZEN_CACHE is set`.

Dotnet team could fix this going forwards, or at least offer a way to set a 
cache folder as part of MSBuild parameters, but until then there are two valid
workarounds:
* Hook into the MSBuild process with a new target to overwrite the `FROZEN_CACHE`
environment variable, and provide a new cache location for it to use. 
([This was the approach taken here by a Silk.NET developer](https://perksey.com/blog/blazor-silkdotnet.html)).
However, this is likely brittle long-term as the process will break when
updating the local .NET runtime/SDK, not any packages or project files.
* Don't rely on Emscripten's ports and instead include your own copy of
the SDL binary built for WebAssembly via Emscripten, using
`<NativeFileReference>` MSBuild parameter. This is stable long-term,
and the only downside is needing to update SDL deliberately when needed. 
**This is the approach this project used.**

### Emscripten Main Loop

One of the few code changes you'd make to allow your project to build via
Emscripten is that you need to change your main program entry-point to
not run all the code in a blocking way (like `while(1) { doThings(); }`), 
but instead define a function that should be called on a regular basis.
That function is then your "main loop" that is called at either a regular
interval, or called when the browser thinks animation should happen. For 
graphical programs, you almost always want your main loop to run as the latter.

Unfortunately, there is no direct/obvious way to tell Emscripten this baked
into the project template, even though it would be pretty easy to add. Hopefully
in the future the dotnet developers add something to the template to describe
this logic (the way they add the `JSImport`/`JSExport` logic), if not make it
easier to add.

For now, these workarounds exist:

* Use some arcane `[DllImport]` attributes pointing at `__Internal_emscripten`
to get references to the `emscripten_set_main_loop` and similar functions,
which the C# code can then run, but also runs into issues with marshalling
function pointers.
([This was the approach taken here by a Silk.NET developer](https://perksey.com/blog/blazor-silkdotnet.html)).
This was previously considered the only way, however it is completely
obsoleted by the next approach.
* Add `EmccExportedRuntimeMethod` parameters to the `.csproj` file to 
expose `setMainLoop` and similar methods in the Javascript side of Emscripten.
In `main.js` you can then pass that function to C# (via `[JsImport]` with
working function marshalling) and call it, or in reverse pass the main loop
function from C# to `main.js` (via `[JsExport]`) and call it there.
To see the correct name to export for any Emscripten function, the Javascript
can be debugged and `dotnet.instance.Module` can be inspected, where all 
methods are listed (whether exported or not). **This is the approach this project
used.**

### Native Library Name

This project uses [the SDL2-CS](https://github.com/flibitijibibo/SDL2-CS)
library as C# binding to SDL. This library targets older .NET versions
and therefore uses `[DllImport]` and traditional marshalling (especially for 
strings), which is not a major issue however is not ideal for something
known to be on .NET 8.

However there is an actual issue within the dotnet runtime that required 
[a custom fork of the SDL2-CS library with a very minor change](https://github.com/RedMike/SDL2-CS). 
This change is just changing the name of the native library that the `[DllImport]` attributes use 
to `libSDL2` (the Linux version) instead of `SDL2`.

The issue within the dotnet runtime is that while `[DllImport]` and `[LibraryImport]`
try to change the name of the library to match the platform (e.g. adding `lib` 
when building for Linux), this logic does not work correctly for WebAssembly
builds, where the library is likely to require the Linux name.

There is a function `NativeLibrary.SetDllImportResolver` which attempts to
allow changing the name used for `[DllImport]` at run-time, which works
only on platforms that are not AOT-compiled or WebAssembly-based. Using this,
when building for WebAssembly, the behaviour is even more broken: resolving 
the library itself succeeds, but using any method fails because the symbols fail
to resolve.

Dotnet team could potentially look at fixing this so the `SetDllImportResolver`
function works as expected with WebAssembly, however it is unclear if full
AOT builds would still work correctly. This would require changes to the
P/Invoke table generation in dotnet's WebAssembly build tasks. In the meantime 
these workarounds exist:

* Duplicate the binding class for this platform and change the library name used. 
Not ideal because duplication, but it works. Simplest solution, **this is the 
approach this project used.**
* Flip the setup: have the default name be `libSDL2`, then use 
`NativeLibrary.SetDllImportResolver` to actively change it back to `SDL2`
for non-WebAssembly platforms. It is unclear if full AOT works correctly on 
all platforms for this, but it seems to work at least with `[LibraryImport]`.

## MonoGame/Library usage of SDL

Technically with this work, it should be possible for someone to take their C#
project and via minor changes re-build it for WebAssembly. Because all input/
output/system interaction is via SDL, as long as SDL works, then *technically*
getting the project to run should be simple. Unfortunately, there is one problem
that most of these libraries have in common: dynamic linking.

Basically, for multiple reasons including portability and ease of maintenance,
these libraries don't directly bind to SDL via a static class like the one in
this project. If they did, then yes outside of a few similar changes to this 
project, it would be straightforward to build them for WebAssembly.

Using MonoGame as the example, 
[what the repository does at time of writing is this:](https://github.com/MonoGame/MonoGame/blob/5e382b08d7c9a7e38dc9f431d3b1c79af41a3645/MonoGame.Framework/Platform/SDL/SDL2.cs#L218)

```c#
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void d_sdl_disablescreensaver();
    public static d_sdl_disablescreensaver DisableScreenSaver = FuncLoader.LoadFunction<d_sdl_disablescreensaver>(NativeLibrary, "SDL_DisableScreenSaver");
```

Where `FuncLoader` [links into a class like this:](https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Platform/Utilities/FuncLoader.Desktop.cs#L11)

```c#
        private class Windows
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryW(string lpszLib);
        }
```

What this basically does is use dynamic linking at run-time, instead of statically linking a library.
So instead of your C# binding directly listing the SDL functions and then tying it to a DLL that's included,
it will use the OS-level functions to load that DLL/each method and call it. 

There is however a simple solution that does work, even if it is more cumbersome: 
provide the SDL C# binding as per the sample, and replace calls to `dlopen/dlsym`
or similar with `NativeLibrary.Load/GetExport` C# native methods. What this will
do is basically "dynamically" link to the SDL C# binding that exists, and the 
existing code will work. 
[This has been done in the `monogame-net8` branch for MonoGame.](https://github.com/RedMike/SdlWasmSample/compare/initial-net8...monogame-net8)

Unfortunately, there is subsequent work that has to happen to also allow the
libraries to correctly work, although it should be possible for the libraries
to do the changes internally. Specifically, OpenGL ES must be used, some flags
need to be avoided, and some legacy GL commands need to not be called.
[This has been done on a MonoGame fork branch here.](https://github.com/MonoGame/MonoGame/compare/develop...RedMike:MonoGame:feature/dotnet-wasm)

At time of writing, this sample in the `monogame-net8` branch works, although
it is not complete and probably has issues with assets/audio/etc as well as
other legacy/old GL methods.




