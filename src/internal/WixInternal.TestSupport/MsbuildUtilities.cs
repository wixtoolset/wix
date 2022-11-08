// Copyright(c) .NET Foundation and contributors.All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;

    public enum BuildSystem
    {
        DotNetCoreSdk,
        MSBuild,
        MSBuild64,
    }

    public static class MsbuildUtilities
    {
        public static MsbuildRunnerResult BuildProject(BuildSystem buildSystem, string projectPath, string[] arguments = null, string configuration = "Release", string verbosityLevel = "normal", bool suppressValidation = true)
        {
            var allArgs = new List<string>
            {
                $"-verbosity:{verbosityLevel}",
                $"-p:Configuration={configuration}",
                $"-p:SuppressValidation={suppressValidation}",
                // Node reuse means that child msbuild processes can stay around after the build completes.
                // Under that scenario, the root msbuild does not reliably close its streams which causes us to hang.
                "-nr:false",
                $"-bl:{Path.ChangeExtension(projectPath, ".binlog")}"
            };

            if (arguments != null)
            {
                allArgs.AddRange(arguments);
            }

            switch (buildSystem)
            {
                case BuildSystem.DotNetCoreSdk:
                    {
                        allArgs.Add(projectPath);
                        var result = DotnetRunner.Execute("msbuild", allArgs.ToArray());
                        return new MsbuildRunnerResult
                        {
                            ExitCode = result.ExitCode,
                            Output = result.StandardOutput,
                        };
                    }
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

        public static string GetQuotedPropertySwitch(BuildSystem buildSystem, string propertyName, string valueToQuote)
        {
            switch (buildSystem)
            {
                case BuildSystem.DotNetCoreSdk:
                    {
                        // If the value ends with a backslash, double-escape it (it should end up with four backslashes).
                        if (valueToQuote?.EndsWith("\\") == true)
                        {
                            valueToQuote += @"\\\";
                        }

                        return $"-p:{propertyName}=\\\"{valueToQuote}\\\"";
                    }
                case BuildSystem.MSBuild:
                case BuildSystem.MSBuild64:
                    {
                        // If the value ends with a backslash, escape it.
                        if (valueToQuote?.EndsWith("\\") == true)
                        {
                            valueToQuote += @"\";
                        }

                        return $"-p:{propertyName}=\"{valueToQuote}\"";
                    }
                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public static IEnumerable<string> GetToolCommandLines(MsbuildRunnerResult result, string toolName, string operation, BuildSystem buildSystem)
        {
            var expectedToolExe = buildSystem == BuildSystem.DotNetCoreSdk ? $"{toolName}.dll\"" : $"{toolName}.exe";
            var expectedToolCommand = $"{expectedToolExe} {operation}";
            return result.Output.Where(line => line.Contains(expectedToolCommand));
        }
    }
}
