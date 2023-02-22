// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class PerformBundleBackendValidationCommand
    {
        public PerformBundleBackendValidationCommand(IMessaging messaging, IBurnBackendHelper burnBackendHelper, IntermediateSection section, PackageFacades packageFacades)
        {
            this.Messaging = messaging;
            this.BackendHelper = burnBackendHelper;
            this.Section = section;
            this.PackageFacades = packageFacades;
        }

        private IMessaging Messaging { get; }

        private IBurnBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private PackageFacades PackageFacades { get; }

        public void Execute()
        {
            foreach (var symbol in this.Section.Symbols)
            {
                if (symbol is WixBundleSymbol wixBundleSymbol)
                {
                    this.ValidateBundle(wixBundleSymbol);
                }
                else if (symbol is WixBundleMsiPropertySymbol wixBundleMsiPropertySymbol)
                {
                    this.ValidateMsiProperty(wixBundleMsiPropertySymbol);
                }
                else if (symbol is WixBundleVariableSymbol wixBundleVariableSymbol)
                {
                    this.ValidateVariable(wixBundleVariableSymbol);
                }
                else if (symbol is WixBundlePackageCommandLineSymbol wixBundlePackageCommandLineSymbol)
                {
                    this.ValidatePackageCommandLine(wixBundlePackageCommandLineSymbol);
                }
                else if (symbol is WixSearchSymbol wixSearchSymbol)
                {
                    this.ValidateSearch(wixSearchSymbol);
                }
            }

            foreach (var packageFacade in this.PackageFacades.Values)
            {
                if (packageFacade.SpecificPackageSymbol is WixBundleBundlePackageSymbol wixBundleBundlePackageSymbol)
                {
                    this.ValidateBundlePackage(wixBundleBundlePackageSymbol, packageFacade.PackageSymbol);
                }
                else if (packageFacade.SpecificPackageSymbol is WixBundleExePackageSymbol wixBundleExePackageSymbol)
                {
                    this.ValidateExePackage(wixBundleExePackageSymbol, packageFacade.PackageSymbol);
                }
                else if (packageFacade.SpecificPackageSymbol is WixBundleMsiPackageSymbol wixBundleMsiPackageSymbol)
                {
                    this.ValidateMsiPackage(wixBundleMsiPackageSymbol, packageFacade.PackageSymbol);
                }
                else if (packageFacade.SpecificPackageSymbol is WixBundleMspPackageSymbol wixBundleMspPackageSymbol)
                {
                    this.ValidateMspPackage(wixBundleMspPackageSymbol, packageFacade.PackageSymbol);
                }
                else if (packageFacade.SpecificPackageSymbol is WixBundleMsuPackageSymbol wixBundleMsuPackageSymbol)
                {
                    this.ValidateMsuPackage(wixBundleMsuPackageSymbol, packageFacade.PackageSymbol);
                }
            }
        }

        private void ValidateBundle(WixBundleSymbol symbol)
        {
            if (symbol.Condition != null)
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "Bundle", "Condition", symbol.Condition, BundleConditionPhase.Startup);
            }
        }

        private void ValidateChainPackage(WixBundlePackageSymbol symbol, string elementName)
        {
            if (!String.IsNullOrEmpty(symbol.InstallCondition))
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, elementName, "InstallCondition", symbol.InstallCondition, BundleConditionPhase.Plan);
            }

            if (!String.IsNullOrEmpty(symbol.RepairCondition))
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, elementName, "RepairCondition", symbol.RepairCondition, BundleConditionPhase.Plan);
            }
        }

        private void ValidateBundlePackage(WixBundleBundlePackageSymbol symbol, WixBundlePackageSymbol packageSymbol)
        {
            this.ValidateChainPackage(packageSymbol, "BundlePackage");
        }

        private void ValidateExePackage(WixBundleExePackageSymbol symbol, WixBundlePackageSymbol packageSymbol)
        {
            this.ValidateChainPackage(packageSymbol, "ExePackage");

            if (!packageSymbol.Permanent)
            {
                if (symbol.DetectionType == WixBundleExePackageDetectionType.Condition)
                {
                    this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "ExePackage", "DetectCondition", symbol.DetectCondition, BundleConditionPhase.Detect);
                }
                else if (symbol.DetectionType == WixBundleExePackageDetectionType.Arp)
                {
                    if (!this.BackendHelper.IsValidWixVersion(symbol.ArpDisplayVersion))
                    {
                        this.Messaging.Write(WarningMessages.InvalidWixVersion(symbol.SourceLineNumbers, symbol.ArpDisplayVersion, "ArpEntry", "Version"));
                    }
                }
            }
        }

        private void ValidateMsiPackage(WixBundleMsiPackageSymbol symbol, WixBundlePackageSymbol packageSymbol)
        {
            this.ValidateChainPackage(packageSymbol, "MsiPackage");
        }

        private void ValidateMsiProperty(WixBundleMsiPropertySymbol symbol)
        {
            // Validate the MSI properties *except* for the ALLUSERS property when set during binding (most likely
            // by the MsiPackage/@ForcePerMachine attribute).
            var nameField = symbol.Fields[(int)WixBundleMsiPropertySymbolFields.Name];
            var name = nameField.AsString();
            if (name != "ALLUSERS" || nameField.Context != "wix.bind")
            {
                this.BackendHelper.ValidateBundleMsiPropertyName(symbol.SourceLineNumbers, "MsiProperty", "Name", name);
            }

            if (symbol.Condition != null)
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "MsiProperty", "Condition", symbol.Condition, BundleConditionPhase.Execute);
            }
        }

        private void ValidateMspPackage(WixBundleMspPackageSymbol symbol, WixBundlePackageSymbol packageSymbol)
        {
            this.ValidateChainPackage(packageSymbol, "MspPackage");
        }

        private void ValidateMsuPackage(WixBundleMsuPackageSymbol symbol, WixBundlePackageSymbol packageSymbol)
        {
            this.ValidateChainPackage(packageSymbol, "MsuPackage");

            if (!packageSymbol.Permanent)
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "MsuPackage", "DetectCondition", symbol.DetectCondition, BundleConditionPhase.Detect);
            }
        }

        private void ValidatePackageCommandLine(WixBundlePackageCommandLineSymbol symbol)
        {
            if (symbol.Condition != null)
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "CommandLine", "Condition", symbol.Condition, BundleConditionPhase.Execute);
            }
        }

        private void ValidateSearch(WixSearchSymbol symbol)
        {
            this.BackendHelper.ValidateBundleVariableNameTarget(symbol.SourceLineNumbers, "*Search", "Variable", symbol.Variable);

            if (symbol.Condition != null)
            {
                this.BackendHelper.ValidateBundleCondition(symbol.SourceLineNumbers, "*Search", "Condition", symbol.Condition, BundleConditionPhase.Detect);
            }
        }

        private void ValidateVariable(WixBundleVariableSymbol symbol)
        {
            this.BackendHelper.ValidateBundleVariableNameDeclaration(symbol.SourceLineNumbers, "Variable", "Name", symbol.Id.Id);
        }
    }
}
