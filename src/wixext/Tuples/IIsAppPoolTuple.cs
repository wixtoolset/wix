// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsAppPool = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsAppPool.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.RecycleMinutes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.RecycleRequests), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.RecycleTimes), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.IdleTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.QueueLimit), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.CPUMon), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.MaxProc), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.VirtualMemory), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.PrivateMemory), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.ManagedRuntimeVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsAppPoolTupleFields.ManagedPipelineMode), IntermediateFieldType.String),
            },
            typeof(IIsAppPoolTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsAppPoolTupleFields
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

    public class IIsAppPoolTuple : IntermediateTuple
    {
        public IIsAppPoolTuple() : base(IisTupleDefinitions.IIsAppPool, null, null)
        {
        }

        public IIsAppPoolTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsAppPool, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsAppPoolTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.Name].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.Name, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.ComponentRef].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.ComponentRef, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.Attributes].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.Attributes, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.UserRef].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.UserRef, value);
        }

        public int RecycleMinutes
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.RecycleMinutes].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.RecycleMinutes, value);
        }

        public int RecycleRequests
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.RecycleRequests].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.RecycleRequests, value);
        }

        public string RecycleTimes
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.RecycleTimes].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.RecycleTimes, value);
        }

        public int IdleTimeout
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.IdleTimeout].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.IdleTimeout, value);
        }

        public int QueueLimit
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.QueueLimit].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.QueueLimit, value);
        }

        public string CPUMon
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.CPUMon].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.CPUMon, value);
        }

        public int MaxProc
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.MaxProc].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.MaxProc, value);
        }

        public int VirtualMemory
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.VirtualMemory].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.VirtualMemory, value);
        }

        public int PrivateMemory
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.PrivateMemory].AsNumber();
            set => this.Set((int)IIsAppPoolTupleFields.PrivateMemory, value);
        }

        public string ManagedRuntimeVersion
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.ManagedRuntimeVersion].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.ManagedRuntimeVersion, value);
        }

        public string ManagedPipelineMode
        {
            get => this.Fields[(int)IIsAppPoolTupleFields.ManagedPipelineMode].AsString();
            set => this.Set((int)IIsAppPoolTupleFields.ManagedPipelineMode, value);
        }
    }
}