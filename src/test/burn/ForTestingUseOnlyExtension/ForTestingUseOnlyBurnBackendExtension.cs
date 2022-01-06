// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using System.Collections.Generic;
    using System.Linq;
    using ForTestingUseOnly.Symbols;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;

    /// <summary>
    /// Extension for doing completely unsupported things in the name of testing.
    /// </summary>
    public class ForTestingUseOnlyBurnBackendExtension : BaseBurnBackendBinderExtension
    {
        private static readonly IntermediateSymbolDefinition[] BurnSymbolDefinitions =
        {
            ForTestingUseOnlySymbolDefinitions.ForTestingUseOnlyBundle,
        };

        protected override IReadOnlyCollection<IntermediateSymbolDefinition> SymbolDefinitions => BurnSymbolDefinitions;

        public override void SymbolsFinalized(IntermediateSection section)
        {
            base.SymbolsFinalized(section);

            this.FinalizeBundleSymbol(section);
        }

        private void FinalizeBundleSymbol(IntermediateSection section)
        {
            var forTestingUseOnlyBundleSymbol = section.Symbols.OfType<ForTestingUseOnlyBundleSymbol>().SingleOrDefault();
            if (null == forTestingUseOnlyBundleSymbol)
            {
                return;
            }

            var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
            bundleSymbol.ProviderKey = bundleSymbol.BundleId = forTestingUseOnlyBundleSymbol.BundleId;
        }
    }
}
