// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class DotnetRunner : ExternalExecutable
    {
        private static readonly object InitLock = new object();
        private static bool Initialized;
        private static DotnetRunner Instance;

        public static ExternalExecutableResult Execute(string command, string[] arguments = null) =>
            InitAndExecute(command, arguments);

        private static ExternalExecutableResult InitAndExecute(string command, string[] arguments)
        {
            lock (InitLock)
            {
                if (!Initialized)
                {
                    Initialized = true;
                    var dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
                    if (String.IsNullOrEmpty(dotnetPath) || !File.Exists(dotnetPath))
                    {
                        dotnetPath = "dotnet";
                    }

                    Instance = new DotnetRunner(dotnetPath);
                }
            }

            return Instance.ExecuteCore(command, arguments);
        }

        private DotnetRunner(string exePath) : base(exePath) { }

        private ExternalExecutableResult ExecuteCore(string command, string[] arguments)
        {
            var total = new List<string>
            {
                command,
            };

            if (arguments != null)
            {
                total.AddRange(arguments);
            }

            var args = CombineArguments(total);
            var mergeErrorIntoOutput = true;
            return this.Run(args, mergeErrorIntoOutput);
        }
    }
}
