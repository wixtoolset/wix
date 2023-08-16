// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using System;
    using WixToolset.Data;

    public enum HttpSymbolDefinitionType
    {
        WixHttpSniSslCert,
        WixHttpUrlAce,
        WixHttpUrlReservation,
        WixHttpSslBinding,
        WixHttpCertificate,
        WixHttpSslBindingCertificates,
        WixHttpCertificateHash
    }

    public static partial class HttpSymbolDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out HttpSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(HttpSymbolDefinitionType type)
        {
            switch (type)
            {
                case HttpSymbolDefinitionType.WixHttpSniSslCert:
                    return HttpSymbolDefinitions.WixHttpSniSslCert;

                case HttpSymbolDefinitionType.WixHttpSslBinding:
                    return HttpSymbolDefinitions.WixHttpSslBinding;

                case HttpSymbolDefinitionType.WixHttpUrlAce:
                    return HttpSymbolDefinitions.WixHttpUrlAce;

                case HttpSymbolDefinitionType.WixHttpUrlReservation:
                    return HttpSymbolDefinitions.WixHttpUrlReservation;

                case HttpSymbolDefinitionType.WixHttpCertificate:
                    return HttpSymbolDefinitions.WixHttpCertificate;

                case HttpSymbolDefinitionType.WixHttpSslBindingCertificates:
                    return HttpSymbolDefinitions.WixHttpSslBindingCertificates;

                case HttpSymbolDefinitionType.WixHttpCertificateHash:
                    return HttpSymbolDefinitions.WixHttpCertificateHash;    
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
