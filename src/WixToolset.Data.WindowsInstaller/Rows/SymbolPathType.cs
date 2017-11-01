// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// The types that the WixDeltaPatchSymbolPaths table can hold.
    /// </summary>
    /// <remarks>The order of these values is important since WixDeltaPatchSymbolPaths are sorted by this type.</remarks>
    public enum SymbolPathType
    {
        File,
        Component,
        Directory,
        Media,
        Product
    };
}
