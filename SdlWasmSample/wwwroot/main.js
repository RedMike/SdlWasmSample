// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

//this allows us to handle the main loop from C# 
setModuleImports('main.js', {
    setMainLoop: (cb) => dotnet.instance.Module.setMainLoop(cb)
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const shouldSetMainLoopFromJs = exports.Program.ShouldSetMainLoopFromJs;
const cSharpMainLoop = exports.Program.MainLoop;

//set canvas
var canvas = document.getElementById("canvas");
dotnet.instance.Module.canvas = canvas;
await dotnet.run();

console.log("Initial setup of JS side");

//this allows us to set the main loop from JS
if (shouldSetMainLoopFromJs()) {
    console.log("Setting up main loop from JS");
    dotnet.instance.Module.setMainLoop(cSharpMainLoop);
}
