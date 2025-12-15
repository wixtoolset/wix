// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System.Collections.Generic;
    using WixToolset.BuildTasks;

    public class FakeMsbuildLogger : ILogger
    {
        public List<string> Messages { get; } = new List<string>();

        public bool HasLoggedErrors { get; private set; }

        public void LogError(string message)
        {
            this.HasLoggedErrors = true;

            this.Messages.Add("Error: " + message);
        }

        public void LogWarning(string message)
        {
            this.Messages.Add("Warning: " + message);
        }
    }
}
