// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.IO;

    public class VswhereRunner : ExternalExecutable
    {
        private static readonly string VswhereRelativePath = @"Microsoft Visual Studio\Installer\vswhere.exe";

        private static readonly object InitLock = new object();
        private static bool Initialized;
        private static VswhereRunner Instance;

        public static ExternalExecutableResult Execute(string args, bool mergeErrorIntoOutput = false) =>
            InitAndExecute(args, mergeErrorIntoOutput);

        private static ExternalExecutableResult InitAndExecute(string args, bool mergeErrorIntoOutput)
        {
            lock (InitLock)
            {
                if (!Initialized)
                {
                    Initialized = true;
                    var vswherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VswhereRelativePath);
                    if (!File.Exists(vswherePath))
                    {
                        throw new InvalidOperationException($"Failed to find vswhere at: {vswherePath}");
                    }

                    Instance = new VswhereRunner(vswherePath);
                }
            }

            return Instance.Run(args, mergeErrorIntoOutput);
        }

        private VswhereRunner(string exePath) : base(exePath) { }
    }
}
