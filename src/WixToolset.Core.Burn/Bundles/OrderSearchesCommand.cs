// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    internal class OrderSearchesCommand
    {
        public OrderSearchesCommand(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public IDictionary<string, IList<IntermediateTuple>> ExtensionSearchTuplesByExtensionId { get; private set; }

        public IList<ISearchFacade> OrderedSearchFacades { get; private set; }

        public void Execute()
        {
            // TODO: Although the WixSearch tables are defined in the Util extension,
            // the Bundle Binder has to know all about them. We hope to revisit all
            // of this in the 4.0 timeframe.
            var legacySearchesById = this.Section.Tuples
                .Where(t => t.Definition.Type == TupleDefinitionType.WixComponentSearch ||
                       t.Definition.Type == TupleDefinitionType.WixFileSearch ||
                       t.Definition.Type == TupleDefinitionType.WixProductSearch ||
                       t.Definition.Type == TupleDefinitionType.WixRegistrySearch)
                .ToDictionary(t => t.Id.Id);
            var extensionSearchesById = this.Section.Tuples
                .Where(t => t.Definition.HasTag(BurnConstants.BundleExtensionSearchTupleDefinitionTag))
                .ToDictionary(t => t.Id.Id);
            var searchTuples = this.Section.Tuples.OfType<WixSearchTuple>().ToList();

            this.ExtensionSearchTuplesByExtensionId = new Dictionary<string, IList<IntermediateTuple>>();
            this.OrderedSearchFacades = new List<ISearchFacade>(legacySearchesById.Keys.Count + extensionSearchesById.Keys.Count);

            foreach (var searchTuple in searchTuples)
            {
                if (legacySearchesById.TryGetValue(searchTuple.Id.Id, out var specificSearchTuple))
                {
                    this.OrderedSearchFacades.Add(new LegacySearchFacade(searchTuple, specificSearchTuple));
                }
                else if (extensionSearchesById.TryGetValue(searchTuple.Id.Id, out var extensionSearchTuple))
                {
                    this.OrderedSearchFacades.Add(new ExtensionSearchFacade(searchTuple));

                    if (!this.ExtensionSearchTuplesByExtensionId.TryGetValue(searchTuple.BundleExtensionRef, out var extensionSearchTuples))
                    {
                        extensionSearchTuples = new List<IntermediateTuple>();
                        this.ExtensionSearchTuplesByExtensionId[searchTuple.BundleExtensionRef] = extensionSearchTuples;
                    }
                    extensionSearchTuples.Add(extensionSearchTuple);
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.MissingBundleSearch(searchTuple.SourceLineNumbers, searchTuple.Id.Id));
                }
            }
        }
    }
}
