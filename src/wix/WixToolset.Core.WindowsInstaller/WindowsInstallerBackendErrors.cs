// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using WixToolset.Data;

    internal static class WindowsInstallerBackendErrors
    {
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

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            CannotLoadWixoutAsTransform = 7500,
            InvalidModuleVersion = 7501,
            ExceededMaximumAllowedComponentsInMsi = 7502,
            ExceededMaximumAllowedFeatureDepthInMsi = 7503,
        } // last available is 7999. 8000 is BurnBackendErrors.
    }
}
