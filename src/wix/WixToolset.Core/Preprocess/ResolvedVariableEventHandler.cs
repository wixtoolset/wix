// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    using System;
    using WixToolset.Data;

    internal delegate void ResolvedVariableEventHandler(object sender, ResolvedVariableEventArgs e);

    internal class ResolvedVariableEventArgs : EventArgs
    {
        public ResolvedVariableEventArgs(SourceLineNumber sourceLineNumbers, string variableName, string variableValue)
        {
            this.SourceLineNumbers = sourceLineNumbers;
            this.VariableName = variableName;
            this.VariableValue = variableValue;
        }

        public SourceLineNumber SourceLineNumbers { get; }

        public string VariableName { get; }

        public string VariableValue { get; }
    }
}
