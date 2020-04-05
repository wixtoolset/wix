// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebApplication = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.Isolation), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.AllowSessions), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.SessionTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.Buffer), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.ParentPaths), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.DefaultScript), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.ScriptTimeout), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.ServerDebugging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.ClientDebugging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationTupleFields.AppPoolRef), IntermediateFieldType.String),
            },
            typeof(IIsWebApplicationTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebApplicationTupleFields
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

    public class IIsWebApplicationTuple : IntermediateTuple
    {
        public IIsWebApplicationTuple() : base(IisTupleDefinitions.IIsWebApplication, null, null)
        {
        }

        public IIsWebApplicationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebApplicationTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.Name].AsString();
            set => this.Set((int)IIsWebApplicationTupleFields.Name, value);
        }

        public int Isolation
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.Isolation].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.Isolation, value);
        }

        public int AllowSessions
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.AllowSessions].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.AllowSessions, value);
        }

        public int SessionTimeout
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.SessionTimeout].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.SessionTimeout, value);
        }

        public int Buffer
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.Buffer].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.Buffer, value);
        }

        public int ParentPaths
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.ParentPaths].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.ParentPaths, value);
        }

        public string DefaultScript
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.DefaultScript].AsString();
            set => this.Set((int)IIsWebApplicationTupleFields.DefaultScript, value);
        }

        public int ScriptTimeout
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.ScriptTimeout].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.ScriptTimeout, value);
        }

        public int ServerDebugging
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.ServerDebugging].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.ServerDebugging, value);
        }

        public int ClientDebugging
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.ClientDebugging].AsNumber();
            set => this.Set((int)IIsWebApplicationTupleFields.ClientDebugging, value);
        }

        public string AppPoolRef
        {
            get => this.Fields[(int)IIsWebApplicationTupleFields.AppPoolRef].AsString();
            set => this.Set((int)IIsWebApplicationTupleFields.AppPoolRef, value);
        }
    }
}