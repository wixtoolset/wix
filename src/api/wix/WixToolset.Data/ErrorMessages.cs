// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.Resources;

    public static class ErrorMessages
    {
        public static Message ActionCircularDependency(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2)
        {
            return Message(sourceLineNumbers, Ids.ActionCircularDependency, "The {0} table contains an action '{1}' that is scheduled to come before or after action '{2}', which is also scheduled to come before or after action '{1}'. Please remove this circular dependency by changing the Before or After attribute for one of the actions.", sequenceTableName, actionName1, actionName2);
        }

        public static Message ActionCollision(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.ActionCollision, "The {0} table contains an action '{1}' that is declared in two different locations. Please remove one of the actions or set the Overridable='yes' attribute on one of their elements.", sequenceTableName, actionName);
        }

        public static Message ActionCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ActionCollision2, "The location of the action related to previous error.");
        }

        public static Message ActionScheduledRelativeToItself(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToItself, "The {0}/@{1} attribute's value '{2}' is invalid because it would make this action dependent upon itself. Please change the value to the name of a different action.", elementName, attributeName, attributeValue);
        }

        public static Message ActionScheduledRelativeToTerminationAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToTerminationAction, "The {0} table contains an action '{1}' that is scheduled to come before or after action '{2}', which is a special action which only occurs when the installer terminates. These special actions can be identified by their negative sequence numbers. Please schedule the action '{1}' to come before or after a different action.", sequenceTableName, actionName1, actionName2);
        }

        public static Message ActionScheduledRelativeToTerminationAction2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToTerminationAction2, "The location of the special termination action related to previous error(s).");
        }

        public static Message AdditionalArgumentUnexpected(string argument)
        {
            return Message(null, Ids.AdditionalArgumentUnexpected, "Additional argument '{0}' was unexpected. Remove the argument and add the '-?' switch for more information.", argument);
        }

        public static Message AdminImageRequired(string productCode)
        {
            return Message(null, Ids.AdminImageRequired, "Source information is required for the product '{0}'. If you ran torch.exe with both target and updated .msi files, you must first perform an administrative installation of both .msi files then pass -a when running torch.exe.", productCode);
        }

        public static Message AdvertiseStateMustMatch(SourceLineNumber sourceLineNumbers, string advertiseState, string parentAdvertiseState)
        {
            return Message(sourceLineNumbers, Ids.AdvertiseStateMustMatch, "The advertise state of this element: '{0}', does not match the advertise state set on the parent element: '{1}'.", advertiseState, parentAdvertiseState);
        }

        public static Message AppIdIncompatibleAdvertiseState(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string parentValue)
        {
            return Message(sourceLineNumbers, Ids.AppIdIncompatibleAdvertiseState, "The {0}/@(1) attribute's value, '{2}' does not match the advertise state on its parent element: '{3}'. (Note: AppIds nested under Fragment, Module, or Product elements must be advertised.)", elementName, attributeName, value, parentValue);
        }

        public static Message BaselineRequired()
        {
            return Message(null, Ids.BaselineRequired, "No baseline was specified for one of the transforms specified. A baseline is required for all transforms in a patch.");
        }

        public static Message BinderFileManagerMissingFile(SourceLineNumber sourceLineNumbers, string exceptionMessage)
        {
            return Message(sourceLineNumbers, Ids.BinderFileManagerMissingFile, "{0}", exceptionMessage);
        }

        public static Message BothUpgradeCodesRequired()
        {
            return Message(null, Ids.BothUpgradeCodesRequired, "Both the target and updated package authoring must define the Package/@UpgradeCode attribute if the transform validates the UpgradeCode (default). Either define the Package/@UpgradeCode attribute in both the target and updated authoring, or set the Validate/@UpgradeCode attribute to 'no' in the patch authoring.");
        }

        public static Message BundleTooNew(string bundleExecutable, long bundleVersion)
        {
            return Message(null, Ids.BundleTooNew, "Unable to read bundle executable '{0}', because this bundle was created with a newer version of WiX (bundle version '{1}'). You must use a newer version of WiX in order to read this bundle.", bundleExecutable, bundleVersion);
        }

        public static Message CabExtractionFailed(string cabName, string directoryName)
        {
            return Message(null, Ids.CabExtractionFailed, "Failed to extract cab '{0}' to directory '{1}'. This is most likely due to a lack of available disk space on the destination drive.", cabName, directoryName);
        }

        public static Message CabExtractionFailed(string cabName, string mergeModulePath, string directoryName)
        {
            return Message(null, Ids.CabExtractionFailed, "Failed to extract cab '{0}' from merge module '{1}' to directory '{2}'. This is most likely due to a lack of available disk space on the destination drive.", cabName, mergeModulePath, directoryName);
        }

        public static Message CabFileDoesNotExist(string cabName, string mergeModulePath, string directoryName)
        {
            return Message(null, Ids.CabFileDoesNotExist, "Attempted to extract cab '{0}' from merge module '{1}' to directory '{2}'. The cab file was not found. This usually means that you have a merge module without a cabinet inside it.", cabName, mergeModulePath, directoryName);
        }

        public static Message CannotAuthorSpecialProperties(SourceLineNumber sourceLineNumbers, string propertyName)
        {
            return Message(sourceLineNumbers, Ids.CannotAuthorSpecialProperties, "The {0} property was specified. Special MSI properties cannot be authored. Use the attributes on the Property element instead.", propertyName);
        }

        public static Message CannotDefaultComponentId(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CannotDefaultComponentId, "The Component/@Id attribute was not found; it is required when there is no valid keypath to use as the default id value.");
        }

        public static Message CannotDefaultMismatchedAdvertiseStates(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CannotDefaultMismatchedAdvertiseStates, "MIME element cannot be marked as the default when its advertise state differs from its parent element. Ensure that the advertise state of the MIME element matches its parents element or remove the Mime/@Advertise attribute completely.");
        }

        public static Message CannotFindFile(SourceLineNumber sourceLineNumbers, string fileId, string fileName, string filePath)
        {
            return Message(sourceLineNumbers, Ids.CannotFindFile, "The file with id '{0}' and name '{1}' could not be found with source path: '{2}'.", fileId, fileName, filePath);
        }

        public static Message CanNotHaveTwoParents(SourceLineNumber sourceLineNumbers, string directorySearch, string parentAttribute, string parentElement)
        {
            return Message(sourceLineNumbers, Ids.CanNotHaveTwoParents, "The DirectorySearchRef {0} can not have a Parent attribute {1} and also be nested under parent element {2}", directorySearch, parentAttribute, parentElement);
        }

        public static Message CannotOpenMergeModule(SourceLineNumber sourceLineNumbers, string mergeModuleIdentifier, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.CannotOpenMergeModule, "Cannot open the merge module '{0}' from file '{1}'.", mergeModuleIdentifier, mergeModuleFile);
        }

        public static Message CannotReundefineVariable(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.CannotReundefineVariable, "The variable '{0}' cannot be undefined because its already undefined.", variableName);
        }

        public static Message CheckBoxValueOnlyValidWithCheckBox(SourceLineNumber sourceLineNumbers, string elementName, string controlType)
        {
            return Message(sourceLineNumbers, Ids.CheckBoxValueOnlyValidWithCheckBox, "A {0} element was specified with Type='{1}' and a CheckBoxValue. Check box values can only be specified with Type='CheckBox'.", elementName, controlType);
        }

        public static Message CircularSearchReference(string chain)
        {
            return Message(null, Ids.CircularSearchReference, "A circular reference of search ordering constraints was detected: {0}. Search ordering references must form a directed acyclic graph.", chain);
        }

        public static Message CommandLineCommandRequired()
        {
            return Message(null, Ids.CommandLineCommandRequired, "A command is required. Add -h for list of available subcommands.");
        }

        public static Message CommandLineCommandRequired(string command)
        {
            return Message(null, Ids.CommandLineCommandRequired, "A subcommand is required for the \"{0}\" command. Add -h for list of available commands.", command);
        }

        public static Message ComponentExpectedFeature(SourceLineNumber sourceLineNumbers, string component, string type, string target)
        {
            return Message(sourceLineNumbers, Ids.ComponentExpectedFeature, "The component '{0}' is not assigned to a feature. The component's {1} '{2}' requires it to be assigned to at least one feature.", component, type, target);
        }

        public static Message ComponentMultipleKeyPaths(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string fileElementName, string registryElementName, string odbcDataSourceElementName)
        {
            return Message(sourceLineNumbers, Ids.ComponentMultipleKeyPaths, "The {0} element has multiple key paths set. The key path may only be set to '{2}' in extension elements that support it or one of the following locations: {0}/@{1}, {3}/@{1}, {4}/@{1}, or {5}/@{1}.", elementName, attributeName, value, fileElementName, registryElementName, odbcDataSourceElementName);
        }

        public static Message ComponentReferencedTwice(SourceLineNumber sourceLineNumbers, string crefChildId)
        {
            return Message(sourceLineNumbers, Ids.ComponentReferencedTwice, "Component {0} cannot be contained in a Module twice.", crefChildId);
        }

        public static Message ConditionExpected(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.ConditionExpected, "The {0} element's inner text cannot be an empty string or completely whitespace. If you don't want a condition, then simply remove the entire {0} element.", elementName);
        }

        public static Message CorruptFileFormat(string path, string format)
        {
            return Message(null, Ids.CorruptFileFormat, "Attempted to load corrupt file from path: {0}. The file with format {1} contained unexpected content. Ensure the correct path was provided and that the file has not been incorrectly modified.", path, format.ToLowerInvariant());
        }

        public static Message CouldNotDetermineProductCodeFromTransformSummaryInfo()
        {
            return Message(null, Ids.CouldNotDetermineProductCodeFromTransformSummaryInfo, "Could not determine ProductCode from transform summary information.");
        }

        public static Message CreateCabAddFileFailed()
        {
            return Message(null, Ids.CreateCabAddFileFailed, "An error (E_FAIL) was returned while creating a CAB file. The most common cause of this error is attempting to create a CAB file larger than 2GB. You can reduce the size of your installation package, use a higher compression level, or split your files into more than one CAB file.");
        }

        public static Message CreateCabInsufficientDiskSpace()
        {
            return Message(null, Ids.CreateCabInsufficientDiskSpace, "An error (ERROR_DISK_FULL) was returned while creating a CAB file. This means you have insufficient disk space. Clear disk space and try again.");
        }

        public static Message CubeFileNotFound(string cubeFile)
        {
            return Message(null, Ids.CubeFileNotFound, "The cube file '{0}' cannot be found. This file is required for MSI validation.", cubeFile);
        }

        public static Message CustomActionMultipleSources(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5)
        {
            return Message(sourceLineNumbers, Ids.CustomActionMultipleSources, "The {0}/@{1} attribute cannot coexist with a previously specified attribute on this element. The {0} element may only have one of the following source attributes specified at a time: {2}, {3}, {4}, {5}, or {6}.", elementName, attributeName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5);
        }

        public static Message CustomActionMultipleTargets(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5, string attributeName6, string attributeName7)
        {
            return Message(sourceLineNumbers, Ids.CustomActionMultipleTargets, "The {0}/@{1} attribute cannot coexist with a previously specified attribute on this element. The {0} element may only have one of the following target attributes specified at a time: {2}, {3}, {4}, {5}, {6}, {7}, or {8}.", elementName, attributeName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5, attributeName6, attributeName7);
        }

        public static Message CustomActionSequencedInModule(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.CustomActionSequencedInModule, "The {0} table contains a custom action '{1}' that has a sequence number specified. The Sequence attribute is not allowed for custom actions in a merge module. Please remove the action or use the Before or After attributes to specify where this action should be sequenced relative to another action.", sequenceTableName, actionName);
        }

        public static Message CustomTableIllegalColumnWidth(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, int value)
        {
            return Message(sourceLineNumbers, Ids.CustomTableIllegalColumnWidth, "The {0}/@{1} attribute's value, '{2}', is not a valid column width. Valid column widths are 2 or 4.", elementName, attributeName, value);
        }

        public static Message CustomTableMissingPrimaryKey(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CustomTableMissingPrimaryKey, "The CustomTable is missing a Column element with the PrimaryKey attribute set to 'yes'. At least one column must be marked as the primary key.");
        }

        public static Message CustomTableNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.CustomTableNameTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for a table name. It cannot be more than than 31 characters long.", elementName, attributeName, value);
        }

        public static Message DatabaseSchemaMismatch(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DatabaseSchemaMismatch, "The table definition of '{0}' in the target database does not match the table definition in the updated database. A transform requires that the target database schema match the updated database schema.", tableName);
        }

        public static Message DirectoryPathRequired(string parameter)
        {
            return Message(null, Ids.DirectoryPathRequired, "The parameter '{0}' must be followed by a directory path.", parameter);
        }

        public static Message DisallowedMsiProperty(SourceLineNumber sourceLineNumbers, string property, string illegalValueList)
        {
            return Message(sourceLineNumbers, Ids.DisallowedMsiProperty, "The '{0}' MsiProperty is controlled by the bootstrapper and cannot be authored. (Illegal properties are: {1}.) Remove the MsiProperty element.", property, illegalValueList);
        }

        public static Message DuplicateCabinetName(SourceLineNumber sourceLineNumbers, string cabinetName)
        {
            return Message(sourceLineNumbers, Ids.DuplicateCabinetName, "Duplicate cabinet name '{0}' found.", cabinetName);
        }

        public static Message DuplicateCabinetName2(SourceLineNumber sourceLineNumbers, string cabinetName)
        {
            return Message(sourceLineNumbers, Ids.DuplicateCabinetName2, "Duplicate cabinet name '{0}' error related to previous error.", cabinetName);
        }

        public static Message DuplicateCommandLineOptionInExtension(string arg)
        {
            return Message(null, Ids.DuplicateCommandLineOptionInExtension, "The command line option '{0}' has already been loaded by another Heat extension.", arg);
        }

        public static Message DuplicateComponentGuids(SourceLineNumber sourceLineNumbers, string componentId, string guid, string type, string keyPath)
        {
            return Message(sourceLineNumbers, Ids.DuplicateComponentGuids, "Component/@Id='{0}' with {2} '{3}' has a @Guid value '{1}' that duplicates another component in this package. It is recommended to give each component its own unique GUID.", componentId, guid, type, keyPath);
        }

        public static Message DuplicateContextValue(SourceLineNumber sourceLineNumbers, string contextValue)
        {
            return Message(sourceLineNumbers, Ids.DuplicateContextValue, "The context value '{0}' was duplicated. Context values must be distinct.", contextValue);
        }

        public static Message DuplicatedUiLocalization(SourceLineNumber sourceLineNumbers, string controlName, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatedUiLocalization, "The localization for control {0} in dialog {1} is duplicated. Only one localization per control is allowed.", controlName, dialogName);
        }

        public static Message DuplicatedUiLocalization(SourceLineNumber sourceLineNumbers, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatedUiLocalization, "The localization for dialog {0} is duplicated. Only one localization per dialog is allowed.", dialogName);
        }

        public static Message DuplicateExtensionPreprocessorType(string extension, string variablePrefix, string collidingExtension)
        {
            return Message(null, Ids.DuplicateExtensionPreprocessorType, "The extension '{0}' uses the same preprocessor variable prefix, '{1}', as previously loaded extension '{2}'. Please remove one of the extensions or rename the prefix to avoid the collision.", extension, variablePrefix, collidingExtension);
        }

        public static Message DuplicateExtensionTable(string extension, string tableName)
        {
            return Message(null, Ids.DuplicateExtensionTable, "The extension '{0}' contains a definition for table '{1}' that collides with a previously loaded table definition. Please remove one of the conflicting extensions or rename one of the tables to avoid the collision.", extension, tableName);
        }

        public static Message DuplicateExtensionXmlSchemaNamespace(string extension, string extensionXmlSchemaNamespace, string collidingExtension)
        {
            return Message(null, Ids.DuplicateExtensionXmlSchemaNamespace, "The extension '{0}' uses the same xml schema namespace, '{1}', as previously loaded extension '{2}'. Please either remove one of the extensions or rename the xml schema namespace to avoid the collision.", extension, extensionXmlSchemaNamespace, collidingExtension);
        }

        public static Message DuplicateFileId(string fileId)
        {
            return Message(null, Ids.DuplicateFileId, "Multiple files with ID '{0}' exist. Windows Installer does not support file IDs that differ only by case. Change the file IDs to be unique.", fileId);
        }

        public static Message DuplicateLocalizationIdentifier(SourceLineNumber sourceLineNumbers, string localizationId)
        {
            return Message(sourceLineNumbers, Ids.DuplicateLocalizationIdentifier, "The localization identifier '{0}' has been duplicated in multiple locations. Please resolve the conflict.", localizationId);
        }

        public static Message DuplicateModuleCaseInsensitiveFileIdentifier(SourceLineNumber sourceLineNumbers, string moduleId, string fileId1, string fileId2)
        {
            return Message(sourceLineNumbers, Ids.DuplicateModuleCaseInsensitiveFileIdentifier, "The merge module '{0}' contains 2 or more file identifiers that only differ by case: '{1}' and '{2}'. The WiX toolset extracts merge module files to the file system using these identifiers. Since most file systems are not case-sensitive a collision is likely. Please contact the owner of the merge module for a fix.", moduleId, fileId1, fileId2);
        }

        public static Message DuplicateModuleFileIdentifier(SourceLineNumber sourceLineNumbers, string moduleId, string fileId)
        {
            return Message(sourceLineNumbers, Ids.DuplicateModuleFileIdentifier, "The merge module '{0}' contains a file identifier, '{1}', that is duplicated either in another merge module or in a File/@Id attribute. File identifiers must be unique. Please change one of the file identifiers to a different value.", moduleId, fileId);
        }

        public static Message DuplicatePrimaryKey(SourceLineNumber sourceLineNumbers, string primaryKey, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatePrimaryKey, "The primary key '{0}' is duplicated in table '{1}'. Please remove one of the entries or rename a part of the primary key to avoid the collision.", primaryKey, tableName);
        }

        public static Message DuplicateProviderDependencyKey(string providerKey, string packageId)
        {
            return Message(null, Ids.DuplicateProviderDependencyKey, "The provider dependency key '{0}' was already imported from the package with Id '{1}'. Please remove the Provides element with the key '{0}' from the package authoring.", providerKey, packageId);
        }

        public static Message DuplicateSourcesForOutput(string sourceList, string outputFile)
        {
            return Message(null, Ids.DuplicateSourcesForOutput, "Multiple source files ({0}) have resulted in the same output file '{1}'. This is likely because the source files only differ in extension or path. Rename the source files to avoid this problem.", sourceList, outputFile);
        }

        public static Message DuplicateSymbol(SourceLineNumber sourceLineNumbers, string symbolName)
        {
            return Message(sourceLineNumbers, Ids.DuplicateSymbol, "Duplicate symbol '{0}' found. This typically means that an Id is duplicated. Access modifiers (internal, protected, private) cannot prevent these conflicts. Ensure all your identifiers of a given type (File, Component, Feature) are unique.", symbolName);
        }

        public static Message DuplicateSymbol(SourceLineNumber sourceLineNumbers, string symbolName, string referencingSourceLineNumber)
        {
            return Message(sourceLineNumbers, Ids.DuplicateSymbol, "Duplicate symbol '{0}' referenced by {1}. This typically means that an Id is duplicated. Ensure all your identifiers of a given type (File, Component, Feature) are unique or use an access modifier to scope the identfier.", symbolName, referencingSourceLineNumber);
        }

        public static Message DuplicateSymbol2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DuplicateSymbol2, "Location of symbol related to previous error.");
        }

        public static Message DuplicateTransform(string transform)
        {
            return Message(null, Ids.DuplicateTransform, "The transform {0} was included twice on the command line. Each transform can be applied to a patch only once.", transform);
        }

        public static Message DuplicateVariableDefinition(string variableName, string variableValue, string variableCollidingValue)
        {
            return Message(null, Ids.DuplicateVariableDefinition, "The variable '{0}' with value '{1}' was previously declared with value '{2}'.", variableName, variableValue, variableCollidingValue);
        }

        public static Message ExampleGuid(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.ExampleGuid, "The {0}/@{1} attribute's value, '{2}', is not a legal Guid value. A Guid needs to be generated and put in place of '{2}' in the source file.", elementName, attributeName, value);
        }

        public static Message ExpectedArgument(string argument)
        {
            return Message(null, Ids.ExpectedArgument, "{0} is expected to be followed by a value. See -? for additional detail.", argument);
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

        public static Message ExpectedAttributeOrElement(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string childElement)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeOrElement, "Element '{0}' missing attribute '{1}' or child element '{2}'. Exactly one of those is required.", parentElement, attribute, childElement);
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

        public static Message ExpectedAttributesWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributesWithoutOtherAttribute, "The {0} element's {1} or {2} attribute was not found; one of these is required without attribute {3} present.", elementName, attributeName1, attributeName2, otherAttributeName);
        }

        public static Message ExpectedAttributeWhenElementNotUnderElement(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWhenElementNotUnderElement, "The '{0}/@{1}' attribute was not found; it is required when element '{0}' is not nested under a '{2}' element.", elementName, attributeName, parentElementName);
        }

        public static Message ExpectedAttributeWithElement(SourceLineNumber sourceLineNumbers, string elementName, string attribute, string childElementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWithElement, "The {0} element must have attribute '{1}' when child element '{2}' is present.", elementName, attribute, childElementName);
        }

        public static Message ExpectedAttributeWithoutOtherAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWithoutOtherAttributes, "The {0} element's {1} attribute was not found; it is required without attribute {2} present.", elementName, attributeName, otherAttributeName);
        }

        public static Message ExpectedAttributeWithoutOtherAttributes(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName1, string otherAttributeName2)
        {
            return Message(sourceLineNumbers, Ids.ExpectedAttributeWithoutOtherAttributes, "The {0} element's {1} attribute was not found; it is required without attribute {2} or {3} present.", elementName, attributeName, otherAttributeName1, otherAttributeName2);
        }

        public static Message ExpectedBinaryCategory(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedBinaryCategory, "The Column element specifies a binary column but does not have the correct Category specified. Windows Installer requires binary columns to specify their category as binary. Please set the Category attribute's value to 'Binary'.");
        }

        public static Message ExpectedClientPatchIdInWixMsp()
        {
            return Message(null, Ids.ExpectedClientPatchIdInWixMsp, "The WixMsp is missing the client patch ID. Recompile the patch source files with the latest WiX toolset.");
        }

        public static Message ExpectedDecompiler(string identifier)
        {
            return Message(null, Ids.ExpectedDecompiler, "No decompiler was provided. {0} requires a decompiler.", identifier);
        }

        public static Message ExpectedDirectory(string directory)
        {
            return Message(null, Ids.ExpectedDirectory, "The directory '{0}' could not be found.", directory);
        }

        public static Message ExpectedDirectoryGotFile(string option, string path)
        {
            return Message(null, Ids.ExpectedDirectoryGotFile, "The {0} option requires a directory, but the provided path is a file: {1}", option, path);
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

        public static Message ExpectedEndElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedEndElement, "The end element matching the '{0}' start element was not found.", elementName);
        }

        public static Message ExpectedEndforeach(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedEndforeach, "A <?foreach?> statement was found that had no matching <?endforeach?>.");
        }

        public static Message ExpectedExpressionAfterNot(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.ExpectedExpressionAfterNot, "Expecting an argument for 'NOT' in expression '{0}'.", expression);
        }

        public static Message ExpectedFileGotDirectory(string option, string path)
        {
            return Message(null, Ids.ExpectedFileGotDirectory, "The {0} option requires a file, but the provided path is a directory: {1}", option, path);
        }

        public static Message ExpectedMediaCabinet(SourceLineNumber sourceLineNumbers, string fileId, int diskId)
        {
            return Message(sourceLineNumbers, Ids.ExpectedMediaCabinet, "The file '{0}' should be compressed but is not part of a compressed media. Files will be compressed if either the File/@Compressed or Package/@Compressed attributes are set to 'yes'. This can be fixed by setting the Media/@Cabinet attribute for media '{1}'.", fileId, diskId);
        }

        public static Message ExpectedMediaRowsInWixMsp()
        {
            return Message(null, Ids.ExpectedMediaRowsInWixMsp, "The WixMsp has no media rows defined.");
        }

        public static Message ExpectedParentWithAttribute(SourceLineNumber sourceLineNumbers, string parentElement, string attribute, string grandparentElement)
        {
            return Message(sourceLineNumbers, Ids.ExpectedParentWithAttribute, "When the {0}/@{1} attribute is specified, the {0} element must be nested under a {2} element.", parentElement, attribute, grandparentElement);
        }

        public static Message ExpectedPatchIdInWixMsp()
        {
            return Message(null, Ids.ExpectedPatchIdInWixMsp, "The WixMsp is missing the patch ID.");
        }

        public static Message ExpectedRowInPatchCreationPackage(string tableName)
        {
            return Message(null, Ids.ExpectedRowInPatchCreationPackage, "Could not find a row in the '{0}' table for this patch creation package. Patch creation packages must contain at least one row in the '{0}' table.", tableName);
        }

        public static Message ExpectedSignedCabinetName(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedSignedCabinetName, "The Media/@Cabinet attribute was not found; it is required when this element contains a DigitalSignature child element. This is because Windows Installer can only verify the digital signatures of external cabinets. Please either remove the DigitalSignature element or specify a valid external cabinet name via the Cabinet attribute.");
        }

        public static Message ExpectedTableInMergeModule(string identifier)
        {
            return Message(null, Ids.ExpectedTableInMergeModule, "The table '{0}' was expected but was missing.", identifier);
        }

        public static Message ExpectedVariable(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.ExpectedVariable, "A required variable was missing in the expression '{0}'.", expression);
        }

        public static Message ExpectedBindVariableValue(string variableId)
        {
            return Message(null, Ids.ExpectedBindVariableValue, "The bind variable '{0}' was declared without a value. Please specify a value for the variable.", variableId);
        }

        public static Message FamilyNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int length)
        {
            return Message(sourceLineNumbers, Ids.FamilyNameTooLong, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. This is too long for a family name because the maximum allowed length is 8 characters long.", elementName, attributeName, value, length);
        }

        public static Message FeatureCannotFavorAndDisallowAdvertise(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.FeatureCannotFavorAndDisallowAdvertise, "The {0}/@{1} attribute's value, '{2}', cannot coexist with the {3} attribute's value of '{4}'. These options would ask the installer to disallow the advertised state for this feature while at the same time favoring it.", elementName, attributeName, value, otherAttributeName, otherValue);
        }

        public static Message FeatureCannotFollowParentAndFavorLocalOrSource(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.FeatureCannotFollowParentAndFavorLocalOrSource, "The {0}/@{1} attribute cannot be specified if the {2} attribute's value is '{3}'. These options would ask the installer to force this feature to follow the parent installation state and simultaneously favor a particular installation state just for this feature.", elementName, attributeName, otherAttributeName, otherValue);
        }

        public static Message FeatureConfigurableDirectoryNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.FeatureConfigurableDirectoryNotUppercase, "The {0}/@{1} attribute's value, '{2}', contains lowercase characters. Since this directory is user-configurable, it needs to be a public property. This means the value must be completely uppercase.", elementName, attributeName, value);
        }

        public static Message FeatureNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.FeatureNameTooLong, "The {0}/@{1} attribute with value '{2}', is too long for a feature name. Due to limitations in the Windows Installer, feature names cannot be longer than 38 characters in length.", elementName, attributeName, attributeValue);
        }

        public static Message FileIdentifierNotFound(SourceLineNumber sourceLineNumbers, string fileIdentifier)
        {
            return Message(sourceLineNumbers, Ids.FileIdentifierNotFound, "The file row with identifier '{0}' could not be found.", fileIdentifier);
        }

        public static Message FileInUse(SourceLineNumber sourceLineNumbers, string file)
        {
            return Message(sourceLineNumbers, Ids.FileInUse, "The process can not access the file '{0}' because it is being used by another process.", file);
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

        public static Message FileOrDirectoryPathRequired(string parameter)
        {
            return Message(null, Ids.FileOrDirectoryPathRequired, "The parameter '{0}' must be followed by a file or directory path. To specify a directory path the string must end with a backslash, for example: \"C:\\Path\\\".", parameter);
        }

        public static Message FilePathRequired(string filePurpose)
        {
            return Message(null, Ids.FilePathRequired, "The path to the {0} is required.", filePurpose);
        }

        public static Message FilePathRequired(string parameter, string filePurpose)
        {
            return Message(null, Ids.FilePathRequired, "The parameter '{0}' must be followed by a file path for the {1}.", parameter, filePurpose);
        }

        public static Message FileTooLarge(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.FileTooLarge, "'{0}' is too large, file size must be less than 2147483648.", fileName);
        }

        public static Message FileWriteError(string path, string error)
        {
            return Message(null, Ids.FileWriteError, "Error writing to the path: '{0}'. Error message: '{1}'", path, error);
        }

        public static Message FinishCabFailed()
        {
            return Message(null, Ids.FinishCabFailed, "An error (E_FAIL) was returned while finalizing a CAB file. This most commonly happens when creating a CAB file with more than 65535 files in it or a compressed size greater than 2GB. Either reduce the number of files in your installation package or split your installation package's files into more than one CAB file using the Media element.");
        }

        public static Message FullTempDirectory(string prefix, string directory)
        {
            return Message(null, Ids.FullTempDirectory, "Unable to create temporary file. A common cause is that too many files that have names beginning with '{0}' are present. Delete any unneeded files in the '{1}' directory and try again.", prefix, directory);
        }

        public static Message GACAssemblyIdentityWarning(SourceLineNumber sourceLineNumbers, string fileName, string assemblyName)
        {
            return Message(sourceLineNumbers, Ids.GACAssemblyIdentityWarning, "The destination name of file '{0}' does not match its assembly name '{1}' in your authoring. This will cause an installation failure for this assembly, because it will be installed to the Global Assembly Cache. To fix this error, update File/@Name of file '{0}' to be the actual name of the assembly.", fileName, assemblyName);
        }

        public static Message GacAssemblyNoStrongName(SourceLineNumber sourceLineNumbers, string assemblyName, string componentName)
        {
            return Message(sourceLineNumbers, Ids.GacAssemblyNoStrongName, "Assembly {0} in component {1} has no strong name and has been marked to be placed in the GAC. All assemblies installed to the GAC must have a valid strong name.", assemblyName, componentName);
        }

        public static Message GenericReadNotAllowed(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.GenericReadNotAllowed, "Permission elements cannot have GenericRead as the only permission specified. Include at least one other permission.");
        }

        public static Message GuidContainsLowercaseLetters(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.GuidContainsLowercaseLetters, "The {0}/@{1} attribute's value, '{2}', is a mixed-case guid. All letters in a guid value should be uppercase.", elementName, attributeName, value);
        }

        public static Message HarvestSourceNotSpecified()
        {
            return Message(null, Ids.HarvestSourceNotSpecified, "A harvest source must be specified after the harvest type and can be followed by harvester arguments.");
        }

        public static Message HarvestTypeNotFound()
        {
            return Message(null, Ids.HarvestTypeNotFound, "The harvest type was not found in the list of loaded Heat extensions.");
        }

        public static Message HarvestTypeNotFound(string harvestType)
        {
            return Message(null, Ids.HarvestTypeNotFound, "The harvest type '{0}' was specified. Harvest types cannot start with a '-'. Remove the '-' to specify a valid harvest type.", harvestType);
        }

        public static Message IdentifierNotFound(string type, string identifier)
        {
            return Message(null, Ids.IdentifierNotFound, "An expected identifier ('{1}', of type '{0}') was not found.", type, identifier);
        }

        public static Message IdentifierTooLongError(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int maxLength)
        {
            return Message(sourceLineNumbers, Ids.IdentifierTooLongError, "The {0}/@{1} attribute's value, '{2}', is too long. {0}/@{1} attribute's must be {3} characters long or less.", elementName, attributeName, value, maxLength);
        }

        public static Message IllegalAttributeExceptOnElement(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string expectedElementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeExceptOnElement, "The {1} attribute can only be specified on the {2} element.", elementName, attributeName, expectedElementName);
        }

        public static Message IllegalAttributeInMergeModule(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeInMergeModule, "The {0}/@{1} attribute cannot be specified in a merge module.", elementName, attributeName);
        }

        public static Message IllegalAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, params string[] legalValues)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValue, "The {0}/@{1} attribute's value, '{2}', is not one of the legal options: '{3}'.", elementName, attributeName, value, String.Join(",", legalValues));
        }

        public static Message IllegalAttributeValueWhenNested(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attrivuteValue, string parentElementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWhenNested, "The {0}/@{1} attribute value, '{2}', cannot be specified when the {0} element is nested underneath a {3} element.", elementName, attributeName, attrivuteValue, parentElementName);
        }

        public static Message IllegalAttributeValueWithIllegalList(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string illegalValueList)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithIllegalList, "The {0}/@{1} attribute's value, '{2}', is one of the illegal options: {3}.", elementName, attributeName, value, illegalValueList);
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

        public static Message IllegalAttributeWhenAdvertised(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWhenAdvertised, "The {0}/@{1} attribute cannot be specified because the element is advertised.", elementName, attributeName);
        }

        public static Message IllegalAttributeWhenNested(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string parentElement)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWhenNested, "The {0}/@{1} attribute cannot be specified when the {0} element is nested underneath a {2} element. If this {0} is a member of a ComponentGroup where ComponentGroup/@{1} is set, then the {0}/@{1} attribute should be removed.", elementName, attributeName, parentElement);
        }

        public static Message IllegalAttributeWithInnerText(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithInnerText, "The {0}/@{1} attribute cannot be specified when the element has body text as well. Specify either the attribute or the body, but not both.", elementName, attributeName);
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

        public static Message IllegalBinderClassName()
        {
            return Message(null, Ids.IllegalBinderClassName, "Illegal binder class name specified for -binder command line option.");
        }

        public static Message IllegalCabbingThreadCount(string numThreads)
        {
            return Message(null, Ids.IllegalCabbingThreadCount, "Illegal number of threads to create cabinets: '{0}' for -ct <N> command line option. Specify the number of threads to use like -ct 2.", numThreads);
        }

        public static Message IllegalCharactersInPath(string pathName)
        {
            return Message(null, Ids.IllegalCharactersInPath, "Illegal characters in path '{0}'. Ensure you provided a valid path to the file.", pathName);
        }

        public static Message IllegalCodepage(int codepage)
        {
            return Message(null, Ids.IllegalCodepage, "The code page '{0}' is not a valid Windows code page. Update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, PatchCreation/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message IllegalCodepage(SourceLineNumber sourceLineNumbers, int codepage)
        {
            return Message(sourceLineNumbers, Ids.IllegalCodepage, "The code page '{0}' is not a valid Windows code page. Update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, PatchCreation/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message IllegalCodepageAttribute(SourceLineNumber sourceLineNumbers, string codepage, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalCodepageAttribute, "The code page '{0}' is not a valid Windows code page. Please check the {1}/@{2} attribute value in your source file.", codepage, elementName, attributeName);
        }

        public static Message IllegalColumnName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalColumnName, "The {0}/@{1} attribute's value, '{2}', is not a legal column name. It will collide with the sentinel values used in the _TransformView table.", elementName, attributeName, value);
        }

        public static Message IllegalCommandLineArgumentValue(string arg, string value, IEnumerable<string> validValues)
        {
            var combinedValidValues = String.Join(", ", validValues);
            return Message(null, Ids.IllegalCommandLineArgumentValue, "The argument {0} value '{1}' is invalid. Use one of the following values {2}", arg, value, combinedValidValues);
        }

        public static Message IllegalComponentWithAutoGeneratedGuid(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalComponentWithAutoGeneratedGuid, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components using a Directory as a KeyPath or containing ODBCDataSource child elements cannot use an automatically generated guid. Make sure your component doesn't have a Directory as the KeyPath and move any ODBCDataSource child elements to components with explicit component guids.");
        }

        public static Message IllegalComponentWithAutoGeneratedGuid(SourceLineNumber sourceLineNumbers, bool registryKeyPath)
        {
            return Message(sourceLineNumbers, Ids.IllegalComponentWithAutoGeneratedGuid, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with registry keypaths and files cannot use an automatically generated guid. Create multiple components, each with one file and/or one registry value keypath, to use automatically generated guids.", registryKeyPath);
        }

        public static Message IllegalCompressionLevel(SourceLineNumber sourceLineNumbers, string compressionLevel)
        {
            return Message(sourceLineNumbers, Ids.IllegalCompressionLevel, "The compression level '{0}' is not valid. Valid values are 'none', 'low', 'medium', 'high', and 'mszip'.", compressionLevel);
        }

        public static Message IllegalDefineStatement(SourceLineNumber sourceLineNumbers, string defineStatement)
        {
            return Message(sourceLineNumbers, Ids.IllegalDefineStatement, "The define statement '<?define {0}?>' is not well-formed. Define statements should be in the form <?define variableName = \"variable value\"?>.", defineStatement);
        }

        public static Message IllegalEmptyAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalEmptyAttributeValue, "The {0}/@{1} attribute's value cannot be an empty string. If a value is not required, simply remove the entire attribute.", elementName, attributeName);
        }

        public static Message IllegalEmptyAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string defaultValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalEmptyAttributeValue, "The {0}/@{1} attribute's value cannot be an empty string. To use the default value \"{2}\", simply remove the entire attribute.", elementName, attributeName, defaultValue);
        }

        public static Message IllegalEnvironmentVariable(string environmentVariable, string value)
        {
            return Message(null, Ids.IllegalEnvironmentVariable, "The {0} environment variable is set to an invalid value of '{1}'.", environmentVariable, value);
        }

        public static Message IllegalFamilyName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalFamilyName, "The {0}/@{1} attribute's value, '{2}', contains illegal characters for a family name. Legal values include letters, numbers, and underscores.", elementName, attributeName, value);
        }

        public static Message IllegalFileCompressionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalFileCompressionAttributes, "Cannot have both the MsidbFileAttributesCompressed and MsidbFileAttributesNoncompressed options set in a file attributes column.");
        }

        public static Message IllegalForeach(SourceLineNumber sourceLineNumbers, string foreachStatement)
        {
            return Message(sourceLineNumbers, Ids.IllegalForeach, "The foreach statement '{0}' is illegal. The proper format for foreach is <?foreach varName in valueList?>.", foreachStatement);
        }

        public static Message IllegalGeneratedGuidComponentUnversionedKeypath(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalGeneratedGuidComponentUnversionedKeypath, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with more than one file cannot use an automatically generated guid unless a versioned file is the keypath and the other files are unversioned. This component's keypath is not versioned. Create multiple components to use automatically generated guids.");
        }

        public static Message IllegalGeneratedGuidComponentVersionedNonkeypath(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalGeneratedGuidComponentVersionedNonkeypath, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with more than one file cannot use an automatically generated guid unless a versioned file is the keypath and the other files are unversioned. This component has a non-keypath file that is versioned. Create multiple components to use automatically generated guids.");
        }

        public static Message IllegalGuidValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalGuidValue, "The {0}/@{1} attribute's value, '{2}', is not a legal guid value.", elementName, attributeName, value);
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

        public static Message IllegalIdentifierLooksLikeFormatted(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifierLooksLikeFormatted, "The {0}/@{1} attribute's value, '{2}', is not a legal identifier. The {0}/@{1} attribute does not support formatted string values, such as property names enclosed in brackets ([LIKETHIS]). The value must be the identifier of another element, such as the Directory/@Id attribute value.", elementName, attributeName, value);
        }

        public static Message IllegalInlineLocVariable(SourceLineNumber sourceLineNumbers, string variableName, string variableValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalInlineLocVariable, "The localization variable '{0}' specifies an illegal inline default value of '{1}'. Localization variables cannot specify default values inline, instead the value should be specified in a WiX localization (.wxl) file.", variableName, variableValue);
        }

        public static Message IllegalIntegerInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.IllegalIntegerInExpression, "An illegal number was found in the expression '{0}'.", expression);
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

        public static Message IllegalLongValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalLongValue, "The {0}/@{1} attribute's value, '{2}', is not a legal long value. Legal long values are from -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807.", elementName, attributeName, value);
        }

        public static Message IllegalModuleExclusionLanguageAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalModuleExclusionLanguageAttributes, "Cannot set both ExcludeLanguage and ExcludeExceptLanguage attributes on a ModuleExclusion element.");
        }

        public static Message IllegalParentAttributeWhenNested(SourceLineNumber sourceLineNumbers, string parentElementName, string parentAttributeName, string childElement)
        {
            return Message(sourceLineNumbers, Ids.IllegalParentAttributeWhenNested, "The {0}/@{1} attribute cannot be specified when a {2} element is nested underneath the {0} element.", parentElementName, parentAttributeName, childElement);
        }

        public static Message IllegalPathForGeneratedComponentGuid(SourceLineNumber sourceLineNumbers, string componentName, string keyFilePath)
        {
            return Message(sourceLineNumbers, Ids.IllegalPathForGeneratedComponentGuid, "The component '{0}' has a key file with path '{1}'. Since this path is not rooted in one of the standard directories (like ProgramFilesFolder), this component does not fit the criteria for having an automatically generated guid. (This error may also occur if a path contains a likely standard directory such as nesting a directory with name \"Common Files\" under ProgramFilesFolder.)", componentName, keyFilePath);
        }

        public static Message IllegalPropertyCustomActionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalPropertyCustomActionAttributes, "The CustomAction sets a property but its Execute attribute is not 'immediate' (the default). Property-setting custom actions cannot be deferred.\"");
        }

        public static Message IllegalRelativeLongFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalRelativeLongFilename, "The {0}/@{1} attribute's value, '{2}', is not a valid relative long name because it contains illegal characters. Legal relative long names contain no more than 260 characters and must contain at least one non-period character. Any character except for the follow may be used: ? | > < : / * \".", elementName, attributeName, value);
        }

        public static Message IllegalRootDirectory(SourceLineNumber sourceLineNumbers, string directoryId)
        {
            return Message(sourceLineNumbers, Ids.IllegalRootDirectory, "The Directory with Id '{0}' is not a valid root directory. There may only be a single root directory per product or module and its Id attribute value must be 'TARGETDIR' and its Name attribute value must be 'SourceDir'.", directoryId);
        }

        public static Message IllegalSearchIdForParentDepth(SourceLineNumber sourceLineNumbers, string id, string parentId)
        {
            return Message(sourceLineNumbers, Ids.IllegalSearchIdForParentDepth, "When the parent DirectorySearch/@Depth attribute is greater than 1 for the DirectorySearch '{1}', the FileSearch/@Id attribute must be absent for FileSearch '{0}' unless the parent DirectorySearch/@AssignToProperty attribute value is 'yes'. Remove the FileSearch/@Id attribute for '{0}' to resolve this issue.", id, parentId);
        }

        public static Message IllegalShortFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalShortFilename, "The {0}/@{1} attribute's value, '{2}', is not a valid 8.3-compliant name. Legal names contain no more than 8 non-period characters followed by an optional period and extension of no more than 3 non-period characters. Any character except for the follow may be used: \\ ? | > < : / * \" + , ; = [ ] (space).", elementName, attributeName, value);
        }

        public static Message IllegalSuppressWarningId(string suppressedId)
        {
            return Message(null, Ids.IllegalSuppressWarningId, "Illegal value '{0}' for the -sw<N> command line option. Specify a particular warning number, like '-sw6' to suppress the warning with ID 6, or '-sw' alone to suppress all warnings.", suppressedId);
        }

        public static Message IllegalTargetDirDefaultDir(SourceLineNumber sourceLineNumbers, string defaultDir)
        {
            return Message(sourceLineNumbers, Ids.IllegalTargetDirDefaultDir, "The 'TARGETDIR' directory has an illegal DefaultDir value of '{0}'. The DefaultDir value is created from the *Name attributes of the Directory element. The TARGETDIR directory is a special directory which must have its Name attribute set to 'SourceDir'.", defaultDir);
        }

        public static Message IllegalTerminalServerCustomActionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalTerminalServerCustomActionAttributes, "The CustomAction/@TerminalServerAware attribute's value is 'yes' but the Execute attribute is not 'deferred,' 'rollback,' or 'commit.' Terminal-Server-aware custom actions must be deferred, rollback, or commit custom actions. For more information, see https://learn.microsoft.com/en-us/windows/win32/msi/terminalserver .\"");
        }

        public static Message IllegalValidationArguments()
        {
            return Message(null, Ids.IllegalValidationArguments, "You may only specify a single default type using -t or specify custom validation using -serr and -val.");
        }

        public static Message IllegalVersionValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalVersionValue, "The {0}/@{1} attribute's value, '{2}', is not a valid version. Specify a four-part version or semantic version, such as '#.#.#.#' or '#.#.#-label.#'.", elementName, attributeName, value);
        }

        public static Message IllegalWarningIdAsError(string warningId)
        {
            return Message(null, Ids.IllegalWarningIdAsError, "Illegal value '{0}' for the -wx<N> command line option. Specify a particular warning number, like '-wx6' to display the warning with ID 6 as an error, or '-wx' alone to suppress all warnings.", warningId);
        }

        public static Message IllegalBindVariablePrefix(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.IllegalBindVariablePrefix, "The bind variable $(wix.{0}) uses an illegal prefix '$'. Please use the '!' prefix instead.", variableId);
        }

        public static Message IllegalYesNoAlwaysValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalYesNoAlwaysValue, "The {0}/@{1} attribute's value, '{2}', is not a legal yes/no/always value. The only legal values are 'always', 'no' or 'yes'.", elementName, attributeName, value);
        }

        public static Message IllegalYesNoDefaultValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalYesNoDefaultValue, "The {0}/@{1} attribute's value, '{2}', is not a legal yes/no/default value. The only legal values are 'default', 'no' or 'yes'.", elementName, attributeName, value);
        }

        public static Message IllegalYesNoValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalYesNoValue, "The {0}/@{1} attribute's value, '{2}', is not a legal yes/no value. The only legal values are 'no' and 'yes'.", elementName, attributeName, value);
        }

        public static Message ImplicitComponentKeyPath(SourceLineNumber sourceLineNumbers, string componentId)
        {
            return Message(sourceLineNumbers, Ids.ImplicitComponentKeyPath, "The component '{0}' does not have an explicit key path specified. If the ordering of the elements under the Component element changes, the key path will also change. To prevent accidental changes, the key path should be set to 'yes' in one of the following locations: Component/@KeyPath, File/@KeyPath, ODBCDataSource/@KeyPath, or Registry/@KeyPath.", componentId);
        }

        public static Message InlineDirectorySyntaxRequiresPath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string identifier)
        {
            return Message(sourceLineNumbers, Ids.InlineDirectorySyntaxRequiresPath, "The {0}/@{1} attribute's value '{2}' only specifies a directory reference. The inline directory syntax requires that at least one directory be specified in addition to the value. For example, use '{3}:\\foo\\' to add a 'foo' directory.", elementName, attributeName, value, identifier);
        }

        public static Message InsecureBundleFilename(string filename)
        {
            return Message(null, Ids.InsecureBundleFilename, "The file name '{0}' creates an insecure bundle. Windows will load unnecessary compatibility shims into a bundle with that file name. These compatibility shims can be DLL hijacked allowing attackers to compromise your customers' computer. Choose a different bundle file name.", filename);
        }

        public static Message InsertInvalidSequenceActionOrder(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionNameBefore, string actionNameAfter, string actionNameNew)
        {
            return Message(sourceLineNumbers, Ids.InsertInvalidSequenceActionOrder, "Invalid order of actions {1} and {2} in sequence table {0}. Action {3} must occur after {1} and before {2}, but {2} is currently sequenced after {1}. Please fix the ordering or explicitly supply a location for the action {3}.", sequenceTableName, actionNameBefore, actionNameAfter, actionNameNew);
        }

        public static Message InsertSequenceNoSpace(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionNameBefore, string actionNameAfter, string actionNameNew)
        {
            return Message(sourceLineNumbers, Ids.InsertSequenceNoSpace, "Not enough space exists to sequence action {3} in table {0}. It must be sequenced after {1} and before {2}, but those two actions are currently sequenced next to each other. Please move one of those actions to allow {3} to be inserted between them.", sequenceTableName, actionNameBefore, actionNameAfter, actionNameNew);
        }

        public static Message InsufficientVersion(SourceLineNumber sourceLineNumbers, Version currentVersion, Version requiredVersion)
        {
            return Message(sourceLineNumbers, Ids.InsufficientVersion, "The current version of the toolset is {0}, but version {1} is required.", currentVersion, requiredVersion);
        }

        public static Message InsufficientVersion(SourceLineNumber sourceLineNumbers, Version currentVersion, Version requiredVersion, string extension)
        {
            return Message(sourceLineNumbers, Ids.InsufficientVersion, "The current version of the extension '{2}' is {0}, but version {1} is required.", currentVersion, requiredVersion, extension);
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

        public static Message InvalidAddedFileRowWithoutSequence(SourceLineNumber sourceLineNumbers, string fileRowId)
        {
            return Message(sourceLineNumbers, Ids.InvalidAddedFileRowWithoutSequence, "A row has been added to the File table with id '{1}' that does not have a sequence number assigned to it. Create your transform from a pair of msi's instead of xml outputs to get sequences assigned to your File table's rows.", fileRowId);
        }

        public static Message InvalidAssemblyFile(SourceLineNumber sourceLineNumbers, string assemblyFile, string moreInformation)
        {
            return Message(sourceLineNumbers, Ids.InvalidAssemblyFile, "The assembly file '{0}' appears to be invalid. Please ensure this is a valid assembly file and that the user has the appropriate access rights to this file. More information: {1}", assemblyFile, moreInformation);
        }

        public static Message InvalidBundle(string bundleExecutable)
        {
            return Message(null, Ids.InvalidBundle, "Unable to read bundle executable '{0}'. This is not a valid WiX bundle.", bundleExecutable);
        }

        public static Message InvalidCabinetTemplate(SourceLineNumber sourceLineNumbers, string cabinetTemplate)
        {
            return Message(sourceLineNumbers, Ids.InvalidCabinetTemplate, "CabinetTemplate attribute's value '{0}' must contain '{{0}}' and should contain no more than 8 characters followed by an optional extension of no more than 3 characters. Any character except for the follow may be used: \\ ? | > < : / * \" + , ; = [ ] (space). The Windows Installer team has recommended following the 8.3 format for external cabinet files and any other naming scheme is officially unsupported (which means it is not guaranteed to work on all platforms).", cabinetTemplate);
        }

        public static Message InvalidCommandLineFileName(string fileName, string error)
        {
            return Message(null, Ids.InvalidCommandLineFileName, "Invalid file name specified on the command line: '{0}'. Error message: '{1}'", fileName, error);
        }

        public static Message InvalidBundleCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition)
        {
            return Message(sourceLineNumbers, Ids.InvalidBundleCondition, "The {0}/@{1} attribute's value '{2}' is not a valid bundle condition.", elementName, attributeName, condition);
        }

        public static Message InvalidDateTimeFormat(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.InvalidDateTimeFormat, "The {0}/@{1} attribute's value '{2}' is not a valid date/time value. A date/time value should follow the format YYYY-MM-DDTHH:mm:ss.", elementName, attributeName, value);
        }

        public static Message InvalidDocumentElement(SourceLineNumber sourceLineNumbers, string elementName, string fileType, string expectedElementName)
        {
            return Message(sourceLineNumbers, Ids.InvalidDocumentElement, "The document element name '{0}' is invalid. A WiX {1} file must use '{2}' as the document element name.", elementName, fileType, expectedElementName);
        }

        public static Message InvalidEmbeddedUIFileName(SourceLineNumber sourceLineNumbers, string codepage)
        {
            return Message(sourceLineNumbers, Ids.InvalidEmbeddedUIFileName, "The EmbeddedUI/@Name attribute value, '{0}', does not contain an extension. Windows Installer will not load an embedded UI DLL without an extension. Include an extension or just omit the Name attribute so it defaults to the file name portion of the Source attribute value.", codepage);
        }

        public static Message CouldNotFindExtensionInPaths(string extensionPath, IEnumerable<string> checkedPaths)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be found. Checked paths: {1}", extensionPath, String.Join(", ", checkedPaths));
        }

        public static Message InvalidExtension(string extension)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be loaded.", extension);
        }

        public static Message InvalidExtension(string extension, string invalidReason)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be loaded because of the following reason: {1}", extension, invalidReason);
        }

        public static Message InvalidExtension(string extension, string extensionType, string expectedType)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' is the wrong type: '{1}'. The expected type was '{2}'.", extension, extensionType, expectedType);
        }

        public static Message InvalidExtension(string extension, string extensionType, string expectedType1, string expectedType2)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' is the wrong type: '{1}'. The expected type was '{2}' or '{3}'.", extension, extensionType, expectedType1, expectedType2);
        }

        public static Message InvalidExtensionType(string extension, string attributeType)
        {
            return Message(null, Ids.InvalidExtensionType, "Either '{1}' was not defined in the assembly or the type defined in extension '{0}' could not be loaded.", extension, attributeType);
        }

        public static Message InvalidExtensionType(string extension, string className, string expectedType)
        {
            return Message(null, Ids.InvalidExtensionType, "The extension type '{1}' in extension '{0}' does not inherit from the expected class '{2}'.", extension, className, expectedType);
        }

        public static Message InvalidExtensionType(string extension, string className, string exceptionType, string exceptionMessage)
        {
            return Message(null, Ids.InvalidExtensionType, "The type '{1}' in extension '{0}' could not be loaded. Exception type '{2}' returned the following message: {3}", extension, className, exceptionType, exceptionMessage);
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

        public static Message InvalidKeyColumn(string tableName, string columnName, string foreignTableName, int foreignColumnNumber)
        {
            return Message(null, Ids.InvalidKeyColumn, "The definition for the '{0}' table's '{1}' column is an invalid foreign key relationship to the {2} table's column number {3}. It is not a valid foreign key table column number because it is too small (less than 1) or greater than the count of columns in the foreign table's definition.", tableName, columnName, foreignTableName, foreignColumnNumber);
        }

        public static Message InvalidKeypathChange(SourceLineNumber sourceLineNumbers, string component, string transformPath)
        {
            return Message(sourceLineNumbers, Ids.InvalidKeypathChange, "Component '{0}' has a changed keypath in the transform '{1}'. Patches cannot change the keypath of a component.", component, transformPath);
        }

        public static Message InvalidManifestContent(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.InvalidManifestContent, "The manifest '{0}' does not have the required assembly/assemblyIdentity element.", fileName);
        }

        public static Message InvalidMergeLanguage(SourceLineNumber sourceLineNumbers, string mergeId, string mergeLanguage)
        {
            return Message(sourceLineNumbers, Ids.InvalidMergeLanguage, "The Merge element '{0}' specified an invalid language '{1}'. Verify that localization tokens are being properly resolved to a numeric LCID.", mergeId, mergeLanguage);
        }

        public static Message InvalidFourPartVersion(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string version)
        {
            return Message(sourceLineNumbers, Ids.InvalidFourPartVersion, "Invalid {0}/@Version '{1}'. {0} version has a max value of \"65535.65535.65535.65535\" and must be all numeric.", elementName, version);
        }

        public static Message InvalidPlatformValue(SourceLineNumber sourceLineNumbers, string value)
        {
            return Message(sourceLineNumbers, Ids.InvalidPlatformValue, "The Platform attribute has an invalid value {0}. Possible values are x86, x64, or arm64.", value);
        }

        public static Message InvalidPreprocessorFunction(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorFunction, "Ill-formed preprocessor function '${0}'. Functions must have a prefix (like 'fun.'), a name at least 1 character long, and matching opening and closing parentheses.", variable);
        }

        public static Message InvalidPreprocessorFunctionAutoVersion(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorFunctionAutoVersion, "Invalid AutoVersion template specified.");
        }

        public static Message InvalidPreprocessorPragma(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorPragma, "Malformed preprocessor pragma '{0}'. Pragmas must have a prefix, a name of at least 1 character long, and be followed by optional arguments.", variable);
        }

        public static Message InvalidPreprocessorVariable(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorVariable, "Ill-formed preprocessor variable '$({0})'. Variables must have a prefix (like 'var.', 'env.', or 'sys.') and a name at least 1 character long. If the literal string '$({0})' is desired, use '$$({0})'.", variable);
        }

        public static Message InvalidRemoveComponent(SourceLineNumber sourceLineNumbers, string component, string feature, string transformPath)
        {
            return Message(sourceLineNumbers, Ids.InvalidRemoveComponent, "Removing component '{0}' from feature '{1}' is not supported. Either the component was removed or the guid changed in the transform '{2}'. Add the component back, undo the change to the component guid, or remove the entire feature.", component, feature, transformPath);
        }

        public static Message InvalidSequenceTable(string sequenceTableName)
        {
            return Message(null, Ids.InvalidSequenceTable, "Found an invalid sequence table '{0}'.", sequenceTableName);
        }

        public static Message InvalidStringForCodepage(SourceLineNumber sourceLineNumbers, string codepage)
        {
            return Message(sourceLineNumbers, Ids.InvalidStringForCodepage, "A string was provided with characters that are not available in the specified database code page '{0}'. Either change these characters to ones that exist in the database's code page, or update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, PatchCreation/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message InvalidStubExe(string filename)
        {
            return Message(null, Ids.InvalidStubExe, "Stub executable '{0}' is not a valid Win32 executable.", filename);
        }

        public static Message InvalidSubExpression(SourceLineNumber sourceLineNumbers, string subExpression, string expression)
        {
            return Message(sourceLineNumbers, Ids.InvalidSubExpression, "Found invalid subexpression '{0}' in expression '{1}'.", subExpression, expression);
        }

        public static Message InvalidSummaryInfoCodePage(SourceLineNumber sourceLineNumbers, int codePage)
        {
            return Message(sourceLineNumbers, Ids.InvalidSummaryInfoCodePage, "The code page '{0}' is invalid for summary information. You must specify an ANSI code page.", codePage);
        }

        public static Message InvalidValidatorMessageType(string type)
        {
            return Message(null, Ids.InvalidValidatorMessageType, "Unknown validation message type '{0}'.", type);
        }

        public static Message InvalidVariableDefinition(string variableDefinition)
        {
            return Message(null, Ids.InvalidVariableDefinition, "The variable definition '{0}' is not valid. Variable definitions should be in the form -dname=value where the value is optional.", variableDefinition);
        }

        public static Message InvalidWixTransform(string fileName)
        {
            return Message(null, Ids.InvalidWixTransform, "The file '{0}' is not a valid WiX Transform.", fileName);
        }

        public static Message InvalidWixXmlNamespace(SourceLineNumber sourceLineNumbers, string wixElementName, string wixNamespace)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixXmlNamespace, "The {0} element has no namespace. Please make the {0} element look like the following: <{0} xmlns=\"{1}\">.", wixElementName, wixNamespace);
        }

        public static Message InvalidWixXmlNamespace(SourceLineNumber sourceLineNumbers, string wixElementName, string elementNamespace, string wixNamespace)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixXmlNamespace, "The {0} element has an incorrect namespace of '{1}'. Please make the {0} element look like the following: <{0} xmlns=\"{2}\">.", wixElementName, elementNamespace, wixNamespace);
        }

        public static Message InvalidXml(SourceLineNumber sourceLineNumbers, string fileType, string detail)
        {
            return Message(sourceLineNumbers, Ids.InvalidXml, "Not a valid {0} file; detail: {1}", fileType, detail);
        }

        public static Message LocalizationVariableUnknown(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.LocalizationVariableUnknown, "The localization variable !(loc.{0}) is unknown. Please ensure the variable is defined.", variableId);
        }

        public static Message MaximumCabinetSizeForLargeFileSplittingTooLarge(SourceLineNumber sourceLineNumbers, int maximumCabinetSizeForLargeFileSplitting, int maxValueOfMaxCabSizeForLargeFileSplitting)
        {
            return Message(sourceLineNumbers, Ids.MaximumCabinetSizeForLargeFileSplittingTooLarge, "'{0}' is too large. Reduce the size of maximum cabinet size for large file splitting. The maximum permitted value is '{1}' MB.", maximumCabinetSizeForLargeFileSplitting, maxValueOfMaxCabSizeForLargeFileSplitting);
        }

        public static Message MaximumUncompressedMediaSizeTooLarge(SourceLineNumber sourceLineNumbers, int maximumUncompressedMediaSize)
        {
            return Message(sourceLineNumbers, Ids.MaximumUncompressedMediaSizeTooLarge, "'{0}' is too large. Reduce the size of maximum uncompressed media size.", maximumUncompressedMediaSize);
        }

        public static Message MediaEmbeddedCabinetNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int length)
        {
            return Message(sourceLineNumbers, Ids.MediaEmbeddedCabinetNameTooLong, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. The name is too long for an embedded cabinet. It cannot be more than than 62 characters long.", elementName, attributeName, value, length);
        }

        public static Message MediaTableCollision(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MediaTableCollision, "Only one of Media and MediaTemplate tables should be authored.");
        }

        public static Message MergeExcludedModule(SourceLineNumber sourceLineNumbers, string mergeId, string otherMergeId)
        {
            return Message(sourceLineNumbers, Ids.MergeExcludedModule, "The module '{0}' cannot be merged because it excludes or is excluded by the merge module with signature '{1}'.", mergeId, otherMergeId);
        }

        public static Message MergeFeatureRequired(SourceLineNumber sourceLineNumbers, string tableName, string primaryKeys, string mergeModuleFile, string mergeId)
        {
            return Message(sourceLineNumbers, Ids.MergeFeatureRequired, "The {0} table contains a row with primary key(s) '{1}' which requires a feature to properly merge from the merge module '{2}'. Nest a MergeRef element with an Id attribute set to the value '{3}' under a Feature element to fix this error.", tableName, primaryKeys, mergeModuleFile, mergeId);
        }

        public static Message MergeLanguageFailed(SourceLineNumber sourceLineNumbers, short language, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeLanguageFailed, "The language '{0}' is supported but uses an invalid language transform in the merge module '{1}'.", language, mergeModuleFile);
        }

        public static Message MergeLanguageUnsupported(SourceLineNumber sourceLineNumbers, short language, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeLanguageUnsupported, "Could not locate language '{0}' (or a transform for this language) in the merge module '{1}'. This is likely due to an incorrectly authored Merge/@Language attribute.", language, mergeModuleFile);
        }

        public static Message MergeModuleExpectedFeature(SourceLineNumber sourceLineNumbers, string mergeId)
        {
            return Message(sourceLineNumbers, Ids.MergeModuleExpectedFeature, "The merge module '{0}' is not assigned to a feature. All merge modules must be assigned to at least one feature.", mergeId);
        }

        public static Message MergePlatformMismatch(SourceLineNumber sourceLineNumbers, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergePlatformMismatch, "'{0}' is a 64-bit merge module but the product consuming it is 32-bit. 32-bit products can consume only 32-bit merge modules.", mergeModuleFile);
        }

        public static Message MissingBundleInformation(string friendlyName)
        {
            return Message(null, Ids.MissingBundleInformation, "The Bundle is missing {0} data, and cannot continue.", friendlyName);
        }

        public static Message MissingBundleSearch(SourceLineNumber sourceLineNumbers, string searchId)
        {
            return Message(sourceLineNumbers, Ids.MissingBundleSearch, "Bundle Search with id '{0}' has no corresponding implementation symbol.", searchId);
        }

        public static Message MissingDependencyVersion(string packageId)
        {
            return Message(null, Ids.MissingDependencyVersion, "The provider dependency version was not authored for the package with Id '{0}'. Please author the Provides/@Version attribute for this package.", packageId);
        }

        public static Message MissingEntrySection()
        {
            return Message(null, Ids.MissingEntrySection, "Could not find entry section in provided list of intermediates. Supported entry section types are: Package, Bundle, Patch, PatchCreation, Module.");
        }

        public static Message MissingEntrySection(string sectionType)
        {
            return Message(null, Ids.MissingEntrySection, "Could not find entry section in provided list of intermediates. Expected section of type '{0}'.", sectionType);
        }

        public static Message MissingManifestForWin32Assembly(SourceLineNumber sourceLineNumbers, string file, string manifest)
        {
            return Message(sourceLineNumbers, Ids.MissingManifestForWin32Assembly, "File '{0}' is marked as a Win32 assembly but it refers to assembly manifest '{1}' that is not present in this product.", file, manifest);
        }

        public static Message MissingMedia(SourceLineNumber sourceLineNumbers, int diskId)
        {
            return Message(sourceLineNumbers, Ids.MissingMedia, "There is no media defined for disk id '{0}'. You must author either <Media Id='{0}' ...> or <MediaTemplate ...>.", diskId);
        }

        public static Message MissingOrInvalidModuleInstallerVersion(SourceLineNumber sourceLineNumbers, string moduleId, string mergeModuleFile, string productInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.MissingOrInvalidModuleInstallerVersion, "The merge module '{0}' from file '{1}' is either missing or has an invalid installer version. The value read from the installer version in module's summary information was '{2}'. This should be a numeric value representing a valid installer version such as 200 or 301.", moduleId, mergeModuleFile, productInstallerVersion);
        }

        public static Message MissingPackagePayload(SourceLineNumber sourceLineNumbers, string packageId, string packageType)
        {
            return Message(sourceLineNumbers, Ids.MissingPackagePayload, "There is no payload defined for package '{0}'. This is specified on the {1}Package element or a child {1}PackagePayload element.", packageId, packageType);
        }

        public static Message MissingTableDefinition(string tableName)
        {
            return Message(null, Ids.MissingTableDefinition, "Cannot find the table definitions for the '{0}' table. This is likely due to a typing error or missing extension. Please ensure all the necessary extensions are supplied on the command line with the -ext parameter.", tableName);
        }

        public static Message MissingTypeLibFile(SourceLineNumber sourceLineNumbers, string elementName, string fileElementName)
        {
            return Message(sourceLineNumbers, Ids.MissingTypeLibFile, "The {0} element is non-advertised and therefore requires a parent {1} element.", elementName, fileElementName);
        }

        public static Message MissingValidatorExtension()
        {
            return Message(null, Ids.MissingValidatorExtension, "The validator requires at least one extension. Add \"ValidatorExtension, Wix\" for the default implementation.");
        }

        public static Message MsiTransactionInvalidPackage(SourceLineNumber sourceLineNumbers, string packageId, string packageType)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionInvalidPackage, "Invalid package '{0}' in MSI transaction. It is type '{1}' but must be Msi or Msp.", packageId, packageType);
        }

        public static Message MsiTransactionInvalidPackage2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionInvalidPackage2, "Location of rollback boundary related to previous error.");
        }

        public static Message MsiTransactionX86BeforeX64Package(SourceLineNumber sourceLineNumbers, string x64PackageId, string x86PackageId)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionX86BeforeX64Package, "Package '{0}' is x64 but Package '{1}' is x86. MSI transactions must install all x64 packages before any x86 package.", x64PackageId, x86PackageId);
        }

        public static Message MsiTransactionX86BeforeX64Package2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionX86BeforeX64Package2, "Location of x86 package related to previous error.");
        }

        public static Message MultipleEntrySections(SourceLineNumber sourceLineNumbers, string sectionName1, string sectionName2)
        {
            return Message(sourceLineNumbers, Ids.MultipleEntrySections, "Multiple entry sections '{0}' and '{1}' found. Only one entry section may be present in a single target.", sectionName1, sectionName2);
        }

        public static Message MultipleEntrySections2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultipleEntrySections2, "Location of entry section related to previous error.");
        }

        public static Message MultipleFilesMatchedWithOutputSpecification(string sourceSpecification, string sourceList)
        {
            return Message(null, Ids.MultipleFilesMatchedWithOutputSpecification, "A per-source file output specification has been provided ('{0}'), but multiple source files match the source specification ({1}). Specifying a unique output requires that only a single source file match.", sourceSpecification, sourceList);
        }

        public static Message MultipleIdentifiersFound(SourceLineNumber sourceLineNumbers, string elementName, string identifier, string mismatchIdentifier)
        {
            return Message(sourceLineNumbers, Ids.MultipleIdentifiersFound, "Under a '{0}' element, multiple identifiers were found: '{1}' and '{2}'. All search elements under this element must have the same id.", elementName, identifier, mismatchIdentifier);
        }

        public static Message MultiplePackagePayloads(SourceLineNumber sourceLineNumbers, string packageId, string packagePayloadId1, string packagePayloadId2)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads, "The package '{0}' has multiple PackagePayloads: '{1}' and '{2}'. This normally happens when the payload is defined on the package element and a child PackagePayload element.", packageId, packagePayloadId1, packagePayloadId2);
        }

        public static Message MultiplePackagePayloads2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads2, "The location of the package payload related to previous error.");
        }

        public static Message MultiplePackagePayloads3(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultiplePackagePayloads3, "The location of the package related to previous error.");
        }

        public static Message MultiplePrimaryReferences(SourceLineNumber sourceLineNumbers, string crefChildType, string crefChildId, string crefParentType, string crefParentId, string conflictParentType, string conflictParentId)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrimaryReferences, "Multiple primary references were found for {0} '{1}' in {2} '{3}' and {4} '{5}'.", crefChildType, crefChildId, crefParentType, crefParentId, conflictParentType, conflictParentId);
        }

        public static Message MustSpecifyOutputWithMoreThanOneInput()
        {
            return Message(null, Ids.MustSpecifyOutputWithMoreThanOneInput, "You must specify an output file using the \"-o\" or \"-out\" switch when you provide more than one input file.");
        }

        public static Message NeedSequenceBeforeOrAfter(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.NeedSequenceBeforeOrAfter, "A {0} element must have a Before attribute, After attribute, or a Sequence attribute.", elementName);
        }

        public static Message NewRowAddedInTable(SourceLineNumber sourceLineNumbers, string productCode, string tableName, string rowId)
        {
            return Message(sourceLineNumbers, Ids.NewRowAddedInTable, "Product '{0}': Table '{1}' has a new row '{2}' added. This makes the patch not uninstallable.", productCode, tableName, rowId);
        }

        public static Message NoDataForColumn(SourceLineNumber sourceLineNumbers, string columnName, string tableName)
        {
            return Message(sourceLineNumbers, Ids.NoDataForColumn, "There is no data for column '{0}' in a contained row of custom table '{1}'. A non-null value must be supplied for this column.", columnName, tableName);
        }

        public static Message NoDifferencesInTransform(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.NoDifferencesInTransform, "The transform being built did not contain any differences so it could not be created.");
        }

        public static Message NoFirstControlSpecified(SourceLineNumber sourceLineNumbers, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.NoFirstControlSpecified, "The '{0}' dialog element does not have a valid tabbable control. You must either have a tabbable control that is not marked TabSkip='yes', or you must mark a control TabSkip='no'. If you have a page with no tabbable controls (a progress page, for example), you might want to set the first Text control to be TabSkip='no'.", dialogName);
        }

        public static Message NonterminatedPreprocessorInstruction(SourceLineNumber sourceLineNumbers, string beginInstruction, string endInstruction)
        {
            return Message(sourceLineNumbers, Ids.NonterminatedPreprocessorInstruction, "Found a <?{0}?> processing instruction without a matching <?{1}?> after it.", beginInstruction, endInstruction);
        }

        public static Message NoUniqueActionSequenceNumber(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2)
        {
            return Message(sourceLineNumbers, Ids.NoUniqueActionSequenceNumber, "The {0} table contains an action '{1}' which cannot have a unique sequence number because it is scheduled before or after action '{2}'. There is not enough room before or after this action to assign a unique sequence number. Please schedule one of the actions differently so that it will be in a position with more sequence numbers available. Please note that sequence numbers must be an integer in the range 1 - 32767 (inclusive).", sequenceTableName, actionName1, actionName2);
        }

        public static Message NoUniqueActionSequenceNumber2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.NoUniqueActionSequenceNumber2, "The location of the sequenced action related to previous error.");
        }

        public static Message OpenDatabaseFailed(string databaseFile)
        {
            return Message(null, Ids.OpenDatabaseFailed, "Failed to open database '{0}'. Ensure it is a valid database, and it is not open by another process.", databaseFile);
        }

        public static Message OrderingReferenceLoopDetected(SourceLineNumber sourceLineNumbers, string loopList)
        {
            return Message(sourceLineNumbers, Ids.OrderingReferenceLoopDetected, "A circular reference of ordering dependencies was detected. The infinite loop includes: {0}. Ordering dependency references must form a directed acyclic graph.", loopList);
        }

        public static Message OrphanedComponent(SourceLineNumber sourceLineNumbers, string componentName)
        {
            return Message(sourceLineNumbers, Ids.OrphanedComponent, "Found orphaned Component '{0}'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.", componentName);
        }

        public static Message OutputCodepageMismatch(SourceLineNumber sourceLineNumbers, int beforeCodepage, int afterCodepage)
        {
            return Message(sourceLineNumbers, Ids.OutputCodepageMismatch, "The code pages of the outputs do not match. One output's code page is '{0}' while the other is '{1}'.", beforeCodepage, afterCodepage);
        }

        public static Message OutputCodepageMismatch2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.OutputCodepageMismatch2, "The location of the mismatched code page related to the previous warning.");
        }

        public static Message OutputTargetNotSpecified()
        {
            return Message(null, Ids.OutputTargetNotSpecified, "The '-out' or '-o' parameter must specify a file path.");
        }

        public static Message OutputTypeMismatch(SourceLineNumber sourceLineNumbers, string beforeOutputType, string afterOutputType)
        {
            return Message(sourceLineNumbers, Ids.OutputTypeMismatch, "The types of the outputs do not match. One output's type is '{0}' while the other is '{1}'.", beforeOutputType, afterOutputType);
        }

        public static Message OverridableActionCollision(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.OverridableActionCollision, "The {0} table contains an action '{1}' that is declared overridable in two different locations. Please remove one of the actions or the Overridable='yes' attribute from one of the actions.", sequenceTableName, actionName);
        }

        public static Message OverridableActionCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.OverridableActionCollision2, "The location of the action related to previous error.");
        }

        public static Message PackagePayloadUnsupported(SourceLineNumber sourceLineNumbers, string packageType)
        {
            return Message(sourceLineNumbers, Ids.PackagePayloadUnsupported, "The {0}PackagePayload element can only be used for {0}Packages.", packageType);
        }

        public static Message PackagePayloadUnsupported2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PackagePayloadUnsupported2, "The location of the package related to previous error.");
        }

        public static Message ParentElementAttributeRequired(SourceLineNumber sourceLineNumbers, string parentElement, string parentAttribute, string childElement)
        {
            return Message(sourceLineNumbers, Ids.ParentElementAttributeRequired, "The parent {0} element is missing the {1} attribute that is required for the {2} child element.", parentElement, parentAttribute, childElement);
        }

        public static Message PatchNotRemovable()
        {
            return Message(null, Ids.PatchNotRemovable, "This patch is not uninstallable. The 'Patch' element's attribute 'AllowRemoval' should be set to 'no'.");
        }

        public static Message PatchWithoutTransforms()
        {
            return Message(null, Ids.PatchWithoutTransforms, "No transforms were provided to attach to the patch.");
        }

        public static Message PatchWithoutValidTransforms()
        {
            return Message(null, Ids.PatchWithoutValidTransforms, "No valid transforms were provided to attach to the patch. Check to make sure the transforms you passed on the command line have a matching baseline authored in the patch. Also, make sure there are differences between your target and upgrade.");
        }

        public static Message PathCannotContainQuote(string fileName)
        {
            return Message(null, Ids.PathCannotContainQuote, "Path '{0}' contains a literal quote character. Quotes are often accidentally introduced when trying to refer to a directory path with spaces in it, such as \"C:\\Out Directory\\\" -- the backslash before the quote acts an escape character. The correct representation for that path is: \"C:\\Out Directory\\\\\".", fileName);
        }

        public static Message PathTooLong(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.PathTooLong, "'{0}' is too long, the fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters.", fileName);
        }

        public static Message PayloadMustBeRelativeToCache(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.PayloadMustBeRelativeToCache, "The {0}/@{1} attribute's value, '{2}', is not a relative path.", elementName, attributeName, attributeValue);
        }

        public static Message PerUserButAllUsersEquals1(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.PerUserButAllUsersEquals1, "The MSI '{0}' is explicitly marked to not elevate so it must be a per-user package but the ALLUSERS Property is set to '1' creating a per-machine package. Remove the Property with Id='ALLUSERS' and use Package/@Scope attribute to be explicit instead.", path);
        }

        public static Message PreprocessorError(SourceLineNumber sourceLineNumbers, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorError, "{0}", message);
        }

        public static Message PreprocessorExtensionEvaluateFunctionFailed(SourceLineNumber sourceLineNumbers, string prefix, string function, string args, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionEvaluateFunctionFailed, "In the preprocessor extension that handles prefix '{0}' while trying to call function '{1}({2})' and exception has occurred : {3}", prefix, function, args, message);
        }

        public static Message PreprocessorExtensionForParameterMissing(SourceLineNumber sourceLineNumbers, string parameterName, string parameterPrefix)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionForParameterMissing, "Could not find the preprocessor extension for parameter '{0}'. A preprocessor extension is expected because the parameter prefix, '{1}', is not one of the standard types: 'env', 'res', 'sys', or 'var'.", parameterName, parameterPrefix);
        }

        public static Message PreprocessorExtensionGetVariableValueFailed(SourceLineNumber sourceLineNumbers, string prefix, string variable, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionGetVariableValueFailed, "In the preprocessor extension that handles prefix '{0}' while trying to get the value for variable '{1}' and exception has occured : {2}", prefix, variable, message);
        }

        public static Message PreprocessorExtensionPragmaFailed(SourceLineNumber sourceLineNumbers, string pragma, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionPragmaFailed, "Exception thrown while processing pragma '{0}'. The exception's message is: {1}", pragma, message);
        }

        public static Message PreprocessorIllegalForeachVariable(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorIllegalForeachVariable, "The variable named '{0}' is not allowed in a foreach expression.", variableName);
        }

        public static Message PreprocessorMissingParameterPrefix(SourceLineNumber sourceLineNumbers, string parameterName)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorMissingParameterPrefix, "Could not find the prefix in parameter name: '{0}'.", parameterName);
        }

        public static Message ProductCodeInvalidForTransform(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ProductCodeInvalidForTransform, "The value '*' is not valid for the ProductCode when used in a transform or in a patch. Copy the ProductCode from your target product MSI into the Package/@ProductCode attribute value for your product authoring.");
        }

        public static Message ProgIdNestedTooDeep(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ProgIdNestedTooDeep, "ProgId elements may not be nested more than 1 level deep.");
        }

        public static Message RadioButtonBitmapAndIconDisallowed(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RadioButtonBitmapAndIconDisallowed, "RadioButtonGroup elements that contain RadioButton elements with Bitmap or Icon attributes set to \"yes\" can only be specified under a Control element. Move your RadioButtonGroup element as a child of the appropriate Control element.");
        }

        public static Message RadioButtonTypeInconsistent(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RadioButtonTypeInconsistent, "All RadioButton elements in a RadioButtonGroup must be consistent with their use of the Bitmap, Icon, and Text attributes. Ensure all of the RadioButton elements in this group have the same attribute specified.");
        }

        public static Message ReadOnlyOutputFile(string filePath)
        {
            return Message(null, Ids.ReadOnlyOutputFile, "Unable to output to file '{0}' because it is marked as read-only.", filePath);
        }

        public static Message RealTableMissingPrimaryKeyColumn(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.RealTableMissingPrimaryKeyColumn, "The table '{0}' does not contain any primary key columns. At least one column must be marked as the primary key to ensure this table can be patched.", tableName);
        }

        public static Message RecursiveAction(string action, string tableName)
        {
            return Message(null, Ids.RecursiveAction, "The action '{0}' is recursively placed in the '{1}' table.", action, tableName);
        }

        public static Message ReferenceLoopDetected(SourceLineNumber sourceLineNumbers, string loopList)
        {
            return Message(sourceLineNumbers, Ids.ReferenceLoopDetected, "A circular reference of groups was detected. The infinite loop includes: {0}. Group references must form a directed acyclic graph.", loopList);
        }

        public static Message RegistryMultipleValuesWithoutMultiString(SourceLineNumber sourceLineNumbers, string registryElementName, string valueAttributeName, string registryValueElementName, string typeAttributeName)
        {
            return Message(sourceLineNumbers, Ids.RegistryMultipleValuesWithoutMultiString, "The {0}/@{1} attribute and a {0}/{2} element cannot both be specified. Only one may be specified if the {3} attribute's value is not 'multiString'.", registryElementName, valueAttributeName, registryValueElementName, typeAttributeName);
        }

        public static Message RegistryNameValueIncorrect(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.RegistryNameValueIncorrect, "The {0}/@{1} attribute's value, '{2}', is incorrect. It should not contain values of '+', '-', or '*' when the {0}/@Value attribute is empty. Instead, use the proper element and attributes: for Name='+' use RegistryKey/@Action='createKey', for Name='-' use RemoveRegistryKey/@Action='removeOnUninstall', for Name='*' use RegistryKey/@Action='createAndRemoveOnUninstall'.", elementName, attributeName, value);
        }

        public static Message RegistryRootInvalid(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RegistryRootInvalid, "Registry/@Root attribute is invalid on a nested Registry element. Either remove the Root attribute or move the Registry element so it is not nested under another Registry element.");
        }

        public static Message RegistrySubElementCannotBeRemoved(SourceLineNumber sourceLineNumbers, string registryElementName, string registryValueElementName, string actionAttributeName, string removeValue, string removeKeyOnInstallValue)
        {
            return Message(sourceLineNumbers, Ids.RegistrySubElementCannotBeRemoved, "The {0}/{1} element cannot be specified if the {2} attribute's value is '{3}' or '{4}'.", registryElementName, registryValueElementName, actionAttributeName, removeValue, removeKeyOnInstallValue);
        }

        public static Message RelativePathForRegistryElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RelativePathForRegistryElement, "Cannot convert RelativePath into Registry elements.");
        }

        public static Message ReservedBurnNamespaceViolation(SourceLineNumber sourceLineNumbers, string element, string attribute, string prefix)
        {
            return Message(sourceLineNumbers, Ids.ReservedNamespaceViolation, "The {0}/@{1} attribute's value begins with the reserved prefix '{2}'. Some prefixes are reserved by the WiX toolset for well-known values. Change your attribute's value to not begin with the same prefix.", element, attribute, prefix);
        }

        public static Message ReservedNamespaceViolation(SourceLineNumber sourceLineNumbers, string element, string attribute, string prefix)
        {
            return Message(sourceLineNumbers, Ids.ReservedNamespaceViolation, "The {0}/@{1} attribute's value begins with the reserved prefix '{2}'. Some prefixes are reserved by the Windows Installer and WiX toolset for well-known values. Change your attribute's value to not begin with the same prefix.", element, attribute, prefix);
        }

        public static Message RootFeatureCannotFollowParent(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RootFeatureCannotFollowParent, "The Feature element specifies a root feature with an illegal InstallDefault value of 'followParent'. Root features cannot follow their parent feature's install state because they don't have a parent feature. Please remove or change the value of the InstallDefault attribute.");
        }

        public static Message SameFileIdDifferentSource(SourceLineNumber sourceLineNumbers, string fileId, string sourcePath1, string sourcePath2)
        {
            return Message(sourceLineNumbers, Ids.SameFileIdDifferentSource, "Two different source paths '{1}' and '{2}' were detected for the same file identifier '{0}'. You must either author these under Media elements with different Id attribute values or in different patches.", fileId, sourcePath1, sourcePath2);
        }

        public static Message SamePatchBaselineId(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.SamePatchBaselineId, "The PatchBaseline/@Id attribute value '{0}' is a child of multiple Media elements. This prevents transforms from being resolved to distinct media. Change the PatchBaseline/@Id attribute values to be unique.", id);
        }

        public static Message SchemaValidationFailed(SourceLineNumber sourceLineNumbers, string validationError, int lineNumber, int linePosition)
        {
            return Message(sourceLineNumbers, Ids.SchemaValidationFailed, "Schema validation failed with the following error at line {1}, column {2}: {0}", validationError, lineNumber, linePosition);
        }

        public static Message SearchElementRequired(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.SearchElementRequired, "A '{0}' element must have a search element as a child.", elementName);
        }

        public static Message SearchElementRequiredWithAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.SearchElementRequiredWithAttribute, "A {0} element must have a search element as a child when the {0}/@{1} attribute has the value '{2}'.", elementName, attributeName, attributeValue);
        }

        public static Message SearchPropertyNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.SearchPropertyNotUppercase, "The {0}/@{1} attribute's value, '{2}', cannot contain lowercase characters. Since this is a search property, it must also be a public property. This means the Property/@Id value must be completely uppercase.", elementName, attributeName, value);
        }

        public static Message SecurePropertyNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string propertyId)
        {
            return Message(sourceLineNumbers, Ids.SecurePropertyNotUppercase, "The {0}/@{1} attribute's value, '{2}', cannot contain lowercase characters. Since this is a secure property, it must also be a public property. This means the Property/@Id value must be completely uppercase.", elementName, attributeName, propertyId);
        }

        public static Message SignedEmbeddedCabinet(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.SignedEmbeddedCabinet, "The DigitalSignature element cannot be nested under a Media element which specifies EmbedCab='yes'. This is because Windows Installer can only verify the digital signatures of external cabinets. Please either remove the DigitalSignature element or change the value of the Media/@EmbedCab attribute to 'no'.");
        }

        public static Message SingleExtensionSupported()
        {
            return Message(null, Ids.SingleExtensionSupported, "Multiple extensions were specified on the command line, only a single extension is supported.");
        }

        public static Message SpecifiedBinderNotFound(string binderClass)
        {
            return Message(null, Ids.SpecifiedBinderNotFound, "The specified binder class '{0}' was not found in any extensions.", binderClass);
        }

        public static Message SplitCabinetCopyRegistrationFailed(string newCabName, string firstCabName)
        {
            return Message(null, Ids.SplitCabinetCopyRegistrationFailed, "Failed to register the copy command for cabinet '{0}' formed by splitting cabinet '{1}'.", newCabName, firstCabName);
        }

        public static Message SplitCabinetNameCollision(string newCabName, string firstCabName)
        {
            return Message(null, Ids.SplitCabinetNameCollision, "The cabinet name '{0}' collides with the new cabinet formed by splitting cabinet '{1}', consider renaming cabinet '{0}'.", newCabName, firstCabName);
        }

        public static Message StandardActionRelativelyScheduledInModule(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.StandardActionRelativelyScheduledInModule, "The {0} table contains a standard action '{1}' that does not have a sequence number specified. The Sequence attribute is required for standard actions in a merge module. Please remove the action or use the Sequence attribute.", sequenceTableName, actionName);
        }

        public static Message StreamNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int length, int maximumLength)
        {
            return Message(sourceLineNumbers, Ids.StreamNameTooLong, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. This is too long because it will be used to create a stream name. It cannot be more than than {4} characters long.", elementName, attributeName, value, length, maximumLength);
        }

        public static Message StreamNameTooLong(SourceLineNumber sourceLineNumbers, string tableName, string streamName, int streamLength)
        {
            return Message(sourceLineNumbers, Ids.StreamNameTooLong, "The binary value in table '{0}' will be stored with a stream name, '{1}', that is {2} characters long. This is too long because the maximum allowed length for a stream name is 62 characters long. Since the stream name is created by concatenating the table name and values of the primary key for a row (delimited by periods), this error can be resolved by shortening a value that is part of the primary key.", tableName, streamName, streamLength);
        }

        public static Message StubMissingWixburnSection(string filename)
        {
            return Message(null, Ids.StubMissingWixburnSection, "Stub executable '{0}' does not contain a .wixburn data section.", filename);
        }

        public static Message StubWixburnSectionTooSmall(string filename)
        {
            return Message(null, Ids.StubWixburnSectionTooSmall, "Stub executable '{0}' .wixburn data section is too small to store the Burn container header.", filename);
        }

        public static Message SuppressNonoverridableAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.SuppressNonoverridableAction, "The {0} table contains an action '{1}' that cannot be suppressed because it is not declared overridable in the base definition. Please stop suppressing the action or make it overridable in its base declaration.", sequenceTableName, actionName);
        }

        public static Message SuppressNonoverridableAction2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.SuppressNonoverridableAction2, "The location of the non-overridable definition of the action related to previous error.");
        }

        public static Message TabbableControlNotAllowedInBillboard(SourceLineNumber sourceLineNumbers, string elementName, string controlType)
        {
            return Message(sourceLineNumbers, Ids.TabbableControlNotAllowedInBillboard, "A {0} element was specified with Type='{1}' and TabSkip='no'. Tabbable controls are not allowed in Billboards.", elementName, controlType);
        }

        public static Message TableDecompilationUnimplemented(string tableName)
        {
            return Message(null, Ids.TableDecompilationUnimplemented, "Decompilation of the {0} table has not been implemented by its extension.", tableName);
        }

        public static Message TableNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.TableNameTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for a table name. It cannot be more than than 31 characters long.", elementName, attributeName, value);
        }

        public static Message TooDeeplyIncluded(SourceLineNumber sourceLineNumbers, int depth)
        {
            return Message(sourceLineNumbers, Ids.TooDeeplyIncluded, "Include files cannot be nested more deeply than {0} times. Make sure included files don't accidentally include themselves.", depth);
        }

        public static Message TooManyChildren(SourceLineNumber sourceLineNumbers, string elementName, string childElementName)
        {
            return Message(sourceLineNumbers, Ids.TooManyChildren, "The {0} element contains multiple {1} child elements. There can only be one {1} child element per {0} element.", elementName, childElementName);
        }

        public static Message TooManyColumnsInRealTable(string tableName, int columnCount, int supportedColumnCount)
        {
            return Message(null, Ids.TooManyColumnsInRealTable, "The table '{0}' contains {1} columns which is not supported by Windows Installer. Windows Installer supports a maximum of {2} columns.", tableName, columnCount, supportedColumnCount);
        }

        public static Message TooManyElements(SourceLineNumber sourceLineNumbers, string elementName, string childElementName, int expectedInstances)
        {
            return Message(sourceLineNumbers, Ids.TooManyElements, "The {0} element contains an unexpected child element '{1}'. The '{1}' element may only occur {2} time(s) under the {0} element.", elementName, childElementName, expectedInstances);
        }

        public static Message TooManySearchElements(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.TooManySearchElements, "Only one search element can appear under a '{0}' element.", elementName);
        }

        public static Message TransformSchemaMismatch()
        {
            return Message(null, Ids.TransformSchemaMismatch, "The transform schema does not match the database schema. The transform may have been generated from a different database.");
        }

        public static Message TypeSpecificationForExtensionRequired(string parameter)
        {
            return Message(null, Ids.TypeSpecificationForExtensionRequired, "The parameter '{0}' must be followed by the extension's type specification. The type specification should be a fully qualified class and assembly identity, for example: \"MyNamespace.MyClass,myextension.dll\".", parameter);
        }

        public static Message UnableToGetAuthenticodeCertOfFile(string filePath, string moreInformation)
        {
            return Message(null, Ids.UnableToGetAuthenticodeCertOfFile, "Unable to get the authenticode certificate of '{0}'. More information: {1}", filePath, moreInformation);
        }

        public static Message UnableToGetAuthenticodeCertOfFileDownlevelOS(string filePath, string moreInformation)
        {
            return Message(null, Ids.UnableToGetAuthenticodeCertOfFileDownlevelOS, "Unable to get the authenticode certificate of '{0}'. The cryptography API has limitations on Windows XP and Windows Server 2003. More information: {1}", filePath, moreInformation);
        }

        public static Message UnableToConvertFieldToNumber(string value)
        {
            return Message(null, Ids.UnableToConvertFieldToNumber, "Unable to convert intermediate symbol field value '{0}' to a number. This means the intermediate is corrupt or of an unsupported version.", value);
        }

        public static Message UnableToOpenModule(SourceLineNumber sourceLineNumbers, string modulePath, string message)
        {
            return Message(sourceLineNumbers, Ids.UnableToOpenModule, "Unable to open merge module '{0}'. Check to make sure the module language is correct. '{1}'", modulePath, message);
        }

        public static Message UnableToReadPackageInformation(SourceLineNumber sourceLineNumbers, string packagePath, string detailedErrorMessage)
        {
            return Message(sourceLineNumbers, Ids.UnableToReadPackageInformation, "Unable to read package '{0}'. {1}", packagePath, detailedErrorMessage);
        }

        public static Message UnauthorizedAccess(string filePath)
        {
            return Message(null, Ids.UnauthorizedAccess, "Access to the path '{0}' is denied.", filePath);
        }

        public static Message UndefinedPreprocessorFunction(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.UndefinedPreprocessorFunction, "Undefined preprocessor function '$({0})'.", variableName);
        }

        public static Message UndefinedPreprocessorVariable(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.UndefinedPreprocessorVariable, "Undefined preprocessor variable '$({0})'.", variableName);
        }

        public static Message UnexpectedAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAttribute, "The {0} element contains an unexpected attribute '{1}'.", elementName, attributeName);
        }

        public static Message UnexpectedColumnCount(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedColumnCount, "A parsed row has more fields that contain data for table '{0}' than are defined. This is potentially because a standard table is being redefined as a custom table or is based on an older table schema.", tableName);
        }

        public static Message UnexpectedContentNode(SourceLineNumber sourceLineNumbers, string elementName, string unexpectedNodeType)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedContentNode, "The {0} element contains an unexpected xml node of type {1}.", elementName, unexpectedNodeType);
        }

        public static Message UnexpectedCustomTableColumn(SourceLineNumber sourceLineNumbers, string column)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedCustomTableColumn, "The custom table column '{0}' is unknown.", column);
        }

        public static Message UnexpectedElement(SourceLineNumber sourceLineNumbers, string elementName, string childElementName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedElement, "The {0} element contains an unexpected child element '{1}'.", elementName, childElementName);
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

        public static Message UnexpectedEmptySubexpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedEmptySubexpression, "The empty subexpression is unexpected in the expression '{0}'.", expression);
        }

        public static Message UnexpectedException(Exception exception)
        {
            return Message(null, Ids.UnexpectedException, exception.ToString());
        }

        public static Message UnexpectedException(string message, string type, string stackTrace)
        {
            return Message(null, Ids.UnexpectedException, "{0}\r\n\r\nException Type: {1}\r\n\r\nStack Trace:\r\n{2}", message, type, stackTrace);
        }

        public static Message UnexpectedExternalUIMessage(string message)
        {
            return Message(null, Ids.UnexpectedExternalUIMessage, "Error executing unknown ICE action. The following string format was not expected by the external UI message logger: \"{0}\".", message);
        }

        public static Message UnexpectedExternalUIMessage(string message, string action)
        {
            return Message(null, Ids.UnexpectedExternalUIMessage, "Error executing ICE action '{1}'. The following string format was not expected by the external UI message logger: \"{0}\".", message, action);
        }

        public static Message UnexpectedFileExtension(string fileName, string expectedExtensions)
        {
            return Message(null, Ids.UnexpectedFileExtension, "The file '{0}' has an unexpected extension. Expected one of the following: '{1}'.", fileName, expectedExtensions);
        }

        public static Message UnexpectedFileFormat(string path, string expectedFormat, string actualFormat)
        {
            return Message(null, Ids.UnexpectedFileFormat, "Unexpected file format loaded from path: {0}. The file was expected to be a {1} but was actually: {2}. Ensure the correct path was provided.", path, expectedFormat.ToLowerInvariant(), actualFormat.ToLowerInvariant());
        }

        public static Message UnexpectedGroupChild(string parentType, string parentId, string childType, string childId)
        {
            return Message(null, Ids.UnexpectedGroupChild, "A group parent ('{0}'/'{1}') had an unexpected child ('{2}'/'{3}').", parentType, parentId, childType, childId);
        }

        public static Message UnexpectedLiteral(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedLiteral, "An unexpected literal was found in the expression '{0}'.", expression);
        }

        public static Message UnexpectedPreprocessorOperator(SourceLineNumber sourceLineNumbers, string op)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedPreprocessorOperator, "The operator '{0}' is unexpected.", op);
        }

        public static Message UnexpectedTableInMergeModule(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInMergeModule, "An unexpected row in the '{0}' table was found in this merge module. Merge modules cannot contain the '{0}' table.", tableName);
        }

        public static Message UnexpectedTableInPatch(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInPatch, "An unexpected row in the '{0}' table was found in this patch. Patches cannot contain the '{0}' table.", tableName);
        }

        public static Message UnexpectedTableInPatchCreationPackage(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInPatchCreationPackage, "An unexpected row in the '{0}' table was found in this patch creation package. Patch creation packages cannot contain the '{0}' table.", tableName);
        }

        public static Message UnhandledExtensionAttribute(SourceLineNumber sourceLineNumbers, string elementName, string extensionAttributeName, string extensionNamespace)
        {
            return Message(sourceLineNumbers, Ids.UnhandledExtensionAttribute, "The {0} element contains an unhandled extension attribute '{1}'. Please ensure that the extension for attributes in the '{2}' namespace has been provided.", elementName, extensionAttributeName, extensionNamespace);
        }

        public static Message UnhandledExtensionElement(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName, string extensionNamespace)
        {
            return Message(sourceLineNumbers, Ids.UnhandledExtensionElement, "The {0} element contains an unhandled extension element '{1}'. Please ensure that the extension for elements in the '{2}' namespace has been provided.", elementName, extensionElementName, extensionNamespace);
        }

        public static Message UniqueFileSearchIdRequired(SourceLineNumber sourceLineNumbers, string id, string elementName)
        {
            return Message(sourceLineNumbers, Ids.UniqueFileSearchIdRequired, "The DirectorySearch element '{0}' requires that the child {1} element has a unique Id when the DirectorySearch/@AssignToProperty attribute is set to 'yes'.", id, elementName);
        }

        public static Message UnknownCustomTableColumnType(SourceLineNumber sourceLineNumbers, string columnType)
        {
            return Message(sourceLineNumbers, Ids.UnknownCustomTableColumnType, "Encountered an unknown custom table column type '{0}'.", columnType);
        }

        public static Message UnmatchedParenthesisInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedParenthesisInExpression, "The parenthesis don't match in the expression '{0}'.", expression);
        }

        public static Message UnmatchedPreprocessorInstruction(SourceLineNumber sourceLineNumbers, string beginInstruction, string endInstruction)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedPreprocessorInstruction, "Found a <?{1}?> processing instruction without a matching <?{0}?> before it.", beginInstruction, endInstruction);
        }

        public static Message UnmatchedQuotesInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedQuotesInExpression, "The quotes don't match in the expression '{0}'.", expression);
        }

        public static Message UnresolvedBindReference(SourceLineNumber sourceLineNumbers, string BindRef)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedBindReference, "Unresolved bind-time variable {0}.", BindRef);
        }

        public static Message UnresolvedReference(SourceLineNumber sourceLineNumbers, string symbolName)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedReference, "The identifier '{0}' could not be found. Ensure you have typed the reference correctly and that all the necessary inputs are provided to the linker.", symbolName);
        }

        public static Message UnresolvedReference(SourceLineNumber sourceLineNumbers, string symbolName, WixToolset.Data.AccessModifier accessModifier)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedReference, "The identifier '{0}' is inaccessible due to its protection level.", symbolName, accessModifier);
        }

        public static Message UnsupportedAllUsersValue(SourceLineNumber sourceLineNumbers, string path, string value)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedAllUsersValue, "The MSI '{0}' set the ALLUSERS Property to '{0}' which is not supported. Remove the Property with Id='ALLUSERS' and use Package/@Scope attribute instead.", path, value);
        }

        public static Message UnsupportedExtensionAttribute(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedExtensionAttribute, "The {0} element contains an unsupported extension attribute '{1}'. The {0} element does not currently support extension attributes. Is the {1} attribute using the correct XML namespace?", elementName, extensionElementName);
        }

        public static Message UnsupportedExtensionElement(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedExtensionElement, "The {0} element contains an unsupported extension element '{1}'. The {0} element does not currently support extension elements. Is the {1} element using the correct XML namespace?", elementName, extensionElementName);
        }

        public static Message UnsupportedPlatformForElement(SourceLineNumber sourceLineNumbers, string platform, string elementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedPlatformForElement, "The element {1} does not support platform '{0}'. Consider removing the element or using the preprocessor to conditionally include the element based on the platform.", platform, elementName);
        }

        public static Message ValidationError(SourceLineNumber sourceLineNumbers, string ice, string message)
        {
            return Message(sourceLineNumbers, Ids.ValidationError, "{0}: {1}", ice, message);
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

        public static Message ValueAndMaskMustBeSameLength(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ValueAndMaskMustBeSameLength, "The FileTypeMask/@Value and FileTypeMask/@Mask attributes must be the same length.");
        }

        public static Message ValueNotSupported(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.ValueNotSupported, "The {0}/@{1} attribute's value, '{2}, is not supported by the Windows Installer.", elementName, attributeName, attributeValue);
        }

        public static Message VariableDeclarationCollision(SourceLineNumber sourceLineNumbers, string variableName, string variableValue, string variableCollidingValue)
        {
            return Message(sourceLineNumbers, Ids.VariableDeclarationCollision, "The variable '{0}' with value '{1}' was previously declared with value '{2}'.", variableName, variableValue, variableCollidingValue);
        }

        public static Message VersionIndependentProgIdsCannotHaveIcons(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.VersionIndependentProgIdsCannotHaveIcons, "Version independent ProgIds cannot have Icons. Remove the Icon and/or IconIndex attributes from your ProgId element.");
        }

        public static Message VersionMismatch(SourceLineNumber sourceLineNumbers, string fileType, string version, string expectedVersion)
        {
            return Message(sourceLineNumbers, Ids.VersionMismatch, "The {0} file format version {1} is not compatible with the expected {0} file format version {2}.", fileType, version, expectedVersion);
        }

        public static Message Win32Exception(int nativeErrorCode, string message)
        {
            return Message(null, Ids.Win32Exception, "An unexpected Win32 exception with error code 0x{0:X} occurred: {1}", nativeErrorCode, message);
        }

        public static Message Win32Exception(int nativeErrorCode, string file, string message)
        {
            return Message(null, Ids.Win32Exception, "An unexpected Win32 exception with error code 0x{0:X} occurred while accessing file '{1}': {2}", nativeErrorCode, file, message);
        }

        public static Message WixFileNotFound(string file)
        {
            return Message(null, Ids.WixFileNotFound, "The file '{0}' cannot be found.", file);
        }

        public static Message BindVariableCollision(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.BindVariableCollision, "The bind variable '{0}' is declared in more than one location. Please remove one of the declarations.", variableId);
        }

        public static Message BindVariableUnknown(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.BindVariableUnknown, "The bind variable !(wix.{0}) is unknown. Please ensure the variable is declared on the command line for wix.exe, via a WixVariable element, or inline using the syntax !(wix.{0}=some value which doesn't contain parentheses).", variableId);
        }

        public static Message NoSourceFiles()
        {
            return Message(null, Ids.NoSourceFiles, "No source files specified.");
        }

        public static Message WixiplSourceFileIsExclusive()
        {
            return Message(null, Ids.WixiplSourceFileIsExclusive, "When an intermediate post link source file is specified, it must be the only source file provided.");
        }

        public static Message IntermediatesMustBeCompiled(string invalidIntermediates)
        {
            return Message(null, Ids.IntermediatesMustBeCompiled, "Intermediates being linked must have been compiled. Intermediates with these ids were not compiled: {0}", invalidIntermediates);
        }

        public static Message IntermediatesMustBeResolved(string invalidIntermediate)
        {
            return Message(null, Ids.IntermediatesMustBeResolved, "Intermediates being bound must have been resolved. This intermediate was not resolved: {0}", invalidIntermediate);
        }

        public static Message UnknownSymbolType(string symbolName)
        {
            return Message(null, Ids.UnknownSymbolType, "Could not deserialize symbol of type type '{0}' because it is not a standard symbol type or one provided by a loaded extension.", symbolName);
        }

        public static Message IllegalInnerText(SourceLineNumber sourceLineNumbers, string elementName, string innerText)
        {
            return Message(sourceLineNumbers, Ids.IllegalInnerText, "The {0} element contains illegal inner text: '{1}'.", elementName, innerText);
        }

        public static Message IllegalInnerText(SourceLineNumber sourceLineNumbers, string elementName, string innerText, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalInnerText, "The {0} element contains inner text which is obsolete. Use the {1} attribute instead.", elementName, attributeName);
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
            InsufficientVersion = 7,
            IllegalIntegerValue = 8,
            IllegalGuidValue = 9,
            ExpectedAttribute = 10,
            SecurePropertyNotUppercase = 11,
            SearchPropertyNotUppercase = 12,
            StreamNameTooLong = 13,
            IllegalIdentifier = 14,
            IllegalYesNoValue = 15,
            CommandLineCommandRequired = 16,
            CabExtractionFailed = 17,
            AppIdIncompatibleAdvertiseState = 18,
            IllegalAttributeWhenAdvertised = 19,
            ConditionExpected = 20,
            IllegalAttributeValue = 21,
            CustomActionMultipleSources = 22,
            CustomActionMultipleTargets = 23,
            IllegalShortFilename = 26,
            IllegalLongFilename = 27,
            TableNameTooLong = 28,
            FeatureConfigurableDirectoryNotUppercase = 29,
            FeatureCannotFavorAndDisallowAdvertise = 30,
            FeatureCannotFollowParentAndFavorLocalOrSource = 31,
            MediaEmbeddedCabinetNameTooLong = 32,
            RegistrySubElementCannotBeRemoved = 33,
            RegistryMultipleValuesWithoutMultiString = 34,
            IllegalAttributeWithOtherAttribute = 35,
            IllegalAttributeWithOtherAttributes = 36,
            IllegalAttributeWithoutOtherAttributes = 37,
            IllegalAttributeValueWithoutOtherAttribute = 38,
            IntegralValueSentinelCollision = 39,
            ExampleGuid = 40,
            TooManyChildren = 41,
            ComponentMultipleKeyPaths = 42,
            ExpectedAttributes = 44,
            ExpectedAttributesWithOtherAttribute = 45,
            ExpectedAttributesWithoutOtherAttribute = 46,
            MissingTypeLibFile = 47,
            InvalidDocumentElement = 48,
            ExpectedAttributeInElementOrParent = 49,
            UnauthorizedAccess = 50,
            IllegalModuleExclusionLanguageAttributes = 51,
            NoFirstControlSpecified = 52,
            NoDataForColumn = 53,
            ValueAndMaskMustBeSameLength = 54,
            TooManySearchElements = 55,
            IllegalAttributeExceptOnElement = 56,
            SearchElementRequired = 57,
            MultipleIdentifiersFound = 58,
            AdvertiseStateMustMatch = 59,
            DuplicateContextValue = 60,
            RelativePathForRegistryElement = 61,
            IllegalAttributeWhenNested = 62,
            ExpectedElement = 63,
            RegistryRootInvalid = 64,
            IllegalYesNoDefaultValue = 65,
            IllegalAttributeInMergeModule = 66,
            GenericReadNotAllowed = 67,
            IllegalAttributeWithInnerText = 68,
            SearchElementRequiredWithAttribute = 69,
            CannotAuthorSpecialProperties = 70,
            NeedSequenceBeforeOrAfter = 72,
            ValueNotSupported = 73,
            TabbableControlNotAllowedInBillboard = 74,
            CheckBoxValueOnlyValidWithCheckBox = 75,
            CabFileDoesNotExist = 76,
            RadioButtonTypeInconsistent = 77,
            RadioButtonBitmapAndIconDisallowed = 78,
            IllegalSuppressWarningId = 79,
            PreprocessorIllegalForeachVariable = 80,
            PreprocessorMissingParameterPrefix = 81,
            PreprocessorExtensionForParameterMissing = 82,
            CannotFindFile = 83,
            BinderFileManagerMissingFile = 84,
            InvalidFileName = 85,
            ReferenceLoopDetected = 86,
            GuidContainsLowercaseLetters = 87,
            InvalidDateTimeFormat = 88,
            MultipleEntrySections = 89,
            MultipleEntrySections2 = 90,
            DuplicateSymbol = 91,
            DuplicateSymbol2 = 92,
            MissingEntrySection = 93,
            UnresolvedReference = 94,
            MultiplePrimaryReferences = 95,
            ComponentReferencedTwice = 96,
            DuplicateModuleFileIdentifier = 97,
            DuplicateModuleCaseInsensitiveFileIdentifier = 98,
            ImplicitComponentKeyPath = 99,
            DuplicateLocalizationIdentifier = 100,
            LocalizationVariableUnknown = 102,
            FileNotFound = 103,
            InvalidXml = 104,
            ProgIdNestedTooDeep = 105,
            CanNotHaveTwoParents = 106,
            SchemaValidationFailed = 107,
            IllegalVersionValue = 108,
            CustomTableNameTooLong = 109,
            CustomTableIllegalColumnWidth = 110,
            CustomTableMissingPrimaryKey = 111,
            TypeSpecificationForExtensionRequired = 113,
            FilePathRequired = 114,
            DirectoryPathRequired = 115,
            FileOrDirectoryPathRequired = 116,
            PathCannotContainQuote = 117,
            AdditionalArgumentUnexpected = 118,
            RegistryNameValueIncorrect = 119,
            FamilyNameTooLong = 120,
            IllegalFamilyName = 121,
            IllegalLongValue = 122,
            IntegralValueOutOfRange = 123,
            DuplicateExtensionXmlSchemaNamespace = 125,
            DuplicateExtensionTable = 126,
            DuplicateExtensionPreprocessorType = 127,
            FileInUse = 128,
            CannotOpenMergeModule = 129,
            DuplicatePrimaryKey = 130,
            FileIdentifierNotFound = 131,
            InvalidAssemblyFile = 132,
            ExpectedEndElement = 133,
            IllegalCodepage = 134,
            ExpectedMediaCabinet = 135,
            InvalidIdt = 136,
            InvalidSequenceTable = 137,
            ExpectedDirectory = 138,
            ComponentExpectedFeature = 139,
            RecursiveAction = 140,
            VersionMismatch = 141,
            UnexpectedContentNode = 142,
            UnexpectedColumnCount = 143,
            InvalidExtension = 144,
            InvalidSubExpression = 145,
            UnmatchedPreprocessorInstruction = 146,
            NonterminatedPreprocessorInstruction = 147,
            ExpectedExpressionAfterNot = 148,
            InvalidPreprocessorVariable = 149,
            UndefinedPreprocessorVariable = 150,
            IllegalDefineStatement = 151,
            VariableDeclarationCollision = 152,
            CannotReundefineVariable = 153,
            IllegalForeach = 154,
            IllegalParentAttributeWhenNested = 155,
            ExpectedEndforeach = 156,
            UnmatchedQuotesInExpression = 158,
            UnmatchedParenthesisInExpression = 159,
            ExpectedVariable = 160,
            UnexpectedLiteral = 161,
            IllegalIntegerInExpression = 162,
            UnexpectedPreprocessorOperator = 163,
            UnexpectedEmptySubexpression = 164,
            UnexpectedCustomTableColumn = 165,
            UnknownCustomTableColumnType = 166,
            IllegalFileCompressionAttributes = 167,
            OverridableActionCollision = 168,
            OverridableActionCollision2 = 169,
            ActionCollision = 170,
            ActionCollision2 = 171,
            SuppressNonoverridableAction = 172,
            SuppressNonoverridableAction2 = 173,
            CustomActionSequencedInModule = 174,
            StandardActionRelativelyScheduledInModule = 175,
            ActionCircularDependency = 176,
            ActionScheduledRelativeToTerminationAction = 177,
            ActionScheduledRelativeToTerminationAction2 = 178,
            NoUniqueActionSequenceNumber = 179,
            NoUniqueActionSequenceNumber2 = 180,
            ActionScheduledRelativeToItself = 181,
            MissingTableDefinition = 182,
            ExpectedRowInPatchCreationPackage = 183,
            UnexpectedTableInMergeModule = 184,
            UnexpectedTableInPatchCreationPackage = 185,
            MergeExcludedModule = 186,
            MergeFeatureRequired = 187,
            MergeLanguageFailed = 188,
            MergeLanguageUnsupported = 189,
            TableDecompilationUnimplemented = 190,
            CannotDefaultMismatchedAdvertiseStates = 191,
            VersionIndependentProgIdsCannotHaveIcons = 192,
            IllegalAttributeValueWithOtherAttribute = 193,
            InvalidMergeLanguage = 194,
            BindVariableCollision = 195,
            ExpectedBindVariableValue = 196,
            BindVariableUnknown = 197,
            IllegalBindVariablePrefix = 198,
            InvalidWixXmlNamespace = 199,
            UnhandledExtensionElement = 200,
            UnhandledExtensionAttribute = 201,
            UnsupportedExtensionAttribute = 202,
            UnsupportedExtensionElement = 203,
            ValidationError = 204,
            IllegalRootDirectory = 205,
            IllegalTargetDirDefaultDir = 206,
            TooManyElements = 207,
            ExpectedBinaryCategory = 208,
            RootFeatureCannotFollowParent = 209,
            FeatureNameTooLong = 210,
            SignedEmbeddedCabinet = 211,
            ExpectedSignedCabinetName = 212,
            IllegalInlineLocVariable = 213,
            MergeModuleExpectedFeature = 215,
            Win32Exception = 216,
            UnexpectedExternalUIMessage = 217,
            IllegalCabbingThreadCount = 218,
            IllegalEnvironmentVariable = 219,
            InvalidKeyColumn = 220,
            CollidingModularizationTypes = 221,
            CubeFileNotFound = 222,
            OpenDatabaseFailed = 223,
            OutputTypeMismatch = 224,
            RealTableMissingPrimaryKeyColumn = 225,
            IllegalColumnName = 226,
            NoDifferencesInTransform = 227,
            OutputCodepageMismatch = 228,
            OutputCodepageMismatch2 = 229,
            IllegalComponentWithAutoGeneratedGuid = 230,
            IllegalPathForGeneratedComponentGuid = 231,
            IllegalTerminalServerCustomActionAttributes = 232,
            IllegalPropertyCustomActionAttributes = 233,
            InvalidPreprocessorFunction = 234,
            UndefinedPreprocessorFunction = 235,
            PreprocessorExtensionEvaluateFunctionFailed = 236,
            PreprocessorExtensionGetVariableValueFailed = 237,
            InvalidManifestContent = 238,
            InvalidWixTransform = 239,
            UnexpectedFileExtension = 240,
            UnexpectedTableInPatch = 241,
            InvalidKeypathChange = 243,
            MissingValidatorExtension = 244,
            InvalidValidatorMessageType = 245,
            PatchWithoutTransforms = 246,
            SingleExtensionSupported = 247,
            DuplicateTransform = 248,
            BaselineRequired = 249,
            PreprocessorError = 250,
            ExpectedArgument = 251,
            PatchWithoutValidTransforms = 252,
            ExpectedDecompiler = 253,
            ExpectedTableInMergeModule = 254,
            UnexpectedElementWithAttributeValue = 255,
            ExpectedPatchIdInWixMsp = 256,
            ExpectedMediaRowsInWixMsp = 257,
            WixFileNotFound = 258,
            ExpectedClientPatchIdInWixMsp = 259,
            NewRowAddedInTable = 260,
            PatchNotRemovable = 261,
            PathTooLong = 262,
            FileTooLarge = 263,
            InvalidPlatformParameter = 264,
            InvalidPlatformValue = 265,
            IllegalValidationArguments = 266,
            OrphanedComponent = 267,
            IllegalCommandLineArgumentValue = 268,
            ProductCodeInvalidForTransform = 269,
            InsertInvalidSequenceActionOrder = 270,
            InsertSequenceNoSpace = 271,
            MissingManifestForWin32Assembly = 272,
            UnableToOpenModule = 273,
            ExpectedAttributeWhenElementNotUnderElement = 274,
            IllegalIdentifierLooksLikeFormatted = 275,
            IllegalCodepageAttribute = 276,
            IllegalCompressionLevel = 277,
            TransformSchemaMismatch = 278,
            DatabaseSchemaMismatch = 279,
            ExpectedDirectoryGotFile = 280,
            ExpectedFileGotDirectory = 281,
            GacAssemblyNoStrongName = 282,
            FileWriteError = 283,
            InvalidCommandLineFileName = 284,
            ExpectedParentWithAttribute = 285,
            IllegalWarningIdAsError = 286,
            ExpectedAttributeOrElement = 287,
            DuplicateVariableDefinition = 288,
            InvalidVariableDefinition = 289,
            DuplicateCabinetName = 290,
            DuplicateCabinetName2 = 291,
            InvalidAddedFileRowWithoutSequence = 292,
            DuplicateFileId = 293,
            FullTempDirectory = 294,
            CreateCabAddFileFailed = 296,
            CreateCabInsufficientDiskSpace = 297,
            UnresolvedBindReference = 298,
            GACAssemblyIdentityWarning = 299,
            IllegalCharactersInPath = 300,
            ValidationFailedToOpenDatabase = 301,
            MustSpecifyOutputWithMoreThanOneInput = 302,
            IllegalSearchIdForParentDepth = 303,
            IdentifierTooLongError = 304,
            InvalidRemoveComponent = 305,
            FinishCabFailed = 306,
            InvalidExtensionType = 307,
            ValidationFailedDueToMultilanguageMergeModule = 309,
            ValidationFailedDueToInvalidPackage = 310,
            InvalidStringForCodepage = 311,
            InvalidEmbeddedUIFileName = 312,
            UniqueFileSearchIdRequired = 313,
            IllegalAttributeValueWhenNested = 314,
            AdminImageRequired = 315,
            SamePatchBaselineId = 316,
            SameFileIdDifferentSource = 317,
            HarvestSourceNotSpecified = 318,
            OutputTargetNotSpecified = 319,
            DuplicateCommandLineOptionInExtension = 320,
            HarvestTypeNotFound = 321,
            BothUpgradeCodesRequired = 322,
            IllegalBinderClassName = 323,
            SpecifiedBinderNotFound = 324,
            UnableToGetAuthenticodeCertOfFile = 327,
            UnableToGetAuthenticodeCertOfFileDownlevelOS = 328,
            ReadOnlyOutputFile = 329,
            CannotDefaultComponentId = 330,
            ParentElementAttributeRequired = 331,
            PreprocessorExtensionPragmaFailed = 333,
            InvalidPreprocessorPragma = 334,
            InvalidStubExe = 338,
            StubMissingWixburnSection = 339,
            StubWixburnSectionTooSmall = 340,
            MissingBundleInformation = 341,
            UnexpectedGroupChild = 342,
            OrderingReferenceLoopDetected = 343,
            IdentifierNotFound = 344,
            MergePlatformMismatch = 345,
            IllegalRelativeLongFilename = 346,
            IllegalAttributeValueWithLegalList = 347,
            IllegalAttributeValueWithIllegalList = 348,
            InvalidSummaryInfoCodePage = 349,
            ValidationFailedDueToLowMsiEngine = 350,
            DuplicateSourcesForOutput = 351,
            UnableToReadPackageInformation = 352,
            MultipleFilesMatchedWithOutputSpecification = 353,
            InvalidBundle = 354,
            BundleTooNew = 355,
            MediaTableCollision = 357,
            InvalidCabinetTemplate = 358,
            MaximumUncompressedMediaSizeTooLarge = 359,
            ReservedNamespaceViolation = 362,
            PerUserButAllUsersEquals1 = 363,
            UnsupportedAllUsersValue = 364,
            DisallowedMsiProperty = 365,
            MissingOrInvalidModuleInstallerVersion = 366,
            IllegalGeneratedGuidComponentUnversionedKeypath = 367,
            IllegalGeneratedGuidComponentVersionedNonkeypath = 368,
            DuplicateComponentGuids = 369,
            DuplicateProviderDependencyKey = 370,
            MissingDependencyVersion = 371,
            UnexpectedElementWithAttribute = 372,
            ExpectedAttributeWithElement = 373,
            DuplicatedUiLocalization = 374,
            MaximumCabinetSizeForLargeFileSplittingTooLarge = 375,
            SplitCabinetCopyRegistrationFailed = 376,
            SplitCabinetNameCollision = 377,
            InvalidPreprocessorFunctionAutoVersion = 379,
            InvalidFourPartVersion = 380,
            UnsupportedPlatformForElement = 381,
            MissingMedia = 382,
            IllegalYesNoAlwaysValue = 384,
            TooDeeplyIncluded = 385,
            TooManyColumnsInRealTable = 386,
            InlineDirectorySyntaxRequiresPath = 387,
            InsecureBundleFilename = 388,
            PayloadMustBeRelativeToCache = 389,
            MsiTransactionX86BeforeX64Package = 390,
            NoSourceFiles = 391,
            WixiplSourceFileIsExclusive = 392,
            UnableToConvertFieldToNumber = 393,
            CouldNotDetermineProductCodeFromTransformSummaryInfo = 394,
            IntermediatesMustBeCompiled = 395,
            IntermediatesMustBeResolved = 396,
            MissingBundleSearch = 397,
            CircularSearchReference = 398,
            UnknownSymbolType = 399,
            IllegalInnerText = 400,
            ExpectedAttributeWithValueWithOtherAttribute = 401,
            PackagePayloadUnsupported = 402,
            PackagePayloadUnsupported2 = 403,
            MultiplePackagePayloads = 404,
            MultiplePackagePayloads2 = 405,
            MultiplePackagePayloads3 = 406,
            MissingPackagePayload = 407,
            ExpectedAttributeWithoutOtherAttributes = 408,
            InvalidBundleCondition = 409,
            MsiTransactionX86BeforeX64Package2 = 410,
            MsiTransactionInvalidPackage = 411,
            MsiTransactionInvalidPackage2 = 412,
            ExpectedAttributeOrElementWithOtherAttribute = 413,
            ExpectedAttributeOrElementWithoutOtherAttribute = 414,
        }
    }
}
