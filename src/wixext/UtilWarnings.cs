// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class UtilWarnings
    {
        public static Message AssemblyHarvestFailed(string file, string message)
        {
            return Message(null, Ids.AssemblyHarvestFailed, "Could not harvest data from a file that was expected to be an assembly: {0}. If this file is not an assembly you can ignore this warning. Otherwise, this error detail may be helpful to diagnose the failure: {1}", file, message);
        }

        public static Message DeprecatedPerfCounterElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.DeprecatedPerfCounterElement, "The PerfCounter element has been deprecated.  Please use the PerformanceCounter element instead.");
        }

        public static Message DuplicateDllRegistryEntry(string registryKey, string componentId)
        {
            return Message(null, Ids.DuplicateDllRegistryEntry, "Ignoring the registry key '{0}', it has already been added to the component '{1}'.", registryKey, componentId);
        }

        public static Message DuplicateDllRegistryEntry(string registryKey, string registryKeyValue, string componentId)
        {
            return Message(null, Ids.DuplicateDllRegistryEntry, "Ignoring the registry key '{0}', it has already been added to the component '{2}'. The registry key value '{1}' will not be harvested.", registryKey, registryKeyValue, componentId);
        }

        public static Message RequiredAttributeForWindowsXP(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.RequiredAttributeForWindowsXP, "The {0}/@{1} attribute must be specified to successfully install on Windows XP.  You can ignore this warning if this installation does not install on Windows XP.", elementName, attributeName);
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
            DeprecatedPerfCounterElement = 5153,
            RequiredAttributeForWindowsXP = 5154,
            DuplicateDllRegistryEntry = 5156,
            UnsupportedRegistryType = 5157,
        }
    }
}
