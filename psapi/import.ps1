dotnet build "$PSScriptRoot/Module"
pwsh-preview -i -noexit -c "ipmo '$PSScriptRoot/Module/bin/Debug/net6.0/Module.dll'"