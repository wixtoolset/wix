# WixToolset.PowerShell.wixext - PowerShell WiX Toolset Extension

This WiX Extension provides support for configuring PowerShell.

[Web Site][web] | [Documentation][docs] | [Issue Tracker][issues] | [Discussions][discussions]


## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate revenue must pay an [Open Source Maintenance Fee][osmf]. While the source code is freely available under the terms of the [LICENSE][license], this package and other aspects of the project require [adherence to the Open Source Maintenance Fee EULA][eula].

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/wixtoolset).


## Getting started

Add the WiX Extension as a PackageReference to your .wixproj:

```
<Project Sdk="WixToolset.Sdk/6.0.0">
  <ItemGroup>
    <PackageReference Include="WixToolset.PowerShell.wixext" Version="6.0.0" />
  </ItemGroup>
</Project>
```

Then add the extension's namespace:

```
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ps="http://wixtoolset.org/schemas/v4/wxs/powershell">

  ..
</Wix>
```

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
