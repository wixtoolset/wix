// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System.IO;

    public interface IBootstrapperApplicationData
    {
        FileInfo BADataFile { get; }
        IBundleInfo Bundle { get; }
    }
}