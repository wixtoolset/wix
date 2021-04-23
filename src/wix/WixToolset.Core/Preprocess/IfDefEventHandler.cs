// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    using System;
    using WixToolset.Data;

    internal delegate void IfDefEventHandler(object sender, IfDefEventArgs e);

    internal class IfDefEventArgs : EventArgs
    {
        public IfDefEventArgs(SourceLineNumber sourceLineNumbers, bool isIfDef, bool isDefined, string variableName)
        {
            this.SourceLineNumbers = sourceLineNumbers;
            this.IsIfDef = isIfDef;
            this.IsDefined = isDefined;
            this.VariableName = variableName;
        }

        public SourceLineNumber SourceLineNumbers { get; }

        public bool IsDefined { get; }

        public bool IsIfDef { get; }

        public string VariableName { get; }
    }
}
