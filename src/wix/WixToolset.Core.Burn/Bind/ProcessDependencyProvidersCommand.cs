// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;
    using WixToolset.Data.Symbols;

    internal class ProcessDependencyProvidersCommand
    {
        public ProcessDependencyProvidersCommand(IServiceProvider serviceProvider, IntermediateSection section, PackageFacades facades)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();

            this.Section = section;
            this.Facades = facades;
        }

        public string BundleProviderKey { get; private set; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private PackageFacades Facades { get; }

        /// <summary>
        /// Sets the explicitly provided bundle provider key, if provided. And...
        /// Imports authored dependency providers for each package in the manifest,
        /// and generates dependency providers for certain package types that do not
        /// have a provider defined.
        /// </summary>
        public void Execute()
        {
            this.ProcessHarvestedProviders();

            var dependencySymbols = this.Section.Symbols.OfType<WixDependencyProviderSymbol>();

            foreach (var dependency in dependencySymbols)
            {
                // Sets the provider key for the bundle, if it is not set already.
                if (dependency.Bundle)
                {
                    if (String.IsNullOrEmpty(this.BundleProviderKey))
                    {
                        this.BundleProviderKey = dependency.ProviderKey;
                    }
                    else
                    {
                        this.Messaging.Write(BurnBackendErrors.BundleMultipleProviders(dependency.SourceLineNumbers, dependency.ProviderKey, this.BundleProviderKey));
                    }
                }

                // Import any authored dependencies. These may merge with imported provides from MSI packages.
                var packageId = dependency.ParentRef;

                if (this.Facades.TryGetFacadeByPackageId(packageId, out var facade))
                {
                    if (String.IsNullOrEmpty(dependency.ProviderKey))
                    {
                        switch (facade.SpecificPackageSymbol)
                        {
                            // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                            case WixBundleMsiPackageSymbol msiPackage:
                                dependency.ProviderKey = msiPackage.ProductCode;
                                break;
                            case WixBundleMspPackageSymbol mspPackage:
                                dependency.ProviderKey = mspPackage.PatchCode;
                                break;
                        }
                    }

                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        dependency.Version = facade.PackageSymbol.Version;
                    }

                    // If the version is still missing, a version could not be gathered from the package and was not authored.
                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        this.Messaging.Write(BurnBackendErrors.MissingDependencyVersion(facade.PackageId));
                    }

                    if (String.IsNullOrEmpty(dependency.DisplayName))
                    {
                        dependency.DisplayName = facade.PackageSymbol.DisplayName;
                    }
                }
            }

            var dependencySymbolsByPackageId = this.GetDependencySymbolsByPackageId(dependencySymbols);

            // Generate providers for MSI and MSP packages that still do not have providers.
            foreach (var facade in this.Facades.Values)
            {
                string key = null;

                if (facade.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage)
                {
                    key = msiPackage.ProductCode;
                }
                else if (facade.SpecificPackageSymbol is WixBundleMspPackageSymbol mspPackage)
                {
                    key = mspPackage.PatchCode;
                }

                if (!String.IsNullOrEmpty(key) && !dependencySymbolsByPackageId.Contains(facade.PackageId))
                {
                    this.Section.AddSymbol(new WixDependencyProviderSymbol(facade.PackageSymbol.SourceLineNumbers, facade.PackageSymbol.Id)
                    {
                        ParentRef = facade.PackageId,
                        ProviderKey = $"{key}_v{facade.PackageSymbol.Version}",
                        Version = facade.PackageSymbol.Version,
                        DisplayName = facade.PackageSymbol.DisplayName
                    });
                }
            }
        }

        private HashSet<string> GetDependencySymbolsByPackageId(IEnumerable<WixDependencyProviderSymbol> dependencySymbols)
        {
            var dependencySymbolsByKey = new Dictionary<string, WixDependencyProviderSymbol>();
            var dependencySymbolsByPackageId = new HashSet<string>();

            foreach (var dependency in dependencySymbols)
            {
                if (dependencySymbolsByKey.TryGetValue(dependency.ProviderKey, out var collision))
                {
                    // If not a perfect dependency collision, display an error.
                    if (dependency.ProviderKey != collision.ProviderKey ||
                        dependency.Version != collision.Version ||
                        dependency.DisplayName != collision.DisplayName)
                    {
                        this.Messaging.Write(BurnBackendErrors.DuplicateProviderDependencyKey(dependency.ProviderKey, dependency.ParentRef));
                    }
                }
                else
                {
                    dependencySymbolsByKey.Add(dependency.ProviderKey, dependency);
                }

                dependencySymbolsByPackageId.Add(dependency.ParentRef);
            }

            return dependencySymbolsByPackageId;
        }

        private void ProcessHarvestedProviders()
        {
            var harvestedDependencies = this.Section.Symbols.OfType<WixBundleHarvestedDependencyProviderSymbol>().ToList();
            foreach (var harvestedDependency in harvestedDependencies)
            {
                if (!this.Facades.TryGetFacadesByPackagePayloadId(harvestedDependency.PackagePayloadRef, out var facades))
                {
                    this.Messaging.Write(ErrorMessages.IdentifierNotFound("Package.PayloadRef", harvestedDependency.PackagePayloadRef));
                    continue;
                }

                foreach (var facade in facades)
                {
                    var depId = new Identifier(AccessModifier.Section, this.BackendHelper.GenerateIdentifier("dep", facade.PackageId, harvestedDependency.Id.Id));
                    this.Section.AddSymbol(new WixDependencyProviderSymbol(harvestedDependency.SourceLineNumbers, depId)
                    {
                        ParentRef = facade.PackageId,
                        ProviderKey = harvestedDependency.ProviderKey,
                        Version = harvestedDependency.Version,
                        DisplayName = harvestedDependency.DisplayName,
                        Attributes = WixDependencyProviderAttributes.ProvidesAttributesImported | (WixDependencyProviderAttributes)harvestedDependency.ProviderAttributes,
                    });
                }
            }
        }
    }
}
