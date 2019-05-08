// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Msi
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Wrapper class for managing MSI API database handles.
    /// </summary>
    internal sealed class Database : MsiHandle
    {
        private const int STG_E_LOCKVIOLATION = unchecked((int)0x80030021);

        /// <summary>
        /// Constructor that opens an MSI database.
        /// </summary>
        /// <param name="path">Path to the database to be opened.</param>
        /// <param name="type">Persist mode to use when opening the database.</param>
        public Database(string path, OpenDatabase type)
        {
            int error = MsiInterop.MsiOpenDatabase(path, new IntPtr((int)type), out var handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
            this.Handle = handle;
        }

        public void ApplyTransform(string transformFile)
        {
            // get the curret validation bits
            TransformErrorConditions conditions = TransformErrorConditions.None;
            using (SummaryInformation summaryInfo = new SummaryInformation(transformFile))
            {
                string value = summaryInfo.GetProperty((int)SummaryInformation.Transform.ValidationFlags);
                try
                {
                    int validationFlags = Int32.Parse(value, CultureInfo.InvariantCulture);
                    conditions = (TransformErrorConditions)(validationFlags & 0xffff);
                }
                catch (FormatException)
                {
                    // fallback to default of None
                }
            }

            this.ApplyTransform(transformFile, conditions);
        }

        /// <summary>
        /// Applies a transform to this database.
        /// </summary>
        /// <param name="transformFile">Path to the transform file being applied.</param>
        /// <param name="errorConditions">Specifies the error conditions that are to be suppressed.</param>
        public void ApplyTransform(string transformFile, TransformErrorConditions errorConditions)
        {
            int error = MsiInterop.MsiDatabaseApplyTransform(this.Handle, transformFile, errorConditions);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Commits changes made to the database.
        /// </summary>
        public void Commit()
        {
            // Retry this call 3 times to deal with an MSI internal locking problem.
            const int retryWait = 300;
            const int retryLimit = 3;
            int error = 0;

            for (int i = 1; i <= retryLimit; ++i)
            {
                error = MsiInterop.MsiDatabaseCommit(this.Handle);

                if (0 == error)
                {
                    return;
                }
                else
                {
                    MsiException exception = new MsiException(error);

                    // We need to see if the error code is contained in any of the strings in ErrorInfo.
                    // Join the array together and search for the error code to cover the string array.
                    if (!String.Join(", ", exception.ErrorInfo).Contains(STG_E_LOCKVIOLATION.ToString()))
                    {
                        break;
                    }

                    Console.Error.WriteLine(String.Format("Failed to create the database. Info: {0}. Retrying ({1} of {2})", String.Join(", ", exception.ErrorInfo), i, retryLimit));
                    Thread.Sleep(retryWait);
                }
            }

            throw new MsiException(error);
        }

        /// <summary>
        /// Creates and populates the summary information stream of an existing transform file.
        /// </summary>
        /// <param name="referenceDatabase">Required database that does not include the changes.</param>
        /// <param name="transformFile">The name of the generated transform file.</param>
        /// <param name="errorConditions">Required error conditions that should be suppressed when the transform is applied.</param>
        /// <param name="validations">Required when the transform is applied to a database;
        /// shows which properties should be validated to verify that this transform can be applied to the database.</param>
        public void CreateTransformSummaryInfo(Database referenceDatabase, string transformFile, TransformErrorConditions errorConditions, TransformValidations validations)
        {
            int error = MsiInterop.MsiCreateTransformSummaryInfo(this.Handle, referenceDatabase.Handle, transformFile, errorConditions, validations);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Imports an installer text archive table (idt file) into an open database.
        /// </summary>
        /// <param name="idtPath">Specifies the path to the file to import.</param>
        /// <exception cref="WixInvalidIdtException">Attempted to import an IDT file with an invalid format or unsupported data.</exception>
        /// <exception cref="MsiException">Another error occured while importing the IDT file.</exception>
        public void Import(string idtPath)
        {
            string folderPath = Path.GetFullPath(Path.GetDirectoryName(idtPath));
            string fileName = Path.GetFileName(idtPath);

            int error = MsiInterop.MsiDatabaseImport(this.Handle, folderPath, fileName);
            if (1627 == error) // ERROR_FUNCTION_FAILED
            {
                throw new WixInvalidIdtException(idtPath);
            }
            else if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Exports an installer table from an open database to a text archive file (idt file).
        /// </summary>
        /// <param name="tableName">Specifies the name of the table to export.</param>
        /// <param name="folderPath">Specifies the name of the folder that contains archive files. If null or empty string, uses current directory.</param>
        /// <param name="fileName">Specifies the name of the exported table archive file.</param>
        public void Export(string tableName, string folderPath, string fileName)
        {
            if (String.IsNullOrEmpty(folderPath))
            {
                folderPath = Environment.CurrentDirectory;
            }

            int error = MsiInterop.MsiDatabaseExport(this.Handle, tableName, folderPath, fileName);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Creates a transform that, when applied to the reference database, results in this database.
        /// </summary>
        /// <param name="referenceDatabase">Required database that does not include the changes.</param>
        /// <param name="transformFile">The name of the generated transform file. This is optional.</param>
        /// <returns>true if a transform is generated; false if a transform is not generated because
        /// there are no differences between the two databases.</returns>
        public bool GenerateTransform(Database referenceDatabase, string transformFile)
        {
            int error = MsiInterop.MsiDatabaseGenerateTransform(this.Handle, referenceDatabase.Handle, transformFile, 0, 0);
            if (0 != error && 0xE8 != error) // ERROR_NO_DATA(0xE8) means no differences were found
            {
                throw new MsiException(error);
            }

            return (0xE8 != error);
        }

        /// <summary>
        /// Merges two databases together.
        /// </summary>
        /// <param name="mergeDatabase">The database to merge into the base database.</param>
        /// <param name="tableName">The name of the table to receive merge conflict information.</param>
        public void Merge(Database mergeDatabase, string tableName)
        {
            int error = MsiInterop.MsiDatabaseMerge(this.Handle, mergeDatabase.Handle, tableName);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Prepares a database query and creates a <see cref="View">View</see> object.
        /// </summary>
        /// <param name="query">Specifies a SQL query string for querying the database.</param>
        /// <returns>A view object is returned if the query was successful.</returns>
        public View OpenView(string query)
        {
            return new View(this, query);
        }

        /// <summary>
        /// Prepares and executes a database query and creates a <see cref="View">View</see> object.
        /// </summary>
        /// <param name="query">Specifies a SQL query string for querying the database.</param>
        /// <returns>A view object is returned if the query was successful.</returns>
        public View OpenExecuteView(string query)
        {
            View view = new View(this, query);

            view.Execute();
            return view;
        }

        /// <summary>
        /// Verifies the existence or absence of a table.
        /// </summary>
        /// <param name="tableName">Table name to to verify the existence of.</param>
        /// <returns>Returns true if the table exists, false if it does not.</returns>
        public bool TableExists(string tableName)
        {
            int result = MsiInterop.MsiDatabaseIsTablePersistent(this.Handle, tableName);
            return MsiInterop.MSICONDITIONTRUE == result;
        }

        /// <summary>
        /// Returns a <see cref="Record">Record</see> containing the names of all the primary 
        /// key columns for a specified table.
        /// </summary>
        /// <param name="tableName">Specifies the name of the table from which to obtain 
        /// primary key names.</param>
        /// <returns>Returns a <see cref="Record">Record</see> containing the names of all the 
        /// primary key columns for a specified table.</returns>
        public Record PrimaryKeys(string tableName)
        {
            var error = MsiInterop.MsiDatabaseGetPrimaryKeys(this.Handle, tableName, out var recordHandle);
            if (error != 0)
            {
                throw new MsiException(error);
            }

            return new Record(recordHandle);
        }
    }
}
