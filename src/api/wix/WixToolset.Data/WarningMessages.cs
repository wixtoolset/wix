// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class WarningMessages
    {
        public static Message AccessDeniedForDeletion(SourceLineNumber sourceLineNumbers, string tempFilesBasePath)
        {
            return Message(sourceLineNumbers, Ids.AccessDeniedForDeletion, "Access denied; cannot delete '{0}'.", tempFilesBasePath);
        }

        public static Message AccessDeniedForSettingAttributes(SourceLineNumber sourceLineNumbers, string filePath)
        {
            return Message(sourceLineNumbers, Ids.AccessDeniedForSettingAttributes, "Access denied; cannot set attributes on '{0}'.", filePath);
        }

        public static Message ActionSequenceCollision(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2, int sequenceNumber)
        {
            return Message(sourceLineNumbers, Ids.ActionSequenceCollision, "The {0} table contains actions '{1}' and '{2}' which both have the same sequence number {3}. Please change the sequence number for one of these actions to avoid an ICE warning.", sequenceTableName, actionName1, actionName2, sequenceNumber);
        }

        public static Message ActionSequenceCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ActionSequenceCollision2, "The location of the action related to previous warning.");
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

        public static Message BackslashTerminateInlineDirectorySyntax(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.BackslashTerminateInlineDirectorySyntax, "Backslash terminate the {0}/@{1} attribute's inline directory value '{2}'. A backslash ensures a directory name will not be mistaken for a directory reference.", elementName, attributeName, value);
        }

        public static Message BadColumnDataIgnored(SourceLineNumber sourceLineNumbers, string value, string tableName, string columnName)
        {
            return Message(sourceLineNumbers, Ids.BadColumnDataIgnored, "The value '{0}' in table '{1}', column '{2}' is invalid according to the column's validation information. The decompiled output includes a best-effort representation of this value.", value, tableName, columnName);
        }

        public static Message CannotUpdateCabCache(SourceLineNumber sourceLineNumbers, string cabinetPath, string detail)
        {
            return Message(sourceLineNumbers, Ids.CannotUpdateCabCache, "Cannot update the timestamp of cached cabinet: '{0}'. If the timestamp is not updated, the build may rebuild more than is necessary. To fix the issue, ensure that the cabinet file is writable, error: {1}", cabinetPath, detail);
        }

        public static Message ColumnsIncompatibleWithInstallerVersion(SourceLineNumber sourceLineNumbers, string tableName, int productInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.ColumnsIncompatibleWithInstallerVersion, "Table '{0}' uses columns that require a version of Windows Installer greater than specified in your package ('{1}').", tableName, productInstallerVersion);
        }

        public static Message CopyFileFileIdUseless(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CopyFileFileIdUseless, "Since the CopyFile/@FileId attribute was specified but none of the following attributes (DestinationName, DestinationDirectory, DestinationProperty) were specified, this authoring will not do anything.");
        }

        public static Message DangerousTableInMergeModule(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DangerousTableInMergeModule, "Merge modules should not contain the '{0}' table because all merge conflicts cannot avoided. However, this warning can be suppressed if all of the consumers of the Merge Module agree to not duplicate identifiers in the '{0}' table.", tableName);
        }

        public static Message DecompiledStandardActionRelativelyScheduledInModule(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.DecompiledStandardActionRelativelyScheduledInModule, "The {0} table contains a standard action '{1}' that does not have a sequence number specified. A value in the Sequence column is required for standard actions in a merge module. Remove the action from the decompiled authoring to have WiX automatically sequence it.", sequenceTableName, actionName);
        }

        public static Message DecompilingAsCustomTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DecompilingAsCustomTable, "The {0} table is being decompiled as a custom table.", tableName);
        }

        public static Message DefaultLanguageUsedForUnversionedFile(SourceLineNumber sourceLineNumbers, string language, string fileId)
        {
            return Message(sourceLineNumbers, Ids.DefaultLanguageUsedForUnversionedFile, "The DefaultLanguage '{0}' was used for file '{1}' which has no language or version. For unversioned files, specifying a value for DefaultLanguage is not neccessary and it will not be used when determining file versions. Remove the DefaultLanguage attribute to eliminate this warning.", language, fileId);
        }

        public static Message DefaultLanguageUsedForVersionedFile(SourceLineNumber sourceLineNumbers, string language, string fileId)
        {
            return Message(sourceLineNumbers, Ids.DefaultLanguageUsedForVersionedFile, "The DefaultLanguage '{0}' was used for file '{1}' which has no language. Specifying a language that is different from the actual file may result in unexpected versioning behavior during a repair or while patching. Either specify a value for DefaultLanguage or put the language in the version information resource to eliminate this warning.", language, fileId);
        }

        public static Message DefaultVersionUsedForUnversionedFile(SourceLineNumber sourceLineNumbers, string version, string fileId)
        {
            return Message(sourceLineNumbers, Ids.DefaultVersionUsedForUnversionedFile, "The DefaultVersion '{0}' was used for file '{1}' which has no version. No entry for this file will be placed in the MsiFileHash table. For unversioned files, specifying a version that is different from the actual file may result in unexpected versioning behavior during a repair or while patching. Version the resource to eliminate this warning.", version, fileId);
        }

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

        public static Message DeprecatedCommandLineSwitch(string oldSwitch)
        {
            return Message(null, Ids.DeprecatedCommandLineSwitch, "The command line switch '{0}' is deprecated.", oldSwitch);
        }

        public static Message DeprecatedCommandLineSwitch(string oldSwitch, string newSwitch)
        {
            return Message(null, Ids.DeprecatedCommandLineSwitch, "The command line switch '{0}' is deprecated. Please use '{1}' instead.", oldSwitch, newSwitch);
        }

        public static Message DeprecatedComponentGroupId(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedComponentGroupId, "The {0}/@Id attribute contains invalid characters for an identifier. Being able to use invalid identifier characters for a {0} identifier has been deprecated.", elementName);
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

        public static Message DeprecatedIgnoreModularizationElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedIgnoreModularizationElement, "The IgnoreModularization element has been deprecated. Use the Binary/@SuppressModularization, CustomAction/@SuppressModularization, or Property/@SuppressModularization attribute instead.");
        }

        public static Message DeprecatedLocalizationVariablePrefix(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedLocalizationVariablePrefix, "The localization variable $(loc.{0}) uses a deprecated prefix '$'. Please use the '!' prefix instead. Since the prefix '$' is also used by the preprocessor, it has been deprecated to avoid namespace collisions.", variableId);
        }

        public static Message DeprecatedLongNameAttribute(SourceLineNumber sourceLineNumbers, string elementName, string longNameAttributeName, string nameAttributeName, string shortNameAttributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedLongNameAttribute, "The {0}/@{1} attribute has been deprecated. Since WiX now has the ability to generate short file/directory names, the desired name should be specified in the {2} attribute instead. If the name specified in the {2} attribute is a short name, then WiX will not generate a short name. If the name specified in the {2} attribute is a long name and you want to manually specify the short name, please set the short name value in the {3} attribute.", elementName, longNameAttributeName, nameAttributeName, shortNameAttributeName);
        }

        public static Message DeprecatedPatchSequenceTargetAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedPatchSequenceTargetAttribute, "The {0}/@{1} attribute has been deprecated in favor of the more strongly-typed ProductCode or TargetImage attributes. Please use the ProductCode attribute for indicating the ProductCode of a patch family directly, or the TargetImage attribute to specify the TargetImage which in turn will retrieve the ProductCode of the patch family.", elementName, attributeName);
        }

        public static Message DeprecatedPreProcVariable(SourceLineNumber sourceLineNumbers, string oldName, string newName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedPreProcVariable, "The built-in preprocessor variable '{0}' is deprecated. Please correct your authoring to use the new '{1}' preprocessor variable instead.", oldName, newName);
        }

        public static Message DeprecatedQuestionMarksGuid(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedQuestionMarksGuid, "The {0}/@{1} attribute's value '????????-????-????-????-????????????' has been deprecated. Use '*' instead.", elementName, attributeName);
        }

        public static Message DeprecatedRegistryElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedRegistryElement, "The Registry element has been deprecated. Please use one of the new elements which replaces its functionality: RegistryKey for creating registry keys, RegistryValue for writing registry values, RemoveRegistryKey for removing registry keys, and RemoveRegistryValue for removing registry values.");
        }

        public static Message DeprecatedRegistryKeyActionAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedRegistryKeyActionAttribute, "The RegistryKey/@Action attribute has been deprecated. In most cases, you can simply omit @Action. If you need to force Windows Installer to create an empty key or recursively delete the key, use the ForceCreateOnInstall or ForceDeleteOnUninstall attributes instead.");
        }

        public static Message DeprecatedTable(string tableName)
        {
            return Message(null, Ids.DeprecatedTable, "The {0} table is not supported by the WiX toolset because it has been deprecated by the Windows Installer team. Any information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message DeprecatedUpgradeProperty(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedUpgradeProperty, "Specifying a Property element as a child of an Upgrade element has been deprecated. Please specify this Property element as a child of a different element such as Product or Fragment.");
        }

        public static Message DirectoryInUse(SourceLineNumber sourceLineNumbers, string filePath)
        {
            return Message(sourceLineNumbers, Ids.DirectoryInUse, "The directory '{0}' is in use and cannot be deleted.", filePath);
        }

        public static Message DirectoryRedundantNames(SourceLineNumber sourceLineNumbers, string elementName, string shortNameAttributeName, string longNameAttributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRedundantNames, "The {0} element's {1} and {2} values are both '{3}'. This is redundant; the {2} attribute should be removed.", elementName, shortNameAttributeName, longNameAttributeName, attributeValue);
        }

        public static Message DirectoryRedundantNames(SourceLineNumber sourceLineNumbers, string elementName, string sourceNameAttributeName, string longSourceAttributeName)
        {
            return Message(sourceLineNumbers, Ids.DirectoryRedundantNames, "The {0} element's source and destination names are identical. This is redundant; the {1} and {2} attributes should be removed if present.", elementName, sourceNameAttributeName, longSourceAttributeName);
        }

        public static Message DiscardedRollbackBoundary(SourceLineNumber sourceLineNumbers, string rollbackBoundaryId)
        {
            return Message(sourceLineNumbers, Ids.DiscardedRollbackBoundary, "The RollbackBoundary '{0}' was discarded because it was not followed by a package. Without a package the rollback boundary doesn't do anything. Verify that the RollbackBoundary element is not followed by another RollbackBoundary and that the element is not at the end of the chain.", rollbackBoundaryId);
        }

        public static Message DiscardedRollbackBoundary2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DiscardedRollbackBoundary2, "Location of rollback boundary related to previous warning.");
        }

        public static Message DiscouragedAllUsersValue(SourceLineNumber sourceLineNumbers, string path, string machineOrUser)
        {
            return Message(sourceLineNumbers, Ids.DiscouragedAllUsersValue, "Bundles require a package to be either per-machine or per-user. The MSI '{0}' ALLUSERS Property is set to '2' which may change from per-user to per-machine at install time. The Bundle will assume the package is per-{1} and will not work correctly if that changes. If possible, remove the Property with Id='ALLUSERS' and use Package/@InstallScope attribute instead.", path, machineOrUser);
        }

        public static Message DetectConditionRecommended(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.DetectConditionRecommended, "The {0}/@DetectCondition attribute is recommended so the package is only installed when absent.", elementName);
        }

        public static Message ExePackageDetectInformationRecommended(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExePackageDetectInformationRecommended, "The ExePackage/@DetectCondition attribute or child element ArpEntry is recommended so the package is only installed when absent.");
        }

        public static Message DownloadUrlNotSupportedForAttachedContainers(SourceLineNumber sourceLineNumbers, string containerId)
        {
            return Message(sourceLineNumbers, Ids.DownloadUrlNotSupportedForAttachedContainers, "The Container '{0}' is attached but included a @DownloadUrl attribute. Attached Containers cannot be downloaded so the download URL is being ignored.", containerId);
        }

        public static Message DownloadUrlNotSupportedForBAPayloads(SourceLineNumber sourceLineNumbers, string payloadId)
        {
            return Message(sourceLineNumbers, Ids.DownloadUrlNotSupportedForBAPayloads, "The BootstrapperApplication Payload '{0}' included a @DownloadUrl attribute. BootstrapperApplication Payloads cannot be downloaded so the download URL is being ignored.", payloadId);
        }

        public static Message DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(SourceLineNumber sourceLineNumbers, string componentId, string guid, string type, string keyPath)
        {
            return Message(sourceLineNumbers, Ids.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions, "Component/@Id='{0}' with {2} '{3}' has a @Guid value '{1}' that duplicates another component in this package. This is not officially supported by Windows Installer and cannot be used when creating patches. It otherwise works as long as all components with the same GUID have mutually-exclusive conditions. It is recommended to give each component its own unique GUID.", componentId, guid, type, keyPath);
        }

        public static Message DuplicatePrimaryKey(SourceLineNumber sourceLineNumbers, string primaryKey, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatePrimaryKey, "The primary key '{0}' is duplicated in table '{1}' and will be ignored. Please remove one of the entries or rename a part of the primary key to avoid the collision.", primaryKey, tableName);
        }

        public static Message EmptyAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.EmptyAttributeValue, "The {0}/@{1} attribute's value cannot be an empty string. If you want the value to be null or empty, simply remove the entire attribute.", elementName, attributeName);
        }

        public static Message EmptyCabinet(SourceLineNumber sourceLineNumbers, string cabinetName, bool isPatch)
        {
            if (isPatch)
            {
                return Message(sourceLineNumbers, Ids.EmptyCabinet, "The cabinet '{0}' does not contain any files. If this patch contains no files, this warning can likely be safely ignored. Otherwise, try passing -p to torch.exe when first building the transforms, or add a ComponentRef to your PatchFamily authoring to pull changed files into the cabinet.", cabinetName, isPatch);
            }

            return Message(sourceLineNumbers, Ids.EmptyCabinet, "The cabinet '{0}' does not contain any files. If this installation contains no files, this warning can likely be safely ignored. Otherwise, please add files to the cabinet or remove it.", cabinetName);
        }

        public static Message ExpectedForeignRow(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, string columnName, string columnValue, string foreignTableName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedForeignRow, "The {0} table contains a row with primary key(s) '{1}' whose {2} column contains a value, '{3}', which specifies a foreign key relationship with the {4} table. However, since the expected foreign row specified by this value does not exist, this will result in some information being left out of the decompiled output.", tableName, primaryKey, columnName, columnValue, foreignTableName);
        }

        public static Message ExpectedForeignRow(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, string columnName1, string columnValue1, string columnName2, string columnValue2, string foreignTableName)
        {
            return Message(sourceLineNumbers, Ids.ExpectedForeignRow, "The {0} table contains a row with primary key(s) '{1}' whose {2} and {4} columns contain the values, '{3}' and '{5}', which specify a foreign key relationship with the {6} table. However, since the expected foreign row specified by this value does not exist, this will result in some information being left out of the decompiled output.", tableName, primaryKey, columnName1, columnValue1, columnName2, columnValue2, foreignTableName);
        }

        public static Message ExternalCabsAreNotSigned(string databaseFile)
        {
            return Message(null, Ids.ExternalCabsAreNotSigned, "The installer database '{0}' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build.", databaseFile);
        }

        public static Message FailedToDeleteTempDir(string directory)
        {
            return Message(null, Ids.FailedToDeleteTempDir, "Failed to delete temporary directory: {0}", directory);
        }

        public static Message FileSearchFileNameIssue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2)
        {
            return Message(sourceLineNumbers, Ids.FileSearchFileNameIssue, "The {0} element's {1} and {2} attributes were found. Due to a bug with the Windows Installer, only the Name or LongName attribute should be used. Use the Name attribute for 8.3 compliant file names and the LongName attribute for longer ones. When using only the LongName attribute, ICE03 should be ignored for the Signature table's FileName column.", elementName, attributeName1, attributeName2);
        }

        public static Message GeneratedShortFileNameConflict(SourceLineNumber sourceLineNumbers, string shortFileName)
        {
            return Message(sourceLineNumbers, Ids.GeneratedShortFileNameConflict, "The short file name '{0}' was generated for multiple files that may be installed to the same directory. This could be due to conflicting long file names specified by the File/@Name attribute. If that is the case, please resolve the conflict in those attributes. Otherwise, please manually set the File/@ShortName attribute on the conflicting row to fix the collision. If one of the colliding files was added via a patch, that short file name should be specified manually to avoid disturbing the original short file name.", shortFileName);
        }

        public static Message GeneratedShortFileNameConflict2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.GeneratedShortFileNameConflict2, "The location of a conflicting generated short file name related to the previous warning.");
        }

        public static Message IdentifierCannotBeModularized(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string identifier, int length, int maximumLength)
        {
            return Message(sourceLineNumbers, Ids.IdentifierCannotBeModularized, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. It will be too long if modularized. The identifier shouldn't be longer than {4} characters long to allow for modularization (appending a guid for merge modules).", elementName, attributeName, identifier, length, maximumLength);
        }

        public static Message IdentifierTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IdentifierTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for an identifier. Standard identifiers are 72 characters long or less.", elementName, attributeName, value);
        }

        public static Message IllegalActionInSequence(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.IllegalActionInSequence, "The {0} table contains an action '{1}' which is not allowed in this table. If this is a standard action then it is not valid for this table, if it is a custom action or dialog then this table does not accept actions of that type. This action will be left out of the decompiled output.", sequenceTableName, actionName);
        }

        public static Message IllegalColumnValue(SourceLineNumber sourceLineNumbers, string tableName, string columnName, object value)
        {
            return Message(sourceLineNumbers, Ids.IllegalColumnValue, "The {0}.{1} column's value, '{2}', is not a recognized legal value. This information will be left out of the decompiled output.", tableName, columnName, value);
        }

        public static Message IllegalPatchCreationTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.IllegalPatchCreationTable, "The {0} table is not legal in a patch creation file. The information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message IllegalRegistryKeyPath(SourceLineNumber sourceLineNumbers, string componentName, string registryId)
        {
            return Message(sourceLineNumbers, Ids.IllegalRegistryKeyPath, "Component '{0}' specifies an illegal registry keypath of '{1}'. Since this entry actually represents a registry key, not a registry value, it cannot be the keypath.", componentName, registryId);
        }

        public static Message ImplicitComponentPrimaryFeature(string componentId)
        {
            return Message(null, Ids.ImplicitComponentPrimaryFeature, "The component '{0}' does not have an explicit primary feature parent specified. If the source files are linked in a different order, the primary parent feature may change. To prevent accidental changes, the primary feature parent should be set to 'yes' in one of the ComponentRef/@Primary, ComponentGroupRef/@Primary, or FeatureGroupRef/@Primary locations for this component.", componentId);
        }

        public static Message ImplicitlyPerUser(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.ImplicitlyPerUser, "The MSI '{0}' does not explicitly indicate that it is a per-user package even though the ALLUSERS Property is blank. This suggests a per-user package so the Bundle will assume the package is per-user. If possible, use the Package/@InstallScope attribute to be explicit instead.", path);
        }

        public static Message ImplicitMergeModulePrimaryFeature(string componentId)
        {
            return Message(null, Ids.ImplicitMergeModulePrimaryFeature, "The merge module '{0}' does not have an explicit primary feature parent specified. If the source files are linked in a different order, the primary parent feature may change. To prevent accidental changes, the primary feature parent should be set to 'yes' in one of the MergeRef/@Primary or FeatureGroupRef/@Primary locations for this component.", componentId);
        }

        public static Message InsufficientPermissionHarvestTypeLib()
        {
            return Message(null, Ids.InsufficientPermissionHarvestTypeLib, "Not enough permissions to harvest type library. On Windows Vista, you must either run Heat elevated, or install Windows Vista SP1 (or higher).");
        }

        public static Message InvalidAttributeCombination(SourceLineNumber sourceLineNumbers, string attrib1, string attrib2, string name, string value)
        {
            return Message(sourceLineNumbers, Ids.InvalidAttributeCombination, "It is invalid to combine attributes {0} and {1}. The decompiled output will set attribute {2} to {3}.", attrib1, attrib2, name, value);
        }

        public static Message InvalidHigherInstallerVersionInModule(SourceLineNumber sourceLineNumbers, string moduleId, int moduleInstallerVersion, int productInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.InvalidHigherInstallerVersionInModule, "Merge module '{0}' has an installer version of {1} which is greater than the product's installer version of {2}. Merging a module with a higher installer version than the product it is being merged into can result in invalid values in the resulting msi. You must set the Package/@InstallerVersion attribute to {1} or greater to merge this merge module into your product.", moduleId, moduleInstallerVersion, productInstallerVersion);
        }

        public static Message InvalidFourPartVersion(SourceLineNumber sourceLineNumbers, string elementName, string version)
        {
            return Message(sourceLineNumbers, Ids.InvalidFourPartVersion, "Invalid {0}/@Version '{1}'. {0} version has a max value of \"65535.65535.65535.65535\" and must be all numeric.", elementName, version);
        }

        public static Message InvalidRemoveFile(SourceLineNumber sourceLineNumbers, string file, string component)
        {
            return Message(sourceLineNumbers, Ids.InvalidRemoveFile, "File '{0}' was removed from component '{1}'. Removing a file from a component will not result in the file being removed by a patch. You should author a RemoveFile element in your component to remove the file from the installation if you want the file to be removed.", file, component);
        }

        public static Message MajorUpgradePatchNotRecommended()
        {
            return Message(null, Ids.MajorUpgradePatchNotRecommended, "Changing the ProductCode in a patch is not recommended because the patch cannot be uninstalled nor can it be sequenced along with other patches for the target product. See https://learn.microsoft.com/en-us/windows/win32/msi/applying-major-upgrades-by-patching-the-local-installation-of-the-product for more information.");
        }

        public static Message MediaExternalCabinetFilenameIllegal(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.MediaExternalCabinetFilenameIllegal, "The {0}/@{1} attribute's value, '{2}', is not a valid external cabinet name. Legal cabinet names should follow 8.3 format: they should contain no more than 8 characters followed by an optional extension of no more than 3 characters. Any character except for the following may be used: \\ ? | > < : / * \" + , ; = [ ] (space). The Windows Installer team has recommended following the 8.3 format for external cabinet files and any other naming scheme is officially unsupported (which means it is not guaranteed to work on all platforms).", elementName, attributeName, value);
        }

        public static Message MergeRescheduledAction(SourceLineNumber sourceLineNumbers, string tableName, string actionName, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeRescheduledAction, "The {0} table contains an action '{1}' which cannot be merged from the merge module '{2}'. This action is likely colliding with an action in the database that is being created. The colliding action may have been authored in the database or merged in from another merge module. If this is a standard action, it is likely colliding due to a difference in the condition for the action in the database and merge module. If this is a custom action, it should only be declared in the database or one merge module.", tableName, actionName, mergeModuleFile);
        }

        public static Message MergeTableFailed(SourceLineNumber sourceLineNumbers, string tableName, string primaryKeys, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeTableFailed, "The {0} table contains a row with primary key(s) '{1}' which cannot be merged from the merge module '{2}'. This is likely due to collision of rows with the same primary key(s) (but other different values in other columns) between the database and the merge module.", tableName, primaryKeys, mergeModuleFile);
        }

        public static Message MissingUpgradeCode(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MissingUpgradeCode, "The Product/@UpgradeCode attribute was not found; it is strongly recommended to ensure that this product can be upgraded.");
        }

        public static Message MsiTransactionLimitations(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MsiTransactionLimitations, "MSI transactions have limitations that make it hard to use them successfully in a bundle. Test the bundle thoroughly, especially in upgrade scenarios and the scenario that required them in the first place.");
        }

        public static Message NestedInstall(SourceLineNumber sourceLineNumbers, string tableName, string columnName, object value)
        {
            return Message(sourceLineNumbers, Ids.NestedInstall, "The {0}.{1} column's value, '{2}', indicates a nested install. Nested installations are not supported by the WiX team. This action will be left out of the decompiled output.", tableName, columnName, value);
        }

        public static Message NewComponentAddedToExistingFeature(SourceLineNumber sourceLineNumbers, string component, string feature, string transformPath)
        {
            return Message(sourceLineNumbers, Ids.NewComponentAddedToExistingFeature, "Component '{0}' was added to feature '{1}' in the transform '{2}'. If you cannot guarantee that this feature will always be installed, you should consider adding new components to new top-level features to prevent prompts for source when installing this patch.", component, feature, transformPath);
        }

        public static Message NoPerMachineDependencies(SourceLineNumber sourceLineNumbers, string packageId)
        {
            return Message(sourceLineNumbers, Ids.NoPerMachineDependencies, "Bundle dependencies will not be registered on per-machine package '{0}' for a per-user bundle. Either make sure that all packages are installed per-machine, or author any per-machine dependencies as permanent packages.", packageId);
        }

        public static Message NotABinaryWixlib(string wixlib)
        {
            return Message(null, Ids.NotABinaryWixlib, "'{0}' is not a binary Wixlib and has no embedded files.", wixlib);
        }

        public static Message NullMsiAssemblyNameValue(SourceLineNumber sourceLineNumbers, string componentName, string name)
        {
            return Message(sourceLineNumbers, Ids.NullMsiAssemblyNameValue, "The assembly in component '{0}' has a null or empty {1} assembly name value.", componentName, name);
        }

        public static Message OrphanedProgId(SourceLineNumber sourceLineNumbers, string progId)
        {
            return Message(sourceLineNumbers, Ids.OrphanedProgId, "ProgId '{0}' is orphaned. It has no associated component, so it will never install. Every ProgId should have either a parent Class element or child Extension element (at any distance).", progId);
        }

        public static Message PackageCodeSet(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.PackageCodeSet, "The Package/@Id attribute has been set. Setting this attribute will allow nonidentical .msi files to have the same package code. This may be a problem because the package code is the primary identifier used by the installer to search for and validate the correct package for a given installation. If a package is changed without changing the package code, the installer may not use the newer package if both are still accessible to the installer. Please remove the Id attribute in order to automatically generate a new package code for each new .msi file.");
        }

        public static Message PatchTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.PatchTable, "The {0} table is added to the install package by a transform from a patch package (.msp) and not authored directly into an install package (.msi). The information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message PathCanonicalized(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string originalValue, string canonicalValue)
        {
            return Message(sourceLineNumbers, Ids.PathCanonicalized, "The {0}/@{1} attribute's value, '{2}', has been canonicalized to '{3}'.", elementName, attributeName, originalValue, canonicalValue);
        }

        public static Message PerUserButForcingPerMachine(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.PerUserButForcingPerMachine, "The MSI '{0}' is a per-user package being forced to per-machine. Verify that the MsiPackage/@ForcePerMachine attribute is expected and that the per-user package works correctly when forced to install per-machine.", path);
        }

        public static Message PlaceholderValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.PlaceholderValue, "The {0}/@{1} attribute's value, '{2}', is a placeholder value used in example files. Please replace this placeholder with the appropriate value.", elementName, attributeName, value);
        }

        public static Message PossiblyIncorrectTypelibVersion(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.PossiblyIncorrectTypelibVersion, "The Typelib table entry with Id '{0}' could have an incorrect version of '256.0'. InstallShield has a bug relating to the Typelib Version column: it will incorrectly set the value '65536' in to represent version '1.0'. However, this number actually corresponds to version '256.0'. This bug will not affect the typelib version that is registered during installation, however, it will prevent the Windows Installer from correctly identifying whether a typelib is already installed and lead to unnecessary reinstallations of the typelib.", id);
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

        public static Message RelatedAttributeConditionallyIgnored(SourceLineNumber sourceLineNumbers, string recessiveAttribute, string dominantAttribute, string dominantValue)
        {
            return Message(sourceLineNumbers, Ids.RelatedAttributeConditionallyIgnored, "Ignoring attribute {0} because attribute {1} is set to {2}.", recessiveAttribute, dominantAttribute, dominantValue);
        }

        public static Message RemotePayloadsMustNotAlsoBeCompressed(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.RemotePayloadsMustNotAlsoBeCompressed, "The {0}/@Compressed attribute must have value 'no' when a RemotePayload child element is present. RemotePayload indicates that a package will always be downloaded and cannot be compressed into a bundle. To eliminate this warning, explicitly set the {0}/@Compressed attribute to 'no'.", elementName);
        }

        public static Message RemoveFileNameRequired(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RemoveFileNameRequired, "The RemoveFile/@Name attribute will soon become required. In order to match the old functionality of not specifying this attribute, please use the new RemoveFolder element instead.");
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

        public static Message RetainRangeMismatch(SourceLineNumber sourceLineNumbers, string fileId)
        {
            return Message(sourceLineNumbers, Ids.RetainRangeMismatch, "Mismatch in RetainRangeCounts for the file '{0}' - ignoring the retain ranges.", fileId);
        }

        public static Message ServiceConfigFamilyNotSupported(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.ServiceConfigFamilyNotSupported, "{0} functionality is documented in the Windows Installer SDK to \"not [work] as expected.\" Consider replacing {0} with the WixUtilExtension ServiceConfig element.", elementName);
        }

        public static Message SkippingMergeModuleTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.SkippingMergeModuleTable, "The {0} table can only be represented in WiX for merge modules. The information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message SkippingPatchCreationTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.SkippingPatchCreationTable, "The {0} table can only be represented in WiX for patch creation files. The information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message StandardDirectoryConflictInMergeModule(SourceLineNumber sourceLineNumbers, string directory, string standardDirectory)
        {
            return Message(sourceLineNumbers, Ids.StandardDirectoryConflictInMergeModule, "The Directory '{0}' starts with the same Id as the standard folder in Windows Installer '{1}'. A directory Id that begins with the same Id as a standard folder that is in an MSM may encounter a conflict when merging the MSM into an MSI. This may result in the contents of this merge module being installed to an unexpected location. To eliminate this warning, change your directory Id to not start with the same Id as any standard folders.", directory, standardDirectory);
        }

        public static Message SuppressAction(SourceLineNumber sourceLineNumbers, string action, string sequenceName)
        {
            return Message(sourceLineNumbers, Ids.SuppressAction, "The action '{0}' in the {1} table is being suppressed.", action, sequenceName);
        }

        public static Message SuppressAction2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.SuppressAction2, "The location of the suppressed action related to previous warning.");
        }

        public static Message SuppressMergedAction(string action, string sequenceName)
        {
            return Message(null, Ids.SuppressMergedAction, "The merged action '{0}' in the {1} table is being suppressed.", action, sequenceName);
        }

        public static Message TableIncompatibleWithInstallerVersion(SourceLineNumber sourceLineNumbers, string tableName, int productInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.TableIncompatibleWithInstallerVersion, "Using table '{0}' requires a version of Windows Installer greater than specified in your package ('{1}').", tableName, productInstallerVersion);
        }

        public static Message TargetDirCorrectedDefaultDir()
        {
            return Message(null, Ids.TargetDirCorrectedDefaultDir, "The Directory with Id 'TARGETDIR' must have the value 'SourceDir' in its 'DefaultDir' column. This has been automatically corrected for you in the decompiled output.");
        }

        public static Message TooManyProgIds(SourceLineNumber sourceLineNumbers, string clsId, string progId, string otherClsId)
        {
            return Message(sourceLineNumbers, Ids.TooManyProgIds, "Class '{0}' tried to use ProgId '{1}' which has already been associated with class '{2}'. This information will be left out of the decompiled output.", clsId, progId, otherClsId);
        }

        public static Message SymbolNotTranslatedToOutput(IntermediateSymbol symbol)
        {
            var symbolString = $"SymbolName: '{symbol.Definition.Name}', Id: '{symbol.Id?.Id}'";
            return Message(symbol.SourceLineNumbers, Ids.SymbolNotTranslatedToOutput, "The binder doesn't know how to place the following symbol into the output: {0}", symbolString);
        }

        public static Message UnableToFindFileFromCabOrImage(SourceLineNumber sourceLineNumbers, string existingFileSpec, string srcFileSpec)
        {
            return Message(sourceLineNumbers, Ids.UnableToFindFileFromCabOrImage, "Unable to find existing file {0} to place in src location {1}. Will likely cause a linker break.", existingFileSpec, srcFileSpec);
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

        public static Message UnexpectedEntrySection(SourceLineNumber sourceLineNumbers, string sectionType, string expectedType)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedEntrySection, "Found entry point <{0}> that does not match expected <{1}> output type. Verify that your source code is correct and matches the expected output type.", sectionType, expectedType);
        }

        public static Message UnexpectedTableInProduct(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInProduct, "An unexpected row in the '{0}' table was found in this product. Products should not contain the '{0}' table.", tableName);
        }

        public static Message UnknownAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.UnknownAction, "The {0} table contains an action '{1}' which is not a known custom action, dialog, or standard action. This action will be left out of the decompiled output.", sequenceTableName, actionName);
        }

        public static Message UnknownPermission(SourceLineNumber sourceLineNumbers, string tableName, string primaryKey, int bitPosition)
        {
            return Message(sourceLineNumbers, Ids.UnknownPermission, "The {0} table contains a row with primary key '{1}' which has an unknown permission at bit {2}.", tableName, primaryKey, bitPosition);
        }

        public static Message UnrepresentableColumnValue(SourceLineNumber sourceLineNumbers, string tableName, string columnName, object value)
        {
            return Message(sourceLineNumbers, Ids.UnrepresentableColumnValue, "The {0}.{1} column's value, '{2}', cannot currently be represented in the WiX schema.", tableName, columnName, value);
        }

        public static Message UnsupportedCommandLineArgument(string arg)
        {
            return Message(null, Ids.UnsupportedCommandLineArgument, "'{0}' is not a valid command line argument.", arg);
        }

        public static Message UnsupportedCommandLineArgumentValue(string arg, string value, string fallback)
        {
            return Message(null, Ids.UnsupportedCommandLineArgument, "The value '{0}' is not a valid value for command line argument '{1}'. Using the value '{2}' instead.", value, arg, fallback);
        }

        public static Message UpdateOfNonKeyPathFile(string nonKeyPathFileId, string componentId, string keyPathFileId)
        {
            return Message(null, Ids.UpdateOfNonKeyPathFile, "File '{0}' in Component '{1}' was changed, but the KeyPath file '{2}' was not. This file will not be patched on the target system if the REINSTALLMODE does not contain 'A'. The KeyPath file should also be changed and included in your patch.", nonKeyPathFileId, componentId, keyPathFileId);
        }

        public static Message UxPayloadsOnlySupportEmbedding(SourceLineNumber sourceLineNumbers, string sourceFile)
        {
            return Message(sourceLineNumbers, Ids.UxPayloadsOnlySupportEmbedding, "A bootstrapper application or bundle extension payload ('{0}') was marked for something other than embedded packaging, possibly because it included a @DownloadUrl attribute. Bootstrapper application and bundle extension payloads must be embedded in the bundle, so the requested packaging is being ignored and the file is being embedded anyway.", sourceFile);
        }

        public static Message ValidationFailedDueToSystemPolicy()
        {
            return Message(null, Ids.ValidationFailedDueToSystemPolicy, "Validation could not run due to system policy. To eliminate this warning, run the process as admin or suppress ICE validation.");
        }

        public static Message ValidationWarning(SourceLineNumber sourceLineNumbers, string ice, string message)
        {
            return Message(sourceLineNumbers, Ids.ValidationWarning, "{0}: {1}", ice, message);
        }

        public static Message VariableDeclarationCollision(SourceLineNumber sourceLineNumbers, string variableName, string variableValue, string variableCollidingValue)
        {
            return Message(sourceLineNumbers, Ids.VariableDeclarationCollision, "The variable '{0}' with value '{1}' was previously declared with value '{2}'.", variableName, variableValue, variableCollidingValue);
        }

        public static Message InvalidMsiProductVersion(SourceLineNumber sourceLineNumbers, string version, string package)
        {
            return Message(sourceLineNumbers, Ids.InvalidMsiProductVersion, "Invalid product version '{0}' in MSI package '{1}'. Product version should have a major version less than 256, a minor version less than 256, and a build version less than 65536. The bundle may incorrectly detect upgrades of this package.", version, package);
        }

        public static Message InvalidMsiProductVersion(SourceLineNumber sourceLineNumbers, string version)
        {
            return Message(sourceLineNumbers, Ids.InvalidMsiProductVersion,
                "Invalid MSI package version: '{0}'. " +
                "The Windows Installer SDK says that MSI product versions must have a major version less than 256, a minor version less than 256, and a build version less than 65536. " +
                "The revision value is ignored but version labels and metadata are not allowed. " +
                "Violating the MSI rules sometimes works as expected but the behavior is unpredictable and undefined. "+
                "Future versions of WiX might treat invalid package versions as an error.",
                version);
        }

        public static Message CollidingModularizationTypes(string tableName, string columnName, string foreignTableName, int foreignColumnNumber, string modularizationType, string foreignModularizationType)
        {
            return Message(null, Ids.CollidingModularizationTypes, "The definition for the '{0}' table's '{1}' column is a foreign key relationship to the '{2}' table's column number {3}. The modularization types of the two column definitions differ: table '{0}' uses type {4} and table '{2}' uses type {5}. Change one of the modularization types so that they match.", tableName, columnName, foreignTableName, foreignColumnNumber, modularizationType, foreignModularizationType);
        }

        public static Message InvalidEnvironmentVariable(string environmentVariable, string value, string defaultValue)
        {
            return Message(null, Ids.InvalidEnvironmentVariable, "The {0} environment variable is set to an invalid value of '{1}'. The default value '{2}' will be used instead.", environmentVariable, value, defaultValue);
        }

        public static Message WindowsInstallerFileTooLarge(SourceLineNumber sourceLineNumbers, string path, string fileDescription)
        {
            if (String.IsNullOrEmpty(fileDescription))
            {
                fileDescription = "MSI or cabinet";
            }

            return Message(sourceLineNumbers, Ids.WindowsInstallerFileTooLarge, "The Windows Installer does not support {0} files larger than 2GB in size. Reduce the size or number of files embedded in '{1}' or the installation will likely fail with an unexpected error.", fileDescription, path);
        }

        public static Message InvalidWixVersion(SourceLineNumber sourceLineNumbers, string version, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.InvalidWixVersion, "Invalid WixVersion '{0}' in {1}/@'{2}'. Comparisons may yield unexpected results.", version, elementName, attributeName);
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
            IdentifierCannotBeModularized = 1000,
            EmptyAttributeValue = 1001,
            UnableToFindFileFromCabOrImage = 1002,
            CopyFileFileIdUseless = 1003,
            NestedInstall = 1004,
            OrphanedProgId = 1005,
            PropertyUseless = 1006,
            RemoveFileNameRequired = 1007,
            SuppressAction = 1008,
            SuppressMergedAction = 1009,
            TargetDirCorrectedDefaultDir = 1010,
            AccessDeniedForDeletion = 1011,
            DirectoryInUse = 1012,
            AccessDeniedForSettingAttributes = 1013,
            UnknownAction = 1024,
            IdentifierTooLong = 1026,
            UnknownPermission = 1030,
            DirectoryRedundantNames = 1031,
            UnableToResetAcls = 1032,
            MediaExternalCabinetFilenameIllegal = 1033,
            DeprecatedPreProcVariable = 1034,
            FileSearchFileNameIssue = 1043,
            AmbiguousFileOrDirectoryName = 1044,
            PossiblyIncorrectTypelibVersion = 1048,
            ImplicitComponentPrimaryFeature = 1049,
            ActionSequenceCollision = 1050,
            ActionSequenceCollision2 = 1051,
            SuppressAction2 = 1052,
            UnexpectedTableInProduct = 1053,
            DeprecatedAttribute = 1054,
            MergeRescheduledAction = 1055,
            MergeTableFailed = 1056,
            DecompiledStandardActionRelativelyScheduledInModule = 1057,
            IllegalActionInSequence = 1058,
            ExpectedForeignRow = 1059,
            DecompilingAsCustomTable = 1060,
            IllegalPatchCreationTable = 1061,
            SkippingMergeModuleTable = 1062,
            SkippingPatchCreationTable = 1063,
            UnrepresentableColumnValue = 1064,
            DeprecatedTable = 1065,
            PatchTable = 1066,
            IllegalColumnValue = 1067,
            DeprecatedLongNameAttribute = 1069,
            GeneratedShortFileNameConflict = 1070,
            GeneratedShortFileNameConflict2 = 1071,
            DangerousTableInMergeModule = 1072,
            DeprecatedLocalizationVariablePrefix = 1073,
            PlaceholderValue = 1074,
            MissingUpgradeCode = 1075,
            ValidationWarning = 1076,
            PropertyValueContainsPropertyReference = 1077,
            DeprecatedUpgradeProperty = 1078,
            EmptyCabinet = 1079,
            DeprecatedRegistryElement = 1080,
            IllegalRegistryKeyPath = 1081,
            DeprecatedPatchSequenceTargetAttribute = 1082,
            ProductIdAuthored = 1083,
            ImplicitMergeModulePrimaryFeature = 1084,
            DeprecatedIgnoreModularizationElement = 1085,
            PropertyModularizationSuppressed = 1086,
            DeprecatedPackageCompressedAttribute = 1087,
            DeprecatedQuestionMarksGuid = 1090,
            PackageCodeSet = 1091,
            InvalidFourPartVersion = 1093,
            InvalidRemoveFile = 1095,
            PreprocessorWarning = 1096,
            UpdateOfNonKeyPathFile = 1097,
            UnsupportedCommandLineArgument = 1098,
            MajorUpgradePatchNotRecommended = 1099,
            RetainRangeMismatch = 1100,
            DefaultLanguageUsedForVersionedFile = 1101,
            DefaultLanguageUsedForUnversionedFile = 1102,
            DefaultVersionUsedForUnversionedFile = 1103,
            InvalidHigherInstallerVersionInModule = 1104,
            ValidationFailedDueToSystemPolicy = 1105,
            ColumnsIncompatibleWithInstallerVersion = 1106,
            TableIncompatibleWithInstallerVersion = 1107,
            DeprecatedCommandLineSwitch = 1108,
            UnexpectedEntrySection = 1109,
            NewComponentAddedToExistingFeature = 1110,
            DeprecatedAttributeValue = 1111,
            InsufficientPermissionHarvestTypeLib = 1112,
            UnclearShortcut = 1113,
            TooManyProgIds = 1114,
            BadColumnDataIgnored = 1115,
            NullMsiAssemblyNameValue = 1116,
            InvalidAttributeCombination = 1117,
            VariableDeclarationCollision = 1118,
            DuplicatePrimaryKey = 1119,
            RequiresMsi200for64bitPackage = 1121,
            ExternalCabsAreNotSigned = 1122,
            FailedToDeleteTempDir = 1123,
            StandardDirectoryConflictInMergeModule = 1124,
            PreprocessorUnknownPragma = 1125,
            DeprecatedComponentGroupId = 1126,
            UxPayloadsOnlySupportEmbedding = 1127,
            DiscardedRollbackBoundary = 1129,
            DeprecatedElement = 1130,
            CannotUpdateCabCache = 1131,
            DownloadUrlNotSupportedForBAPayloads = 1132,
            DiscouragedAllUsersValue = 1133,
            ImplicitlyPerUser = 1134,
            PerUserButForcingPerMachine = 1135,
            AttributeShouldContain = 1136,
            DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions = 1137,
            DeprecatedRegistryKeyActionAttribute = 1138,
            NotABinaryWixlib = 1139,
            NoPerMachineDependencies = 1140,
            DownloadUrlNotSupportedForAttachedContainers = 1141,
            ReservedAttribute = 1142,
            RequiresMsi500forArmPackage = 1143,
            RemotePayloadsMustNotAlsoBeCompressed = 1144,
            AllChangesIncludedInPatch = 1145,
            RelatedAttributeConditionallyIgnored = 1146,
            BackslashTerminateInlineDirectorySyntax = 1147,
            InvalidMsiProductVersion = 1148,
            ServiceConfigFamilyNotSupported = 1149,
            SymbolNotTranslatedToOutput = 1150,
            MsiTransactionLimitations = 1151,
            PathCanonicalized = 1152,
            DetectConditionRecommended = 1153,
            CollidingModularizationTypes = 1156,
            InvalidEnvironmentVariable = 1157,
            WindowsInstallerFileTooLarge = 1158,
            UnavailableBundleConditionVariable = 1159,
            DiscardedRollbackBoundary2 = 1160,
            ExePackageDetectInformationRecommended = 1161,
            InvalidWixVersion = 1162,
        }
    }
}
