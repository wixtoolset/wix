// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http.Symbols
{
    /// <summary>
    /// Must match constants in wixhttpca.cpp
    /// </summary>
    public enum HandleExisting
    {
        Replace = 0,
        Ignore = 1,
        Fail = 2,
    }
}
