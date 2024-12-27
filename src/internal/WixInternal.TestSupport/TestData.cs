// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.TestSupport
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class TestData
    {
        public static void CreateFile(string path, long size, bool fill = false)
        {
            // Ensure the directory exists.
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var file = File.OpenWrite(path))
            {
                if (fill)
                {
                    var random = new Random();
                    var bytes = new byte[4096];
                    var generated = 0L;

                    // Put fill bytes in the file so it doesn't compress trivially.
                    while (generated < size)
                    {
                        var generate = (int)Math.Min(size - generated, bytes.Length);

                        random.NextBytes(bytes);

                        file.Write(bytes, 0, generate);

                        generated += generate;
                    }
                }
                else
                {
                    file.SetLength(size);
                }
            }
        }

        public static string Get(params string[] paths)
        {
            var localPath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(localPath, Path.Combine(paths));
        }

        public static string GetUnitTestLogsFolder([CallerFilePath] string path = "", [CallerMemberName] string method = "")
        {
            var startingPath = AppDomain.CurrentDomain.BaseDirectory;
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
