// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a preprocessor extension.
    /// </summary>
    public abstract class BaseWindowsInstallerBackendBinderExtension : IWindowsInstallerBackendBinderExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IBindContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Backend helper for use by the extension.
        /// </summary>
        protected IWindowsInstallerBackendHelper BackendHelper { get; private set; }

        /// <summary>
        /// Optional table definitions.
        /// </summary>
        public virtual IEnumerable<TableDefinition> TableDefinitions => Enumerable.Empty<TableDefinition>();

        /// <summary>
        /// Creates a resolved cabinet result.
        /// </summary>
        protected IResolvedCabinet CreateResolvedCabinet() => this.Context.ServiceProvider.GetService<IResolvedCabinet>();

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.BackendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.FullyResolved(IntermediateSection)"/>
        /// </summary>
        public virtual void FullyResolved(IntermediateSection section)
        {
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual IResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<IBindFileWithPath> files) => null;

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual string ResolveMedia(MediaSymbol mediaRow, string mediaLayoutDirectory, string layoutDirectory) => null;

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual bool TryAddSymbolToOutput(IntermediateSection section, IntermediateSymbol symbol, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            if (this.TableDefinitions.Any(t => t.SymbolDefinition == symbol.Definition))
            {
                return this.BackendHelper.TryAddSymbolToOutputMatchingTableDefinitions(section, symbol, output, tableDefinitions);
            }

            return false;
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerBackendBinderExtension.PreBackendBind(IBindContext)"/>
        /// </summary>
        public virtual void PostBackendBind(IBindResult result)
        {
        }
    }
}
