// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Component = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Component,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Location), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.DisableRegistryReflection), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.NeverOverwrite), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Permanent), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.SharedDllRefCount), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Shared), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Transitive), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.UninstallWhenSuperseded), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Win64), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.KeyPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.KeyPathType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComponentSymbolFields.WiX3CompatibleGuid), IntermediateFieldType.Bool),
            },
            typeof(ComponentSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ComponentSymbolFields
    {
        ComponentId,
        DirectoryRef,
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
        WiX3CompatibleGuid,
    }

    public enum ComponentLocation
    {
        LocalOnly,
        SourceOnly,
        Either
    }

    public class ComponentSymbol : IntermediateSymbol
    {
        public ComponentSymbol() : base(SymbolDefinitions.Component, null, null)
        {
        }

        public ComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Component, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComponentSymbolFields index] => this.Fields[(int)index];

        public string ComponentId
        {
            get => (string)this.Fields[(int)ComponentSymbolFields.ComponentId];
            set => this.Set((int)ComponentSymbolFields.ComponentId, value);
        }

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)ComponentSymbolFields.DirectoryRef];
            set => this.Set((int)ComponentSymbolFields.DirectoryRef, value);
        }

        public ComponentLocation Location
        {
            get => (ComponentLocation)this.Fields[(int)ComponentSymbolFields.Location].AsNumber();
            set => this.Set((int)ComponentSymbolFields.Location, (int)value);
        }

        public bool DisableRegistryReflection
        {
            get => this.Fields[(int)ComponentSymbolFields.DisableRegistryReflection].AsBool();
            set => this.Set((int)ComponentSymbolFields.DisableRegistryReflection, value);
        }

        public bool NeverOverwrite
        {
            get => this.Fields[(int)ComponentSymbolFields.NeverOverwrite].AsBool();
            set => this.Set((int)ComponentSymbolFields.NeverOverwrite, value);
        }

        public bool Permanent
        {
            get => this.Fields[(int)ComponentSymbolFields.Permanent].AsBool();
            set => this.Set((int)ComponentSymbolFields.Permanent, value);
        }

        public bool SharedDllRefCount
        {
            get => this.Fields[(int)ComponentSymbolFields.SharedDllRefCount].AsBool();
            set => this.Set((int)ComponentSymbolFields.SharedDllRefCount, value);
        }

        public bool Shared
        {
            get => this.Fields[(int)ComponentSymbolFields.Shared].AsBool();
            set => this.Set((int)ComponentSymbolFields.Shared, value);
        }

        public bool Transitive
        {
            get => this.Fields[(int)ComponentSymbolFields.Transitive].AsBool();
            set => this.Set((int)ComponentSymbolFields.Transitive, value);
        }

        public bool UninstallWhenSuperseded
        {
            get => this.Fields[(int)ComponentSymbolFields.UninstallWhenSuperseded].AsBool();
            set => this.Set((int)ComponentSymbolFields.UninstallWhenSuperseded, value);
        }

        public bool Win64
        {
            get => this.Fields[(int)ComponentSymbolFields.Win64].AsBool();
            set => this.Set((int)ComponentSymbolFields.Win64, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)ComponentSymbolFields.Condition];
            set => this.Set((int)ComponentSymbolFields.Condition, value);
        }

        public string KeyPath
        {
            get => (string)this.Fields[(int)ComponentSymbolFields.KeyPath];
            set => this.Set((int)ComponentSymbolFields.KeyPath, value);
        }

        public ComponentKeyPathType KeyPathType
        {
            get => (ComponentKeyPathType)this.Fields[(int)ComponentSymbolFields.KeyPathType].AsNumber();
            set => this.Set((int)ComponentSymbolFields.KeyPathType, (int)value);
        }

        public bool WiX3CompatibleGuid
        {
            get => this.Fields[(int)ComponentSymbolFields.WiX3CompatibleGuid].AsBool();
            set => this.Set((int)ComponentSymbolFields.WiX3CompatibleGuid, value);
        }
    }
}
