// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class NormalizeRelatedBundlesCommand
    {
        public NormalizeRelatedBundlesCommand(IMessaging messaging, WixBundleSymbol bundleSymbol, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.BundleSymbol = bundleSymbol;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            foreach (var relatedBundleSymbol in this.Section.Symbols.OfType<WixRelatedBundleSymbol>())
            {
                var elementName = "RelatedBundle";
                var attributeName = "Id";

                if (this.BundleSymbol.UpgradeCode == relatedBundleSymbol.BundleId)
                {
                    elementName = "Bundle";
                    attributeName = "UpgradeCode";
                }

                relatedBundleSymbol.BundleId = this.NormalizeBundleRelatedBundleId(relatedBundleSymbol.SourceLineNumbers, relatedBundleSymbol.BundleId, elementName, attributeName);
            }

            this.BundleSymbol.UpgradeCode = this.NormalizeBundleRelatedBundleId(this.BundleSymbol.SourceLineNumbers, this.BundleSymbol.UpgradeCode, null, null);
        }

        private string NormalizeBundleRelatedBundleId(SourceLineNumber sourceLineNumber, string relatedBundleId, string elementName, string attributeName)
        {
            if (Guid.TryParse(relatedBundleId, out var guid))
            {
                return guid.ToString("B").ToUpperInvariant();
            }
            else if (!String.IsNullOrEmpty(elementName))
            {
                this.Messaging.Write(ErrorMessages.IllegalGuidValue(sourceLineNumber, elementName, attributeName, relatedBundleId));
            }

            return relatedBundleId;
        }
    }
}
