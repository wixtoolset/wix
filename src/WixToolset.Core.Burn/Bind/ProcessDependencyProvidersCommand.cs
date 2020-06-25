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
        public ProcessDependencyProvidersCommand(IMessaging messaging, IntermediateSection section, IDictionary<string, PackageFacade> facades)
        {
            this.Messaging = messaging;
            this.Section = section;

            this.Facades = facades;
        }

        public string BundleProviderKey { get; private set; }

        public Dictionary<string, ProvidesDependencySymbol> DependencySymbolsByKey { get; private set; }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private IDictionary<string, PackageFacade> Facades { get; }

        /// <summary>
        /// Sets the explicitly provided bundle provider key, if provided. And...
        /// Imports authored dependency providers for each package in the manifest,
        /// and generates dependency providers for certain package types that do not
        /// have a provider defined.
        /// </summary>
        public void Execute()
        {
            var wixDependencyProviderSymbols = this.Section.Symbols.OfType<WixDependencyProviderSymbol>();

            foreach (var wixDependencyProviderSymbol in wixDependencyProviderSymbols)
            {
                // Sets the provider key for the bundle, if it is not set already.
                if (String.IsNullOrEmpty(this.BundleProviderKey))
                {
                    if (wixDependencyProviderSymbol.Bundle)
                    {
                        this.BundleProviderKey = wixDependencyProviderSymbol.ProviderKey;
                    }
                }

                // Import any authored dependencies. These may merge with imported provides from MSI packages.
                var packageId = wixDependencyProviderSymbol.Id.Id;

                if (this.Facades.TryGetValue(packageId, out var facade))
                {
                    var dependency = this.Section.AddSymbol(new ProvidesDependencySymbol(wixDependencyProviderSymbol.SourceLineNumbers, wixDependencyProviderSymbol.Id)
                    {
                        PackageRef = packageId,
                        Key = wixDependencyProviderSymbol.ProviderKey,
                        Version = wixDependencyProviderSymbol.Version,
                        DisplayName = wixDependencyProviderSymbol.DisplayName,
                        Attributes = (int)wixDependencyProviderSymbol.Attributes
                    });

                    if (String.IsNullOrEmpty(dependency.Key))
                    {
                        switch (facade.SpecificPackageSymbol)
                        {
                            // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                            case WixBundleMsiPackageSymbol msiPackage:
                                dependency.Key = msiPackage.ProductCode;
                                break;
                            case WixBundleMspPackageSymbol mspPackage:
                                dependency.Key = mspPackage.PatchCode;
                                break;
                        }
                    }

                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        dependency.Version = facade.PackageSymbol.Version;
                    }

                    // If the version is still missing, a version could not be harvested from the package and was not authored.
                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        this.Messaging.Write(ErrorMessages.MissingDependencyVersion(facade.PackageId));
                    }

                    if (String.IsNullOrEmpty(dependency.DisplayName))
                    {
                        dependency.DisplayName = facade.PackageSymbol.DisplayName;
                    }
                }
            }

            this.DependencySymbolsByKey = this.GetDependencySymbolsByKey();

            // Generate providers for MSI and MSP packages that still do not have providers.
            foreach (var facade in this.Facades.Values)
            {
                string key = null;

                //if (WixBundlePackageType.Msi == facade.PackageSymbol.Type)
                if (facade.SpecificPackageSymbol is WixBundleMsiPackageSymbol msiPackage)
                {
                    //var msiPackage = (WixBundleMsiPackageSymbol)facade.SpecificPackageSymbol;
                    key = msiPackage.ProductCode;
                }
                //else if (WixBundlePackageType.Msp == facade.PackageSymbol.Type)
                else if (facade.SpecificPackageSymbol is WixBundleMspPackageSymbol mspPackage)
                {
                    //var mspPackage = (WixBundleMspPackageSymbol)facade.SpecificPackageSymbol;
                    key = mspPackage.PatchCode;
                }

                if (!String.IsNullOrEmpty(key) && !this.DependencySymbolsByKey.ContainsKey(key))
                {
                    var dependency = this.Section.AddSymbol(new ProvidesDependencySymbol(facade.PackageSymbol.SourceLineNumbers, facade.PackageSymbol.Id)
                    {
                        PackageRef = facade.PackageId,
                        Key = key,
                        Version = facade.PackageSymbol.Version,
                        DisplayName = facade.PackageSymbol.DisplayName
                    });

                    this.DependencySymbolsByKey.Add(dependency.Key, dependency);
                }
            }
        }

        private Dictionary<string, ProvidesDependencySymbol> GetDependencySymbolsByKey()
        {
            var dependencySymbolsByKey = new Dictionary<string, ProvidesDependencySymbol>();

            var dependencySymbols = this.Section.Symbols.OfType<ProvidesDependencySymbol>();

            foreach (var dependency in dependencySymbols)
            {
                if (dependencySymbolsByKey.TryGetValue(dependency.Key, out var collision))
                {
                    // If not a perfect dependency collision, display an error.
                    if (dependency.Key != collision.Key ||
                        dependency.Version != collision.Version ||
                        dependency.DisplayName != collision.DisplayName)
                    {
                        this.Messaging.Write(ErrorMessages.DuplicateProviderDependencyKey(dependency.Key, dependency.PackageRef));
                    }
                }
                else
                {
                    dependencySymbolsByKey.Add(dependency.Key, dependency);
                }
            }

            return dependencySymbolsByKey;
        }
    }
}
