// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using WixToolset.Core.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Update file information.
    /// </summary>
    internal class MergeModulesCommand
    {
        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public IMessaging Messaging { private get; set; }

        public WindowsInstallerData Output { private get; set; }

        public string OutputPath { private get; set; }

        public IEnumerable<string> SuppressedTableNames { private get; set; }

        public string IntermediateFolder { private get; set; }

        public void Execute()
        {
            Table wixMergeTable = this.Output.Tables["WixMerge"];
            Table wixFeatureModulesTable = this.Output.Tables["WixFeatureModules"];

            // check for merge rows to see if there is any work to do
            if (null == wixMergeTable || 0 == wixMergeTable.Rows.Count)
            {
                return;
            }

            IMsmMerge2 merge = null;
            bool commit = true;
            bool logOpen = false;
            bool databaseOpen = false;
            string logPath = null;
            try
            {
                merge = MsmInterop.GetMsmMerge();

                logPath = Path.Combine(this.IntermediateFolder, "merge.log");
                merge.OpenLog(logPath);
                logOpen = true;

                merge.OpenDatabase(this.OutputPath);
                databaseOpen = true;

                // process all the merge rows
                foreach (WixMergeRow wixMergeRow in wixMergeTable.Rows)
                {
                    bool moduleOpen = false;

                    try
                    {
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            this.Messaging.Write(ErrorMessages.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        this.Messaging.Write(VerboseMessages.OpeningMergeModule(wixMergeRow.SourceFile, mergeLanguage));
                        merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                        moduleOpen = true;

                        // If there is merge configuration data, create a callback object to contain it all.
                        ConfigurationCallback callback = null;
                        if (!String.IsNullOrEmpty(wixMergeRow.ConfigurationData))
                        {
                            callback = new ConfigurationCallback(wixMergeRow.ConfigurationData);
                        }

                        // merge the module into the database that's being built
                        this.Messaging.Write(VerboseMessages.MergingMergeModule(wixMergeRow.SourceFile));
                        merge.MergeEx(wixMergeRow.Feature, wixMergeRow.Directory, callback);

                        // connect any non-primary features
                        if (null != wixFeatureModulesTable)
                        {
                            foreach (Row row in wixFeatureModulesTable.Rows)
                            {
                                if (wixMergeRow.Id == (string)row[1])
                                {
                                    this.Messaging.Write(VerboseMessages.ConnectingMergeModule(wixMergeRow.SourceFile, (string)row[0]));
                                    merge.Connect((string)row[0]);
                                }
                            }
                        }
                    }
                    catch (COMException)
                    {
                        commit = false;
                    }
                    finally
                    {
                        IMsmErrors mergeErrors = merge.Errors;

                        // display all the errors encountered during the merge operations for this module
                        for (int i = 1; i <= mergeErrors.Count; i++)
                        {
                            IMsmError mergeError = mergeErrors[i];
                            StringBuilder databaseKeys = new StringBuilder();
                            StringBuilder moduleKeys = new StringBuilder();

                            // build a string of the database keys
                            for (int j = 1; j <= mergeError.DatabaseKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    databaseKeys.Append(';');
                                }
                                databaseKeys.Append(mergeError.DatabaseKeys[j]);
                            }

                            // build a string of the module keys
                            for (int j = 1; j <= mergeError.ModuleKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    moduleKeys.Append(';');
                                }
                                moduleKeys.Append(mergeError.ModuleKeys[j]);
                            }

                            // display the merge error based on the msm error type
                            switch (mergeError.Type)
                            {
                                case MsmErrorType.msmErrorExclusion:
                                    this.Messaging.Write(ErrorMessages.MergeExcludedModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleKeys.ToString()));
                                    break;
                                case MsmErrorType.msmErrorFeatureRequired:
                                    this.Messaging.Write(ErrorMessages.MergeFeatureRequired(wixMergeRow.SourceLineNumbers, mergeError.ModuleTable, moduleKeys.ToString(), wixMergeRow.SourceFile, wixMergeRow.Id));
                                    break;
                                case MsmErrorType.msmErrorLanguageFailed:
                                    this.Messaging.Write(ErrorMessages.MergeLanguageFailed(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorLanguageUnsupported:
                                    this.Messaging.Write(ErrorMessages.MergeLanguageUnsupported(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorResequenceMerge:
                                    this.Messaging.Write(WarningMessages.MergeRescheduledAction(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorTableMerge:
                                    if ("_Validation" != mergeError.DatabaseTable) // ignore merge errors in the _Validation table
                                    {
                                        this.Messaging.Write(WarningMessages.MergeTableFailed(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    }
                                    break;
                                case MsmErrorType.msmErrorPlatformMismatch:
                                    this.Messaging.Write(ErrorMessages.MergePlatformMismatch(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, "Encountered an unexpected merge error of type '{0}' for which there is currently no error message to display.  More information about the merge and the failure can be found in the merge log: '{1}'", Enum.GetName(typeof(MsmErrorType), mergeError.Type), logPath), "InvalidOperationException", Environment.StackTrace));
                                    break;
                            }
                        }

                        if (0 >= mergeErrors.Count && !commit)
                        {
                            this.Messaging.Write(ErrorMessages.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, "Encountered an unexpected error while merging '{0}'. More information about the merge and the failure can be found in the merge log: '{1}'", wixMergeRow.SourceFile, logPath), "InvalidOperationException", Environment.StackTrace));
                        }

                        if (moduleOpen)
                        {
                            merge.CloseModule();
                        }
                    }
                }
            }
            finally
            {
                if (databaseOpen)
                {
                    merge.CloseDatabase(commit);
                }

                if (logOpen)
                {
                    merge.CloseLog();
                }
            }

            // stop processing if an error previously occurred
            if (this.Messaging.EncounteredError)
            {
                return;
            }

            using (Database db = new Database(this.OutputPath, OpenDatabase.Direct))
            {
                Table suppressActionTable = this.Output.Tables["WixSuppressAction"];

                // suppress individual actions
                if (null != suppressActionTable)
                {
                    foreach (Row row in suppressActionTable.Rows)
                    {
                        if (db.TableExists((string)row[0]))
                        {
                            string query = String.Format(CultureInfo.InvariantCulture, "SELECT * FROM {0} WHERE `Action` = '{1}'", row[0].ToString(), (string)row[1]);

                            using (View view = db.OpenExecuteView(query))
                            {
                                using (Record record = view.Fetch())
                                {
                                    if (null != record)
                                    {
                                        this.Messaging.Write(WarningMessages.SuppressMergedAction((string)row[1], row[0].ToString()));
                                        view.Modify(ModifyView.Delete, record);
                                    }
                                }
                            }
                        }
                    }
                }

                // query for merge module actions in suppressed sequences and drop them
                foreach (string tableName in this.SuppressedTableNames)
                {
                    if (!db.TableExists(tableName))
                    {
                        continue;
                    }

                    using (View view = db.OpenExecuteView(String.Concat("SELECT `Action` FROM ", tableName)))
                    {
                        while (true)
                        {
                            using (Record resultRecord = view.Fetch())
                            {
                                if (null == resultRecord)
                                {
                                    break;
                                }

                                this.Messaging.Write(WarningMessages.SuppressMergedAction(resultRecord.GetString(1), tableName));
                            }
                        }
                    }

                    // drop suppressed sequences
                    using (View view = db.OpenExecuteView(String.Concat("DROP TABLE ", tableName)))
                    {
                    }

                    // delete the validation rows
                    using (View view = db.OpenView(String.Concat("DELETE FROM _Validation WHERE `Table` = ?")))
                    {
                        using (Record record = new Record(1))
                        {
                            record.SetString(1, tableName);
                            view.Execute(record);
                        }
                    }
                }

                // now update the Attributes column for the files from the Merge Modules
                this.Messaging.Write(VerboseMessages.ResequencingMergeModuleFiles());
                using (View view = db.OpenView("SELECT `Sequence`, `Attributes` FROM `File` WHERE `File`=?"))
                {
                    foreach (var file in this.FileFacades)
                    {
                        if (!file.FromModule)
                        {
                            continue;
                        }

                        using (Record record = new Record(1))
                        {
                            record.SetString(1, file.File.Id.Id);
                            view.Execute(record);
                        }

                        using (Record recordUpdate = view.Fetch())
                        {
                            if (null == recordUpdate)
                            {
                                throw new InvalidOperationException("Failed to fetch a File row from the database that was merged in from a module.");
                            }

                            recordUpdate.SetInteger(1, file.File.Sequence);

                            // Update the file attributes to match the compression specified
                            // on the Merge element or on the Package element.
                            var attributes = 0;

                            // Get the current value if its not null.
                            if (!recordUpdate.IsNull(2))
                            {
                                attributes = recordUpdate.GetInteger(2);
                            }

                            if ((file.File.Attributes & FileTupleAttributes.Compressed) == FileTupleAttributes.Compressed)
                            {
                                attributes |= WindowsInstallerConstants.MsidbFileAttributesCompressed;
                                attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                            }
                            else if ((file.File.Attributes & FileTupleAttributes.Uncompressed) == FileTupleAttributes.Uncompressed)
                            {
                                attributes |= WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                                attributes &= ~WindowsInstallerConstants.MsidbFileAttributesCompressed;
                            }
                            else // clear all compression bits.
                            {
                                attributes &= ~WindowsInstallerConstants.MsidbFileAttributesCompressed;
                                attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                            }

                            recordUpdate.SetInteger(2, attributes);

                            view.Modify(ModifyView.Update, recordUpdate);
                        }
                    }
                }

                db.Commit();
            }
        }
    }
}
