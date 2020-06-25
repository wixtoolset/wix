// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using WixToolset.Core.Bind;
    using WixToolset.Core.Native;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Merge modules into the database at output path.
    /// </summary>
    internal class MergeModulesCommand
    {
        public MergeModulesCommand(IMessaging messaging, IEnumerable<FileFacade> fileFacades, IntermediateSection section, IEnumerable<string> suppressedTableNames, string outputPath, string intermediateFolder)
        {
            this.Messaging = messaging;
            this.FileFacades = fileFacades;
            this.Section = section;
            this.SuppressedTableNames = suppressedTableNames ?? Array.Empty<string>();
            this.OutputPath = outputPath;
            this.IntermediateFolder = intermediateFolder;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<FileFacade> FileFacades { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<string> SuppressedTableNames { get; }

        private string OutputPath { get; }

        private string IntermediateFolder { get; }

        public void Execute()
        {
            var wixMergeSymbols = this.Section.Symbols.OfType<WixMergeSymbol>().ToList();
            if (!wixMergeSymbols.Any())
            {
                return;
            }

            IMsmMerge2 merge = null;
            var commit = true;
            var logOpen = false;
            var databaseOpen = false;
            var logPath = Path.Combine(this.IntermediateFolder, "merge.log");

            try
            {
                var interop = new MsmInterop();
                merge = interop.GetMsmMerge();

                merge.OpenLog(logPath);
                logOpen = true;

                merge.OpenDatabase(this.OutputPath);
                databaseOpen = true;

                var featureModulesByMergeId = this.Section.Symbols.OfType<WixFeatureModulesSymbol>().GroupBy(t => t.WixMergeRef).ToDictionary(g => g.Key);

                // process all the merge rows
                foreach (var wixMergeRow in wixMergeSymbols)
                {
                    var moduleOpen = false;

                    try
                    {
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            this.Messaging.Write(ErrorMessages.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id.Id, wixMergeRow.Language.ToString()));
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

                        // Merge the module into the database that's being built.
                        this.Messaging.Write(VerboseMessages.MergingMergeModule(wixMergeRow.SourceFile));
                        merge.MergeEx(wixMergeRow.FeatureRef, wixMergeRow.DirectoryRef, callback);

                        // Connect any non-primary features.
                        if (featureModulesByMergeId.TryGetValue(wixMergeRow.Id.Id, out var featureModules))
                        {
                            foreach (var featureModule in featureModules)
                            {
                                this.Messaging.Write(VerboseMessages.ConnectingMergeModule(wixMergeRow.SourceFile, featureModule.FeatureRef));
                                merge.Connect(featureModule.FeatureRef);
                            }
                        }
                    }
                    catch (COMException)
                    {
                        commit = false;
                    }
                    finally
                    {
                        var mergeErrors = merge.Errors;

                        // display all the errors encountered during the merge operations for this module
                        for (var i = 1; i <= mergeErrors.Count; i++)
                        {
                            var mergeError = mergeErrors[i];
                            var databaseKeys = new StringBuilder();
                            var moduleKeys = new StringBuilder();

                            // build a string of the database keys
                            for (var j = 1; j <= mergeError.DatabaseKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    databaseKeys.Append(';');
                                }
                                databaseKeys.Append(mergeError.DatabaseKeys[j]);
                            }

                            // build a string of the module keys
                            for (var j = 1; j <= mergeError.ModuleKeys.Count; j++)
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
                                    this.Messaging.Write(ErrorMessages.MergeExcludedModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id.Id, moduleKeys.ToString()));
                                    break;
                                case MsmErrorType.msmErrorFeatureRequired:
                                    this.Messaging.Write(ErrorMessages.MergeFeatureRequired(wixMergeRow.SourceLineNumbers, mergeError.ModuleTable, moduleKeys.ToString(), wixMergeRow.SourceFile, wixMergeRow.Id.Id));
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

            using (var db = new Database(this.OutputPath, OpenDatabase.Direct))
            {
                // Suppress individual actions.
                foreach (var suppressAction in this.Section.Symbols.OfType<WixSuppressActionSymbol>())
                {
                    var tableName = suppressAction.SequenceTable.WindowsInstallerTableName();
                    if (db.TableExists(tableName))
                    {
                        var query = $"SELECT * FROM {tableName} WHERE `Action` = '{suppressAction.Action}'";

                        using (var view = db.OpenExecuteView(query))
                        using (var record = view.Fetch())
                        {
                            if (null != record)
                            {
                                this.Messaging.Write(WarningMessages.SuppressMergedAction(suppressAction.Action, tableName));
                                view.Modify(ModifyView.Delete, record);
                            }
                        }
                    }
                }

                // Query for merge module actions in suppressed sequences and drop them.
                foreach (var tableName in this.SuppressedTableNames)
                {
                    if (!db.TableExists(tableName))
                    {
                        continue;
                    }

                    using (var view = db.OpenExecuteView(String.Concat("SELECT `Action` FROM ", tableName)))
                    {
                        foreach (var resultRecord in view.Records)
                        {
                            this.Messaging.Write(WarningMessages.SuppressMergedAction(resultRecord.GetString(1), tableName));
                        }
                    }

                    // drop suppressed sequences
                    using (var view = db.OpenExecuteView(String.Concat("DROP TABLE ", tableName)))
                    {
                    }

                    // delete the validation rows
                    using (var view = db.OpenView(String.Concat("DELETE FROM _Validation WHERE `Table` = ?")))
                    using (var record = new Record(1))
                    {
                        record.SetString(1, tableName);
                        view.Execute(record);
                    }
                }

                // now update the Attributes column for the files from the Merge Modules
                this.Messaging.Write(VerboseMessages.ResequencingMergeModuleFiles());
                using (var view = db.OpenView("SELECT `Sequence`, `Attributes` FROM `File` WHERE `File`=?"))
                {
                    foreach (var file in this.FileFacades)
                    {
                        if (!file.FromModule)
                        {
                            continue;
                        }

                        using (var record = new Record(1))
                        {
                            record.SetString(1, file.Id);
                            view.Execute(record);
                        }

                        using (var recordUpdate = view.Fetch())
                        {
                            if (null == recordUpdate)
                            {
                                throw new InvalidOperationException("Failed to fetch a File row from the database that was merged in from a module.");
                            }

                            recordUpdate.SetInteger(1, file.Sequence);

                            // Update the file attributes to match the compression specified
                            // on the Merge element or on the Package element.
                            var attributes = 0;

                            // Get the current value if its not null.
                            if (!recordUpdate.IsNull(2))
                            {
                                attributes = recordUpdate.GetInteger(2);
                            }

                            if (file.Compressed)
                            {
                                attributes |= WindowsInstallerConstants.MsidbFileAttributesCompressed;
                                attributes &= ~WindowsInstallerConstants.MsidbFileAttributesNoncompressed;
                            }
                            else if (file.Uncompressed)
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
