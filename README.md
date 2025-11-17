![The WiX Toolset Logo](https://github.com/wixtoolset/.github/raw/master/profile/images/readme-header.png)

[![latest version](https://img.shields.io/nuget/vpre/wix)](https://www.nuget.org/packages/wix)
[![download count](https://img.shields.io/nuget/dt/wix)](https://www.nuget.org/stats/packages/WiX?groupby=Version)
[![build status](https://img.shields.io/github/actions/workflow/status/wixtoolset/wix/build.yml?branch=main)](https://github.com/wixtoolset/wix/actions/workflows/build.yml?query=branch%3Amain)

# WiX Toolset

The WiX Toolset is the most powerful set of tools available to create your Windows installation experience. This repository contains the WiX Toolset code itself.

If you're new to WiX, check out our [Quick Start](https://docs.firegiant.com/quick-start/) to build your first installation package in just a few minutes.


## Open Source Maintenance Fee

<a href="https://opensourcemaintenancefee.org/"><img src='https://github.com/wixtoolset/.github/blob/master/profile/images/osmf-logo-square-dark.png' height='146' align='right' /></a>

To ensure the long-term sustainability of this project, use of the WiX Toolset requires an [Open Source Maintenance Fee](https://opensourcemaintenancefee.org). While the source code is freely available under the terms of the [LICENSE](./LICENSE.TXT), all other aspects of the project--including opening or commenting on issues, participating in discussions and downloading releases--require [adherence to the Maintenance Fee](./OSMFEULA.txt).

In short, if you use this project to generate revenue, the [Maintenance Fee is required](./OSMFEULA.txt).

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/wixtoolset).


## Developing WiX

### Prerequisites

Before building the WiX Toolset, ensure you have Visual Studio 2026 (17.8.2 or higher) with the following installed:

| Workloads |
| :-------- |
| ASP.NET and web development |
| .NET desktop development |
| Desktop development with C++ |

| Individual components |
| :-------------------- |
| .NET 10.0 Runtime (Long Term Support) |
| .NET Framework 4.7.2 SDK |
| .NET Framework 4.7.2 targeting pack |
| .NET Framework 4.6.2 targeting pack |
| ATL v143 - VS 2026 C++ x64/x86 build tools (Latest) |
| MSVC v143 - VS 2026 C++ ARM64/ARM64EC build tools (Latest) |
| MSVC v143 - VS 2026 C++ x64/x86 build tools (Latest) |
| Git for Windows |

Also, download the latest [nuget.exe command-line tool](https://www.nuget.org/downloads) and place it in a directory on your path.

#### Getting started:

* [Fork the WiX repository](https://github.com/wixtoolset/wix/fork)
 into your own GitHub repository
* Clone the WiX repository from your fork (`git clone https://github.com/yourdomain/wix.git`)
 into the directory of your choice

#### To build the WiX toolset:

 * Start a VS2026 'Developer Command Prompt'
 * Change directory to the root of the cloned repository
 * Issue the command `devbuild` (or `devbuild release` if you want to create a release version)

#### Executing your newly built WiX toolset

 * `build\wix\Debug\publish\wix\wix --help` (Change `Debug` to `Release` if you built in release mode)

#### Pull request expectations

 * Pick an [outstanding WiX issue](https://github.com/wixtoolset/issues/issues?q=is%3Aissue+is%3Aopen+label%3A%22up+for+grabs%22) (or [create a new one](https://github.com/wixtoolset/issues/issues/new/choose)). Add a comment requesting that you be assigned to the issue. Wait for confirmation.
 * To create a pull request, [fork a new branch](https://github.com/wixtoolset/wix/fork) from the `main` branch
 * Make changes to effect whatever changed behavior is required for the pull request
 * Push the changes to your repository origin as needed
 * If the `main` branch has changed since you created your branch, rebase to the latest updates.
 * If needed (ie, you squashed or rebased), do a force push of your branch
 * Create a pull request with your branch against the WiX repository.

## Additional information

* Web site: https://www.firegiant.com/wixtoolset/
* Documentation [WiX Documentation](https://docs.firegiant.com/wixtoolset/)
* Issue Tracker: [GitHub Issues](https://github.com/wixtoolset/issues/issues)
* Discussions: [WiX Toolset Discussions](https://github.com/orgs/wixtoolset/discussions)
