/**
 * This file exists because dotnet uses Emscripten 3.1.34, whereas modern SDL requires 3.1.35+,
 * this is because these two methods moved from the runtime to the library, and therefore SDL code
 * references them as library functions. This should work as a shim to call the runtime.
 */

if (
    (DEFAULT_LIBRARY_FUNCS_TO_INCLUDE.indexOf("$stringToUTF8") >= 0) && //if it's 3.1.35+ code and was built with 3.1.34
    !("$stringToUTF8__deps" in LibraryManager.library)    //and it's being linked in Emscripten 3.1.34
) {
    if (VERBOSE) {
        warn(`Forcing $stringToUTF8 as a library shim`)        
    }
    mergeInto(LibraryManager.library, {
        '$stringToUTF8__internal': true,
        '$stringToUTF8': function (str, outPtr, maxBytesToWrite) {
            return stringToUTF8(str, outPtr, maxBytesToWrite);
        },
    });
}

if (
    (DEFAULT_LIBRARY_FUNCS_TO_INCLUDE.indexOf("$UTF8ToString") >= 0) && //if it's 3.1.35+ code and was built with 3.1.34
    !("$UTF8ToString__deps" in LibraryManager.library)    //and it's being linked in Emscripten 3.1.34
) {
    if (VERBOSE) {
        warn(`Forcing $UTF8ToString as a library shim`)
    }
    mergeInto(LibraryManager.library, {
        '$UTF8ToString__internal': true,
        '$UTF8ToString': function (str, outPtr, maxBytesToWrite) {
            return stringToUTF8(str, outPtr, maxBytesToWrite);
        },
    });
}