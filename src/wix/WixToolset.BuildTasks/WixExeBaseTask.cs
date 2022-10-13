// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to run WiX to update cabinet signatures in a MSI.
    /// </summary>
    public abstract class WixExeBaseTask : BaseToolsetTask
    {
        protected override string ToolName => "wix.exe";
    }
}
