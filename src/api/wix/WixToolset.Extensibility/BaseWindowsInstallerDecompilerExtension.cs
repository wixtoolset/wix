// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Base class for creating a windows installer decompiler extensions.
    /// </summary>
    public abstract class BaseWindowsInstallerDecompilerExtension : IWindowsInstallerDecompilerExtension
    {
        /// <summary>
        /// Context for use by the extension.
        /// </summary>
        protected IWindowsInstallerDecompileContext Context { get; private set; }

        /// <summary>
        /// Messaging for use by the extension.
        /// </summary>
        protected IMessaging Messaging { get; private set; }

        /// <summary>
        /// Decompiler helper for use by the extension.
        /// </summary>
        protected IWindowsInstallerDecompilerHelper DecompilerHelper { get; private set; }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.TableDefinitions"/>
        /// </summary>
        public virtual IReadOnlyCollection<TableDefinition> TableDefinitions { get; }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.PostDecompile(IWindowsInstallerDecompileResult)"/>
        /// </summary>
        public virtual void PreDecompile(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper helper)
        {
            this.Context = context;

            this.DecompilerHelper = helper;

            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.PreDecompileTables(TableIndexedCollection)"/>
        /// </summary>
        public virtual void PreDecompileTables(TableIndexedCollection tables)
        {
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.TryDecompileTable(Table)"/>
        /// </summary>
        public virtual bool TryDecompileTable(Table table)
        {
            return false;
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.PostDecompileTables(TableIndexedCollection)"/>
        /// </summary>
        public virtual void PostDecompileTables(TableIndexedCollection tables)
        {
        }

        /// <summary>
        /// See <see cref="IWindowsInstallerDecompilerExtension.PostDecompile(IWindowsInstallerDecompileResult)"/>
        /// </summary>
        public virtual void PostDecompile(IWindowsInstallerDecompileResult result)
        {
        }
    }
}
