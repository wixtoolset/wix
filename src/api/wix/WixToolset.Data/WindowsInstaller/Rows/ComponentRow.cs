// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller.Rows
{
    /// <summary>
    /// Specialization of a row for the Component table.
    /// </summary>
    public sealed class ComponentRow : Row
    {
        private string sourceFile;

        /// <summary>
        /// Creates a Control row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Component row belongs to and should get its column definitions from.</param>
        public ComponentRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public ComponentRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition) :
            base(sourceLineNumbers, tableDefinition)
        {
        }

        /// <summary>
        /// Gets or sets the identifier for this Component row.
        /// </summary>
        /// <value>Identifier for this Component row.</value>
        public string Component
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the ComponentId for this Component row.
        /// </summary>
        /// <value>guid for this Component row.</value>
        public string Guid
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Directory_ of the Component.
        /// </summary>
        /// <value>Directory of the Component.</value>
        public string Directory
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the local only attribute of the Component.
        /// </summary>
        /// <value>Local only attribute of the component.</value>
        public bool IsLocalOnly
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesLocalOnly == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesLocalOnly); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesLocalOnly;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesLocalOnly;
                }
            }
        }

        /// <summary>
        /// Gets or sets the source only attribute of the Component.
        /// </summary>
        /// <value>Source only attribute of the component.</value>
        public bool IsSourceOnly
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesSourceOnly == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesSourceOnly); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesSourceOnly;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesSourceOnly;
                }
            }
        }

        /// <summary>
        /// Gets or sets the optional attribute of the Component.
        /// </summary>
        /// <value>Optional attribute of the component.</value>
        public bool IsOptional
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesOptional == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesOptional); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesOptional;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesOptional;
                }
            }
        }

        /// <summary>
        /// Gets or sets the registry key path attribute of the Component.
        /// </summary>
        /// <value>Registry key path attribute of the component.</value>
        public bool IsRegistryKeyPath
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath;
                }
            }
        }

        /// <summary>
        /// Gets or sets the shared dll ref count attribute of the Component.
        /// </summary>
        /// <value>Shared dll ref countattribute of the component.</value>
        public bool IsSharedDll
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount;
                }
            }
        }

        /// <summary>
        /// Gets or sets the permanent attribute of the Component.
        /// </summary>
        /// <value>Permanent attribute of the component.</value>
        public bool IsPermanent
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesPermanent == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesPermanent); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesPermanent;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesPermanent;
                }
            }
        }

        /// <summary>
        /// Gets or sets the ODBC data source key path attribute of the Component.
        /// </summary>
        /// <value>ODBC data source key path attribute of the component.</value>
        public bool IsOdbcDataSourceKeyPath
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 64 bit attribute of the Component.
        /// </summary>
        /// <value>64-bitness of the component.</value>
        public bool Is64Bit
        {
            get { return WindowsInstallerConstants.MsidbComponentAttributes64bit == ((int)this.Fields[3].Data & WindowsInstallerConstants.MsidbComponentAttributes64bit); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | WindowsInstallerConstants.MsidbComponentAttributes64bit;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~WindowsInstallerConstants.MsidbComponentAttributes64bit;
                }
            }
        }

        /// <summary>
        /// Gets or sets the condition of the Component.
        /// </summary>
        /// <value>Condition of the Component.</value>
        public string Condition
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the key path of the Component.
        /// </summary>
        /// <value>Key path of the Component.</value>
        public string KeyPath
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file to fill in the Text of the control.
        /// </summary>
        /// <value>Source location to the file to fill in the Text of the control.</value>
        public string SourceFile
        {
            get { return this.sourceFile; }
            set { this.sourceFile = value; }
        }
    }
}
