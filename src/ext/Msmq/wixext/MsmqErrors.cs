// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class MsmqErrors
    {
        public static Message IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutComponent, "The {0}/@{1} attribute cannot be specified unless the element has a component as an ancestor. A {0} that does not have a component ancestor is not installed.", elementName, attributeName);
        }

        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalElementWithoutComponent, "The {0} element cannot be specified unless the element has a component as an ancestor. A {0} that does not have a component ancestor is not installed.", elementName);
        }

        public static Message RequiredAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttribute, "A {0} element must have either a {1} attribute or a {2} attribute, or both set.", elementName, attributeName1, attributeName2);
        }

        public static Message RequiredAttributeNotUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttributeNotUnderComponent, "A {0} element not nested under a component must have either a {1} attribute or a {2} attribute, or both set.", elementName, attributeName1, attributeName2);
        }

        public static Message RequiredAttributeUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttributeUnderComponent, "The {0}/@{1} attribute must be provided when {0} element is nested under a component.", elementName, attributeName);
        }

        public static Message UnexpectedAttributeWithOtherValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAttributeWithOtherValue, "The {0}/@{1} attribute cannot coexist with the {2} attribute's value of '{3}'.", elementName, attributeName, otherAttributeName, otherValue);
        }

        public static Message UnexpectedAttributeWithOtherValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAttributeWithOtherValue, "The {0}/@{1} attribute's value, '{2}', cannot coexist with the {3} attribute's value of '{4}'.", elementName, attributeName, value, otherAttributeName, otherValue);
        }

        public static Message UnexpectedAttributeWithoutOtherValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAttributeWithoutOtherValue, "The {0}/@{1} cannot be provided unless the {2} attribute is provided with a value of '{3}'.", elementName, attributeName, otherAttributeName, otherValue);
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
            IllegalAttributeWithoutComponent = 6000,
            IllegalElementWithoutComponent = 6001,
            UnexpectedAttributeWithOtherValue = 6002,
            UnexpectedAttributeWithoutOtherValue = 6003,
            RequiredAttributeUnderComponent = 6004,
            RequiredAttribute = 6005,
            RequiredAttributeNotUnderComponent = 6006,
        }
    }
}
