// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller.Rows
{
    using System.Diagnostics;

    /// <summary>
    /// Specialization of a row for the file table.
    /// </summary>
    public sealed class FileRow : Row
    {
        /// <summary>
        /// Creates a File row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this File row belongs to and should get its column definitions from.</param>
        public FileRow(SourceLineNumber sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Creates a File row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDefinition">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public FileRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
            : base(sourceLineNumbers, tableDefinition)
        {
        }

        /// <summary>
        /// Gets or sets the primary key of the file row.
        /// </summary>
        /// <value>Primary key of the file row.</value>
        public string File
        {
            get => this.FieldAsString(0);
            set => this.Fields[0].Data = value;
        }

        /// <summary>
        /// Gets or sets the component this file row belongs to.
        /// </summary>
        /// <value>Component this file row belongs to.</value>
        public string Component
        {
            get => this.FieldAsString(1);
            set => this.Fields[1].Data = value;
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>Name of the file.</value>
        public string FileName
        {
            get => this.FieldAsString(2);
            set => this.Fields[2].Data = value;
        }

        /// <summary>
        /// Gets or sets the real filesystem name of the file (without a pipe). This is typically the long name of the file.
        /// However, if no long name is available, falls back to the short name.
        /// </summary>
        /// <value>Long Name of the file - or if no long name is available, falls back to the short name.</value>
        public string LongFileName
        {
            get
            {
                var fileName = this.FileName;
                var index = fileName.IndexOf('|');

                // If it doesn't contain a pipe, just return the whole string
                // otherwise, extract the part of the string after the pipe.
                return (-1 == index) ? fileName : fileName.Substring(index + 1);
            }
        }

        /// <summary>
        /// Gets or sets the size of the file.
        /// </summary>
        /// <value>Size of the file.</value>
        public int FileSize
        {
            get => this.FieldAsInteger(3);
            set => this.Fields[3].Data = value;
        }

        /// <summary>
        /// Gets or sets the version of the file.
        /// </summary>
        /// <value>Version of the file.</value>
        public string Version
        {
            get => this.FieldAsString(4);
            set => this.Fields[4].Data = value;
        }

        /// <summary>
        /// Gets or sets the LCID of the file.
        /// </summary>
        /// <value>LCID of the file.</value>
        public string Language
        {
            get => this.FieldAsString(5);
            set => this.Fields[5].Data = value;
        }

        /// <summary>
        /// Gets or sets the attributes on a file.
        /// </summary>
        /// <value>Attributes on a file.</value>
        public int Attributes
        {
            get => this.FieldAsInteger(6);
            set => this.Fields[6].Data = value;
        }

        /// <summary>
        /// Gets or sets whether this file should be compressed.
        /// </summary>
        /// <value>Whether this file should be compressed.</value>
        public YesNoType Compressed
        {
            get
            {
                var compressedFlag = (0 < (this.Attributes & WindowsInstallerConstants.MsidbFileAttributesCompressed));
                var noncompressedFlag = (0 < (this.Attributes & WindowsInstallerConstants.MsidbFileAttributesNoncompressed));

                if (compressedFlag && noncompressedFlag)
                {
                    throw new WixException(ErrorMessages.IllegalFileCompressionAttributes(this.SourceLineNumbers));
                }
                else if (compressedFlag)
                {
                    return YesNoType.Yes;
                }
                else if (noncompressedFlag)
                {
                    return YesNoType.No;
                }
                else
                {
                    return YesNoType.NotSet;
                }
            }

            set
            {
                if (YesNoType.Yes == value)
                {
                    // these are mutually exclusive
                    this.Attributes |= WindowsInstallerConstants.MsidbFileAttributesCompressed;
                    this.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                }
                else if (YesNoType.No == value)
                {
                    // these are mutually exclusive
                    this.Attributes |= WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                    this.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesCompressed;
                }
                else // not specified
                {
                    Debug.Assert(YesNoType.NotSet == value);

                    // clear any compression bits
                    this.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesCompressed;
                    this.Attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                }
            }
        }

        /// <summary>
        /// Gets or sets the sequence of the file row.
        /// </summary>
        /// <value>Sequence of the file row.</value>
        public int Sequence
        {
            get => this.FieldAsInteger(7);
            set => this.Fields[7].Data = value;
        }

        /// <summary>
        /// Gets or sets the disk id for this file.
        /// </summary>
        /// <value>Disk id for the file.</value>
        public int DiskId
        {
            get => this.FieldAsInteger(8);
            set => this.Fields[8].Data = value;
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string Source
        {
            get => this.FieldAsString(9);
            set => this.Fields[9].Data = value;
        }

        /// <summary>
        /// Gets or sets the source location to the previous file.
        /// </summary>
        /// <value>Source location to the previous file.</value>
        public string PreviousSource
        {
            get => this.Fields[9].PreviousData;
            set => this.Fields[9].PreviousData = value;
        }
    }
}
