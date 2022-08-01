// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class CompilerWarnings
    {
        public static Message DirectoryRefStandardDirectoryDeprecated(SourceLineNumber sourceLineNumbers, string directoryId)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRefStandardDirectoryDeprecated, "Using DirectoryRef to reference the standard directory '{0}' is deprecated. Use the StandardDirectory element instead.", directoryId);
        }

        public static Message DefiningStandardDirectoryDeprecated(SourceLineNumber sourceLineNumbers, string directoryId)
        {
            return Message(sourceLineNumbers, Ids.DefiningStandardDirectoryDeprecated, "It is no longer necessary to define the standard directory '{0}'. Use the StandardDirectory element instead.", directoryId);
        }

        public static Message DiscouragedVersionAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedVersionAttribute, "The Provides/@Version attribute should not be specified in an MSI package. The ProductVersion will be used by default.");
        }

        public static Message DiscouragedVersionAttribute(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedVersionAttribute, "The Provides/@Version attribute should not be specified for MSI package {0}. The ProductVersion will be used by default.", id);
        }

        public static Message PropertyRemoved(string name)
        {
            return Message(null, Ids.PropertyRemoved, "The property {0} was authored in the package with a value and will be removed. The property should not be authored.", name);
        }

        public static Message ProvidesKeyNotFound(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.ProvidesKeyNotFound, "The provider key with identifier {0} was not found in the Wix4DependencyProvider table. Related registry rows will not be removed from authoring.", id);
        }

        public static Message ReadonlyLogVariableTarget(SourceLineNumber sourceLineNumbers, string element, string attribute, string name)
        {
            return Message(sourceLineNumbers, Ids.ReadonlyLogVariableTarget, "The {0}/@{1} attribute's value references the well-known log Variable '{2}' to change its value. This variable is set by the engine and is intended to be read-only. Change your attribute's value to reference a custom variable.", element, attribute, name);
        }

        public static Message RequiresKeyNotFound(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.RequiresKeyNotFound, "The dependency key with identifier {0} was not found in the Wix4Dependency table. Related registry rows will not be removed from authoring.", id);
        }

        public static Message ReservedBurnNamespaceWarning(SourceLineNumber sourceLineNumbers, string element, string attribute, string prefix)
        {
            return Message(sourceLineNumbers, Ids.ReservedBurnNamespaceWarning, "The {0}/@{1} attribute's value begins with the reserved prefix '{2}'. Some prefixes are reserved by the WiX toolset for well-known values. Change your attribute's value to not begin with the same prefix.", element, attribute, prefix);
        }

        public static Message Win64Component(SourceLineNumber sourceLineNumbers, string componentId)
        {
            return Message(sourceLineNumbers, Ids.Win64Component, "The Provides element should not be authored in the 64-bit component with identifier {0}. The dependency feature may not work if installing this package on 64-bit Windows operating systems prior to Windows 7 and Windows Server 2008 R2. Set the Component/@Bitness attribute to \"always32\" to ensure the dependency feature works correctly on legacy operating systems.", componentId);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            ProvidesKeyNotFound = 5431,
            RequiresKeyNotFound = 5432,
            PropertyRemoved = 5433,
            DiscouragedVersionAttribute = 5434,
            Win64Component = 5435,
            DirectoryRefStandardDirectoryDeprecated = 5436,
            DefiningStandardDirectoryDeprecated = 5437,
            ReadonlyLogVariableTarget = 5438,
            ReservedBurnNamespaceWarning = 5439,
        } // 5400-5499 and 6600-6699 were the ranges for Dependency and Tag which are now in Core between CompilerWarnings and CompilerErrors.
    }
}
