// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public static class WarningMessages
    {
        public static Message DeprecatedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAttribute, "The {0}/@{1} attribute has been deprecated.", elementName, attributeName);
        }

        public static Message DeprecatedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string newAttributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAttribute, "The {0}/@{1} attribute has been deprecated. Please use the {2} attribute instead.", elementName, attributeName, newAttributeName);
        }

        public static Message DeprecatedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string newAttributeName1, string newAttributeName2)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAttribute, "The {0}/@{1} attribute has been deprecated. Please use the {2} or {3} attribute instead.", elementName, attributeName, newAttributeName1, newAttributeName2);
        }

        public static Message DeprecatedAttributeValue(SourceLineNumber sourceLineNumbers, string attributeValue, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAttributeValue, "The value \"{0}\" for the {1}/@{2} attribute has been deprecated. Remove the attribute.", attributeValue, elementName, attributeName);
        }

        public static Message DeprecatedAttributeValue(SourceLineNumber sourceLineNumbers, string attributeValue, string elementName, string attributeName, string newAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedAttributeValue, "The value \"{0}\" for the {1}/@{2} attribute has been deprecated. Please use \"{3}\" instead.", attributeValue, elementName, attributeName, newAttributeValue);
        }

        public static Message DeprecatedElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedElement, "The {0} element has been deprecated.", elementName);
        }

        public static Message DeprecatedElement(SourceLineNumber sourceLineNumbers, string elementName, string newElementName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedElement, "The {0} element has been deprecated. Please use the {1} element instead.", elementName, newElementName);
        }

        public static Message DeprecatedElement(SourceLineNumber sourceLineNumbers, string elementName, string newElementName1, string newElementName2)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedElement, "The {0} element has been deprecated. Please use the {1} or {2} element instead.", elementName, newElementName1, newElementName2);
        }

        public static Message ExpectedForeignRow(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, string columnName, string columnValue, string foreignTableName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedForeignRow, "The {0} table contains a row with primary key(s) '{1}' whose {2} column contains a value, '{3}', which specifies a foreign key relationship with the {4} table. However, since the expected foreign row specified by this value does not exist, this will result in some information being left out of the decompiled output.", tableName, primaryKey, columnName, columnValue, foreignTableName);
        }

        public static Message ExpectedForeignRow(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, string columnName1, string columnValue1, string columnName2, string columnValue2, string foreignTableName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedForeignRow, "The {0} table contains a row with primary key(s) '{1}' whose {2} and {4} columns contain the values, '{3}' and '{5}', which specify a foreign key relationship with the {6} table. However, since the expected foreign row specified by this value does not exist, this will result in some information being left out of the decompiled output.", tableName, primaryKey, columnName1, columnValue1, columnName2, columnValue2, foreignTableName);
        }

        public static Message IllegalColumnValue(SourceLineNumber sourceLineNumbers, string tableName, string columnName, object value)
        {
            return Message(sourceLineNumbers, Ids.IllegalColumnValue, "The {0}.{1} column's value, '{2}', is not a recognized legal value. This information will be left out of the decompiled output.", tableName, columnName, value);
        }

        public static Message UnknownPermission(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, int bitPosition)
        {
            return Message(sourceLineNumbers, Ids.UnknownPermission, "The {0} table contains a row with primary key '{1}' which has an unknown permission at bit {2}.", tableName, primaryKey, bitPosition);
        }

        public static Message DownloadUrlNotSupportedForBAPayloads(SourceLineNumber sourceLineNumbers, string payloadId)
        {
            return Message(sourceLineNumbers, Ids.DownloadUrlNotSupportedForBAPayloads, "The BootstrapperApplication Payload '{0}' included a @DownloadUrl attribute. BootstrapperApplication Payloads cannot be downloaded so the download URL is being ignored.", payloadId);
        }

        public static Message InvalidMsiProductVersion(SourceLineNumber sourceLineNumbers, string version, string package)
        {
            return Message(sourceLineNumbers, Ids.InvalidMsiProductVersion, "Invalid package version '{0}' in MSI package '{1}'. Package version should have a major version less than 256, a minor version less than 256, and a build version less than 65536. The bundle may incorrectly detect upgrades of this package.", version, package);
        }

        public static Message InvalidMsiProductVersion(SourceLineNumber sourceLineNumbers, string version)
        {
            return Message(sourceLineNumbers, Ids.InvalidMsiProductVersion,
                "Invalid MSI package version: '{0}'. " +
                "The Windows Installer SDK says that MSI package versions must have a major version less than 256, a minor version less than 256, and a build version less than 65536. " +
                "The revision value is ignored but version labels and metadata are not allowed. " +
                "Violating the MSI rules sometimes works as expected but the behavior is unpredictable and undefined. "+
                "Future versions of WiX might treat invalid package versions as an error.",
                version);
        }

        public static Message SymbolNotTranslatedToOutput(IntermediateSymbol symbol)
        {
            var symbolString = $"SymbolName: '{symbol.Definition.Name}', Id: '{symbol.Id?.Id}'";
            return Message(symbol.SourceLineNumbers, Ids.SymbolNotTranslatedToOutput, "The binder doesn't know how to place the following symbol into the output: {0}", symbolString);
        }

        public static Message WindowsInstallerFileTooLarge(SourceLineNumber sourceLineNumbers, string path, string fileDescription)
        {
            if (String.IsNullOrEmpty(fileDescription))
            {
                fileDescription = "MSI or cabinet";
            }

            return Message(sourceLineNumbers, Ids.WindowsInstallerFileTooLarge, "The Windows Installer does not support {0} files larger than 2GB in size. Reduce the size or number of files embedded in '{1}' or the installation will likely fail with an unexpected error.", fileDescription, path);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            UnknownPermission = 1030,
            DeprecatedAttribute = 1054,
            ExpectedForeignRow = 1059,
            IllegalColumnValue = 1067,
            DeprecatedAttributeValue = 1111,
            DeprecatedElement = 1130,
            DownloadUrlNotSupportedForBAPayloads = 1132,
            InvalidMsiProductVersion = 1148,
            SymbolNotTranslatedToOutput = 1150,
            WindowsInstallerFileTooLarge = 1158,
        }
    }
}
