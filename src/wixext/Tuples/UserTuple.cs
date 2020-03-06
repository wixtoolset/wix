// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition User = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.User.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(UserTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserTupleFields.Domain), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserTupleFields.Password), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(UserTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum UserTupleFields
    {
        ComponentRef,
        Name,
        Domain,
        Password,
        Attributes,
    }

    public class UserTuple : IntermediateTuple
    {
        public UserTuple() : base(UtilTupleDefinitions.User, null, null)
        {
        }

        public UserTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.User, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UserTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)UserTupleFields.ComponentRef].AsString();
            set => this.Set((int)UserTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)UserTupleFields.Name].AsString();
            set => this.Set((int)UserTupleFields.Name, value);
        }

        public string Domain
        {
            get => this.Fields[(int)UserTupleFields.Domain].AsString();
            set => this.Set((int)UserTupleFields.Domain, value);
        }

        public string Password
        {
            get => this.Fields[(int)UserTupleFields.Password].AsString();
            set => this.Set((int)UserTupleFields.Password, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)UserTupleFields.Attributes].AsNumber();
            set => this.Set((int)UserTupleFields.Attributes, value);
        }
    }
}