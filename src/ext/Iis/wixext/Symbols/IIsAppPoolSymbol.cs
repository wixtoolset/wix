// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsAppPool = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsAppPool.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.RecycleMinutes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.RecycleRequests), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.RecycleTimes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.IdleTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.QueueLimit), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.CPUMon), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.MaxProc), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.VirtualMemory), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.PrivateMemory), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.ManagedRuntimeVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolSymbolFields.ManagedPipelineMode), IntermediateFieldType.String),
            },
            typeof(IIsAppPoolSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsAppPoolSymbolFields
    {
        Name,
        ComponentRef,
        Attributes,
        UserRef,
        RecycleMinutes,
        RecycleRequests,
        RecycleTimes,
        IdleTimeout,
        QueueLimit,
        CPUMon,
        MaxProc,
        VirtualMemory,
        PrivateMemory,
        ManagedRuntimeVersion,
        ManagedPipelineMode,
    }

    public class IIsAppPoolSymbol : IntermediateSymbol
    {
        public IIsAppPoolSymbol() : base(IisSymbolDefinitions.IIsAppPool, null, null)
        {
        }

        public IIsAppPoolSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsAppPool, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsAppPoolSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.Name].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.Name, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.ComponentRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.Attributes, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.UserRef].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.UserRef, value);
        }

        public int? RecycleMinutes
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.RecycleMinutes].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.RecycleMinutes, value);
        }

        public int? RecycleRequests
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.RecycleRequests].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.RecycleRequests, value);
        }

        public string RecycleTimes
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.RecycleTimes].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.RecycleTimes, value);
        }

        public int? IdleTimeout
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.IdleTimeout].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.IdleTimeout, value);
        }

        public int? QueueLimit
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.QueueLimit].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.QueueLimit, value);
        }

        public string CPUMon
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.CPUMon].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.CPUMon, value);
        }

        public int? MaxProc
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.MaxProc].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.MaxProc, value);
        }

        public int? VirtualMemory
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.VirtualMemory].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.VirtualMemory, value);
        }

        public int? PrivateMemory
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.PrivateMemory].AsNullableNumber();
            set => this.Set((int)IIsAppPoolSymbolFields.PrivateMemory, value);
        }

        public string ManagedRuntimeVersion
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.ManagedRuntimeVersion].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.ManagedRuntimeVersion, value);
        }

        public string ManagedPipelineMode
        {
            get => this.Fields[(int)IIsAppPoolSymbolFields.ManagedPipelineMode].AsString();
            set => this.Set((int)IIsAppPoolSymbolFields.ManagedPipelineMode, value);
        }
    }
}