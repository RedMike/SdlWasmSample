/**
 * This file exists because dotnet uses Emscripten 3.1.34, whereas modern SDL requires 3.1.35+,
 * this is because these two methods moved from the runtime to the library, and therefore SDL code
 * references them as library functions. This should work as a shim to call the runtime.
 */

mergeInto(LibraryManager.library, {
    '$stringToUTF8__internal': true,
    '$stringToUTF8': function(str, outPtr, maxBytesToWrite) {
         return stringToUTF8(str, outPtr, maxBytesToWrite);
    },
    '$UTF8ToString__internal': true,
    '$UTF8ToString': function(ptr, maxBytesToRead) {
        return UTF8ToString(ptr, maxBytesToRead);
    }
});
