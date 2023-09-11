// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using WixToolset.Data;

    public enum IisSymbolDefinitionType
    {
        Certificate,
        CertificateHash,
        IIsAppPool,
        IIsFilter,
        IIsHttpHeader,
        IIsMimeMap,
        IIsProperty,
        IIsWebAddress,
        IIsWebApplication,
        IIsWebApplicationExtension,
        IIsWebDir,
        IIsWebDirProperties,
        IIsWebError,
        IIsWebLog,
        IIsWebServiceExtension,
        IIsWebSite,
        IIsWebSiteCertificates,
        IIsWebVirtualDir,
    }

    public static partial class IisSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out IisSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(IisSymbolDefinitionType type)
        {
            switch (type)
            {
                case IisSymbolDefinitionType.Certificate:
                    return IisSymbolDefinitions.Certificate;

                case IisSymbolDefinitionType.CertificateHash:
                    return IisSymbolDefinitions.CertificateHash;

                case IisSymbolDefinitionType.IIsAppPool:
                    return IisSymbolDefinitions.IIsAppPool;

                case IisSymbolDefinitionType.IIsFilter:
                    return IisSymbolDefinitions.IIsFilter;

                case IisSymbolDefinitionType.IIsHttpHeader:
                    return IisSymbolDefinitions.IIsHttpHeader;

                case IisSymbolDefinitionType.IIsMimeMap:
                    return IisSymbolDefinitions.IIsMimeMap;

                case IisSymbolDefinitionType.IIsProperty:
                    return IisSymbolDefinitions.IIsProperty;

                case IisSymbolDefinitionType.IIsWebAddress:
                    return IisSymbolDefinitions.IIsWebAddress;

                case IisSymbolDefinitionType.IIsWebApplication:
                    return IisSymbolDefinitions.IIsWebApplication;

                case IisSymbolDefinitionType.IIsWebApplicationExtension:
                    return IisSymbolDefinitions.IIsWebApplicationExtension;

                case IisSymbolDefinitionType.IIsWebDir:
                    return IisSymbolDefinitions.IIsWebDir;

                case IisSymbolDefinitionType.IIsWebDirProperties:
                    return IisSymbolDefinitions.IIsWebDirProperties;

                case IisSymbolDefinitionType.IIsWebError:
                    return IisSymbolDefinitions.IIsWebError;

                case IisSymbolDefinitionType.IIsWebLog:
                    return IisSymbolDefinitions.IIsWebLog;

                case IisSymbolDefinitionType.IIsWebServiceExtension:
                    return IisSymbolDefinitions.IIsWebServiceExtension;

                case IisSymbolDefinitionType.IIsWebSite:
                    return IisSymbolDefinitions.IIsWebSite;

                case IisSymbolDefinitionType.IIsWebSiteCertificates:
                    return IisSymbolDefinitions.IIsWebSiteCertificates;

                case IisSymbolDefinitionType.IIsWebVirtualDir:
                    return IisSymbolDefinitions.IIsWebVirtualDir;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
