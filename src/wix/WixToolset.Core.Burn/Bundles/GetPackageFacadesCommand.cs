// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class GetPackageFacadesCommand
    {
        public GetPackageFacadesCommand(IMessaging messaging, IEnumerable<WixBundlePackageSymbol> chainPackageSymbols, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.ChainPackageSymbols = chainPackageSymbols;
            this.Section = section;
        }

        private IEnumerable<WixBundlePackageSymbol> ChainPackageSymbols { get; }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public PackageFacades PackageFacades { get; private set; }

        public void Execute()
        {
            var wixGroupPackagesGroupedById = this.Section.Symbols.OfType<WixGroupSymbol>().Where(g => g.ParentType == ComplexReferenceParentType.Package).ToLookup(g => g.ParentId);
            var bundlePackages = this.Section.Symbols.OfType<WixBundleBundlePackageSymbol>().ToDictionary(t => t.Id.Id);
            var exePackages = this.Section.Symbols.OfType<WixBundleExePackageSymbol>().ToDictionary(t => t.Id.Id);
            var msiPackages = this.Section.Symbols.OfType<WixBundleMsiPackageSymbol>().ToDictionary(t => t.Id.Id);
            var mspPackages = this.Section.Symbols.OfType<WixBundleMspPackageSymbol>().ToDictionary(t => t.Id.Id);
            var msuPackages = this.Section.Symbols.OfType<WixBundleMsuPackageSymbol>().ToDictionary(t => t.Id.Id);
            var bundlePackagePayloads = this.Section.Symbols.OfType<WixBundleBundlePackagePayloadSymbol>().ToDictionary(t => t.Id.Id);
            var exePackagePayloads = this.Section.Symbols.OfType<WixBundleExePackagePayloadSymbol>().ToDictionary(t => t.Id.Id);
            var msiPackagePayloads = this.Section.Symbols.OfType<WixBundleMsiPackagePayloadSymbol>().ToDictionary(t => t.Id.Id);
            var mspPackagePayloads = this.Section.Symbols.OfType<WixBundleMspPackagePayloadSymbol>().ToDictionary(t => t.Id.Id);
            var msuPackagePayloads = this.Section.Symbols.OfType<WixBundleMsuPackagePayloadSymbol>().ToDictionary(t => t.Id.Id);

            var facades = new PackageFacades();

            foreach (var package in this.ChainPackageSymbols)
            {
                var id = package.Id.Id;

                IntermediateSymbol packagePayload = null;
                foreach (var wixGroup in wixGroupPackagesGroupedById[id])
                {
                    if (wixGroup.ChildType == ComplexReferenceChildType.PackagePayload)
                    {
                        IntermediateSymbol tempPackagePayload = null;
                        if (bundlePackagePayloads.TryGetValue(wixGroup.ChildId, out var bundlePackagePayload))
                        {
                            if (package.Type == WixBundlePackageType.Bundle)
                            {
                                tempPackagePayload = bundlePackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported(bundlePackagePayload.SourceLineNumbers, "Bundle"));
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported2(package.SourceLineNumbers));
                            }
                        }
                        else if (exePackagePayloads.TryGetValue(wixGroup.ChildId, out var exePackagePayload))
                        {
                            if (package.Type == WixBundlePackageType.Exe)
                            {
                                tempPackagePayload = exePackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported(exePackagePayload.SourceLineNumbers, "Exe"));
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported2(package.SourceLineNumbers));
                            }
                        }
                        else if (msiPackagePayloads.TryGetValue(wixGroup.ChildId, out var msiPackagePayload))
                        {
                            if (package.Type == WixBundlePackageType.Msi)
                            {
                                tempPackagePayload = msiPackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported(msiPackagePayload.SourceLineNumbers, "Msi"));
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported2(package.SourceLineNumbers));
                            }
                        }
                        else if (mspPackagePayloads.TryGetValue(wixGroup.ChildId, out var mspPackagePayload))
                        {
                            if (package.Type == WixBundlePackageType.Msp)
                            {
                                tempPackagePayload = mspPackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported(mspPackagePayload.SourceLineNumbers, "Msp"));
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported2(package.SourceLineNumbers));
                            }
                        }
                        else if (msuPackagePayloads.TryGetValue(wixGroup.ChildId, out var msuPackagePayload))
                        {
                            if (package.Type == WixBundlePackageType.Msu)
                            {
                                tempPackagePayload = msuPackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported(msuPackagePayload.SourceLineNumbers, "Msu"));
                                this.Messaging.Write(BurnBackendErrors.PackagePayloadUnsupported2(package.SourceLineNumbers));
                            }
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound(package.Type + "PackagePayload", wixGroup.ChildId));
                        }

                        if (tempPackagePayload != null)
                        {
                            if (packagePayload == null)
                            {
                                packagePayload = tempPackagePayload;
                            }
                            else
                            {
                                this.Messaging.Write(BurnBackendErrors.MultiplePackagePayloads(tempPackagePayload.SourceLineNumbers, id, packagePayload.Id.Id, tempPackagePayload.Id.Id));
                                this.Messaging.Write(BurnBackendErrors.MultiplePackagePayloads2(packagePayload.SourceLineNumbers));
                                this.Messaging.Write(BurnBackendErrors.MultiplePackagePayloads3(package.SourceLineNumbers));
                            }
                        }
                    }
                }

                if (packagePayload == null)
                {
                    this.Messaging.Write(BurnBackendErrors.MissingPackagePayload(package.SourceLineNumbers, id, package.Type.ToString()));
                    continue;
                }
                else
                {
                    package.PayloadRef = packagePayload.Id.Id;
                }

                switch (package.Type)
                {
                    case WixBundlePackageType.Bundle:
                        if (bundlePackages.TryGetValue(id, out var bundlePackage))
                        {
                            facades.Add(new PackageFacade(package, bundlePackage, packagePayload));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound("WixBundleBundlePackage", id));
                        }
                        break;

                    case WixBundlePackageType.Exe:
                        if (exePackages.TryGetValue(id, out var exePackage))
                        {
                            facades.Add(new PackageFacade(package, exePackage, packagePayload));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound("WixBundleExePackage", id));
                        }
                        break;

                    case WixBundlePackageType.Msi:
                        if (msiPackages.TryGetValue(id, out var msiPackage))
                        {
                            facades.Add(new PackageFacade(package, msiPackage, packagePayload));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound("WixBundleMsiPackage", id));
                        }
                        break;

                    case WixBundlePackageType.Msp:
                        if (mspPackages.TryGetValue(id, out var mspPackage))
                        {
                            facades.Add(new PackageFacade(package, mspPackage, packagePayload));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound("WixBundleMspPackage", id));
                        }
                        break;

                    case WixBundlePackageType.Msu:
                        if (msuPackages.TryGetValue(id, out var msuPackage))
                        {
                            facades.Add(new PackageFacade(package, msuPackage, packagePayload));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.IdentifierNotFound("WixBundleMsuPackage", id));
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            this.PackageFacades = facades;
        }
    }
}
