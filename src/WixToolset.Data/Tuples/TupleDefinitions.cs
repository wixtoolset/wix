// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using WixToolset.Data.Tuples;

    public enum TupleDefinitionType
    {
        _Streams,
        _SummaryInformation,
        _TransformView,
        _Validation,
        ActionText,
        AdminExecuteSequence,
        AdminUISequence,
        AdvtExecuteSequence,
        AppId,
        AppSearch,
        BBControl,
        Billboard,
        Binary,
        BindImage,
        CCPSearch,
        CheckBox,
        Class,
        ComboBox,
        CompLocator,
        Complus,
        Component,
        Condition,
        Control,
        ControlCondition,
        ControlEvent,
        CreateFolder,
        CustomAction,
        Dialog,
        Directory,
        DrLocator,
        DuplicateFile,
        Environment,
        Error,
        EventMapping,
        Extension,
        ExternalFiles,
        FamilyFileRanges,
        Feature,
        FeatureComponents,
        File,
        FileSFPCatalog,
        Font,
        Icon,
        ImageFamilies,
        IniFile,
        IniLocator,
        InstallExecuteSequence,
        InstallUISequence,
        IsolatedComponent,
        LaunchCondition,
        ListBox,
        ListView,
        LockPermissions,
        Media,
        MIME,
        ModuleAdminExecuteSequence,
        ModuleAdminUISequence,
        ModuleAdvtExecuteSequence,
        ModuleComponents,
        ModuleConfiguration,
        ModuleDependency,
        ModuleExclusion,
        ModuleIgnoreTable,
        ModuleInstallExecuteSequence,
        ModuleInstallUISequence,
        ModuleSignature,
        ModuleSubstitution,
        MoveFile,
        MsiAssembly,
        MsiAssemblyName,
        MsiDigitalCertificate,
        MsiDigitalSignature,
        MsiEmbeddedChainer,
        MsiEmbeddedUI,
        MsiFileHash,
        MsiLockPermissionsEx,
        MsiPackageCertificate,
        MsiPatchCertificate,
        MsiPatchHeaders,
        MsiPatchMetadata,
        MsiPatchOldAssemblyFile,
        MsiPatchOldAssemblyName,
        MsiPatchSequence,
        MsiServiceConfig,
        MsiServiceConfigFailureActions,
        MsiShortcutProperty,
        ODBCAttribute,
        ODBCDataSource,
        ODBCDriver,
        ODBCSourceAttribute,
        ODBCTranslator,
        Patch,
        PatchMetadata,
        PatchPackage,
        PatchSequence,
        ProgId,
        Properties,
        Property,
        PublishComponent,
        RadioButton,
        Registry,
        RegLocator,
        RemoveFile,
        RemoveIniFile,
        RemoveRegistry,
        ReserveCost,
        SelfReg,
        ServiceControl,
        ServiceInstall,
        SFPCatalog,
        Shortcut,
        Signature,
        TargetFiles_OptionalData,
        TargetImages,
        TextStyle,
        TypeLib,
        UIText,
        Upgrade,
        UpgradedFiles_OptionalData,
        UpgradedFilesToIgnore,
        UpgradedImages,
        Verb,
        WixAction,
        WixApprovedExeForElevation,
        WixBindUpdatedFiles,
        WixBootstrapperApplication,
        WixBuildInfo,
        WixBundle,
        WixBundleCatalog,
        WixBundleContainer,
        WixBundleExePackage,
        WixBundleMsiFeature,
        WixBundleMsiPackage,
        WixBundleMsiProperty,
        WixBundleMspPackage,
        WixBundleMsuPackage,
        WixBundlePackage,
        WixBundlePackageCommandLine,
        WixBundlePackageExitCode,
        WixBundlePackageGroup,
        WixBundlePatchTargetCode,
        WixBundlePayload,
        WixBundlePayloadGroup,
        WixBundleProperties,
        WixBundleRelatedPackage,
        WixBundleRollbackBoundary,
        WixBundleSlipstreamMsp,
        WixBundleUpdate,
        WixBundleVariable,
        WixChain,
        WixChainItem,
        WixComplexReference,
        WixComponentGroup,
        WixComponentSearch,
        WixCustomRow,
        WixCustomTable,
        WixDeltaPatchFile,
        WixDeltaPatchSymbolPaths,
        WixDirectory,
        WixEnsureTable,
        WixFeatureGroup,
        WixFeatureModules,
        WixFile,
        WixFileSearch,
        WixFragment,
        WixGroup,
        WixInstanceComponent,
        WixInstanceTransforms,
        WixMedia,
        WixMediaTemplate,
        WixMerge,
        WixOrdering,
        WixPackageFeatureInfo,
        WixPackageProperties,
        WixPatchBaseline,
        WixPatchFamilyGroup,
        WixPatchId,
        WixPatchMetadata,
        WixPatchRef,
        WixPatchTarget,
        WixPayloadProperties,
        WixProductSearch,
        WixProperty,
        WixRegistrySearch,
        WixRelatedBundle,
        WixSearch,
        WixSearchRelation,
        WixSimpleReference,
        WixSuppressAction,
        WixSuppressModularization,
        WixUI,
        WixUpdateRegistration,
        WixVariable,
        MustBeFromAnExtension,
    }

    public static partial class TupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out TupleDefinitionType type) || type == TupleDefinitionType.MustBeFromAnExtension)
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(TupleDefinitionType type)
        {
            switch (type)
            {
                case TupleDefinitionType._Streams:
                    return TupleDefinitions._Streams;

                case TupleDefinitionType._SummaryInformation:
                    return TupleDefinitions._SummaryInformation;

                case TupleDefinitionType._TransformView:
                    return TupleDefinitions._TransformView;

                case TupleDefinitionType._Validation:
                    return TupleDefinitions._Validation;

                case TupleDefinitionType.ActionText:
                    return TupleDefinitions.ActionText;

                case TupleDefinitionType.AdminExecuteSequence:
                    return TupleDefinitions.AdminExecuteSequence;

                case TupleDefinitionType.AdminUISequence:
                    return TupleDefinitions.AdminUISequence;

                case TupleDefinitionType.AdvtExecuteSequence:
                    return TupleDefinitions.AdvtExecuteSequence;

                case TupleDefinitionType.AppId:
                    return TupleDefinitions.AppId;

                case TupleDefinitionType.AppSearch:
                    return TupleDefinitions.AppSearch;

                case TupleDefinitionType.BBControl:
                    return TupleDefinitions.BBControl;

                case TupleDefinitionType.Billboard:
                    return TupleDefinitions.Billboard;

                case TupleDefinitionType.Binary:
                    return TupleDefinitions.Binary;

                case TupleDefinitionType.BindImage:
                    return TupleDefinitions.BindImage;

                case TupleDefinitionType.CCPSearch:
                    return TupleDefinitions.CCPSearch;

                case TupleDefinitionType.CheckBox:
                    return TupleDefinitions.CheckBox;

                case TupleDefinitionType.Class:
                    return TupleDefinitions.Class;

                case TupleDefinitionType.ComboBox:
                    return TupleDefinitions.ComboBox;

                case TupleDefinitionType.CompLocator:
                    return TupleDefinitions.CompLocator;

                case TupleDefinitionType.Complus:
                    return TupleDefinitions.Complus;

                case TupleDefinitionType.Component:
                    return TupleDefinitions.Component;

                case TupleDefinitionType.Condition:
                    return TupleDefinitions.Condition;

                case TupleDefinitionType.Control:
                    return TupleDefinitions.Control;

                case TupleDefinitionType.ControlCondition:
                    return TupleDefinitions.ControlCondition;

                case TupleDefinitionType.ControlEvent:
                    return TupleDefinitions.ControlEvent;

                case TupleDefinitionType.CreateFolder:
                    return TupleDefinitions.CreateFolder;

                case TupleDefinitionType.CustomAction:
                    return TupleDefinitions.CustomAction;

                case TupleDefinitionType.Dialog:
                    return TupleDefinitions.Dialog;

                case TupleDefinitionType.Directory:
                    return TupleDefinitions.Directory;

                case TupleDefinitionType.DrLocator:
                    return TupleDefinitions.DrLocator;

                case TupleDefinitionType.DuplicateFile:
                    return TupleDefinitions.DuplicateFile;

                case TupleDefinitionType.Environment:
                    return TupleDefinitions.Environment;

                case TupleDefinitionType.Error:
                    return TupleDefinitions.Error;

                case TupleDefinitionType.EventMapping:
                    return TupleDefinitions.EventMapping;

                case TupleDefinitionType.Extension:
                    return TupleDefinitions.Extension;

                case TupleDefinitionType.ExternalFiles:
                    return TupleDefinitions.ExternalFiles;

                case TupleDefinitionType.FamilyFileRanges:
                    return TupleDefinitions.FamilyFileRanges;

                case TupleDefinitionType.Feature:
                    return TupleDefinitions.Feature;

                case TupleDefinitionType.FeatureComponents:
                    return TupleDefinitions.FeatureComponents;

                case TupleDefinitionType.File:
                    return TupleDefinitions.File;

                case TupleDefinitionType.FileSFPCatalog:
                    return TupleDefinitions.FileSFPCatalog;

                case TupleDefinitionType.Font:
                    return TupleDefinitions.Font;

                case TupleDefinitionType.Icon:
                    return TupleDefinitions.Icon;

                case TupleDefinitionType.ImageFamilies:
                    return TupleDefinitions.ImageFamilies;

                case TupleDefinitionType.IniFile:
                    return TupleDefinitions.IniFile;

                case TupleDefinitionType.IniLocator:
                    return TupleDefinitions.IniLocator;

                case TupleDefinitionType.InstallExecuteSequence:
                    return TupleDefinitions.InstallExecuteSequence;

                case TupleDefinitionType.InstallUISequence:
                    return TupleDefinitions.InstallUISequence;

                case TupleDefinitionType.IsolatedComponent:
                    return TupleDefinitions.IsolatedComponent;

                case TupleDefinitionType.LaunchCondition:
                    return TupleDefinitions.LaunchCondition;

                case TupleDefinitionType.ListBox:
                    return TupleDefinitions.ListBox;

                case TupleDefinitionType.ListView:
                    return TupleDefinitions.ListView;

                case TupleDefinitionType.LockPermissions:
                    return TupleDefinitions.LockPermissions;

                case TupleDefinitionType.Media:
                    return TupleDefinitions.Media;

                case TupleDefinitionType.MIME:
                    return TupleDefinitions.MIME;

                case TupleDefinitionType.ModuleAdminExecuteSequence:
                    return TupleDefinitions.ModuleAdminExecuteSequence;

                case TupleDefinitionType.ModuleAdminUISequence:
                    return TupleDefinitions.ModuleAdminUISequence;

                case TupleDefinitionType.ModuleAdvtExecuteSequence:
                    return TupleDefinitions.ModuleAdvtExecuteSequence;

                case TupleDefinitionType.ModuleComponents:
                    return TupleDefinitions.ModuleComponents;

                case TupleDefinitionType.ModuleConfiguration:
                    return TupleDefinitions.ModuleConfiguration;

                case TupleDefinitionType.ModuleDependency:
                    return TupleDefinitions.ModuleDependency;

                case TupleDefinitionType.ModuleExclusion:
                    return TupleDefinitions.ModuleExclusion;

                case TupleDefinitionType.ModuleIgnoreTable:
                    return TupleDefinitions.ModuleIgnoreTable;

                case TupleDefinitionType.ModuleInstallExecuteSequence:
                    return TupleDefinitions.ModuleInstallExecuteSequence;

                case TupleDefinitionType.ModuleInstallUISequence:
                    return TupleDefinitions.ModuleInstallUISequence;

                case TupleDefinitionType.ModuleSignature:
                    return TupleDefinitions.ModuleSignature;

                case TupleDefinitionType.ModuleSubstitution:
                    return TupleDefinitions.ModuleSubstitution;

                case TupleDefinitionType.MoveFile:
                    return TupleDefinitions.MoveFile;

                case TupleDefinitionType.MsiAssembly:
                    return TupleDefinitions.MsiAssembly;

                case TupleDefinitionType.MsiAssemblyName:
                    return TupleDefinitions.MsiAssemblyName;

                case TupleDefinitionType.MsiDigitalCertificate:
                    return TupleDefinitions.MsiDigitalCertificate;

                case TupleDefinitionType.MsiDigitalSignature:
                    return TupleDefinitions.MsiDigitalSignature;

                case TupleDefinitionType.MsiEmbeddedChainer:
                    return TupleDefinitions.MsiEmbeddedChainer;

                case TupleDefinitionType.MsiEmbeddedUI:
                    return TupleDefinitions.MsiEmbeddedUI;

                case TupleDefinitionType.MsiFileHash:
                    return TupleDefinitions.MsiFileHash;

                case TupleDefinitionType.MsiLockPermissionsEx:
                    return TupleDefinitions.MsiLockPermissionsEx;

                case TupleDefinitionType.MsiPackageCertificate:
                    return TupleDefinitions.MsiPackageCertificate;

                case TupleDefinitionType.MsiPatchCertificate:
                    return TupleDefinitions.MsiPatchCertificate;

                case TupleDefinitionType.MsiPatchHeaders:
                    return TupleDefinitions.MsiPatchHeaders;

                case TupleDefinitionType.MsiPatchMetadata:
                    return TupleDefinitions.MsiPatchMetadata;

                case TupleDefinitionType.MsiPatchOldAssemblyFile:
                    return TupleDefinitions.MsiPatchOldAssemblyFile;

                case TupleDefinitionType.MsiPatchOldAssemblyName:
                    return TupleDefinitions.MsiPatchOldAssemblyName;

                case TupleDefinitionType.MsiPatchSequence:
                    return TupleDefinitions.MsiPatchSequence;

                case TupleDefinitionType.MsiServiceConfig:
                    return TupleDefinitions.MsiServiceConfig;

                case TupleDefinitionType.MsiServiceConfigFailureActions:
                    return TupleDefinitions.MsiServiceConfigFailureActions;

                case TupleDefinitionType.MsiShortcutProperty:
                    return TupleDefinitions.MsiShortcutProperty;

                case TupleDefinitionType.ODBCAttribute:
                    return TupleDefinitions.ODBCAttribute;

                case TupleDefinitionType.ODBCDataSource:
                    return TupleDefinitions.ODBCDataSource;

                case TupleDefinitionType.ODBCDriver:
                    return TupleDefinitions.ODBCDriver;

                case TupleDefinitionType.ODBCSourceAttribute:
                    return TupleDefinitions.ODBCSourceAttribute;

                case TupleDefinitionType.ODBCTranslator:
                    return TupleDefinitions.ODBCTranslator;

                case TupleDefinitionType.Patch:
                    return TupleDefinitions.Patch;

                case TupleDefinitionType.PatchMetadata:
                    return TupleDefinitions.PatchMetadata;

                case TupleDefinitionType.PatchPackage:
                    return TupleDefinitions.PatchPackage;

                case TupleDefinitionType.PatchSequence:
                    return TupleDefinitions.PatchSequence;

                case TupleDefinitionType.ProgId:
                    return TupleDefinitions.ProgId;

                case TupleDefinitionType.Properties:
                    return TupleDefinitions.Properties;

                case TupleDefinitionType.Property:
                    return TupleDefinitions.Property;

                case TupleDefinitionType.PublishComponent:
                    return TupleDefinitions.PublishComponent;

                case TupleDefinitionType.RadioButton:
                    return TupleDefinitions.RadioButton;

                case TupleDefinitionType.Registry:
                    return TupleDefinitions.Registry;

                case TupleDefinitionType.RegLocator:
                    return TupleDefinitions.RegLocator;

                case TupleDefinitionType.RemoveFile:
                    return TupleDefinitions.RemoveFile;

                case TupleDefinitionType.RemoveIniFile:
                    return TupleDefinitions.RemoveIniFile;

                case TupleDefinitionType.RemoveRegistry:
                    return TupleDefinitions.RemoveRegistry;

                case TupleDefinitionType.ReserveCost:
                    return TupleDefinitions.ReserveCost;

                case TupleDefinitionType.SelfReg:
                    return TupleDefinitions.SelfReg;

                case TupleDefinitionType.ServiceControl:
                    return TupleDefinitions.ServiceControl;

                case TupleDefinitionType.ServiceInstall:
                    return TupleDefinitions.ServiceInstall;

                case TupleDefinitionType.SFPCatalog:
                    return TupleDefinitions.SFPCatalog;

                case TupleDefinitionType.Shortcut:
                    return TupleDefinitions.Shortcut;

                case TupleDefinitionType.Signature:
                    return TupleDefinitions.Signature;

                case TupleDefinitionType.TargetFiles_OptionalData:
                    return TupleDefinitions.TargetFiles_OptionalData;

                case TupleDefinitionType.TargetImages:
                    return TupleDefinitions.TargetImages;

                case TupleDefinitionType.TextStyle:
                    return TupleDefinitions.TextStyle;

                case TupleDefinitionType.TypeLib:
                    return TupleDefinitions.TypeLib;

                case TupleDefinitionType.UIText:
                    return TupleDefinitions.UIText;

                case TupleDefinitionType.Upgrade:
                    return TupleDefinitions.Upgrade;

                case TupleDefinitionType.UpgradedFiles_OptionalData:
                    return TupleDefinitions.UpgradedFiles_OptionalData;

                case TupleDefinitionType.UpgradedFilesToIgnore:
                    return TupleDefinitions.UpgradedFilesToIgnore;

                case TupleDefinitionType.UpgradedImages:
                    return TupleDefinitions.UpgradedImages;

                case TupleDefinitionType.Verb:
                    return TupleDefinitions.Verb;

                case TupleDefinitionType.WixAction:
                    return TupleDefinitions.WixAction;

                case TupleDefinitionType.WixApprovedExeForElevation:
                    return TupleDefinitions.WixApprovedExeForElevation;

                case TupleDefinitionType.WixBindUpdatedFiles:
                    return TupleDefinitions.WixBindUpdatedFiles;

                case TupleDefinitionType.WixBootstrapperApplication:
                    return TupleDefinitions.WixBootstrapperApplication;

                case TupleDefinitionType.WixBuildInfo:
                    return TupleDefinitions.WixBuildInfo;

                case TupleDefinitionType.WixBundle:
                    return TupleDefinitions.WixBundle;

                case TupleDefinitionType.WixBundleCatalog:
                    return TupleDefinitions.WixBundleCatalog;

                case TupleDefinitionType.WixBundleContainer:
                    return TupleDefinitions.WixBundleContainer;

                case TupleDefinitionType.WixBundleExePackage:
                    return TupleDefinitions.WixBundleExePackage;

                case TupleDefinitionType.WixBundleMsiFeature:
                    return TupleDefinitions.WixBundleMsiFeature;

                case TupleDefinitionType.WixBundleMsiPackage:
                    return TupleDefinitions.WixBundleMsiPackage;

                case TupleDefinitionType.WixBundleMsiProperty:
                    return TupleDefinitions.WixBundleMsiProperty;

                case TupleDefinitionType.WixBundleMspPackage:
                    return TupleDefinitions.WixBundleMspPackage;

                case TupleDefinitionType.WixBundleMsuPackage:
                    return TupleDefinitions.WixBundleMsuPackage;

                case TupleDefinitionType.WixBundlePackage:
                    return TupleDefinitions.WixBundlePackage;

                case TupleDefinitionType.WixBundlePackageCommandLine:
                    return TupleDefinitions.WixBundlePackageCommandLine;

                case TupleDefinitionType.WixBundlePackageExitCode:
                    return TupleDefinitions.WixBundlePackageExitCode;

                case TupleDefinitionType.WixBundlePackageGroup:
                    return TupleDefinitions.WixBundlePackageGroup;

                case TupleDefinitionType.WixBundlePatchTargetCode:
                    return TupleDefinitions.WixBundlePatchTargetCode;

                case TupleDefinitionType.WixBundlePayload:
                    return TupleDefinitions.WixBundlePayload;

                case TupleDefinitionType.WixBundlePayloadGroup:
                    return TupleDefinitions.WixBundlePayloadGroup;

                case TupleDefinitionType.WixBundleProperties:
                    return TupleDefinitions.WixBundleProperties;

                case TupleDefinitionType.WixBundleRelatedPackage:
                    return TupleDefinitions.WixBundleRelatedPackage;

                case TupleDefinitionType.WixBundleRollbackBoundary:
                    return TupleDefinitions.WixBundleRollbackBoundary;

                case TupleDefinitionType.WixBundleSlipstreamMsp:
                    return TupleDefinitions.WixBundleSlipstreamMsp;

                case TupleDefinitionType.WixBundleUpdate:
                    return TupleDefinitions.WixBundleUpdate;

                case TupleDefinitionType.WixBundleVariable:
                    return TupleDefinitions.WixBundleVariable;

                case TupleDefinitionType.WixChain:
                    return TupleDefinitions.WixChain;

                case TupleDefinitionType.WixChainItem:
                    return TupleDefinitions.WixChainItem;

                case TupleDefinitionType.WixComplexReference:
                    return TupleDefinitions.WixComplexReference;

                case TupleDefinitionType.WixComponentGroup:
                    return TupleDefinitions.WixComponentGroup;

                case TupleDefinitionType.WixComponentSearch:
                    return TupleDefinitions.WixComponentSearch;

                case TupleDefinitionType.WixCustomRow:
                    return TupleDefinitions.WixCustomRow;

                case TupleDefinitionType.WixCustomTable:
                    return TupleDefinitions.WixCustomTable;

                case TupleDefinitionType.WixDeltaPatchFile:
                    return TupleDefinitions.WixDeltaPatchFile;

                case TupleDefinitionType.WixDeltaPatchSymbolPaths:
                    return TupleDefinitions.WixDeltaPatchSymbolPaths;

                case TupleDefinitionType.WixDirectory:
                    return TupleDefinitions.WixDirectory;

                case TupleDefinitionType.WixEnsureTable:
                    return TupleDefinitions.WixEnsureTable;

                case TupleDefinitionType.WixFeatureGroup:
                    return TupleDefinitions.WixFeatureGroup;

                case TupleDefinitionType.WixFeatureModules:
                    return TupleDefinitions.WixFeatureModules;

                case TupleDefinitionType.WixFile:
                    return TupleDefinitions.WixFile;

                case TupleDefinitionType.WixFileSearch:
                    return TupleDefinitions.WixFileSearch;

                case TupleDefinitionType.WixFragment:
                    return TupleDefinitions.WixFragment;

                case TupleDefinitionType.WixGroup:
                    return TupleDefinitions.WixGroup;

                case TupleDefinitionType.WixInstanceComponent:
                    return TupleDefinitions.WixInstanceComponent;

                case TupleDefinitionType.WixInstanceTransforms:
                    return TupleDefinitions.WixInstanceTransforms;

                case TupleDefinitionType.WixMedia:
                    return TupleDefinitions.WixMedia;

                case TupleDefinitionType.WixMediaTemplate:
                    return TupleDefinitions.WixMediaTemplate;

                case TupleDefinitionType.WixMerge:
                    return TupleDefinitions.WixMerge;

                case TupleDefinitionType.WixOrdering:
                    return TupleDefinitions.WixOrdering;

                case TupleDefinitionType.WixPackageFeatureInfo:
                    return TupleDefinitions.WixPackageFeatureInfo;

                case TupleDefinitionType.WixPackageProperties:
                    return TupleDefinitions.WixPackageProperties;

                case TupleDefinitionType.WixPatchBaseline:
                    return TupleDefinitions.WixPatchBaseline;

                case TupleDefinitionType.WixPatchFamilyGroup:
                    return TupleDefinitions.WixPatchFamilyGroup;

                case TupleDefinitionType.WixPatchId:
                    return TupleDefinitions.WixPatchId;

                case TupleDefinitionType.WixPatchMetadata:
                    return TupleDefinitions.WixPatchMetadata;

                case TupleDefinitionType.WixPatchRef:
                    return TupleDefinitions.WixPatchRef;

                case TupleDefinitionType.WixPatchTarget:
                    return TupleDefinitions.WixPatchTarget;

                case TupleDefinitionType.WixPayloadProperties:
                    return TupleDefinitions.WixPayloadProperties;

                case TupleDefinitionType.WixProductSearch:
                    return TupleDefinitions.WixProductSearch;

                case TupleDefinitionType.WixProperty:
                    return TupleDefinitions.WixProperty;

                case TupleDefinitionType.WixRegistrySearch:
                    return TupleDefinitions.WixRegistrySearch;

                case TupleDefinitionType.WixRelatedBundle:
                    return TupleDefinitions.WixRelatedBundle;

                case TupleDefinitionType.WixSearch:
                    return TupleDefinitions.WixSearch;

                case TupleDefinitionType.WixSearchRelation:
                    return TupleDefinitions.WixSearchRelation;

                case TupleDefinitionType.WixSimpleReference:
                    return TupleDefinitions.WixSimpleReference;

                case TupleDefinitionType.WixSuppressAction:
                    return TupleDefinitions.WixSuppressAction;

                case TupleDefinitionType.WixSuppressModularization:
                    return TupleDefinitions.WixSuppressModularization;

                case TupleDefinitionType.WixUI:
                    return TupleDefinitions.WixUI;

                case TupleDefinitionType.WixUpdateRegistration:
                    return TupleDefinitions.WixUpdateRegistration;

                case TupleDefinitionType.WixVariable:
                    return TupleDefinitions.WixVariable;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
