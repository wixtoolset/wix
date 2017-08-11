// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixFile table.
    /// </summary>
    public sealed class WixFileRow : Row
    {
        /// <summary>
        /// Creates a WixFile row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixFileRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixFile row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixFileRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the primary key of the file row.
        /// </summary>
        /// <value>Primary key of the file row.</value>
        public string File
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the assembly type of the file row.
        /// </summary>
        /// <value>Assembly type of the file row.</value>
        public FileAssemblyType AssemblyType
        {
            get { return (null == this.Fields[1]) ? FileAssemblyType.NotAnAssembly : (FileAssemblyType)this.Fields[1].AsInteger(); }
            set { this.Fields[1].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the identifier for the assembly manifest.
        /// </summary>
        /// <value>Identifier for the assembly manifest.</value>
        public string AssemblyManifest
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the application for the assembly.
        /// </summary>
        /// <value>Application for the assembly.</value>
        public string AssemblyApplication
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the directory of the file.
        /// </summary>
        /// <value>Directory of the file.</value>
        public string Directory
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the disk id for this file.
        /// </summary>
        /// <value>Disk id for the file.</value>
        public int DiskId
        {
            get { return (int)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string Source
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string PreviousSource
        {
            get { return (string)this.Fields[6].PreviousData; }
            set { this.Fields[6].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the architecture the file executes on.
        /// </summary>
        /// <value>Architecture the file executes on.</value>
        public string ProcessorArchitecture
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        /// <summary>
        /// Gets or sets the patch group of a patch-added file.
        /// </summary>
        /// <value>The patch group of a patch-added file.</value>
        public int PatchGroup
        {
            get { return (null == this.Fields[8].Data) ? 0 : (int)this.Fields[8].Data; }
            set { this.Fields[8].Data = value; }
        }

        /// <summary>
        /// Gets or sets the attributes on a file.
        /// </summary>
        /// <value>Attributes on a file.</value>
        public int Attributes
        {
            get { return (int)this.Fields[9].Data; }
            set { this.Fields[9].Data = value; }
        }

        /// <summary>
        /// Gets or sets the patching attributes to the file.
        /// </summary>
        /// <value>Patching attributes of the file.</value>
        public PatchAttributeType PatchAttributes
        {
            get { return (PatchAttributeType)this.Fields[10].AsInteger(); }
            set { this.Fields[10].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the path to the delta patch header.
        /// </summary>
        /// <value>Patch header path.</value>
        /// <remarks>Set by the binder only when doing delta patching.</remarks>
        public string DeltaPatchHeaderSource
        {
            get { return (string)this.Fields[11].Data; }
            set { this.Fields[11].Data = value; }
        }
    }
}
