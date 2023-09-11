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
    }

    public static partial class HttpSymbolDefinitions
    {
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

                case HttpSymbolDefinitionType.WixHttpUrlAce:
                    return HttpSymbolDefinitions.WixHttpUrlAce;

                case HttpSymbolDefinitionType.WixHttpUrlReservation:
                    return HttpSymbolDefinitions.WixHttpUrlReservation;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
