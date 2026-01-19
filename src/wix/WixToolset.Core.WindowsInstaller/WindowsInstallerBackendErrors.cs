// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using WixToolset.Data;

    internal static class WindowsInstallerBackendErrors
    {
        public static Message OpenDatabaseFailed(string databaseFile, string error)
        {
            return Message(null, Ids.OpenDatabaseFailed, "Failed to open database '{0}'. Ensure it is a valid database, is writable, and it is not open by another process. {1}", databaseFile, error);
        }

        public static Message CannotLoadWixoutAsTransform(SourceLineNumber sourceLineNumbers, Exception exception)
        {
            var additionalDetail = exception == null ? String.Empty : ", detail: " + exception.Message;

            return Message(sourceLineNumbers, Ids.CannotLoadWixoutAsTransform, "Could not load wixout file as a transform{1}", additionalDetail);
        }

        internal static Message ExceededMaximumAllowedComponentsInMsi(int maximumAllowedComponentsInMsi, int componentCount)
        {
            return Message(null, Ids.ExceededMaximumAllowedComponentsInMsi, "Maximum number of Components allowed in an MSI was exceeded. An MSI cannot contain more than {0} Components. The MSI contains {1} Components.", maximumAllowedComponentsInMsi, componentCount);
        }

        internal static Message ExceededMaximumAllowedFeatureDepthInMsi(SourceLineNumber sourceLineNumbers, int maximumAllowedFeatureDepthInMsi, string featureId, int featureDepth)
        {
            return Message(sourceLineNumbers, Ids.ExceededMaximumAllowedFeatureDepthInMsi, "Maximum depth of the Feature tree allowed in an MSI was exceeded. An MSI does not support a Feature tree with depth greater than {0}. The Feature '{1}' is at depth {2}.", maximumAllowedFeatureDepthInMsi, featureId, featureDepth);
        }

        public static Message InvalidModuleVersion(SourceLineNumber originalLineNumber, string version)
        {
            return Message(originalLineNumber, Ids.InvalidModuleVersion, "The Module/@Version was not be able to be used as a four-part version. A valid four-part version has a max value of \"65535.65535.65535.65535\" and must be all numeric.", version);
        }

        public static Message InvalidWindowsInstallerWixpdbForValidation(string wixpdbPath)
        {
            return Message(null, Ids.InvalidWindowsInstallerWixpdbForValidation, "The validation .wixpdb file: {0} was not from a Windows Installer database build (.msi or .msm). Verify that the output type was actually an MSI Package or Merge Module.", wixpdbPath);
        }

        public static Message UnexpectedAnonymousDirectoryCollision(SourceLineNumber sourceLineNumbers, string id, string parentDir, string defaultDir, SourceLineNumber existingSourceLineNumbers, string existingParentDir, string existingDefaultDir)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedAnonymousDirectoryCollision, "This should not happen. The first directory id '{0}' uses parent directory '{1}' with DefaultDir '{2}'. The colliding directory uses parent directory '{3}' with DefaultDir '{4}' from line: {5}", id, parentDir, defaultDir, existingParentDir, existingDefaultDir, existingSourceLineNumbers.ToString());
        }

        public static Message UnknownDecompileType(string decompileType, string filePath)
        {
            return Message(null, Ids.UnknownDecompileType, "Unknown decompile type '{0}' from input: {1}", decompileType, filePath);
        }

        public static Message UnknownValidationTargetFileExtension(string fileExtension)
        {
            return Message(null, Ids.UnknownValidationTargetFileExtension, "Unknown file extension: {0}. Use the -cub switch to specify the path to the ICE CUBe file", fileExtension);
        }

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

        public static Message ActionScheduledRelativeToTerminationAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToTerminationAction, "The {0} table contains an action '{1}' that is scheduled to come before or after action '{2}', which is a special action which only occurs when the installer terminates. These special actions can be identified by their negative sequence numbers. Please schedule the action '{1}' to come before or after a different action.", sequenceTableName, actionName1, actionName2);
        }

        public static Message ActionScheduledRelativeToTerminationAction2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToTerminationAction2, "The location of the special termination action related to previous error(s).");
        }

        public static Message BinderFileManagerMissingFile(SourceLineNumber sourceLineNumbers, string exceptionMessage)
        {
            return Message(sourceLineNumbers, Ids.BinderFileManagerMissingFile, "{0}", exceptionMessage);
        }

        public static Message BothUpgradeCodesRequired()
        {
            return Message(null, Ids.BothUpgradeCodesRequired, "Both the target and updated package authoring must define the Package/@UpgradeCode attribute if the transform validates the UpgradeCode (default). Either define the Package/@UpgradeCode attribute in both the target and updated authoring, or set the Validate/@UpgradeCode attribute to 'no' in the patch authoring.");
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

        public static Message CannotFindFile(SourceLineNumber sourceLineNumbers, string fileId, string fileName, string filePath)
        {
            return Message(sourceLineNumbers, Ids.CannotFindFile, "The file with id '{0}' and name '{1}' could not be found with source path: '{2}'.", fileId, fileName, filePath);
        }

        public static Message CannotOpenMergeModule(SourceLineNumber sourceLineNumbers, string mergeModuleIdentifier, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.CannotOpenMergeModule, "Cannot open the merge module '{0}' from file '{1}'.", mergeModuleIdentifier, mergeModuleFile);
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

        public static Message CustomActionSequencedInModule(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.CustomActionSequencedInModule, "The {0} table contains a custom action '{1}' that has a sequence number specified. The Sequence attribute is not allowed for custom actions in a merge module. Please remove the action or use the Before or After attributes to specify where this action should be sequenced relative to another action.", sequenceTableName, actionName);
        }

        public static Message DatabaseSchemaMismatch(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DatabaseSchemaMismatch, "The table definition of '{0}' in the target database does not match the table definition in the updated database. A transform requires that the target database schema match the updated database schema.", tableName);
        }

        public static Message DuplicateCabinetName(SourceLineNumber sourceLineNumbers, string cabinetName)
        {
            return Message(sourceLineNumbers, Ids.DuplicateCabinetName, "Duplicate cabinet name '{0}' found.", cabinetName);
        }

        public static Message DuplicateCabinetName2(SourceLineNumber sourceLineNumbers, string cabinetName)
        {
            return Message(sourceLineNumbers, Ids.DuplicateCabinetName2, "Duplicate cabinet name '{0}' error related to previous error.", cabinetName);
        }

        public static Message DuplicateComponentGuids(SourceLineNumber sourceLineNumbers, string componentId, string guid, string type, string keyPath)
        {
            return Message(sourceLineNumbers, Ids.DuplicateComponentGuids, "Component/@Id='{0}' with {2} '{3}' has a @Guid value '{1}' that duplicates another component in this package. It is recommended to give each component its own unique GUID.", componentId, guid, type, keyPath);
        }

        public static Message DuplicateExtensionTable(string extension, string tableName)
        {
            return Message(null, Ids.DuplicateExtensionTable, "The extension '{0}' contains a definition for table '{1}' that collides with a previously loaded table definition. Please remove one of the conflicting extensions or rename one of the tables to avoid the collision.", extension, tableName);
        }

        public static Message DuplicateModuleCaseInsensitiveFileIdentifier(SourceLineNumber sourceLineNumbers, string moduleId, string fileId1, string fileId2)
        {
            return Message(sourceLineNumbers, Ids.DuplicateModuleCaseInsensitiveFileIdentifier, "The merge module '{0}' contains 2 or more file identifiers that only differ by case: '{1}' and '{2}'. The WiX toolset extracts merge module files to the file system using these identifiers. Since most file systems are not case-sensitive a collision is likely. Please contact the owner of the merge module for a fix.", moduleId, fileId1, fileId2);
        }

        public static Message DuplicateModuleFileIdentifier(SourceLineNumber sourceLineNumbers, string moduleId, string fileId)
        {
            return Message(sourceLineNumbers, Ids.DuplicateModuleFileIdentifier, "The merge module '{0}' contains a file identifier, '{1}', that is duplicated either in another merge module or in a File/@Id attribute. File identifiers must be unique. Please change one of the file identifiers to a different value.", moduleId, fileId);
        }

        public static Message ExpectedClientPatchIdInWixMsp()
        {
            return Message(null, Ids.ExpectedClientPatchIdInWixMsp, "The WixMsp is missing the client patch ID. Recompile the patch source files with the latest WiX toolset.");
        }

        public static Message ExpectedMediaCabinet(SourceLineNumber sourceLineNumbers, string fileId, int diskId)
        {
            return Message(sourceLineNumbers, Ids.ExpectedMediaCabinet, "The file '{0}' should be compressed but is not part of a compressed media. Files will be compressed if either the File/@Compressed or Package/@Compressed attributes are set to 'yes'. This can be fixed by setting the Media/@Cabinet attribute for media '{1}'.", fileId, diskId);
        }

        public static Message ExpectedMediaRowsInWixMsp()
        {
            return Message(null, Ids.ExpectedMediaRowsInWixMsp, "The WixMsp has no media rows defined.");
        }

        public static Message ExpectedPatchIdInWixMsp()
        {
            return Message(null, Ids.ExpectedPatchIdInWixMsp, "The WixMsp is missing the patch ID.");
        }

        public static Message FileIdentifierNotFound(SourceLineNumber sourceLineNumbers, string fileIdentifier)
        {
            return Message(sourceLineNumbers, Ids.FileIdentifierNotFound, "The file row with identifier '{0}' could not be found.", fileIdentifier);
        }

        public static Message FileTooLarge(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.FileTooLarge, "'{0}' is too large, file size must be less than 2147483648.", fileName);
        }

        public static Message GACAssemblyIdentityWarning(SourceLineNumber sourceLineNumbers, string fileName, string assemblyName)
        {
            return Message(sourceLineNumbers, Ids.GACAssemblyIdentityWarning, "The destination name of file '{0}' does not match its assembly name '{1}' in your authoring. This will cause an installation failure for this assembly, because it will be installed to the Global Assembly Cache. To fix this error, update File/@Name of file '{0}' to be the actual name of the assembly.", fileName, assemblyName);
        }

        public static Message GacAssemblyNoStrongName(SourceLineNumber sourceLineNumbers, string assemblyName, string componentName)
        {
            return Message(sourceLineNumbers, Ids.GacAssemblyNoStrongName, "Assembly {0} in component {1} has no strong name and has been marked to be placed in the GAC. All assemblies installed to the GAC must have a valid strong name.", assemblyName, componentName);
        }

        public static Message IllegalGeneratedGuidComponentUnversionedKeypath(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalGeneratedGuidComponentUnversionedKeypath, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with more than one file cannot use an automatically generated guid unless a versioned file is the keypath and the other files are unversioned. This component's keypath is not versioned. Create multiple components to use automatically generated guids.");
        }

        public static Message IllegalGeneratedGuidComponentVersionedNonkeypath(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalGeneratedGuidComponentVersionedNonkeypath, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with more than one file cannot use an automatically generated guid unless a versioned file is the keypath and the other files are unversioned. This component has a non-keypath file that is versioned. Create multiple components to use automatically generated guids.");
        }

        public static Message IllegalPathForGeneratedComponentGuid(SourceLineNumber sourceLineNumbers, string componentName, string keyFilePath)
        {
            return Message(sourceLineNumbers, Ids.IllegalPathForGeneratedComponentGuid, "The component '{0}' has a key file with path '{1}'. Since this path is not rooted in one of the standard directories (like ProgramFilesFolder), this component does not fit the criteria for having an automatically generated guid. (This error may also occur if a path contains a likely standard directory such as nesting a directory with name \"Common Files\" under ProgramFilesFolder.)", componentName, keyFilePath);
        }

        public static Message InsertInvalidSequenceActionOrder(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionNameBefore, string actionNameAfter, string actionNameNew)
        {
            return Message(sourceLineNumbers, Ids.InsertInvalidSequenceActionOrder, "Invalid order of actions {1} and {2} in sequence table {0}. Action {3} must occur after {1} and before {2}, but {2} is currently sequenced after {1}. Please fix the ordering or explicitly supply a location for the action {3}.", sequenceTableName, actionNameBefore, actionNameAfter, actionNameNew);
        }

        public static Message InsertSequenceNoSpace(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionNameBefore, string actionNameAfter, string actionNameNew)
        {
            return Message(sourceLineNumbers, Ids.InsertSequenceNoSpace, "Not enough space exists to sequence action {3} in table {0}. It must be sequenced after {1} and before {2}, but those two actions are currently sequenced next to each other. Please move one of those actions to allow {3} to be inserted between them.", sequenceTableName, actionNameBefore, actionNameAfter, actionNameNew);
        }

        public static Message IntermediatesMustBeResolved(string invalidIntermediate)
        {
            return Message(null, Ids.IntermediatesMustBeResolved, "Intermediates being bound must have been resolved. This intermediate was not resolved: {0}", invalidIntermediate);
        }

        public static Message InvalidAddedFileRowWithoutSequence(SourceLineNumber sourceLineNumbers, string fileRowId)
        {
            return Message(sourceLineNumbers, Ids.InvalidAddedFileRowWithoutSequence, "A row has been added to the File table with id '{1}' that does not have a sequence number assigned to it. Create your transform from a pair of msi's instead of xml outputs to get sequences assigned to your File table's rows.", fileRowId);
        }

        public static Message InvalidAssemblyFile(SourceLineNumber sourceLineNumbers, string assemblyFile, string moreInformation)
        {
            return Message(sourceLineNumbers, Ids.InvalidAssemblyFile, "The assembly file '{0}' appears to be invalid. Please ensure this is a valid assembly file and that the user has the appropriate access rights to this file. More information: {1}", assemblyFile, moreInformation);
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

        public static Message InvalidRemoveComponent(SourceLineNumber sourceLineNumbers, string component, string feature, string transformPath)
        {
            return Message(sourceLineNumbers, Ids.InvalidRemoveComponent, "Removing component '{0}' from feature '{1}' is not supported. Either the component was removed or the guid changed in the transform '{2}'. Add the component back, undo the change to the component guid, or remove the entire feature.", component, feature, transformPath);
        }

        public static Message InvalidStringForCodepage(SourceLineNumber sourceLineNumbers, string codepage)
        {
            return Message(sourceLineNumbers, Ids.InvalidStringForCodepage, "A string was provided with characters that are not available in the specified database code page '{0}'. Either change these characters to ones that exist in the database's code page, or update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.", codepage);
        }

        public static Message MaximumCabinetSizeForLargeFileSplittingTooLarge(SourceLineNumber sourceLineNumbers, int maximumCabinetSizeForLargeFileSplitting, int maxValueOfMaxCabSizeForLargeFileSplitting)
        {
            return Message(sourceLineNumbers, Ids.MaximumCabinetSizeForLargeFileSplittingTooLarge, "'{0}' is too large. Reduce the size of maximum cabinet size for large file splitting. The maximum permitted value is '{1}' MB.", maximumCabinetSizeForLargeFileSplitting, maxValueOfMaxCabSizeForLargeFileSplitting);
        }

        public static Message MaximumUncompressedMediaSizeTooLarge(SourceLineNumber sourceLineNumbers, int maximumUncompressedMediaSize)
        {
            return Message(sourceLineNumbers, Ids.MaximumUncompressedMediaSizeTooLarge, "'{0}' is too large. Reduce the size of maximum uncompressed media size.", maximumUncompressedMediaSize);
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

        public static Message MergePlatformMismatch(SourceLineNumber sourceLineNumbers, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergePlatformMismatch, "'{0}' is a 64-bit merge module but the product consuming it is 32-bit. 32-bit products can consume only 32-bit merge modules.", mergeModuleFile);
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

        public static Message NoUniqueActionSequenceNumber(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2)
        {
            return Message(sourceLineNumbers, Ids.NoUniqueActionSequenceNumber, "The {0} table contains an action '{1}' which cannot have a unique sequence number because it is scheduled before or after action '{2}'. There is not enough room before or after this action to assign a unique sequence number. Please schedule one of the actions differently so that it will be in a position with more sequence numbers available. Please note that sequence numbers must be an integer in the range 1 - 32767 (inclusive).", sequenceTableName, actionName1, actionName2);
        }

        public static Message NoUniqueActionSequenceNumber2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.NoUniqueActionSequenceNumber2, "The location of the sequenced action related to previous error.");
        }

        public static Message OutputCodepageMismatch(SourceLineNumber sourceLineNumbers, int beforeCodepage, int afterCodepage)
        {
            return Message(sourceLineNumbers, Ids.OutputCodepageMismatch, "The code pages of the outputs do not match. One output's code page is '{0}' while the other is '{1}'.", beforeCodepage, afterCodepage);
        }

        public static Message OutputCodepageMismatch2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.OutputCodepageMismatch2, "The location of the mismatched code page related to the previous warning.");
        }

        public static Message OutputTypeMismatch(SourceLineNumber sourceLineNumbers, string beforeOutputType, string afterOutputType)
        {
            return Message(sourceLineNumbers, Ids.OutputTypeMismatch, "The types of the outputs do not match. One output's type is '{0}' while the other is '{1}'.", beforeOutputType, afterOutputType);
        }

        public static Message OverlengthTableNameInProductOrMergeModule(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.OverlengthTableNameInProductOrMergeModule, "The table name '{0}' is invalid because the table name exceeds 31 characters in length. For more information, see: https://learn.microsoft.com/en-au/windows/win32/msi/table-names", tableName);
        }

        public static Message OverridableActionCollision(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.OverridableActionCollision, "The {0} table contains an action '{1}' that is declared overridable in two different locations. Please remove one of the actions or the Overridable='yes' attribute from one of the actions.", sequenceTableName, actionName);
        }

        public static Message OverridableActionCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.OverridableActionCollision2, "The location of the action related to previous error.");
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

        public static Message ProductCodeInvalidForTransform(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ProductCodeInvalidForTransform, "The value '*' is not valid for the ProductCode when used in a transform or in a patch. Copy the ProductCode from your target product MSI into the Package/@ProductCode attribute value for your product authoring.");
        }

        public static Message ReadOnlyOutputFile(string filePath)
        {
            return Message(null, Ids.ReadOnlyOutputFile, "Unable to output to file '{0}' because it is marked as read-only.", filePath);
        }

        public static Message SameFileIdDifferentSource(SourceLineNumber sourceLineNumbers, string fileId, string sourcePath1, string sourcePath2)
        {
            return Message(sourceLineNumbers, Ids.SameFileIdDifferentSource, "Two different source paths '{1}' and '{2}' were detected for the same file identifier '{0}'. You must either author these under Media elements with different Id attribute values or in different patches.", fileId, sourcePath1, sourcePath2);
        }

        public static Message SplitCabinetCopyRegistrationFailed(string newCabName, string firstCabName)
        {
            return Message(null, Ids.SplitCabinetCopyRegistrationFailed, "Failed to register the copy command for cabinet '{0}' formed by splitting cabinet '{1}'.", newCabName, firstCabName);
        }

        public static Message StandardActionRelativelyScheduledInModule(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.StandardActionRelativelyScheduledInModule, "The {0} table contains a standard action '{1}' that does not have a sequence number specified. The Sequence attribute is required for standard actions in a merge module. Please remove the action or use the Sequence attribute.", sequenceTableName, actionName);
        }

        public static Message SuppressNonoverridableAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.SuppressNonoverridableAction, "The {0} table contains an action '{1}' that cannot be suppressed because it is not declared overridable in the base definition. Please stop suppressing the action or make it overridable in its base declaration.", sequenceTableName, actionName);
        }

        public static Message SuppressNonoverridableAction2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.SuppressNonoverridableAction2, "The location of the non-overridable definition of the action related to previous error.");
        }

        public static Message TooManyColumnsInRealTable(string tableName, int columnCount, int supportedColumnCount)
        {
            return Message(null, Ids.TooManyColumnsInRealTable, "The table '{0}' contains {1} columns which is not supported by Windows Installer. Windows Installer supports a maximum of {2} columns.", tableName, columnCount, supportedColumnCount);
        }

        public static Message TransformSchemaMismatch()
        {
            return Message(null, Ids.TransformSchemaMismatch, "The transform schema does not match the database schema. The transform may have been generated from a different database.");
        }

        public static Message UnableToGetAuthenticodeCertOfFile(string filePath, string moreInformation)
        {
            return Message(null, Ids.UnableToGetAuthenticodeCertOfFile, "Unable to get the authenticode certificate of '{0}'. More information: {1}", filePath, moreInformation);
        }

        public static Message UnableToGetAuthenticodeCertOfFileDownlevelOS(string filePath, string moreInformation)
        {
            return Message(null, Ids.UnableToGetAuthenticodeCertOfFileDownlevelOS, "Unable to get the authenticode certificate of '{0}'. The cryptography API has limitations on Windows XP and Windows Server 2003. More information: {1}", filePath, moreInformation);
        }

        public static Message UnableToOpenModule(SourceLineNumber sourceLineNumbers, string modulePath, string message)
        {
            return Message(sourceLineNumbers, Ids.UnableToOpenModule, "Unable to open merge module '{0}'. Check to make sure the module language is correct. '{1}'", modulePath, message);
        }

        public static Message UnexpectedCustomTableColumn(SourceLineNumber sourceLineNumbers, string column)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedCustomTableColumn, "The custom table column '{0}' is unknown.", column);
        }

        public static Message UnexpectedTableInMergeModule(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInMergeModule, "An unexpected row in the '{0}' table was found in this merge module. Merge modules cannot contain the '{0}' table.", tableName);
        }

        public static Message UnexpectedTableInPatch(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInPatch, "An unexpected row in the '{0}' table was found in this patch. Patches cannot contain the '{0}' table.", tableName);
        }

        public static Message ValidationError(SourceLineNumber sourceLineNumbers, string ice, string message)
        {
            return Message(sourceLineNumbers, Ids.ValidationError, "{0}: {1}", ice, message);
        }

        public static Message WixFileNotFound(string file)
        {
            return Message(null, Ids.WixFileNotFound, "The file '{0}' cannot be found.", file);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            CabExtractionFailed = 17,
            NoDataForColumn = 53,
            CabFileDoesNotExist = 76,
            CannotFindFile = 83,
            BinderFileManagerMissingFile = 84,
            DuplicateModuleFileIdentifier = 97,
            DuplicateModuleCaseInsensitiveFileIdentifier = 98,
            DuplicateExtensionTable = 126,
            CannotOpenMergeModule = 129,
            FileIdentifierNotFound = 131,
            InvalidAssemblyFile = 132,
            ExpectedMediaCabinet = 135,
            UnexpectedCustomTableColumn = 165,
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
            UnexpectedTableInMergeModule = 184,
            MergeExcludedModule = 186,
            MergeFeatureRequired = 187,
            MergeLanguageFailed = 188,
            MergeLanguageUnsupported = 189,
            InvalidMergeLanguage = 194,
            ValidationError = 204,
            InvalidKeyColumn = 220,
            OpenDatabaseFailed = 223,
            OutputTypeMismatch = 224,
            NoDifferencesInTransform = 227,
            OutputCodepageMismatch = 228,
            OutputCodepageMismatch2 = 229,
            IllegalPathForGeneratedComponentGuid = 231,
            InvalidManifestContent = 238,
            UnexpectedTableInPatch = 241,
            InvalidKeypathChange = 243,
            PatchWithoutTransforms = 246,
            PatchWithoutValidTransforms = 252,
            ExpectedPatchIdInWixMsp = 256,
            ExpectedMediaRowsInWixMsp = 257,
            WixFileNotFound = 258,
            ExpectedClientPatchIdInWixMsp = 259,
            NewRowAddedInTable = 260,
            PatchNotRemovable = 261,
            FileTooLarge = 263,
            ProductCodeInvalidForTransform = 269,
            InsertInvalidSequenceActionOrder = 270,
            InsertSequenceNoSpace = 271,
            MissingManifestForWin32Assembly = 272,
            UnableToOpenModule = 273,
            TransformSchemaMismatch = 278,
            DatabaseSchemaMismatch = 279,
            GacAssemblyNoStrongName = 282,
            DuplicateCabinetName = 290,
            DuplicateCabinetName2 = 291,
            InvalidAddedFileRowWithoutSequence = 292,
            CreateCabAddFileFailed = 296,
            CreateCabInsufficientDiskSpace = 297,
            GACAssemblyIdentityWarning = 299,
            InvalidRemoveComponent = 305,
            InvalidStringForCodepage = 311,
            SameFileIdDifferentSource = 317,
            BothUpgradeCodesRequired = 322,
            UnableToGetAuthenticodeCertOfFile = 327,
            UnableToGetAuthenticodeCertOfFileDownlevelOS = 328,
            ReadOnlyOutputFile = 329,
            MergePlatformMismatch = 345,
            MediaTableCollision = 357,
            MaximumUncompressedMediaSizeTooLarge = 359,
            MissingOrInvalidModuleInstallerVersion = 366,
            IllegalGeneratedGuidComponentUnversionedKeypath = 367,
            IllegalGeneratedGuidComponentVersionedNonkeypath = 368,
            DuplicateComponentGuids = 369,
            MaximumCabinetSizeForLargeFileSplittingTooLarge = 375,
            SplitCabinetCopyRegistrationFailed = 376,
            MissingMedia = 382,
            TooManyColumnsInRealTable = 386,
            CouldNotDetermineProductCodeFromTransformSummaryInfo = 394,
            IntermediatesMustBeResolved = 396,
            OverlengthTableNameInProductOrMergeModule = 415,
            CannotLoadWixoutAsTransform = 7500,
            InvalidModuleVersion = 7501,
            ExceededMaximumAllowedComponentsInMsi = 7502,
            ExceededMaximumAllowedFeatureDepthInMsi = 7503,
            UnknownDecompileType = 7504,
            UnknownValidationTargetFileExtension = 7505,
            InvalidWindowsInstallerWixpdbForValidation = 7506,
            UnexpectedAnonymousDirectoryCollision = 7507,
        } // last available is 7999. 8000 is BurnBackendErrors.
    }
}
