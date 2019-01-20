// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using System;
    using WixToolset.Data;

    public enum HttpTupleDefinitionType
    {
        WixHttpUrlAce,
        WixHttpUrlReservation,
    }

    public static partial class HttpTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out HttpTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(HttpTupleDefinitionType type)
        {
            switch (type)
            {
                case HttpTupleDefinitionType.WixHttpUrlAce:
                    return HttpTupleDefinitions.WixHttpUrlAce;

                case HttpTupleDefinitionType.WixHttpUrlReservation:
                    return HttpTupleDefinitions.WixHttpUrlReservation;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
