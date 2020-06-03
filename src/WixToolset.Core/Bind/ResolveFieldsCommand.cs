// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolve source fields in the tables included in the output
    /// </summary>
    internal class ResolveFieldsCommand
    {
        public IMessaging Messaging { private get; set; }

        public bool BuildingPatch { private get; set; }

        public IVariableResolver VariableResolver { private get; set; }

        public IEnumerable<IBindPath> BindPaths { private get; set; }

        public IEnumerable<IResolverExtension> Extensions { private get; set; }

        public ExtractEmbeddedFiles FilesWithEmbeddedFiles { private get; set; }

        public string IntermediateFolder { private get; set; }

        public Intermediate Intermediate { private get; set; }

        public bool SupportDelayedResolution { private get; set; }

        public bool AllowUnresolvedVariables { private get; set; }

        public IEnumerable<DelayedField> DelayedFields { get; private set; }

        public void Execute()
        {
            var delayedFields = this.SupportDelayedResolution ? new List<DelayedField>() : null;

            var fileResolver = new FileResolver(this.BindPaths, this.Extensions);

            // Build the column lookup only when needed.
            Dictionary<string, WixCustomTableColumnTuple> customColumnsById = null;

            foreach (var sections in this.Intermediate.Sections)
            {
                foreach (var tuple in sections.Tuples)
                {
                    foreach (var field in tuple.Fields)
                    {
                        if (field.IsNull())
                        {
                            continue;
                        }

                        var fieldType = field.Type;

                        // Custom table cells require an extra look up to the column definition as the
                        // cell's data type is always a string (because strings can store anything) but
                        // the column definition may be more specific.
                        if (tuple.Definition.Type == TupleDefinitionType.WixCustomTableCell)
                        {
                            // We only care about the Data in a CustomTable cell.
                            if (field.Name != nameof(WixCustomTableCellTupleFields.Data))
                            {
                                continue;
                            }

                            if (customColumnsById == null)
                            {
                                customColumnsById = this.Intermediate.Sections.SelectMany(s => s.Tuples.OfType<WixCustomTableColumnTuple>()).ToDictionary(t => t.Id.Id);
                            }

                            if (customColumnsById.TryGetValue(tuple.Fields[(int)WixCustomTableCellTupleFields.TableRef].AsString() + "/" + tuple.Fields[(int)WixCustomTableCellTupleFields.ColumnRef].AsString(), out var customColumn))
                            {
                                fieldType = customColumn.Type;
                            }
                        }

                        var isDefault = true;

                        // Check to make sure we're in a scenario where we can handle variable resolution.
                        if (null != delayedFields)
                        {
                            // resolve localization and wix variables
                            if (fieldType == IntermediateFieldType.String)
                            {
                                var original = field.AsString();
                                if (!String.IsNullOrEmpty(original))
                                {
                                    var resolution = this.VariableResolver.ResolveVariables(tuple.SourceLineNumbers, original, !this.AllowUnresolvedVariables);
                                    if (resolution.UpdatedValue)
                                    {
                                        field.Set(resolution.Value);
                                    }

                                    if (resolution.DelayedResolve)
                                    {
                                        delayedFields.Add(new DelayedField(tuple, field));
                                    }

                                    isDefault = resolution.IsDefault;
                                }
                            }
                        }

                        // Move to next tuple if we've hit an error resolving variables.
                        if (this.Messaging.EncounteredError) // TODO: make this error handling more specific to just the failure to resolve variables in this field.
                        {
                            continue;
                        }

                        // Resolve file paths
                        if (fieldType == IntermediateFieldType.Path)
                        {
                            var objectField = field.AsPath();

#if TODO_PATCHING
                            // Skip file resolution if the file is to be deleted.
                            if (RowOperation.Delete == tuple.Operation)
                            {
                                continue;
                            }
#endif

                            // File is embedded and path to it was not modified above.
                            if (isDefault && objectField.Embed)
                            {
                                var extractPath = this.FilesWithEmbeddedFiles.AddEmbeddedFileToExtract(objectField.BaseUri, objectField.Path, this.IntermediateFolder);

                                // Set the path to the embedded file once where it will be extracted.
                                field.Set(extractPath);
                            }
                            else if (null != objectField.Path) // non-compressed file (or localized value)
                            {
                                try
                                {
                                    if (!this.BuildingPatch) // Normal binding for non-Patch scenario such as link (light.exe)
                                    {
#if TODO_PATCHING
                                        // keep a copy of the un-resolved data for future replay. This will be saved into wixpdb file
                                        if (null == objectField.UnresolvedData)
                                        {
                                            objectField.UnresolvedData = (string)objectField.Data;
                                        }
#endif

                                        // resolve the path to the file
                                        var value = fileResolver.ResolveFile(objectField.Path, tuple.Definition, tuple.SourceLineNumbers, BindStage.Normal);
                                        field.Set(value);
                                    }
                                    else if (!fileResolver.RebaseTarget && !fileResolver.RebaseUpdated) // Normal binding for Patch Scenario (normal patch, no re-basing logic)
                                    {
                                        // resolve the path to the file
                                        var value = fileResolver.ResolveFile(objectField.Path, tuple.Definition, tuple.SourceLineNumbers, BindStage.Normal);
                                        field.Set(value);
                                    }
#if TODO_PATCHING
                                    else // Re-base binding path scenario caused by pyro.exe -bt -bu
                                    {
                                        // by default, use the resolved Data for file lookup
                                        string filePathToResolve = (string)objectField.Data;

                                        // if -bu is used in pyro command, this condition holds true and the tool
                                        // will use pre-resolved source for new wixpdb file
                                        if (fileResolver.RebaseUpdated)
                                        {
                                            // try to use the unResolved Source if it exists.
                                            // New version of wixpdb file keeps a copy of pre-resolved Source. i.e. !(bindpath.test)\foo.dll
                                            // Old version of winpdb file does not contain this attribute and the value is null.
                                            if (null != objectField.UnresolvedData)
                                            {
                                                filePathToResolve = objectField.UnresolvedData;
                                            }
                                        }

                                        objectField.Data = fileResolver.ResolveFile(filePathToResolve, tuple.Definition.Name, tuple.SourceLineNumbers, BindStage.Updated);
                                    }
#endif
                                }
                                catch (WixException e)
                                {
                                    this.Messaging.Write(e.Error);
                                }
                            }

#if TODO_PATCHING
                            if (null != objectField.PreviousData)
                            {
                                objectField.PreviousData = this.BindVariableResolver.ResolveVariables(tuple.SourceLineNumbers, objectField.PreviousData, false, out isDefault);

                                if (!Messaging.Instance.EncounteredError) // TODO: make this error handling more specific to just the failure to resolve variables in this field.
                                {
                                    // file is compressed in a cabinet (and not modified above)
                                    if (objectField.PreviousEmbeddedFileIndex.HasValue && isDefault)
                                    {
                                        // when loading transforms from disk, PreviousBaseUri may not have been set
                                        if (null == objectField.PreviousBaseUri)
                                        {
                                            objectField.PreviousBaseUri = objectField.BaseUri;
                                        }

                                        string extractPath = this.FilesWithEmbeddedFiles.AddEmbeddedFileIndex(objectField.PreviousBaseUri, objectField.PreviousEmbeddedFileIndex.Value, this.IntermediateFolder);

                                        // set the path to the file once its extracted from the cabinet
                                        objectField.PreviousData = extractPath;
                                    }
                                    else if (null != objectField.PreviousData) // non-compressed file (or localized value)
                                    {
                                        try
                                        {
                                            if (!fileResolver.RebaseTarget && !fileResolver.RebaseUpdated)
                                            {
                                                // resolve the path to the file
                                                objectField.PreviousData = fileResolver.ResolveFile((string)objectField.PreviousData, tuple.Definition.Name, tuple.SourceLineNumbers, BindStage.Normal);
                                            }
                                            else
                                            {
                                                if (fileResolver.RebaseTarget)
                                                {
                                                    // if -bt is used, it come here
                                                    // Try to use the original unresolved source from either target build or update build
                                                    // If both target and updated are of old wixpdb, it behaves the same as today, no re-base logic here
                                                    // If target is old version and updated is new version, it uses unresolved path from updated build
                                                    // If both target and updated are of new versions, it uses unresolved path from target build
                                                    if (null != objectField.UnresolvedPreviousData || null != objectField.UnresolvedData)
                                                    {
                                                        objectField.PreviousData = objectField.UnresolvedPreviousData ?? objectField.UnresolvedData;
                                                    }
                                                }

                                                // resolve the path to the file
                                                objectField.PreviousData = fileResolver.ResolveFile((string)objectField.PreviousData, tuple.Definition.Name, tuple.SourceLineNumbers, BindStage.Target);

                                            }
                                        }
                                        catch (WixFileNotFoundException)
                                        {
                                            // display the error with source line information
                                            Messaging.Instance.Write(WixErrors.FileNotFound(tuple.SourceLineNumbers, (string)objectField.PreviousData));
                                        }
                                    }
                                }
                            }
#endif
                        }
                    }
                }
            }

            this.DelayedFields = delayedFields;
        }
    }
}
