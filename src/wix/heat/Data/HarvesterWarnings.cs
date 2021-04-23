// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Data
{
    using System;
    using System.Resources;
    using WixToolset.Data;

#pragma warning disable 1591 // TODO: add documentation
    public static class HarvesterWarnings
    {
        public static Message AssemblyHarvestFailed(string file, string message)
        {
            return Message(null, Ids.AssemblyHarvestFailed, "Could not harvest data from a file that was expected to be an assembly: {0}. If this file is not an assembly you can ignore this warning. Otherwise, this error detail may be helpful to diagnose the failure: {1}", file, message);
        }

        public static Message DuplicateDllRegistryEntry(string registryKey, string componentId)
        {
            return Message(null, Ids.DuplicateDllRegistryEntry, "Ignoring the registry key '{0}', it has already been added to the component '{1}'.", registryKey, componentId);
        }

        public static Message DuplicateDllRegistryEntry(string registryKey, string registryKeyValue, string componentId)
        {
            return Message(null, Ids.DuplicateDllRegistryEntry, "Ignoring the registry key '{0}', it has already been added to the component '{2}'. The registry key value '{1}' will not be harvested.", registryKey, registryKeyValue, componentId);
        }

        public static Message EncounteredNullDirectoryForWebSite(string directory)
        {
            return Message(null, Ids.EncounteredNullDirectoryForWebSite, "Could not harvest website directory: {0}.  Please update the output with the appropriate directory ID before using.", directory);
        }

        public static Message NoLogger(string exceptionMessage)
        {
            return Message(null, Ids.NoLogger, "Failed to set loggers: {0}", exceptionMessage);
        }

        public static Message NoProjectConfiguration(string exceptionMessage)
        {
            return Message(null, Ids.NoProjectConfiguration, "Failed to set project configuration and platform: {0}", exceptionMessage);
        }

        public static Message SelfRegHarvestFailed(string file, string message)
        {
            return Message(null, Ids.SelfRegHarvestFailed, "Could not harvest data from a file that was expected to be a SelfReg DLL: {0}. If this file does not support SelfReg you can ignore this warning. Otherwise, this error detail may be helpful to diagnose the failure: {1}", file, message);
        }

        public static Message TypeLibLoadFailed(string file, string message)
        {
            return Message(null, Ids.TypeLibLoadFailed, "Could not load file that was expected to be a type library based off of file extension: {0}. If this file is not a type library you can ignore this warning. Otherwise, this error detail may be helpful to diagnose the load failure: {1}", file, message);
        }

        public static Message UnsupportedRegistryType(string registryValue, int regFileLineNumber, string unsupportedType)
        {
            return Message(null, Ids.UnsupportedRegistryType, "Ignoring the registry value '{0}' found on line {1}, because it is of a type unsupported by Windows Installer ({2}).", registryValue, regFileLineNumber, unsupportedType);
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
            SelfRegHarvestFailed = 5150,
            AssemblyHarvestFailed = 5151,
            TypeLibLoadFailed = 5152,
            DuplicateDllRegistryEntry = 5156,
            UnsupportedRegistryType = 5157,
            NoProjectConfiguration = 5398,
            NoLogger = 5399,
            EncounteredNullDirectoryForWebSite = 5400,
        }
    }
}
