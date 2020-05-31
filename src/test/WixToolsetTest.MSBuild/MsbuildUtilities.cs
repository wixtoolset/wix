// Copyright(c) .NET Foundation and contributors.All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixBuildTools.TestSupport;

    public enum BuildSystem
    {
        MSBuild,
        MSBuild64,
    }

    public static class MsbuildUtilities
    {
        public static readonly string WixPropsPath = Path.Combine(new Uri(typeof(MsbuildUtilities).Assembly.CodeBase).AbsolutePath, "..", "..", "publish", "WixToolset.MSBuild", "build", "WixToolset.MSBuild.props");

        public static MsbuildRunnerResult BuildProject(BuildSystem buildSystem, string projectPath, params string[] arguments)
        {
            var allArgs = new List<string>
            {
                $"-p:WixMSBuildProps={MsbuildUtilities.WixPropsPath}",
                // Node reuse means that child msbuild processes can stay around after the build completes.
                // Under that scenario, the root msbuild does not reliably close its streams which causes us to hang.
                "-nr:false",
            };

            if (arguments != null)
            {
                allArgs.AddRange(arguments);
            }

            switch (buildSystem)
            {
                case BuildSystem.MSBuild:
                case BuildSystem.MSBuild64:
                    {
                        return MsbuildRunner.Execute(projectPath, allArgs.ToArray(), buildSystem == BuildSystem.MSBuild64);
                    }
                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }
    }
}
