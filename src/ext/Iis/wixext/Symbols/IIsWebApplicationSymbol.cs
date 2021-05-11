// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebApplication = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.Isolation), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.AllowSessions), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.SessionTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.Buffer), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.ParentPaths), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.DefaultScript), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.ScriptTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.ServerDebugging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.ClientDebugging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationSymbolFields.AppPoolRef), IntermediateFieldType.String),
            },
            typeof(IIsWebApplicationSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebApplicationSymbolFields
    {
        Name,
        Isolation,
        AllowSessions,
        SessionTimeout,
        Buffer,
        ParentPaths,
        DefaultScript,
        ScriptTimeout,
        ServerDebugging,
        ClientDebugging,
        AppPoolRef,
    }

    public class IIsWebApplicationSymbol : IntermediateSymbol
    {
        public IIsWebApplicationSymbol() : base(IisSymbolDefinitions.IIsWebApplication, null, null)
        {
        }

        public IIsWebApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebApplicationSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.Name].AsString();
            set => this.Set((int)IIsWebApplicationSymbolFields.Name, value);
        }

        public int Isolation
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.Isolation].AsNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.Isolation, value);
        }

        public int? AllowSessions
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.AllowSessions].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.AllowSessions, value);
        }

        public int? SessionTimeout
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.SessionTimeout].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.SessionTimeout, value);
        }

        public int? Buffer
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.Buffer].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.Buffer, value);
        }

        public int? ParentPaths
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.ParentPaths].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.ParentPaths, value);
        }

        public string DefaultScript
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.DefaultScript].AsString();
            set => this.Set((int)IIsWebApplicationSymbolFields.DefaultScript, value);
        }

        public int? ScriptTimeout
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.ScriptTimeout].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.ScriptTimeout, value);
        }

        public int? ServerDebugging
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.ServerDebugging].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.ServerDebugging, value);
        }

        public int? ClientDebugging
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.ClientDebugging].AsNullableNumber();
            set => this.Set((int)IIsWebApplicationSymbolFields.ClientDebugging, value);
        }

        public string AppPoolRef
        {
            get => this.Fields[(int)IIsWebApplicationSymbolFields.AppPoolRef].AsString();
            set => this.Set((int)IIsWebApplicationSymbolFields.AppPoolRef, value);
        }
    }
}