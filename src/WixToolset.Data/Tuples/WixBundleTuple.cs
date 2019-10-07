// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundle = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Copyright), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.AboutUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.HelpUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.HelpTelephone), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.UpdateUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Compressed), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.LogPathVariable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.LogPrefix), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.LogExtension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.IconSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.SplashScreenSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Tag), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Platform), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.ParentName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.ProviderKey), IntermediateFieldType.String),
            },
            typeof(WixBundleTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleTupleFields
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
        None,
        DisableModify,
        DisableRemove,
        SingleChangeUninstallButton,
        PerMachine,
    }

    public class WixBundleTuple : IntermediateTuple
    {
        public WixBundleTuple() : base(TupleDefinitions.WixBundle, null, null)
        {
        }

        public WixBundleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleTupleFields index] => this.Fields[(int)index];

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.UpgradeCode];
            set => this.Set((int)WixBundleTupleFields.UpgradeCode, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Version];
            set => this.Set((int)WixBundleTupleFields.Version, value);
        }

        public string Copyright
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Copyright];
            set => this.Set((int)WixBundleTupleFields.Copyright, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Name];
            set => this.Set((int)WixBundleTupleFields.Name, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Manufacturer];
            set => this.Set((int)WixBundleTupleFields.Manufacturer, value);
        }

        public WixBundleAttributes Attributes
        {
            get => (WixBundleAttributes)this.Fields[(int)WixBundleTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleTupleFields.Attributes, (int)value);
        }

        public string AboutUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.AboutUrl];
            set => this.Set((int)WixBundleTupleFields.AboutUrl, value);
        }

        public string HelpTelephone
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.HelpTelephone];
            set => this.Set((int)WixBundleTupleFields.HelpTelephone, value);
        }

        public string HelpUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.HelpUrl];
            set => this.Set((int)WixBundleTupleFields.HelpUrl, value);
        }

        public string UpdateUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.UpdateUrl];
            set => this.Set((int)WixBundleTupleFields.UpdateUrl, value);
        }

        public bool? Compressed
        {
            get => (bool?)this.Fields[(int)WixBundleTupleFields.Compressed];
            set => this.Set((int)WixBundleTupleFields.Compressed, value);
        }

        public string LogPathVariable
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.LogPathVariable];
            set => this.Set((int)WixBundleTupleFields.LogPathVariable, value);
        }

        public string LogPrefix
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.LogPrefix];
            set => this.Set((int)WixBundleTupleFields.LogPrefix, value);
        }

        public string LogExtension
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.LogExtension];
            set => this.Set((int)WixBundleTupleFields.LogExtension, value);
        }

        public string IconSourceFile
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.IconSourceFile];
            set => this.Set((int)WixBundleTupleFields.IconSourceFile, value);
        }

        public string SplashScreenSourceFile
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.SplashScreenSourceFile];
            set => this.Set((int)WixBundleTupleFields.SplashScreenSourceFile, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Condition];
            set => this.Set((int)WixBundleTupleFields.Condition, value);
        }

        public string Tag
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Tag];
            set => this.Set((int)WixBundleTupleFields.Tag, value);
        }

        public Platform Platform
        {
            get => (Platform)this.Fields[(int)WixBundleTupleFields.Platform].AsNumber();
            set => this.Set((int)WixBundleTupleFields.Platform, (int)value);
        }

        public string ParentName
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.ParentName];
            set => this.Set((int)WixBundleTupleFields.ParentName, value);
        }

        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.BundleId];
            set => this.Set((int)WixBundleTupleFields.BundleId, value);
        }

        public string ProviderKey
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.ProviderKey];
            set => this.Set((int)WixBundleTupleFields.ProviderKey, value);
        }

        public PackagingType DefaultPackagingType => (this.Compressed.HasValue && !this.Compressed.Value) ? PackagingType.External : PackagingType.Embedded;

        public bool DisableModify => (this.Attributes & WixBundleAttributes.DisableModify) == WixBundleAttributes.DisableModify;

        public bool DisableRemove => (this.Attributes & WixBundleAttributes.DisableRemove) == WixBundleAttributes.DisableRemove;

        public bool PerMachine => (this.Attributes & WixBundleAttributes.PerMachine) == WixBundleAttributes.PerMachine;

        public bool SingleChangeUninstallButton => (this.Attributes & WixBundleAttributes.SingleChangeUninstallButton) == WixBundleAttributes.SingleChangeUninstallButton;
    }
}
