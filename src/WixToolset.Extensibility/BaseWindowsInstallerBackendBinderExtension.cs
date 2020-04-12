// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
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

        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.BackendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();
        }

        public virtual IResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<IBindFileWithPath> files) => null;

        public virtual string ResolveMedia(MediaTuple mediaRow, string mediaLayoutDirectory, string layoutDirectory) => null;

        public virtual bool TryAddTupleToOutput(IntermediateSection section, IntermediateTuple tuple, WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            if (this.TableDefinitions.Any())
            {
                return this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(section, tuple, output, tableDefinitions);
            }

            return false;
        }

        public virtual void PostBackendBind(IBindResult result)
        {
        }
    }
}
