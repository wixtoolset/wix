// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class AssignDefaultFeatureCommand
    {
        public AssignDefaultFeatureCommand(IntermediateSection entrySection, IEnumerable<IntermediateSection> sections)
        {
            this.EntrySection = entrySection;
            this.Sections = sections;
        }

        public IntermediateSection EntrySection { get; }

        public IEnumerable<IntermediateSection> Sections { get; }

        public void Execute()
        {
            foreach (var section in this.Sections)
            {
                var components = section.Symbols.OfType<ComponentSymbol>().ToList();
                foreach (var component in components)
                {
                    this.EntrySection.AddSymbol(new WixComplexReferenceSymbol(component.SourceLineNumbers)
                    {
                        Parent = WixStandardLibraryIdentifiers.DefaultFeatureName,
                        ParentType = ComplexReferenceParentType.Feature,
                        ParentLanguage = null,
                        Child = component.Id.Id,
                        ChildType = ComplexReferenceChildType.Component,
                        IsPrimary = true,
                    });

                    this.EntrySection.AddSymbol(new WixGroupSymbol(component.SourceLineNumbers)
                    {
                        ParentId = WixStandardLibraryIdentifiers.DefaultFeatureName,
                        ParentType = ComplexReferenceParentType.Feature,
                        ChildId = component.Id.Id,
                        ChildType = ComplexReferenceChildType.Component,
                    });
                }
            }

            this.EntrySection.AddSymbol(new WixSimpleReferenceSymbol()
            {
                Table = "Feature",
                PrimaryKeys = WixStandardLibraryIdentifiers.DefaultFeatureName,
            });
        }
    }
}
