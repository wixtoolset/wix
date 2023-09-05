// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.IO;
    using WixToolset.Extensibility;

    internal class WindowsInstallerBackendFactory : IBackendFactory
    {
        public bool TryCreateBackend(string outputType, string outputFile, out IBackend backend)
        {
            if (String.IsNullOrEmpty(outputType))
            {
                outputType = Path.GetExtension(outputFile);
            }

            switch (outputType?.ToLowerInvariant())
            {
                case "module":
                case "msm":
                case ".msm":
                    backend = new MsmBackend();
                    return true;

                case "msipackage":
                case "package":
                case "product":
                case "msi":
                case ".msi":
                    backend = new MsiBackend();
                    return true;

                case "msppackage":
                case "patch":
                case "msp":
                case ".msp":
                    backend = new MspBackend();
                    return true;

                //case "patchcreation":
                //case ".pcp":
                //    return new PatchCreationBackend();
            }

            backend = null;
            return false;
        }
    }
}
