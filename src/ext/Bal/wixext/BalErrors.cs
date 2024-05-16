// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
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

        public static Message IuibaNonMsiPrimaryPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaNonMsiPrimaryPackage, "When using WixInternalUIBootstrapperApplication, each primary package must be an MsiPackage.");
        }

        public static Message IuibaNonPermanentNonPrimaryPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaNonPermanentNonPrimaryPackage, "When using WixInternalUIBootstrapperApplication, packages must either be non-permanent and have the bal:PrimaryPackageType attribute, or be permanent and have the bal:PrereqPackage attribute set to 'yes'.");
        }

        public static Message IuibaNonPermanentPrereqPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaNonPermanentPrereqPackage, "When using WixInternalUIBootstrapperApplication and bal:PrereqPackage is set to 'yes', the package must be permanent.");
        }

        public static Message IuibaPermanentPrimaryPackageType(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPermanentPrimaryPackageType, "When using WixInternalUIBootstrapperApplication, packages with the bal:PrimaryPackageType attribute must not be permanent.");
        }

        public static Message IuibaPrimaryPackageEnableFeatureSelection(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPrimaryPackageEnableFeatureSelection, "When using WixInternalUIBootstrapperApplication, primary packages must not have feature selection enabled because it interferes with the user selecting feature through the MSI UI.");
        }

        public static Message MissingPrereq(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MissingPrereq, "There must be at least one package with bal:PrereqPackage=\"yes\" when using the bal:WixPrerequisiteBootstrapperApplication.");
        }

        public static Message MissingIUIPrimaryPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MissingIUIPrimaryPackage, "When using WixInternalUIBootstrapperApplication, there must be one package with bal:PrimaryPackageType=\"default\".");
        }

        public static Message MultipleBAFunctions(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultipleBAFunctions, "WixStandardBootstrapperApplication doesn't support multiple BAFunctions DLLs.");
        }

        public static Message MultiplePrereqLicenses(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrereqLicenses, "There may only be one package in the bundle that has either the PrereqLicenseFile attribute or the PrereqLicenseUrl attribute.");
        }

        public static Message MultiplePrimaryPackageType(SourceLineNumber sourceLineNumbers, string primaryPackageType)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrimaryPackageType, "There may only be one package in the bundle with PrimaryPackageType of '{0}'.", primaryPackageType);
        }

        public static Message MultiplePrimaryPackageType2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrimaryPackageType2, "The location of the package related to the previous error.");
        }

        public static Message OverridableVariableCollision(SourceLineNumber sourceLineNumbers, string name, string collisionName)
        {
            return Message(sourceLineNumbers, Ids.OverridableVariableCollision, "Overridable variable '{0}' collides with '{1}' with Bundle/@CommandLineVariables value 'caseInsensitive'.", name, collisionName);
        }

        public static Message OverridableVariableCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.OverridableVariableCollision2, "The location of the Variable related to the previous error.");
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
            MissingPrereq = 6802,
            MultiplePrereqLicenses = 6803,
            MultipleBAFunctions = 6804,
            BAFunctionsPayloadRequiredInUXContainer = 6805,
            MissingIUIPrimaryPackage = 6808,
            MultiplePrimaryPackageType = 6809,
            MultiplePrimaryPackageType2 = 6810,
            IuibaNonPermanentNonPrimaryPackage = 6811,
            IuibaNonPermanentPrereqPackage = 6812,
            IuibaPermanentPrimaryPackageType = 6813,
            IuibaNonMsiPrimaryPackage = 6814,
            IuibaPrimaryPackageEnableFeatureSelection = 6815,
            OverridableVariableCollision = 6816,
            OverridableVariableCollision2 = 6817,
            MissingDNCBAFactoryAssembly = 6818,
        }
    }
}
