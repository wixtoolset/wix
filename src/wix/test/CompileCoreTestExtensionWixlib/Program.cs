// Copyright(c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using WixInternal.Core.TestPackage;

namespace CompileCoreTestExtensionWixlib
{
    // We want to be able to test Core with extensions, but there's no easy way to build an extension without Tools.
    // So we have this helper exe.
    public class Program
    {
        public static void Main(string[] args)
        {
            var intermediateFolder = args[0];
            var wixlibPath = args[1];

            var buildArgs = new List<string>
            {
              "build",
              "-acceptEula", "wix" + SomeVerInfo.Major,
              "-bindfiles",
              "-intermediateFolder", intermediateFolder,
              "-o", wixlibPath
            };

            foreach (var path in args[2].Split(';'))
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(path));

                buildArgs.Add("-bindpath");
                buildArgs.Add(folder);

                buildArgs.Add(path);
            }

            var result = WixRunner.Execute(buildArgs.ToArray());

            result.AssertSuccess();
        }
    }
}
