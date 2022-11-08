// Copyright(c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Collections.Generic;
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

            var buildArgs = new List<string>();
            buildArgs.Add("build");
            buildArgs.Add("-bindfiles");
            buildArgs.Add("-bindpath");
            buildArgs.Add("Data");
            buildArgs.Add("-intermediateFolder");
            buildArgs.Add(intermediateFolder);
            buildArgs.Add("-o");
            buildArgs.Add(wixlibPath);

            foreach (var path in args[2].Split(';'))
            {
                buildArgs.Add(path);
            }

            var result = WixRunner.Execute(buildArgs.ToArray());

            result.AssertSuccess();
        }
    }
}
