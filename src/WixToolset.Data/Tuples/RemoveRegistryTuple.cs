// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RemoveRegistry = new IntermediateTupleDefinition(
            TupleDefinitionType.RemoveRegistry,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveRegistryTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(RemoveRegistryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RemoveRegistryTupleFields
    {
        Root,
        Key,
        Name,
        Action,
        Component_,
    }

    public enum RemoveRegistryActionType
    {
        RemoveOnInstall,
        RemoveOnUninstall
    };

    public class RemoveRegistryTuple : IntermediateTuple
    {
        public RemoveRegistryTuple() : base(TupleDefinitions.RemoveRegistry, null, null)
        {
        }

        public RemoveRegistryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RemoveRegistry, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveRegistryTupleFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RemoveRegistryTupleFields.Key]?.AsNumber();
            set => this.Set((int)RemoveRegistryTupleFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Key]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Name]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Name, value);
        }

        public RemoveRegistryActionType Action
        {
            get => (RemoveRegistryActionType)this.Fields[(int)RemoveRegistryTupleFields.Action].AsNumber();
            set => this.Set((int)RemoveRegistryTupleFields.Action, (int)value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)RemoveRegistryTupleFields.Component_]?.Value;
            set => this.Set((int)RemoveRegistryTupleFields.Component_, value);
        }
    }
}