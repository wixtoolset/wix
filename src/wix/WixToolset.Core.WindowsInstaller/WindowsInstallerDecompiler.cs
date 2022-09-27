// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Decompile;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
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
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IExtensionManager ExtensionManager { get; }

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
            // Delete the directory and its files to prevent cab extraction failure due to an existing file.
            if (!String.IsNullOrEmpty(context.ExtractFolder) && Directory.Exists(context.ExtractFolder))
            {
                Directory.Delete(context.ExtractFolder, true);
            }

            var backendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();

            var pathResolver = context.ServiceProvider.GetService<IPathResolver>();

            if (context.DecompileType == OutputType.Transform)
            {
                return this.DecompileTransform(context, backendHelper, pathResolver);
            }
            else
            {
                return this.DecompileDatabase(context, backendHelper, pathResolver);
            }
        }

        private IWindowsInstallerDecompileResult DecompileDatabase(IWindowsInstallerDecompileContext context, IWindowsInstallerBackendHelper backendHelper, IPathResolver pathResolver)
        {
            var extractFilesFolder = context.SuppressExtractCabinets || (String.IsNullOrEmpty(context.CabinetExtractFolder) && String.IsNullOrEmpty(context.ExtractFolder)) ? null :
                String.IsNullOrEmpty(context.CabinetExtractFolder) ? Path.Combine(context.ExtractFolder, "File") : context.CabinetExtractFolder;

            var outputType = context.TreatProductAsModule ? OutputType.Module : context.DecompileType;
            var unbindCommand = new UnbindDatabaseCommand(this.Messaging, backendHelper, pathResolver, context.DecompilePath, null, outputType, context.ExtractFolder, extractFilesFolder, context.IntermediateFolder, enableDemodularization: true, skipSummaryInfo: false);
            var output = unbindCommand.Execute();
            var extractedFilePaths = unbindCommand.ExportedFiles;

            var decompilerHelper = context.ServiceProvider.GetService<IWindowsInstallerDecompilerHelper>();
            var decompiler = new Decompiler(this.Messaging, backendHelper, decompilerHelper, context.Extensions, context.ExtensionData, context.SymbolDefinitionCreator, context.BaseSourcePath, context.SuppressCustomTables, context.SuppressDroppingEmptyTables, context.SuppressRelativeActionSequencing, context.SuppressUI, context.TreatProductAsModule);
            var document = decompiler.Decompile(output);

            var result = context.ServiceProvider.GetService<IWindowsInstallerDecompileResult>();
            result.Data = output;
            result.Document = document;
            result.Platform = GetPlatformFromOutput(output);
            result.ExtractedFilePaths = extractedFilePaths.ToList();
            return result;
        }

        private IWindowsInstallerDecompileResult DecompileTransform(IWindowsInstallerDecompileContext context, IWindowsInstallerBackendHelper backendHelper, IPathResolver pathResolver)
        {
            var fileSystemExtensions = this.ExtensionManager.GetServices<IFileSystemExtension>();

            var fileSystemManager = new FileSystemManager(fileSystemExtensions);

            var unbindCommand = new UnbindTransformCommand(this.Messaging, backendHelper, pathResolver, fileSystemManager, context.DecompilePath, context.ExtractFolder, context.IntermediateFolder);
            var output = unbindCommand.Execute();

            var result = context.ServiceProvider.GetService<IWindowsInstallerDecompileResult>();
            result.Data = output;
            return result;
        }

        private static Platform? GetPlatformFromOutput(WindowsInstallerData output)
        {
            var template = output.Tables["_SummaryInformation"]?.Rows.SingleOrDefault(row => row.FieldAsInteger(0) == 7)?.FieldAsString(1);

            return Decompiler.GetPlatformFromTemplateSummaryInformation(template?.Split(';'));
        }
    }
}
