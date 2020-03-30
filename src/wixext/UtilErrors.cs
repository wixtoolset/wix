// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class UtilErrors
    {
        public static Message ArgumentRequiresValue(string argument)
        {
            return Message(null, Ids.ArgumentRequiresValue, "The argument '{0}' does not have a value specified and it is required.", argument);
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

        public static Message IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWithoutComponent, "The {0}/@{1} attribute cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName, attributeName);
        }

        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalElementWithoutComponent, "The {0} element cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName);
        }

        public static Message IllegalFileValueInPerfmonOrManifest(string file, string table)
        {
            return Message(null, Ids.IllegalFileValueInPerfmonOrManifest, "The value '{0}' in the File column, {1} table is invalid. It should be in the form of '[#file]' or '[!file]'.", file, table);
        }

        public static Message InvalidRegistryObject(SourceLineNumber sourceLineNumbers, string registryElementName)
        {
            return Message(sourceLineNumbers, Ids.InvalidRegistryObject, "The {0} element has no id and cannot have its permissions set. If you want to set permissions on a 'placeholder' registry key, force its creation by setting the ForceCreateOnInstall attribute to yes.", registryElementName);
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
            IllegalAttributeWithoutComponent = 5050,
            IllegalElementWithoutComponent = 5051,
            DirectoryNotFound = 5052,
            EmptyDirectory = 5053,
            IllegalFileValueInPerfmonOrManifest = 5054,
            ErrorTransformingHarvestedWiX = 5055,
            UnableToOpenRegistryKey = 5056,
            SpacesNotAllowedInArgumentValue = 5057,
            ArgumentRequiresValue = 5058,
            FileNotFound = 5059,
            PerformanceCategoryNotFound = 5060,
            UnsupportedPerformanceCounterType = 5061,
            InvalidRegistryObject = 5063,
        }
    }
}
