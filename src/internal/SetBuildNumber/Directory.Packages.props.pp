<Project>
  <ItemGroup>
    <PackageVersion Include="WixToolset.Dtf.Compression" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.Compression.Cab" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.CustomAction" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.Resources" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.WindowsInstaller" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.WindowsInstaller.Package" Version="{packageversion}" />

    <PackageVersion Include="WixInternal.TestSupport" Version="{packageversion}" />
    <PackageVersion Include="WixInternal.TestSupport.Native" Version="{packageversion}" />
    <PackageVersion Include="WixInternal.BaseBuildTasks.Sources" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.DUtil" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.WcaUtil" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.BootstrapperApplicationApi" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.BootstrapperExtensionApi" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.WixStandardBootstrapperApplicationFunctionApi" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Data" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Extensibility" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Versioning" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Burn" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Core" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Core.Burn" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Core.WindowsInstaller" Version="{packageversion}" />
    <PackageVersion Include="WixInternal.Core.TestPackage" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Heat" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Bal.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.BootstrapperApplications.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.ComPlus.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dependency.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.NetFx.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.UI.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Util.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Firewall.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Msmq.wixext" Version="{packageversion}" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="System.Configuration.ConfigurationManager" Version="6.0.1" />
    <PackageVersion Include="System.Diagnostics.PerformanceCounter" Version="4.7.0" />
    <PackageVersion Include="System.DirectoryServices" Version="4.7.0" />
    <PackageVersion Include="System.DirectoryServices.AccountManagement" Version="4.7.0" />
    <PackageVersion Include="System.Management" Version="4.7.0" />
    <PackageVersion Include="System.IO.Compression" Version="4.3.0" />
    <PackageVersion Include="System.IO.FileSystem.AccessControl" Version="4.7.0" />
    <PackageVersion Include="System.Net.NetworkInformation" Version="4.3.0" />
    <PackageVersion Include="System.Reflection.Metadata" Version="1.8.1" />
    <PackageVersion Include="System.Security.Principal.Windows" Version="4.7.0" />
    <PackageVersion Include="System.Text.Encoding.CodePages" Version="4.7.1" />
    <PackageVersion Include="System.Text.Json" Version="6.0.9" />

    <PackageVersion Include="Microsoft.AspNetCore.Owin" Version="3.1.13" />
    <PackageVersion Include="Microsoft.VisualStudio.Setup.Configuration.Native" Version="3.10.2154" />
    <PackageVersion Include="Microsoft.Win32.Registry" Version="4.7.0" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="NuGet.Credentials" Version="6.10.1" />
    <PackageVersion Include="NuGet.Protocol" Version="6.10.1" />
    <PackageVersion Include="NuGet.Versioning" Version="6.10.1" />
  </ItemGroup>

  <!--
    These MSBuild versions are trapped in antiquity for heat.exe.
  -->
  <ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <PackageVersion Include="Microsoft.Build.Tasks.Core" Version="14.3" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFrameworkIdentifier)' != '.NETFramework'">
    <PackageVersion Include="Microsoft.Build.Tasks.Core" Version="15.7.179" />
  </ItemGroup>

  <!-- Keep the following versions in sync with internal\WixInternal.TestSupport.Native\packages.config -->
  <ItemGroup>
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />

    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageVersion Include="xunit" Version="2.8.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.1" />
    <PackageVersion Include="xunit.assert" Version="2.8.1" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.NET.Tools.NETCoreCheck.x86" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Tools.NETCoreCheck.x64" Version="6.0.0" />
    <PackageVersion Include="Microsoft.NET.Tools.NETCoreCheck.arm64" Version="6.0.0" />
  </ItemGroup>
</Project>
