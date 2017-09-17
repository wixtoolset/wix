// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Text;
    using WixToolset.Data;

    public delegate void ResolvedVariableEventHandler(object sender, ResolvedVariableEventArgs e);

    public class ResolvedVariableEventArgs : EventArgs
    {
        private SourceLineNumber sourceLineNumbers;
        private string variableName;
        private string variableValue;

        public ResolvedVariableEventArgs(SourceLineNumber sourceLineNumbers, string variableName, string variableValue)
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.variableName = variableName;
            this.variableValue = variableValue;
        }

        public SourceLineNumber SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }

        public string VariableName
        {
            get { return this.variableName; }
        }

        public string VariableValue
        {
            get { return this.variableValue; }
        }
    }
}
