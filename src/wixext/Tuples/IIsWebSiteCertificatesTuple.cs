// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebSiteCertificates = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebSiteCertificates.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesTupleFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebSiteCertificatesTupleFields.CertificateRef), IntermediateFieldType.String),
            },
            typeof(IIsWebSiteCertificatesTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebSiteCertificatesTupleFields
    {
        WebRef,
        CertificateRef,
    }

    public class IIsWebSiteCertificatesTuple : IntermediateTuple
    {
        public IIsWebSiteCertificatesTuple() : base(IisTupleDefinitions.IIsWebSiteCertificates, null, null)
        {
        }

        public IIsWebSiteCertificatesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebSiteCertificates, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebSiteCertificatesTupleFields index] => this.Fields[(int)index];

        public string WebRef
        {
            get => this.Fields[(int)IIsWebSiteCertificatesTupleFields.WebRef].AsString();
            set => this.Set((int)IIsWebSiteCertificatesTupleFields.WebRef, value);
        }

        public string CertificateRef
        {
            get => this.Fields[(int)IIsWebSiteCertificatesTupleFields.CertificateRef].AsString();
            set => this.Set((int)IIsWebSiteCertificatesTupleFields.CertificateRef, value);
        }
    }
}