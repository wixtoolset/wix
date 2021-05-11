// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Core.WindowsInstaller.Unbind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class CreatePatchTransformsCommand
    {
        public CreatePatchTransformsCommand(IMessaging messaging, IBackendHelper backendHelper, Intermediate intermediate, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.Intermediate = intermediate;
            this.IntermediateFolder = intermediateFolder;
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private Intermediate Intermediate { get; }

        private string IntermediateFolder { get; }

        public IEnumerable<PatchTransform> PatchTransforms { get; private set; }

        public IEnumerable<PatchTransform> Execute()
        {
            var patchTransforms = new List<PatchTransform>();

            var symbols = this.Intermediate.Sections.SelectMany(s => s.Symbols).OfType<WixPatchBaselineSymbol>();

            foreach (var symbol in symbols)
            {
                WindowsInstallerData transform;

                if (symbol.TransformFile is null)
                {
                    var baselineData = this.GetData(symbol.BaselineFile.Path);
                    var updateData = this.GetData(symbol.UpdateFile.Path);

                    var command = new GenerateTransformCommand(this.Messaging, baselineData, updateData, preserveUnchangedRows: true, showPedanticMessages: false);
                    transform = command.Execute();
                }
                else
                {
                    var exportBasePath = Path.Combine(this.IntermediateFolder, "_trans"); // TODO: come up with a better path.

                    var command = new UnbindTransformCommand(this.Messaging, this.BackendHelper, symbol.TransformFile.Path, exportBasePath, this.IntermediateFolder);
                    transform = command.Execute();
                }

                patchTransforms.Add(new PatchTransform(symbol.Id.Id, transform));
            }

            this.PatchTransforms = patchTransforms;

            return this.PatchTransforms;
        }

        private WindowsInstallerData GetData(string path)
        {
            var ext = Path.GetExtension(path);

            if (".msi".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                using (var database = new Database(path, OpenDatabase.ReadOnly))
                {
                    var exportBasePath = Path.Combine(this.IntermediateFolder, "_msi"); // TODO: come up with a better path.

                    var isAdminImage = false; // TODO: need a better way to set this

                    var command = new UnbindDatabaseCommand(this.Messaging, this.BackendHelper, database, path, OutputType.Product, exportBasePath, this.IntermediateFolder, isAdminImage, suppressDemodularization: true, skipSummaryInfo: true);
                    return command.Execute();
                }
            }
            else // assume .wixpdb (or .wixout)
            {
                var data = WindowsInstallerData.Load(path, true);
                return data;
            }
        }
    }
}
