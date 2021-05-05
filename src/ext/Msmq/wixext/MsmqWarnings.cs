// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class MsmqWarnings
    {
        public static Message MissingComponents(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MissingComponents, "The MsmqAssembly element has a Type attribute with a value of 'native', but the element does not contain any MsmqComponent elements. All components contained in a native assembly must be listed, or they will not be correctly removed during uninstall.");
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
            MissingComponents = 6007,
        }
    }
}
