// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.PowerShell
{
    using System.Resources;
    using WixToolset.Data;

    public static class PSWarnings
    {
        public static Message DeprecatedAssemblyNameAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAssemblyNameAttribute, "The SnapIn/@AssemblyName attribute is deprecated. It is assigned automatically.");
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
            DeprecatedAssemblyNameAttribute = 5350,
        }
    }
}
