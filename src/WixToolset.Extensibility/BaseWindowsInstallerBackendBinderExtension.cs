// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
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
        /// Optional table definitions to automatically map to tuples.
        /// </summary>
        protected virtual TableDefinition[] TableDefinitionsForTuples { get; }

        public virtual void PreBackendBind(IBindContext context)
        {
            this.Context = context;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();

            this.BackendHelper = context.ServiceProvider.GetService<IWindowsInstallerBackendHelper>();
        }

        public virtual ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<BindFileWithPath> files)
        {
            return null;
        }

        public virtual string ResolveMedia(MediaTuple mediaRow, string mediaLayoutDirectory, string layoutDirectory)
        {
            return null;
        }

        public virtual bool TryAddTupleToOutput(IntermediateTuple tuple, Output output)
        {
            if (this.TableDefinitionsForTuples != null)
            {
                return this.BackendHelper.TryAddTupleToOutputMatchingTableDefinitions(tuple, output, this.TableDefinitionsForTuples);
            }

            return false;
        }

        public virtual void PostBackendBind(BindResult result, Pdb pdb)
        {
        }
    }
}
