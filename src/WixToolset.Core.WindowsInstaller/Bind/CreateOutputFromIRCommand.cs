// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Data.Tuples;

    internal class CreateOutputFromIRCommand
    {
        public CreateOutputFromIRCommand(IntermediateSection section, TableDefinitionCollection tableDefinitions)
        {
            this.Section = section;
            this.TableDefinitions = tableDefinitions;
        }

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

                    case TupleDefinitionType.WixAction:
                        this.AddWixActionTuple((WixActionTuple)tuple, output);
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

        private void AddWixActionTuple(WixActionTuple actionRow, Output output)
        {
            // Get the table definition for the action (and ensure the proper table exists for a module).
            TableDefinition sequenceTableDefinition = null;
            switch (actionRow.SequenceTable)
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
            var row = sequenceTable.CreateRow(actionRow.SourceLineNumbers);

            if (SectionType.Module == this.Section.Type)
            {
                row[0] = actionRow.Action;
                if (0 != actionRow.Sequence)
                {
                    row[1] = actionRow.Sequence;
                }
                else
                {
                    bool after = (null == actionRow.Before);
                    row[2] = after ? actionRow.After : actionRow.Before;
                    row[3] = after ? 1 : 0;
                }
                row[4] = actionRow.Condition;
            }
            else
            {
                row[0] = actionRow.Action;
                row[1] = actionRow.Condition;
                row[2] = actionRow.Sequence;
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
                if (i < tableDefinition.Columns.Count)
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
