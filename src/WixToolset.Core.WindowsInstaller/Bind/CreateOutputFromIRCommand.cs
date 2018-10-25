// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility;

    internal class CreateOutputFromIRCommand
    {
        public CreateOutputFromIRCommand(IntermediateSection section, TableDefinitionCollection tableDefinitions, IEnumerable<IWindowsInstallerBackendBinderExtension> backendExtensions)
        {
            this.Section = section;
            this.TableDefinitions = tableDefinitions;
            this.BackendExtensions = backendExtensions;
        }

        private IEnumerable<IWindowsInstallerBackendBinderExtension> BackendExtensions { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        private IntermediateSection Section { get; }

        public Output Output { get; private set; }

        public void Execute()
        {
            var output = new Output(this.Section.Tuples.First().SourceLineNumbers);
            output.Codepage = this.Section.Codepage;
            output.Type = SectionTypeToOutputType(this.Section.Type);

            this.AddSectionToOutput(this.Section, output);

            this.Output = output;
        }

        private void AddSectionToOutput(IntermediateSection section, Output output)
        {
            foreach (var tuple in section.Tuples)
            {
                switch (tuple.Definition.Type)
                {
                    case TupleDefinitionType.File:
                        this.AddFileTuple((FileTuple)tuple, output);
                        break;

                    case TupleDefinitionType.Media:
                        this.AddMediaTuple((MediaTuple)tuple, output);
                        break;

                    case TupleDefinitionType.Property:
                        this.AddPropertyTuple((PropertyTuple)tuple, output);
                        break;

                    case TupleDefinitionType.WixAction:
                        this.AddWixActionTuple((WixActionTuple)tuple, output);
                        break;

                    case TupleDefinitionType.WixMedia:
                        // Ignored.
                        break;

                    case TupleDefinitionType.WixMediaTemplate:
                        this.AddWixMediaTemplateTuple((WixMediaTemplateTuple)tuple, output);
                        break;

                    case TupleDefinitionType.MustBeFromAnExtension:
                        this.AddTupleFromExtension(tuple, output);
                        break;

                    default:
                        this.AddTupleDefaultly(tuple, output);
                        break;
                }
            }
        }

        private void AddFileTuple(FileTuple tuple, Output output)
        {
            var table = output.EnsureTable(this.TableDefinitions["File"]);
            var row = (FileRow)table.CreateRow(tuple.SourceLineNumbers);
            row.File = tuple.File;
            row.Component = tuple.Component_;
            row.FileName = GetMsiFilenameValue(tuple.ShortFileName, tuple.LongFileName);
            row.FileSize = tuple.FileSize;
            row.Version = tuple.Version;
            row.Language = tuple.Language;

            var attributes = tuple.Checksum ? MsiInterop.MsidbFileAttributesChecksum : 0;
            attributes |= (tuple.Compressed.HasValue && tuple.Compressed.Value) ? MsiInterop.MsidbFileAttributesCompressed : 0;
            attributes |= (tuple.Compressed.HasValue && !tuple.Compressed.Value) ? MsiInterop.MsidbFileAttributesNoncompressed : 0;
            attributes |= tuple.Hidden ? MsiInterop.MsidbFileAttributesHidden : 0;
            attributes |= tuple.ReadOnly ? MsiInterop.MsidbFileAttributesReadOnly : 0;
            attributes |= tuple.System ? MsiInterop.MsidbFileAttributesSystem : 0;
            attributes |= tuple.Vital ? MsiInterop.MsidbFileAttributesVital : 0;
            row.Attributes = attributes;
        }

        private void AddMediaTuple(MediaTuple tuple, Output output)
        {
            if (this.Section.Type != SectionType.Module)
            {
                var table = output.EnsureTable(this.TableDefinitions["Media"]);
                var row = (MediaRow)table.CreateRow(tuple.SourceLineNumbers);
                row.DiskId = tuple.DiskId;
                row.LastSequence = tuple.LastSequence;
                row.DiskPrompt = tuple.DiskPrompt;
                row.Cabinet = tuple.Cabinet;
                row.VolumeLabel = tuple.VolumeLabel;
                row.Source = tuple.Source;
            }
        }

        private void AddPropertyTuple(PropertyTuple tuple, Output output)
        {
            if (String.IsNullOrEmpty(tuple.Value))
            {
                return;
            }

            var table = output.EnsureTable(this.TableDefinitions["Property"]);
            var row = (PropertyRow)table.CreateRow(tuple.SourceLineNumbers);
            row.Property = tuple.Property;
            row.Value = tuple.Value;
        }

        private void AddWixActionTuple(WixActionTuple tuple, Output output)
        {
            // Get the table definition for the action (and ensure the proper table exists for a module).
            TableDefinition sequenceTableDefinition = null;
            switch (tuple.SequenceTable)
            {
                case SequenceTable.AdminExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdminExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdminExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdminExecuteSequence"];
                    }
                    break;
                case SequenceTable.AdminUISequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdminUISequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdminUISequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdminUISequence"];
                    }
                    break;
                case SequenceTable.AdvtExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["AdvtExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleAdvtExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["AdvtExecuteSequence"];
                    }
                    break;
                case SequenceTable.InstallExecuteSequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["InstallExecuteSequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleInstallExecuteSequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["InstallExecuteSequence"];
                    }
                    break;
                case SequenceTable.InstallUISequence:
                    if (OutputType.Module == output.Type)
                    {
                        output.EnsureTable(this.TableDefinitions["InstallUISequence"]);
                        sequenceTableDefinition = this.TableDefinitions["ModuleInstallUISequence"];
                    }
                    else
                    {
                        sequenceTableDefinition = this.TableDefinitions["InstallUISequence"];
                    }
                    break;
            }

            // create the action sequence row in the output
            var sequenceTable = output.EnsureTable(sequenceTableDefinition);
            var row = sequenceTable.CreateRow(tuple.SourceLineNumbers);

            if (SectionType.Module == this.Section.Type)
            {
                row[0] = tuple.Action;
                if (0 != tuple.Sequence)
                {
                    row[1] = tuple.Sequence;
                }
                else
                {
                    bool after = (null == tuple.Before);
                    row[2] = after ? tuple.After : tuple.Before;
                    row[3] = after ? 1 : 0;
                }
                row[4] = tuple.Condition;
            }
            else
            {
                row[0] = tuple.Action;
                row[1] = tuple.Condition;
                row[2] = tuple.Sequence;
            }
        }

        private void AddWixMediaTemplateTuple(WixMediaTemplateTuple tuple, Output output)
        {
            var table = output.EnsureTable(this.TableDefinitions["WixMediaTemplate"]);
            var row = (WixMediaTemplateRow)table.CreateRow(tuple.SourceLineNumbers);
            row.CabinetTemplate = tuple.CabinetTemplate;
            row.CompressionLevel = tuple.CompressionLevel;
            row.DiskPrompt = tuple.DiskPrompt;
            row.VolumeLabel = tuple.VolumeLabel;
            row.MaximumUncompressedMediaSize = tuple.MaximumUncompressedMediaSize;
            row.MaximumCabinetSizeForLargeFileSplitting = tuple.MaximumCabinetSizeForLargeFileSplitting;
        }

        private void AddTupleFromExtension(IntermediateTuple tuple, Output output)
        {
            foreach (var extension in this.BackendExtensions)
            {
                if (extension.TryAddTupleToOutput(tuple, output))
                {
                    break;
                }
            }
        }

        private void AddTupleDefaultly(IntermediateTuple tuple, Output output)
        {
            if (!this.TableDefinitions.TryGet(tuple.Definition.Name, out var tableDefinition))
            {
                return;
            }

            var table = output.EnsureTable(tableDefinition);
            var row = table.CreateRow(tuple.SourceLineNumbers);
            for (var i = 0; i < tuple.Fields.Length; ++i)
            {
                if (i < tableDefinition.Columns.Length)
                {
                    var column = tableDefinition.Columns[i];

                    switch (column.Type)
                    {
                        case ColumnType.Number:
                            row[i] = tuple.AsNumber(i);
                            break;

                        default:
                            row[i] = tuple.AsString(i);
                            break;
                    }
                }
            }
        }

        private static OutputType SectionTypeToOutputType(SectionType type)
        {
            switch (type)
            {
                case SectionType.Bundle:
                    return OutputType.Bundle;
                case SectionType.Module:
                    return OutputType.Module;
                case SectionType.Product:
                    return OutputType.Product;
                case SectionType.PatchCreation:
                    return OutputType.PatchCreation;
                case SectionType.Patch:
                    return OutputType.Patch;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static string GetMsiFilenameValue(string shortName, string longName)
        {
            if (String.IsNullOrEmpty(shortName))
            {
                return longName;
            }
            else
            {
                return shortName + "|" + longName;
            }
        }
    }
}
