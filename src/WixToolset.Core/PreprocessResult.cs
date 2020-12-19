// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Extensibility.Data;

    internal class PreprocessResult : IPreprocessResult
    {
        public XDocument Document { get; set; }

        public IEnumerable<IIncludedFile> IncludedFiles { get; set; }
    }
}
