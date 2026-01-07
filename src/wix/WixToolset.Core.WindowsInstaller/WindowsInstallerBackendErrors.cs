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

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            OpenDatabaseFailed = 223,

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
