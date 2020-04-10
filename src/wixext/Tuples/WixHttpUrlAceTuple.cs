// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Tuples;

    public static partial class HttpTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixHttpUrlAce = new IntermediateTupleDefinition(
            HttpTupleDefinitionType.WixHttpUrlAce.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceTupleFields.WixHttpUrlReservationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceTupleFields.SecurityPrincipal), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceTupleFields.Rights), IntermediateFieldType.Number),
            },
            typeof(WixHttpUrlAceTuple));
    }
}

namespace WixToolset.Http.Tuples
{
    using WixToolset.Data;

    public enum WixHttpUrlAceTupleFields
    {
        WixHttpUrlReservationRef,
        SecurityPrincipal,
        Rights,
    }

    public class WixHttpUrlAceTuple : IntermediateTuple
    {
        public WixHttpUrlAceTuple() : base(HttpTupleDefinitions.WixHttpUrlAce, null, null)
        {
        }

        public WixHttpUrlAceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpTupleDefinitions.WixHttpUrlAce, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpUrlAceTupleFields index] => this.Fields[(int)index];

        public string WixHttpUrlReservationRef
        {
            get => this.Fields[(int)WixHttpUrlAceTupleFields.WixHttpUrlReservationRef].AsString();
            set => this.Set((int)WixHttpUrlAceTupleFields.WixHttpUrlReservationRef, value);
        }

        public string SecurityPrincipal
        {
            get => this.Fields[(int)WixHttpUrlAceTupleFields.SecurityPrincipal].AsString();
            set => this.Set((int)WixHttpUrlAceTupleFields.SecurityPrincipal, value);
        }

        public int Rights
        {
            get => this.Fields[(int)WixHttpUrlAceTupleFields.Rights].AsNumber();
            set => this.Set((int)WixHttpUrlAceTupleFields.Rights, value);
        }
    }
}