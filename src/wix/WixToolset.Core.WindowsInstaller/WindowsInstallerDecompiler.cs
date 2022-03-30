// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Core.WindowsInstaller.Decompile;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Decompiler of the WiX toolset.
    /// </summary>
    internal class WindowsInstallerDecompiler : IWindowsInstallerDecompiler
    {
        internal WindowsInstallerDecompiler(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public IWindowsInstallerDecompileResult Decompile(IWindowsInstallerDecompileContext context)
        {
            if (context.SymbolDefinitionCreator == null)
            {
                context.SymbolDefinitionCreator = this.ServiceProvider.GetService<ISymbolDefinitionCreator>();
            }

            // Pre-decompile.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreDecompile(context);
            }

            // Decompile.
            //
            var result = this.Execute(context);

            if (result != null)
            {
                // Post-decompile.
                //
                foreach (var extension in context.Extensions)
                {
                    extension.PostDecompile(result);
                }
            }

            return result;
        }

        private IWindowsInstallerDecompileResult Execute(IWindowsInstallerDecompileContext context)
        {
            var result = context.ServiceProvider.GetService<IWindowsInstallerDecompileResult>();

            try
            {
                using (var database = new Database(context.DecompilePath, OpenDatabase.ReadOnly))
                {
                    // Delete the directory and its files to prevent cab extraction failure due to an existing file.
                    if (Directory.Exists(context.ExtractFolder))
                    {
                        Directory.Delete(context.ExtractFolder, true);
                    }

                    var backendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();
                    var decompilerHelper = context.ServiceProvider.GetService<IWindowsInstallerDecompilerHelper>();

                    var unbindCommand = new UnbindDatabaseCommand(this.Messaging, backendHelper, database, context.DecompilePath, context.DecompileType, context.ExtractFolder, context.IntermediateFolder, context.IsAdminImage, suppressDemodularization: false, skipSummaryInfo: false);
                    var output = unbindCommand.Execute();
                    var extractedFilePaths = new List<string>(unbindCommand.ExportedFiles);

                    var decompiler = new Decompiler(this.Messaging, backendHelper, decompilerHelper, context.Extensions, context.ExtensionData, context.SymbolDefinitionCreator, context.BaseSourcePath, context.SuppressCustomTables, context.SuppressDroppingEmptyTables, context.SuppressRelativeActionSequencing, context.SuppressUI, context.TreatProductAsModule);
                    result.Document = decompiler.Decompile(output);

                    result.Platform = GetPlatformFromOutput(output);

                    // extract the files from the cabinets
                    if (!String.IsNullOrEmpty(context.ExtractFolder) && !context.SuppressExtractCabinets)
                    {
                        var fileDirectory = String.IsNullOrEmpty(context.CabinetExtractFolder) ? Path.Combine(context.ExtractFolder, "File") : context.CabinetExtractFolder;

                        var extractCommand = new ExtractCabinetsCommand(output, database, context.DecompilePath, fileDirectory, context.IntermediateFolder, context.TreatProductAsModule);
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
                    throw new WixException(ErrorMessages.OpenDatabaseFailed(context.DecompilePath));
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
