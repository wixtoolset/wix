// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;

    internal interface ISearchFacade
    {
        /// <summary>
        /// Writes the search to the Burn manifest.
        /// </summary>
        /// <param name="writer"></param>
        void WriteXml(XmlTextWriter writer);
    }
}
