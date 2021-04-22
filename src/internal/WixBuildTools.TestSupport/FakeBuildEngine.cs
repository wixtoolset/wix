// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System.Collections;
    using System.Text;
    using Microsoft.Build.Framework;

    public class FakeBuildEngine : IBuildEngine
    {
        private readonly StringBuilder output = new StringBuilder();

        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => "fake_wix.targets";

        public string Output => this.output.ToString();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new System.NotImplementedException();

        public void LogCustomEvent(CustomBuildEventArgs e) => this.output.AppendLine(e.Message);

        public void LogErrorEvent(BuildErrorEventArgs e) => this.output.AppendLine(e.Message);

        public void LogMessageEvent(BuildMessageEventArgs e) => this.output.AppendLine(e.Message);

        public void LogWarningEvent(BuildWarningEventArgs e) => this.output.AppendLine(e.Message);
    }
}
