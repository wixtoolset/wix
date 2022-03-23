                        // Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Decompile
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class DecompileMsiOrMsmCommand
    {
        public DecompileMsiOrMsmCommand(IWindowsInstallerDecompileContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        private IWindowsInstallerDecompileContext Context { get; }

        private IMessaging Messaging { get; }

        public IWindowsInstallerDecompileResult Execute()
        {
            var result = this.Context.ServiceProvider.GetService<IWindowsInstallerDecompileResult>();

            try
            {
                using (var database = new Database(this.Context.DecompilePath, OpenDatabase.ReadOnly))
                {
                    // Delete the directory and its files to prevent cab extraction failure due to an existing file.
                    if (Directory.Exists(this.Context.ExtractFolder))
                    {
                        Directory.Delete(this.Context.ExtractFolder, true);
                    }

                    var backendHelper = this.Context.ServiceProvider.GetService<IBackendHelper>();

                    var unbindCommand = new UnbindDatabaseCommand(this.Messaging, backendHelper, database, this.Context.DecompilePath, this.Context.DecompileType, this.Context.ExtractFolder, this.Context.IntermediateFolder, this.Context.IsAdminImage, suppressDemodularization: false, skipSummaryInfo: false);
                    var output = unbindCommand.Execute();
                    var extractedFilePaths = new List<string>(unbindCommand.ExportedFiles);

                    var decompiler = new Decompiler(this.Messaging, backendHelper, this.Context.Extensions, this.Context.BaseSourcePath, this.Context.SuppressCustomTables, this.Context.SuppressDroppingEmptyTables, this.Context.SuppressRelativeActionSequencing, this.Context.SuppressUI, this.Context.TreatProductAsModule);
                    result.Document = decompiler.Decompile(output);

                    result.Platform = GetPlatformFromOutput(output);

                    // extract the files from the cabinets
                    if (!String.IsNullOrEmpty(this.Context.ExtractFolder) && !this.Context.SuppressExtractCabinets)
                    {
                        var fileDirectory = String.IsNullOrEmpty(this.Context.CabinetExtractFolder) ? Path.Combine(this.Context.ExtractFolder, "File") : this.Context.CabinetExtractFolder;

                        var extractCommand = new ExtractCabinetsCommand(output, database, this.Context.DecompilePath, fileDirectory, this.Context.IntermediateFolder, this.Context.TreatProductAsModule);
                        extractCommand.Execute();

                        extractedFilePaths.AddRange(extractCommand.ExtractedFiles);
                        result.ExtractedFilePaths = extractedFilePaths;
                    }
                    else
                    {
                        result.ExtractedFilePaths = new string[0];
                    }
                }
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    throw new WixException(ErrorMessages.OpenDatabaseFailed(this.Context.DecompilePath));
                }

                throw;
            }

            return result;
        }

        private static Platform? GetPlatformFromOutput(WindowsInstallerData output)
        {
            var template = output.Tables["_SummaryInformation"]?.Rows.SingleOrDefault(row => row.FieldAsInteger(0) == 7)?.FieldAsString(1);

            return Decompiler.GetPlatformFromTemplateSummaryInformation(template?.Split(';'));
        }
    }
}
