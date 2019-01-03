// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using System;
    using WixToolset.Data;

    public enum SqlTupleDefinitionType
    {
        SqlDatabase,
        SqlFileSpec,
        SqlScript,
        SqlString,
    }

    public static partial class SqlTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out SqlTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(SqlTupleDefinitionType type)
        {
            switch (type)
            {
                case SqlTupleDefinitionType.SqlDatabase:
                    return SqlTupleDefinitions.SqlDatabase;

                case SqlTupleDefinitionType.SqlFileSpec:
                    return SqlTupleDefinitions.SqlFileSpec;

                case SqlTupleDefinitionType.SqlScript:
                    return SqlTupleDefinitions.SqlScript;

                case SqlTupleDefinitionType.SqlString:
                    return SqlTupleDefinitions.SqlString;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
