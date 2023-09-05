// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Binder of the WiX toolset.
    /// </summary>
    internal class Binder : IBinder
    {
        internal Binder(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IBindResult Bind(IBindContext context)
        {
            // Prebind.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreBind(context);
            }

            // Bind.
            //
            this.WriteBuildInfoSymbol(context.IntermediateRepresentation, context.OutputPath, context.PdbPath);

            var bindResult = this.BackendBind(context);

            if (bindResult != null)
            {
                // Postbind.
                //
                foreach (var extension in context.Extensions)
                {
                    extension.PostBind(bindResult);
                }
            }

            return bindResult;
        }

        private IBindResult BackendBind(IBindContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendFactories = extensionManager.GetServices<IBackendFactory>();

            foreach (var factory in backendFactories)
            {
                if (factory.TryCreateBackend(context.OutputType, context.OutputPath, out var backend))
                {
                    var result = backend.Bind(context);
                    return result;
                }
            }

            var messaging = context.ServiceProvider.GetService<IMessaging>();

            messaging.Write(CoreErrors.BackendNotFound(context.OutputType, context.OutputPath));

            return null;
        }

        private void WriteBuildInfoSymbol(Intermediate output, string outputFile, string outputPdbPath)
        {
            var entrySection = output.Sections.First(s => s.Type != SectionType.Fragment);

            var executingAssembly = Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);

            var buildInfoSymbol = entrySection.AddSymbol(new WixBuildInfoSymbol()
            {
                WixVersion = fileVersion.FileVersion,
                WixOutputFile = outputFile,
            });

            if (!String.IsNullOrEmpty(outputPdbPath))
            {
                buildInfoSymbol.WixPdbFile = outputPdbPath;
            }
        }
    }
}
