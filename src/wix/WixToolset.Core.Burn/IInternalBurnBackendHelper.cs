// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Xml;
    using WixToolset.Extensibility.Services;

    internal interface IInternalBurnBackendHelper : IBurnBackendHelper
    {
        void WriteBootstrapperApplicationData(XmlWriter writer);

        void WriteBundleExtensionData(XmlWriter writer);
    }
}
