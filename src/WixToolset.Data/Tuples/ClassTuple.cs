// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Class = new IntermediateTupleDefinition(
            TupleDefinitionType.Class,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ClassTupleFields.CLSID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.Context), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.DefaultProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.AppIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.FileTypeMask), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.IconRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.IconIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.DefInprocHandler), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassTupleFields.RelativePath), IntermediateFieldType.Bool),
            },
            typeof(ClassTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ClassTupleFields
    {
        CLSID,
        Context,
        ComponentRef,
        DefaultProgIdRef,
        Description,
        AppIdRef,
        FileTypeMask,
        IconRef,
        IconIndex,
        DefInprocHandler,
        Argument,
        FeatureRef,
        RelativePath,
    }

    public class ClassTuple : IntermediateTuple
    {
        public ClassTuple() : base(TupleDefinitions.Class, null, null)
        {
        }

        public ClassTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Class, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ClassTupleFields index] => this.Fields[(int)index];

        public string CLSID
        {
            get => (string)this.Fields[(int)ClassTupleFields.CLSID];
            set => this.Set((int)ClassTupleFields.CLSID, value);
        }

        public string Context
        {
            get => (string)this.Fields[(int)ClassTupleFields.Context];
            set => this.Set((int)ClassTupleFields.Context, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ClassTupleFields.ComponentRef];
            set => this.Set((int)ClassTupleFields.ComponentRef, value);
        }

        public string DefaultProgIdRef
        {
            get => (string)this.Fields[(int)ClassTupleFields.DefaultProgIdRef];
            set => this.Set((int)ClassTupleFields.DefaultProgIdRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ClassTupleFields.Description];
            set => this.Set((int)ClassTupleFields.Description, value);
        }

        public string AppIdRef
        {
            get => (string)this.Fields[(int)ClassTupleFields.AppIdRef];
            set => this.Set((int)ClassTupleFields.AppIdRef, value);
        }

        public string FileTypeMask
        {
            get => (string)this.Fields[(int)ClassTupleFields.FileTypeMask];
            set => this.Set((int)ClassTupleFields.FileTypeMask, value);
        }

        public string IconRef
        {
            get => (string)this.Fields[(int)ClassTupleFields.IconRef];
            set => this.Set((int)ClassTupleFields.IconRef, value);
        }

        public int IconIndex
        {
            get => (int)this.Fields[(int)ClassTupleFields.IconIndex];
            set => this.Set((int)ClassTupleFields.IconIndex, value);
        }

        public string DefInprocHandler
        {
            get => (string)this.Fields[(int)ClassTupleFields.DefInprocHandler];
            set => this.Set((int)ClassTupleFields.DefInprocHandler, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)ClassTupleFields.Argument];
            set => this.Set((int)ClassTupleFields.Argument, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)ClassTupleFields.FeatureRef];
            set => this.Set((int)ClassTupleFields.FeatureRef, value);
        }

        public bool RelativePath
        {
            get => this.Fields[(int)ClassTupleFields.RelativePath].AsBool();
            set => this.Set((int)ClassTupleFields.RelativePath, value);
        }
    }
}