// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class GetBootstrapperApplicationSymbolsCommand
    {
        public GetBootstrapperApplicationSymbolsCommand(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public WixBootstrapperApplicationSymbol Primary { get; private set; }

        public WixBootstrapperApplicationSymbol Secondary { get; private set; }

        public void Execute()
        {
            var applications = this.Section.Symbols.OfType<WixBootstrapperApplicationSymbol>().ToList();

            var primaries = applications.Where(a => a.Secondary != true).ToList();

            var secondaries = applications.Where(a => a.Secondary == true).ToList();

            if (primaries.Count > 1)
            {
                this.ReportTooManyBootstrapperApplications(primaries);
            }
            else if (primaries.Count == 0)
            {
                this.Messaging.Write(BurnBackendErrors.MissingPrimaryBootstrapperApplication());
            }
            else
            {
                this.Primary = primaries[0];
            }

            if (secondaries.Count > 1)
            {
                this.ReportTooManyBootstrapperApplications(secondaries);
            }
            else if (secondaries.Count == 1)
            {
                this.Secondary = secondaries[0];
            }
        }

        public void ReportTooManyBootstrapperApplications(IEnumerable<WixBootstrapperApplicationSymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                this.Messaging.Write(BurnBackendErrors.TooManyBootstrapperApplications(symbol.SourceLineNumbers, symbol));
            }
        }
    }
}
