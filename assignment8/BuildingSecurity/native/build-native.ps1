param([string]$Compiler = "gcc")
$ErrorActionPreference = "Stop"
$machine = & $Compiler -dumpmachine
if ($LASTEXITCODE -ne 0 -or $machine -notmatch '^x86_64-.*mingw32$') {
    throw "Use a Windows x64 MinGW-w64 compiler (for example MSYS2 UCRT64 GCC)."
}
$destination = Join-Path $PSScriptRoot '../runtimes/win-x64/native'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
& $Compiler -std=c11 -O2 -Wall -Wextra -Werror -shared -static-libgcc `
    (Join-Path $PSScriptRoot 'security_native.c') -o (Join-Path $destination 'security_native.dll')
if ($LASTEXITCODE -ne 0) { throw "Native compilation failed." }
Write-Output "Built Windows x64 library: $destination/security_native.dll"
