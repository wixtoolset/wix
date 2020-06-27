// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System;
    using WixToolset.Data;

    public enum VSSymbolDefinitionType
    {
        HelpFile,
        HelpFileToNamespace,
        HelpFilter,
        HelpFilterToNamespace,
        HelpNamespace,
        HelpPlugin,
    }

    public static partial class VSSymbolDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out VSSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(VSSymbolDefinitionType type)
        {
            switch (type)
            {
                case VSSymbolDefinitionType.HelpFile:
                    return VSSymbolDefinitions.HelpFile;

                case VSSymbolDefinitionType.HelpFileToNamespace:
                    return VSSymbolDefinitions.HelpFileToNamespace;

                case VSSymbolDefinitionType.HelpFilter:
                    return VSSymbolDefinitions.HelpFilter;

                case VSSymbolDefinitionType.HelpFilterToNamespace:
                    return VSSymbolDefinitions.HelpFilterToNamespace;

                case VSSymbolDefinitionType.HelpNamespace:
                    return VSSymbolDefinitions.HelpNamespace;

                case VSSymbolDefinitionType.HelpPlugin:
                    return VSSymbolDefinitions.HelpPlugin;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
