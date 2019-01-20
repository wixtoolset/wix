// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Tuples;

    public static partial class HttpTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixHttpUrlReservation = new IntermediateTupleDefinition(
            HttpTupleDefinitionType.WixHttpUrlReservation.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationTupleFields.WixHttpUrlReservation), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationTupleFields.HandleExisting), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationTupleFields.Sddl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationTupleFields.Url), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(WixHttpUrlReservationTuple));
    }
}

namespace WixToolset.Http.Tuples
{
    using WixToolset.Data;

    public enum WixHttpUrlReservationTupleFields
    {
        WixHttpUrlReservation,
        HandleExisting,
        Sddl,
        Url,
        Component_,
    }

    public class WixHttpUrlReservationTuple : IntermediateTuple
    {
        public WixHttpUrlReservationTuple() : base(HttpTupleDefinitions.WixHttpUrlReservation, null, null)
        {
        }

        public WixHttpUrlReservationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpTupleDefinitions.WixHttpUrlReservation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpUrlReservationTupleFields index] => this.Fields[(int)index];

        public string WixHttpUrlReservation
        {
            get => this.Fields[(int)WixHttpUrlReservationTupleFields.WixHttpUrlReservation].AsString();
            set => this.Set((int)WixHttpUrlReservationTupleFields.WixHttpUrlReservation, value);
        }

        public int HandleExisting
        {
            get => this.Fields[(int)WixHttpUrlReservationTupleFields.HandleExisting].AsNumber();
            set => this.Set((int)WixHttpUrlReservationTupleFields.HandleExisting, value);
        }

        public string Sddl
        {
            get => this.Fields[(int)WixHttpUrlReservationTupleFields.Sddl].AsString();
            set => this.Set((int)WixHttpUrlReservationTupleFields.Sddl, value);
        }

        public string Url
        {
            get => this.Fields[(int)WixHttpUrlReservationTupleFields.Url].AsString();
            set => this.Set((int)WixHttpUrlReservationTupleFields.Url, value);
        }

        public string Component_
        {
            get => this.Fields[(int)WixHttpUrlReservationTupleFields.Component_].AsString();
            set => this.Set((int)WixHttpUrlReservationTupleFields.Component_, value);
        }
    }
}