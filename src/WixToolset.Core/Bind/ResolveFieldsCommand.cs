// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
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
            Dictionary<string, WixCustomTableColumnSymbol> customColumnsById = null;

            foreach (var sections in this.Intermediate.Sections)
            {
                foreach (var symbol in sections.Symbols)
                {
                    foreach (var field in symbol.Fields)
                    {
                        if (field.IsNull())
                        {
                            continue;
                        }

                        var fieldType = field.Type;

                        // Custom table cells require an extra look up to the column definition as the
                        // cell's data type is always a string (because strings can store anything) but
                        // the column definition may be more specific.
                        if (symbol.Definition.Type == SymbolDefinitionType.WixCustomTableCell)
                        {
                            // We only care about the Data in a CustomTable cell.
                            if (field.Name != nameof(WixCustomTableCellSymbolFields.Data))
                            {
                                continue;
                            }

                            if (customColumnsById == null)
                            {
                                customColumnsById = this.Intermediate.Sections.SelectMany(s => s.Symbols.OfType<WixCustomTableColumnSymbol>()).ToDictionary(t => t.Id.Id);
                            }

                            if (customColumnsById.TryGetValue(symbol.Fields[(int)WixCustomTableCellSymbolFields.TableRef].AsString() + "/" + symbol.Fields[(int)WixCustomTableCellSymbolFields.ColumnRef].AsString(), out var customColumn))
                            {
                                fieldType = customColumn.Type;
                            }
                        }

                        // Check to make sure we're in a scenario where we can handle variable resolution.
                        if (null != delayedFields)
                        {
                            // resolve localization and wix variables
                            if (fieldType == IntermediateFieldType.String)
                            {
                                var original = field.AsString();
                                if (!String.IsNullOrEmpty(original))
                                {
                                    var resolution = this.VariableResolver.ResolveVariables(symbol.SourceLineNumbers, original, !this.AllowUnresolvedVariables);
                                    if (resolution.UpdatedValue)
                                    {
                                        field.Set(resolution.Value);
                                    }

                                    if (resolution.DelayedResolve)
                                    {
                                        delayedFields.Add(new DelayedField(symbol, field));
                                    }
                                }
                            }
                        }

                        // Move to next symbol if we've hit an error resolving variables.
                        if (this.Messaging.EncounteredError) // TODO: make this error handling more specific to just the failure to resolve variables in this field.
                        {
                            continue;
                        }

                        // Resolve file paths
                        if (fieldType == IntermediateFieldType.Path)
                        {
                            this.ResolvePathField(fileResolver, symbol, field);

#if TODO_PATCHING
                            if (null != objectField.PreviousData)
                            {
                                objectField.PreviousData = this.BindVariableResolver.ResolveVariables(symbol.SourceLineNumbers, objectField.PreviousData, false, out isDefault);

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
                                                objectField.PreviousData = fileResolver.ResolveFile((string)objectField.PreviousData, symbol.Definition.Name, symbol.SourceLineNumbers, BindStage.Normal);
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
                                                objectField.PreviousData = fileResolver.ResolveFile((string)objectField.PreviousData, symbol.Definition.Name, symbol.SourceLineNumbers, BindStage.Target);

                                            }
                                        }
                                        catch (WixFileNotFoundException)
                                        {
                                            // display the error with source line information
                                            Messaging.Instance.Write(WixErrors.FileNotFound(symbol.SourceLineNumbers, (string)objectField.PreviousData));
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

        private void ResolvePathField(FileResolver fileResolver, IntermediateSymbol symbol, IntermediateField field)
        {
            var fieldValue = field.AsPath();
            var originalFieldPath = fieldValue.Path;

#if TODO_PATCHING
            // Skip file resolution if the file is to be deleted.
            if (RowOperation.Delete == symbol.Operation)
            {
                continue;
            }
#endif

            // If the file is embedded and if the previous value has a bind variable in the path
            // which gets modified by resolving the previous value again then switch to that newly
            // resolved path instead of using the embedded file.
            if (fieldValue.Embed && field.PreviousValue != null)
            {
                var resolution = this.VariableResolver.ResolveVariables(symbol.SourceLineNumbers, field.PreviousValue.AsString(), errorOnUnknown: false);

                if (resolution.UpdatedValue && !resolution.IsDefault)
                {
                    fieldValue = new IntermediateFieldPathValue { Path = resolution.Value };
                }
            }

            // If we're still using the embedded file.
            if (fieldValue.Embed)
            {
                // Set the path to the embedded file once where it will be extracted.
                var extractPath = this.FilesWithEmbeddedFiles.AddEmbeddedFileToExtract(fieldValue.BaseUri, fieldValue.Path, this.IntermediateFolder);

                field.Set(extractPath);
            }
            else if (fieldValue.Path != null)
            {
                try
                {
                    var resolvedPath = fieldValue.Path;

                    if (!this.BuildingPatch) // Normal binding for non-Patch scenario such as link (light.exe)
                    {
#if TODO_PATCHING
                        // keep a copy of the un-resolved data for future replay. This will be saved into wixpdb file
                        if (null == objectField.UnresolvedData)
                        {
                            objectField.UnresolvedData = (string)objectField.Data;
                        }
#endif
                        resolvedPath = fileResolver.ResolveFile(fieldValue.Path, symbol.Definition, symbol.SourceLineNumbers, BindStage.Normal);
                    }
                    else if (!fileResolver.RebaseTarget && !fileResolver.RebaseUpdated) // Normal binding for Patch Scenario (normal patch, no re-basing logic)
                    {
                        resolvedPath = fileResolver.ResolveFile(fieldValue.Path, symbol.Definition, symbol.SourceLineNumbers, BindStage.Normal);
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

                        objectField.Data = fileResolver.ResolveFile(filePathToResolve, symbol.Definition.Name, symbol.SourceLineNumbers, BindStage.Updated);
                    }
#endif

                    if (!String.Equals(originalFieldPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        field.Set(resolvedPath);
                    }
                }
                catch (WixException e)
                {
                    this.Messaging.Write(e.Error);
                }
            }
        }
    }
}
