// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;
    using WixToolset.Data.Tuples;

    internal class ProcessDependencyProvidersCommand
    {
        public ProcessDependencyProvidersCommand(IMessaging messaging, IntermediateSection section, IDictionary<string, PackageFacade> facades)
        {
            this.Messaging = messaging;
            this.Section = section;

            this.Facades = facades;
        }

        public string BundleProviderKey { get; private set; }

        public Dictionary<string, ProvidesDependencyTuple> DependencyTuplesByKey { get; private set; }

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
            var wixDependencyProviderTuples = this.Section.Tuples.OfType<WixDependencyProviderTuple>();

            foreach (var wixDependencyProviderTuple in wixDependencyProviderTuples)
            {
                // Sets the provider key for the bundle, if it is not set already.
                if (String.IsNullOrEmpty(this.BundleProviderKey))
                {
                    if (wixDependencyProviderTuple.Bundle)
                    {
                        this.BundleProviderKey = wixDependencyProviderTuple.ProviderKey;
                    }
                }

                // Import any authored dependencies. These may merge with imported provides from MSI packages.
                var packageId = wixDependencyProviderTuple.Id.Id;

                if (this.Facades.TryGetValue(packageId, out var facade))
                {
                    var dependency = new ProvidesDependencyTuple(wixDependencyProviderTuple.SourceLineNumbers, wixDependencyProviderTuple.Id)
                    {
                        PackageRef = packageId,
                        Key = wixDependencyProviderTuple.ProviderKey,
                        Version = wixDependencyProviderTuple.Version,
                        DisplayName = wixDependencyProviderTuple.DisplayName,
                        Attributes = (int)wixDependencyProviderTuple.Attributes
                    };

                    if (String.IsNullOrEmpty(dependency.Key))
                    {
                        switch (facade.SpecificPackageTuple)
                        {
                            // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                            case WixBundleMsiPackageTuple msiPackage:
                                dependency.Key = msiPackage.ProductCode;
                                break;
                            case WixBundleMspPackageTuple mspPackage:
                                dependency.Key = mspPackage.PatchCode;
                                break;
                        }
                    }

                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        dependency.Version = facade.PackageTuple.Version;
                    }

                    // If the version is still missing, a version could not be harvested from the package and was not authored.
                    if (String.IsNullOrEmpty(dependency.Version))
                    {
                        this.Messaging.Write(ErrorMessages.MissingDependencyVersion(facade.PackageId));
                    }

                    if (String.IsNullOrEmpty(dependency.DisplayName))
                    {
                        dependency.DisplayName = facade.PackageTuple.DisplayName;
                    }

                    this.Section.Tuples.Add(dependency);
                }
            }

            this.DependencyTuplesByKey = this.GetDependencyTuplesByKey();

            // Generate providers for MSI and MSP packages that still do not have providers.
            foreach (var facade in this.Facades.Values)
            {
                string key = null;

                //if (WixBundlePackageType.Msi == facade.PackageTuple.Type)
                if (facade.SpecificPackageTuple is WixBundleMsiPackageTuple msiPackage)
                {
                    //var msiPackage = (WixBundleMsiPackageTuple)facade.SpecificPackageTuple;
                    key = msiPackage.ProductCode;
                }
                //else if (WixBundlePackageType.Msp == facade.PackageTuple.Type)
                else if (facade.SpecificPackageTuple is WixBundleMspPackageTuple mspPackage)
                {
                    //var mspPackage = (WixBundleMspPackageTuple)facade.SpecificPackageTuple;
                    key = mspPackage.PatchCode;
                }

                if (!String.IsNullOrEmpty(key) && !this.DependencyTuplesByKey.ContainsKey(key))
                {
                    var dependency = new ProvidesDependencyTuple(facade.PackageTuple.SourceLineNumbers, facade.PackageTuple.Id)
                    {
                        PackageRef = facade.PackageId,
                        Key = key,
                        Version = facade.PackageTuple.Version,
                        DisplayName = facade.PackageTuple.DisplayName
                    };

                    this.Section.Tuples.Add(dependency);

                    this.DependencyTuplesByKey.Add(dependency.Key, dependency);
                }
            }
        }

        private Dictionary<string, ProvidesDependencyTuple> GetDependencyTuplesByKey()
        {
            var dependencyTuplesByKey = new Dictionary<string, ProvidesDependencyTuple>();

            var dependencyTuples = this.Section.Tuples.OfType<ProvidesDependencyTuple>();

            foreach (var dependency in dependencyTuples)
            {
                if (dependencyTuplesByKey.TryGetValue(dependency.Key, out var collision))
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
                    dependencyTuplesByKey.Add(dependency.Key, dependency);
                }
            }

            return dependencyTuplesByKey;
        }
    }
}
