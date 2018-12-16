// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using System.Resources;
    using WixToolset.Data;

    public static class SqlErrors
    {
        public static Message IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutComponent, "The {0}/@{1} attribute cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName, attributeName);
        }
        
        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalElementWithoutComponent, "The {0} element cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName);
        }
        
        public static Message OneOfAttributesRequiredUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4)
        {
            return Message(sourceLineNumbers, Ids.OneOfAttributesRequiredUnderComponent, "When nested under a Component, the {0} element must have one of the following attributes specified: {1}, {2}, {3} or {4}.", elementName, attributeName1, attributeName2, attributeName3, attributeName4);
        }
        
        public static Message DeprecatedBinaryChildElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedBinaryChildElement, "The {0} element contains a deprecated child Binary element.  Please move the Binary element under a Fragment, Module, or Product element and set the {0}/@BinaryKey attribute to the value of the Binary/@Id attribute.", elementName);
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
            IllegalAttributeWithoutComponent = 5100,
            IllegalElementWithoutComponent = 5101,
            OneOfAttributesRequiredUnderComponent = 5102,
            DeprecatedBinaryChildElement = 5103,
        }
    }
}