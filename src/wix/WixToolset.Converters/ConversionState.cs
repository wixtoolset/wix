// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Data;

    internal enum ConvertOperation
    {
        Convert,
        Format,
    }


    internal class ConversionState
    {
        public ConversionState(ConvertOperation operation, string sourceFile)
        {
            this.ConversionMessages = new List<Message>();
            this.Operation = operation;
            this.SourceFile = sourceFile;
            this.SourceVersion = 0;
        }

        public List<Message> ConversionMessages { get; }

        public ConvertOperation Operation { get; }

        public string SourceFile { get; }

        public int SourceVersion { get; set; }

        public XDocument XDocument { get; set; }

        public void Initialize()
        {
            this.XDocument = XDocument.Load(this.SourceFile, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }

        public void Initialize(XDocument document)
        {
            this.XDocument = document;
        }
    }
}
