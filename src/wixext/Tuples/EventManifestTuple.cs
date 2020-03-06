// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition EventManifest = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.EventManifest.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(EventManifestTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(EventManifestTupleFields.File), IntermediateFieldType.String),
            },
            typeof(EventManifestTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum EventManifestTupleFields
    {
        ComponentRef,
        File,
    }

    public class EventManifestTuple : IntermediateTuple
    {
        public EventManifestTuple() : base(UtilTupleDefinitions.EventManifest, null, null)
        {
        }

        public EventManifestTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.EventManifest, sourceLineNumber, id)
        {
        }

        public IntermediateField this[EventManifestTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)EventManifestTupleFields.ComponentRef].AsString();
            set => this.Set((int)EventManifestTupleFields.ComponentRef, value);
        }

        public string File
        {
            get => this.Fields[(int)EventManifestTupleFields.File].AsString();
            set => this.Set((int)EventManifestTupleFields.File, value);
        }
    }
}