# wix - the WiX Toolset from the command-line

The `wix` package provides the WiX Toolset as a .NET Tool, perfect for your command-line packaging pleasure (even if we recommend using the [WixToolset.Sdk][sdk]).

[Web Site][web] | [Documentation][docs] | [Issue Tracker][issues] | [Discussions][discussions]


## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate revenue must pay an [Open Source Maintenance Fee][osmf]. While the source code is freely available under the terms of the [LICENSE][license], this package and other aspects of the project require [adherence to the Open Source Maintenance Fee EULA][eula].

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/wixtoolset).


## Before we begin

For most users, we recommend using the [WixToolset.Sdk][sdk] to build your installation packages instead of this command-line tool. The Sdk provides a better development experience than the command-line, especially as your project grows. The following is a quick example.

Example project file: `QuickStart.wixproj`
```
<Project Sdk="WixToolset.Sdk/6.0.2">
</Project>
```

Example source code: `QuickStart.wxs`
```
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Id="AcmeCorp.QuickStart" Name="QuickStart Example" Manufacturer="ACME Corp" Version="0.0.1">
    <File Source="example.txt" />
  </Package>
</Wix>
```

Build your MSI from the command-line:
```
dotnet build
```


## Getting started

If you still decide to use this command-line tool instead of the [WixToolset.Sdk][sdk], install the latest version with:

```
dotnet tool install --global wix
```

Verify it was successfully installed with:

```
wix --version
```

For more information, see https://docs.firegiant.com/wix/using-wix/#command-line-net-tool. To read about available commands and switches, see https://docs.firegiant.com/wix/tools/wixexe/.


## Additional resources

* [WiX Website][web]
* [WiX Documentation][docs]
* [WiX Issue Tracker][issues]
* [WiX Discussions][discussions]


[web]: https://www.firegiant.com/wixtoolset/
[docs]: https://docs.firegiant.com/wixtoolset/
[issues]: https://github.com/wixtoolset/issues/issues
[discussions]: https://github.com/orgs/wixtoolset/discussions
[sdk]: https://www.nuget.org/packages/WixToolset.Sdk/
[osmf]: https://opensourcemaintenancefee.org/
[license]: https://github.com/wixtoolset/wix/blob/main/LICENSE.TXT
[eula]: https://github.com/wixtoolset/wix/blob/main/OSMFEULA.txt
