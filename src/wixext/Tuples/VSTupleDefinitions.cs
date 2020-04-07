// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System;
    using WixToolset.Data;

    public enum VSTupleDefinitionType
    {
        HelpFile,
        HelpFileToNamespace,
        HelpFilter,
        HelpFilterToNamespace,
        HelpNamespace,
        HelpPlugin,
    }

    public static partial class VSTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out VSTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(VSTupleDefinitionType type)
        {
            switch (type)
            {
                case VSTupleDefinitionType.HelpFile:
                    return VSTupleDefinitions.HelpFile;

                case VSTupleDefinitionType.HelpFileToNamespace:
                    return VSTupleDefinitions.HelpFileToNamespace;

                case VSTupleDefinitionType.HelpFilter:
                    return VSTupleDefinitions.HelpFilter;

                case VSTupleDefinitionType.HelpFilterToNamespace:
                    return VSTupleDefinitions.HelpFilterToNamespace;

                case VSTupleDefinitionType.HelpNamespace:
                    return VSTupleDefinitions.HelpNamespace;

                case VSTupleDefinitionType.HelpPlugin:
                    return VSTupleDefinitions.HelpPlugin;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
