// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using WixToolset.Mba.Core;

[assembly: AssemblyTitle("Example.FullFramework2MBA")]
[assembly: AssemblyDescription("Example.FullFramework2MBA")]
[assembly: AssemblyProduct("WiX Toolset")]
[assembly: AssemblyCompany("WiX Toolset Team")]
[assembly: AssemblyCopyright("Copyright (c) .NET Foundation and contributors. All rights reserved.")]

// Types should not be visible to COM by default.
[assembly: ComVisible(false)]
[assembly: Guid("7A671EAF-FAE5-41A2-83DD-C35AB3779651")]

[assembly: BootstrapperApplicationFactory(typeof(Example.FullFramework2MBA.FullFramework2BAFactory))]
