// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiLockPermissionsEx = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiLockPermissionsEx,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExTupleFields.MsiLockPermissionsEx), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExTupleFields.LockObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExTupleFields.SDDLText), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExTupleFields.Condition), IntermediateFieldType.String),
            },
            typeof(MsiLockPermissionsExTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiLockPermissionsExTupleFields
    {
        MsiLockPermissionsEx,
        LockObject,
        Table,
        SDDLText,
        Condition,
    }

    public class MsiLockPermissionsExTuple : IntermediateTuple
    {
        public MsiLockPermissionsExTuple() : base(TupleDefinitions.MsiLockPermissionsEx, null, null)
        {
        }

        public MsiLockPermissionsExTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiLockPermissionsEx, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiLockPermissionsExTupleFields index] => this.Fields[(int)index];

        public string MsiLockPermissionsEx
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExTupleFields.MsiLockPermissionsEx];
            set => this.Set((int)MsiLockPermissionsExTupleFields.MsiLockPermissionsEx, value);
        }

        public string LockObject
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExTupleFields.LockObject];
            set => this.Set((int)MsiLockPermissionsExTupleFields.LockObject, value);
        }

        public string Table
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExTupleFields.Table];
            set => this.Set((int)MsiLockPermissionsExTupleFields.Table, value);
        }

        public string SDDLText
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExTupleFields.SDDLText];
            set => this.Set((int)MsiLockPermissionsExTupleFields.SDDLText, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExTupleFields.Condition];
            set => this.Set((int)MsiLockPermissionsExTupleFields.Condition, value);
        }
    }
}