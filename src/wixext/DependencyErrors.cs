// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class DependencyErrors
    {
        public static Message IllegalCharactersInProvider(SourceLineNumber sourceLineNumbers, string attributeName, Char illegalChar, string illegalChars)
        {
            return Message(sourceLineNumbers, Ids.IllegalCharactersInProvider, "The provider key authored into the {0} attribute contains an illegal character, '{1}'. Please author the provider key without any of the following characters: {2}", attributeName, illegalChar, illegalChars);
        }

        public static Message ReservedValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.ReservedValue, "The {0}/@{1} attribute value '{2}' is reserved and cannot be used here. Please choose a different value.", elementName, attributeName, attributeValue);
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
            IllegalCharactersInProvider = 5400,
            ReservedValue = 5401,
        }
    }
}
