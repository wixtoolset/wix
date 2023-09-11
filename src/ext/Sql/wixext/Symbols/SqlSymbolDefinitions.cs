// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using System;
    using WixToolset.Data;

    public enum SqlSymbolDefinitionType
    {
        SqlDatabase,
        SqlFileSpec,
        SqlScript,
        SqlString,
    }

    public static partial class SqlSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out SqlSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(SqlSymbolDefinitionType type)
        {
            switch (type)
            {
                case SqlSymbolDefinitionType.SqlDatabase:
                    return SqlSymbolDefinitions.SqlDatabase;

                case SqlSymbolDefinitionType.SqlFileSpec:
                    return SqlSymbolDefinitions.SqlFileSpec;

                case SqlSymbolDefinitionType.SqlScript:
                    return SqlSymbolDefinitions.SqlScript;

                case SqlSymbolDefinitionType.SqlString:
                    return SqlSymbolDefinitions.SqlString;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
