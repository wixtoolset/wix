// Copyright(c) .NET Foundation and contributors.All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;

    public enum BuildSystem
    {
        DotNetCoreSdk,
        MSBuild,
        MSBuild64,
    }

    public static class MsbuildUtilities
    {
        private static string _wixMsbuildPath = null;
        public static string WixMsbuildPath
        {
            get
            {
                if (_wixMsbuildPath == null)
#if !(NET461 || NET472 || NET48 || NETCOREAPP3_1 || NET5_0)
                {
                    throw new System.NotImplementedException();
                }
#else
                {
#if NET461 || NET472 || NET48
                    _wixMsbuildPath = Path.Combine(Path.GetDirectoryName((new Uri(typeof(MsbuildUtilities).Assembly.CodeBase)).LocalPath), "..", "publish", "WixToolset.Sdk");
#else // NETCOREAPP3_1 || NET5_0
                    _wixMsbuildPath = Path.Combine(Path.GetDirectoryName(typeof(MsbuildUtilities).Assembly.Location), "..", "publish", "WixToolset.Sdk");
#endif
                }
#endif

                return _wixMsbuildPath;
            }
        }

        private static string _wixPropsPath = null;

        public static string WixPropsPath
        {
            get
            {
#if !(NET461 || NET472 || NET48 || NETCOREAPP3_1 || NET5_0)
                {
                    throw new System.NotImplementedException();
                }
#else
                {
                    _wixPropsPath = Path.Combine(WixMsbuildPath, "build", "WixToolset.Sdk.props");
                }
#endif
                return _wixPropsPath;
            }
        }

        public static MsbuildRunnerResult BuildProject(BuildSystem buildSystem, string projectPath, string[] arguments = null, string configuration = "Release", bool? outOfProc = null, string verbosityLevel = "normal", bool suppressValidation = true)
        {
            var allArgs = new List<string>
            {
                $"-verbosity:{verbosityLevel}",
                $"-p:Configuration={configuration}",
                $"-p:SuppressValidation={suppressValidation}",
                GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildUtilities.WixPropsPath),
                // Node reuse means that child msbuild processes can stay around after the build completes.
                // Under that scenario, the root msbuild does not reliably close its streams which causes us to hang.
                "-nr:false",
                $"-bl:{Path.ChangeExtension(projectPath, ".binlog")}"
            };

            if (outOfProc.HasValue)
            {
                allArgs.Add($"-p:RunWixToolsOutOfProc={outOfProc.Value}");
            }

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
                        return $"-p:{propertyName}=\\\"{valueToQuote}\\\"";
                    }
                case BuildSystem.MSBuild:
                case BuildSystem.MSBuild64:
                    {
                        return $"-p:{propertyName}=\"{valueToQuote}\"";
                    }
                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public static IEnumerable<string> GetToolCommandLines(MsbuildRunnerResult result, string toolName, string operation, BuildSystem buildSystem, bool? outOfProc = null)
        {
            var expectedOutOfProc = buildSystem == BuildSystem.DotNetCoreSdk || outOfProc.HasValue && outOfProc.Value;
            var expectedToolExe = !expectedOutOfProc ? $"({toolName}.exe)" :
                                  buildSystem == BuildSystem.DotNetCoreSdk ? $"{toolName}.dll\"" : $"{toolName}.exe";
            var expectedToolCommand = $"{expectedToolExe} {operation}";
            return result.Output.Where(line => line.Contains(expectedToolCommand));
        }
    }
}
