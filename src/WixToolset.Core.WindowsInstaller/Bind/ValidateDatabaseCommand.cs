// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WixToolset.Core.Native;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class ValidateDatabaseCommand : IWindowsInstallerValidatorCallback
    {
        // Set of ICEs that have equivalent-or-better checks in WiX.
        private static readonly string[] WellKnownSuppressedIces = new[] { "ICE08", "ICE33", "ICE47", "ICE66" };

        public ValidateDatabaseCommand(IMessaging messaging, string intermediateFolder, WindowsInstallerData data, string outputPath, IEnumerable<string> cubeFiles, IEnumerable<string> ices, IEnumerable<string> suppressedIces)
        {
            this.Messaging = messaging;
            this.Data = data;
            this.OutputPath = outputPath;
            this.CubeFiles = cubeFiles;
            this.Ices = ices;
            this.SuppressedIces = suppressedIces == null ? WellKnownSuppressedIces : suppressedIces.Union(WellKnownSuppressedIces);

            this.IntermediateFolder = intermediateFolder;
            this.OutputSourceLineNumber = new SourceLineNumber(outputPath);
        }

        /// <summary>
        /// Encountered error implementation for <see cref="IWindowsInstallerValidatorCallback"/>.
        /// </summary>
        public bool EncounteredError => this.Messaging.EncounteredError;

        private IMessaging Messaging { get; }

        private WindowsInstallerData Data { get; }

        private string OutputPath { get; }

        private IEnumerable<string> CubeFiles { get; }

        private IEnumerable<string> Ices { get; }

        private IEnumerable<string> SuppressedIces { get; }

        private string IntermediateFolder { get; }

        /// <summary>
        /// Fallback when an exact source line number cannot be calculated for a validation error.
        /// </summary>
        private SourceLineNumber OutputSourceLineNumber { get; set; }

        private Dictionary<string, SourceLineNumber> SourceLineNumbersByTablePrimaryKey { get; set; }

        public void Execute()
        {
            var stopwatch = Stopwatch.StartNew();

            this.Messaging.Write(VerboseMessages.ValidatingDatabase());

            // Ensure the temporary files can be created the working folder.
            var workingFolder = Path.Combine(this.IntermediateFolder, "_validate");
            Directory.CreateDirectory(workingFolder);

            // Copy the database to a temporary location so it can be manipulated.
            // Ensure it is not read-only.
            var workingDatabasePath = Path.Combine(workingFolder, Path.GetFileName(this.OutputPath));
            FileSystem.CopyFile(this.OutputPath, workingDatabasePath, allowHardlink: false);

            var attributes = File.GetAttributes(workingDatabasePath);
            File.SetAttributes(workingDatabasePath, attributes & ~FileAttributes.ReadOnly);

            var validator = new WindowsInstallerValidator(this, workingDatabasePath, this.CubeFiles, this.Ices, this.SuppressedIces);
            validator.Execute();

            stopwatch.Stop();
            this.Messaging.Write(VerboseMessages.ValidatedDatabase(stopwatch.ElapsedMilliseconds));
        }

        private void LogValidationMessage(ValidationMessage message)
        {
            var messageSourceLineNumbers = this.OutputSourceLineNumber;
            if (!String.IsNullOrEmpty(message.Table) && !String.IsNullOrEmpty(message.Column) && message.PrimaryKeys != null)
            {
                messageSourceLineNumbers = this.GetSourceLineNumbers(message.Table, message.PrimaryKeys);
            }

            switch (message.Type)
            {
                case ValidationMessageType.InternalFailure:
                case ValidationMessageType.Error:
                    this.Messaging.Write(ErrorMessages.ValidationError(messageSourceLineNumbers, message.IceName, message.Description));
                    break;
                case ValidationMessageType.Warning:
                    this.Messaging.Write(WarningMessages.ValidationWarning(messageSourceLineNumbers, message.IceName, message.Description));
                    break;
                case ValidationMessageType.Info:
                    this.Messaging.Write(VerboseMessages.ValidationInfo(message.IceName, message.Description));
                    break;
                default:
                    throw new WixException(ErrorMessages.InvalidValidatorMessageType(message.Type.ToString()));
            }
        }

        /// <summary>
        /// Validation blocked by other installation operation for <see cref="IWindowsInstallerValidatorCallback"/>.
        /// </summary>
        public void ValidationBlocked()
        {
            this.Messaging.Write(VerboseMessages.ValidationSerialized());
        }

        /// <summary>
        /// Validation message implementation for <see cref="IWindowsInstallerValidatorCallback"/>.
        /// </summary>
        public bool ValidationMessage(ValidationMessage message)
        {
            this.LogValidationMessage(message);
            return true;
        }

        /// <summary>
        /// Gets the source line information (if available) for a row by its table name and primary key.
        /// </summary>
        /// <param name="tableName">The table name of the row.</param>
        /// <param name="primaryKeys">The primary keys of the row.</param>
        /// <returns>The source line number information if found; null otherwise.</returns>
        private SourceLineNumber GetSourceLineNumbers(string tableName, IEnumerable<string> primaryKeys)
        {
            // Source line information only exists if an output file was supplied
            if (this.Data == null)
            {
                // Use the file name as the source line information.
                return this.OutputSourceLineNumber;
            }

            // Index the source line information if it hasn't been indexed already.
            if (this.SourceLineNumbersByTablePrimaryKey == null)
            {
                this.SourceLineNumbersByTablePrimaryKey = new Dictionary<string, SourceLineNumber>();

                // Index each real table
                foreach (var table in this.Data.Tables.Where(t => !t.Definition.Unreal))
                {
                    // Index each row that contain source line information
                    foreach (var row in table.Rows.Where(r => r.SourceLineNumbers != null))
                    {
                        // Index the row using its table name and primary key
                        var primaryKey = row.GetPrimaryKey(';');

                        if (!String.IsNullOrEmpty(primaryKey))
                        {
                            try
                            {
                                var key = String.Concat(table.Name, ":", primaryKey);
                                this.SourceLineNumbersByTablePrimaryKey.Add(key, row.SourceLineNumbers);
                            }
                            catch (ArgumentException)
                            {
                                this.Messaging.Write(WarningMessages.DuplicatePrimaryKey(row.SourceLineNumbers, primaryKey, table.Name));
                            }
                        }
                    }
                }
            }

            return this.SourceLineNumbersByTablePrimaryKey.TryGetValue(String.Concat(tableName, ":", String.Join(";", primaryKeys)), out var sourceLineNumbers) ? sourceLineNumbers : null;
        }
    }
}
