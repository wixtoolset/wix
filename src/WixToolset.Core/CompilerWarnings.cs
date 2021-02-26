// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class CompilerWarnings
    {
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
            return Message(sourceLineNumbers, Ids.ProvidesKeyNotFound, "The provider key with identifier {0} was not found in the WixDependencyProvider table. Related registry rows will not be removed from authoring.", id);
        }

        public static Message RequiresKeyNotFound(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.RequiresKeyNotFound, "The dependency key with identifier {0} was not found in the WixDependency table. Related registry rows will not be removed from authoring.", id);
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
        }
    }
}
