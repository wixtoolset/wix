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
        public ResolveFieldsCommand(IMessaging messaging, IFileResolver fileResolver, IVariableResolver variableResolver, IReadOnlyCollection<IBindPath> bindPaths, IReadOnlyCollection<IResolverExtension> extensions, ExtractEmbeddedFiles filesWithEmbeddedFiles, string intermediateFolder, Intermediate intermediate, bool allowUnresolvedVariables)
        {
            this.Messaging = messaging;
            this.FileResolver = fileResolver;
            this.VariableResolver = variableResolver;
            this.BindPaths = bindPaths;
            this.Extensions = extensions;
            this.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
            this.IntermediateFolder = intermediateFolder;
            this.Intermediate = intermediate;
            this.AllowUnresolvedVariables = allowUnresolvedVariables;
        }

        private IMessaging Messaging { get; }

        private IFileResolver FileResolver { get; }

        private IVariableResolver VariableResolver { get; }

        private IEnumerable<IBindPath> BindPaths { get; }

        private IEnumerable<IResolverExtension> Extensions { get; }

        private ExtractEmbeddedFiles FilesWithEmbeddedFiles { get; }

        private string IntermediateFolder { get; }

        private Intermediate Intermediate { get; }

        private bool AllowUnresolvedVariables { get; }

        public IReadOnlyCollection<DelayedField> DelayedFields { get; private set; }

        public void Execute()
        {
            var delayedFields = new List<DelayedField>();

            var bindPaths = this.BindPaths.Where(b => b.Stage == BindStage.Normal).ToList();

            // Build the column lookup only when needed.
            Dictionary<string, WixCustomTableColumnSymbol> customColumnsById = null;

            foreach (var symbol in this.Intermediate.Sections.SelectMany(s => s.Symbols))
            {
                foreach (var field in symbol.Fields.Where(f => !f.IsNull()))
                {
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
                        this.ResolvePathField(this.FileResolver, bindPaths, symbol, field);
                    }
                }
            }

            this.DelayedFields = delayedFields;
        }

        private void ResolvePathField(IFileResolver fileResolver, IEnumerable<IBindPath> bindPaths, IntermediateSymbol symbol, IntermediateField field)
        {
            var fieldValue = field.AsPath();
            var originalFieldPath = fieldValue.Path;

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
                    var resolvedPath = fileResolver.ResolveFile(fieldValue.Path, this.Extensions, bindPaths, BindStage.Normal, symbol.SourceLineNumbers, symbol.Definition);

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
