// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class TestData
    {
        public static string Get(params string[] paths)
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
            return Path.Combine(localPath, Path.Combine(paths));
        }

        public static string GetUnitTestLogsFolder([CallerFilePath] string path = "", [CallerMemberName] string method = "")
        {
            var startingPath = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
            var buildPath = startingPath;

            while (!String.IsNullOrEmpty(buildPath))
            {
                var folderName = Path.GetFileName(buildPath);
                if (String.Equals("build", folderName, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                buildPath = Path.GetDirectoryName(buildPath);
            }

            if (String.IsNullOrEmpty(buildPath))
            {
                throw new InvalidOperationException($"Could not find the 'build' folder in the test path: {startingPath}. Cannot get test logs folder without being able to find the build folder.");
            }

            var testLogsFolder = Path.Combine(buildPath, "logs", "UnitTests", $"{Path.GetFileNameWithoutExtension(path)}_{method}");
            Directory.CreateDirectory(testLogsFolder);

            return testLogsFolder;
        }
    }
}
