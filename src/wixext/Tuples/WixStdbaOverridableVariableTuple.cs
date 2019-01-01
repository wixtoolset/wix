// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixStdbaOverridableVariable = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixStdbaOverridableVariable.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixStdbaOverridableVariableTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(WixStdbaOverridableVariableTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixStdbaOverridableVariableTupleFields
    {
        Name,
    }

    public class WixStdbaOverridableVariableTuple : IntermediateTuple
    {
        public WixStdbaOverridableVariableTuple() : base(BalTupleDefinitions.WixStdbaOverridableVariable, null, null)
        {
        }

        public WixStdbaOverridableVariableTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixStdbaOverridableVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixStdbaOverridableVariableTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)WixStdbaOverridableVariableTupleFields.Name].AsString();
            set => this.Set((int)WixStdbaOverridableVariableTupleFields.Name, value);
        }
    }
}