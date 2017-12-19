// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Extensibility.Services;

    public interface IUnbindContext
    {
        IServiceProvider ServiceProvider { get; }

        IMessaging Messaging { get; set; }

        string ExportBasePath { get; set; }

        string InputFilePath { get; set; }

        string IntermediateFolder { get; set; }

        bool IsAdminImage { get; set; }

        bool SuppressDemodularization { get; set; }

        bool SuppressExtractCabinets { get; set; }
    }
}