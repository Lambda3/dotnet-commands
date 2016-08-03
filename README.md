# .NET Commands

A tool that allows you to use .NET Core apps as .NET CLI Commands.

[![Nuget count](https://img.shields.io/nuget/v/dotnet-commands.svg)](https://www.nuget.org/packages/dotnet-commands/)
[![License](https://img.shields.io/badge/licence-Apache%20License%202.0-blue.svg)](https://github.com/Lambda3/dotnet-commands/blob/master/LICENSE.txt)
[![Issues open](https://img.shields.io/github/issues-raw/Lambda3/dotnet-commands.svg)](https://huboard.com/Lambda3/dotnet-commands/)

This is a community project, free and open source. Everyone is invited to contribute, fork, share and use the code.

## Installing

On Windows, run:

### Powershell
````powershell
&{$wc=New-Object System.Net.WebClient;$wc.Proxy=[System.Net.WebRequest]::DefaultWebProxy;$wc.Proxy.Credentials=[System.Net.CredentialCache]::DefaultNetworkCredentials;Invoke-Expression($wc.DownloadString('https://raw.githubusercontent.com/Lambda3/dotnet-commands/master/src/dotnet-commands/install.ps1'))}
````

### CMD
````cmd
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$wc=New-Object System.Net.WebClient;$wc.Proxy=[System.Net.WebRequest]::DefaultWebProxy;$wc.Proxy.Credentials=[System.Net.CredentialCache]::DefaultNetworkCredentials;Invoke-Expression($wc.DownloadString('https://raw.githubusercontent.com/Lambda3/dotnet-commands/master/src/dotnet-commands/install.ps1'))}"
````

On Linux, TBD.

## Running

Simply run `dotnet commands` to see the options, which are similar to this:

````
DotNet Commands
  Usage:
    dotnet commands install <command> [--force] [--pre] [--verbose]
    dotnet commands uninstall <command> [ --verbose]
    dotnet commands update (<command> | all) [--pre] [--verbose]
    dotnet commands --help
    dotnet commands --version
  Options:
    --force                    Installs even if package was already installed. Optional.
    --pre                      Include pre-release versions. Optional.
    --verbose                  Verbose. Optional.
    --help -h                  Show this screen.
    --version -v               Show version.
````

You can try to install `dotnet-foo`, a library to try.

```powershell
dotnet commands install dotnet-foo
```

And you can then use `dotnet-foo` like this:

```powershell
dotnet foo
```

## Writing commands

A more detailed explanation is comming. But the idea is simple, either create a Nuget which contains an executable (*.exe, *.ps1, or *.cmd)
in the `tools` folder named `dotnet-yourtool`, or add a `commandMetadata.json` similar
to [the one in this project](https://github.com/Lambda3/dotnet-commands/blob/master/src/dotnet-commands/commandMetadata.json) and add it to the
`content` folder.

## Status

We are still missing work on Linux and Mac, and only install works. PRs welcome.

## Maintainers/Core team

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), aka Giggio, [Lambda3](http://www.lambda3.com.br), [@giovannibassi](https://twitter.com/giovannibassi)

Contributors can be found at the [contributors](https://github.com/Lambda3/dotnet-commands/graphs/contributors) page on Github.

## Contact

Twitter is the best option.

## License

This software is open source, licensed under the Apache License, Version 2.0.
See [LICENSE.txt](https://github.com/Lambda3/dotnet-commands/blob/master/LICENSE.txt) for details.
Check out the terms of the license before you contribute, fork, copy or do anything
with the code. If you decide to contribute you agree to grant copyright of all your contribution to this project, and agree to
mention clearly if do not agree to these terms. Your work will be licensed with the project at Apache V2, along the rest of the code.
