// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleExePackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleExePackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.DetectCondition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.InstallCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.RepairCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.UninstallCommand), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.ExeProtocol), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.DetectionType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.ArpId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleExePackageSymbolFields.ArpDisplayVersion), IntermediateFieldType.String),
            },
            typeof(WixBundleExePackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleExePackageSymbolFields
    {
        Attributes,
        DetectCondition,
        InstallCommand,
        RepairCommand,
        UninstallCommand,
        ExeProtocol,
        DetectionType,
        ArpId,
        ArpDisplayVersion,
    }

    /// <summary>
    /// How Burn will detect the ExePackage.
    /// </summary>
    public enum WixBundleExePackageDetectionType
    {
        None,
        Condition,
        Arp,
    }

    [Flags]
    public enum WixBundleExePackageAttributes
    {
        None = 0,
        Bundle = 1,
        ArpWin64 = 2,
        ArpUseUninstallString = 4,
    }

    public class WixBundleExePackageSymbol : IntermediateSymbol
    {
        public WixBundleExePackageSymbol() : base(SymbolDefinitions.WixBundleExePackage, null, null)
        {
        }

        public WixBundleExePackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleExePackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleExePackageSymbolFields index] => this.Fields[(int)index];

        public WixBundleExePackageAttributes Attributes
        {
            get => (WixBundleExePackageAttributes)(int)this.Fields[(int)WixBundleExePackageSymbolFields.Attributes];
            set => this.Set((int)WixBundleExePackageSymbolFields.Attributes, (int)value);
        }

        public string DetectCondition
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.DetectCondition];
            set => this.Set((int)WixBundleExePackageSymbolFields.DetectCondition, value);
        }

        public string InstallCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.InstallCommand];
            set => this.Set((int)WixBundleExePackageSymbolFields.InstallCommand, value);
        }

        public string RepairCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.RepairCommand];
            set => this.Set((int)WixBundleExePackageSymbolFields.RepairCommand, value);
        }

        public string UninstallCommand
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.UninstallCommand];
            set => this.Set((int)WixBundleExePackageSymbolFields.UninstallCommand, value);
        }

        public string ExeProtocol
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.ExeProtocol];
            set => this.Set((int)WixBundleExePackageSymbolFields.ExeProtocol, value);
        }

        public WixBundleExePackageDetectionType DetectionType
        {
            get => (WixBundleExePackageDetectionType)this.Fields[(int)WixBundleExePackageSymbolFields.DetectionType].AsNumber();
            set => this.Set((int)WixBundleExePackageSymbolFields.DetectionType, (int)value);
        }

        public string ArpId
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.ArpId];
            set => this.Set((int)WixBundleExePackageSymbolFields.ArpId, value);
        }

        public string ArpDisplayVersion
        {
            get => (string)this.Fields[(int)WixBundleExePackageSymbolFields.ArpDisplayVersion];
            set => this.Set((int)WixBundleExePackageSymbolFields.ArpDisplayVersion, value);
        }

        public bool IsBundle
        {
            get { return this.Attributes.HasFlag(WixBundleExePackageAttributes.Bundle); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleExePackageAttributes.Bundle;
                }
                else
                {
                    this.Attributes &= ~WixBundleExePackageAttributes.Bundle;
                }
            }
        }

        public bool ArpWin64
        {
            get { return this.Attributes.HasFlag(WixBundleExePackageAttributes.ArpWin64); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleExePackageAttributes.ArpWin64;
                }
                else
                {
                    this.Attributes &= ~WixBundleExePackageAttributes.ArpWin64;
                }
            }
        }

        public bool ArpUseUninstallString
        {
            get { return this.Attributes.HasFlag(WixBundleExePackageAttributes.ArpUseUninstallString); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixBundleExePackageAttributes.ArpUseUninstallString;
                }
                else
                {
                    this.Attributes &= ~WixBundleExePackageAttributes.ArpUseUninstallString;
                }
            }
        }

        public bool Repairable => this.RepairCommand != null;

        public bool Uninstallable => this.UninstallCommand != null;
    }
}
