// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Decompiler of the WiX toolset.
    /// </summary>
    internal class Decompiler : IDecompiler
    {
        internal Decompiler(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public BindResult Decompile(IDecompileContext context)
        {
            // Pre-decompile.
            //
            foreach (var extension in context.Extensions)
            {
                extension.PreDecompile(context);
            }

            // Decompile.
            //
            var bindResult = this.BackendDecompile(context);

            if (bindResult != null)
            {
                // Post-decompile.
                //
                foreach (var extension in context.Extensions)
                {
                    extension.PostDecompile(bindResult);
                }
            }

            return bindResult;
        }

        private BindResult BackendDecompile(IDecompileContext context)
        {
            var extensionManager = context.ServiceProvider.GetService<IExtensionManager>();

            var backendFactories = extensionManager.Create<IBackendFactory>();

            foreach (var factory in backendFactories)
            {
                if (factory.TryCreateBackend(context.DecompileType.ToString(), context.OutputPath, out var backend))
                {
                    var result = backend.Decompile(context);
                    return result;
                }
            }

            // TODO: messaging that a backend could not be found to decompile the decompile type?

            return null;
        }
    }
}
