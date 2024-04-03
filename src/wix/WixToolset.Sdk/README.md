# WixToolset.Sdk

The `WixToolset.Sdk` package provides the WiX Toolset as an MSBuild SDK for both .NET (v6 or later) and .NET Framework (v4.7.2 or later). SDK-style projects have smart defaults that make for simple .wixproj project authoring. For example, here's a minimal .wixproj that builds an MSI from the .wxs source files in the project directory:

```xml
<Project Sdk="WixToolset.Sdk/5.0.0">
</Project>
```

For more information about WiX as an MSBuild SDK, see https://wixtoolset.org/docs/intro/#msbuild.

For more information about WiX targets, properties, and items, see https://wixtoolset.org/docs/tools/msbuild/.
