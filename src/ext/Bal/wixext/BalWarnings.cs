// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class BalWarnings
    {
        public static Message UnmarkedBAFunctionsDLL(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.UnmarkedBAFunctionsDLL, "WixStandardBootstrapperApplication doesn't automatically load BAFunctions.dll. Use the bal:BAFunctions attribute to indicate that it should be loaded.");
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
        }
    }
}
