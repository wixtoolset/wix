// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Link;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;
    using static System.Collections.Specialized.BitVector32;

    internal class AssignDefaultFeatureCommand
    {
        private const string DefaultFeatureName = "WixDefaultFeature";

        public AssignDefaultFeatureCommand(IMessaging messaging, IntermediateSection entrySection, IEnumerable<IntermediateSection> sections, HashSet<string> referencedComponents, Link.ConnectToFeatureCollection componentsToFeatures)
        {
            this.Messaging = messaging;
            this.EntrySection = entrySection;
            this.Sections = sections;
            this.ReferencedComponents = referencedComponents;
            this.ComponentsToFeatures = componentsToFeatures;
        }

        public IMessaging Messaging { get; }

        public IntermediateSection EntrySection { get; }

        public IEnumerable<IntermediateSection> Sections { get; }

        public HashSet<string> ReferencedComponents { get; }

        public ConnectToFeatureCollection ComponentsToFeatures { get; }

        public void Execute()
        {
            var assignedComponents = false;

            foreach (var section in this.Sections)
            {
                foreach (var component in section.Symbols.OfType<ComponentSymbol>().ToList())
                {
                    if (!this.ReferencedComponents.Contains(component.Id.Id))
                    {
                        assignedComponents = true;

                        this.ComponentsToFeatures.Add(new ConnectToFeature(section, component.Id.Id, DefaultFeatureName, explicitPrimaryFeature: true));

                        section.AddSymbol(new FeatureComponentsSymbol
                        {
                            FeatureRef = DefaultFeatureName,
                            ComponentRef = component.Id.Id,
                        });
                    }
                }
            }

            if (assignedComponents)
            {
                this.EntrySection.AddSymbol(new FeatureSymbol(null, new Identifier(AccessModifier.Global, DefaultFeatureName))
                {
                    Level = 1,
                    Display = 0,
                    InstallDefault = FeatureInstallDefault.Local,
                });
            }
        }
    }
}
