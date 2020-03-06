// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Group = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.Group.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(GroupTupleFields.Group), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupTupleFields.Domain), IntermediateFieldType.String),
            },
            typeof(GroupTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum GroupTupleFields
    {
        Group,
        ComponentRef,
        Name,
        Domain,
    }

    public class GroupTuple : IntermediateTuple
    {
        public GroupTuple() : base(UtilTupleDefinitions.Group, null, null)
        {
        }

        public GroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.Group, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupTupleFields index] => this.Fields[(int)index];

        public string Group
        {
            get => this.Fields[(int)GroupTupleFields.Group].AsString();
            set => this.Set((int)GroupTupleFields.Group, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)GroupTupleFields.ComponentRef].AsString();
            set => this.Set((int)GroupTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)GroupTupleFields.Name].AsString();
            set => this.Set((int)GroupTupleFields.Name, value);
        }

        public string Domain
        {
            get => this.Fields[(int)GroupTupleFields.Domain].AsString();
            set => this.Set((int)GroupTupleFields.Domain, value);
        }
    }
}