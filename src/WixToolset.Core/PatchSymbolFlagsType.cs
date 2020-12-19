// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;

    /// <summary>
    /// The following flags are used with PATCH_OPTION_DATA SymbolOptionFlags:
    /// </summary>
    [Flags]
    public enum PatchSymbolFlagsType : uint
    {
        /// <summary>
        /// don't use imagehlp.dll
        /// </summary>
        PATCH_SYMBOL_NO_IMAGEHLP = 0x00000001,
        /// <summary>
        /// don't fail patch due to imagehlp failures
        /// </summary>
        PATCH_SYMBOL_NO_FAILURES = 0x00000002,
        /// <summary>
        /// after matching decorated symbols, try to match remaining by undecorated names
        /// </summary>
        PATCH_SYMBOL_UNDECORATED_TOO = 0x00000004,
        /// <summary>
        /// (used internally)
        /// </summary>
        PATCH_SYMBOL_RESERVED1 = 0x80000000,
        /// <summary>
        /// 
        /// </summary>
        MaxValue = PATCH_SYMBOL_NO_IMAGEHLP | PATCH_SYMBOL_NO_FAILURES | PATCH_SYMBOL_UNDECORATED_TOO
    }
}
