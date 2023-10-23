// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System.Linq;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class ExampleOptimizerExtension : BaseOptimizerExtension
    {
        public override void PostOptimize(IOptimizeContext context)
        {
            foreach (var intermediate in context.Intermediates)
            {
                foreach (var symbol in intermediate.Sections.SelectMany(s=>s.Symbols).OfType<ExampleSymbol>())
                {
                    symbol.Value = $"{symbol.Value} <optimized>";
                }
            }
        }
    }
}
