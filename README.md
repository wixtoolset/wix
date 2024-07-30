<img src="https://github.com/wixtoolset/Home/raw/master/imgs/wix-white-bg.png" alt="WiX Toolset" height="128" />

[![latest version](https://img.shields.io/nuget/vpre/wix)](https://www.nuget.org/packages/wix)
[![download count](https://img.shields.io/nuget/dt/wix)](https://www.nuget.org/stats/packages/WiX?groupby=Version)
[![build status](https://img.shields.io/github/actions/workflow/status/wixtoolset/wix/build.yml?branch=main)](https://github.com/wixtoolset/wix/actions/workflows/build.yml?query=branch%3Amain)

# WiX Toolset

This repository contains the WiX Toolset codebase.

# Developing WiX

## Prerequisites

- A command line Git client that is in the system path
- Visual Studio 2022 (17.8.2 or higher) with the following installed:

| Workloads |
| :-------- |
| ASP.NET and web development |
| .NET desktop development |
| Desktop development with C++ |

| Individual components |
| :-------------------- |
| .NET 8.0 Runtime (Long Term Support) |
| .NET Framework 4.7.2 SDK |
| .NET Framework 4.7.2 targeting pack |
| .NET Framework 4.6.2 targeting pack |
| MSVC v143 - VS 2022 C++ ARM64/ARM64EC build tools (Latest) |
| MSVC v143 - VS 2022 C++ x64/x86 build tools (Latest) |
| Git for Windows |

- [Download the latest nuget.exe command-line tool](https://www.nuget.org/downloads) and put it in a directory on the path.

##### Getting started:

* [Fork the WiX repository](https://github.com/wixtoolset/wix/fork)
 into your own GitHub repository
* Clone the WiX repository from your fork (`git clone https://github.com/yourdomain/wix.git`)
 into the directory of your choice

##### To build the WiX toolset:

 * Start a VS2022 'Developer Command Prompt'
 * Change directory to the root of the cloned repository
 * Issue the command `devbuild` (or `devbuild release` if you want to create a release version)

 ##### Executing your newly built WiX toolset

 * `build\wix\Debug\publish\wix\wix --help` (Change `Debug` to `Release` if you built in release mode)

 ##### Pull request expectations

 * Pick an [outstanding WiX issue](https://github.com/wixtoolset/issues/issues?q=is%3Aissue+is%3Aopen+label%3A%22up+for+grabs%22) (or [create a new one](https://github.com/wixtoolset/issues/issues/new/choose)). Add a comment requesting that you be assigned to the issue. Wait for confirmation.
 * To create a pull request, [fork a new branch](https://github.com/wixtoolset/wix/fork) from the `main` branch
 * Make changes to effect whatever changed behavior is required for the pull request
 * Push the changes to your repository origin as needed
 * If the `main` branch has changed since you created your branch, rebase to the latest updates.
 * If needed (ie, you squashed or rebased), do a force push of your branch
 * Create a pull request with your branch against the WiX repository.
