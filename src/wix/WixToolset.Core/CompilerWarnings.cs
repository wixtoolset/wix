// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Data;

    internal static class CompilerWarnings
    {
        public static Message DirectoryRefStandardDirectoryDeprecated(SourceLineNumber sourceLineNumbers, string directoryId)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRefStandardDirectoryDeprecated, "Using DirectoryRef to reference the standard directory '{0}' is deprecated. Use the StandardDirectory element instead.", directoryId);
        }

        public static Message DirectoryRefStandardDirectoryDeprecated(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRefStandardDirectoryDeprecated, "Using DirectoryRef to reference the standard directory 'TARGETDIR' is deprecated. WiX automatically supplies a root 'TARGETDIR' directory, so you can just omit a DirectoryRef to 'TARGETDIR'. You can also use the StandardDirectory element instead.");
        }

        public static Message DefiningStandardDirectoryDeprecated(SourceLineNumber sourceLineNumbers, string directoryId)
        {
            return Message(sourceLineNumbers, Ids.DefiningStandardDirectoryDeprecated, "It is no longer necessary to define the standard directory '{0}'. Use the StandardDirectory element instead.", directoryId);
        }

        public static Message DiscouragedVersionAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedVersionAttribute, "The Provides/@Version attribute should not be specified in an MSI package. The ProductVersion will be used by default.");
        }

        public static Message DiscouragedVersionAttribute(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedVersionAttribute, "The Provides/@Version attribute should not be specified for MSI package {0}. The ProductVersion will be used by default.", id);
        }

        public static Message PatchCreationDeprecated(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PatchCreationDeprecated, "The PatchCreation element is not supported in WiX v4 and later. Use the Patch element instead.");
        }

        public static Message PropertyRemoved(string name)
        {
            return Message(null, Ids.PropertyRemoved, "The property {0} was authored in the package with a value and will be removed. The property should not be authored.", name);
        }

        public static Message ProvidesKeyNotFound(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.ProvidesKeyNotFound, "The provider key with identifier {0} was not found in the Wix4DependencyProvider table. Related registry rows will not be removed from authoring.", id);
        }

        public static Message ReadonlyLogVariableTarget(SourceLineNumber sourceLineNumbers, string element, string attribute, string name)
        {
            return Message(sourceLineNumbers, Ids.ReadonlyLogVariableTarget, "The {0}/@{1} attribute's value references the well-known log Variable '{2}' to change its value. This variable is set by the engine and is intended to be read-only. Change your attribute's value to reference a custom variable.", element, attribute, name);
        }

        public static Message RequiresKeyNotFound(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.RequiresKeyNotFound, "The dependency key with identifier {0} was not found in the Wix4Dependency table. Related registry rows will not be removed from authoring.", id);
        }

        public static Message Win64Component(SourceLineNumber sourceLineNumbers, string componentId)
        {
            return Message(sourceLineNumbers, Ids.Win64Component, "The Provides element should not be authored in the 64-bit component with identifier {0}. The dependency feature may not work if installing this package on 64-bit Windows operating systems prior to Windows 7 and Windows Server 2008 R2. Set the Component/@Bitness attribute to \"always32\" to ensure the dependency feature works correctly on legacy operating systems.", componentId);
        }

        public static Message AllChangesIncludedInPatch(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.AllChangesIncludedInPatch, "All changes between the baseline and upgraded packages will be included in the patch except for any change to the ProductCode. The 'All' element is supported primarily for testing purposes and negates the benefits of patch families.");
        }

        public static Message AmbiguousFileOrDirectoryName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.AmbiguousFileOrDirectoryName, "The {0}/@{1} attribute's value '{2}' is an ambiguous short name because it ends with a '~' character followed by a number. Under some circumstances, this name could resolve to more than one file or directory name and lead to unpredictable results (for example 'MICROS~1' may correspond to 'Microsoft Shared' or 'Microsoft Foo' or literally 'Micros~1').", elementName, attributeName, value);
        }

        public static Message AttributeShouldContain(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue, string expectedContains, string otherAttributeName, string otherAttributeValue)
        {
            return Message(sourceLineNumbers, Ids.AttributeShouldContain, "The {0}/@{1} attribute value '{2}' should contain '{3}' when the {0}/@{4} attribute is set to '{5}'.", elementName, attributeName, attributeValue, expectedContains, otherAttributeName, otherAttributeValue);
        }

        public static Message CopyFileFileIdUseless(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CopyFileFileIdUseless, "Since the CopyFile/@FileId attribute was specified but none of the following attributes (DestinationName, DestinationDirectory, DestinationProperty) were specified, this authoring will not do anything.");
        }

        public static Message DeprecatedPreProcVariable(SourceLineNumber sourceLineNumbers, string oldName, string newName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedPreProcVariable, "The built-in preprocessor variable '{0}' is deprecated. Please correct your authoring to use the new '{1}' preprocessor variable instead.", oldName, newName);
        }

        public static Message DeprecatedUpgradeProperty(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedUpgradeProperty, "Specifying a Property element as a child of an Upgrade element has been deprecated. Please specify this Property element as a child of a different element such as Package or Fragment.");
        }

        public static Message DirectoryRedundantNames(SourceLineNumber sourceLineNumbers, string elementName, string shortNameAttributeName, string longNameAttributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRedundantNames, "The {0} element's {1} and {2} values are both '{3}'. This is redundant; the {2} attribute should be removed.", elementName, shortNameAttributeName, longNameAttributeName, attributeValue);
        }

        public static Message DirectoryRedundantNames(SourceLineNumber sourceLineNumbers, string elementName, string sourceNameAttributeName, string longSourceAttributeName)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRedundantNames, "The {0} element's source and destination names are identical. This is redundant; the {1} and {2} attributes should be removed if present.", elementName, sourceNameAttributeName, longSourceAttributeName);
        }

        public static Message DetectConditionRecommended(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DetectConditionRecommended, "The {0}/@DetectCondition attribute is recommended so the package is only installed when absent.", elementName);
        }

        public static Message ExePackageDetectInformationRecommended(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExePackageDetectInformationRecommended, "The ExePackage/@DetectCondition attribute or child element ArpEntry is recommended so the package is only installed when absent.");
        }


        public static Message FileSearchFileNameIssue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.FileSearchFileNameIssue, "The {0} element's {1} and {2} attributes were found. Due to a bug with the Windows Installer, only the Name or LongName attribute should be used. Use the Name attribute for 8.3 compliant file names and the LongName attribute for longer ones. When using only the LongName attribute, ICE03 should be ignored for the Signature table's FileName column.", elementName, attributeName1, attributeName2);
        }

        public static Message IdentifierCannotBeModularized(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string identifier, int length, int maximumLength)
        {
            return Message(sourceLineNumbers, Ids.IdentifierCannotBeModularized, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. It will be too long if modularized. The identifier shouldn't be longer than {4} characters long to allow for modularization (appending a guid for merge modules).", elementName, attributeName, identifier, length, maximumLength);
        }

        public static Message IdentifierTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IdentifierTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for an identifier. Standard identifiers are 72 characters long or less.", elementName, attributeName, value);
        }

        public static Message MediaExternalCabinetFilenameIllegal(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.MediaExternalCabinetFilenameIllegal, "The {0}/@{1} attribute's value, '{2}', is not a valid external cabinet name. Legal cabinet names should follow 8.3 format: they should contain no more than 8 characters followed by an optional extension of no more than 3 characters. Any character except for the following may be used: \\ ? | > < : / * \" + , ; = [ ] (space). The Windows Installer team has recommended following the 8.3 format for external cabinet files and any other naming scheme is officially unsupported (which means it is not guaranteed to work on all platforms).", elementName, attributeName, value);
        }

        public static Message MissingUpgradeCode(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MissingUpgradeCode, "The Package/@UpgradeCode attribute was not found; it is strongly recommended to ensure that this package can be upgraded.");
        }

        public static Message OrphanedProgId(SourceLineNumber sourceLineNumbers, string progId)
        {
            return Message(sourceLineNumbers, Ids.OrphanedProgId, "ProgId '{0}' is orphaned. It has no associated component, so it will never install. Every ProgId should have either a parent Class element or child Extension element (at any distance).", progId);
        }

        public static Message PathCanonicalized(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string originalValue, string canonicalValue)
        {
            return Message(sourceLineNumbers, Ids.PathCanonicalized, "The {0}/@{1} attribute's value, '{2}', has been canonicalized to '{3}'.", elementName, attributeName, originalValue, canonicalValue);
        }

        public static Message PlaceholderValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.PlaceholderValue, "The {0}/@{1} attribute's value, '{2}', is a placeholder value used in example files. Please replace this placeholder with the appropriate value.", elementName, attributeName, value);
        }

        public static Message PreprocessorUnknownPragma(SourceLineNumber sourceLineNumbers, string pragmaName)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorUnknownPragma, "The pragma '{0}' is unknown. Please ensure you have referenced the extension that defines this pragma.", pragmaName);
        }

        public static Message PreprocessorWarning(SourceLineNumber sourceLineNumbers, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorWarning, "{0}", message);
        }

        public static Message ProductIdAuthored(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ProductIdAuthored, "The 'ProductID' property should not be directly authored because it will prevent the ValidateProductID standard action from performing any validation during the installation. This property will be set by the ValidateProductID standard action or control event.");
        }

        public static Message PropertyModularizationSuppressed(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PropertyModularizationSuppressed, "The Property/@SuppressModularization attribute has been set to 'yes'. Using this functionality is strongly discouraged; it should only be necessary as a workaround of last resort in rare scenarios.");
        }

        public static Message PropertyUseless(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.PropertyUseless, "Property '{0}' does not contain a Value attribute and is not marked as Admin, Secure, or Hidden. The Property element is being ignored.", id);
        }

        public static Message PropertyValueContainsPropertyReference(SourceLineNumber sourceLineNumbers, string propertyId, string otherProperty)
        {
            return Message(sourceLineNumbers, Ids.PropertyValueContainsPropertyReference, "The '{0}' Property contains '[{1}]' in its value which is an illegal reference to another property. If this value is a string literal, not a property reference, please ignore this warning. To set a property with the value of another property, use a CustomAction with Property and Value attributes.", propertyId, otherProperty);
        }

        public static Message RemotePayloadsMustNotAlsoBeCompressed(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.RemotePayloadsMustNotAlsoBeCompressed, "The {0}/@Compressed attribute must have value 'no' when a RemotePayload child element is present. RemotePayload indicates that a package will always be downloaded and cannot be compressed into a bundle. To eliminate this warning, explicitly set the {0}/@Compressed attribute to 'no'.", elementName);
        }

        public static Message RequiresMsi200for64bitPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RequiresMsi200for64bitPackage, "Package/@InstallerVersion must be 200 or greater for a 64-bit package. The value will be changed to 200. Please specify a value of 200 or greater in order to eliminate this warning.");
        }

        public static Message RequiresMsi500forArmPackage(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RequiresMsi500forArmPackage, "Package/@InstallerVersion must be 500 or greater for an ARM64 package. The value will be changed to 500. Please specify a value of 500 or greater in order to eliminate this warning.");
        }

        public static Message ReservedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.ReservedAttribute, "The {0}/@{1} attribute is reserved for future use and has no effect in this version of the WiX toolset.", elementName, attributeName);
        }

        public static Message ServiceConfigFamilyNotSupported(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.ServiceConfigFamilyNotSupported, "{0} functionality is documented in the Windows Installer SDK to \"not [work] as expected.\" Consider replacing {0} with the WixToolset.Util.wixext ServiceConfig element.", elementName);
        }


        public static Message UnableToResetAcls(string error)
        {
            return Message(null, Ids.UnableToResetAcls, "Unable to reset acls on destination files. Exception detail: {0}", error);
        }

        public static Message UnavailableBundleConditionVariable(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variable, string illegalValueList)
        {
            return Message(sourceLineNumbers, Ids.UnavailableBundleConditionVariable, "{0}/@{1} contains the built-in Variable '{2}', which is not available when it is evaluated. (Unavailable Variables are: {3}.). Rewrite the condition to avoid Variables that are never valid during its evaluation.", elementName, attributeName, variable, illegalValueList);
        }

        public static Message UnclearShortcut(SourceLineNumber sourceLineNumbers, string shortcutId, string fileId, string componentId)
        {
            return Message(sourceLineNumbers, Ids.UnclearShortcut, "Because it is an advertised shortcut, the target of shortcut '{0}' will be the keypath of component '{2}' rather than parent file '{1}'. To eliminate this warning, you can (1) make the Shortcut element a child of the File element that is the keypath of component '{2}', (2) make file '{1}' the keypath of component '{2}', or (3) remove the @Advertise attribute so the shortcut is a non-advertised shortcut.", shortcutId, fileId, componentId);
        }

        public static Message UnsupportedCommandLineArgumentValue(string arg, string value, string fallback)
        {
            return Message(null, Ids.UnsupportedCommandLineArgument, "The value '{0}' is not a valid value for command line argument '{1}'. Using the value '{2}' instead.", value, arg, fallback);
        }

        public static Message UxPayloadsOnlySupportEmbedding(SourceLineNumber sourceLineNumbers, string sourceFile)
        {
            return Message(sourceLineNumbers, Ids.UxPayloadsOnlySupportEmbedding, "A bootstrapper application or bundle extension payload ('{0}') was marked for something other than embedded packaging, possibly because it included a @DownloadUrl attribute. Bootstrapper application and bundle extension payloads must be embedded in the bundle, so the requested packaging is being ignored and the file is being embedded anyway.", sourceFile);
        }

        public static Message VariableDeclarationCollision(SourceLineNumber sourceLineNumbers, string variableName, string variableValue, string variableCollidingValue)
        {
            return Message(sourceLineNumbers, Ids.VariableDeclarationCollision, "The variable '{0}' with value '{1}' was previously declared with value '{2}'.", variableName, variableValue, variableCollidingValue);
        }




        public static Message VBScriptIsDeprecated(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.VBScriptIsDeprecated, "VBScript is a deprecated Windows component: https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features. VBScript custom actions might fail on some Windows systems. Rewrite or eliminate VBScript custom actions for best compatibility.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            IdentifierCannotBeModularized = 1000,
            CopyFileFileIdUseless = 1003,
            OrphanedProgId = 1005,
            PropertyUseless = 1006,
            IdentifierTooLong = 1026,
            DirectoryRedundantNames = 1031,
            UnableToResetAcls = 1032,
            MediaExternalCabinetFilenameIllegal = 1033,
            DeprecatedPreProcVariable = 1034,
            FileSearchFileNameIssue = 1043,
            AmbiguousFileOrDirectoryName = 1044,
            PlaceholderValue = 1074,
            MissingUpgradeCode = 1075,
            PropertyValueContainsPropertyReference = 1077,
            DeprecatedUpgradeProperty = 1078,
            ProductIdAuthored = 1083,
            PropertyModularizationSuppressed = 1086,
            PreprocessorWarning = 1096,
            UnsupportedCommandLineArgument = 1098,
            UnclearShortcut = 1113,
            VariableDeclarationCollision = 1118,
            RequiresMsi200for64bitPackage = 1121,
            PreprocessorUnknownPragma = 1125,
            UxPayloadsOnlySupportEmbedding = 1127,
            AttributeShouldContain = 1136,
            ReservedAttribute = 1142,
            RequiresMsi500forArmPackage = 1143,
            RemotePayloadsMustNotAlsoBeCompressed = 1144,
            AllChangesIncludedInPatch = 1145,
            ServiceConfigFamilyNotSupported = 1149,
            PathCanonicalized = 1152,
            DetectConditionRecommended = 1153,
            UnavailableBundleConditionVariable = 1159,
            ExePackageDetectInformationRecommended = 1161,
            VBScriptIsDeprecated = 1163,
            ProvidesKeyNotFound = 5431,
            RequiresKeyNotFound = 5432,
            PropertyRemoved = 5433,
            DiscouragedVersionAttribute = 5434,
            Win64Component = 5435,
            DirectoryRefStandardDirectoryDeprecated = 5436,
            DefiningStandardDirectoryDeprecated = 5437,
            ReadonlyLogVariableTarget = 5438,
            PatchCreationDeprecated = 5440,
        } // 5400-5499 and 6600-6699 were the ranges for Dependency and Tag which are now in Core between CompilerWarnings and CompilerErrors.
    }
}
