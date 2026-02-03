// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using WixToolset.BaseBuildTasks;

    /// <summary>
    /// An MSBuild task to accept the WiX Toolset EULA.
    /// </summary>
    public sealed class WixAcceptEula : WixExeBaseTask
    {
        [Required]
        public string EulaId { get; set; }

        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendTextUnquoted("eula");
            commandLineBuilder.AppendTextUnquoted(" accept");

            foreach (var eulaId in SplitEulaVersions(this.EulaId))
            {
                commandLineBuilder.AppendTextQuotedIfNotWhitespace(eulaId);
            }

            base.BuildCommandLine(commandLineBuilder);
        }

        private static IEnumerable<string> SplitEulaVersions(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            return value.Split([ ';' ], StringSplitOptions.RemoveEmptyEntries)
                        .Where(v => !String.IsNullOrWhiteSpace(v))
                        .Select(v => v.Trim());
        }
    }
}
