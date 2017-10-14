// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;

    //
    // The following flags are used with PATCH_OPTION_DATA SymbolOptionFlags:
    //
    [Flags]
    public enum PatchSymbolFlagsType : uint
    {
        PATCH_SYMBOL_NO_IMAGEHLP = 0x00000001, // don't use imagehlp.dll
        PATCH_SYMBOL_NO_FAILURES = 0x00000002, // don't fail patch due to imagehlp failures
        PATCH_SYMBOL_UNDECORATED_TOO = 0x00000004, // after matching decorated symbols, try to match remaining by undecorated names
        PATCH_SYMBOL_RESERVED1 = 0x80000000, // (used internally)
        MaxValue = PATCH_SYMBOL_NO_IMAGEHLP | PATCH_SYMBOL_NO_FAILURES | PATCH_SYMBOL_UNDECORATED_TOO
    }
}
