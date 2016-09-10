@echo off
dotnet %~dp0\dotnet-commands.dll %*
set EL=%ERRORLEVEL%
IF %EL% EQU 113 (
    powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$wc=New-Object System.Net.WebClient;$wc.Proxy=[System.Net.WebRequest]::DefaultWebProxy;$wc.Proxy.Credentials=[System.Net.CredentialCache]::DefaultNetworkCredentials;Invoke-Expression($wc.DownloadString('https://raw.githubusercontent.com/Lambda3/dotnet-commands/master/src/dotnet-commands/install.ps1'))}"
) else (
    exit %EL%
)