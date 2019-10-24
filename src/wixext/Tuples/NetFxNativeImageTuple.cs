// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx.Tuples
{
    using WixToolset.Data;

    public enum NetFxNativeImageTupleFields
    {
        FileRef,
        Priority,
        Attributes,
        ApplicationFileRef,
        ApplicationBaseDirectoryRef,
    }

    public class NetFxNativeImageTuple : IntermediateTuple
    {
        public NetFxNativeImageTuple() : base(NetfxTupleDefinitions.NetFxNativeImage, null, null)
        {
        }

        public NetFxNativeImageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxTupleDefinitions.NetFxNativeImage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNativeImageTupleFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.FileRef].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.FileRef, value);
        }

        public int Priority
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.Priority].AsNumber();
            set => this.Set((int)NetFxNativeImageTupleFields.Priority, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.Attributes].AsNumber();
            set => this.Set((int)NetFxNativeImageTupleFields.Attributes, value);
        }

        public string ApplicationFileRef
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.ApplicationFileRef].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.ApplicationFileRef, value);
        }

        public string ApplicationBaseDirectoryRef
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.ApplicationBaseDirectoryRef].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.ApplicationBaseDirectoryRef, value);
        }
    }
}