// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;

#pragma warning disable 1591 // TODO: add documentation
    public interface IUnbindContext
    {
        IServiceProvider ServiceProvider { get; }

        string ExportBasePath { get; set; }

        string InputFilePath { get; set; }

        string IntermediateFolder { get; set; }

        bool IsAdminImage { get; set; }

        bool SuppressDemodularization { get; set; }

        bool SuppressExtractCabinets { get; set; }
    }
}