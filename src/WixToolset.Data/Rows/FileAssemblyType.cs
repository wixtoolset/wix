// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Every file row has an assembly type.
    /// </summary>
    public enum FileAssemblyType
    {
        /// <summary>File is not an assembly.</summary>
        NotAnAssembly,

        /// <summary>File is a Common Language Runtime Assembly.</summary>
        DotNetAssembly,

        /// <summary>File is Win32 SxS assembly.</summary>
        Win32Assembly,
    }
}
