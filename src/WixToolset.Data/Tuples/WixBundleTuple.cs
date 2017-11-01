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
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Copyright), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.AboutUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.DisableModify), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.DisableRemove), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.DisableRepair), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.HelpTelephone), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.HelpUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.UpdateUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Compressed), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.LogPrefixAndExtension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.IconSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.SplashScreenSourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Tag), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.Platform), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.ParentName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.UpgradeCode), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTupleFields.PerMachine), IntermediateFieldType.Number),
            },
            typeof(WixBundleTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleTupleFields
    {
        Version,
        Copyright,
        Name,
        AboutUrl,
        DisableModify,
        DisableRemove,
        DisableRepair,
        HelpTelephone,
        HelpUrl,
        Manufacturer,
        UpdateUrl,
        Compressed,
        LogPrefixAndExtension,
        IconSourceFile,
        SplashScreenSourceFile,
        Condition,
        Tag,
        Platform,
        ParentName,
        UpgradeCode,
        BundleId,
        ProviderKey,
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

        public string Version
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Version]?.Value;
            set => this.Set((int)WixBundleTupleFields.Version, value);
        }

        public string Copyright
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Copyright]?.Value;
            set => this.Set((int)WixBundleTupleFields.Copyright, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Name]?.Value;
            set => this.Set((int)WixBundleTupleFields.Name, value);
        }

        public string AboutUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.AboutUrl]?.Value;
            set => this.Set((int)WixBundleTupleFields.AboutUrl, value);
        }

        public int DisableModify
        {
            get => (int)this.Fields[(int)WixBundleTupleFields.DisableModify]?.Value;
            set => this.Set((int)WixBundleTupleFields.DisableModify, value);
        }

        public int DisableRemove
        {
            get => (int)this.Fields[(int)WixBundleTupleFields.DisableRemove]?.Value;
            set => this.Set((int)WixBundleTupleFields.DisableRemove, value);
        }

        public int DisableRepair
        {
            get => (int)this.Fields[(int)WixBundleTupleFields.DisableRepair]?.Value;
            set => this.Set((int)WixBundleTupleFields.DisableRepair, value);
        }

        public string HelpTelephone
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.HelpTelephone]?.Value;
            set => this.Set((int)WixBundleTupleFields.HelpTelephone, value);
        }

        public string HelpUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.HelpUrl]?.Value;
            set => this.Set((int)WixBundleTupleFields.HelpUrl, value);
        }

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Manufacturer]?.Value;
            set => this.Set((int)WixBundleTupleFields.Manufacturer, value);
        }

        public string UpdateUrl
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.UpdateUrl]?.Value;
            set => this.Set((int)WixBundleTupleFields.UpdateUrl, value);
        }

        public int Compressed
        {
            get => (int)this.Fields[(int)WixBundleTupleFields.Compressed]?.Value;
            set => this.Set((int)WixBundleTupleFields.Compressed, value);
        }

        public string LogPrefixAndExtension
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.LogPrefixAndExtension]?.Value;
            set => this.Set((int)WixBundleTupleFields.LogPrefixAndExtension, value);
        }

        public string IconSourceFile
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.IconSourceFile]?.Value;
            set => this.Set((int)WixBundleTupleFields.IconSourceFile, value);
        }

        public string SplashScreenSourceFile
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.SplashScreenSourceFile]?.Value;
            set => this.Set((int)WixBundleTupleFields.SplashScreenSourceFile, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Condition]?.Value;
            set => this.Set((int)WixBundleTupleFields.Condition, value);
        }

        public string Tag
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Tag]?.Value;
            set => this.Set((int)WixBundleTupleFields.Tag, value);
        }

        public string Platform
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.Platform]?.Value;
            set => this.Set((int)WixBundleTupleFields.Platform, value);
        }

        public string ParentName
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.ParentName]?.Value;
            set => this.Set((int)WixBundleTupleFields.ParentName, value);
        }

        public string UpgradeCode
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.UpgradeCode]?.Value;
            set => this.Set((int)WixBundleTupleFields.UpgradeCode, value);
        }

        public string BundleId
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.BundleId]?.Value;
            set => this.Set((int)WixBundleTupleFields.BundleId, value);
        }

        public string ProviderKey
        {
            get => (string)this.Fields[(int)WixBundleTupleFields.ProviderKey]?.Value;
            set => this.Set((int)WixBundleTupleFields.ProviderKey, value);
        }

        public int PerMachine
        {
            get => (int)this.Fields[(int)WixBundleTupleFields.PerMachine]?.Value;
            set => this.Set((int)WixBundleTupleFields.PerMachine, value);
        }
    }
}