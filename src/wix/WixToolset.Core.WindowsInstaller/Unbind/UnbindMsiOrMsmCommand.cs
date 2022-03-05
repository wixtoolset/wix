// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.ComponentModel;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class UnbindMsiOrMsmCommand
    {
        public UnbindMsiOrMsmCommand(IMessaging messaging, IBackendHelper backendHelper, string databasePath, string exportBasePath, string intermediateFolder, bool adminImage, bool suppressDemodularization, bool suppressExtractCabinets)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.DatabasePath = databasePath;
            this.ExportBasePath = exportBasePath;
            this.IntermediateFolder = intermediateFolder;
            this.IsAdminImage = adminImage;
            this.SuppressDemodularization = suppressDemodularization;
            this.SuppressExtractCabinets = suppressExtractCabinets;
        }

        public UnbindMsiOrMsmCommand(IUnbindContext context)
        {
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.DatabasePath = context.InputFilePath;
            this.ExportBasePath = context.ExportBasePath;
            this.IntermediateFolder = context.IntermediateFolder;
            this.IsAdminImage = context.IsAdminImage;
            this.SuppressDemodularization = context.SuppressDemodularization;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private string DatabasePath { get; }

        private string ExportBasePath { get; }

        private string IntermediateFolder { get; }

        private bool IsAdminImage { get; }

        private bool SuppressDemodularization { get; }

        private bool SuppressExtractCabinets { get; }

        public WindowsInstallerData Execute()
        {
            try
            {
                using (var database = new Database(this.DatabasePath, OpenDatabase.ReadOnly))
                {
                    var unbindCommand = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, database, this.DatabasePath, OutputType.Product, this.ExportBasePath, this.IntermediateFolder, this.IsAdminImage, this.SuppressDemodularization, skipSummaryInfo: false);
                    var data = unbindCommand.Execute();

                    // extract the files from the cabinets
                    if (!String.IsNullOrEmpty(this.ExportBasePath) && !this.SuppressExtractCabinets)
                    {
                        var extractCommand = new ExtractCabinetsCommand(data, database, this.DatabasePath, this.ExportBasePath, this.IntermediateFolder);
                        extractCommand.Execute();
                    }

                    return data;
                }
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    //throw new WixException(WixErrors.OpenDatabaseFailed(this.DatabasePath));
                }

                throw;
            }
        }
    }
}
