// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using WixToolset.Data;

    internal static class WindowsInstallerBackendWarnings
    {
        internal static Message LongPatchBaselineIdTrimmed(SourceLineNumber sourceLineNumbers, string baseTransformName, string trimmedTransformName)
        {
            return Message(sourceLineNumbers, Ids.LongPatchBaselineIdTrimmed, "The PatchBaseline/@Id='{0}' is too long. It is recommended to use short identifiers like 'RTM' and 'SP1'. The identifier has been trimmed to '{1}' so the patch can be created.", baseTransformName, trimmedTransformName);
        }

        public static Message ActionSequenceCollision(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName1, string actionName2, int sequenceNumber)
        {
            return Message(sourceLineNumbers, Ids.ActionSequenceCollision, "The {0} table contains actions '{1}' and '{2}' which both have the same sequence number {3}. Please change the sequence number for one of these actions to avoid an ICE warning.", sequenceTableName, actionName1, actionName2, sequenceNumber);
        }

        public static Message ActionSequenceCollision2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ActionSequenceCollision2, "The location of the action related to previous warning.");
        }

        public static Message BadColumnDataIgnored(SourceLineNumber sourceLineNumbers, string value, string tableName, string columnName)
        {
            return Message(sourceLineNumbers, Ids.BadColumnDataIgnored, "The value '{0}' in table '{1}', column '{2}' is invalid according to the column's validation information. The decompiled output includes a best-effort representation of this value.", value, tableName, columnName);
        }

        public static Message CannotUpdateCabCache(SourceLineNumber sourceLineNumbers, string cabinetPath, string detail)
        {
            return Message(sourceLineNumbers, Ids.CannotUpdateCabCache, "Cannot update the timestamp of cached cabinet: '{0}'. If the timestamp is not updated, the build may rebuild more than is necessary. To fix the issue, ensure that the cabinet file is writable, error: {1}", cabinetPath, detail);
        }

        public static Message ColumnsIncompatibleWithInstallerVersion(SourceLineNumber sourceLineNumbers, string tableName, int packageInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.ColumnsIncompatibleWithInstallerVersion, "Table '{0}' uses columns that require a version of Windows Installer greater than specified in your package ('{1}').", tableName, packageInstallerVersion);
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

        public static Message DeprecatedTable(string tableName)
        {
            return Message(null, Ids.DeprecatedTable, "The {0} table is not supported by the WiX toolset because it has been deprecated by the Windows Installer team. Any information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(SourceLineNumber sourceLineNumbers, string componentId, string guid, string type, string keyPath)
        {
            return Message(sourceLineNumbers, Ids.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions, "Component/@Id='{0}' with {2} '{3}' has a @Guid value '{1}' that duplicates another component in this package. This is not officially supported by Windows Installer and cannot be used when creating patches. It otherwise works as long as all components with the same GUID have mutually-exclusive conditions. It is recommended to give each component its own unique GUID.", componentId, guid, type, keyPath);
        }

        public static Message DuplicatePrimaryKey(SourceLineNumber sourceLineNumbers, string primaryKey, string tableName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatePrimaryKey, "The primary key '{0}' is duplicated in table '{1}' and will be ignored. Please remove one of the entries or rename a part of the primary key to avoid the collision.", primaryKey, tableName);
        }

        public static Message EmptyCabinet(SourceLineNumber sourceLineNumbers, string cabinetName, bool isPatch)
        {
            if (isPatch)
            {
                return Message(sourceLineNumbers, Ids.EmptyCabinet, "The cabinet '{0}' does not contain any files. If this patch contains no files, this warning can likely be safely ignored. Otherwise, try passing -p to torch.exe when first building the transforms, or add a ComponentRef to your PatchFamily authoring to pull changed files into the cabinet.", cabinetName, isPatch);
            }

            return Message(sourceLineNumbers, Ids.EmptyCabinet, "The cabinet '{0}' does not contain any files. If this installation contains no files, this warning can likely be safely ignored. Otherwise, please add files to the cabinet or remove it.", cabinetName);
        }

        public static Message ExternalCabsAreNotSigned(string databaseFile)
        {
            return Message(null, Ids.ExternalCabsAreNotSigned, "The installer database '{0}' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build.", databaseFile);
        }

        public static Message GeneratedShortFileNameConflict(SourceLineNumber sourceLineNumbers, string shortFileName)
        {
            return Message(sourceLineNumbers, Ids.GeneratedShortFileNameConflict, "The short file name '{0}' was generated for multiple files that may be installed to the same directory. This could be due to conflicting long file names specified by the File/@Name attribute. If that is the case, please resolve the conflict in those attributes. Otherwise, please manually set the File/@ShortName attribute on the conflicting row to fix the collision. If one of the colliding files was added via a patch, that short file name should be specified manually to avoid disturbing the original short file name.", shortFileName);
        }

        public static Message GeneratedShortFileNameConflict2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.GeneratedShortFileNameConflict2, "The location of a conflicting generated short file name related to the previous warning.");
        }

        public static Message IllegalActionInSequence(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.IllegalActionInSequence, "The {0} table contains an action '{1}' which is not allowed in this table. If this is a standard action then it is not valid for this table, if it is a custom action or dialog then this table does not accept actions of that type. This action will be left out of the decompiled output.", sequenceTableName, actionName);
        }

        public static Message IllegalRegistryKeyPath(SourceLineNumber sourceLineNumbers, string componentName, string registryId)
        {
            return Message(sourceLineNumbers, Ids.IllegalRegistryKeyPath, "Component '{0}' specifies an illegal registry keypath of '{1}'. Since this entry actually represents a registry key, not a registry value, it cannot be the keypath.", componentName, registryId);
        }

        public static Message InvalidAttributeCombination(SourceLineNumber sourceLineNumbers, string attrib1, string attrib2, string name, string value)
        {
            return Message(sourceLineNumbers, Ids.InvalidAttributeCombination, "It is invalid to combine attributes {0} and {1}. The decompiled output will set attribute {2} to {3}.", attrib1, attrib2, name, value);
        }

        public static Message InvalidHigherInstallerVersionInModule(SourceLineNumber sourceLineNumbers, string moduleId, int moduleInstallerVersion, int packageInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.InvalidHigherInstallerVersionInModule, "Merge module '{0}' has an installer version of {1} which is greater than the package's installer version of {2}. Merging a module with a higher installer version than the package it is being merged into can result in invalid values in the resulting msi. You must set the Package/@InstallerVersion attribute to {1} or greater to merge this merge module into your package.", moduleId, moduleInstallerVersion, packageInstallerVersion);
        }

        public static Message InvalidRemoveFile(SourceLineNumber sourceLineNumbers, string file, string component)
        {
            return Message(sourceLineNumbers, Ids.InvalidRemoveFile, "File '{0}' was removed from component '{1}'. Removing a file from a component will not result in the file being removed by a patch. You should author a RemoveFile element in your component to remove the file from the installation if you want the file to be removed.", file, component);
        }

        public static Message MajorUpgradePatchNotRecommended()
        {
            return Message(null, Ids.MajorUpgradePatchNotRecommended, "Changing the ProductCode in a patch is not recommended because the patch cannot be uninstalled nor can it be sequenced along with other patches for the target package. See https://learn.microsoft.com/en-us/windows/win32/msi/applying-major-upgrades-by-patching-the-local-installation-of-the-product for more information.");
        }

        public static Message MergeRescheduledAction(SourceLineNumber sourceLineNumbers, string tableName, string actionName, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeRescheduledAction, "The {0} table contains an action '{1}' which cannot be merged from the merge module '{2}'. This action is likely colliding with an action in the database that is being created. The colliding action may have been authored in the database or merged in from another merge module. If this is a standard action, it is likely colliding due to a difference in the condition for the action in the database and merge module. If this is a custom action, it should only be declared in the database or one merge module.", tableName, actionName, mergeModuleFile);
        }

        public static Message MergeTableFailed(SourceLineNumber sourceLineNumbers, string tableName, string primaryKeys, string mergeModuleFile)
        {
            return Message(sourceLineNumbers, Ids.MergeTableFailed, "The {0} table contains a row with primary key(s) '{1}' which cannot be merged from the merge module '{2}'. This is likely due to collision of rows with the same primary key(s) (but other different values in other columns) between the database and the merge module.", tableName, primaryKeys, mergeModuleFile);
        }

        public static Message NestedInstall(SourceLineNumber sourceLineNumbers, string tableName, string columnName, object value)
        {
            return Message(sourceLineNumbers, Ids.NestedInstall, "The {0}.{1} column's value, '{2}', indicates a nested install. Nested installations are not supported by the WiX team. This action will be left out of the decompiled output.", tableName, columnName, value);
        }

        public static Message NewComponentAddedToExistingFeature(SourceLineNumber sourceLineNumbers, string component, string feature, string transformPath)
        {
            return Message(sourceLineNumbers, Ids.NewComponentAddedToExistingFeature, "Component '{0}' was added to feature '{1}' in the transform '{2}'. If you cannot guarantee that this feature will always be installed, you should consider adding new components to new top-level features to prevent prompts for source when installing this patch.", component, feature, transformPath);
        }

        public static Message NullMsiAssemblyNameValue(SourceLineNumber sourceLineNumbers, string componentName, string name)
        {
            return Message(sourceLineNumbers, Ids.NullMsiAssemblyNameValue, "The assembly in component '{0}' has a null or empty {1} assembly name value.", componentName, name);
        }

        public static Message PatchTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.PatchTable, "The {0} table is added to the install package by a transform from a patch package (.msp) and not authored directly into an install package (.msi). The information in this table will be left out of the decompiled output.", tableName);
        }

        public static Message PossiblyIncorrectTypelibVersion(SourceLineNumber sourceLineNumbers, string id)
        {
            return Message(sourceLineNumbers, Ids.PossiblyIncorrectTypelibVersion, "The Typelib table entry with Id '{0}' could have an incorrect version of '256.0'. InstallShield has a bug relating to the Typelib Version column: it will incorrectly set the value '65536' in to represent version '1.0'. However, this number actually corresponds to version '256.0'. This bug will not affect the typelib version that is registered during installation, however, it will prevent the Windows Installer from correctly identifying whether a typelib is already installed and lead to unnecessary reinstallations of the typelib.", id);
        }

        public static Message SkippingMergeModuleTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.SkippingMergeModuleTable, "The {0} table can only be represented in WiX for merge modules. The information in this table will be left out of the decompiled output.", tableName);
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

        public static Message TableIncompatibleWithInstallerVersion(SourceLineNumber sourceLineNumbers, string tableName, int packageInstallerVersion)
        {
            return Message(sourceLineNumbers, Ids.TableIncompatibleWithInstallerVersion, "Using table '{0}' requires a version of Windows Installer greater than specified in your package ('{1}').", tableName, packageInstallerVersion);
        }

        public static Message TargetDirCorrectedDefaultDir()
        {
            return Message(null, Ids.TargetDirCorrectedDefaultDir, "The Directory with Id 'TARGETDIR' must have the value 'SourceDir' in its 'DefaultDir' column. This has been automatically corrected for you in the decompiled output.");
        }

        public static Message TooManyProgIds(SourceLineNumber sourceLineNumbers, string clsId, string progId, string otherClsId)
        {
            return Message(sourceLineNumbers, Ids.TooManyProgIds, "Class '{0}' tried to use ProgId '{1}' which has already been associated with class '{2}'. This information will be left out of the decompiled output.", clsId, progId, otherClsId);
        }

        public static Message UnexpectedTableInProduct(SourceLineNumber sourceLineNumbers, string tableName)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedTableInProduct, "An unexpected row in the '{0}' table was found in this package. Packages should not contain the '{0}' table.", tableName);
        }

        public static Message UnknownAction(SourceLineNumber sourceLineNumbers, string sequenceTableName, string actionName)
        {
            return Message(sourceLineNumbers, Ids.UnknownAction, "The {0} table contains an action '{1}' which is not a known custom action, dialog, or standard action. This action will be left out of the decompiled output.", sequenceTableName, actionName);
        }

        public static Message UpdateOfNonKeyPathFile(string nonKeyPathFileId, string componentId, string keyPathFileId)
        {
            return Message(null, Ids.UpdateOfNonKeyPathFile, "File '{0}' in Component '{1}' was changed, but the KeyPath file '{2}' was not. This file will not be patched on the target system if the REINSTALLMODE does not contain 'A'. The KeyPath file should also be changed and included in your patch.", nonKeyPathFileId, componentId, keyPathFileId);
        }

        public static Message ValidationWarning(SourceLineNumber sourceLineNumbers, string ice, string message)
        {
            return Message(sourceLineNumbers, Ids.ValidationWarning, "{0}: {1}", ice, message);
        }

        public static Message CollidingModularizationTypes(string tableName, string columnName, string foreignTableName, int foreignColumnNumber, string modularizationType, string foreignModularizationType)
        {
            return Message(null, Ids.CollidingModularizationTypes, "The definition for the '{0}' table's '{1}' column is a foreign key relationship to the '{2}' table's column number {3}. The modularization types of the two column definitions differ: table '{0}' uses type {4} and table '{2}' uses type {5}. Change one of the modularization types so that they match.", tableName, columnName, foreignTableName, foreignColumnNumber, modularizationType, foreignModularizationType);
        }

        public static Message InvalidEnvironmentVariable(string environmentVariable, string value, string defaultValue)
        {
            return Message(null, Ids.InvalidEnvironmentVariable, "The {0} environment variable is set to an invalid value of '{1}'. The default value '{2}' will be used instead.", environmentVariable, value, defaultValue);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            NestedInstall = 1004,
            SuppressAction = 1008,
            SuppressMergedAction = 1009,
            TargetDirCorrectedDefaultDir = 1010,
            UnknownAction = 1024,
            PossiblyIncorrectTypelibVersion = 1048,
            ActionSequenceCollision = 1050,
            ActionSequenceCollision2 = 1051,
            SuppressAction2 = 1052,
            UnexpectedTableInProduct = 1053,
            MergeRescheduledAction = 1055,
            MergeTableFailed = 1056,
            DecompiledStandardActionRelativelyScheduledInModule = 1057,
            IllegalActionInSequence = 1058,
            DecompilingAsCustomTable = 1060,
            SkippingMergeModuleTable = 1062,
            DeprecatedTable = 1065,
            PatchTable = 1066,
            GeneratedShortFileNameConflict = 1070,
            GeneratedShortFileNameConflict2 = 1071,
            DangerousTableInMergeModule = 1072,
            ValidationWarning = 1076,
            EmptyCabinet = 1079,
            IllegalRegistryKeyPath = 1081,
            InvalidRemoveFile = 1095,
            UpdateOfNonKeyPathFile = 1097,
            MajorUpgradePatchNotRecommended = 1099,
            DefaultLanguageUsedForVersionedFile = 1101,
            DefaultLanguageUsedForUnversionedFile = 1102,
            DefaultVersionUsedForUnversionedFile = 1103,
            InvalidHigherInstallerVersionInModule = 1104,
            ColumnsIncompatibleWithInstallerVersion = 1106,
            TableIncompatibleWithInstallerVersion = 1107,
            NewComponentAddedToExistingFeature = 1110,
            TooManyProgIds = 1114,
            BadColumnDataIgnored = 1115,
            NullMsiAssemblyNameValue = 1116,
            InvalidAttributeCombination = 1117,
            DuplicatePrimaryKey = 1119,
            ExternalCabsAreNotSigned = 1122,
            StandardDirectoryConflictInMergeModule = 1124,
            CannotUpdateCabCache = 1131,
            DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions = 1137,
            CollidingModularizationTypes = 1156,
            InvalidEnvironmentVariable = 1157,
            LongPatchBaselineIdTrimmed = 7100,
        } // last available is 7499. 7500 is WindowsInstallerBackendErrors.
    }
}
