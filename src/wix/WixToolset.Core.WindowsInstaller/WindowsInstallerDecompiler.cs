// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
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

            var decompilerHelper = context.ServiceProvider.GetService<IWindowsInstallerDecompilerHelper>();

            // Pre-decompile.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreDecompile(context, decompilerHelper);
            }

            // Decompile.
            //
            var result = this.Execute(context, decompilerHelper);

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

        private IWindowsInstallerDecompileResult Execute(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper decompilerHelper)
        {
            // Delete the directory and its files to prevent cab extraction failure due to an existing file.
            if (!String.IsNullOrEmpty(context.ExtractFolder) && Directory.Exists(context.ExtractFolder))
            {
                Directory.Delete(context.ExtractFolder, true);
            }

            var backendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();

            var fileSystem = context.ServiceProvider.GetService<IFileSystem>();

            var pathResolver = context.ServiceProvider.GetService<IPathResolver>();

            if (context.DecompileType == OutputType.Transform)
            {
                return this.DecompileTransform(context, backendHelper, fileSystem, pathResolver);
            }
            else
            {
                return this.DecompileDatabase(context, decompilerHelper, backendHelper, fileSystem, pathResolver);
            }
        }

        private IWindowsInstallerDecompileResult DecompileDatabase(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper decompilerHelper, IWindowsInstallerBackendHelper backendHelper, IFileSystem fileSystem, IPathResolver pathResolver)
        {
            var extractFilesFolder = context.SuppressExtractCabinets || (String.IsNullOrEmpty(context.CabinetExtractFolder) && String.IsNullOrEmpty(context.ExtractFolder)) ? null :
                String.IsNullOrEmpty(context.CabinetExtractFolder) ? Path.Combine(context.ExtractFolder, "File") : context.CabinetExtractFolder;

            var demodularize = !context.KeepModularizationIds;
            var sectionType = context.DecompileType;
            var unbindCommand = new UnbindDatabaseCommand(this.Messaging, backendHelper, fileSystem, pathResolver, context.DecompilePath, null, sectionType, context.ExtractFolder, extractFilesFolder, context.IntermediateFolder, demodularize, skipSummaryInfo: false);
            var output = unbindCommand.Execute();
            var extractedFilePaths = unbindCommand.ExportedFiles;

            var decompiler = new Decompiler(this.Messaging, backendHelper, decompilerHelper, context.Extensions, context.ExtensionData, context.SymbolDefinitionCreator, context.BaseSourcePath, context.SuppressCustomTables, context.SuppressDroppingEmptyTables, context.SuppressRelativeActionSequencing, context.SuppressUI, context.KeepModularizationIds);
            var document = decompiler.Decompile(output);

            var result = context.ServiceProvider.GetService<IWindowsInstallerDecompileResult>();
            result.Data = output;
            result.Document = document;
            result.Platform = GetPlatformFromOutput(output);
            result.ExtractedFilePaths = extractedFilePaths.ToList();
            return result;
        }

        private IWindowsInstallerDecompileResult DecompileTransform(IWindowsInstallerDecompileContext context, IWindowsInstallerBackendHelper backendHelper, IFileSystem fileSystem, IPathResolver pathResolver)
        {
            var fileSystemExtensions = this.ExtensionManager.GetServices<IFileSystemExtension>();

            var fileSystemManager = new FileSystemManager(fileSystem, fileSystemExtensions);

            var unbindCommand = new UnbindTransformCommand(this.Messaging, backendHelper, fileSystem, pathResolver, fileSystemManager, context.DecompilePath, context.ExtractFolder, context.IntermediateFolder);
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
