// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class UtilWarnings
    {
        public static Message DeprecatedPerfCounterElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedPerfCounterElement, "The PerfCounter element has been deprecated.  Please use the PerformanceCounter element instead.");
        }

        public static Message RequiredAttributeForWindowsXP(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttributeForWindowsXP, "The {0}/@{1} attribute must be specified to successfully install on Windows XP.  You can ignore this warning if this installation does not install on Windows XP.", elementName, attributeName);
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
            DeprecatedPerfCounterElement = 5153,
            RequiredAttributeForWindowsXP = 5154,
        }
    }
}
