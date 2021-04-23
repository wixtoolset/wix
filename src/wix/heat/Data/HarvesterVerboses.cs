// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Data
{
    using System;
    using System.Resources;
    using WixToolset.Data;

#pragma warning disable 1591 // TODO: add documentation
    public static class HarvesterVerboses
    {
        public static Message FoundToolsVersion(string toolsVersion)
        {
            return Message(null, Ids.FoundToolsVersion, "Found ToolsVersion {0} inside project file.", toolsVersion);
        }

        public static Message HarvestingAssembly(string fileName)
        {
            return Message(null, Ids.HarvestingAssembly, "Trying to harvest {0} as an assembly.", fileName);
        }

        public static Message HarvestingSelfReg(string fileName)
        {
            return Message(null, Ids.HarvestingSelfReg, "Trying to harvest self-registration information from native DLL {0}.", fileName);
        }

        public static Message HarvestingTypeLib(string fileName)
        {
            return Message(null, Ids.HarvestingTypeLib, "Trying to harvest type-library information from native DLL {0}.", fileName);
        }

        public static Message LoadingProjectWithBinPath(string msbuildBinPath)
        {
            return Message(null, Ids.LoadingProjectWithBinPath, "Loading project using MSBuild bin path {0}.", msbuildBinPath);
        }

        public static Message LoadingProjectWithVersion(string msbuildVersion)
        {
            return Message(null, Ids.LoadingProjectWithVersion, "Loading project using MSBuild version {0}.", msbuildVersion);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Verbose, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Verbose, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            HarvestingAssembly = 5100,
            HarvestingSelfReg = 5101,
            HarvestingTypeLib = 5102,
            LoadingProjectWithVersion = 5378,
            FoundToolsVersion = 5379,
            LoadingProjectWithBinPath = 5380,
        }
    }
}
