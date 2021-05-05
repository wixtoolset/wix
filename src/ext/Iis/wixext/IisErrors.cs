// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class IIsErrors
    {
        public static Message DeprecatedBinaryChildElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedBinaryChildElement, "The {0} element contains a deprecated child Binary element.  Please move the Binary element under a Fragment, Module, or Product element and set the {0}/@BinaryKey attribute to the value of the Binary/@Id attribute.", elementName);
        }

        public static Message IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutComponent, "The {0}/@{1} attribute cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName, attributeName);
        }

        public static Message IllegalCharacterInAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, Char illegalCharacter)
        {
            return Message(sourceLineNumbers, Ids.IllegalCharacterInAttributeValue, "The {0}/@{1} attribute's value, '{2}', is invalid.  It cannot contain the character '{3}'.", elementName, attributeName, value, illegalCharacter);
        }

        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalElementWithoutComponent, "The {0} element cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName);
        }

        public static Message MimeMapExtensionMissingPeriod(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.MimeMapExtensionMissingPeriod, "The {0}/@{1} attribute's value, '{2}', is not a valid mime map extension.  It must begin with a period.", elementName, attributeName, attributeValue);
        }

        public static Message OneOfAttributesRequiredUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4)
        {
            return Message(sourceLineNumbers, Ids.OneOfAttributesRequiredUnderComponent, "When nested under a Component, the {0} element must have one of the following attributes specified: {1}, {2}, {3} or {4}.", elementName, attributeName1, attributeName2, attributeName3, attributeName4);
        }

        public static Message RequiredAttributeUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttributeUnderComponent, "The {0}/@{1} attribute must be specified when the element has a Component as an ancestor.", elementName, attributeName);
        }

        public static Message WebApplicationAlreadySpecified(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.WebApplicationAlreadySpecified, "The {0} element can have at most a single WebApplication specified. This can be either through the WebApplication attribute, or through a nested WebApplication element, but not both.", elementName);
        }

        public static Message WebSiteAttributeUnderWebSite(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.WebSiteAttributeUnderWebSite, "The {0}/@WebSite attribute cannot be specified when the {0} element is nested under a WebSite element.", elementName);
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
            MimeMapExtensionMissingPeriod = 5150,
            IllegalAttributeWithoutComponent = 5151,
            IllegalElementWithoutComponent = 5152,
            OneOfAttributesRequiredUnderComponent = 5153,
            WebSiteAttributeUnderWebSite = 5154,
            WebApplicationAlreadySpecified = 5155,
            IllegalCharacterInAttributeValue = 5156,
            DeprecatedBinaryChildElement = 5157,
            RequiredAttributeUnderComponent = 5161,
        }
    }
}
