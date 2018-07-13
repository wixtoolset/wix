// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System.Collections;
    using System.Diagnostics;
    using Microsoft.Build.Framework;

    internal class FakeBuildEngine : IBuildEngine
    {
        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => "fake_wix.targets";

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new System.NotImplementedException();

        public void LogCustomEvent(CustomBuildEventArgs e) => Debug.Write(e.Message);

        public void LogErrorEvent(BuildErrorEventArgs e) => Debug.Write(e.Message);

        public void LogMessageEvent(BuildMessageEventArgs e) => Debug.Write(e.Message);

        public void LogWarningEvent(BuildWarningEventArgs e) => Debug.Write(e.Message);
    }
}
