// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Resources;

    public static class ErrorMessages
    {








        public static Message CorruptFileFormat(string path, string format)
        {
            return Message(null, Ids.CorruptFileFormat, "Attempted to load corrupt file from path: {0}. The file with format {1} contained unexpected content. Ensure the correct path was provided and that the file has not been incorrectly modified.", path, format.ToLowerInvariant());
        }

        public static Message ExpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttribute, "The {0}/@{1} attribute was not found; it is required.", elementName, attributeName);
        }

        public static Message ExpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attribute1Name, string attribute2Name, bool eitherOr)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttribute, "The {0} element must have a value for exactly one of the {1} or {2} attributes.", elementName, attribute1Name, attribute2Name, eitherOr);
        }

        public static Message ExpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttribute, "The {0}/@{1} attribute was not found; it is required when attribute {2} is specified.", elementName, attributeName, otherAttributeName);
        }

        public static Message ExpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttribute, "The {0}/@{1} attribute was not found; it is required when attribute {2} has a value of '{3}'.", elementName, attributeName, otherAttributeName, otherAttributeValue);
        }

        public static Message ExpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherAttributeValue, bool otherAttributeValueUnless)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttribute, "The {0}/@{1} attribute was not found; it is required unless the attribute {2} has a value of '{3}'.", elementName, attributeName, otherAttributeName, otherAttributeValue, otherAttributeValueUnless);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1} or {2} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1}, {2}, or {3} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2, attributeName3);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1}, {2}, {3}, or {4} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2, attributeName3, attributeName4);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1}, {2}, {3}, {4}, or {5} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5, string attributeName6)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1}, {2}, {3}, {4}, {5}, or {6} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5, attributeName6);
        }

        public static Message ExpectedAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5, string attributeName6, string attributeName7)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributes, "The {0} element's {1}, {2}, {3}, {4}, {5}, {6}, or {7} attribute was not found; one of these is required.", elementName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5, attributeName6, attributeName7);
        }

        public static Message ExpectedAttributesWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributesWithOtherAttribute, "The {0} element's {1} or {2} attribute was not found; at least one of these attributes must be specified.", elementName, attributeName1, attributeName2);
        }

        public static Message ExpectedAttributesWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributesWithOtherAttribute, "The {0} element's {1} or {2} attribute was not found; one of these is required when attribute {3} is present.", elementName, attributeName1, attributeName2, otherAttributeName);
        }

        public static Message ExpectedAttributesWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributesWithOtherAttribute, "The {0} element's {1} or {2} attribute was not found; one of these is required when attribute {3} has a value of '{4}'.", elementName, attributeName1, attributeName2, otherAttributeName, otherAttributeValue);
        }

        public static Message ExpectedAttributeInElementOrParent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeInElementOrParent, "The {0}/@{1} attribute was not found or empty; it is required unless it is specified in the parent element.", elementName, attributeName);
        }

        public static Message ExpectedAttributeInElementOrParent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeInElementOrParent, "The {0}/@{1} attribute was not found or empty; it is required, or it can be specified in the parent {2} element.", elementName, attributeName, parentElementName);
        }

        public static Message ExpectedAttributeInElementOrParent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElementName, string parentAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeInElementOrParent, "The {0}/@{1} attribute was not found or empty; it is required, or it can be specified in the parent {2}/@{3} attribute.", elementName, attributeName, parentElementName, parentAttributeName);
        }

        public static Message ExpectedAttributeWithValueWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWithValueWithOtherAttribute, "The {0}/@{1} attribute is required to have a value when attribute {2} is present.", elementName, attributeName, attributeName2);
        }

        public static Message ExpectedAttributeOrElementWithOtherAttribute(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string childElement, string otherAttribute)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeOrElementWithOtherAttribute, "Element '{0}' missing attribute '{1}' or child element '{2}'. Exactly one of those is required when attribute '{3}' is specified.", parentElement, attribute, childElement, otherAttribute);
        }

        public static Message ExpectedAttributeOrElementWithOtherAttribute(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string childElement, string otherAttribute, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeOrElementWithOtherAttribute, "Element '{0}' missing attribute '{1}' or child element '{2}'. Exactly one of those is required when attribute '{3}' is specified with value '{4}'.", parentElement, attribute, childElement, otherAttribute, otherAttributeValue);
        }

        public static Message ExpectedAttributeOrElementWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string childElement, string otherAttribute)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeOrElementWithoutOtherAttribute, "Element '{0}' missing attribute '{1}' or child element '{2}'. Exactly one of those is required when attribute '{3}' is not specified.", parentElement, attribute, childElement, otherAttribute);
        }

        public static Message ExpectedAttributesWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributesWithoutOtherAttribute, "The {0} element's {1} or {2} attribute was not found; one of these is required without attribute {3} present.", elementName, attributeName1, attributeName2, otherAttributeName);
        }

        public static Message ExpectedAttributeWhenElementNotUnderElement(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWhenElementNotUnderElement, "The '{0}/@{1}' attribute was not found; it is required when element '{0}' is not nested under a '{2}' element.", elementName, attributeName, parentElementName);
        }

        public static Message ExpectedAttributeWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWithoutOtherAttributes, "The {0} element's {1} attribute was not found; it is required without attribute {2} present.", elementName, attributeName, otherAttributeName);
        }

        public static Message ExpectedElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedElement, "A {0} element must have at least one child element.", elementName);
        }

        public static Message ExpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedElement, "A {0} element must have at least one child element of type {1}.", elementName, childName);
        }

        public static Message ExpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childName1, string childName2)
        {
            return Message(sourceLineNumbers, Ids.ExpectedElement, "A {0} element must have at least one child element of type {1} or {2}.", elementName, childName1, childName2);
        }

        public static Message ExpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childName1, string childName2, string childName3)
        {
            return Message(sourceLineNumbers, Ids.ExpectedElement, "A {0} element must have at least one child element of type {1}, {2}, or {3}.", elementName, childName1, childName2, childName3);
        }

        public static Message ExpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childName1, string childName2, string childName3, string childName4)
        {
            return Message(sourceLineNumbers, Ids.ExpectedElement, "A {0} element must have at least one child element of type {1}, {2}, {3}, or {4}.", elementName, childName1, childName2, childName3, childName4);
        }

        public static Message ExpectedParentWithAttribute(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string grandparentElement)
        {
            return Message(sourceLineNumbers, Ids.ExpectedParentWithAttribute, "When the {0}/@{1} attribute is specified, the {0} element must be nested under a {2} element.", parentElement, attribute, grandparentElement);
        }

        public static Message FileNotFound(SourceLineNumber sourceLineNumbers, string file)
        {
            return Message(sourceLineNumbers, Ids.FileNotFound, "Cannot find the file '{0}'.", file);
        }

        public static Message FileNotFound(SourceLineNumber sourceLineNumbers, string file, string fileType)
        {
            return Message(sourceLineNumbers, Ids.FileNotFound, "Cannot find the {0} file '{1}'.", fileType, file);
        }

        public static Message FileNotFound(SourceLineNumber sourceLineNumbers, string file, string fileType, IEnumerable<string> checkedPaths)
        {
            var combinedCheckedPaths = String.Join(", ", checkedPaths);
            var fileTypePrefix = String.IsNullOrEmpty(fileType) ? String.Empty : fileType + " ";
            return Message(sourceLineNumbers, Ids.FileNotFound, "Cannot find the {0}file '{1}'. The following paths were checked: {2}", fileTypePrefix, file, combinedCheckedPaths);
        }

        public static Message GenericReadNotAllowed(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.GenericReadNotAllowed, "Permission elements cannot have GenericRead as the only permission specified. Include at least one other permission.");
        }

        public static Message IllegalAttributeExceptOnElement(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string expectedElementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeExceptOnElement, "The {1} attribute can only be specified on the {2} element.", elementName, attributeName, expectedElementName);
        }

        public static Message IllegalAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, params string[] legalValues)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValue, "The {0}/@{1} attribute's value, '{2}', is not one of the legal options: '{3}'.", elementName, attributeName, value, String.Join(", ", legalValues));
        }

        public static Message IllegalAttributeValueWhenNested(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attrivuteValue, string parentElementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWhenNested, "The {0}/@{1} attribute value, '{2}', cannot be specified when the {0} element is nested underneath a {3} element.", elementName, attributeName, attrivuteValue, parentElementName);
        }

        public static Message IllegalAttributeValueWithLegalList(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string legalValueList)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithLegalList, "The {0}/@{1} attribute's value, '{2}', is not one of the legal options: {3}.", elementName, attributeName, value, legalValueList);
        }

        public static Message IllegalAttributeValueWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithOtherAttribute, "The {0}/@{1} attribute's value, '{2}', cannot be specified with attribute {3} present.", elementName, attributeName, attributeValue, otherAttributeName);
        }

        public static Message IllegalAttributeValueWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithOtherAttribute, "The {0}/@{1} attribute's value, '{2}', cannot be specified with attribute {3} present with value '{4}'.", elementName, attributeName, attributeValue, otherAttributeName, otherAttributeValue);
        }

        public static Message IllegalAttributeValueWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithoutOtherAttribute, "The {0}/@{1} attribute's value, '{2}', can only be specified with attribute {3} present with value '{4}'.", elementName, attributeName, attributeValue, otherAttributeName, otherAttributeValue);
        }

        public static Message IllegalAttributeValueWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithoutOtherAttribute, "The {0}/@{1} attribute's value, '{2}', cannot be specified without attribute {3} present.", elementName, attributeName, attributeValue, otherAttributeName);
        }

        public static Message IllegalAttributeWhenNested(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElement)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWhenNested, "The {0}/@{1} attribute cannot be specified when the {0} element is nested underneath a {2} element. If this {0} is a member of a ComponentGroup where ComponentGroup/@{1} is set, then the {0}/@{1} attribute should be removed.", elementName, attributeName, parentElement);
        }

        public static Message IllegalAttributeWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithOtherAttribute, "The {0}/@{1} attribute cannot be specified when attribute {2} is present.", elementName, attributeName, otherAttributeName);
        }

        public static Message IllegalAttributeWithOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithOtherAttribute, "The {0}/@{1} attribute cannot be specified when attribute {2} is present with value '{3}'.", elementName, attributeName, otherAttributeName, otherAttributeValue);
        }

        public static Message IllegalAttributeWithOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithOtherAttributes, "The {0}/@{1} attribute cannot be specified when attribute {2} or {3} is also present.", elementName, attributeName, otherAttributeName1, otherAttributeName2);
        }

        public static Message IllegalAttributeWithOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2, string otherAttributeName3)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithOtherAttributes, "The {0}/@{1} attribute cannot be specified when attribute {2}, {3}, or {4} is also present.", elementName, attributeName, otherAttributeName1, otherAttributeName2, otherAttributeName3);
        }

        public static Message IllegalAttributeWithOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2, string otherAttributeName3, string otherAttributeName4)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithOtherAttributes, "The {0}/@{1} attribute cannot be specified when attribute {2}, {3}, {4}, or {5} is also present.", elementName, attributeName, otherAttributeName1, otherAttributeName2, otherAttributeName3, otherAttributeName4);
        }

        public static Message IllegalAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutOtherAttributes, "The {0}/@{1} attribute can only be specified with the following attribute {2} present.", elementName, attributeName, otherAttributeName);
        }

        public static Message IllegalAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutOtherAttributes, "The {0}/@{1} attribute can only be specified with one of the following attributes: {2} or {3} present.", elementName, attributeName, otherAttributeName1, otherAttributeName2);
        }

        public static Message IllegalAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2, string otherAttributeValue, bool uniquifier)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutOtherAttributes, "The {0}/@{1} attribute can only be specified with one of the following attributes: {2} or {3} present with value '{4}'.", elementName, attributeName, otherAttributeName1, otherAttributeName2, otherAttributeValue, uniquifier);
        }

        public static Message IllegalAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2, string otherAttributeName3)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutOtherAttributes, "The {0}/@{1} attribute can only be specified with one of the following attributes: {2}, {3}, or {4} present.", elementName, attributeName, otherAttributeName1, otherAttributeName2, otherAttributeName3);
        }

        public static Message IllegalAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2, string otherAttributeName3, string otherAttributeName4)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutOtherAttributes, "The {0}/@{1} attribute can only be specified with one of the following attributes: {2}, {3}, {4}, or {5} present.", elementName, attributeName, otherAttributeName1, otherAttributeName2, otherAttributeName3, otherAttributeName4);
        }

        public static Message IllegalComponentWithAutoGeneratedGuid(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalComponentWithAutoGeneratedGuid, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components using a Directory as a KeyPath or containing ODBCDataSource child elements cannot use an automatically generated guid. Make sure your component doesn't have a Directory as the KeyPath and move any ODBCDataSource child elements to components with explicit component guids.");
        }

        public static Message IllegalComponentWithAutoGeneratedGuid(SourceLineNumber sourceLineNumbers, bool registryKeyPath)
        {
            return Message(sourceLineNumbers, Ids.IllegalComponentWithAutoGeneratedGuid, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with registry keypaths and files cannot use an automatically generated guid. Create multiple components, each with one file and/or one registry value keypath, to use automatically generated guids.", registryKeyPath);
        }

        public static Message IllegalEmptyAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalEmptyAttributeValue, "The {0}/@{1} attribute's value cannot be an empty string. If a value is not required, simply remove the entire attribute.", elementName, attributeName);
        }

        public static Message IllegalEmptyAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string defaultValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalEmptyAttributeValue, "The {0}/@{1} attribute's value cannot be an empty string. To use the default value \"{2}\", simply remove the entire attribute.", elementName, attributeName, defaultValue);
        }

        public static Message IllegalFileCompressionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalFileCompressionAttributes, "Cannot have both the MsidbFileAttributesCompressed and MsidbFileAttributesNoncompressed options set in a file attributes column.");
        }

        public static Message IllegalIdentifier(SourceLineNumber sourceLineNumbers, string elementName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifier, "The {0} element's value, '{1}', is not a legal identifier. Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.). Every identifier must begin with either a letter or an underscore.", elementName, value);
        }

        public static Message IllegalIdentifier(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, int disambiguator)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifier, "The {0}/@{1} attribute's value is not a legal identifier. Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.). Every identifier must begin with either a letter or an underscore.", elementName, attributeName, disambiguator);
        }

        public static Message IllegalIdentifier(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifier, "The {0}/@{1} attribute's value, '{2}', is not a legal identifier. Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.). Every identifier must begin with either a letter or an underscore.", elementName, attributeName, value);
        }

        public static Message IllegalIdentifier(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string identifier)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifier, "The {0}/@{1} attribute's value '{2}' contains an illegal identifier '{3}'. Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.). Every identifier must begin with either a letter or an underscore.", elementName, attributeName, value, identifier);
        }

        public static Message IllegalIntegerValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalIntegerValue, "The {0}/@{1} attribute's value, '{2}', is not a legal integer value. Legal integer values are from -2,147,483,648 to 2,147,483,647.", elementName, attributeName, value);
        }

        public static Message IllegalLongFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalLongFilename, "The {0}/@{1} attribute's value, '{2}', is not a valid filename because it contains illegal characters. Legal filenames contain no more than 260 characters and must contain at least one non-period character. Any character except for the follow may be used: \\ ? | > < : / * \".", elementName, attributeName, value);
        }

        public static Message IllegalLongFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string filename)
        {
            return Message(sourceLineNumbers, Ids.IllegalLongFilename, "The {0}/@{1} attribute's value '{2}' contains a invalid filename '{3}'. Legal filenames contain no more than 260 characters and must contain at least one non-period character. Any character except for the follow may be used: \\ ? | > < : / * \".", elementName, attributeName, value, filename);
        }

        public static Message IllegalParentAttributeWhenNested(SourceLineNumber sourceLineNumbers, string parentElementName, string parentAttributeName, string childElement)
        {
            return Message(sourceLineNumbers, Ids.IllegalParentAttributeWhenNested, "The {0}/@{1} attribute cannot be specified when a {2} element is nested underneath the {0} element.", parentElementName, parentAttributeName, childElement);
        }

        public static Message IllegalRelativeLongFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalRelativeLongFilename, "The {0}/@{1} attribute's value, '{2}', is not a valid relative long name because it contains illegal characters. Legal relative long names contain no more than 260 characters and must contain at least one non-period character. Any character except for the follow may be used: ? | > < : / * \".", elementName, attributeName, value);
        }

        public static Message TooManyElements(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, int expectedInstances)
        {
            return Message(sourceLineNumbers, Ids.TooManyElements, "The {0} element contains an unexpected child element '{1}'. The '{1}' element may only occur {2} time(s) under the {0} element.", elementName, childElementName, expectedInstances);
        }

        public static Message IdentifierTooLongError(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int maxLength)
        {
            return Message(sourceLineNumbers, Ids.IdentifierTooLongError, "The {0}/@{1} attribute's value, '{2}', is too long. {0}/@{1} attribute's must be {3} characters long or less.", elementName, attributeName, value, maxLength);
        }

        public static Message IllegalYesNoValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalYesNoValue, "The {0}/@{1} attribute's value, '{2}', is not a legal yes/no value. The only legal values are 'no' and 'yes'.", elementName, attributeName, value);
        }

        public static Message IntegralValueOutOfRange(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, int value, int minimum, int maximum)
        {
            return Message(sourceLineNumbers, Ids.IntegralValueOutOfRange, "The {0}/@{1} attribute's value, '{2}', is not in the range of legal values. Legal values for this attribute are from {3} to {4}.", elementName, attributeName, value, minimum, maximum);
        }

        public static Message IntegralValueOutOfRange(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, long value, long minimum, long maximum)
        {
            return Message(sourceLineNumbers, Ids.IntegralValueOutOfRange, "The {0}/@{1} attribute's value, '{2}', is not in the range of legal values. Legal values for this attribute are from {3} to {4}.", elementName, attributeName, value, minimum, maximum);
        }

        public static Message IntegralValueSentinelCollision(SourceLineNumber sourceLineNumbers, int value)
        {
            return Message(sourceLineNumbers, Ids.IntegralValueSentinelCollision, "The integer value {0} collides with a sentinel value in the compiler code.", value);
        }

        public static Message IntegralValueSentinelCollision(SourceLineNumber sourceLineNumbers, long value)
        {
            return Message(sourceLineNumbers, Ids.IntegralValueSentinelCollision, "The long integral value {0} collides with a sentinel value in the compiler code.", value);
        }

        public static Message InvalidDocumentElement(SourceLineNumber sourceLineNumbers, string elementName, string fileType, string expectedElementName)
        {
            return Message(sourceLineNumbers, Ids.InvalidDocumentElement, "The document element name '{0}' is invalid. A WiX {1} file must use '{2}' as the document element name.", elementName, fileType, expectedElementName);
        }

        public static Message InvalidWixXmlNamespace(SourceLineNumber sourceLineNumbers, string wixElementName, string wixNamespace)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixXmlNamespace, "The {0} element has no namespace. Please make the {0} element look like the following: <{0} xmlns=\"{1}\">.", wixElementName, wixNamespace);
        }

        public static Message InvalidWixXmlNamespace(SourceLineNumber sourceLineNumbers, string wixElementName, string elementNamespace, string wixNamespace)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixXmlNamespace, "The {0} element has an incorrect namespace of '{1}'. Please make the {0} element look like the following: <{0} xmlns=\"{2}\">.", wixElementName, elementNamespace, wixNamespace);
        }

        public static Message PayloadMustBeRelativeToCache(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.PayloadMustBeRelativeToCache, "The {0}/@{1} attribute's value, '{2}', is not a relative path.", elementName, attributeName, attributeValue);
        }

        public static Message RealTableMissingPrimaryKeyColumn(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.RealTableMissingPrimaryKeyColumn, "The table '{0}' does not contain any primary key columns. At least one column must be marked as the primary key to ensure this table can be patched.", tableName);
        }

        public static Message StreamNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int length, int maximumLength)
        {
            return Message(sourceLineNumbers, Ids.StreamNameTooLong, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. This is too long because it will be used to create a stream name. It cannot be more than than {4} characters long.", elementName, attributeName, value, length, maximumLength);
        }

        public static Message StreamNameTooLong(SourceLineNumber sourceLineNumbers, string tableName, string streamName, int streamLength)
        {
            return Message(sourceLineNumbers, Ids.StreamNameTooLong, "The binary value in table '{0}' will be stored with a stream name, '{1}', that is {2} characters long. This is too long because the maximum allowed length for a stream name is 62 characters long. Since the stream name is created by concatenating the table name and values of the primary key for a row (delimited by periods), this error can be resolved by shortening a value that is part of the primary key.", tableName, streamName, streamLength);
        }

        public static Message UnableToConvertFieldToNumber(string value)
        {
            return Message(null, Ids.UnableToConvertFieldToNumber, "Unable to convert intermediate symbol field value '{0}' to a number. This means the intermediate is corrupt or of an unsupported version.", value);
        }

        public static Message UnexpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAttribute, "The {0} element contains an unexpected attribute '{1}'.", elementName, attributeName);
        }

        public static Message UnexpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childElementName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElement, "The {0} element contains an unexpected child element '{1}'.", elementName, childElementName);
        }

        public static Message UnexpectedException(Exception exception)
        {
            return Message(null, Ids.UnexpectedException, exception.ToString());
        }

        public static Message UnexpectedException(string message, string type, string stackTrace)
        {
            return Message(null, Ids.UnexpectedException, "{0}\r\n\r\nException Type: {1}\r\n\r\nStack Trace:\r\n{2}", message, type, stackTrace);
        }

        public static Message UnexpectedFileFormat(string path, string expectedFormat, string actualFormat)
        {
            return Message(null, Ids.UnexpectedFileFormat, "Unexpected file format loaded from path: {0}. The file was expected to be a {1} but was actually: {2}. Ensure the correct path was provided.", path, expectedFormat.ToLowerInvariant(), actualFormat.ToLowerInvariant());
        }

        public static Message UnsupportedPlatformForElement(SourceLineNumber sourceLineNumbers, string platform, string elementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedPlatformForElement, "The element {1} does not support platform '{0}'. Consider removing the element or using the preprocessor to conditionally include the element based on the platform.", platform, elementName);
        }

        public static Message VersionMismatch(SourceLineNumber sourceLineNumbers, string fileType, string version, string expectedVersion)
        {
            return Message(sourceLineNumbers, Ids.VersionMismatch, "The {0} file format version {1} is not compatible with the expected {0} file format version {2}.", fileType, version, expectedVersion);
        }

        public static Message UnknownSymbolType(string symbolName)
        {
            return Message(null, Ids.UnknownSymbolType, "Could not deserialize symbol of type type '{0}' because it is not a standard symbol type or one provided by a loaded extension.", symbolName);
        }

        public static Message IllegalAttributeWhenNested(SourceLineNumber sourceLineNumbers, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWhenNested, "The File element contains an attribute '{0}' that cannot be used in a File element that is a child of a Component element.", attributeName);
        }

        public static Message CommandLineCommandRequired()
        {
            return Message(null, Ids.CommandLineCommandRequired, "A command is required. Add -h for list of available subcommands.");
        }

        public static Message CommandLineCommandRequired(string command)
        {
            return Message(null, Ids.CommandLineCommandRequired, "A subcommand is required for the \"{0}\" command. Add -h for list of available commands.", command);
        }

        public static Message CubeFileNotFound(string cubeFile)
        {
            return Message(null, Ids.CubeFileNotFound, "The cube file '{0}' cannot be found. This file is required for MSI validation.", cubeFile);
        }

        public static Message DuplicatePrimaryKey(SourceLineNumber sourceLineNumbers, string primaryKey, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatePrimaryKey, "The primary key '{0}' is duplicated in table '{1}'. Please remove one of the entries or rename a part of the primary key to avoid the collision.", primaryKey, tableName);
        }

        public static Message FilePathRequired(string filePurpose)
        {
            return Message(null, Ids.FilePathRequired, "The path to the {0} is required.", filePurpose);
        }

        public static Message FilePathRequired(string parameter, string filePurpose)
        {
            return Message(null, Ids.FilePathRequired, "The parameter '{0}' must be followed by a file path for the {1}.", parameter, filePurpose);
        }

        public static Message IdentifierNotFound(string type, string identifier)
        {
            return Message(null, Ids.IdentifierNotFound, "An expected identifier ('{1}', of type '{0}') was not found.", type, identifier);
        }

        public static Message IllegalCharactersInPath(string pathName)
        {
            return Message(null, Ids.IllegalCharactersInPath, "Illegal characters in path '{0}'. Ensure you provided a valid path to the file.", pathName);
        }

        public static Message IllegalCodepage(int codepage)
        {
            return Message(null, Ids.IllegalCodepage, "The code page '{0}' is not a valid Windows code page. Update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message IllegalCodepage(SourceLineNumber sourceLineNumbers, int codepage)
        {
            return Message(sourceLineNumbers, Ids.IllegalCodepage, "The code page '{0}' is not a valid Windows code page. Update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message IllegalCommandLineArgumentValue(string arg, string value, IEnumerable<string> validValues)
        {
            var combinedValidValues = String.Join(", ", validValues);
            return Message(null, Ids.IllegalCommandLineArgumentValue, "The argument {0} value '{1}' is invalid. Use one of the following values {2}", arg, value, combinedValidValues);
        }

        public static Message IllegalEnvironmentVariable(string environmentVariable, string value)
        {
            return Message(null, Ids.IllegalEnvironmentVariable, "The {0} environment variable is set to an invalid value of '{1}'.", environmentVariable, value);
        }

        public static Message IllegalGuidValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalGuidValue, "The {0}/@{1} attribute's value, '{2}', is not a legal guid value.", elementName, attributeName, value);
        }

        public static Message IllegalVersionValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalVersionValue, "The {0}/@{1} attribute's value, '{2}', is not a valid version. Specify a four-part version or semantic version, such as '#.#.#.#' or '#.#.#-label.#'.", elementName, attributeName, value);
        }

        public static Message InvalidFileName(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.InvalidFileName, "Invalid file name '{0}'.", fileName);
        }

        public static Message InvalidIdt(SourceLineNumber sourceLineNumbers, string idtFile)
        {
            return Message(sourceLineNumbers, Ids.InvalidIdt, "There was an error importing the file '{0}'.", idtFile);
        }

        public static Message InvalidIdt(SourceLineNumber sourceLineNumbers, string idtFile, string tableName)
        {
            return Message(sourceLineNumbers, Ids.InvalidIdt, "There was an error importing table '{1}' from file '{0}'.", idtFile, tableName);
        }

        public static Message InvalidValidatorMessageType(string type)
        {
            return Message(null, Ids.InvalidValidatorMessageType, "Unknown validation message type '{0}'.", type);
        }

        public static Message InvalidXml(SourceLineNumber sourceLineNumbers, string fileType, string detail)
        {
            return Message(sourceLineNumbers, Ids.InvalidXml, "Not a valid {0} file; detail: {1}", fileType, detail);
        }

        public static Message MissingTableDefinition(string tableName)
        {
            return Message(null, Ids.MissingTableDefinition, "Cannot find the table definitions for the '{0}' table. This is likely due to a typing error or missing extension. Please ensure all the necessary extensions are supplied on the command line with the -ext parameter.", tableName);
        }

        public static Message UnexpectedElementWithAttribute(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, string attribute)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElementWithAttribute, "The {0} element cannot have a child element '{1}' when attribute '{2}' is set.", elementName, childElementName, attribute);
        }

        public static Message UnexpectedElementWithAttribute(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, string attribute1, string attribute2, string attribute3, string attribute4)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElementWithAttribute, "The {0} element cannot have a child element '{1}' when any of attributes '{2}', '{3}', '{4}', or '{5}' are set.", elementName, childElementName, attribute1, attribute2, attribute3, attribute4);
        }

        public static Message UnexpectedElementWithAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, string attribute, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElementWithAttributeValue, "The {0} element cannot have a child element '{1}' unless attribute '{2}' is set to '{3}'.", elementName, childElementName, attribute, attributeValue);
        }

        public static Message UnexpectedElementWithAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, string attribute, string attributeValue1, string attributeValue2)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElementWithAttributeValue, "The {0} element cannot have a child element '{1}' unless attribute '{2}' is set to '{3}' or '{4}'.", elementName, childElementName, attribute, attributeValue1, attributeValue2);
        }

        public static Message UnexpectedExternalUIMessage(string message)
        {
            return Message(null, Ids.UnexpectedExternalUIMessage, "Error executing unknown ICE action. The following string format was not expected by the external UI message logger: \"{0}\".", message);
        }

        public static Message UnexpectedExternalUIMessage(string message, string action)
        {
            return Message(null, Ids.UnexpectedExternalUIMessage, "Error executing ICE action '{1}'. The following string format was not expected by the external UI message logger: \"{0}\".", message, action);
        }

        public static Message ValidationFailedDueToInvalidPackage()
        {
            return Message(null, Ids.ValidationFailedDueToInvalidPackage, "Failed to open package for validation. The most common cause of this error is validating an x64 package on an x86 system. To fix this error, run validation on an x64 system or disable validation.");
        }

        public static Message ValidationFailedDueToLowMsiEngine()
        {
            return Message(null, Ids.ValidationFailedDueToLowMsiEngine, "The package being validated requires a higher version of Windows Installer than is installed on this machine. Validation cannot continue.");
        }

        public static Message ValidationFailedDueToMultilanguageMergeModule()
        {
            return Message(null, Ids.ValidationFailedDueToMultilanguageMergeModule, "Failed to open merge module for validation. The most common cause of this error is specifying that the merge module supports multiple languages (using the Package/@Languages attribute) but not including language-specific embedded transforms. To fix this error, make the merge module language-neutral, make it language-specific, embed language transforms as specified in the MSI SDK at https://learn.microsoft.com/en-us/windows/win32/msi/authoring-multiple-language-merge-modules, or disable validation.");
        }

        public static Message ValidationFailedToOpenDatabase()
        {
            return Message(null, Ids.ValidationFailedToOpenDatabase, "Failed to open the database. During validation, this most commonly happens when attempting to open a database using an unsupported code page or a file that is not a valid Windows Installer database. Please use a different code page in Module/@Codepage, Package/@SummaryCodepage, Package/@Codepage, or WixLocalization/@Codepage; or make sure you provide the path to a valid Windows Installer database.");
        }

        public static Message Win32Exception(int nativeErrorCode, string message)
        {
            return Message(null, Ids.Win32Exception, "An unexpected Win32 exception with error code 0x{0:X} occurred: {1}", nativeErrorCode, message);
        }

        public static Message Win32Exception(int nativeErrorCode, string file, string message)
        {
            return Message(null, Ids.Win32Exception, "An unexpected Win32 exception with error code 0x{0:X} occurred while accessing file '{1}': {2}", nativeErrorCode, file, message);
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
            UnexpectedException = 1,
            UnexpectedFileFormat = 2,
            CorruptFileFormat = 3,
            UnexpectedAttribute = 4,
            UnexpectedElement = 5,
            IllegalEmptyAttributeValue = 6,
            IllegalIntegerValue = 8,
            IllegalGuidValue = 9,
            ExpectedAttribute = 10,
            StreamNameTooLong = 13,
            IllegalIdentifier = 14,
            IllegalYesNoValue = 15,
            CommandLineCommandRequired = 16,
            IllegalAttributeValue = 21,
            IllegalLongFilename = 27,
            IllegalAttributeWithOtherAttribute = 35,
            IllegalAttributeWithOtherAttributes = 36,
            IllegalAttributeWithoutOtherAttributes = 37,
            IllegalAttributeValueWithoutOtherAttribute = 38,
            IntegralValueSentinelCollision = 39,
            ExpectedAttributes = 44,
            ExpectedAttributesWithOtherAttribute = 45,
            ExpectedAttributesWithoutOtherAttribute = 46,
            InvalidDocumentElement = 48,
            ExpectedAttributeInElementOrParent = 49,
            IllegalAttributeExceptOnElement = 56,
            IllegalAttributeWhenNested = 62,
            ExpectedElement = 63,
            GenericReadNotAllowed = 67,
            InvalidFileName = 85,
            FileNotFound = 103,
            InvalidXml = 104,
            IllegalVersionValue = 108,
            FilePathRequired = 114,
            IntegralValueOutOfRange = 123,
            DuplicatePrimaryKey = 130,
            IllegalCodepage = 134,
            InvalidIdt = 136,
            VersionMismatch = 141,
            IllegalParentAttributeWhenNested = 155,
            IllegalFileCompressionAttributes = 167,
            MissingTableDefinition = 182,
            IllegalAttributeValueWithOtherAttribute = 193,
            InvalidWixXmlNamespace = 199,
            TooManyElements = 207,
            Win32Exception = 216,
            UnexpectedExternalUIMessage = 217,
            IllegalEnvironmentVariable = 219,
            CollidingModularizationTypes = 221,
            CubeFileNotFound = 222,
            RealTableMissingPrimaryKeyColumn = 225,
            IllegalComponentWithAutoGeneratedGuid = 230,
            InvalidValidatorMessageType = 245,
            UnexpectedElementWithAttributeValue = 255,
            InvalidPlatformParameter = 264,
            IllegalCommandLineArgumentValue = 268,
            ExpectedAttributeWhenElementNotUnderElement = 274,
            ExpectedParentWithAttribute = 285,
            IllegalCharactersInPath = 300,
            ValidationFailedToOpenDatabase = 301,
            IdentifierTooLongError = 304,
            ValidationFailedDueToMultilanguageMergeModule = 309,
            ValidationFailedDueToInvalidPackage = 310,
            IllegalAttributeValueWhenNested = 314,
            IdentifierNotFound = 344,
            IllegalRelativeLongFilename = 346,
            IllegalAttributeValueWithLegalList = 347,
            ValidationFailedDueToLowMsiEngine = 350,
            UnexpectedElementWithAttribute = 372,
            UnsupportedPlatformForElement = 381,
            PayloadMustBeRelativeToCache = 389,
            UnableToConvertFieldToNumber = 393,
            UnknownSymbolType = 399,
            ExpectedAttributeWithValueWithOtherAttribute = 401,
            ExpectedAttributeWithoutOtherAttributes = 408,
            ExpectedAttributeOrElementWithOtherAttribute = 413,
            ExpectedAttributeOrElementWithoutOtherAttribute = 414,
        }
    }
}
