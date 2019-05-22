// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Registry = new IntermediateTupleDefinition(
            TupleDefinitionType.Registry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.ValueType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.ValueAction), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegistryTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(RegistryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RegistryTupleFields
    {
        Root,
        Key,
        Name,
        Value,
        ValueType,
        ValueAction,
        ComponentRef,
    }

    public enum RegistryValueType
    {
        String,
        Binary,
        Expandable,
        Integer,
        MultiString,
    }

    public enum RegistryValueActionType
    {
        Write,
        Append,
        Prepend,
    }

    public class RegistryTuple : IntermediateTuple
    {
        public RegistryTuple() : base(TupleDefinitions.Registry, null, null)
        {
        }

        public RegistryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Registry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegistryTupleFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RegistryTupleFields.Root].AsNumber();
            set => this.Set((int)RegistryTupleFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Key];
            set => this.Set((int)RegistryTupleFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RegistryTupleFields.Name];
            set => this.Set((int)RegistryTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)RegistryTupleFields.Value].AsString();
            set => this.Set((int)RegistryTupleFields.Value, value);
        }

        public RegistryValueType ValueType
        {
            get => (RegistryValueType)this.Fields[(int)RegistryTupleFields.ValueType].AsNumber();
            set => this.Set((int)RegistryTupleFields.ValueType, (int)value);
        }

        public RegistryValueActionType ValueAction
        {
            get => (RegistryValueActionType)this.Fields[(int)RegistryTupleFields.ValueAction].AsNumber();
            set => this.Set((int)RegistryTupleFields.ValueAction, (int)value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)RegistryTupleFields.ComponentRef];
            set => this.Set((int)RegistryTupleFields.ComponentRef, value);
        }
    }
}