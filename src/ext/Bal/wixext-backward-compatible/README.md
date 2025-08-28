# WixToolset.Bal.wixext - Bootstrapper Applications WiX Toolset Extension

This WiX Extension was renamed. Use `WixToolset.BootstrapperApplications.wixext` instead.

- [WixStandardBootstrapperApplication](https://www.nuget.org/packages/WixToolset.BootstrapperApplications.wixext)

[Web Site][web] | [Documentation][docs] | [Issue Tracker][issues] | [Discussions][discussions]

## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate revenue must pay an [Open Source Maintenance Fee][osmf]. While the source code is freely available under the terms of the [LICENSE][license], this package and other aspects of the project require [adherence to the Open Source Maintenance Fee EULA][eula].

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/wixtoolset).


## Getting started

Add the WiX Extension as a PackageReference to your .wixproj:

```
<Project Sdk="WixToolset.Sdk/6.0.2">
  <ItemGroup>
    <PackageReference Include="WixToolset.BootstrapperApplications.wixext" Version="6.0.2" />
  </ItemGroup>
</Project>
```

Then add the namespace and bootstrapper application of choice:

```
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:bal="http://wixtoolset.org/schemas/v4/wxs/bal">
  <Bundle BundleId="AcmeCorp.Example" Name="Example Bundle" Version="0.0.0.1" Manufacturer="ACME Corporation">
    <BootstrapperApplication>
      <bal:WixStandardBootstrapperApplication LicenseUrl="http://wixtoolset.org/about/license/" Theme="hyperlinkLicense" />
    </BootstrapperApplication>
  </Bundle>
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
