// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Component = new IntermediateTupleDefinition(
            TupleDefinitionType.Component,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Location), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.DisableRegistryReflection), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.NeverOverwrite), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Permanent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.SharedDllRefCount), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Shared), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Transitive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.UninstallWhenSuperseded), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Win64), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.KeyPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentTupleFields.KeyPathType), IntermediateFieldType.Number),
            },
            typeof(ComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ComponentTupleFields
    {
        ComponentId,
        Directory_,
        Location,
        DisableRegistryReflection,
        NeverOverwrite,
        Permanent,
        SharedDllRefCount,
        Shared,
        Transitive,
        UninstallWhenSuperseded,
        Win64,
        Condition,
        KeyPath,
        KeyPathType,
    }

    public enum ComponentLocation
    {
        LocalOnly,
        SourceOnly,
        Either
    }

    public class ComponentTuple : IntermediateTuple
    {
        public ComponentTuple() : base(TupleDefinitions.Component, null, null)
        {
        }

        public ComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Component, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComponentTupleFields index] => this.Fields[(int)index];

        public string ComponentId
        {
            get => (string)this.Fields[(int)ComponentTupleFields.ComponentId];
            set => this.Set((int)ComponentTupleFields.ComponentId, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)ComponentTupleFields.Directory_];
            set => this.Set((int)ComponentTupleFields.Directory_, value);
        }

        public ComponentLocation Location
        {
            get => (ComponentLocation)this.Fields[(int)ComponentTupleFields.Location].AsNumber();
            set => this.Set((int)ComponentTupleFields.Location, (int)value);
        }

        public bool DisableRegistryReflection
        {
            get => this.Fields[(int)ComponentTupleFields.DisableRegistryReflection].AsBool();
            set => this.Set((int)ComponentTupleFields.DisableRegistryReflection, value);
        }

        public bool NeverOverwrite
        {
            get => this.Fields[(int)ComponentTupleFields.NeverOverwrite].AsBool();
            set => this.Set((int)ComponentTupleFields.NeverOverwrite, value);
        }

        public bool Permanent
        {
            get => this.Fields[(int)ComponentTupleFields.Permanent].AsBool();
            set => this.Set((int)ComponentTupleFields.Permanent, value);
        }

        public bool SharedDllRefCount
        {
            get => this.Fields[(int)ComponentTupleFields.SharedDllRefCount].AsBool();
            set => this.Set((int)ComponentTupleFields.SharedDllRefCount, value);
        }

        public bool Shared
        {
            get => this.Fields[(int)ComponentTupleFields.Shared].AsBool();
            set => this.Set((int)ComponentTupleFields.Shared, value);
        }

        public bool Transitive
        {
            get => this.Fields[(int)ComponentTupleFields.Transitive].AsBool();
            set => this.Set((int)ComponentTupleFields.Transitive, value);
        }

        public bool UninstallWhenSuperseded
        {
            get => this.Fields[(int)ComponentTupleFields.UninstallWhenSuperseded].AsBool();
            set => this.Set((int)ComponentTupleFields.UninstallWhenSuperseded, value);
        }

        public bool Win64
        {
            get => this.Fields[(int)ComponentTupleFields.Win64].AsBool();
            set => this.Set((int)ComponentTupleFields.Win64, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ComponentTupleFields.Condition];
            set => this.Set((int)ComponentTupleFields.Condition, value);
        }

        public string KeyPath
        {
            get => (string)this.Fields[(int)ComponentTupleFields.KeyPath];
            set => this.Set((int)ComponentTupleFields.KeyPath, value);
        }

        public ComponentKeyPathType KeyPathType
        {
            get => (ComponentKeyPathType)this.Fields[(int)ComponentTupleFields.KeyPathType].AsNumber();
            set => this.Set((int)ComponentTupleFields.KeyPathType, (int)value);
        }
    }
}