// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Link
{
    using WixToolset.Data;

    internal static class IntermediateSymbolExtensions
    {
        public static bool IsIdentical(this IntermediateSymbol first, IntermediateSymbol second)
        {
            var identical = (first.Definition.Type == second.Definition.Type &&
                             (first.Definition.Type != SymbolDefinitionType.MustBeFromAnExtension || first.Definition.Name == second.Definition.Name) &&
                             first.Definition.FieldDefinitions.Length == second.Definition.FieldDefinitions.Length);

            for (var i = 0; identical && i < first.Definition.FieldDefinitions.Length; ++i)
            {
                var firstField = first[i];
                var secondField = second[i];

                identical = (firstField.AsString() == secondField.AsString());
            }

            return identical;
        }
    }
}
