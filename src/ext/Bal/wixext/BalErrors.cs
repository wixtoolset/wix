// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class BalErrors
    {
        public static Message AttributeRequiresPrereqPackage(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.AttributeRequiresPrereqPackage, "When the {0}/@{1} attribute is specified, the {0}/@PrereqPackage attribute must be set to \"yes\".", elementName, attributeName);
        }

        public static Message BAFunctionsPayloadRequiredInUXContainer(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.BAFunctionsPayloadRequiredInUXContainer, "The BAFunctions DLL Payload element must be located inside the BootstrapperApplication container.");
        }

        public static Message MissingDNCPrereq()
        {
            return Message(null, Ids.MissingDNCPrereq, "There must be at least one PrereqPackage when using the DotNetCoreBootstrapperApplicationHost with SelfContainedDeployment set to \"no\".");
        }

        public static Message MissingMBAPrereq()
        {
            return Message(null, Ids.MissingMBAPrereq, "There must be at least one PrereqPackage when using the ManagedBootstrapperApplicationHost.\nThis is typically done by using the WixNetFxExtension and referencing one of the NetFxAsPrereq package groups.");
        }

        public static Message MultipleBAFunctions(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultipleBAFunctions, "WixStandardBootstrapperApplication doesn't support multiple BAFunctions DLLs.");
        }

        public static Message MultiplePrereqLicenses(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrereqLicenses, "There may only be one package in the bundle that has either the PrereqLicenseFile attribute or the PrereqLicenseUrl attribute.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            AttributeRequiresPrereqPackage = 6801,
            MissingMBAPrereq = 6802,
            MultiplePrereqLicenses = 6803,
            MultipleBAFunctions = 6804,
            BAFunctionsPayloadRequiredInUXContainer = 6805,
            MissingDNCPrereq = 6806,
        }
    }
}
