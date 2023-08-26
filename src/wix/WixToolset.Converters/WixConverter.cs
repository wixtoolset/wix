// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// How to convert CustomTable elements.
    /// </summary>
    public enum CustomTableTarget
    {
        /// <summary>
        /// Ambiguous elements will be left alone.
        /// </summary>
        Unknown,

        /// <summary>
        /// Use CustomTable, CustomTableRef, and Unreal.
        /// </summary>
        Msi,

        /// <summary>
        /// Use BundleCustomData and BundleCustomDataRef.
        /// </summary>
        Bundle,
    }

    /// <summary>
    /// WiX source code converter.
    /// </summary>
    public sealed class WixConverter
    {
        private static readonly Regex AddPrefix = new Regex(@"^[^a-zA-Z_]", RegexOptions.Compiled);
        private static readonly Regex IllegalIdentifierCharacters = new Regex(@"[^A-Za-z0-9_\.]|\.{2,}", RegexOptions.Compiled); // non 'words' and assorted valid characters

        private const char XDocumentNewLine = '\n'; // XDocument normalizes "\r\n" to just "\n".
        private static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";
        private static readonly XNamespace Wix3Namespace = "http://schemas.microsoft.com/wix/2006/wi";
        private static readonly XNamespace WixBalNamespace = "http://wixtoolset.org/schemas/v4/wxs/bal";
        private static readonly XNamespace WixDependencyNamespace = "http://wixtoolset.org/schemas/v4/wxs/dependency";
        private static readonly XNamespace WixDirectXNamespace = "http://wixtoolset.org/schemas/v4/wxs/directx";
        private static readonly XNamespace WixFirewallNamespace = "http://wixtoolset.org/schemas/v4/wxs/firewall";
        private static readonly XNamespace WixIisNamespace = "http://wixtoolset.org/schemas/v4/wxs/iis";
        private static readonly XNamespace WixUiNamespace = "http://wixtoolset.org/schemas/v4/wxs/ui";
        private static readonly XNamespace WixUtilNamespace = "http://wixtoolset.org/schemas/v4/wxs/util";
        private static readonly XNamespace WixVSNamespace = "http://wixtoolset.org/schemas/v4/wxs/vs";
        private static readonly XNamespace WxlNamespace = "http://wixtoolset.org/schemas/v4/wxl";
        private static readonly XNamespace Wxl3Namespace = "http://schemas.microsoft.com/wix/2006/localization";

        private static readonly XName AdminExecuteSequenceElementName = WixNamespace + "AdminExecuteSequence";
        private static readonly XName AdminUISequenceSequenceElementName = WixNamespace + "AdminUISequence";
        private static readonly XName AdvertiseExecuteSequenceElementName = WixNamespace + "AdvertiseExecuteSequence";
        private static readonly XName InstallExecuteSequenceElementName = WixNamespace + "InstallExecuteSequence";
        private static readonly XName InstallUISequenceSequenceElementName = WixNamespace + "InstallUISequence";
        private static readonly XName BootstrapperApplicationElementName = WixNamespace + "BootstrapperApplication";
        private static readonly XName BootstrapperApplicationDllElementName = WixNamespace + "BootstrapperApplicationDll";
        private static readonly XName BootstrapperApplicationRefElementName = WixNamespace + "BootstrapperApplicationRef";
        private static readonly XName ApprovedExeForElevationElementName = WixNamespace + "ApprovedExeForElevation";
        private static readonly XName BundleAttributeElementName = WixNamespace + "BundleAttribute";
        private static readonly XName BundleAttributeDefinitionElementName = WixNamespace + "BundleAttributeDefinition";
        private static readonly XName BundleCustomDataElementName = WixNamespace + "BundleCustomData";
        private static readonly XName BundleCustomDataRefElementName = WixNamespace + "BundleCustomDataRef";
        private static readonly XName BundleElementElementName = WixNamespace + "BundleElement";
        private static readonly XName CustomElementName = WixNamespace + "Custom";
        private static readonly XName CustomTableElementName = WixNamespace + "CustomTable";
        private static readonly XName CustomTableRefElementName = WixNamespace + "CustomTableRef";
        private static readonly XName CatalogElementName = WixNamespace + "Catalog";
        private static readonly XName CertificateElementName = WixIisNamespace + "Certificate";
        private static readonly XName ColumnElementName = WixNamespace + "Column";
        private static readonly XName ComponentElementName = WixNamespace + "Component";
        private static readonly XName ControlElementName = WixNamespace + "Control";
        private static readonly XName ConditionElementName = WixNamespace + "Condition";
        private static readonly XName CreateFolderElementName = WixNamespace + "CreateFolder";
        private static readonly XName DataElementName = WixNamespace + "Data";
        private static readonly XName OldProvidesElementName = WixDependencyNamespace + "Provides";
        private static readonly XName OldRequiresElementName = WixDependencyNamespace + "Requires";
        private static readonly XName OldRequiresRefElementName = WixDependencyNamespace + "RequiresRef";
        private static readonly XName DirectoryElementName = WixNamespace + "Directory";
        private static readonly XName DirectoryRefElementName = WixNamespace + "DirectoryRef";
        private static readonly XName EmbeddedChainerElementName = WixNamespace + "EmbeddedChainer";
        private static readonly XName ErrorElementName = WixNamespace + "Error";
        private static readonly XName FeatureElementName = WixNamespace + "Feature";
        private static readonly XName FileElementName = WixNamespace + "File";
        private static readonly XName FragmentElementName = WixNamespace + "Fragment";
        private static readonly XName FirewallRemoteAddressElementName = WixFirewallNamespace + "RemoteAddress";
        private static readonly XName LaunchElementName = WixNamespace + "Launch";
        private static readonly XName LevelElementName = WixNamespace + "Level";
        private static readonly XName ExePackageElementName = WixNamespace + "ExePackage";
        private static readonly XName ExePackagePayloadElementName = WixNamespace + "ExePackagePayload";
        private static readonly XName ModuleElementName = WixNamespace + "Module";
        private static readonly XName MsiPackageElementName = WixNamespace + "MsiPackage";
        private static readonly XName MspPackageElementName = WixNamespace + "MspPackage";
        private static readonly XName MsuPackageElementName = WixNamespace + "MsuPackage";
        private static readonly XName MsuPackagePayloadElementName = WixNamespace + "MsuPackagePayload";
        private static readonly XName PackageElementName = WixNamespace + "Package";
        private static readonly XName PayloadElementName = WixNamespace + "Payload";
        private static readonly XName PermissionExElementName = WixNamespace + "PermissionEx";
        private static readonly XName ProductElementName = WixNamespace + "Product";
        private static readonly XName ProgressTextElementName = WixNamespace + "ProgressText";
        private static readonly XName PropertyRefElementName = WixNamespace + "PropertyRef";
        private static readonly XName PublishElementName = WixNamespace + "Publish";
        private static readonly XName ProvidesElementName = WixNamespace + "Provides";
        private static readonly XName RequiresElementName = WixNamespace + "Requires";
        private static readonly XName RequiresRefElementName = WixNamespace + "RequiresRef";
        private static readonly XName MultiStringValueElementName = WixNamespace + "MultiStringValue";
        private static readonly XName RelatedBundleElementName = WixNamespace + "RelatedBundle";
        private static readonly XName RemotePayloadElementName = WixNamespace + "RemotePayload";
        private static readonly XName RegistryKeyElementName = WixNamespace + "RegistryKey";
        private static readonly XName RegistrySearchElementName = WixNamespace + "RegistrySearch";
        private static readonly XName RequiredPrivilegeElementName = WixNamespace + "RequiredPrivilege";
        private static readonly XName RowElementName = WixNamespace + "Row";
        private static readonly XName ServiceArgumentElementName = WixNamespace + "ServiceArgument";
        private static readonly XName SetDirectoryElementName = WixNamespace + "SetDirectory";
        private static readonly XName SetPropertyElementName = WixNamespace + "SetProperty";
        private static readonly XName ShortcutPropertyElementName = WixNamespace + "ShortcutProperty";
        private static readonly XName SoftwareTagElementName = WixNamespace + "SoftwareTag";
        private static readonly XName SoftwareTagRefElementName = WixNamespace + "SoftwareTagRef";
        private static readonly XName StandardDirectoryElementName = WixNamespace + "StandardDirectory";
        private static readonly XName TagElementName = XNamespace.None + "Tag";
        private static readonly XName TagRefElementName = XNamespace.None + "TagRef";
        private static readonly XName TextElementName = WixNamespace + "Text";
        private static readonly XName UITextElementName = WixNamespace + "UIText";
        private static readonly XName VariableElementName = WixNamespace + "Variable";
        private static readonly XName VerbElementName = WixNamespace + "Verb";
        private static readonly XName BalConditionElementName = WixBalNamespace + "Condition";
        private static readonly XName BalPrereqLicenseUrlAttributeName = WixBalNamespace + "PrereqLicenseUrl";
        private static readonly XName BalPrereqPackageAttributeName = WixBalNamespace + "PrereqPackage";
        private static readonly XName BalUseUILanguagesName = WixBalNamespace + "UseUILanguages";
        private static readonly XName BalStandardBootstrapperApplicationName = WixBalNamespace + "WixStandardBootstrapperApplication";
        private static readonly XName BalManagedBootstrapperApplicationHostName = WixBalNamespace + "WixManagedBootstrapperApplicationHost";
        private static readonly XName BalOldDotNetCoreBootstrapperApplicationName = WixBalNamespace + "WixDotNetCoreBootstrapperApplication";
        private static readonly XName BalNewDotNetCoreBootstrapperApplicationName = WixBalNamespace + "WixDotNetCoreBootstrapperApplicationHost";
        private static readonly XName UtilCloseApplicationElementName = WixUtilNamespace + "CloseApplication";
        private static readonly XName UtilPermissionExElementName = WixUtilNamespace + "PermissionEx";
        private static readonly XName UtilRegistrySearchName = WixUtilNamespace + "RegistrySearch";
        private static readonly XName UtilXmlConfigElementName = WixUtilNamespace + "XmlConfig";
        private static readonly XName CustomActionElementName = WixNamespace + "CustomAction";
        private static readonly XName CustomActionRefElementName = WixNamespace + "CustomActionRef";
        private static readonly XName UIRefElementName = WixNamespace + "UIRef";
        private static readonly XName PropertyElementName = WixNamespace + "Property";
        private static readonly XName Wix4ElementName = WixNamespace + "Wix";
        private static readonly XName Wix3ElementName = Wix3Namespace + "Wix";
        private static readonly XName WixElementWithoutNamespaceName = XNamespace.None + "Wix";
        private static readonly XName WixVariableElementName = WixNamespace + "WixVariable";
        private static readonly XName Include4ElementName = WixNamespace + "Include";
        private static readonly XName Include3ElementName = Wix3Namespace + "Include";
        private static readonly XName IncludeElementWithoutNamespaceName = XNamespace.None + "Include";
        private static readonly XName SummaryInformationElementName = WixNamespace + "SummaryInformation";
        private static readonly XName MediaTemplateElementName = WixNamespace + "MediaTemplate";

        private static readonly XName DependencyCheckAttributeName = WixDependencyNamespace + "Check";
        private static readonly XName DependencyEnforceAttributeName = WixDependencyNamespace + "Enforce";

        private static readonly XName WixLocalization4ElementName = WxlNamespace + "WixLocalization";
        private static readonly XName WixLocalizationStringElementName = WxlNamespace + "String";
        private static readonly XName WixLocalizationUIElementName = WxlNamespace + "UI";
        private static readonly XName WixLocalization3ElementName = Wxl3Namespace + "WixLocalization";
        private static readonly XName WixLocalizationElementWithoutNamespaceName = XNamespace.None + "WixLocalization";

        private static readonly Dictionary<string, XNamespace> OldToNewNamespaceMapping = new Dictionary<string, XNamespace>()
        {
            { "http://schemas.microsoft.com/wix/BalExtension", WixBalNamespace },
            { "http://schemas.microsoft.com/wix/ComPlusExtension", "http://wixtoolset.org/schemas/v4/wxs/complus" },
            { "http://schemas.microsoft.com/wix/DependencyExtension", WixDependencyNamespace },
            { "http://schemas.microsoft.com/wix/DifxAppExtension", "http://wixtoolset.org/schemas/v4/wxs/difxapp" },
            { "http://schemas.microsoft.com/wix/FirewallExtension", WixFirewallNamespace },
            { "http://schemas.microsoft.com/wix/HttpExtension", "http://wixtoolset.org/schemas/v4/wxs/http" },
            { "http://schemas.microsoft.com/wix/IIsExtension", WixIisNamespace },
            { "http://schemas.microsoft.com/wix/MsmqExtension", "http://wixtoolset.org/schemas/v4/wxs/msmq" },
            { "http://schemas.microsoft.com/wix/NetFxExtension", "http://wixtoolset.org/schemas/v4/wxs/netfx" },
            { "http://schemas.microsoft.com/wix/PSExtension", "http://wixtoolset.org/schemas/v4/wxs/powershell" },
            { "http://schemas.microsoft.com/wix/SqlExtension", "http://wixtoolset.org/schemas/v4/wxs/sql" },
            { "http://schemas.microsoft.com/wix/TagExtension", XNamespace.None },
            { "http://schemas.microsoft.com/wix/UtilExtension", WixUtilNamespace },
            { "http://schemas.microsoft.com/wix/VSExtension", WixVSNamespace },
            { "http://wixtoolset.org/schemas/thmutil/2010", "http://wixtoolset.org/schemas/v4/thmutil" },
            { "http://schemas.microsoft.com/wix/2009/Lux", "http://wixtoolset.org/schemas/v4/lux" },
            { "http://schemas.microsoft.com/wix/2006/wi", "http://wixtoolset.org/schemas/v4/wxs" },
            { "http://schemas.microsoft.com/wix/2006/localization", "http://wixtoolset.org/schemas/v4/wxl" },
            { "http://schemas.microsoft.com/wix/2006/libraries", "http://wixtoolset.org/schemas/v4/wixlib" },
            { "http://schemas.microsoft.com/wix/2006/objects", "http://wixtoolset.org/schemas/v4/wixobj" },
            { "http://schemas.microsoft.com/wix/2006/outputs", "http://wixtoolset.org/schemas/v4/wixout" },
            { "http://schemas.microsoft.com/wix/2007/pdbs", "http://wixtoolset.org/schemas/v4/wixpdb" },
            { "http://schemas.microsoft.com/wix/2003/04/actions", "http://wixtoolset.org/schemas/v4/wi/actions" },
            { "http://schemas.microsoft.com/wix/2006/tables", "http://wixtoolset.org/schemas/v4/wi/tables" },
            { "http://schemas.microsoft.com/wix/2006/WixUnit", "http://wixtoolset.org/schemas/v4/wixunit" },
        };

        private static readonly Dictionary<string, string> CustomActionIdsWithPlatformSuffix = new Dictionary<string, string>()
        {
            { "ConfigureComPlusUninstall", "Wix4ConfigureComPlusUninstall_<PlatformSuffix>" },
            { "ConfigureComPlusInstall", "Wix4ConfigureComPlusInstall_<PlatformSuffix>" },
            { "WixDependencyRequire", "Wix4DependencyRequire_<PlatformSuffix>" },
            { "WixDependencyCheck", "Wix4DependencyCheck_<PlatformSuffix>" },
            { "WixQueryDirectXCaps", "Wix4QueryDirectXCaps_<PlatformSuffix>" },
            { "WixSchedFirewallExceptionsUninstall", "Wix5SchedFirewallExceptionsUninstall_<PlatformSuffix>" },
            { "WixSchedFirewallExceptionsInstall", "Wix5SchedFirewallExceptionsInstall_<PlatformSuffix>" },
            { "WixSchedHttpUrlReservationsUninstall", "Wix4SchedHttpUrlReservationsUninstall_<PlatformSuffix>" },
            { "WixSchedHttpUrlReservationsInstall", "Wix4SchedHttpUrlReservationsInstall_<PlatformSuffix>" },
            { "ConfigureIIs", "Wix4ConfigureIIs_<PlatformSuffix>" },
            { "UninstallCertificates", "Wix4UninstallCertificates_<PlatformSuffix>" },
            { "InstallCertificates", "Wix4_<PlatformSuffix>" },
            { "MessageQueuingUninstall", "Wix4MessageQueuingUninstall_<PlatformSuffix>" },
            { "MessageQueuingInstall", "Wix4_MessageQueuingInstall<PlatformSuffix>" },
            { "NetFxScheduleNativeImage", "Wix4NetFxScheduleNativeImage_<PlatformSuffix>" },
            { "NetFxExecuteNativeImageCommitUninstall", "Wix4NetFxExecuteNativeImageCommitUninstall_<PlatformSuffix>" },
            { "NetFxExecuteNativeImageUninstall", "Wix4NetFxExecuteNativeImageUninstall_<PlatformSuffix>" },
            { "NetFxExecuteNativeImageCommitInstall", "Wix4NetFxExecuteNativeImageCommitInstall_<PlatformSuffix>" },
            { "NetFxExecuteNativeImageInstall", "Wix4NetFxExecuteNativeImageInstall_<PlatformSuffix>" },
            { "UninstallSqlData", "Wix4UninstallSqlData_<PlatformSuffix>" },
            { "InstallSqlData", "Wix4InstallSqlData_<PlatformSuffix>" },
            { "WixCheckRebootRequired", "Wix4CheckRebootRequired_<PlatformSuffix>" },
            { "WixCloseApplications", "Wix4CloseApplications_<PlatformSuffix>" },
            { "WixRegisterRestartResources", "Wix4RegisterRestartResources_<PlatformSuffix>" },
            { "ConfigureUsers", "Wix4ConfigureUsers_<PlatformSuffix>" },
            { "ConfigureSmbInstall", "Wix4ConfigureSmbInstall_<PlatformSuffix>" },
            { "ConfigureSmbUninstall", "Wix4ConfigureSmbUninstall_<PlatformSuffix>" },
            { "InstallPerfCounterData", "Wix4InstallPerfCounterData_<PlatformSuffix>" },
            { "UninstallPerfCounterData", "Wix4UninstallPerfCounterData_<PlatformSuffix>" },
            { "ConfigurePerfmonInstall", "Wix4ConfigurePerfmonInstall_<PlatformSuffix>" },
            { "ConfigurePerfmonUninstall", "Wix4ConfigurePerfmonUninstall_<PlatformSuffix>" },
            { "ConfigurePerfmonManifestRegister", "Wix4ConfigurePerfmonManifestRegister_<PlatformSuffix>" },
            { "ConfigurePerfmonManifestUnregister", "Wix4ConfigurePerfmonManifestUnregister_<PlatformSuffix>" },
            { "ConfigureEventManifestRegister", "Wix4ConfigureEventManifestRegister_<PlatformSuffix>" },
            { "ConfigureEventManifestUnregister", "Wix4ConfigureEventManifestUnregister_<PlatformSuffix>" },
            { "SchedServiceConfig", "Wix4SchedServiceConfig_<PlatformSuffix>" },
            { "SchedXmlFile", "Wix4SchedXmlFile_<PlatformSuffix>" },
            { "SchedXmlConfig", "Wix4SchedXmlConfig_<PlatformSuffix>" },
            { "WixSchedInternetShortcuts", "Wix4SchedInternetShortcuts_<PlatformSuffix>" },
            { "WixRollbackInternetShortcuts", "Wix4RollbackInternetShortcuts_<PlatformSuffix>" },
            { "WixCreateInternetShortcuts", "Wix4CreateInternetShortcuts_<PlatformSuffix>" },
            { "WixQueryOsInfo", "Wix4QueryOsInfo_<PlatformSuffix>" },
            { "WixQueryOsDirs", "Wix4QueryOsDirs_<PlatformSuffix>" },
            { "WixQueryOsWellKnownSID", "Wix4QueryOsWellKnownSID_<PlatformSuffix>" },
            { "WixQueryOsDriverInfo", "Wix4QueryOsDriverInfo_<PlatformSuffix>" },
            { "WixQueryNativeMachine", "Wix4QueryNativeMachine_<PlatformSuffix>" },
            { "WixFailWhenDeferred", "Wix4FailWhenDeferred_<PlatformSuffix>" },
            { "WixWaitForEvent", "Wix4WaitForEvent_<PlatformSuffix>" },
            { "WixWaitForEventDeferred", "Wix4WaitForEventDeferred_<PlatformSuffix>" },
            { "WixExitEarlyWithSuccess", "Wix4ExitEarlyWithSuccess_<PlatformSuffix>" },
            { "WixBroadcastSettingChange", "Wix4BroadcastSettingChange_<PlatformSuffix>" },
            { "WixBroadcastEnvironmentChange", "Wix4BroadcastEnvironmentChange_<PlatformSuffix>" },
            { "SchedSecureObjects", "Wix4SchedSecureObjects_<PlatformSuffix>" },
            { "SchedSecureObjectsRollback", "Wix4SchedSecureObjectsRollback_<PlatformSuffix>" },
            { "VSFindInstances", "Wix4VSFindInstances_<PlatformSuffix>" },
        };

        private readonly Dictionary<XName, Action<XElement>> ConvertElementMapping;
        private readonly Regex DeprecatedPrefixRegex = new Regex(@"(?<=(^|[^\$])(\$\$)*)\$(?=\(loc\.[^.].*\))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Instantiate a new Converter class.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
        /// <param name="ignoreErrors">Test errors to ignore.</param>
        /// <param name="customTableTarget">How to convert CustomTable elements.</param>
        public WixConverter(IMessaging messaging, int indentationAmount, IEnumerable<string> errorsAsWarnings = null, IEnumerable<string> ignoreErrors = null, CustomTableTarget customTableTarget = CustomTableTarget.Unknown)
        {
            this.ConvertElementMapping = new Dictionary<XName, Action<XElement>>
            {
                { WixConverter.AdminExecuteSequenceElementName, this.ConvertSequenceElement },
                { WixConverter.AdminUISequenceSequenceElementName, this.ConvertSequenceElement },
                { WixConverter.AdvertiseExecuteSequenceElementName, this.ConvertSequenceElement },
                { WixConverter.InstallUISequenceSequenceElementName, this.ConvertSequenceElement },
                { WixConverter.InstallExecuteSequenceElementName, this.ConvertSequenceElement },
                { WixConverter.BalConditionElementName, this.ConvertBalConditionElement },
                { WixConverter.BootstrapperApplicationElementName, this.ConvertBootstrapperApplicationElement },
                { WixConverter.BootstrapperApplicationRefElementName, this.ConvertBootstrapperApplicationRefElement },
                { WixConverter.ApprovedExeForElevationElementName, this.ConvertApprovedExeForElevationElement },
                { WixConverter.CatalogElementName, this.ConvertCatalogElement },
                { WixConverter.CertificateElementName, this.ConvertCertificateElement },
                { WixConverter.ColumnElementName, this.ConvertColumnElement },
                { WixConverter.ComponentElementName, this.ConvertComponentElement },
                { WixConverter.ControlElementName, this.ConvertControlElement },
                { WixConverter.CustomElementName, this.ConvertCustomElement },
                { WixConverter.CustomActionElementName, this.ConvertCustomActionElement },
                { WixConverter.CustomTableElementName, this.ConvertCustomTableElement },
                { WixConverter.DataElementName, this.ConvertDataElement },
                { WixConverter.DirectoryElementName, this.ConvertDirectoryElement },
                { WixConverter.DirectoryRefElementName, this.ConvertDirectoryRefElement },
                { WixConverter.FeatureElementName, this.ConvertFeatureElement },
                { WixConverter.FileElementName, this.ConvertFileElement },
                { WixConverter.ConditionElementName, this.ConvertLaunchConditionElement },
                { WixConverter.FirewallRemoteAddressElementName, this.ConvertFirewallRemoteAddressElement },
                { WixConverter.EmbeddedChainerElementName, this.ConvertEmbeddedChainerElement },
                { WixConverter.ErrorElementName, this.ConvertErrorElement },
                { WixConverter.ExePackageElementName, this.ConvertExePackageElement },
                { WixConverter.ModuleElementName, this.ConvertModuleElement },
                { WixConverter.MsiPackageElementName, this.ConvertWindowsInstallerPackageElement },
                { WixConverter.MspPackageElementName, this.ConvertWindowsInstallerPackageElement },
                { WixConverter.MsuPackageElementName, this.ConvertMsuPackageElement },
                { WixConverter.OldProvidesElementName, this.ConvertProvidesElement },
                { WixConverter.OldRequiresElementName, this.ConvertRequiresElement },
                { WixConverter.OldRequiresRefElementName, this.ConvertRequiresRefElement },
                { WixConverter.PayloadElementName, this.ConvertSuppressSignatureVerification },
                { WixConverter.PermissionExElementName, this.ConvertPermissionExElement },
                { WixConverter.ProductElementName, this.ConvertProductElement },
                { WixConverter.ProgressTextElementName, this.ConvertProgressTextElement },
                { WixConverter.PropertyRefElementName, this.ConvertPropertyRefElement },
                { WixConverter.PublishElementName, this.ConvertPublishElement },
                { WixConverter.MultiStringValueElementName, this.ConvertMultiStringValueElement },
                { WixConverter.RegistryKeyElementName, this.ConvertRegistryKeyElement },
                { WixConverter.RegistrySearchElementName, this.ConvertRegistrySearchElement },
                { WixConverter.RelatedBundleElementName, this.ConvertRelatedBundleElement },
                { WixConverter.RemotePayloadElementName, this.ConvertRemotePayloadElement },
                { WixConverter.RequiredPrivilegeElementName, this.ConvertRequiredPrivilegeElement },
                { WixConverter.CustomActionRefElementName, this.ConvertCustomActionRefElement },
                { WixConverter.ServiceArgumentElementName, this.ConvertServiceArgumentElement },
                { WixConverter.SetDirectoryElementName, this.ConvertSetDirectoryElement },
                { WixConverter.SetPropertyElementName, this.ConvertSetPropertyElement },
                { WixConverter.ShortcutPropertyElementName, this.ConvertShortcutPropertyElement },
                { WixConverter.TagElementName, this.ConvertTagElement },
                { WixConverter.TagRefElementName, this.ConvertTagRefElement },
                { WixConverter.TextElementName, this.ConvertTextElement },
                { WixConverter.UITextElementName, this.ConvertUITextElement },
                { WixConverter.VariableElementName, this.ConvertVariableElement },
                { WixConverter.UtilCloseApplicationElementName, this.ConvertUtilCloseApplicationElementName },
                { WixConverter.UtilPermissionExElementName, this.ConvertUtilPermissionExElement },
                { WixConverter.UtilRegistrySearchName, this.ConvertUtilRegistrySearchElement },
                { WixConverter.UtilXmlConfigElementName, this.ConvertUtilXmlConfigElement },
                { WixConverter.PropertyElementName, this.ConvertPropertyElement },
                { WixConverter.WixElementWithoutNamespaceName, this.ConvertElementWithoutNamespace },
                { WixConverter.IncludeElementWithoutNamespaceName, this.ConvertElementWithoutNamespace },
                { WixConverter.VerbElementName, this.ConvertVerbElement },
                { WixConverter.UIRefElementName, this.ConvertUIRefElement },
                { WixConverter.WixLocalizationElementWithoutNamespaceName, this.ConvertWixLocalizationElementWithoutNamespace },
                { WixConverter.WixLocalizationStringElementName, this.ConvertWixLocalizationStringElement},
                { WixConverter.WixLocalizationUIElementName, this.ConvertWixLocalizationUIElement},
            };

            this.Messaging = messaging;

            this.IndentationAmount = indentationAmount;

            this.ErrorsAsWarnings = new HashSet<ConverterTestType>(this.YieldConverterTypes(errorsAsWarnings));

            this.IgnoreErrors = new HashSet<ConverterTestType>(this.YieldConverterTypes(ignoreErrors));

            this.CustomTableSetting = customTableTarget;
        }

        private CustomTableTarget CustomTableSetting { get; }

        private List<Message> ConversionMessages
        {
            get { return this.State.ConversionMessages; }
        }

        private ConversionState State { get; set; }

        private HashSet<ConverterTestType> ErrorsAsWarnings { get; set; }

        private HashSet<ConverterTestType> IgnoreErrors { get; set; }

        private IMessaging Messaging { get; }

        private int IndentationAmount { get; set; }

        private ConvertOperation Operation
        {
            get { return this.State.Operation; }
        }

        private string SourceFile
        {
            get { return this.State.SourceFile; }
        }

        private int SourceVersion
        {
            get { return this.State.SourceVersion; }
            set { this.State.SourceVersion = value; }
        }

        private XElement XRoot
        {
            get { return this.State.XDocument.Root; }
        }

        /// <summary>
        /// Convert a file.
        /// </summary>
        /// <param name="sourceFile">The file to convert.</param>
        /// <param name="saveConvertedFile">Option to save the converted Messages that are found.</param>
        /// <returns>The number of conversions found.</returns>
        public int ConvertFile(string sourceFile, bool saveConvertedFile)
        {
            var savedDocument = false;

            if (this.TryOpenSourceFile(ConvertOperation.Convert, sourceFile))
            {
                this.DoIt(this.State.XDocument);

                // Fix Messages if requested and necessary.
                if (saveConvertedFile && 0 < this.ConversionMessages.Count)
                {
                    savedDocument = this.SaveDocument(this.State.XDocument);
                }
            }

            return this.ReportMessages(this.State.XDocument, savedDocument);
        }

        /// <summary>
        /// Convert a document.
        /// </summary>
        /// <param name="document">The document to convert.</param>
        /// <param name="sourceFile">The file that the document was loaded from.</param>
        /// <returns>The number of conversions found.</returns>
        public int ConvertDocument(XDocument document, string sourceFile = "InMemoryXml")
        {
            this.State = new ConversionState(ConvertOperation.Convert, sourceFile);
            this.State.Initialize(document);
            this.DoIt(document);

            return this.ReportMessages(document, false);
        }

        /// <summary>
        /// Format a file.
        /// </summary>
        /// <param name="sourceFile">The file to format.</param>
        /// <param name="saveConvertedFile">Option to save the format Messages that are found.</param>
        /// <returns>The number of Messages found.</returns>
        public int FormatFile(string sourceFile, bool saveConvertedFile)
        {
            var savedDocument = false;

            if (this.TryOpenSourceFile(ConvertOperation.Format, sourceFile))
            {
                this.FormatDocument(this.State.XDocument);

                // Fix Messages if requested and necessary.
                if (saveConvertedFile && 0 < this.ConversionMessages.Count)
                {
                    savedDocument = this.SaveDocument(this.State.XDocument);
                }
            }

            return this.ReportMessages(this.State.XDocument, savedDocument);
        }

        /// <summary>
        /// Format a document.
        /// </summary>
        /// <param name="document">The document to format.</param>
        /// <param name="sourceFile">The file that the document was loaded from.</param>
        /// <returns>The number of Messages found.</returns>
        public int FormatDocument(XDocument document, string sourceFile = "InMemoryXml")
        {
            this.State = new ConversionState(ConvertOperation.Format, sourceFile);
            this.State.Initialize(document);
            this.DoIt(document);

            return this.ReportMessages(document, false);
        }

        private void DoIt(XDocument document)
        {
            // Remove the declaration.
            if (null != document.Declaration
                && this.OnInformation(ConverterTestType.DeclarationPresent, document, "This file contains an XML declaration on the first line."))
            {
                document.Declaration = null;
                TrimLeadingText(document);
            }

            // Start converting the nodes at the top.
            this.ConvertNodes(document.Nodes(), 0);
            this.ConvertMbaPrereqVariables();
            this.RemoveUnusedNamespaces(document.Root);
            this.MoveNamespacesToRoot(document.Root);
        }

        private bool TryOpenSourceFile(ConvertOperation operation, string sourceFile)
        {
            this.State = new ConversionState(operation, sourceFile);

            try
            {
                this.State.Initialize();
                return true;
            }
            catch (XmlException e)
            {
                this.OnError(ConverterTestType.XmlException, null, "The xml is invalid. Detail: '{0}'", e.Message);
                return false;
            }
        }

        private bool SaveDocument(XDocument document)
        {
            var ignoreDeclarationError = this.IgnoreErrors.Contains(ConverterTestType.DeclarationPresent);

            try
            {
                using (var writer = XmlWriter.Create(this.SourceFile, new XmlWriterSettings { OmitXmlDeclaration = !ignoreDeclarationError }))
                {
                    document.Save(writer);
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                this.OnError(ConverterTestType.UnauthorizedAccessException, null, "Could not write to file.");
            }

            return false;
        }

        private void ConvertNodes(IEnumerable<XNode> nodes, int level)
        {
            // Note we operate on a copy of the node list since we may
            // remove some whitespace nodes during this processing.
            foreach (var node in nodes.ToList())
            {
                if (node is XText text)
                {
                    if (null != text.Value)
                    {
                        if (this.TryFixDeprecatedLocalizationPrefixes(node, text.Value, out var newValue, ConverterTestType.DeprecatedLocalizationVariablePrefixInTextValue))
                        {
                            text.Value = newValue;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(text.Value))
                    {
                        text.Value = text.Value.Trim();
                    }
                    else if (node.NextNode is XCData)
                    {
                        this.EnsurePrecedingWhitespaceRemoved(text, node, ConverterTestType.WhitespacePrecedingNodeWrong);
                    }
                    else if (node.NextNode is XElement)
                    {
                        this.EnsurePrecedingWhitespaceCorrect(text, node, level, ConverterTestType.WhitespacePrecedingNodeWrong);
                    }
                    else if (node.NextNode is null) // this is the space before the close element
                    {
                        if (node.PreviousNode is null || node.PreviousNode is XCData)
                        {
                            this.EnsurePrecedingWhitespaceRemoved(text, node.Parent, ConverterTestType.WhitespacePrecedingEndElementWrong);
                        }
                        else if (level == 0) // root element's close tag
                        {
                            this.EnsurePrecedingWhitespaceCorrect(text, node, 0, ConverterTestType.WhitespacePrecedingEndElementWrong);
                        }
                        else
                        {
                            this.EnsurePrecedingWhitespaceCorrect(text, node, level - 1, ConverterTestType.WhitespacePrecedingEndElementWrong);
                        }
                    }
                }
                else if (node is XElement element)
                {
                    this.ConvertElement(element);
                    var before = element.Nodes().ToList();
                    this.ConvertNodes(before, level + 1);

                    // If any nodes were added during the processing of the children,
                    // ensure those added children get processed as well.
                    var added = element.Nodes().Except(before).ToList();

                    if (added.Any())
                    {
                        this.ConvertNodes(added, level + 1);
                    }
                }
            }
        }

        private bool TryFixDeprecatedLocalizationPrefixes(XNode node, string value, out string newValue, ConverterTestType testType)
        {
            newValue = this.DeprecatedPrefixRegex.Replace(value, "!");

            if (Object.ReferenceEquals(newValue, value))
            {
                return false;
            }

            var message = testType == ConverterTestType.DeprecatedLocalizationVariablePrefixInTextValue ? "The prefix on the localization variable in the inner text is incorrect." : "The prefix on the localization variable in the attribute value is incorrect.";

            return this.OnInformation(testType, node, message);
        }

        private void EnsurePrecedingWhitespaceCorrect(XText whitespace, XNode node, int level, ConverterTestType testType)
        {
            if (!WixConverter.LeadingWhitespaceValid(this.IndentationAmount, level, whitespace.Value))
            {
                var message = testType == ConverterTestType.WhitespacePrecedingEndElementWrong ? "The whitespace preceding this end element is incorrect." : "The whitespace preceding this comment is incorrect.";

                if (this.OnInformation(testType, node, message))
                {
                    WixConverter.FixupWhitespace(this.IndentationAmount, level, whitespace);
                }
            }
        }

        private void EnsurePrecedingWhitespaceRemoved(XText whitespace, XNode node, ConverterTestType testType)
        {
            if (!String.IsNullOrEmpty(whitespace.Value) && whitespace.NodeType != XmlNodeType.CDATA)
            {
                var message = testType == ConverterTestType.WhitespacePrecedingEndElementWrong ? "The whitespace preceding this end element is incorrect." : "The whitespace preceding this comment is incorrect.";

                if (this.OnInformation(testType, node, message))
                {
                    whitespace.Remove();
                }
            }
        }

        private void ConvertElement(XElement element)
        {
            var deprecatedToUpdatedNamespaces = new Dictionary<XNamespace, XNamespace>();

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    // Gather any deprecated namespaces, then update this element tree based on those deprecations.
                    var declaration = attribute;

                    if (element.Name == Wix3ElementName || element.Name == Include3ElementName || element.Name == WixLocalization3ElementName)
                    {
                        this.SourceVersion = 3;
                    }
                    else if (element.Name == Wix4ElementName || element.Name == Include4ElementName || element.Name == WixLocalization4ElementName)
                    {
                        this.SourceVersion = 4;
                    }

                    if (WixConverter.OldToNewNamespaceMapping.TryGetValue(declaration.Value, out var ns))
                    {
                        if (this.OnInformation(ConverterTestType.XmlnsValueWrong, declaration, "The namespace '{0}' is out of date. It must be '{1}'.", declaration.Value, ns.NamespaceName))
                        {
                            deprecatedToUpdatedNamespaces.Add(declaration.Value, ns);
                        }
                    }
                }
                else
                {
                    if (null != attribute.Value)
                    {
                        if (this.TryFixDeprecatedLocalizationPrefixes(element, attribute.Value, out var newValue, ConverterTestType.DeprecatedLocalizationVariablePrefixInAttributeValue))
                        {
                            attribute.Value = newValue;
                        }
                    }
                }
            }

            if (deprecatedToUpdatedNamespaces.Any())
            {
                WixConverter.UpdateElementsWithDeprecatedNamespaces(element.DescendantsAndSelf(), deprecatedToUpdatedNamespaces);
            }

            // Apply any specialized conversion actions.
            if (this.ConvertElementMapping.TryGetValue(element.Name, out var convert))
            {
                convert(element);
            }
        }

        private void ConvertBalConditionElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Condition");
        }

        private void ConvertBootstrapperApplicationElement(XElement element)
        {
            var xUseUILanguages = element.Attribute(BalUseUILanguagesName);
            if (xUseUILanguages != null &&
                this.OnInformation(ConverterTestType.BalUseUILanguagesDeprecated, element, "bal:UseUILanguages is deprecated, 'true' is now the standard behavior."))
            {
                xUseUILanguages.Remove();
            }

            var xBADll = element.Elements(BootstrapperApplicationDllElementName).FirstOrDefault();
            if (xBADll == null)
            {
                xBADll = this.CreateBootstrapperApplicationDllElement(element);

                if (xBADll != null)
                {
                    element.Add(Environment.NewLine);
                    element.Add(xBADll);
                    element.Add(Environment.NewLine);
                }
            }
        }

        private XElement CreateBootstrapperApplicationDllElement(XElement element)
        {
            XElement xBADll = null;
            var xSource = element.Attribute("SourceFile");
            var xDpiAwareness = element.Attribute("DpiAwareness");

            if (xSource != null)
            {
                if (xBADll != null || CreateBADllElement(element, out xBADll))
                {
                    MoveAttribute(element, "SourceFile", xBADll);
                    MoveAttribute(element, "Name", xBADll);
                }
            }
            else if (xDpiAwareness != null || this.SourceVersion < 4) // older code might be relying on old behavior of first Payload element being the BA dll.
            {
                var xFirstChild = element.Elements().FirstOrDefault();
                if (xFirstChild?.Name == PayloadElementName)
                {
                    if (xBADll != null || CreateBADllElement(element, out xBADll))
                    {
                        var attributes = xFirstChild.Attributes().ToList();
                        xFirstChild.Remove();

                        foreach (var attribute in attributes)
                        {
                            xBADll.Add(attribute);
                        }
                    }
                }
                else
                {
                    this.OnError(ConverterTestType.BootstrapperApplicationDllRequired, element, "The new BootstrapperApplicationDll element is required but could not be added automatically since the bootstrapper application dll was not directly specified. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
                }
            }

            if (xDpiAwareness != null)
            {
                if (xBADll != null || CreateBADllElement(element, out xBADll))
                {
                    MoveAttribute(element, "DpiAwareness", xBADll);
                }
            }
            else if (this.SourceVersion < 4 && xBADll != null &&
                this.OnInformation(ConverterTestType.AssignBootstrapperApplicationDpiAwareness, element, "The BootstrapperApplicationDll DpiAwareness attribute is being set to 'unaware' to ensure it remains the same as the v3 default"))
            {
                xBADll.Add(new XAttribute("DpiAwareness", "unaware"));
            }

            return xBADll;

            bool CreateBADllElement(XObject node, out XElement xCreatedBADll)
            {
                var create = this.OnInformation(ConverterTestType.BootstrapperApplicationDll, node, "The bootstrapper application dll is now specified in the BootstrapperApplicationDll element.");
                xCreatedBADll = create ? new XElement(BootstrapperApplicationDllElementName) : null;
                return create;
            }
        }

        private void ConvertBootstrapperApplicationRefElement(XElement element)
        {
            var xUseUILanguages = element.Attribute(BalUseUILanguagesName);
            if (xUseUILanguages != null &&
                this.OnInformation(ConverterTestType.BalUseUILanguagesDeprecated, element, "bal:UseUILanguages is deprecated, 'true' is now the standard behavior."))
            {
                xUseUILanguages.Remove();
            }

            var xId = element.Attribute("Id");
            if (xId != null)
            {
                XName balBAName = null;
                XName oldBalBAName = null;
                string theme = null;

                switch (xId.Value)
                {
                    case "WixStandardBootstrapperApplication.RtfLicense":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "rtfLicense";
                        break;
                    case "WixStandardBootstrapperApplication.RtfLargeLicense":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "rtfLargeLicense";
                        break;
                    case "WixStandardBootstrapperApplication.HyperlinkLicense":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "hyperlinkLicense";
                        break;
                    case "WixStandardBootstrapperApplication.HyperlinkLargeLicense":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "hyperlinkLargeLicense";
                        break;
                    case "WixStandardBootstrapperApplication.HyperlinkSidebarLicense":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "hyperlinkSidebarLicense";
                        break;
                    case "WixStandardBootstrapperApplication.Foundation":
                        balBAName = BalStandardBootstrapperApplicationName;
                        theme = "none";
                        break;
                    case "ManagedBootstrapperApplicationHost":
                    case "ManagedBootstrapperApplicationHost.RtfLicense":
                        balBAName = BalManagedBootstrapperApplicationHostName;
                        theme = "standard";
                        break;
                    case "ManagedBootstrapperApplicationHost.Minimal":
                    case "ManagedBootstrapperApplicationHost.RtfLicense.Minimal":
                    case "ManagedBootstrapperApplicationHost.Foundation":
                        balBAName = BalManagedBootstrapperApplicationHostName;
                        theme = "none";
                        break;
                    case "DotNetCoreBootstrapperApplicationHost":
                    case "DotNetCoreBootstrapperApplicationHost.RtfLicense":
                        balBAName = BalNewDotNetCoreBootstrapperApplicationName;
                        oldBalBAName = BalOldDotNetCoreBootstrapperApplicationName;
                        theme = "standard";
                        break;
                    case "DotNetCoreBootstrapperApplicationHost.Minimal":
                    case "DotNetCoreBootstrapperApplicationHost.RtfLicense.Minimal":
                    case "DotNetCoreBootstrapperApplicationHost.Foundation":
                        balBAName = BalNewDotNetCoreBootstrapperApplicationName;
                        oldBalBAName = BalOldDotNetCoreBootstrapperApplicationName;
                        theme = "none";
                        break;
                }

                if (balBAName != null && theme != null &&
                    this.OnInformation(ConverterTestType.BalBootstrapperApplicationRefToElement, element, "Built-in bootstrapper applications must be referenced through their custom element"))
                {
                    element.Name = BootstrapperApplicationElementName;
                    xId.Remove();
                    this.ConvertBalBootstrapperApplicationRef(element, theme, balBAName, oldBalBAName);
                }
            }
        }

        private void ConvertApprovedExeForElevationElement(XElement element)
        {
            this.RenameWin64ToBitness(element);
        }

        private void ConvertBalBootstrapperApplicationRef(XElement element, string theme, XName balBAElementName, XName oldBalBAElementName = null)
        {
            var xBalBa = element.Element(oldBalBAElementName ?? balBAElementName);
            if (xBalBa == null)
            {
                xBalBa = new XElement(balBAElementName);
                element.Add(Environment.NewLine);
                element.Add(xBalBa);
                element.Add(Environment.NewLine);
            }
            else if (oldBalBAElementName != null)
            {
                xBalBa.Name = BalNewDotNetCoreBootstrapperApplicationName;
            }

            if (theme != "standard")
            {
                xBalBa.Add(new XAttribute("Theme", theme));
            }
        }

        private void ConvertCatalogElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.SuppressSignatureVerificationObsolete, element, "The Catalog element is obsolete. The element will be removed."))
            {
                element.Remove();
            }
        }


        private void ConvertCertificateElement(XElement xCertificate)
        {
            var xBinaryKey = xCertificate.Attribute("BinaryKey");
            if (xBinaryKey != null && this.OnInformation(ConverterTestType.CertificateBinaryKeyIsNowBinaryRef, xCertificate, "The Certificate BinaryKey element has been renamed to BinaryRef."))
            {
                xCertificate.SetAttributeValue("BinaryRef", xBinaryKey.Value);
                xBinaryKey.Remove();
            }
        }

        private void ConvertColumnElement(XElement element)
        {
            var category = element.Attribute("Category");
            if (category != null)
            {
                var camelCaseValue = LowercaseFirstChar(category.Value);
                if (category.Value != camelCaseValue &&
                    this.OnInformation(ConverterTestType.ColumnCategoryCamelCase, element, "The CustomTable Category attribute contains an incorrectly cased '{0}' value. Lowercase the first character instead.", category.Name))
                {
                    category.Value = camelCaseValue;
                }
            }

            var modularization = element.Attribute("Modularize");
            if (modularization != null)
            {
                var camelCaseValue = LowercaseFirstChar(modularization.Value);
                if (modularization.Value != camelCaseValue &&
                    this.OnInformation(ConverterTestType.ColumnModularizeCamelCase, element, "The CustomTable Modularize attribute contains an incorrectly cased '{0}' value. Lowercase the first character instead.", modularization.Name))
                {
                    modularization.Value = camelCaseValue;
                }
            }
        }

        private void ConvertCustomElement(XElement element)
        {
            var actionId = element.Attribute("Action")?.Value;

            if (actionId != null
                && CustomActionIdsWithPlatformSuffix.TryGetValue(actionId, out var replacementId))
            {
                this.OnError(ConverterTestType.CustomActionIdsIncludePlatformSuffix, element,
                    $"Custom action ids have changed in WiX v4 extensions to support platform-specific custom actions. The platform is applied as a suffix: _X86, _X64, _A64 (Arm64). When manually rescheduling custom action '{actionId}', you must use the new custom action id '{replacementId}'. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages");
            }
        }

        private void ConvertCustomTableElement(XElement element)
        {
            var bootstrapperApplicationData = element.Attribute("BootstrapperApplicationData");
            if (bootstrapperApplicationData?.Value == "no")
            {
                if (this.OnInformation(ConverterTestType.BootstrapperApplicationDataDeprecated, element, "The CustomTable element contains deprecated '{0}' attribute. Use the 'Unreal' attribute instead.", bootstrapperApplicationData.Name))
                {
                    bootstrapperApplicationData.Remove();
                }
            }
            else
            {
                if (element.Elements(ColumnElementName).Any() || bootstrapperApplicationData != null)
                {
                    // Table definition
                    if (bootstrapperApplicationData != null)
                    {
                        switch (this.CustomTableSetting)
                        {
                            case CustomTableTarget.Bundle:
                                if (this.OnInformation(ConverterTestType.BootstrapperApplicationDataDeprecated, element, "The CustomTable element contains deprecated '{0}' attribute. Use the 'BundleCustomData' element for Bundles.", bootstrapperApplicationData.Name))
                                {
                                    element.Name = WixConverter.BundleCustomDataElementName;
                                    bootstrapperApplicationData.Remove();
                                    this.ConvertCustomTableElementToBundle(element);
                                }
                                break;
                            case CustomTableTarget.Msi:
                                if (this.OnInformation(ConverterTestType.BootstrapperApplicationDataDeprecated, element, "The CustomTable element contains deprecated '{0}' attribute. Use the 'Unreal' attribute instead.", bootstrapperApplicationData.Name))
                                {
                                    element.Add(new XAttribute("Unreal", bootstrapperApplicationData.Value));
                                    bootstrapperApplicationData.Remove();
                                }
                                break;
                            default:
                                this.OnError(ConverterTestType.CustomTableNotAlwaysConvertable, element, "The CustomTable element contains deprecated '{0}' attribute so can't be converted. Use the 'Unreal' attribute for MSI. Use the 'BundleCustomData' element for Bundles. Use the --custom-table argument to force conversion to 'msi' or 'bundle'. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles", bootstrapperApplicationData.Name);
                                break;
                        }
                    }
                }
                else
                {
                    // Table ref
                    switch (this.CustomTableSetting)
                    {
                        case CustomTableTarget.Bundle:
                            if (this.OnInformation(ConverterTestType.CustomTableRef, element, "CustomTable elements that don't contain the table definition are now BundleCustomDataRef for Bundles."))
                            {
                                element.Name = WixConverter.BundleCustomDataRefElementName;
                                this.ConvertCustomTableElementToBundle(element);
                            }
                            break;
                        case CustomTableTarget.Msi:
                            if (this.OnInformation(ConverterTestType.CustomTableRef, element, "CustomTable elements that don't contain the table definition are now CustomTableRef for MSI."))
                            {
                                element.Name = WixConverter.CustomTableRefElementName;
                            }
                            break;
                        default:
                            this.OnError(ConverterTestType.CustomTableNotAlwaysConvertable, element, "The CustomTable element contains no 'Column' elements so can't be converted. Use the 'CustomTableRef' element for MSI. Use the 'BundleCustomDataRef' element for Bundles. Use the --custom-table argument to force conversion to 'msi' or 'bundle'. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
                            break;
                    }
                }
            }
        }

        private void ConvertCustomTableElementToBundle(XElement element)
        {
            foreach (var xColumn in element.Elements(ColumnElementName))
            {
                xColumn.Name = WixConverter.BundleAttributeDefinitionElementName;

                foreach (var xAttribute in xColumn.Attributes().ToList())
                {
                    if (xAttribute.Name.LocalName != "Id" &&
                        (xAttribute.Name.Namespace == WixConverter.Wix3Namespace ||
                        xAttribute.Name.Namespace == WixConverter.WixNamespace ||
                        String.IsNullOrEmpty(xAttribute.Name.Namespace.NamespaceName)))
                    {
                        xAttribute.Remove();
                    }
                }
            }

            foreach (var xRow in element.Elements(RowElementName))
            {
                xRow.Name = WixConverter.BundleElementElementName;

                foreach (var xData in xRow.Elements(DataElementName))
                {
                    xData.Name = WixConverter.BundleAttributeElementName;

                    var xColumn = xData.Attribute("Column");
                    if (xColumn != null)
                    {
                        xData.Add(new XAttribute("Id", xColumn.Value));
                        xColumn.Remove();
                    }

                    this.ConvertInnerTextToAttribute(xData, "Value");
                }
            }
        }

        private void ConvertControlElement(XElement element)
        {
            using (var lab = new ConversionLab(element))
            {
                var xConditions = element.Elements(ConditionElementName).ToList();
                var comments = new List<XNode>();
                var conditions = new List<KeyValuePair<string, string>>();

                foreach (var xCondition in xConditions)
                {
                    var action = UppercaseFirstChar(xCondition.Attribute("Action")?.Value);
                    if (!String.IsNullOrEmpty(action) &&
                        TryGetInnerText(xCondition, out var text, out comments, comments) &&
                        this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the '{1}Condition' attribute instead.", xCondition.Name.LocalName, action))
                    {
                        conditions.Add(new KeyValuePair<string, string>(action, text));
                    }
                }

                foreach (var actionCondition in conditions.GroupBy(c => c.Key))
                {
                    var conditionValues = actionCondition.Select(c => c.Value).ToList();

                    var finalCondition = (conditionValues.Count == 1) ? conditionValues.Single() : String.Join(" OR ", conditionValues.Select(c => $"({c})"));

                    element.Add(new XAttribute(actionCondition.Key + "Condition", finalCondition));
                }

                foreach (var xCondition in xConditions)
                {
                    xCondition.Remove();
                }

                lab.RemoveOrphanTextNodes();
                lab.AddCommentsAsSiblings(comments);
            }
        }

        private void ConvertComponentElement(XElement element)
        {
            var guid = element.Attribute("Guid");
            if (guid != null && guid.Value == "*")
            {
                if (this.OnInformation(ConverterTestType.AutoGuidUnnecessary, element, "Using '*' for the Component Guid attribute is unnecessary. Remove the attribute to remove the redundancy."))
                {
                    guid.Remove();
                }
            }

            var xCondition = element.Element(ConditionElementName);
            if (xCondition != null)
            {
                if (TryGetInnerText(xCondition, out var text, out var comments) &&
                    this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the 'Condition' attribute instead.", xCondition.Name.LocalName))
                {
                    using (var lab = new ConversionLab(element))
                    {
                        xCondition.Remove();
                        element.Add(new XAttribute("Condition", text));
                        lab.RemoveOrphanTextNodes();
                        lab.AddCommentsAsSiblings(comments);
                    }
                }
            }

            this.RenameWin64ToBitness(element);
        }

        private void ConvertDirectoryElement(XElement element)
        {
            if (null == element.Attribute("Name"))
            {
                var attribute = element.Attribute("ShortName");
                if (null != attribute)
                {
                    var shortName = attribute.Value;
                    if (this.OnInformation(ConverterTestType.AssignDirectoryNameFromShortName, element, "The directory ShortName attribute is being renamed to Name since Name wasn't specified for value '{0}'", shortName))
                    {
                        element.Add(new XAttribute("Name", shortName));
                        attribute.Remove();
                    }
                }
            }

            var id = element.Attribute("Id")?.Value;

            if (id == "TARGETDIR")
            {
                if (this.OnInformation(ConverterTestType.TargetDirDeprecated, element, "The TARGETDIR directory should no longer be explicitly defined. Remove the Directory element with Id attribute 'TARGETDIR'."))
                {
                    AddTargetDirDirectoryAttributeToComponents(element);

                    RemoveElementKeepChildren(element);
                }
            }
            else if (id != null &&
                     WindowsInstallerStandard.IsStandardDirectory(id) &&
                     this.OnInformation(ConverterTestType.DefiningStandardDirectoryDeprecated, element, "Standard directories such as '{0}' should no longer be defined using the Directory element. Use the StandardDirectory element instead.", id))
            {
                RenameElementToStandardDirectory(element);
            }
        }

        private void ConvertDirectoryRefElement(XElement element)
        {
            var id = element.Attribute("Id")?.Value;

            if (id != null && WindowsInstallerStandard.IsStandardDirectory(id))
            {
                if (!element.HasElements)
                {
                    this.OnError(ConverterTestType.EmptyStandardDirectoryRefNotConvertable, element, "Referencing '{0}' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages", id);
                }
                else if (id == "TARGETDIR")
                {
                    if (this.OnInformation(ConverterTestType.StandardDirectoryRefDeprecated, element, "The {0} directory should no longer be explicitly referenced. Remove the DirectoryRef element with Id attribute '{0}'.", id))
                    {
                        AddTargetDirDirectoryAttributeToComponents(element);

                        RemoveElementKeepChildren(element);

                        this.OnError(ConverterTestType.TargetDirRefRemoved, element, "A reference to the TARGETDIR Directory was removed. This can cause unintended side effects. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages");
                    }
                }
                else if (this.OnInformation(ConverterTestType.StandardDirectoryRefDeprecated, element, "The standard directory '{0}' should no longer be directly referenced. Use the StandardDirectory element instead.", id))
                {
                    RenameElementToStandardDirectory(element);
                }
            }
        }

        private void ConvertFeatureElement(XElement element)
        {
            var xAbsent = element.Attribute("Absent");
            if (xAbsent != null &&
                this.OnInformation(ConverterTestType.FeatureAbsentAttributeReplaced, element, "The Feature element's Absent attribute has been replaced with the AllowAbsent attribute. Use the 'AllowAbsent' attribute instead."))
            {
                if (xAbsent.Value == "disallow")
                {
                    element.Add(new XAttribute("AllowAbsent", "no"));
                }
                xAbsent.Remove();
            }

            var xAllowAdvertise = element.Attribute("AllowAdvertise");
            if (xAllowAdvertise != null)
            {
                if ((xAllowAdvertise.Value == "system" || xAllowAdvertise.Value == "allow") &&
                    this.OnInformation(ConverterTestType.FeatureAllowAdvertiseValueDeprecated, element, "The AllowAdvertise attribute's '{0}' value is deprecated. Set the value to 'yes' instead.", xAllowAdvertise.Value))
                {
                    xAllowAdvertise.Value = "yes";
                }
                else if (xAllowAdvertise.Value == "disallow" &&
                    this.OnInformation(ConverterTestType.FeatureAllowAdvertiseValueDeprecated, element, "The AllowAdvertise attribute's '{0}' value is deprecated. Remove the value instead.", xAllowAdvertise.Value))
                {
                    xAllowAdvertise.Remove();
                }
            }

            var xCondition = element.Element(ConditionElementName);
            if (xCondition != null)
            {
                var level = xCondition.Attribute("Level")?.Value;
                if (!String.IsNullOrEmpty(level) &&
                    TryGetInnerText(xCondition, out var text, out var comments) &&
                    this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the 'Level' element instead.", xCondition.Name.LocalName))
                {
                    using (var lab = new ConversionLab(xCondition))
                    {
                        lab.ReplaceTargetElement(new XElement(LevelElementName,
                                                              new XAttribute("Value", level),
                                                              new XAttribute("Condition", text)));
                        lab.AddCommentsAsSiblings(comments);
                    }
                }
            }
        }

        private void ConvertFileElement(XElement element)
        {
            if (this.SourceVersion < 4 && null == element.Attribute("Id"))
            {
                var attribute = element.Attribute("Name");

                if (null == attribute)
                {
                    attribute = element.Attribute("Source");
                }

                if (null != attribute)
                {
                    var name = Path.GetFileName(attribute.Value);

                    if (this.OnInformation(ConverterTestType.AssignAnonymousFileId, element, "The file id is being updated to '{0}' to ensure it remains the same as the v3 default", name))
                    {
                        IEnumerable<XAttribute> attributes = element.Attributes().ToList();
                        element.RemoveAttributes();
                        element.Add(new XAttribute("Id", GetIdentifierFromName(name)));
                        element.Add(attributes);
                    }
                }
            }
        }

        private void ConvertLaunchConditionElement(XElement element)
        {
            var message = element.Attribute("Message")?.Value;

            if (!String.IsNullOrEmpty(message) &&
                TryGetInnerText(element, out var text, out var comments) &&
                this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the 'Launch' element instead.", element.Name.LocalName))
            {
                using (var lab = new ConversionLab(element))
                {
                    lab.ReplaceTargetElement(new XElement(LaunchElementName,
                                                          new XAttribute("Condition", text),
                                                          new XAttribute("Message", message)));
                    lab.AddCommentsAsSiblings(comments);
                }
            }
        }

        private void ConvertFirewallRemoteAddressElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertEmbeddedChainerElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Condition");
        }

        private void ConvertErrorElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Message");
        }

        private void ConvertExePackageElement(XElement element)
        {
            this.ConvertSuppressSignatureVerification(element);

            this.UpdatePackageCacheAttribute(element);

            foreach (var attributeName in new[] { "InstallCommand", "RepairCommand", "UninstallCommand" })
            {
                var newName = attributeName.Replace("Command", "Arguments");
                var attribute = element.Attribute(attributeName);

                if (attribute != null &&
                    this.OnInformation(ConverterTestType.RenameExePackageCommandToArguments, element, "The {0} element {1} attribute has been renamed {2}.", element.Name.LocalName, attribute.Name.LocalName, newName))
                {
                    element.Add(new XAttribute(newName, attribute.Value));
                    attribute.Remove();
                }
            }
        }

        private void ConvertPermissionExElement(XElement element)
        {
            var xCondition = element.Element(ConditionElementName);
            if (xCondition != null)
            {
                if (TryGetInnerText(xCondition, out var text, out var comments) &&
                    this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the 'Condition' attribute instead.", xCondition.Name.LocalName))
                {
                    using (var lab = new ConversionLab(xCondition))
                    {
                        lab.RemoveTargetElement();
                    }
                    using (var lab = new ConversionLab(element))
                    {
                        element.Add(new XAttribute("Condition", text));
                        lab.RemoveOrphanTextNodes();
                        lab.AddCommentsAsSiblings(comments);
                    }
                }
            }
        }

        private void ConvertProgressTextElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Message");
        }

        private void ConvertModuleElement(XElement element)
        {
            if (element.Attribute("Guid") == null // skip already-converted Module elements
                && this.OnInformation(ConverterTestType.ModuleAndPackageRenamed, element, "The Module and Package elements have been renamed and reorganized for simplicity."))
            {
                var xModule = element;

                var xSummaryInformation = xModule.Element(PackageElementName);
                if (xSummaryInformation != null)
                {
                    xSummaryInformation.Name = SummaryInformationElementName;

                    var xInstallerVersion = xSummaryInformation.Attribute("InstallerVersion");
                    if (this.SourceVersion < 4 && xInstallerVersion == null)
                    {
                        this.OnInformation(ConverterTestType.InstallerVersionBehaviorChange, element, "Breaking change: The default value for Package/@InstallerVersion has been changed to '500' regardless of build platform. If you need a lower version, set it manually in the Module element.");
                    }

                    RemoveAttribute(xSummaryInformation, "AdminImage");
                    RemoveAttribute(xSummaryInformation, "Comments");
                    MoveAttribute(xSummaryInformation, "Id", xModule, "Guid");
                    MoveAttribute(xSummaryInformation, "InstallerVersion", xModule);
                    RemoveAttribute(xSummaryInformation, "Languages");
                    RemoveAttribute(xSummaryInformation, "Platform");
                    RemoveAttribute(xSummaryInformation, "Platforms");
                    RemoveAttribute(xSummaryInformation, "ReadOnly");
                    MoveAttribute(xSummaryInformation, "SummaryCodepage", xSummaryInformation, "Codepage", defaultValue: "1252");

                    if (!xSummaryInformation.HasAttributes)
                    {
                        xSummaryInformation.Remove();
                    }
                }
            }
        }

        private void ConvertMsuPackageElement(XElement element)
        {
            this.ConvertSuppressSignatureVerification(element);

            this.UpdatePackageCacheAttribute(element);

            var kbAttribute = element.Attribute("KB");

            if (null != kbAttribute
                && this.OnInformation(ConverterTestType.MsuPackageKBObsolete, element, "The MsuPackage element contains obsolete '{0}' attribute. Windows no longer supports silently removing MSUs so the attribute is unnecessary. The attribute will be removed.", kbAttribute.Name))
            {
                kbAttribute.Remove();
            }

            var permanentAttribute = element.Attribute("Permanent");

            if (null != permanentAttribute
                && this.OnInformation(ConverterTestType.MsuPackagePermanentObsolete, element, "The MsuPackage element contains obsolete '{0}' attribute. MSU packages are now always permanent because Windows no longer supports silently removing MSUs. The attribute will be removed.", permanentAttribute.Name))
            {
                permanentAttribute.Remove();
            }
        }

        private void ConvertProductElement(XElement element)
        {
            var id = element.Attribute("Id");
            if (id != null && id.Value == "*")
            {
                if (this.OnInformation(ConverterTestType.AutoGuidUnnecessary, element, "Using '*' for the Product Id attribute is unnecessary. Remove the attribute to remove the redundancy."))
                {
                    id.Remove();
                }
            }

            var xMediaTemplate = element.Element(MediaTemplateElementName);
            if (xMediaTemplate?.HasAttributes == false
                && this.OnInformation(ConverterTestType.DefaultMediaTemplate, element, "A MediaTemplate with no attributes set is now provided by default. Remove the element."))
            {
                xMediaTemplate.Remove();
            }

            if (this.OnInformation(ConverterTestType.ProductAndPackageRenamed, element, "The Product and Package elements have been renamed and reorganized for simplicity."))
            {
                var xPackage = element;
                xPackage.Name = PackageElementName;

                var xSummaryInformation = xPackage.Element(PackageElementName);
                if (xSummaryInformation != null)
                {
                    xSummaryInformation.Name = SummaryInformationElementName;

                    var xInstallerVersion = xSummaryInformation.Attribute("InstallerVersion");
                    if (this.SourceVersion < 4 && xInstallerVersion == null)
                    {
                        this.OnInformation(ConverterTestType.InstallerVersionBehaviorChange, element, "Breaking change: The default value for Package/@InstallerVersion has been changed to '500' regardless of build platform. If you need a lower version, set it manually in the Package element.");
                    }

                    if (xSummaryInformation.Attribute("Compressed") == null)
                    {
                        xPackage.SetAttributeValue("Compressed", "no");
                    }
                    else
                    {
                        MoveAttribute(xSummaryInformation, "Compressed", xPackage, defaultValue: "yes");
                    }

                    RemoveAttribute(xSummaryInformation, "AdminImage");
                    RemoveAttribute(xSummaryInformation, "Comments");
                    RemoveAttribute(xSummaryInformation, "Id");
                    MoveAttribute(xSummaryInformation, "InstallerVersion", xPackage, defaultValue: "500");
                    MoveAttribute(xSummaryInformation, "InstallScope", xPackage, "Scope", defaultValue: "perMachine");
                    RemoveAttribute(xSummaryInformation, "Languages");
                    RemoveAttribute(xSummaryInformation, "Platform");
                    RemoveAttribute(xSummaryInformation, "Platforms");
                    RemoveAttribute(xSummaryInformation, "ReadOnly");
                    MoveAttribute(xSummaryInformation, "ShortNames", xPackage);
                    MoveAttribute(xSummaryInformation, "SummaryCodepage", xSummaryInformation, "Codepage", defaultValue: "1252");
                    MoveAttribute(xPackage, "Id", xPackage, "ProductCode");

                    var xInstallPrivileges = xSummaryInformation.Attribute("InstallPrivileges");
                    switch (xInstallPrivileges?.Value)
                    {
                        case "limited":
                            xPackage.SetAttributeValue("Scope", "perUser");
                            break;
                        case "elevated":
                        {
                            var xAllUsers = xPackage.Elements(PropertyElementName).SingleOrDefault(p => p.Attribute("Id")?.Value == "ALLUSERS");
                            if (xAllUsers?.Attribute("Value")?.Value == "1")
                            {
                                xAllUsers?.Remove();
                            }
                        }
                        break;
                    }

                    xInstallPrivileges?.Remove();

                    if (!xSummaryInformation.HasAttributes)
                    {
                        xSummaryInformation.Remove();
                    }
                }
            }
        }

        private static void MoveAttribute(XElement xSource, string attributeName, XElement xDestination, string destinationAttributeName = null, string defaultValue = null)
        {
            var xAttribute = xSource.Attribute(attributeName);
            if (xAttribute != null && (defaultValue == null || xAttribute.Value != defaultValue))
            {
                xDestination.SetAttributeValue(destinationAttributeName ?? attributeName, xAttribute.Value);
            }

            xAttribute?.Remove();
        }

        private static void RemoveAttribute(XElement xSummaryInformation, string attributeName)
        {
            var xAttribute = xSummaryInformation.Attribute(attributeName);
            xAttribute?.Remove();
        }

        private void ConvertPropertyRefElement(XElement element)
        {
            var newElementName = String.Empty;
            var newNamespace = WixUtilNamespace;
            var newNamespaceName = "util";
            var replace = true;

            var id = element.Attribute("Id");
            switch (id?.Value)
            {
                case "WIX_SUITE_BACKOFFICE":
                case "WIX_SUITE_BLADE":
                case "WIX_SUITE_COMMUNICATIONS":
                case "WIX_SUITE_COMPUTE_SERVER":
                case "WIX_SUITE_DATACENTER":
                case "WIX_SUITE_EMBEDDED_RESTRICTED":
                case "WIX_SUITE_EMBEDDEDNT":
                case "WIX_SUITE_ENTERPRISE":
                case "WIX_SUITE_MEDIACENTER":
                case "WIX_SUITE_PERSONAL":
                case "WIX_SUITE_SECURITY_APPLIANCE":
                case "WIX_SUITE_SERVERR2":
                case "WIX_SUITE_SINGLEUSERTS":
                case "WIX_SUITE_SMALLBUSINESS":
                case "WIX_SUITE_SMALLBUSINESS_RESTRICTED":
                case "WIX_SUITE_STARTER":
                case "WIX_SUITE_STORAGE_SERVER":
                case "WIX_SUITE_TABLETPC":
                case "WIX_SUITE_TERMINAL":
                case "WIX_SUITE_WH_SERVER":
                    newElementName = "QueryWindowsSuiteInfo";
                    break;
                case "WIX_DIR_ADMINTOOLS":
                case "WIX_DIR_ALTSTARTUP":
                case "WIX_DIR_CDBURN_AREA":
                case "WIX_DIR_COMMON_ADMINTOOLS":
                case "WIX_DIR_COMMON_ALTSTARTUP":
                case "WIX_DIR_COMMON_DOCUMENTS":
                case "WIX_DIR_COMMON_FAVORITES":
                case "WIX_DIR_COMMON_MUSIC":
                case "WIX_DIR_COMMON_PICTURES":
                case "WIX_DIR_COMMON_VIDEO":
                case "WIX_DIR_COOKIES":
                case "WIX_DIR_DESKTOP":
                case "WIX_DIR_HISTORY":
                case "WIX_DIR_INTERNET_CACHE":
                case "WIX_DIR_MYMUSIC":
                case "WIX_DIR_MYPICTURES":
                case "WIX_DIR_MYVIDEO":
                case "WIX_DIR_NETHOOD":
                case "WIX_DIR_PERSONAL":
                case "WIX_DIR_PRINTHOOD":
                case "WIX_DIR_PROFILE":
                case "WIX_DIR_RECENT":
                case "WIX_DIR_RESOURCES":
                    newElementName = "QueryWindowsDirectories";
                    break;
                case "WIX_DWM_COMPOSITION_ENABLED":
                case "WIX_WDDM_DRIVER_PRESENT":
                    newElementName = "QueryWindowsDriverInfo";
                    break;
                case "WIX_ACCOUNT_LOCALSYSTEM":
                case "WIX_ACCOUNT_LOCALSERVICE":
                case "WIX_ACCOUNT_NETWORKSERVICE":
                case "WIX_ACCOUNT_ADMINISTRATORS":
                case "WIX_ACCOUNT_USERS":
                case "WIX_ACCOUNT_GUESTS":
                case "WIX_ACCOUNT_PERFLOGUSERS":
                case "WIX_ACCOUNT_PERFLOGUSERS_NODOMAIN":
                    newElementName = "QueryWindowsWellKnownSIDs";
                    break;
                case "WIX_NATIVE_MACHINE":
                    newElementName = "QueryNativeMachine";
                    break;
                case "VS2017_ROOT_FOLDER":
                case "VS2017_IDE_FSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2017_IDE_VB_PROJECTSYSTEM_INSTALLED":
                case "VS2017_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2017_IDE_VSTS_TESTSYSTEM_INSTALLED":
                case "VS2017_IDE_VC_PROJECTSYSTEM_INSTALLED":
                case "VS2017_IDE_VWD_PROJECTSYSTEM_INSTALLED":
                case "VS2017_IDE_MODELING_PROJECTSYSTEM_INSTALLED":
                case "VS2019_ROOT_FOLDER":
                case "VS2019_IDE_FSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2019_IDE_VB_PROJECTSYSTEM_INSTALLED":
                case "VS2019_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2019_IDE_VSTS_TESTSYSTEM_INSTALLED":
                case "VS2019_IDE_VC_PROJECTSYSTEM_INSTALLED":
                case "VS2019_IDE_VWD_PROJECTSYSTEM_INSTALLED":
                case "VS2019_IDE_MODELING_PROJECTSYSTEM_INSTALLED":
                case "VS2022_ROOT_FOLDER":
                case "VS2022_IDE_FSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2022_IDE_VB_PROJECTSYSTEM_INSTALLED":
                case "VS2022_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED":
                case "VS2022_IDE_VSTS_TESTSYSTEM_INSTALLED":
                case "VS2022_IDE_VC_PROJECTSYSTEM_INSTALLED":
                case "VS2022_IDE_VWD_PROJECTSYSTEM_INSTALLED":
                case "VS2022_IDE_MODELING_PROJECTSYSTEM_INSTALLED":
                    newElementName = "FindVisualStudio";
                    newNamespace = WixVSNamespace;
                    newNamespaceName = "vs";
                    break;
                case "VS2017DEVENV":
                case "VS2017_EXTENSIONS_DIR":
                case "VS2017_ITEMTEMPLATES_DIR":
                case "VS2017_PROJECTTEMPLATES_DIR":
                case "VS2017_SCHEMAS_DIR":
                case "VS2017_IDE_DIR":
                case "VS2017_BOOTSTRAPPER_PACKAGE_FOLDER":
                case "VS2019DEVENV":
                case "VS2019_EXTENSIONS_DIR":
                case "VS2019_ITEMTEMPLATES_DIR":
                case "VS2019_PROJECTTEMPLATES_DIR":
                case "VS2019_SCHEMAS_DIR":
                case "VS2019_IDE_DIR":
                case "VS2019_BOOTSTRAPPER_PACKAGE_FOLDER":
                case "VS2022DEVENV":
                case "VS2022_EXTENSIONS_DIR":
                case "VS2022_ITEMTEMPLATES_DIR":
                case "VS2022_PROJECTTEMPLATES_DIR":
                case "VS2022_SCHEMAS_DIR":
                case "VS2022_IDE_DIR":
                case "VS2022_BOOTSTRAPPER_PACKAGE_FOLDER":
                    // These PropertyRefs need to stay (in addition to the `FindVisualStudio`
                    // addition) because they're constructed from deeper AppSearches.
                    newElementName = "FindVisualStudio";
                    newNamespace = WixVSNamespace;
                    newNamespaceName = "vs";
                    replace = false;
                    break;
                case "WIX_DIRECTX_PIXELSHADERVERSION":
                case "WIX_DIRECTX_VERTEXSHADERVERSION":
                    newElementName = "GetCapabilities";
                    newNamespace = WixDirectXNamespace;
                    newNamespaceName = "directx";
                    break;
            }

            if (!String.IsNullOrEmpty(newElementName)
                && this.OnInformation(ConverterTestType.ReferencesReplaced, element, "UI, custom action, and property reference {0} has been replaced with strongly-typed element.", id))
            {
                using (var lab = new ConversionLab(element))
                {
                    this.XRoot.SetAttributeValue(XNamespace.Xmlns + newNamespaceName, newNamespace.NamespaceName);
                    lab.InsertUniqueElementBeforeTargetElement(new XElement(newNamespace + newElementName));

                    if (replace)
                    {
                        lab.RemoveTargetElement();
                    }
                }
            }
        }

        private void ConvertUIRefElement(XElement element)
        {
            var id = element.Attribute("Id")?.Value;

            if (id != null
                && (id == "WixUI_Advanced" || id == "WixUI_FeatureTree" || id == "WixUI_InstallDir" || id == "WixUI_Minimal" || id == "WixUI_Mondo")
                && this.OnInformation(ConverterTestType.ReferencesReplaced, element, "UI, custom action, and property reference {0} has been replaced with strongly-typed element.", id))
            {
                this.XRoot.SetAttributeValue(XNamespace.Xmlns + "ui", WixUiNamespace.NamespaceName);

                element.AddBeforeSelf(new XElement(WixUiNamespace + "WixUI", new XAttribute("Id", id)));

                element.Remove();
            }
        }

        private void ConvertCustomActionRefElement(XElement element)
        {
            var newElementName = String.Empty;

            var id = element.Attribute("Id");
            switch (id?.Value)
            {
                case "WixBroadcastSettingChange":
                case "WixBroadcastEnvironmentChange":
                case "WixCheckRebootRequired":
                case "WixExitEarlyWithSuccess":
                case "WixFailWhenDeferred":
                case "WixWaitForEvent":
                case "WixWaitForEventDeferred":
                    newElementName = id?.Value.Substring(3); // strip leading Wix
                    break;
            }

            if (!String.IsNullOrEmpty(newElementName)
                && this.OnInformation(ConverterTestType.ReferencesReplaced, element, "UI, custom action and property reference {0} have been replaced with strongly-typed elements.", id))
            {
                element.AddAfterSelf(new XElement(WixUtilNamespace + newElementName));
                element.Remove();
            }
        }

        private void ConvertPublishElement(XElement element)
        {
            if (TryGetInnerText(element, out var text, out var comments) &&
                this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the 'Condition' attribute instead.", element.Name.LocalName))
            {
                using (var lab = new ConversionLab(element))
                {
                    if ("1" == text)
                    {
                        this.OnInformation(ConverterTestType.PublishConditionOneUnnecessary, element, "Adding Condition='1' on {0} elements is no longer necessary. Remove the Condition attribute.", element.Name.LocalName);
                    }
                    else
                    {
                        element.Add(new XAttribute("Condition", text));
                    }

                    lab.RemoveOrphanTextNodes();
                    lab.AddCommentsAsSiblings(comments);
                }
            }

            var evnt = element.Attribute("Event")?.Value;
            var value = element.Attribute("Value")?.Value;

            if (evnt?.Equals("DoAction", StringComparison.OrdinalIgnoreCase) == true
                && value?.StartsWith("WixUI", StringComparison.OrdinalIgnoreCase) == true
                && this.OnInformation(ConverterTestType.CustomActionIdsIncludePlatformSuffix, element, "Custom action ids have changed in WiX v4 extensions to support platform-specific custom actions. For more information, see https://wixtoolset.org/docs/fourthree/#converting-custom-wixui-dialog-sets."))
            {
                element.Attribute("Value").Value = value + "_$(sys.BUILDARCHSHORT)";
            }
        }

        private void ConvertMultiStringValueElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertRegistryKeyElement(XElement element)
        {
            var xAction = element.Attribute("Action");

            if (xAction != null
                && this.OnInformation(ConverterTestType.RegistryKeyActionObsolete, element, "The RegistryKey element's Action attribute is obsolete. Action='create' will be converted to ForceCreateOnInstall='yes'. Action='createAndRemoveOnUninstall' will be converted to ForceCreateOnInstall='yes' and ForceDeleteOnUninstall='yes'."))
            {
                switch (xAction?.Value)
                {
                    case "create":
                        element.SetAttributeValue("ForceCreateOnInstall", "yes");
                        break;
                    case "createAndRemoveOnUninstall":
                        element.SetAttributeValue("ForceCreateOnInstall", "yes");
                        element.SetAttributeValue("ForceDeleteOnUninstall", "yes");
                        break;
                }

                xAction.Remove();
            }
        }

        private void ConvertRelatedBundleElement(XElement element)
        {
            var xAction = element.Attribute("Action");
            var value = xAction?.Value;
            var lowercaseValue = value?.ToLowerInvariant();

            if (value != lowercaseValue
                && this.OnInformation(ConverterTestType.RelatedBundleActionLowercase, element, "The RelatedBundle element's Action attribute value must now be all lowercase. The Action='{0}' will be converted to '{1}'", value, lowercaseValue))
            {
                xAction.Value = lowercaseValue;
            }
        }

        private void ConvertRemotePayloadElement(XElement element)
        {
            var xParent = element.Parent;

            if (xParent.Name == ExePackageElementName &&
                this.OnInformation(ConverterTestType.RemotePayloadRenamed, element, "The RemotePayload element has been renamed. Use the 'ExePackagePayload' instead."))
            {
                element.Name = ExePackagePayloadElementName;
            }
            else if (xParent.Name == MsuPackageElementName &&
                     this.OnInformation(ConverterTestType.RemotePayloadRenamed, element, "The RemotePayload element has been renamed. Use the 'MsuPackagePayload' instead."))
            {
                element.Name = MsuPackagePayloadElementName;
            }

            var xName = xParent.Attribute("Name");
            if (xName != null &&
                this.OnInformation(ConverterTestType.NameAttributeMovedToRemotePayload, xParent, "The Name attribute must be specified on the child XxxPackagePayload element when using a remote payload."))
            {
                element.SetAttributeValue("Name", xName.Value);
                xName.Remove();
            }

            var xDownloadUrl = xParent.Attribute("DownloadUrl");
            if (xDownloadUrl != null &&
                this.OnInformation(ConverterTestType.DownloadUrlAttributeMovedToRemotePayload, xParent, "The DownloadUrl attribute must be specified on the child XxxPackagePayload element when using a remote payload."))
            {
                element.SetAttributeValue("DownloadUrl", xDownloadUrl.Value);
                xDownloadUrl.Remove();
            }

            var xCompressed = xParent.Attribute("Compressed");
            if (xCompressed != null &&
                this.OnInformation(ConverterTestType.CompressedAttributeUnnecessaryForRemotePayload, xParent, "The Compressed attribute should not be specified when using a remote payload."))
            {
                xCompressed.Remove();
            }

            this.OnInformation(ConverterTestType.BurnHashAlgorithmChanged, element, "The hash algorithm for bundles changed from SHA1 to SHA512.");
        }

        private void ConvertRegistrySearchElement(XElement element)
        {
            this.RenameWin64ToBitness(element);
        }

        private void ConvertRequiredPrivilegeElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Name");
        }

        private void ConvertDataElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertSequenceElement(XElement element)
        {
            foreach (var child in element.Elements())
            {
                this.ConvertInnerTextToAttribute(child, "Condition");
            }
        }

        private void ConvertServiceArgumentElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertSetDirectoryElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Condition");
        }

        private void ConvertSetPropertyElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Condition");
        }

        private void ConvertShortcutPropertyElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertProvidesElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The Provides element has been integrated into the WiX v4 namespace. Remove the namespace."))
            {
                element.Name = ProvidesElementName;
            }

            if (element.Parent.Name == ComponentElementName &&
                this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The Provides element has been integrated into the WiX v4 namespace. Add the 'Check' attribute from the WixDependency.wixext to match v3 runtime behavior."))
            {
                element.Add(new XAttribute(DependencyCheckAttributeName, "yes"));
            }
        }

        private void ConvertRequiresElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The Requires element has been integrated into the WiX v4 namespace. Remove the namespace."))
            {
                element.Name = RequiresElementName;
            }

            if (element.Parent.Name == ProvidesElementName &&
                element.Parent.Parent?.Name == ComponentElementName &&
                this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The Requires element has been integrated into the WiX v4 namespace. Add the 'Enforce' attribute from the WixDependency.wixext to match v3 runtime behavior."))
            {
                element.Add(new XAttribute(DependencyEnforceAttributeName, "yes"));
            }
        }

        private void ConvertRequiresRefElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The RequiresRef element has been integrated into the WiX v4 namespace. Remove the namespace."))
            {
                element.Name = RequiresRefElementName;
            }

            if (element.Parent.Name == ProvidesElementName &&
                element.Parent.Parent?.Name == ComponentElementName &&
                this.OnInformation(ConverterTestType.IntegratedDependencyNamespace, element, "The RequiresRef element has been integrated into the WiX v4 namespace. Add the 'Enforce' attribute from the WixDependency.wixext to match v3 runtime behavior."))
            {
                element.Add(new XAttribute(DependencyEnforceAttributeName, "yes"));
            }
        }

        private void ConvertSuppressSignatureVerification(XElement element)
        {
            var suppressSignatureVerification = element.Attribute("SuppressSignatureVerification");

            if (null != suppressSignatureVerification
                && this.OnInformation(ConverterTestType.SuppressSignatureVerificationObsolete, element, "The chain package element contains obsolete '{0}' attribute. The attribute will be removed.", suppressSignatureVerification.Name))
            {
                suppressSignatureVerification.Remove();
            }
        }

        private void ConvertTagElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.TagElementRenamed, element, "The Tag element has been renamed. Use the 'SoftwareTag' element instead."))
            {
                element.Name = SoftwareTagElementName;
            }

            this.RemoveAttributeIfPresent(element, "Licensed", ConverterTestType.SoftwareTagLicensedObsolete, "The {0} element contains obsolete '{1}' attribute. The attribute will be removed.");
            this.RemoveAttributeIfPresent(element, "Type", ConverterTestType.SoftwareTagLicensedObsolete, "The {0} element contains obsolete '{1}' attribute. The attribute will be removed.");
            this.RenameWin64ToBitness(element);
        }

        private void ConvertTagRefElement(XElement element)
        {
            if (this.OnInformation(ConverterTestType.TagRefElementRenamed, element, "The TagRef element has been renamed. Use the 'SoftwareTagRef' element instead."))
            {
                element.Name = SoftwareTagRefElementName;
            }
        }

        private void ConvertTextElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertUITextElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertWindowsInstallerPackageElement(XElement element)
        {
            this.ConvertSuppressSignatureVerification(element);

            this.UpdatePackageCacheAttribute(element);

            if (null != element.Attribute("DisplayInternalUI"))
            {
                this.OnError(ConverterTestType.DisplayInternalUiNotConvertable, element, "The DisplayInternalUI functionality has fundamentally changed and requires BootstrapperApplication support. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
            }
        }

        private void ConvertVerbElement(XElement element)
        {
            if (null != element.Attribute("Target"))
            {
                this.OnError(ConverterTestType.VerbTargetNotConvertable, element, "The Verb/@Target attribute has been replaced with typed @TargetFile and @TargetProperty attributes. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages");
            }
        }

        private void ConvertCustomActionElement(XElement xCustomAction)
        {
            var xBinaryKey = xCustomAction.Attribute("BinaryKey");
            if (xBinaryKey != null && this.OnInformation(ConverterTestType.CustomActionKeysAreNowRefs, xCustomAction, "The CustomAction attributes have been renamed from BinaryKey and FileKey to BinaryRef and FileRef."))
            {
                xCustomAction.SetAttributeValue("BinaryRef", xBinaryKey.Value);
                xBinaryKey.Remove();
                xBinaryKey = xCustomAction.Attribute("BinaryRef");
            }

            var xFileKey = xCustomAction.Attribute("FileKey");
            if (xFileKey != null && this.OnInformation(ConverterTestType.CustomActionKeysAreNowRefs, xCustomAction, "The CustomAction attributes have been renamed from BinaryKey and FileKey to BinaryRef and FileRef."))
            {
                xCustomAction.SetAttributeValue("FileRef", xFileKey.Value);
                xFileKey.Remove();
            }

            if (xBinaryKey?.Value == "WixCA" || xBinaryKey?.Value == "UtilCA")
            {
                if (this.OnInformation(ConverterTestType.WixCABinaryIdRenamed, xCustomAction, "The WixCA custom action DLL Binary table id has been renamed. Use the id 'Wix4UtilCA_X86' instead."))
                {
                    xBinaryKey.Value = "Wix4UtilCA_X86";
                }
            }

            if (xBinaryKey?.Value == "WixCA_x64" || xBinaryKey?.Value == "UtilCA_x64")
            {
                if (this.OnInformation(ConverterTestType.WixCABinaryIdRenamed, xCustomAction, "The WixCA_x64 custom action DLL Binary table id has been renamed. Use the id 'Wix4UtilCA_X64' instead."))
                {
                    xBinaryKey.Value = "Wix4UtilCA_X64";
                }
            }

            var xDllEntry = xCustomAction.Attribute("DllEntry");

            if (xDllEntry?.Value == "CAQuietExec" || xDllEntry?.Value == "CAQuietExec64")
            {
                if (this.OnInformation(ConverterTestType.QuietExecCustomActionsRenamed, xCustomAction, "The CAQuietExec and CAQuietExec64 custom action ids have been renamed. Use the ids 'WixQuietExec' and 'WixQuietExec64' instead."))
                {
                    xDllEntry.Value = xDllEntry.Value.Replace("CAQuietExec", "WixQuietExec");
                }
            }

            var xProperty = xCustomAction.Attribute("Property");

            if (xProperty?.Value == "QtExecCmdLine" || xProperty?.Value == "QtExec64CmdLine")
            {
                if (this.OnInformation(ConverterTestType.QuietExecCustomActionsRenamed, xCustomAction, "The QtExecCmdLine and QtExec64CmdLine property ids have been renamed. Use the ids 'WixQuietExecCmdLine' and 'WixQuietExec64CmdLine' instead."))
                {
                    xProperty.Value = xProperty.Value.Replace("QtExec", "WixQuietExec");
                }
            }

            var xScript = xCustomAction.Attribute("Script");

            if (xScript != null && TryGetInnerText(xCustomAction, out var scriptText, out var comments))
            {
                if (this.OnInformation(ConverterTestType.InnerTextDeprecated, xCustomAction, "Using {0} element text is deprecated. Extract the text to a file and use the 'ScriptSourceFile' attribute to reference it.", xCustomAction.Name.LocalName))
                {
                    var scriptFolder = Path.GetDirectoryName(this.SourceFile) ?? String.Empty;
                    var id = xCustomAction.Attribute("Id")?.Value ?? Guid.NewGuid().ToString("N");
                    var ext = (xScript.Value == "jscript") ? ".js" : (xScript.Value == "vbscript") ? ".vbs" : ".txt";

                    var scriptFile = Path.Combine(scriptFolder, id + ext);
                    File.WriteAllText(scriptFile, scriptText);

                    RemoveChildren(xCustomAction);
                    xCustomAction.Add(new XAttribute("ScriptSourceFile", scriptFile));

                    if (comments.Any())
                    {
                        var remainingNodes = xCustomAction.NodesAfterSelf().ToList();
                        var replacementNodes = remainingNodes.Where(e => XmlNodeType.Text != e.NodeType);
                        foreach (var node in remainingNodes)
                        {
                            node.Remove();
                        }
                        foreach (var comment in comments)
                        {
                            xCustomAction.Add(comment);
                            xCustomAction.Add("\n");
                        }
                        foreach (var node in replacementNodes)
                        {
                            xCustomAction.Add(node);
                        }
                    }
                }
            }
        }

        private void ConvertVariableElement(XElement xVariable)
        {
            var xType = xVariable.Attribute("Type");
            var xValue = xVariable.Attribute("Value");
            if (this.SourceVersion < 4)
            {
                if (xType == null)
                {
                    if (WasImplicitlyStringTyped(xValue?.Value) &&
                        this.OnInformation(ConverterTestType.AssignVariableTypeFormatted, xVariable, "The \"string\" variable type now denotes a literal string. Use \"formatted\" to keep the previous behavior."))
                    {
                        xVariable.Add(new XAttribute("Type", "formatted"));
                    }
                }
                else if (xType.Value == "string" &&
                        this.OnInformation(ConverterTestType.AssignVariableTypeFormatted, xVariable, "The \"string\" variable type now denotes a literal string. Use \"formatted\" to keep the previous behavior."))
                {
                    xType.Value = "formatted";
                }
            }
        }

        private void ConvertPropertyElement(XElement xProperty)
        {
            var xId = xProperty.Attribute("Id");

            if (xId.Value == "QtExecCmdTimeout")
            {
                this.OnInformation(ConverterTestType.QtExecCmdTimeoutAmbiguous, xProperty, "QtExecCmdTimeout was previously used for both CAQuietExec and CAQuietExec64. For WixQuietExec, use WixQuietExecCmdTimeout. For WixQuietExec64, use WixQuietExec64CmdTimeout.");
            }

            this.ConvertInnerTextToAttribute(xProperty, "Value");
        }

        private void ConvertUtilCloseApplicationElementName(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Condition");
        }

        private void ConvertUtilPermissionExElement(XElement element)
        {
            if (this.SourceVersion < 4 && null == element.Attribute("Inheritable"))
            {
                var inheritable = element.Parent.Name == CreateFolderElementName;
                if (!inheritable)
                {
                    if (this.OnInformation(ConverterTestType.AssignPermissionExInheritable, element, "The PermissionEx Inheritable attribute is being set to 'no' to ensure it remains the same as the v3 default."))
                    {
                        element.Add(new XAttribute("Inheritable", "no"));
                    }
                }
            }
        }

        private void ConvertUtilRegistrySearchElement(XElement element)
        {
            this.RenameWin64ToBitness(element);

            if (this.SourceVersion < 4)
            {
                var result = element.Attribute("Result")?.Value;
                if (result == null || result == "value")
                {
                    this.OnError(ConverterTestType.UtilRegistryValueSearchBehaviorChange, element, "Breaking change: util:RegistrySearch for a value no longer clears the variable when the key or value is missing. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
                }
            }
        }

        private void ConvertUtilXmlConfigElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        /// <summary>
        /// Converts a Wix element.
        /// </summary>
        /// <param name="element">The Wix element to convert.</param>
        /// <returns>The converted element.</returns>
        private void ConvertElementWithoutNamespace(XElement element)
        {
            if (this.OnInformation(ConverterTestType.XmlnsMissing, element, "The xmlns attribute is missing. It must be present with a value of '{0}'.", WixNamespace.NamespaceName))
            {
                element.Name = WixNamespace.GetName(element.Name.LocalName);

                element.Add(new XAttribute("xmlns", WixNamespace.NamespaceName)); // set the default namespace.

                foreach (var elementWithoutNamespace in element.DescendantsAndSelf().Where(e => XNamespace.None == e.Name.Namespace))
                {
                    elementWithoutNamespace.Name = WixNamespace.GetName(elementWithoutNamespace.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// Converts a WixLocalization element.
        /// </summary>
        /// <param name="element">The WixLocalization element to convert.</param>
        /// <returns>The converted element.</returns>
        private void ConvertWixLocalizationElementWithoutNamespace(XElement element)
        {
            if (this.OnInformation(ConverterTestType.XmlnsMissing, element, "The xmlns attribute is missing. It must be present with a value of '{0}'.", WxlNamespace.NamespaceName))
            {
                element.Name = WxlNamespace.GetName(element.Name.LocalName);

                element.Add(new XAttribute("xmlns", WxlNamespace.NamespaceName)); // set the default namespace.

                foreach (var elementWithoutNamespace in element.DescendantsAndSelf().Where(e => XNamespace.None == e.Name.Namespace))
                {
                    elementWithoutNamespace.Name = WxlNamespace.GetName(elementWithoutNamespace.Name.LocalName);
                }
            }
        }

        private void ConvertWixLocalizationStringElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Value");
        }

        private void ConvertWixLocalizationUIElement(XElement element)
        {
            this.ConvertInnerTextToAttribute(element, "Text");
        }

        private void ConvertInnerTextToAttribute(XElement element, string attributeName)
        {
            if (TryGetInnerText(element, out var text, out var comments))
            {
                // If the target attribute already exists, error if we have anything more than whitespace.
                var attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    if (!String.IsNullOrWhiteSpace(text))
                    {
                        this.OnError(ConverterTestType.InnerTextDeprecated, attribute, "Using {0} element text is deprecated. Remove the element's text and use only the '{1}' attribute. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages", element.Name.LocalName, attributeName);
                    }
                }
                else if (this.OnInformation(ConverterTestType.InnerTextDeprecated, element, "Using {0} element text is deprecated. Use the '{1}' attribute instead.", element.Name.LocalName, attributeName))
                {
                    using (var lab = new ConversionLab(element))
                    {
                        lab.RemoveOrphanTextNodes();
                        element.Add(new XAttribute(attributeName, text));
                        lab.AddCommentsAsSiblings(comments);
                    }
                }
            }
        }

        void RemoveAttributeIfPresent(XElement element, string attributeName, ConverterTestType type, string format)
        {
            var xAttribute = element.Attribute(attributeName);
            if (null != xAttribute && this.OnInformation(type, element, format, element.Name.LocalName, xAttribute.Name))
            {
                xAttribute.Remove();
            }
        }

        private void RenameWin64ToBitness(XElement element)
        {
            var win64 = element.Attribute("Win64");
            if (win64 != null && this.OnInformation(ConverterTestType.Win64AttributeRenamed, element, "The {0} element's Win64 attribute has been renamed. Use the Bitness attribute instead.", element.Name.LocalName))
            {
                var value = this.UpdateWin64ValueToBitnessValue(win64);
                element.Add(new XAttribute("Bitness", value));
                win64.Remove();
            }
        }

        private void UpdatePackageCacheAttribute(XElement element)
        {
            var cacheAttribute = element.Attribute("Cache");
            var cacheValue = cacheAttribute?.Value;
            string replacement = null;

            switch (cacheValue)
            {
                case "yes":
                    replacement = "keep";
                    break;
                case "no":
                    replacement = "remove";
                    break;
                case "always":
                    replacement = "force";
                    break;
            }

            if (!String.IsNullOrEmpty(replacement) &&
                this.OnInformation(ConverterTestType.BundlePackageCacheAttributeValueObsolete, element, "The chain package element 'Cache' attribute contains obsolete '{0}' value. The value should be '{1}' instead.", cacheValue, replacement))
            {
                cacheAttribute.SetValue(replacement);
            }
        }

        private string UpdateWin64ValueToBitnessValue(XAttribute xWin64Attribute)
        {
            var value = xWin64Attribute.Value ?? String.Empty;
            switch (value)
            {
                case "yes":
                    return "always64";
                case "no":
                    return "always32";
                default:
                    this.OnError(ConverterTestType.Win64AttributeRenameCannotBeAutomatic, xWin64Attribute, "Breaking change: The Win64 attribute's value '{0}' cannot be converted automatically to the new Bitness attribute. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages", value);
                    return value;
            }
        }

        private IEnumerable<ConverterTestType> YieldConverterTypes(IEnumerable<string> types)
        {
            if (null != types)
            {
                foreach (var type in types)
                {
                    if (Enum.TryParse<ConverterTestType>(type, true, out var itt))
                    {
                        yield return itt;
                    }
                    else // not a known ConverterTestType
                    {
                        this.OnError(ConverterTestType.ConverterTestTypeUnknown, null, "Unknown error type: '{0}'.", type);
                    }
                }
            }
        }

        private static void UpdateElementsWithDeprecatedNamespaces(IEnumerable<XElement> elements, Dictionary<XNamespace, XNamespace> deprecatedToUpdatedNamespaces)
        {
            foreach (var element in elements)
            {
                if (deprecatedToUpdatedNamespaces.TryGetValue(element.Name.Namespace, out var ns))
                {
                    element.Name = ns.GetName(element.Name.LocalName);
                }

                // Remove all the attributes and add them back with their namespace updated (as necessary).
                IEnumerable<XAttribute> attributes = element.Attributes().ToList();
                element.RemoveAttributes();

                foreach (var attribute in attributes)
                {
                    var convertedAttribute = attribute;

                    if (attribute.IsNamespaceDeclaration)
                    {
                        if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Value, out ns))
                        {
                            if (ns == XNamespace.None)
                            {
                                continue;
                            }

                            convertedAttribute = ("xmlns" == attribute.Name.LocalName) ? new XAttribute(attribute.Name.LocalName, ns.NamespaceName) : new XAttribute(XNamespace.Xmlns + attribute.Name.LocalName, ns.NamespaceName);
                        }
                    }
                    else if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Name.Namespace, out ns))
                    {
                        convertedAttribute = new XAttribute(ns.GetName(attribute.Name.LocalName), attribute.Value);
                    }

                    if (convertedAttribute != attribute)
                    {
                        foreach (var message in attribute.Annotations<Message>())
                        {
                            convertedAttribute.AddAnnotation(message);
                        }
                    }

                    element.Add(convertedAttribute);
                }
            }
        }

        /// <summary>
        /// Determine if the whitespace preceding a node is appropriate for its depth level.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level that should match this whitespace.</param>
        /// <param name="whitespace">The whitespace to validate.</param>
        /// <returns>true if the whitespace is legal; false otherwise.</returns>
        private static bool LeadingWhitespaceValid(int indentationAmount, int level, string whitespace)
        {
            // Strip off leading newlines; there can be an arbitrary number of these.
            whitespace = whitespace.TrimStart(XDocumentNewLine);

            var indentation = new string(' ', level * indentationAmount);

            return whitespace == indentation;
        }

        /// <summary>
        /// Fix the whitespace in a whitespace node.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level of the desired whitespace.</param>
        /// <param name="whitespace">The whitespace node to fix.</param>
        private static void FixupWhitespace(int indentationAmount, int level, XText whitespace)
        {
            var value = new StringBuilder(whitespace.Value.Length);

            // Keep any previous preceeding new lines.
            var newlines = whitespace.Value.TakeWhile(c => c == XDocumentNewLine).Count();

            // Ensure there is always at least one new line before the indentation.
            value.Append(XDocumentNewLine, newlines == 0 ? 1 : newlines);

            whitespace.Value = value.Append(' ', level * indentationAmount).ToString();
        }

        private void ConvertMbaPrereqVariables()
        {
            XElement root = this.XRoot;
            VisitElement(root, x =>
            {
                if (x is XElement e && e.Attribute("Id") is XAttribute a)
                {
                    if (e.Name == ExePackageElementName || e.Name == MsiPackageElementName ||
                             e.Name == MspPackageElementName || e.Name == MsuPackageElementName)
                    {
                        if (!this.State.ChainPackageElementsById.TryGetValue(a.Value, out var elements))
                        {
                            elements = new List<XElement>();
                            this.State.ChainPackageElementsById.Add(a.Value, elements);
                        }

                        elements.Add(e);
                    }
                    else if (e.Name == WixVariableElementName && e.Attribute("Value") != null)
                    {
                        switch (a.Value)
                        {
                            case "WixMbaPrereqPackageId":
                                this.State.WixMbaPrereqPackageIdElements.Add(e);
                                break;
                            case "WixMbaPrereqLicenseUrl":
                                this.State.WixMbaPrereqLicenseUrlElements.Add(e);
                                break;
                        }
                    }
                }
                return true;
            });

            if (this.State.WixMbaPrereqPackageIdElements.Count == 1 && this.State.WixMbaPrereqLicenseUrlElements.Count < 2)
            {
                var wixMbaPrereqPackageIdElement = this.State.WixMbaPrereqPackageIdElements[0];
                var packageId = wixMbaPrereqPackageIdElement.Attribute("Value")?.Value;
                if (this.State.ChainPackageElementsById.TryGetValue(packageId, out var packageElements) && packageElements.Count == 1)
                {
                    var packageElement = packageElements[0];

                    if (this.OnInformation(ConverterTestType.WixMbaPrereqPackageIdDeprecated, wixMbaPrereqPackageIdElement, "The magic WixVariable 'WixMbaPrereqPackageId' has been removed. Add bal:PrereqPackage=\"yes\" to the target package instead."))
                    {
                        packageElement.Add(new XAttribute(BalPrereqPackageAttributeName, "yes"));

                        using (var lab = new ConversionLab(wixMbaPrereqPackageIdElement))
                        {
                            lab.RemoveTargetElement();
                        }

                        this.State.WixMbaPrereqPackageIdElements.Clear();
                    }

                    if (this.State.WixMbaPrereqLicenseUrlElements.Count == 1)
                    {
                        var wixMbaPrereqLicenseUrlElement = this.State.WixMbaPrereqLicenseUrlElements[0];
                        if (this.OnInformation(ConverterTestType.WixMbaPrereqLicenseUrlDeprecated, wixMbaPrereqLicenseUrlElement, "The magic WixVariable 'WixMbaPrereqLicenseUrl' has been removed. Add bal:PrereqLicenseUrl=\"<url>\" to a prereq package instead."))
                        {
                            var licenseUrl = wixMbaPrereqLicenseUrlElement.Attribute("Value")?.Value;
                            packageElement.Add(new XAttribute(BalPrereqLicenseUrlAttributeName, licenseUrl));
                            using (var lab = new ConversionLab(wixMbaPrereqLicenseUrlElement))
                            {
                                lab.RemoveTargetElement();
                            }

                            this.State.WixMbaPrereqLicenseUrlElements.Clear();
                        }
                    }
                }
            }

            foreach (var element in this.State.WixMbaPrereqPackageIdElements)
            {
                this.OnError(ConverterTestType.WixMbaPrereqPackageIdDeprecated, element, "The magic WixVariable 'WixMbaPrereqPackageId' has been removed. Add bal:PrereqPackage=\"yes\" to the target package instead. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
            }

            foreach (var element in this.State.WixMbaPrereqLicenseUrlElements)
            {
                this.OnError(ConverterTestType.WixMbaPrereqLicenseUrlDeprecated, element, "The magic WixVariable 'WixMbaPrereqLicenseUrl' has been removed. Add bal:PrereqLicenseUrl=\"<url>\" to a prereq package instead. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles");
            }
        }

        /// <summary>
        /// Removes unused namespaces from the element and its children.
        /// </summary>
        /// <param name="root">Root element to start at.</param>
        private void RemoveUnusedNamespaces(XElement root)
        {
            var declarations = new List<XAttribute>();
            var namespaces = new HashSet<string>();

            VisitElement(root, x =>
            {
                if (x is XAttribute a && a.IsNamespaceDeclaration)
                {
                    declarations.Add(a);
                    namespaces.Add(a.Value);
                }
                return true;
            });

            foreach (var ns in namespaces.ToList())
            {
                VisitElement(root, x =>
                {
                    if ((x is XElement e && e.Name.Namespace == ns) ||
                        (x is XAttribute a && !a.IsNamespaceDeclaration && a.Name.Namespace == ns))
                    {
                        namespaces.Remove(ns);
                        return false;
                    }

                    return true;
                });
            }

            foreach (var declaration in declarations)
            {
                if (namespaces.Contains(declaration.Value) &&
                    this.OnInformation(ConverterTestType.RemoveUnusedNamespaces, declaration, "The namespace '{0}' is not used. Remove unused namespaces.", declaration.Value))
                {
                    declaration.Remove();
                }
            }
        }

        private void MoveNamespacesToRoot(XElement root)
        {
            var rootNamespaces = new HashSet<string>();
            var rootNamespacePrefixes = new HashSet<string>();
            var nonRootDeclarations = new List<XAttribute>();

            VisitElement(root, x =>
            {
                if (x is XAttribute a && a.IsNamespaceDeclaration)
                {
                    if (x.Parent == root)
                    {
                        rootNamespaces.Add(a.Value);
                        rootNamespacePrefixes.Add(a.Name.LocalName);
                    }
                    else
                    {
                        nonRootDeclarations.Add(a);
                    }
                }

                return true;
            });

            foreach (var declaration in nonRootDeclarations)
            {
                if (this.OnInformation(ConverterTestType.MoveNamespacesToRoot, declaration, "Namespace should be defined on the root. The '{0}' namespace was move to the root element.", declaration.Value))
                {
                    if (!rootNamespaces.Contains(declaration.Value))
                    {
                        var prefix = GetNamespacePrefix(declaration, rootNamespacePrefixes);

                        var rootDeclaration = new XAttribute(XNamespace.Xmlns + prefix, declaration.Value);
                        root.Add(rootDeclaration);

                        rootNamespaces.Add(rootDeclaration.Value);
                        rootNamespacePrefixes.Add(rootDeclaration.Name.LocalName);
                    }

                    declaration.Remove();
                }
            }
        }

        private int ReportMessages(XDocument document, bool saved)
        {
            var conversionCount = this.ConversionMessages.Count;

            // If the converted/formatted document was saved, update the source line numbers
            // in the messages since they will possibly be wrong.
            if (saved && conversionCount > 0)
            {
                var fixedupMessages = new HashSet<Message>();

                // Load the converted document so we can look up new line numbers for
                // messages.
                var convertedDocument = XDocument.Load(this.SourceFile, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                var convertedNavigator = convertedDocument.CreateNavigator();

                // Look through all nodes in the document and try to fix up their line numbers.
                foreach (var elements in document.Descendants())
                {
                    var fixedup = this.FixupMessageLineNumbers(elements, convertedNavigator);
                    fixedupMessages.AddRange(fixedup);

                    // Attributes are not considered nodes so they must be enumerated independently.
                    if (elements is XElement element)
                    {
                        foreach (var attribute in element.Attributes())
                        {
                            fixedup = this.FixupMessageLineNumbers(attribute, convertedNavigator);
                            fixedupMessages.AddRange(fixedup);
                        }
                    }
                }

                // For any messages that couldn't be fixed up, remove their line numbers since they point at lines
                // that are possibly wrong after the conversion.
                for (var i = 0; i < this.ConversionMessages.Count; ++i)
                {
                    var message = this.ConversionMessages[i];

                    if (!fixedupMessages.Contains(message))
                    {
                        this.ConversionMessages[i] = new Message(new SourceLineNumber(this.SourceFile), message.Level, message.Id, message.ResourceNameOrFormat, message.MessageArgs);
                    }
                }
            }

            foreach (var message in this.ConversionMessages)
            {
                this.Messaging.Write(message);
            }

            return conversionCount;
        }

        private IReadOnlyCollection<Message> FixupMessageLineNumbers(XObject obj, XPathNavigator savedDocument)
        {
            var messages = obj.Annotations<Message>().ToList();

            if (messages.Count == 0)
            {
                return Array.Empty<Message>();
            }

            // We can't fix up line numbers based on attributes but we can fix up using their parent element, so do so.
            if (obj is XAttribute attribute)
            {
                obj = attribute.Parent;
            }

            var fixedupMessages = new List<Message>();

            var modifiedLineNumber = this.GetModifiedNodeLineNumber((XElement)obj, savedDocument);

            foreach (var message in messages)
            {
                var fixedupMessage = message;

                // If the line number wasn't modified, the existing message's source line nuber is correct
                // so skip creating a new one.
                if (modifiedLineNumber != null)
                {
                    var index = this.ConversionMessages.IndexOf(message);

                    fixedupMessage = new Message(modifiedLineNumber, message.Level, message.Id, message.ResourceNameOrFormat, message.MessageArgs);

                    this.ConversionMessages[index] = fixedupMessage;
                }

                fixedupMessages.Add(fixedupMessage);
            }

            return fixedupMessages;
        }

        private SourceLineNumber GetModifiedNodeLineNumber(XElement element, XPathNavigator savedDocument)
        {
            var xpathToOld = CalculateXPath(element);

            var newNode = savedDocument.SelectSingleNode(xpathToOld);
            var newLineNumber = (newNode as IXmlLineInfo).LineNumber;

            var oldLineNumber = (element as IXmlLineInfo)?.LineNumber;
            return (oldLineNumber != newLineNumber) ? new SourceLineNumber(this.SourceFile, newLineNumber) : null;
        }

        /// <summary>
        /// Output an error message to the console.
        /// </summary>
        /// <param name="converterTestType">The type of converter test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this error, and false if it should be ignored.</returns>
        private bool OnError(ConverterTestType converterTestType, XObject node, string message, params object[] args)
        {
            return this.OnMessage(MessageLevel.Error, converterTestType, node, message, args);
        }

        /// <summary>
        /// Output an information message to the console.
        /// </summary>
        /// <param name="converterTestType">The type of converter test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this message, and false if it should be ignored.</returns>
        private bool OnInformation(ConverterTestType converterTestType, XObject node, string message, params object[] args)
        {
            return this.OnMessage(MessageLevel.Information, converterTestType, node, message, args);
        }

        private bool OnMessage(MessageLevel level, ConverterTestType converterTestType, XObject node, string message, params object[] args)
        {
            // Ignore the error if explicitly ignored or outside the range of the current operation.
            if (this.IgnoreErrors.Contains(converterTestType) ||
                (this.Operation == ConvertOperation.Convert && converterTestType < ConverterTestType.EndIgnoreInConvert) ||
                (this.Operation == ConvertOperation.Format && converterTestType > ConverterTestType.BeginIgnoreInFormat))
            {
                return false;
            }

            var sourceLine = SourceLineNumberForXmlLineInfo(this.SourceFile, node);

            var prefix = String.Empty;
            if (level == MessageLevel.Information)
            {
                prefix = "[Converted] ";
            }
            else if (level == MessageLevel.Error && this.ErrorsAsWarnings.Contains(converterTestType))
            {
                level = MessageLevel.Warning;
            }

            var format = prefix + message + $" ({converterTestType})";

            var msg = new Message(sourceLine, level, (int)converterTestType, format, args);

            // Add the message as a node annotation so it could be possible to remap source line numbers
            // to their new locations after converting/formatting (since lines of code could be moved).
            node?.AddAnnotation(msg);
            this.ConversionMessages.Add(msg);

            return true;
        }

        private static string CalculateXPath(XElement element)
        {
            var builder = new StringBuilder();

            while (element != null)
            {
                var index = element.Parent?.Elements().TakeWhile(e => e != element).Count() ?? 0;
                builder.Insert(0, $"/*[{index + 1}]");

                element = element.Parent;
            }

            return builder.ToString();
        }

        private static SourceLineNumber SourceLineNumberForXmlLineInfo(string sourceFile, IXmlLineInfo lineInfo)
        {
            return (lineInfo?.HasLineInfo() == true) ? new SourceLineNumber(sourceFile, lineInfo.LineNumber) : new SourceLineNumber(sourceFile ?? "wix.exe");
        }

        /// <summary>
        /// Return an identifier based on passed file/directory name
        /// </summary>
        /// <param name="name">File/directory name to generate identifer from</param>
        /// <returns>A version of the name that is a legal identifier.</returns>
        /// <remarks>This is duplicated from WiX's Common class.</remarks>
        private static string GetIdentifierFromName(string name)
        {
            var result = IllegalIdentifierCharacters.Replace(name, "_"); // replace illegal characters with "_".

            // MSI identifiers must begin with an alphabetic character or an
            // underscore. Prefix all other values with an underscore.
            if (AddPrefix.IsMatch(name))
            {
                result = String.Concat("_", result);
            }

            return result;
        }

        private static string GetNamespacePrefix(XAttribute declaration, HashSet<string> usedPrefixes)
        {
            var baseNamespace = String.Empty;

            switch (declaration.Value)
            {
                case "http://wixtoolset.org/schemas/v4/wxs/bal":
                    baseNamespace = "bal";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/complus":
                    baseNamespace = "complus";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/dependency":
                    baseNamespace = "dependency";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/difxapp":
                    baseNamespace = "difx";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/directx":
                    baseNamespace = "directx";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/firewall":
                    baseNamespace = "fw";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/http":
                    baseNamespace = "http";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/iis":
                    baseNamespace = "iis";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/msmq":
                    baseNamespace = "msmq";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/netfx":
                    baseNamespace = "netfx";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/powershell":
                    baseNamespace = "ps";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/sql":
                    baseNamespace = "sql";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/ui":
                    baseNamespace = "ui";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/util":
                    baseNamespace = "util";
                    break;

                case "http://wixtoolset.org/schemas/v4/wxs/vs":
                    baseNamespace = "vs";
                    break;
            }

            var ns = baseNamespace;

            for (var i = 1; usedPrefixes.Contains(ns); ++i)
            {
                ns = baseNamespace + i;
            }

            return ns;
        }

        private static string LowercaseFirstChar(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                var c = Char.ToLowerInvariant(value[0]);
                if (c != value[0])
                {
                    var remainder = value.Length > 1 ? value.Substring(1) : String.Empty;
                    return c + remainder;
                }
            }

            return value;
        }

        private static string UppercaseFirstChar(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                var c = Char.ToUpperInvariant(value[0]);
                if (c != value[0])
                {
                    var remainder = value.Length > 1 ? value.Substring(1) : String.Empty;
                    return c + remainder;
                }
            }

            return value;
        }

        private static void AddTargetDirDirectoryAttributeToComponents(XElement element)
        {
            // Move the TARGETDIR reference to the Component Directory attribute
            foreach (var xComponent in element.Elements(ComponentElementName).Where(x => x.Attribute("Directory") is null))
            {
                xComponent.Add(new XAttribute("Directory", "TARGETDIR"));
            }
        }

        private static void RemoveElementKeepChildren(XElement element)
        {
            var parentElement = element.Parent;

            element.Remove();

            if (parentElement.FirstNode is XText text && String.IsNullOrWhiteSpace(text.Value))
            {
                parentElement.FirstNode.Remove();
            }

            foreach (var child in element.Nodes())
            {
                parentElement.Add(child);
            }

            element.RemoveAll();

            if (parentElement.FirstNode is XText textAgain && String.IsNullOrWhiteSpace(textAgain.Value))
            {
                parentElement.FirstNode.Remove();
            }
        }

        private static void RenameElementToStandardDirectory(XElement element)
        {
            element.Name = StandardDirectoryElementName;

            foreach (var attrib in element.Attributes().Where(a => a.Name.LocalName != "Id").ToList())
            {
                attrib.Remove();
            }
        }

        private static bool TryGetInnerText(XElement element, out string value, out List<XNode> comments)
        {
            return TryGetInnerText(element, out value, out comments, new List<XNode>());
        }

        private static bool TryGetInnerText(XElement element, out string value, out List<XNode> comments, List<XNode> initialComments)
        {
            value = null;
            comments = null;
            var found = false;

            var nodes = element.Nodes().ToList();
            comments = initialComments;
            var nonCommentNodes = new List<XNode>();

            foreach (var node in nodes)
            {
                if (XmlNodeType.Comment == node.NodeType)
                {
                    comments.Add(node);
                }
                else
                {
                    nonCommentNodes.Add(node);
                }
            }

            if (nonCommentNodes.Any() && nonCommentNodes.All(e => e.NodeType == XmlNodeType.Text || e.NodeType == XmlNodeType.CDATA))
            {
                value = String.Join(String.Empty, nonCommentNodes.Cast<XText>().Select(TrimTextValue));
                found = true;
            }

            return found;
        }

        private static bool IsTextNode(XNode node, out XText text)
        {
            text = null;

            if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
            {
                text = (XText)node;
            }

            return text != null;
        }

        private static void TrimLeadingText(XDocument document)
        {
            while (IsTextNode(document.Nodes().FirstOrDefault(), out var text))
            {
                text.Remove();
            }
        }

        private static string TrimTextValue(XText text)
        {
            var value = text.Value;

            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }
            else if (text.NodeType == XmlNodeType.CDATA && String.IsNullOrWhiteSpace(value))
            {
                return " ";
            }

            return value.Trim();
        }

        private static void RemoveChildren(XElement element)
        {
            var nodes = element.Nodes().ToList();
            foreach (var node in nodes)
            {
                node.Remove();
            }
        }

        private static bool VisitElement(XElement element, Func<XObject, bool> visitor)
        {
            if (!visitor(element))
            {
                return false;
            }

            if (!element.Attributes().All(a => visitor(a)))
            {
                return false;
            }

            return element.Elements().All(e => VisitElement(e, visitor));
        }

        private static bool WasImplicitlyStringTyped(string value)
        {
            if (value == null)
            {
                return false;
            }
            else if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                if (Int32.TryParse(value.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out var _))
                {
                    return false;
                }
                else if (Version.TryParse(value.Substring(1), out var _))
                {
                    return false;
                }
            }
            else if (Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var _))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converter test types. These are used to condition error messages down to warnings.
        /// </summary>
        private enum ConverterTestType
        {
            /// <summary>
            /// Internal-only: displayed when a string cannot be converted to an ConverterTestType.
            /// </summary>
            ConverterTestTypeUnknown,

            /// <summary>
            /// Displayed when an XML loading exception has occurred.
            /// </summary>
            XmlException,

            /// <summary>
            /// Displayed when the whitespace preceding a node is wrong.
            /// </summary>
            WhitespacePrecedingNodeWrong,

            /// <summary>
            /// Displayed when the whitespace preceding an end element is wrong.
            /// </summary>
            WhitespacePrecedingEndElementWrong,

            /// Before this point, ignore errors on convert operation
            EndIgnoreInConvert,

            /// <summary>
            /// Displayed when the XML declaration is present in the source file.
            /// </summary>
            DeclarationPresent,

            /// <summary>
            /// Displayed when a file cannot be accessed; typically when trying to save back a fixed file.
            /// </summary>
            UnauthorizedAccessException,

            /// After this point, ignore errors on format operation
            BeginIgnoreInFormat,

            /// <summary>
            /// Displayed when the xmlns attribute is missing from the document element.
            /// </summary>
            XmlnsMissing,

            /// <summary>
            /// Displayed when the xmlns attribute on the document element is wrong.
            /// </summary>
            XmlnsValueWrong,

            /// <summary>
            /// Displayed when inner text contains a deprecated $(loc.xxx) reference.
            /// </summary>
            DeprecatedLocalizationVariablePrefixInTextValue,

            /// <summary>
            /// Displayed when an attribute value contains a deprecated $(loc.xxx) reference.
            /// </summary>
            DeprecatedLocalizationVariablePrefixInAttributeValue,

            /// <summary>
            /// Assign an identifier to a File element when on Id attribute is specified.
            /// </summary>
            AssignAnonymousFileId,

            /// <summary>
            /// SuppressSignatureVerification attribute is obsolete and corresponding functionality removed.
            /// </summary>
            SuppressSignatureVerificationObsolete,

            /// <summary>
            /// WixCA Binary/@Id has been renamed to UtilCA.
            /// </summary>
            WixCABinaryIdRenamed,

            /// <summary>
            /// QtExec custom actions have been renamed.
            /// </summary>
            QuietExecCustomActionsRenamed,

            /// <summary>
            /// QtExecCmdTimeout was previously used for both CAQuietExec and CAQuietExec64. For WixQuietExec, use WixQuietExecCmdTimeout. For WixQuietExec64, use WixQuietExec64CmdTimeout.
            /// </summary>
            QtExecCmdTimeoutAmbiguous,

            /// <summary>
            /// Directory/@ShortName may only be specified with Directory/@Name.
            /// </summary>
            AssignDirectoryNameFromShortName,

            /// <summary>
            /// BootstrapperApplicationData attribute is deprecated and replaced with Unreal for MSI. Use BundleCustomData element for Bundles.
            /// </summary>
            BootstrapperApplicationDataDeprecated,

            /// <summary>
            /// Inheritable is new and is now defaulted to 'yes' which is a change in behavior for all but children of CreateFolder.
            /// </summary>
            AssignPermissionExInheritable,

            /// <summary>
            /// Column element's Category attribute is camel-case.
            /// </summary>
            ColumnCategoryCamelCase,

            /// <summary>
            /// Column element's Modularize attribute is camel-case.
            /// </summary>
            ColumnModularizeCamelCase,

            /// <summary>
            /// Inner text value should move to an attribute.
            /// </summary>
            InnerTextDeprecated,

            /// <summary>
            /// Explicit auto-GUID unnecessary.
            /// </summary>
            AutoGuidUnnecessary,

            /// <summary>
            /// The Feature Absent attribute renamed to AllowAbsent.
            /// </summary>
            FeatureAbsentAttributeReplaced,

            /// <summary>
            /// The Feature AllowAdvertise attribute value deprecated.
            /// </summary>
            FeatureAllowAdvertiseValueDeprecated,

            /// <summary>
            /// The Condition='1' attribute is unnecessary on Publish elements.
            /// </summary>
            PublishConditionOneUnnecessary,

            /// <summary>
            /// DpiAwareness is new and is defaulted to 'perMonitorV2' which is a change in behavior.
            /// </summary>
            AssignBootstrapperApplicationDpiAwareness,

            /// <summary>
            /// The string variable type was previously treated as formatted.
            /// </summary>
            AssignVariableTypeFormatted,

            /// <summary>
            /// The CustomAction attributes have been renamed from BinaryKey and FileKey to BinaryRef and FileRef.
            /// </summary>
            CustomActionKeysAreNowRefs,

            /// <summary>
            /// The Product and Package elements have been renamed and reorganized.
            /// </summary>
            ProductAndPackageRenamed,

            /// <summary>
            /// The Module and Package elements have been renamed and reorganized.
            /// </summary>
            ModuleAndPackageRenamed,

            /// <summary>
            /// A MediaTemplate with no attributes set is now provided by default.
            /// </summary>
            DefaultMediaTemplate,

            /// <summary>
            /// util:RegistrySearch has breaking change when value is missing.
            /// </summary>
            UtilRegistryValueSearchBehaviorChange,

            /// <summary>
            /// DisplayInternalUI can't be converted.
            /// </summary>
            DisplayInternalUiNotConvertable,

            /// <summary>
            /// InstallerVersion has breaking change when omitted.
            /// </summary>
            InstallerVersionBehaviorChange,

            /// <summary>
            /// Verb/@Target can't be converted.
            /// </summary>
            VerbTargetNotConvertable,

            /// <summary>
            /// The bootstrapper application dll is now specified in its own element.
            /// </summary>
            BootstrapperApplicationDll,

            /// <summary>
            /// The new bootstrapper application dll element is required.
            /// </summary>
            BootstrapperApplicationDllRequired,

            /// <summary>
            /// bal:UseUILanguages is deprecated, 'true' is now the standard behavior.
            /// </summary>
            BalUseUILanguagesDeprecated,

            /// <summary>
            /// The custom elements for built-in BAs are now required.
            /// </summary>
            BalBootstrapperApplicationRefToElement,

            /// <summary>
            /// The ExePackage elements "XxxCommand" attributes have been renamed to "XxxArguments".
            /// </summary>
            RenameExePackageCommandToArguments,

            /// <summary>
            /// The Win64 attribute has been renamed. Use the Bitness attribute instead.
            /// </summary>
            Win64AttributeRenamed,

            /// <summary>
            /// Breaking change: The Win64 attribute's value '{0}' cannot be converted automatically to the new Bitness attribute.
            /// </summary>
            Win64AttributeRenameCannotBeAutomatic,

            /// <summary>
            /// The Tag element has been renamed. Use the element 'SoftwareTag' name.
            /// </summary>
            TagElementRenamed,

            /// <summary>
            /// The Dependency namespace has been incorporated into WiX v4 namespace.
            /// </summary>
            IntegratedDependencyNamespace,

            /// <summary>
            /// Remove unused namespaces.
            /// </summary>
            RemoveUnusedNamespaces,

            /// <summary>
            /// The Remote element has been renamed. Use the "XxxPackagePayload" element instead.
            /// </summary>
            RemotePayloadRenamed,

            /// <summary>
            /// The XxxPackage/@Name attribute must be specified on the child XxxPackagePayload element when using a remote payload.
            /// </summary>
            NameAttributeMovedToRemotePayload,

            /// <summary>
            /// The XxxPackage/@Compressed attribute should not be specified when using a remote payload.
            /// </summary>
            CompressedAttributeUnnecessaryForRemotePayload,

            /// <summary>
            /// The XxxPackage/@DownloadUrl attribute must be specified on the child XxxPackagePayload element when using a remote payload.
            /// </summary>
            DownloadUrlAttributeMovedToRemotePayload,

            /// <summary>
            /// The hash algorithm used for bundles changed from SHA1 to SHA512.
            /// </summary>
            BurnHashAlgorithmChanged,

            /// <summary>
            /// CustomTable elements can't always be converted.
            /// </summary>
            CustomTableNotAlwaysConvertable,

            /// <summary>
            /// CustomTable elements that don't contain the table definition are now CustomTableRef.
            /// </summary>
            CustomTableRef,

            /// <summary>
            /// The RegistryKey element's Action attribute is obsolete.
            /// </summary>
            RegistryKeyActionObsolete,

            /// <summary>
            /// The TagRef element has been renamed. Use the element 'SoftwareTagRef' name.
            /// </summary>
            TagRefElementRenamed,

            /// <summary>
            /// The SoftwareTag element's Licensed attribute is obsolete.
            /// </summary>
            SoftwareTagLicensedObsolete,

            /// <summary>
            /// The SoftwareTag element's Type attribute is obsolete.
            /// </summary>
            SoftwareTagTypeObsolete,

            /// <summary>
            /// TARGETDIR directory should no longer be explicitly defined.
            /// </summary>
            TargetDirDeprecated,

            /// <summary>
            /// Standard directories should no longer be defined using the Directory element.
            /// </summary>
            DefiningStandardDirectoryDeprecated,

            /// <summary>
            /// Naked UI, custom action, and property references replaced with elements.
            /// </summary>
            ReferencesReplaced,

            /// <summary>
            /// Cache attribute value updated.
            /// </summary>
            BundlePackageCacheAttributeValueObsolete,

            /// <summary>
            /// The MsuPackage element contains obsolete '{0}' attribute. Windows no longer supports silently removing MSUs so the attribute is unnecessary. The attribute will be removed.
            /// </summary>
            MsuPackageKBObsolete,

            /// <summary>
            /// The MsuPackage element contains obsolete '{0}' attribute. MSU packages are now always permanent because Windows no longer supports silently removing MSUs. The attribute will be removed.
            /// </summary>
            MsuPackagePermanentObsolete,

            /// <summary>
            /// Namespace should be defined on the root. The '{0}' namespace was move to the root element.
            /// </summary>
            MoveNamespacesToRoot,

            /// <summary>
            /// Custom action ids have changed in WiX v4 extensions. Because WiX v4 has platform-specific custom actions, the platform is applied as a suffix: _X86, _X64, _A64 (Arm64). When manually rescheduling custom actions, you must use the new custom action id, with platform suffix.
            /// </summary>
            CustomActionIdsIncludePlatformSuffix,

            /// <summary>
            /// The {0} directory should no longer be explicitly referenced. Remove the DirectoryRef element with Id attribute '{0}'.
            /// </summary>
            StandardDirectoryRefDeprecated,

            /// <summary>
            /// Referencing '{0}' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory.
            /// </summary>
            EmptyStandardDirectoryRefNotConvertable,

            /// <summary>
            /// The magic WixVariable 'WixMbaPrereqLicenseUrl' has been removed. Add bal:PrereqLicenseUrl="yes" to a prereq package instead.
            /// </summary>
            WixMbaPrereqLicenseUrlDeprecated,

            /// <summary>
            /// The magic WixVariable 'WixMbaPrereqPackageId' has been removed. Add bal:PrereqPackage="yes" to the target package instead.
            /// </summary>
            WixMbaPrereqPackageIdDeprecated,

            /// <summary>
            /// A reference to the TARGETDIR Directory was removed. This can cause unintended side effects. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages
            /// </summary>
            TargetDirRefRemoved,

            /// <summary>
            ///  The Certificate BinaryKey element has been renamed to BinaryRef.
            /// </summary>
            CertificateBinaryKeyIsNowBinaryRef,

            /// <summary>
            /// The RelatedBundle element's Action attribute value must now be all lowercase. The Action='{0}' will be converted to '{1}'
            /// </summary>
            RelatedBundleActionLowercase,
        }
    }
}
