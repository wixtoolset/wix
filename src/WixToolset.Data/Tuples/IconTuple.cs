// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Icon = new IntermediateTupleDefinition(
            TupleDefinitionType.Icon,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IconTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IconTupleFields.Data), IntermediateFieldType.Path),
            },
            typeof(IconTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum IconTupleFields
    {
        Name,
        Data,
    }

    public class IconTuple : IntermediateTuple
    {
        public IconTuple() : base(TupleDefinitions.Icon, null, null)
        {
        }

        public IconTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Icon, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IconTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)IconTupleFields.Name];
            set => this.Set((int)IconTupleFields.Name, value);
        }

        public string Data
        {
            get => (string)this.Fields[(int)IconTupleFields.Data];
            set => this.Set((int)IconTupleFields.Data, value);
        }
    }
}