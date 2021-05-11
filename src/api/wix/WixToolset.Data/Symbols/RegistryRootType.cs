// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Symbols
{
    using System;

    /// <summary>
    /// Registry root mapping.
    /// </summary>
    public enum RegistryRootType
    {
        Unknown = Int32.MaxValue,

        /// <summary>HKLM in a per-machine and HKCU in per-user.</summary>
        MachineUser = -1,
        ClassesRoot = 0,
        CurrentUser = 1,
        LocalMachine = 2,
        Users = 3
    }
}
