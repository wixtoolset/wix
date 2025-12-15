// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using Microsoft.Build.Utilities;

    internal class MSBuildLoggerAdapter : ILogger
    {
        private readonly TaskLoggingHelper log;

        public MSBuildLoggerAdapter(TaskLoggingHelper log)
        {
            this.log = log;
        }

        public bool HasLoggedErrors => this.log.HasLoggedErrors;

        public void LogError(string message)
        {
            this.log.LogError(message);
        }

        public void LogWarning(string message)
        {
            this.log.LogWarning(message);
        }
    }
}
