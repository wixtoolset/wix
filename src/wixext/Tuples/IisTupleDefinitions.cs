// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using WixToolset.Data;

    public enum IisTupleDefinitionType
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

    public static partial class IisTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out IisTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(IisTupleDefinitionType type)
        {
            switch (type)
            {
                case IisTupleDefinitionType.Certificate:
                    return IisTupleDefinitions.Certificate;

                case IisTupleDefinitionType.CertificateHash:
                    return IisTupleDefinitions.CertificateHash;

                case IisTupleDefinitionType.IIsAppPool:
                    return IisTupleDefinitions.IIsAppPool;

                case IisTupleDefinitionType.IIsFilter:
                    return IisTupleDefinitions.IIsFilter;

                case IisTupleDefinitionType.IIsHttpHeader:
                    return IisTupleDefinitions.IIsHttpHeader;

                case IisTupleDefinitionType.IIsMimeMap:
                    return IisTupleDefinitions.IIsMimeMap;

                case IisTupleDefinitionType.IIsProperty:
                    return IisTupleDefinitions.IIsProperty;

                case IisTupleDefinitionType.IIsWebAddress:
                    return IisTupleDefinitions.IIsWebAddress;

                case IisTupleDefinitionType.IIsWebApplication:
                    return IisTupleDefinitions.IIsWebApplication;

                case IisTupleDefinitionType.IIsWebApplicationExtension:
                    return IisTupleDefinitions.IIsWebApplicationExtension;

                case IisTupleDefinitionType.IIsWebDir:
                    return IisTupleDefinitions.IIsWebDir;

                case IisTupleDefinitionType.IIsWebDirProperties:
                    return IisTupleDefinitions.IIsWebDirProperties;

                case IisTupleDefinitionType.IIsWebError:
                    return IisTupleDefinitions.IIsWebError;

                case IisTupleDefinitionType.IIsWebLog:
                    return IisTupleDefinitions.IIsWebLog;

                case IisTupleDefinitionType.IIsWebServiceExtension:
                    return IisTupleDefinitions.IIsWebServiceExtension;

                case IisTupleDefinitionType.IIsWebSite:
                    return IisTupleDefinitions.IIsWebSite;

                case IisTupleDefinitionType.IIsWebSiteCertificates:
                    return IisTupleDefinitions.IIsWebSiteCertificates;

                case IisTupleDefinitionType.IIsWebVirtualDir:
                    return IisTupleDefinitions.IIsWebVirtualDir;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
