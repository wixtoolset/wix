// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using WixToolset.Data;
    using WixToolset.DifxApp.Tuples;

    public static partial class DifxAppTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiDriverPackages = new IntermediateTupleDefinition(
            DifxAppTupleDefinitionType.MsiDriverPackages.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesTupleFields.Component), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesTupleFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(MsiDriverPackagesTuple));
    }
}

namespace WixToolset.DifxApp.Tuples
{
    using WixToolset.Data;

    public enum MsiDriverPackagesTupleFields
    {
        Component,
        Flags,
        Sequence,
    }

    public class MsiDriverPackagesTuple : IntermediateTuple
    {
        public MsiDriverPackagesTuple() : base(DifxAppTupleDefinitions.MsiDriverPackages, null, null)
        {
        }

        public MsiDriverPackagesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DifxAppTupleDefinitions.MsiDriverPackages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDriverPackagesTupleFields index] => this.Fields[(int)index];

        public string Component
        {
            get => this.Fields[(int)MsiDriverPackagesTupleFields.Component].AsString();
            set => this.Set((int)MsiDriverPackagesTupleFields.Component, value);
        }

        public int Flags
        {
            get => this.Fields[(int)MsiDriverPackagesTupleFields.Flags].AsNumber();
            set => this.Set((int)MsiDriverPackagesTupleFields.Flags, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)MsiDriverPackagesTupleFields.Sequence].AsNumber();
            set => this.Set((int)MsiDriverPackagesTupleFields.Sequence, value);
        }
    }
}