// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{ 
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;

    internal class WindowsInstallerDecompileResult : IWindowsInstallerDecompileResult
    {
        public WindowsInstallerData Data { get; set; }

        public XDocument Document { get; set; }

        public IList<string> ExtractedFilePaths { get; set; }

        public Platform? Platform { get; set; }
    }
}
