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
                MsbuildUtilities.GetQuotedSwitch(buildSystem, "bl", Path.ChangeExtension(projectPath, ".binlog"))
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

        public static string GetQuotedSwitch(BuildSystem _, string switchName, string switchValue)
        {
            // If the value ends with a backslash, escape it.
            if (switchValue?.EndsWith("\\") == true)
            {
                switchValue += @"\";
            }

            return $"-{switchName}:\"{switchValue}\"";
        }

        public static string GetQuotedPropertySwitch(BuildSystem buildSystem, string propertyName, string propertyValue)
        {
            // If the value ends with a backslash, escape it.
            if (propertyValue?.EndsWith("\\") == true)
            {
                propertyValue += @"\";
            }

            var quotedValue = "\"" + propertyValue + "\"";

            // If the value contains a semicolon then escape-quote it (wrap with the characters: \") to wrap the value
            // instead of just quoting the value, otherwise dotnet.exe will not pass the value to MSBuild correctly.
            if (buildSystem == BuildSystem.DotNetCoreSdk && propertyValue?.IndexOf(';') > -1)
            {
                quotedValue = "\\\"" + propertyValue + "\\\"";
            }

            return $"-p:{propertyName}={quotedValue}";
        }

        public static IEnumerable<string> GetToolCommandLines(MsbuildRunnerResult result, string toolName, string operation, BuildSystem buildSystem)
        {
            var expectedToolExe = buildSystem == BuildSystem.DotNetCoreSdk ? $"{toolName}.dll\"" : $"{toolName}.exe";
            var expectedToolCommand = $"{expectedToolExe} {operation}";
            return result.Output.Where(line => line.Contains(expectedToolCommand));
        }
    }
}
