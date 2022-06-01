// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Set the guids for components with generatable guids and validate all are appropriately unique.
    /// </summary>
    internal class ValidateWindowsInstallerProductConstraints
    {
        private const int MaximumAllowedComponentsInMsi = 65536;
        private const int MaximumAllowedFeatureDepthInMsi = 16;

        internal ValidateWindowsInstallerProductConstraints(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var componentCount = this.Section.Symbols.OfType<ComponentSymbol>().Count();
            var featuresWithParent = this.Section.Symbols.OfType<FeatureSymbol>().ToDictionary(f => f.Id.Id, f => f.ParentFeatureRef);
            var featuresWithDepth = new Dictionary<string, int>();

            if (componentCount > MaximumAllowedComponentsInMsi)
            {
                this.Messaging.Write(WindowsInstallerBackendErrors.ExceededMaximumAllowedComponentsInMsi(MaximumAllowedComponentsInMsi, componentCount));
            }

            foreach (var featureSymbol in this.Section.Symbols.OfType<FeatureSymbol>())
            {
                var featureDepth = CalculateFeaturesDepth(featureSymbol.Id.Id, featuresWithParent, featuresWithDepth);

                if (featureDepth > MaximumAllowedFeatureDepthInMsi)
                {
                    this.Messaging.Write(WindowsInstallerBackendErrors.ExceededMaximumAllowedFeatureDepthInMsi(featureSymbol.SourceLineNumbers, MaximumAllowedFeatureDepthInMsi, featureSymbol.Id.Id, featureDepth));
                }
            }
        }

        private static int CalculateFeaturesDepth(string id, Dictionary<string, string> featuresWithParent, Dictionary<string, int> featuresWithDepth)
        {
            if (featuresWithDepth.TryGetValue(id, out var featureDepth))
            {
                return featureDepth;
            }

            var parentId = featuresWithParent[id];
            if (!String.IsNullOrEmpty(parentId))
            {
                var parentDepth = CalculateFeaturesDepth(parentId, featuresWithParent, featuresWithDepth);

                featureDepth = parentDepth + 1;
            }
            else
            {
                featureDepth = 1;
            }

            featuresWithDepth.Add(id, featureDepth);

            return featureDepth;
        }
    }
}
