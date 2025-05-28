# WixToolset.Sdk

The `WixToolset.Sdk` package provides the WiX Toolset as an MSBuild SDK for both .NET (v6 or later) and .NET Framework (v4.7.2 or later). SDK-style projects have smart defaults that make for simple .wixproj project authoring.

[Web Site][web] | [Documentation][docs] | [Issue Tracker][issues] | [Discussions][discussions]


## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate revenue must pay an [Open Source Maintenance Fee][osmf]. While the source code is freely available under the terms of the [LICENSE][license], this package and other aspects of the project require [adherence to the Open Source Maintenance Fee EULA][eula].

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/wixtoolset).


## Getting started

Here's a minimal .wixproj that builds an MSI from the .wxs source files in the project directory:

Example project file: `QuickStart.wixproj`
```
<Project Sdk="WixToolset.Sdk/6.0.1">
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

For more information about WiX as an MSBuild SDK, see https://docs.firegiant.com/wix/using-wix/#msbuild-and-dotnet-build.

For more information about WiX targets, properties, and items, see https://docs.firegiant.com/wix/tools/msbuild/.


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
