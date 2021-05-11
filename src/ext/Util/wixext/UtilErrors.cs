// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class UtilErrors
    {
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
            IllegalFileValueInPerfmonOrManifest = 5054,
            InvalidRegistryObject = 5063,
        }
    }
}
