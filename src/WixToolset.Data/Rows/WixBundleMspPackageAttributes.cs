// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    using System;

    [Flags]
    public enum WixBundleMspPackageAttributes
    {
        DisplayInternalUI = 0x1,
        Slipstream = 0x2,
        TargetUnspecified = 0x4,
    }
}
