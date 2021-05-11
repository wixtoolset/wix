// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Unbind
{
    using System;
    using System.ComponentModel;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Core.Native.Msi;

    internal class UnbindMsiOrMsmCommand
    {
        public UnbindMsiOrMsmCommand(IUnbindContext context)
        {
            this.Context = context;
        }

        public IUnbindContext Context { get; }

        public Intermediate Execute()
        {
#if TODO_PATCHING
            Output output;

            try
            {
                using (Database database = new Database(this.Context.InputFilePath, OpenDatabase.ReadOnly))
                {
                    var unbindCommand = new UnbindDatabaseCommand(this.Context.Messaging, database, this.Context.InputFilePath, OutputType.Product, this.Context.ExportBasePath, this.Context.IntermediateFolder, this.Context.IsAdminImage, this.Context.SuppressDemodularization, skipSummaryInfo: false);
                    output = unbindCommand.Execute();

                    // extract the files from the cabinets
                    if (!String.IsNullOrEmpty(this.Context.ExportBasePath) && !this.Context.SuppressExtractCabinets)
                    {
                        var extractCommand = new ExtractCabinetsCommand(output, database, this.Context.InputFilePath, this.Context.ExportBasePath, this.Context.IntermediateFolder);
                        extractCommand.Execute();
                    }
                }
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    throw new WixException(WixErrors.OpenDatabaseFailed(this.Context.InputFilePath));
                }

                throw;
            }

            return output;
#endif
            throw new NotImplementedException();
        }
    }
}
