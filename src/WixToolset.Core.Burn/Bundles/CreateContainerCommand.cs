// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Creates cabinet files.
    /// </summary>
    internal class CreateContainerCommand
    {
        public CreateContainerCommand(IEnumerable<WixBundlePayloadSymbol> payloads, string outputPath, CompressionLevel? compressionLevel)
        {
            this.Payloads = payloads;
            this.OutputPath = outputPath;
            this.CompressionLevel = compressionLevel;
        }

        public CreateContainerCommand(string manifestPath, IEnumerable<WixBundlePayloadSymbol> payloads, string outputPath, CompressionLevel? compressionLevel)
        {
            this.ManifestFile = manifestPath;
            this.Payloads = payloads;
            this.OutputPath = outputPath;
            this.CompressionLevel = compressionLevel;
        }

        private CompressionLevel? CompressionLevel { get; }

        private string ManifestFile { get; }

        private string OutputPath { get; }

        private IEnumerable<WixBundlePayloadSymbol> Payloads { get; }

        public string Hash { get; private set; }

        public long Size { get; private set; }

        public void Execute()
        {
            var payloadCount = this.Payloads.Count(); // The number of embedded payloads

            if (!String.IsNullOrEmpty(this.ManifestFile))
            {
                ++payloadCount;
            }

            var cabinetPath = Path.GetFullPath(this.OutputPath);

            var files = new List<CabinetCompressFile>();

            // If a manifest was provided always add it as "payload 0" to the container.
            if (!String.IsNullOrEmpty(this.ManifestFile))
            {
                files.Add(new CabinetCompressFile(this.ManifestFile, "0"));
            }

            files.AddRange(this.Payloads.Select(p => new CabinetCompressFile(p.SourceFile.Path, p.EmbeddedId)));

            var cab = new Cabinet(cabinetPath);
            cab.Compress(files, this.CompressionLevel ?? Data.CompressionLevel.Medium);

            // Now that the container is created, set the outputs of the command.
            var fileInfo = new FileInfo(cabinetPath);

            this.Hash = BundleHashAlgorithm.Hash(fileInfo);

            this.Size = fileInfo.Length;
        }
    }
}
