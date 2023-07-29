// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Link;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class AssignDefaultFeatureCommand
    {
        public AssignDefaultFeatureCommand(FindEntrySectionAndLoadSymbolsCommand find, List<IntermediateSection> sections)
        {
            this.Find = find;
            this.Sections = sections;
        }

        public IEnumerable<IntermediateSection> Sections { get; }

        public FindEntrySectionAndLoadSymbolsCommand Find { get; }

        public void Execute()
        {
            if (this.Find.EntrySection.Type == SectionType.Package
                && !this.Sections.Where(s => s.Id != WixStandardLibraryIdentifiers.DefaultFeatureName)
                .SelectMany(s => s.Symbols).OfType<FeatureSymbol>().Any())
            {
                var addedToDefaultFeature = false;

                foreach (var section in this.Sections)
                {
                    var components = section.Symbols.OfType<ComponentSymbol>().ToList();
                    foreach (var component in components)
                    {
                        this.Find.EntrySection.AddSymbol(new WixComplexReferenceSymbol(component.SourceLineNumbers)
                        {
                            Parent = WixStandardLibraryIdentifiers.DefaultFeatureName,
                            ParentType = ComplexReferenceParentType.Feature,
                            ParentLanguage = null,
                            Child = component.Id.Id,
                            ChildType = ComplexReferenceChildType.Component,
                            IsPrimary = true,
                        });

                        this.Find.EntrySection.AddSymbol(new WixGroupSymbol(component.SourceLineNumbers)
                        {
                            ParentId = WixStandardLibraryIdentifiers.DefaultFeatureName,
                            ParentType = ComplexReferenceParentType.Feature,
                            ChildId = component.Id.Id,
                            ChildType = ComplexReferenceChildType.Component,
                        });

                        addedToDefaultFeature = true;
                    }
                }

                if (addedToDefaultFeature)
                {
                    this.Find.EntrySection.AddSymbol(new WixSimpleReferenceSymbol()
                    {
                        Table = "Feature",
                        PrimaryKeys = WixStandardLibraryIdentifiers.DefaultFeatureName,
                    });
                }
            }
        }
    }
}
