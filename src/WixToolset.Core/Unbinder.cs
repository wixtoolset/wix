// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Unbinder core of the WiX toolset.
    /// </summary>
    internal sealed class Unbinder : IUnbinder
    {
        public Unbinder(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();
            this.BackendFactories = extensionManager.GetServices<IBackendFactory>();
        }

        public IServiceProvider ServiceProvider { get; }

        public IEnumerable<IBackendFactory> BackendFactories { get; }

        /// <summary>
        /// Gets or sets whether the input msi is an admin image.
        /// </summary>
        /// <value>Set to true if the input msi is part of an admin image.</value>
        public bool IsAdminImage { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress demodularizing values.
        /// </summary>
        /// <value>The option to suppress demodularizing values.</value>
        public bool SuppressDemodularization { get; set; }

        /// <summary>
        /// Gets or sets the option to suppress extracting cabinets.
        /// </summary>
        /// <value>The option to suppress extracting cabinets.</value>
        public bool SuppressExtractCabinets { get; set; }

        /// <summary>
        /// Gets or sets the temporary path for the Binder.  If left null, the binder
        /// will use %TEMP% environment variable.
        /// </summary>
        /// <value>Path to temp files.</value>
        public string TempFilesLocation => Path.GetTempPath();

        /// <summary>
        /// Unbind a Windows Installer file.
        /// </summary>
        /// <param name="file">The Windows Installer file.</param>
        /// <param name="outputType">The type of output to create.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The output representing the database.</returns>
        public Intermediate Unbind(string file, OutputType outputType, string exportBasePath)
        {
            if (!File.Exists(file))
            {
                if (OutputType.Transform == outputType)
                {
                    throw new WixException(ErrorMessages.FileNotFound(null, file, "Transform"));
                }
                else
                {
                    throw new WixException(ErrorMessages.FileNotFound(null, file, "Database"));
                }
            }

            // if we don't have the temporary files object yet, get one
            Directory.CreateDirectory(this.TempFilesLocation); // ensure the base path is there

            var context = new UnbindContext(this.ServiceProvider);
            context.InputFilePath = file;
            context.ExportBasePath = exportBasePath;
            context.IntermediateFolder = this.TempFilesLocation;
            context.IsAdminImage = this.IsAdminImage;
            context.SuppressDemodularization = this.SuppressDemodularization;
            context.SuppressExtractCabinets = this.SuppressExtractCabinets;

            foreach (var factory in this.BackendFactories)
            {
                if (factory.TryCreateBackend(outputType.ToString(), file, out var backend))
                {
                    return backend.Unbind(context);
                }
            }

            // TODO: Display message that could not find a unbinder for output type?

            return null;
        }
    }
}
