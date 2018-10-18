// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Extensibility.Data;

    internal interface IPreprocessor
    {
        XDocument Preprocess(IPreprocessContext context);

        XDocument Preprocess(IPreprocessContext context, XmlReader reader);
    }
}
