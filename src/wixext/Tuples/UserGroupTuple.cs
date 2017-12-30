// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UserGroup = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.UserGroup.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(UserGroupTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserGroupTupleFields.Group_), IntermediateFieldType.String),
            },
            typeof(UserGroupTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum UserGroupTupleFields
    {
        User_,
        Group_,
    }

    public class UserGroupTuple : IntermediateTuple
    {
        public UserGroupTuple() : base(UtilTupleDefinitions.UserGroup, null, null)
        {
        }

        public UserGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.UserGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UserGroupTupleFields index] => this.Fields[(int)index];

        public string User_
        {
            get => this.Fields[(int)UserGroupTupleFields.User_].AsString();
            set => this.Set((int)UserGroupTupleFields.User_, value);
        }

        public string Group_
        {
            get => this.Fields[(int)UserGroupTupleFields.Group_].AsString();
            set => this.Set((int)UserGroupTupleFields.Group_, value);
        }
    }
}