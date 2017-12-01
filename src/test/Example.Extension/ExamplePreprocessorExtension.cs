// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    internal class ExamplePreprocessorExtension : IPreprocessorExtension
    {
        public ExamplePreprocessorExtension()
        {
        }

        public IPreprocessorCore Core { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string[] Prefixes => throw new NotImplementedException();

        public string EvaluateFunction(string prefix, string function, string[] args)
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            throw new NotImplementedException();
        }

        public string GetVariableValue(string prefix, string name)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void PreprocessDocument(XDocument document)
        {
            throw new NotImplementedException();
        }

        public string PreprocessParameter(string name)
        {
            throw new NotImplementedException();
        }

        public bool ProcessPragma(SourceLineNumber sourceLineNumbers, string prefix, string pragma, string args, XContainer parent)
        {
            throw new NotImplementedException();
        }
    }
}