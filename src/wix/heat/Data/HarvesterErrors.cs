// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Data
{
    using System;
    using System.Resources;
    using WixToolset.Data;

#pragma warning disable 1591 // TODO: add documentation
    public static class HarvesterErrors
    {
        public static Message ArgumentRequiresValue(string argument)
        {
            return Message(null, Ids.ArgumentRequiresValue, "The argument '{0}' does not have a value specified and it is required.", argument);
        }

        public static Message BuildErrorDuringHarvesting(string buildError)
        {
            return Message(null, Ids.BuildErrorDuringHarvesting, "Build error during harvesting: {0}", buildError);
        }

        public static Message BuildFailed()
        {
            return Message(null, Ids.BuildFailed, "Build failed.");
        }

        public static Message CannotBuildProject(string projectFile, string innerExceptionMessage)
        {
            return Message(null, Ids.CannotBuildProject, "Failed to build project {0}: {1}", projectFile, innerExceptionMessage);
        }

        public static Message CannotHarvestWebSite()
        {
            return Message(null, Ids.CannotHarvestWebSite, "Cannot harvest website. On Windows Vista, you must install IIS 6 Management Compatibility.");
        }

        public static Message CannotLoadMSBuildAssembly(string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadMSBuildAssembly, "Failed to load MSBuild assembly: {0}", innerExceptionMessage);
        }

        public static Message CannotLoadMSBuildEngine(string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadMSBuildEngine, "Failed to load MSBuild engine: {0}", innerExceptionMessage);
        }

        public static Message CannotLoadMSBuildWrapperAssembly(string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadMSBuildWrapperAssembly, "Failed to load MSBuild wrapper assembly: {0}", innerExceptionMessage);
        }

        public static Message CannotLoadMSBuildWrapperObject(string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadMSBuildWrapperObject, "Failed to load MSBuild wrapper object: {0}", innerExceptionMessage);
        }

        public static Message CannotLoadMSBuildWrapperType(string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadMSBuildWrapperType, "Failed to load MSBuild wrapper type: {0}", innerExceptionMessage);
        }

        public static Message CannotLoadProject(string projectFile, string innerExceptionMessage)
        {
            return Message(null, Ids.CannotLoadProject, "Failed to load project {0}: {1}", projectFile, innerExceptionMessage);
        }

        public static Message DirectoryAttributeAccessorBadType(string attributeName)
        {
            return Message(null, Ids.DirectoryAttributeAccessorBadType, "DirectoryAttributeAccessor tried to access an invalid element type for attribute '{0'}.", attributeName);
        }

        public static Message DirectoryNotFound(string directory)
        {
            return Message(null, Ids.DirectoryNotFound, "The directory '{0}' could not be found.", directory);
        }

        public static Message EmptyDirectory(string directory)
        {
            return Message(null, Ids.EmptyDirectory, "The directory '{0}' did not contain any files or sub-directories and since empty directories are not being kept, there was nothing to harvest.", directory);
        }

        public static Message ErrorTransformingHarvestedWiX(string transform, string message)
        {
            return Message(null, Ids.ErrorTransformingHarvestedWiX, "Error applying transform {0} to harvested WiX: {1}", transform, message);
        }

        public static Message FileNotFound(string file)
        {
            return Message(null, Ids.FileNotFound, "The file '{0}' cannot be found.", file);
        }

        public static Message InsufficientPermissionHarvestWebSite()
        {
            return Message(null, Ids.InsufficientPermissionHarvestWebSite, "Not enough permissions to harvest website. On Windows Vista, you must run Heat elevated.");
        }

        public static Message InvalidDirectoryId(string generateType)
        {
            return Message(null, Ids.InvalidDirectoryId, "Invalid directory ID: {0}. Check that it doesn't start with a hyphen or slash.", generateType);
        }

        public static Message InvalidDirectoryOutputType(string generateType)
        {
            return Message(null, Ids.InvalidOutputType, "Invalid generated type: {0}. Must be one of: components, payloadgroup.", generateType);
        }

        public static Message InvalidOutputGroup(string outputGroup)
        {
            return Message(null, Ids.InvalidOutputGroup, "Invalid project output group: {0}.", outputGroup);
        }

        public static Message InvalidProjectOutputType(string generateType)
        {
            return Message(null, Ids.InvalidOutputType, "Invalid generated type: {0}. Must be one of: components, container, payloadgroup, packagegroup.", generateType);
        }

        public static Message InvalidProjectName(string generateType)
        {
            return Message(null, Ids.InvalidProjectName, "Invalid project name: {0}. Check that it doesn't start with a hyphen or slash.", generateType);
        }

        public static Message MissingProjectOutputGroup(string projectFile, string outputGroup)
        {
            return Message(null, Ids.MissingProjectOutputGroup, "Missing project output group '{1}' in project {0}.", projectFile, outputGroup);
        }

        public static Message MsbuildBinPathRequired(string version)
        {
            return Message(null, Ids.MsbuildBinPathRequired, "MSBuildBinPath required for ToolsVersion '{0}'", version);
        }

        public static Message NoOutputGroupSpecified()
        {
            return Message(null, Ids.NoOutputGroupSpecified, "No project output group specified.");
        }

        public static Message PerformanceCategoryNotFound(string key)
        {
            return Message(null, Ids.PerformanceCategoryNotFound, "Performance category '{0}' not found.", key);
        }

        public static Message SpacesNotAllowedInArgumentValue(string arg, string value)
        {
            return Message(null, Ids.SpacesNotAllowedInArgumentValue, "The switch '{0}' does not allow the spaces from the value. Please remove the spaces in from the value: {1}", arg, value);
        }

        public static Message UnableToOpenRegistryKey(string key)
        {
            return Message(null, Ids.UnableToOpenRegistryKey, "Unable to open registry key '{0}'.", key);
        }

        public static Message UnsupportedPerformanceCounterType(string key)
        {
            return Message(null, Ids.UnsupportedPerformanceCounterType, "Unsupported performance counter type '{0}'.", key);
        }

        public static Message WebSiteNotFound(string webSiteDescription)
        {
            return Message(null, Ids.WebSiteNotFound, "The web site '{0}' could not be found.  Please check that the web site exists, and that it is spelled correctly (please note, you must use the correct case).", webSiteDescription);
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
            DirectoryNotFound = 5052,
            EmptyDirectory = 5053,
            ErrorTransformingHarvestedWiX = 5055,
            UnableToOpenRegistryKey = 5056,
            SpacesNotAllowedInArgumentValue = 5057,
            ArgumentRequiresValue = 5058,
            FileNotFound = 5059,
            PerformanceCategoryNotFound = 5060,
            UnsupportedPerformanceCounterType = 5061,
            WebSiteNotFound = 5158,
            InsufficientPermissionHarvestWebSite = 5159,
            CannotHarvestWebSite = 5160,
            InvalidOutputGroup = 5301,
            NoOutputGroupSpecified = 5302,
            CannotLoadMSBuildAssembly = 5303,
            CannotLoadMSBuildEngine = 5304,
            CannotLoadProject = 5305,
            CannotBuildProject = 5306,
            BuildFailed = 5307,
            MissingProjectOutputGroup = 5308,
            DirectoryAttributeAccessorBadType = 5309,
            InvalidOutputType = 5310,
            InvalidDirectoryId = 5311,
            InvalidProjectName = 5312,
            BuildErrorDuringHarvesting = 5313,
            CannotLoadMSBuildWrapperAssembly = 5314,
            CannotLoadMSBuildWrapperType = 5315,
            CannotLoadMSBuildWrapperObject = 5316,
            MsbuildBinPathRequired = 5317,
        }
    }
}
