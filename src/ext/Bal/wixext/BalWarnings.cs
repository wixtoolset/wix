// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class BalWarnings
    {
        public static Message IuibaForceCachePrereq(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaForceCachePrereq, "WixInternalUIBootstrapperApplication does not support the value of 'force' for Cache on prereq packages. Prereq packages are only cached when they need to be installed.");
        }

        public static Message IuibaPrereqPackageAfterPrimaryPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPrereqPackageAfterPrimaryPackage, "When using WixInternalUIBootstrapperApplication, all prereq packages should be before the primary package in the chain. The prereq packages are always installed before the primary package.");
        }

        public static Message IuibaPrimaryPackageDisplayInternalUICondition(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPrimaryPackageDisplayInternalUICondition, "WixInternalUIBootstrapperApplication ignores DisplayInternalUICondition for the primary package so that the MSI UI is always shown.");
        }

        public static Message IuibaPrimaryPackageDisplayFilesInUseDialogCondition(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPrimaryPackageDisplayFilesInUseDialogCondition, "WixInternalUIBootstrapperApplication ignores DisplayFilesInUseDialogCondition for the primary package so that the MSI UI is always shown.");
        }

        public static Message IuibaPrimaryPackageInstallCondition(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IuibaPrimaryPackageInstallCondition, "WixInternalUIBootstrapperApplication ignores InstallCondition for the primary package so that the MSI UI is always shown.");
        }

        public static Message UnmarkedBAFunctionsDLL(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.UnmarkedBAFunctionsDLL, "WixStandardBootstrapperApplication doesn't automatically load BAFunctions.dll. Use the bal:BAFunctions attribute to indicate that it should be loaded.");
        }

        public static Message DeprecatedBAFactoryAssemblyAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedBAFactoryAssemblyAttribute, "The {0}/@{1} attribute has been deprecated. Move the Payload/@SourceFile attribute to be the BootstrapperApplication/@SourceFile attribute and remove the Payload element.", elementName, attributeName);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            UnmarkedBAFunctionsDLL = 6501,
            IuibaForceCachePrereq = 6502,
            IuibaPrimaryPackageInstallCondition = 6503,
            IuibaPrimaryPackageDisplayInternalUICondition = 6504,
            IuibaPrereqPackageAfterPrimaryPackage = 6505,
            DeprecatedBAFactoryAssemblyAttribute = 6506,
            IuibaPrimaryPackageDisplayFilesInUseDialogCondition = 6507,
        }
    }
}
