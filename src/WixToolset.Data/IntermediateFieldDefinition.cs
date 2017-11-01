// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public enum IntermediateFieldType
    {
        String,
        Bool,
        Number,
        Path,
    }

    public class IntermediateFieldDefinition
    {
        public IntermediateFieldDefinition(string name, IntermediateFieldType type)
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }

        public IntermediateFieldType Type { get; }
    }
}
