// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundle = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Copyright), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.AboutUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.HelpUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.HelpTelephone), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.UpdateUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Compressed), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.LogPrefix), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.LogExtension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.IconSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.SplashScreenSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Tag), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.Platform), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.ParentName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleSymbolFields.ProviderKey), IntermediateFieldType.String),
            },
            typeof(WixBundleSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleSymbolFields
    {
        UpgradeCode,
        Version,
        Copyright,
        Name,
        Manufacturer,
        Attributes,
        AboutUrl,
        HelpUrl,
        HelpTelephone,
        UpdateUrl,
        Compressed,
        LogPathVariable,
        LogPrefix,
        LogExtension,
        IconSourceFile,
        SplashScreenSourceFile,
        Condition,
        Tag,
        Platform,
        ParentName,
        BundleId,
        ProviderKey,
    }

    [Flags]
    public enum WixBundleAttributes
    {
        None = 0x0,
        DisableModify = 0x1,
        DisableRemove = 0x2,
        SingleChangeUninstallButton = 0x4,
        PerMachine = 0x8,
    }

    public class WixBundleSymbol : IntermediateSymbol
    {
        public WixBundleSymbol() : base(SymbolDefinitions.WixBundle, null, null)
        {
        }

        public WixBundleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleSymbolFields index] => this.Fields[(int)index];

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.UpgradeCode];
            set => this.Set((int)WixBundleSymbolFields.UpgradeCode, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Version];
            set => this.Set((int)WixBundleSymbolFields.Version, value);
        }

        public string Copyright
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Copyright];
            set => this.Set((int)WixBundleSymbolFields.Copyright, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Name];
            set => this.Set((int)WixBundleSymbolFields.Name, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Manufacturer];
            set => this.Set((int)WixBundleSymbolFields.Manufacturer, value);
        }

        public WixBundleAttributes Attributes
        {
            get => (WixBundleAttributes)this.Fields[(int)WixBundleSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleSymbolFields.Attributes, (int)value);
        }

        public string AboutUrl
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.AboutUrl];
            set => this.Set((int)WixBundleSymbolFields.AboutUrl, value);
        }

        public string HelpTelephone
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.HelpTelephone];
            set => this.Set((int)WixBundleSymbolFields.HelpTelephone, value);
        }

        public string HelpUrl
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.HelpUrl];
            set => this.Set((int)WixBundleSymbolFields.HelpUrl, value);
        }

        public string UpdateUrl
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.UpdateUrl];
            set => this.Set((int)WixBundleSymbolFields.UpdateUrl, value);
        }

        public bool? Compressed
        {
            get => (bool?)this.Fields[(int)WixBundleSymbolFields.Compressed];
            set => this.Set((int)WixBundleSymbolFields.Compressed, value);
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.LogPathVariable];
            set => this.Set((int)WixBundleSymbolFields.LogPathVariable, value);
        }

        public string LogPrefix
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.LogPrefix];
            set => this.Set((int)WixBundleSymbolFields.LogPrefix, value);
        }

        public string LogExtension
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.LogExtension];
            set => this.Set((int)WixBundleSymbolFields.LogExtension, value);
        }

        public string IconSourceFile
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.IconSourceFile];
            set => this.Set((int)WixBundleSymbolFields.IconSourceFile, value);
        }

        public string SplashScreenSourceFile
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.SplashScreenSourceFile];
            set => this.Set((int)WixBundleSymbolFields.SplashScreenSourceFile, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Condition];
            set => this.Set((int)WixBundleSymbolFields.Condition, value);
        }

        public string Tag
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.Tag];
            set => this.Set((int)WixBundleSymbolFields.Tag, value);
        }

        public Platform Platform
        {
            get => (Platform)this.Fields[(int)WixBundleSymbolFields.Platform].AsNumber();
            set => this.Set((int)WixBundleSymbolFields.Platform, (int)value);
        }

        public string ParentName
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.ParentName];
            set => this.Set((int)WixBundleSymbolFields.ParentName, value);
        }

        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.BundleId];
            set => this.Set((int)WixBundleSymbolFields.BundleId, value);
        }

        public string ProviderKey
        {
            get => (string)this.Fields[(int)WixBundleSymbolFields.ProviderKey];
            set => this.Set((int)WixBundleSymbolFields.ProviderKey, value);
        }

        public PackagingType DefaultPackagingType => (this.Compressed.HasValue && !this.Compressed.Value) ? PackagingType.External : PackagingType.Embedded;

        public bool DisableModify => (this.Attributes & WixBundleAttributes.DisableModify) == WixBundleAttributes.DisableModify;

        public bool DisableRemove => (this.Attributes & WixBundleAttributes.DisableRemove) == WixBundleAttributes.DisableRemove;

        public bool PerMachine => (this.Attributes & WixBundleAttributes.PerMachine) == WixBundleAttributes.PerMachine;

        public bool SingleChangeUninstallButton => (this.Attributes & WixBundleAttributes.SingleChangeUninstallButton) == WixBundleAttributes.SingleChangeUninstallButton;
    }
}
