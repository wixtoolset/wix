// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleDependency = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleDependency,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleDependencyTupleFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleDependencyTupleFields.ModuleLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleDependencyTupleFields.RequiredID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleDependencyTupleFields.RequiredLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleDependencyTupleFields.RequiredVersion), IntermediateFieldType.String),
            },
            typeof(ModuleDependencyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleDependencyTupleFields
    {
        ModuleID,
        ModuleLanguage,
        RequiredID,
        RequiredLanguage,
        RequiredVersion,
    }

    public class ModuleDependencyTuple : IntermediateTuple
    {
        public ModuleDependencyTuple() : base(TupleDefinitions.ModuleDependency, null, null)
        {
        }

        public ModuleDependencyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleDependencyTupleFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleDependencyTupleFields.ModuleID];
            set => this.Set((int)ModuleDependencyTupleFields.ModuleID, value);
        }

        public int ModuleLanguage
        {
            get => (int)this.Fields[(int)ModuleDependencyTupleFields.ModuleLanguage];
            set => this.Set((int)ModuleDependencyTupleFields.ModuleLanguage, value);
        }

        public string RequiredID
        {
            get => (string)this.Fields[(int)ModuleDependencyTupleFields.RequiredID];
            set => this.Set((int)ModuleDependencyTupleFields.RequiredID, value);
        }

        public int RequiredLanguage
        {
            get => (int)this.Fields[(int)ModuleDependencyTupleFields.RequiredLanguage];
            set => this.Set((int)ModuleDependencyTupleFields.RequiredLanguage, value);
        }

        public string RequiredVersion
        {
            get => (string)this.Fields[(int)ModuleDependencyTupleFields.RequiredVersion];
            set => this.Set((int)ModuleDependencyTupleFields.RequiredVersion, value);
        }
    }
}