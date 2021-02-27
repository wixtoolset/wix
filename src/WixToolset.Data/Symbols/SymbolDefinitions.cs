// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public enum SymbolDefinitionType
    {
        SummaryInformation,
        ActionText,
        AppId,
        AppSearch,
        BBControl,
        Billboard,
        Binary,
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
        Icon,
        ImageFamilies,
        IniFile,
        IniLocator,
        IsolatedComponent,
        LaunchCondition,
        ListBox,
        ListView,
        LockPermissions,
        Media,
        MIME,
        ModuleComponents,
        ModuleConfiguration,
        ModuleDependency,
        ModuleExclusion,
        ModuleIgnoreTable,
        ModuleSignature,
        ModuleSubstitution,
        MoveFile,
        Assembly,
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
        ProvidesDependency,
        PublishComponent,
        RadioButton,
        Registry,
        RegLocator,
        RemoveFile,
        RemoveRegistry,
        ReserveCost,
        ServiceControl,
        ServiceInstall,
        SFPCatalog,
        Shortcut,
        Signature,
        SoftwareIdentificationTag,
        TargetFilesOptionalData,
        TargetImages,
        TextStyle,
        TypeLib,
        UIText,
        Upgrade,
        UpgradedFilesOptionalData,
        UpgradedFilesToIgnore,
        UpgradedImages,
        Verb,
        WixAction,
        WixApprovedExeForElevation,
        WixBindUpdatedFiles,
        WixBootstrapperApplication,
        WixBootstrapperApplicationDll,
        WixBuildInfo,
        WixBundle,
        WixBundleContainer,
        WixBundleCustomData,
        WixBundleCustomDataAttribute,
        WixBundleCustomDataCell,
        WixBundleExePackage,
        WixBundleExtension,
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
        WixBundleRelatedPackage,
        WixBundleRollbackBoundary,
        WixBundleSlipstreamMsp,
        WixBundleTag,
        WixBundleUpdate,
        WixBundleVariable,
        WixChain,
        WixChainItem,
        WixComplexReference,
        WixComponentGroup,
        WixComponentSearch,
        WixCustomTable,
        WixCustomTableCell,
        WixCustomTableColumn,
        WixDeltaPatchFile,
        WixDeltaPatchSymbolPaths,
        WixDependency,
        WixDependencyRef,
        WixDependencyProvider,
        WixEnsureTable,
        WixFeatureGroup,
        WixFeatureModules,
        WixFileSearch,
        WixFragment,
        WixGroup,
        WixInstanceComponent,
        WixInstanceTransforms,
        WixMediaTemplate,
        WixMerge,
        WixOrdering,
        WixPatchBaseline,
        WixPatchFamilyGroup,
        WixPatchId,
        WixPatchRef,
        WixPatchTarget,
        WixProductSearch,
        WixProductTag,
        WixProperty,
        WixRegistrySearch,
        WixRelatedBundle,
        WixSearch,
        WixSearchRelation,
        WixSetVariable,
        WixSimpleReference,
        WixSuppressAction,
        WixSuppressModularization,
        WixUI,
        WixUpdateRegistration,
        WixVariable,
        MustBeFromAnExtension,
    }

    public static partial class SymbolDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out SymbolDefinitionType type) || type == SymbolDefinitionType.MustBeFromAnExtension)
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(SymbolDefinitionType type)
        {
            switch (type)
            {
                case SymbolDefinitionType.SummaryInformation:
                    return SymbolDefinitions.SummaryInformation;

                case SymbolDefinitionType.ActionText:
                    return SymbolDefinitions.ActionText;

                case SymbolDefinitionType.AppId:
                    return SymbolDefinitions.AppId;

                case SymbolDefinitionType.AppSearch:
                    return SymbolDefinitions.AppSearch;

                case SymbolDefinitionType.BBControl:
                    return SymbolDefinitions.BBControl;

                case SymbolDefinitionType.Billboard:
                    return SymbolDefinitions.Billboard;

                case SymbolDefinitionType.Binary:
                    return SymbolDefinitions.Binary;

                case SymbolDefinitionType.CCPSearch:
                    return SymbolDefinitions.CCPSearch;

                case SymbolDefinitionType.CheckBox:
                    return SymbolDefinitions.CheckBox;

                case SymbolDefinitionType.Class:
                    return SymbolDefinitions.Class;

                case SymbolDefinitionType.ComboBox:
                    return SymbolDefinitions.ComboBox;

                case SymbolDefinitionType.CompLocator:
                    return SymbolDefinitions.CompLocator;

                case SymbolDefinitionType.Complus:
                    return SymbolDefinitions.Complus;

                case SymbolDefinitionType.Component:
                    return SymbolDefinitions.Component;

                case SymbolDefinitionType.Condition:
                    return SymbolDefinitions.Condition;

                case SymbolDefinitionType.Control:
                    return SymbolDefinitions.Control;

                case SymbolDefinitionType.ControlCondition:
                    return SymbolDefinitions.ControlCondition;

                case SymbolDefinitionType.ControlEvent:
                    return SymbolDefinitions.ControlEvent;

                case SymbolDefinitionType.CreateFolder:
                    return SymbolDefinitions.CreateFolder;

                case SymbolDefinitionType.CustomAction:
                    return SymbolDefinitions.CustomAction;

                case SymbolDefinitionType.Dialog:
                    return SymbolDefinitions.Dialog;

                case SymbolDefinitionType.Directory:
                    return SymbolDefinitions.Directory;

                case SymbolDefinitionType.DrLocator:
                    return SymbolDefinitions.DrLocator;

                case SymbolDefinitionType.DuplicateFile:
                    return SymbolDefinitions.DuplicateFile;

                case SymbolDefinitionType.Environment:
                    return SymbolDefinitions.Environment;

                case SymbolDefinitionType.Error:
                    return SymbolDefinitions.Error;

                case SymbolDefinitionType.EventMapping:
                    return SymbolDefinitions.EventMapping;

                case SymbolDefinitionType.Extension:
                    return SymbolDefinitions.Extension;

                case SymbolDefinitionType.ExternalFiles:
                    return SymbolDefinitions.ExternalFiles;

                case SymbolDefinitionType.FamilyFileRanges:
                    return SymbolDefinitions.FamilyFileRanges;

                case SymbolDefinitionType.Feature:
                    return SymbolDefinitions.Feature;

                case SymbolDefinitionType.FeatureComponents:
                    return SymbolDefinitions.FeatureComponents;

                case SymbolDefinitionType.File:
                    return SymbolDefinitions.File;

                case SymbolDefinitionType.FileSFPCatalog:
                    return SymbolDefinitions.FileSFPCatalog;

                case SymbolDefinitionType.Icon:
                    return SymbolDefinitions.Icon;

                case SymbolDefinitionType.ImageFamilies:
                    return SymbolDefinitions.ImageFamilies;

                case SymbolDefinitionType.IniFile:
                    return SymbolDefinitions.IniFile;

                case SymbolDefinitionType.IniLocator:
                    return SymbolDefinitions.IniLocator;

                case SymbolDefinitionType.IsolatedComponent:
                    return SymbolDefinitions.IsolatedComponent;

                case SymbolDefinitionType.LaunchCondition:
                    return SymbolDefinitions.LaunchCondition;

                case SymbolDefinitionType.ListBox:
                    return SymbolDefinitions.ListBox;

                case SymbolDefinitionType.ListView:
                    return SymbolDefinitions.ListView;

                case SymbolDefinitionType.LockPermissions:
                    return SymbolDefinitions.LockPermissions;

                case SymbolDefinitionType.Media:
                    return SymbolDefinitions.Media;

                case SymbolDefinitionType.MIME:
                    return SymbolDefinitions.MIME;

                case SymbolDefinitionType.ModuleComponents:
                    return SymbolDefinitions.ModuleComponents;

                case SymbolDefinitionType.ModuleConfiguration:
                    return SymbolDefinitions.ModuleConfiguration;

                case SymbolDefinitionType.ModuleDependency:
                    return SymbolDefinitions.ModuleDependency;

                case SymbolDefinitionType.ModuleExclusion:
                    return SymbolDefinitions.ModuleExclusion;

                case SymbolDefinitionType.ModuleIgnoreTable:
                    return SymbolDefinitions.ModuleIgnoreTable;

                case SymbolDefinitionType.ModuleSignature:
                    return SymbolDefinitions.ModuleSignature;

                case SymbolDefinitionType.ModuleSubstitution:
                    return SymbolDefinitions.ModuleSubstitution;

                case SymbolDefinitionType.MoveFile:
                    return SymbolDefinitions.MoveFile;

                case SymbolDefinitionType.Assembly:
                    return SymbolDefinitions.Assembly;

                case SymbolDefinitionType.MsiAssemblyName:
                    return SymbolDefinitions.MsiAssemblyName;

                case SymbolDefinitionType.MsiDigitalCertificate:
                    return SymbolDefinitions.MsiDigitalCertificate;

                case SymbolDefinitionType.MsiDigitalSignature:
                    return SymbolDefinitions.MsiDigitalSignature;

                case SymbolDefinitionType.MsiEmbeddedChainer:
                    return SymbolDefinitions.MsiEmbeddedChainer;

                case SymbolDefinitionType.MsiEmbeddedUI:
                    return SymbolDefinitions.MsiEmbeddedUI;

                case SymbolDefinitionType.MsiFileHash:
                    return SymbolDefinitions.MsiFileHash;

                case SymbolDefinitionType.MsiLockPermissionsEx:
                    return SymbolDefinitions.MsiLockPermissionsEx;

                case SymbolDefinitionType.MsiPackageCertificate:
                    return SymbolDefinitions.MsiPackageCertificate;

                case SymbolDefinitionType.MsiPatchCertificate:
                    return SymbolDefinitions.MsiPatchCertificate;

                case SymbolDefinitionType.MsiPatchHeaders:
                    return SymbolDefinitions.MsiPatchHeaders;

                case SymbolDefinitionType.MsiPatchMetadata:
                    return SymbolDefinitions.MsiPatchMetadata;

                case SymbolDefinitionType.MsiPatchOldAssemblyFile:
                    return SymbolDefinitions.MsiPatchOldAssemblyFile;

                case SymbolDefinitionType.MsiPatchOldAssemblyName:
                    return SymbolDefinitions.MsiPatchOldAssemblyName;

                case SymbolDefinitionType.MsiPatchSequence:
                    return SymbolDefinitions.MsiPatchSequence;

                case SymbolDefinitionType.MsiServiceConfig:
                    return SymbolDefinitions.MsiServiceConfig;

                case SymbolDefinitionType.MsiServiceConfigFailureActions:
                    return SymbolDefinitions.MsiServiceConfigFailureActions;

                case SymbolDefinitionType.MsiShortcutProperty:
                    return SymbolDefinitions.MsiShortcutProperty;

                case SymbolDefinitionType.ODBCAttribute:
                    return SymbolDefinitions.ODBCAttribute;

                case SymbolDefinitionType.ODBCDataSource:
                    return SymbolDefinitions.ODBCDataSource;

                case SymbolDefinitionType.ODBCDriver:
                    return SymbolDefinitions.ODBCDriver;

                case SymbolDefinitionType.ODBCSourceAttribute:
                    return SymbolDefinitions.ODBCSourceAttribute;

                case SymbolDefinitionType.ODBCTranslator:
                    return SymbolDefinitions.ODBCTranslator;

                case SymbolDefinitionType.Patch:
                    return SymbolDefinitions.Patch;

                case SymbolDefinitionType.PatchMetadata:
                    return SymbolDefinitions.PatchMetadata;

                case SymbolDefinitionType.PatchPackage:
                    return SymbolDefinitions.PatchPackage;

                case SymbolDefinitionType.PatchSequence:
                    return SymbolDefinitions.PatchSequence;

                case SymbolDefinitionType.ProgId:
                    return SymbolDefinitions.ProgId;

                case SymbolDefinitionType.Properties:
                    return SymbolDefinitions.Properties;

                case SymbolDefinitionType.Property:
                    return SymbolDefinitions.Property;

                case SymbolDefinitionType.PublishComponent:
                    return SymbolDefinitions.PublishComponent;

                case SymbolDefinitionType.ProvidesDependency:
                    return SymbolDefinitions.ProvidesDependency;

                case SymbolDefinitionType.RadioButton:
                    return SymbolDefinitions.RadioButton;

                case SymbolDefinitionType.Registry:
                    return SymbolDefinitions.Registry;

                case SymbolDefinitionType.RegLocator:
                    return SymbolDefinitions.RegLocator;

                case SymbolDefinitionType.RemoveFile:
                    return SymbolDefinitions.RemoveFile;

                case SymbolDefinitionType.RemoveRegistry:
                    return SymbolDefinitions.RemoveRegistry;

                case SymbolDefinitionType.ReserveCost:
                    return SymbolDefinitions.ReserveCost;

                case SymbolDefinitionType.ServiceControl:
                    return SymbolDefinitions.ServiceControl;

                case SymbolDefinitionType.ServiceInstall:
                    return SymbolDefinitions.ServiceInstall;

                case SymbolDefinitionType.SFPCatalog:
                    return SymbolDefinitions.SFPCatalog;

                case SymbolDefinitionType.Shortcut:
                    return SymbolDefinitions.Shortcut;

                case SymbolDefinitionType.Signature:
                    return SymbolDefinitions.Signature;

                case SymbolDefinitionType.SoftwareIdentificationTag:
                    return SymbolDefinitions.SoftwareIdentificationTag;

                case SymbolDefinitionType.TargetFilesOptionalData:
                    return SymbolDefinitions.TargetFilesOptionalData;

                case SymbolDefinitionType.TargetImages:
                    return SymbolDefinitions.TargetImages;

                case SymbolDefinitionType.TextStyle:
                    return SymbolDefinitions.TextStyle;

                case SymbolDefinitionType.TypeLib:
                    return SymbolDefinitions.TypeLib;

                case SymbolDefinitionType.UIText:
                    return SymbolDefinitions.UIText;

                case SymbolDefinitionType.Upgrade:
                    return SymbolDefinitions.Upgrade;

                case SymbolDefinitionType.UpgradedFilesOptionalData:
                    return SymbolDefinitions.UpgradedFilesOptionalData;

                case SymbolDefinitionType.UpgradedFilesToIgnore:
                    return SymbolDefinitions.UpgradedFilesToIgnore;

                case SymbolDefinitionType.UpgradedImages:
                    return SymbolDefinitions.UpgradedImages;

                case SymbolDefinitionType.Verb:
                    return SymbolDefinitions.Verb;

                case SymbolDefinitionType.WixAction:
                    return SymbolDefinitions.WixAction;

                case SymbolDefinitionType.WixApprovedExeForElevation:
                    return SymbolDefinitions.WixApprovedExeForElevation;

                case SymbolDefinitionType.WixBindUpdatedFiles:
                    return SymbolDefinitions.WixBindUpdatedFiles;

                case SymbolDefinitionType.WixBootstrapperApplication:
                    return SymbolDefinitions.WixBootstrapperApplication;

                case SymbolDefinitionType.WixBootstrapperApplicationDll:
                    return SymbolDefinitions.WixBootstrapperApplicationDll;

                case SymbolDefinitionType.WixBuildInfo:
                    return SymbolDefinitions.WixBuildInfo;

                case SymbolDefinitionType.WixBundle:
                    return SymbolDefinitions.WixBundle;

                case SymbolDefinitionType.WixBundleContainer:
                    return SymbolDefinitions.WixBundleContainer;

                case SymbolDefinitionType.WixBundleCustomData:
                    return SymbolDefinitions.WixBundleCustomData;

                case SymbolDefinitionType.WixBundleCustomDataAttribute:
                    return SymbolDefinitions.WixBundleCustomDataAttribute;

                case SymbolDefinitionType.WixBundleCustomDataCell:
                    return SymbolDefinitions.WixBundleCustomDataCell;

                case SymbolDefinitionType.WixBundleExtension:
                    return SymbolDefinitions.WixBundleExtension;

                case SymbolDefinitionType.WixBundleExePackage:
                    return SymbolDefinitions.WixBundleExePackage;

                case SymbolDefinitionType.WixBundleMsiFeature:
                    return SymbolDefinitions.WixBundleMsiFeature;

                case SymbolDefinitionType.WixBundleMsiPackage:
                    return SymbolDefinitions.WixBundleMsiPackage;

                case SymbolDefinitionType.WixBundleMsiProperty:
                    return SymbolDefinitions.WixBundleMsiProperty;

                case SymbolDefinitionType.WixBundleMspPackage:
                    return SymbolDefinitions.WixBundleMspPackage;

                case SymbolDefinitionType.WixBundleMsuPackage:
                    return SymbolDefinitions.WixBundleMsuPackage;

                case SymbolDefinitionType.WixBundlePackage:
                    return SymbolDefinitions.WixBundlePackage;

                case SymbolDefinitionType.WixBundlePackageCommandLine:
                    return SymbolDefinitions.WixBundlePackageCommandLine;

                case SymbolDefinitionType.WixBundlePackageExitCode:
                    return SymbolDefinitions.WixBundlePackageExitCode;

                case SymbolDefinitionType.WixBundlePackageGroup:
                    return SymbolDefinitions.WixBundlePackageGroup;

                case SymbolDefinitionType.WixBundlePatchTargetCode:
                    return SymbolDefinitions.WixBundlePatchTargetCode;

                case SymbolDefinitionType.WixBundlePayload:
                    return SymbolDefinitions.WixBundlePayload;

                case SymbolDefinitionType.WixBundlePayloadGroup:
                    return SymbolDefinitions.WixBundlePayloadGroup;

                case SymbolDefinitionType.WixBundleRelatedPackage:
                    return SymbolDefinitions.WixBundleRelatedPackage;

                case SymbolDefinitionType.WixBundleRollbackBoundary:
                    return SymbolDefinitions.WixBundleRollbackBoundary;

                case SymbolDefinitionType.WixBundleSlipstreamMsp:
                    return SymbolDefinitions.WixBundleSlipstreamMsp;

                case SymbolDefinitionType.WixBundleTag:
                    return SymbolDefinitions.WixBundleTag;

                case SymbolDefinitionType.WixBundleUpdate:
                    return SymbolDefinitions.WixBundleUpdate;

                case SymbolDefinitionType.WixBundleVariable:
                    return SymbolDefinitions.WixBundleVariable;

                case SymbolDefinitionType.WixChain:
                    return SymbolDefinitions.WixChain;

                case SymbolDefinitionType.WixChainItem:
                    return SymbolDefinitions.WixChainItem;

                case SymbolDefinitionType.WixComplexReference:
                    return SymbolDefinitions.WixComplexReference;

                case SymbolDefinitionType.WixComponentGroup:
                    return SymbolDefinitions.WixComponentGroup;

                case SymbolDefinitionType.WixComponentSearch:
                    return SymbolDefinitions.WixComponentSearch;

                case SymbolDefinitionType.WixCustomTable:
                    return SymbolDefinitions.WixCustomTable;

                case SymbolDefinitionType.WixCustomTableCell:
                    return SymbolDefinitions.WixCustomTableCell;

                case SymbolDefinitionType.WixCustomTableColumn:
                    return SymbolDefinitions.WixCustomTableColumn;

                case SymbolDefinitionType.WixDeltaPatchFile:
                    return SymbolDefinitions.WixDeltaPatchFile;

                case SymbolDefinitionType.WixDeltaPatchSymbolPaths:
                    return SymbolDefinitions.WixDeltaPatchSymbolPaths;

                case SymbolDefinitionType.WixDependency:
                    return SymbolDefinitions.WixDependency;

                case SymbolDefinitionType.WixDependencyRef:
                    return SymbolDefinitions.WixDependencyRef;

                case SymbolDefinitionType.WixEnsureTable:
                    return SymbolDefinitions.WixEnsureTable;

                case SymbolDefinitionType.WixFeatureGroup:
                    return SymbolDefinitions.WixFeatureGroup;

                case SymbolDefinitionType.WixFeatureModules:
                    return SymbolDefinitions.WixFeatureModules;

                case SymbolDefinitionType.WixFileSearch:
                    return SymbolDefinitions.WixFileSearch;

                case SymbolDefinitionType.WixFragment:
                    return SymbolDefinitions.WixFragment;

                case SymbolDefinitionType.WixGroup:
                    return SymbolDefinitions.WixGroup;

                case SymbolDefinitionType.WixInstanceComponent:
                    return SymbolDefinitions.WixInstanceComponent;

                case SymbolDefinitionType.WixInstanceTransforms:
                    return SymbolDefinitions.WixInstanceTransforms;

                case SymbolDefinitionType.WixMediaTemplate:
                    return SymbolDefinitions.WixMediaTemplate;

                case SymbolDefinitionType.WixMerge:
                    return SymbolDefinitions.WixMerge;

                case SymbolDefinitionType.WixOrdering:
                    return SymbolDefinitions.WixOrdering;

                case SymbolDefinitionType.WixPatchBaseline:
                    return SymbolDefinitions.WixPatchBaseline;

                case SymbolDefinitionType.WixPatchFamilyGroup:
                    return SymbolDefinitions.WixPatchFamilyGroup;

                case SymbolDefinitionType.WixPatchId:
                    return SymbolDefinitions.WixPatchId;

                case SymbolDefinitionType.WixPatchRef:
                    return SymbolDefinitions.WixPatchRef;

                case SymbolDefinitionType.WixPatchTarget:
                    return SymbolDefinitions.WixPatchTarget;

                case SymbolDefinitionType.WixProductSearch:
                    return SymbolDefinitions.WixProductSearch;

                case SymbolDefinitionType.WixProductTag:
                    return SymbolDefinitions.WixProductTag;

                case SymbolDefinitionType.WixProperty:
                    return SymbolDefinitions.WixProperty;

                case SymbolDefinitionType.WixRegistrySearch:
                    return SymbolDefinitions.WixRegistrySearch;

                case SymbolDefinitionType.WixRelatedBundle:
                    return SymbolDefinitions.WixRelatedBundle;

                case SymbolDefinitionType.WixSearch:
                    return SymbolDefinitions.WixSearch;

                case SymbolDefinitionType.WixSearchRelation:
                    return SymbolDefinitions.WixSearchRelation;

                case SymbolDefinitionType.WixSetVariable:
                    return SymbolDefinitions.WixSetVariable;

                case SymbolDefinitionType.WixSimpleReference:
                    return SymbolDefinitions.WixSimpleReference;

                case SymbolDefinitionType.WixSuppressAction:
                    return SymbolDefinitions.WixSuppressAction;

                case SymbolDefinitionType.WixSuppressModularization:
                    return SymbolDefinitions.WixSuppressModularization;

                case SymbolDefinitionType.WixUI:
                    return SymbolDefinitions.WixUI;

                case SymbolDefinitionType.WixUpdateRegistration:
                    return SymbolDefinitions.WixUpdateRegistration;

                case SymbolDefinitionType.WixVariable:
                    return SymbolDefinitions.WixVariable;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
