// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public static class IntermediateSectionExtensions
    {
        public static T AddSymbol<T>(this IntermediateSection section, T symbol)
            where T : IntermediateSymbol
        {
            section.Symbols.Add(symbol);
            return symbol;
        }
    }
}
