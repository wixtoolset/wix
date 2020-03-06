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
                new IntermediateFieldDefinition(nameof(UserGroupTupleFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserGroupTupleFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(UserGroupTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum UserGroupTupleFields
    {
        UserRef,
        GroupRef,
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

        public string UserRef
        {
            get => this.Fields[(int)UserGroupTupleFields.UserRef].AsString();
            set => this.Set((int)UserGroupTupleFields.UserRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)UserGroupTupleFields.GroupRef].AsString();
            set => this.Set((int)UserGroupTupleFields.GroupRef, value);
        }
    }
}