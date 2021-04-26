<Project>
  <PropertyGroup>
    <DtfBuildSuffix>-build.1</DtfBuildSuffix>
    <InternalBuildSuffix>-build.2</InternalBuildSuffix>
    <LibsBuildSuffix>-build.1</LibsBuildSuffix>
    <ApiBuildSuffix>-build.2</ApiBuildSuffix>
    <BurnBuildSuffix>-build.2</BurnBuildSuffix>
    <WixBuildSuffix>-build.3</WixBuildSuffix>
    <BalBuildSuffix>-build.2</BalBuildSuffix>
    <UtilBuildSuffix>-build.1</UtilBuildSuffix>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="WixToolset.Dtf.Compression" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.Compression.Cab" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.Resources" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dtf.WindowsInstaller" Version="{packageversion}" />

    <PackageVersion Include="WixBuildTools.TestSupport" Version="{packageversion}" />
    <PackageVersion Include="WixBuildTools.TestSupport.Native" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.DUtil" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.WcaUtil" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.BootstrapperCore.Native" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.BalUtil" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.BextUtil" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Mba.Core" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Data" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Extensibility" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Burn" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Core" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Core.Burn" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Core.WindowsInstaller" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Core.TestPackage" Version="{packageversion}" />

    <PackageVersion Include="WixToolset.Bal.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Dependency.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.NetFx.wixext" Version="{packageversion}" />
    <PackageVersion Include="WixToolset.Util.wixext" Version="{packageversion}" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="System.Diagnostics.PerformanceCounter" Version="4.7.0" />
    <PackageVersion Include="System.DirectoryServices" Version="4.7.0" />
    <PackageVersion Include="System.IO.FileSystem.AccessControl" Version="4.6.0" />
    <PackageVersion Include="System.IO.Compression" Version="4.3.0" />
    <PackageVersion Include="System.Reflection.Metadata" Version="1.6.0" />
    <PackageVersion Include="System.Security.Principal.Windows" Version="4.7.0" />
    <PackageVersion Include="System.Text.Encoding.CodePages" Version="4.6.0" />

    <PackageVersion Include="Microsoft.AspNetCore.Owin" Version="3.1.13" />
    <PackageVersion Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.0" />
    <PackageVersion Include="Microsoft.VisualStudio.Setup.Configuration.Native" Version="1.14.114" />
    <PackageVersion Include="Microsoft.Win32.Registry" Version="4.7.0" />

    <PackageVersion Include="NuGet.Credentials" Version="5.6.0" />
    <PackageVersion Include="NuGet.Protocol" Version="5.6.0" />
    <PackageVersion Include="NuGet.Versioning" Version="5.6.0" />
  </ItemGroup>



  <ItemGroup Condition="'$(TargetFramework)'=='net461' or '$(TargetFramework)'=='net472'" >
    <PackageVersion Include="Microsoft.Build.Tasks.Core" Version="14.3"/>
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)'=='netstandard2.0' or '$(TargetFramework)'=='netcoreapp3.1'">
    <PackageVersion Include="Microsoft.Build.Tasks.Core" Version="15.7.179" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="1.0.0" />
    <PackageVersion Include="GitInfo" Version="2.1.2" />
  </ItemGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="16.8.3" />
    <PackageVersion Include="xunit" Version="2.4.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.4.1" />
    <PackageVersion Include="xunit.assert" Version="2.4.1" />
  </ItemGroup>
</Project>
