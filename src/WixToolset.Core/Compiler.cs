// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Core;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public sealed class Compiler
    {
        public const string UpgradeDetectedProperty = "WIX_UPGRADE_DETECTED";
        public const string UpgradePreventedCondition = "NOT WIX_UPGRADE_DETECTED";
        public const string DowngradeDetectedProperty = "WIX_DOWNGRADE_DETECTED";
        public const string DowngradePreventedCondition = "NOT WIX_DOWNGRADE_DETECTED";
        public const string DefaultComponentIdPlaceholderFormat = "WixComponentIdPlaceholder{0}";
        public const string DefaultComponentIdPlaceholderWixVariableFormat = "!(wix.{0})";
        public const string BurnUXContainerId = "WixUXContainer";
        public const string BurnDefaultAttachedContainerId = "WixAttachedContainer";

        // The following constants must stay in sync with src\burn\engine\core.h
        private const string BURN_BUNDLE_NAME = "WixBundleName";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE = "WixBundleOriginalSource";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = "WixBundleOriginalSourceFolder";
        private const string BURN_BUNDLE_LAST_USED_SOURCE = "WixBundleLastUsedSource";

        private TableDefinitionCollection tableDefinitions;
        private Dictionary<XNamespace, ICompilerExtension> extensions;
        private List<InspectorExtension> inspectorExtensions;
        private CompilerCore core;
        private bool showPedanticMessages;

        // if these are true you know you are building a module or product
        // but if they are false you cannot not be sure they will not end
        // up a product or module.  Use these flags carefully.
        private bool compilingModule;
        private bool compilingProduct;

        private bool useShortFileNames;
        private string activeName;
        private string activeLanguage;

        private WixVariableResolver componentIdPlaceholdersResolver;

        /// <summary>
        /// Creates a new compiler object with a default set of table definitions.
        /// </summary>
        public Compiler()
        {
            this.tableDefinitions = new TableDefinitionCollection(WindowsInstallerStandard.GetTableDefinitions());
            this.extensions = new Dictionary<XNamespace, ICompilerExtension>();
            this.inspectorExtensions = new List<InspectorExtension>();

            this.CurrentPlatform = Platform.X86;
        }

        /// <summary>
        /// Type of RadioButton element in a group.
        /// </summary>
        private enum RadioButtonType
        {
            /// <summary>Not set, yet.</summary>
            NotSet,

            /// <summary>Text</summary>
            Text,

            /// <summary>Bitmap</summary>
            Bitmap,

            /// <summary>Icon</summary>
            Icon,
        }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        public Platform CurrentPlatform { get; set; }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages
        {
            get { return this.showPedanticMessages; }
            set { this.showPedanticMessages = value; }
        }

        /// <summary>
        /// Adds a compiler extension.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        public void AddExtension(ICompilerExtension extension)
        {
            // Check if this extension is adding a schema namespace that already exists.
            ICompilerExtension collidingExtension;
            if (!this.extensions.TryGetValue(extension.Namespace, out collidingExtension))
            {
                this.extensions.Add(extension.Namespace, extension);
            }
            else
            {
                Messaging.Instance.OnMessage(WixErrors.DuplicateExtensionXmlSchemaNamespace(extension.GetType().ToString(), extension.Namespace.NamespaceName, collidingExtension.GetType().ToString()));
            }

            //if (null != extension.InspectorExtension)
            //{
            //    this.inspectorExtensions.Add(extension.InspectorExtension);
            //}
        }

        /// <summary>
        /// Adds table definitions from an extension
        /// </summary>
        /// <param name="extension">Extension with table definitions.</param>
        public void AddExtensionData(IExtensionData extension)
        {
            if (null != extension.TableDefinitions)
            {
                foreach (TableDefinition tableDefinition in extension.TableDefinitions)
                {
                    if (!this.tableDefinitions.Contains(tableDefinition.Name))
                    {
                        this.tableDefinitions.Add(tableDefinition);
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicateExtensionTable(extension.GetType().ToString(), tableDefinition.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Compiles the provided Xml document into an intermediate object
        /// </summary>
        /// <param name="source">Source xml document to compile.  The BaseURI property
        /// should be properly set to get messages containing source line information.</param>
        /// <returns>Intermediate object representing compiled source document.</returns>
        /// <remarks>This method is not thread-safe.</remarks>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Intermediate Compile(XDocument source)
        {
            if (null == source) throw new ArgumentNullException(nameof(source));

            bool encounteredError = false;

            // create the intermediate
            Intermediate target = new Intermediate();

            // try to compile it
            try
            {
                this.core = new CompilerCore(target, this.tableDefinitions, this.extensions);
                this.core.ShowPedanticMessages = this.showPedanticMessages;
                this.core.CurrentPlatform = this.CurrentPlatform;
                this.componentIdPlaceholdersResolver = new WixVariableResolver();

                foreach (CompilerExtension extension in this.extensions.Values)
                {
                    extension.Core = this.core;
                    extension.Initialize();
                }

                // parse the document
                SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(source.Root);
                if ("Wix" == source.Root.Name.LocalName)
                {
                    if (CompilerCore.WixNamespace == source.Root.Name.Namespace)
                    {
                        this.ParseWixElement(source.Root);
                    }
                    else // invalid or missing namespace
                    {
                        if (String.IsNullOrEmpty(source.Root.Name.NamespaceName))
                        {
                            this.core.OnMessage(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, "Wix", CompilerCore.WixNamespace.ToString()));
                        }
                        else
                        {
                            this.core.OnMessage(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, "Wix", source.Root.Name.NamespaceName, CompilerCore.WixNamespace.ToString()));
                        }
                    }
                }
                else
                {
                    this.core.OnMessage(WixErrors.InvalidDocumentElement(sourceLineNumbers, source.Root.Name.LocalName, "source", "Wix"));
                }

                // Resolve any Component Id placeholders compiled into the intermediate.
                if (0 < this.componentIdPlaceholdersResolver.VariableCount)
                {
                    foreach (var section in target.Sections)
                    {
                        foreach (Table table in section.Tables)
                        {
                            foreach (Row row in table.Rows)
                            {
                                foreach (Field field in row.Fields)
                                {
                                    if (field.Data is string)
                                    {
                                        field.Data = this.componentIdPlaceholdersResolver.ResolveVariables(row.SourceLineNumbers, (string)field.Data, false, false, out var defaultIgnored, out var delayedIgnored);
                                    }
                                }
                            }
                        }
                    }
                }

                // inspect the document
                InspectorCore inspectorCore = new InspectorCore();
                foreach (InspectorExtension inspectorExtension in this.inspectorExtensions)
                {
                    inspectorExtension.Core = inspectorCore;
                    inspectorExtension.InspectIntermediate(target);

                    // reset
                    inspectorExtension.Core = null;
                }

                if (inspectorCore.EncounteredError)
                {
                    encounteredError = true;
                }
            }
            finally
            {
                if (this.core.EncounteredError)
                {
                    encounteredError = true;
                }

                foreach (CompilerExtension extension in this.extensions.Values)
                {
                    extension.Finish();
                    extension.Core = null;
                }
                this.core = null;
            }

            // return the compiled intermediate only if it completed successfully
            return (encounteredError ? null : target);
        }

        /// <summary>
        /// Uppercases the first character of a string.
        /// </summary>
        /// <param name="s">String to uppercase first character of.</param>
        /// <returns>String with first character uppercased.</returns>
        private static string UppercaseFirstChar(string s)
        {
            if (0 == s.Length)
            {
                return s;
            }

            return String.Concat(s.Substring(0, 1).ToUpper(CultureInfo.InvariantCulture), s.Substring(1));
        }

        /// <summary>
        /// Lowercases the string if present.
        /// </summary>
        /// <param name="s">String to lowercase.</param>
        /// <returns>Null if the string is null, otherwise returns the lowercase.</returns>
        private static string LowercaseOrNull(string s)
        {
            return (null == s) ? s : s.ToLowerInvariant();
        }

        /// <summary>
        /// Given a possible short and long file name, create an msi filename value.
        /// </summary>
        /// <param name="shortName">The short file name.</param>
        /// <param name="longName">Possibly the long file name.</param>
        /// <returns>The value in the msi filename data type.</returns>
        private string GetMsiFilenameValue(string shortName, string longName)
        {
            if (null != shortName && null != longName && !String.Equals(shortName, longName, StringComparison.OrdinalIgnoreCase))
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}|{1}", shortName, longName);
            }
            else
            {
                if (this.core.IsValidShortFilename(longName, false))
                {
                    return longName;
                }
                else
                {
                    return shortName;
                }
            }
        }

        /// <summary>
        /// Adds a search property to the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Current source/line number of processing.</param>
        /// <param name="property">Property to add to search.</param>
        /// <param name="signature">Signature for search.</param>
        private void AddAppSearch(SourceLineNumber sourceLineNumbers, Identifier property, string signature)
        {
            if (!this.core.EncounteredError)
            {
                if (property.Id != property.Id.ToUpperInvariant())
                {
                    this.core.OnMessage(WixErrors.SearchPropertyNotUppercase(sourceLineNumbers, "Property", "Id", property.Id));
                }

                Row row = this.core.CreateRow(sourceLineNumbers, "AppSearch", property);
                row[1] = signature;
            }
        }

        /// <summary>
        /// Adds a property to the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Current source/line number of processing.</param>
        /// <param name="property">Name of property to add.</param>
        /// <param name="value">Value of property.</param>
        /// <param name="admin">Flag if property is an admin property.</param>
        /// <param name="secure">Flag if property is a secure property.</param>
        /// <param name="hidden">Flag if property is to be hidden.</param>
        /// <param name="fragment">Adds the property to a new section.</param>
        private void AddProperty(SourceLineNumber sourceLineNumbers, Identifier property, string value, bool admin, bool secure, bool hidden, bool fragment)
        {
            // properties without a valid identifier should not be processed any further
            if (null == property || String.IsNullOrEmpty(property.Id))
            {
                return;
            }

            if (!String.IsNullOrEmpty(value))
            {
                Regex regex = new Regex(@"\[(?<identifier>[a-zA-Z_][a-zA-Z0-9_\.]*)]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                MatchCollection matches = regex.Matches(value);

                foreach (Match match in matches)
                {
                    Group group = match.Groups["identifier"];
                    if (group.Success)
                    {
                        this.core.OnMessage(WixWarnings.PropertyValueContainsPropertyReference(sourceLineNumbers, property.Id, group.Value));
                    }
                }
            }

            if (!this.core.EncounteredError)
            {
                Section section = this.core.ActiveSection;

                // Add the row to a separate section if requested.
                if (fragment)
                {
                    string id = String.Concat(this.core.ActiveSection.Id, ".", property.Id);

                    section = this.core.CreateSection(id, SectionType.Fragment, this.core.ActiveSection.Codepage);

                    // Reference the property in the active section.
                    this.core.CreateSimpleReference(sourceLineNumbers, "Property", property.Id);
                }

                Row row = this.core.CreateRow(sourceLineNumbers, "Property", section, property);

                // Allow row to exist with no value so that PropertyRefs can be made for *Search elements
                // the linker will remove these rows before the final output is created.
                if (null != value)
                {
                    row[1] = value;
                }

                if (admin || hidden || secure)
                {
                    this.AddWixPropertyRow(sourceLineNumbers, property, admin, secure, hidden, section);
                }
            }
        }

        private WixPropertyRow AddWixPropertyRow(SourceLineNumber sourceLineNumbers, Identifier property, bool admin, bool secure, bool hidden, Section section = null)
        {
            if (secure && property.Id != property.Id.ToUpperInvariant())
            {
                this.core.OnMessage(WixErrors.SecurePropertyNotUppercase(sourceLineNumbers, "Property", "Id", property.Id));
            }

            if (null == section)
            {
                section = this.core.ActiveSection;

                this.core.EnsureTable(sourceLineNumbers, "Property"); // Property table is always required when using WixProperty table.
            }

            WixPropertyRow row = (WixPropertyRow)this.core.CreateRow(sourceLineNumbers, "WixProperty", section, property);
            row.Admin = admin;
            row.Hidden = hidden;
            row.Secure = secure;

            return row;
        }

        /// <summary>
        /// Adds a "implemented category" registry key to active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Current source/line number of processing.</param>
        /// <param name="categoryId">GUID for category.</param>
        /// <param name="classId">ClassId for to mark "implemented".</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void RegisterImplementedCategories(SourceLineNumber sourceLineNumbers, string categoryId, string classId, string componentId)
        {
            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Implemented Categories\\", categoryId), "*", null, componentId);
        }

        /// <summary>
        /// Parses an application identifer element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="advertise">The required advertise state (set depending upon the parent).</param>
        /// <param name="fileServer">Optional file identifier for CLSID when not advertised.</param>
        /// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
        /// <param name="typeLibVersion">Optional TypeLib Version for CLSID Interfaces (if any).</param>
        private void ParseAppIdElement(XElement node, string componentId, YesNoType advertise, string fileServer, string typeLibId, string typeLibVersion)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string appId = null;
            string remoteServerName = null;
            string localService = null;
            string serviceParameters = null;
            string dllSurrogate = null;
            YesNoType activateAtStorage = YesNoType.NotSet;
            YesNoType appIdAdvertise = YesNoType.NotSet;
            YesNoType runAsInteractiveUser = YesNoType.NotSet;
            string description = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            appId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ActivateAtStorage":
                            activateAtStorage = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            appIdAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DllSurrogate":
                            dllSurrogate = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "LocalService":
                            localService = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RemoteServerName":
                            remoteServerName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RunAsInteractiveUser":
                            runAsInteractiveUser = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceParameters":
                            serviceParameters = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == appId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == appIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == appIdAdvertise))
            {
                this.core.OnMessage(WixErrors.AppIdIncompatibleAdvertiseState(sourceLineNumbers, node.Name.LocalName, "Advertise", appIdAdvertise.ToString(), advertise.ToString()));
            }
            else
            {
                advertise = appIdAdvertise;
            }

            // if the advertise state has not been set, default to non-advertised
            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Class":
                            this.ParseClassElement(child, componentId, advertise, fileServer, typeLibId, typeLibVersion, appId);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (YesNoType.Yes == advertise)
            {
                if (null != description)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "Description"));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "AppId");
                    row[0] = appId;
                    row[1] = remoteServerName;
                    row[2] = localService;
                    row[3] = serviceParameters;
                    row[4] = dllSurrogate;
                    if (YesNoType.Yes == activateAtStorage)
                    {
                        row[5] = 1;
                    }

                    if (YesNoType.Yes == runAsInteractiveUser)
                    {
                        row[6] = 1;
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (null != description)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), null, description, componentId);
                }
                else
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "+", null, componentId);
                }

                if (null != remoteServerName)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RemoteServerName", remoteServerName, componentId);
                }

                if (null != localService)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "LocalService", localService, componentId);
                }

                if (null != serviceParameters)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ServiceParameters", serviceParameters, componentId);
                }

                if (null != dllSurrogate)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "DllSurrogate", dllSurrogate, componentId);
                }

                if (YesNoType.Yes == activateAtStorage)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ActivateAtStorage", "Y", componentId);
                }

                if (YesNoType.Yes == runAsInteractiveUser)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RunAs", "Interactive User", componentId);
                }
            }
        }

        /// <summary>
        /// Parses an AssemblyName element.
        /// </summary>
        /// <param name="node">File element to parse.</param>
        /// <param name="componentId">Parent's component id.</param>
        private void ParseAssemblyName(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiAssemblyName");
                row[0] = componentId;
                row[1] = id;
                row[2] = value;
            }
        }

        /// <summary>
        /// Parses a binary element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Identifier for the new row.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private Identifier ParseBinaryElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;
            YesNoType suppressModularization = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile", "src"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (!String.IsNullOrEmpty(id.Id)) // only check legal values
            {
                if (55 < id.Id.Length)
                {
                    this.core.OnMessage(WixErrors.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 55));
                }
                else if (!this.compilingProduct) // if we're not doing a product then we can't be sure that a binary identifier will fit when modularized
                {
                    if (18 < id.Id.Length)
                    {
                        this.core.OnMessage(WixWarnings.IdentifierCannotBeModularized(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 18));
                    }
                }
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Binary", id);
                row[1] = sourceFile;

                if (YesNoType.Yes == suppressModularization)
                {
                    Row wixSuppressModularizationRow = this.core.CreateRow(sourceLineNumbers, "WixSuppressModularization");
                    wixSuppressModularizationRow[0] = id;
                }
            }

            return id;
        }

        /// <summary>
        /// Parses an icon element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Identifier for the new row.</returns>
        private string ParseIconElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (!String.IsNullOrEmpty(id.Id)) // only check legal values
            {
                if (57 < id.Id.Length)
                {
                    this.core.OnMessage(WixErrors.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 57));
                }
                else if (!this.compilingProduct) // if we're not doing a product then we can't be sure that a binary identifier will fit when modularized
                {
                    if (20 < id.Id.Length)
                    {
                        this.core.OnMessage(WixWarnings.IdentifierCannotBeModularized(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 20));
                    }
                }
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Icon", id);
                row[1] = sourceFile;
            }

            return id.Id;
        }

        /// <summary>
        /// Parses an InstanceTransforms element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseInstanceTransformsElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string property = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Property":
                            property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Property", property);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            // find unexpected child elements
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Instance":
                            ParseInstanceElement(child, property);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses an instance element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of instance property.</param>
        private void ParseInstanceElement(XElement node, string propertyId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string productCode = null;
            string productName = null;
            string upgradeCode = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "ProductName":
                            productName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == productCode)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductCode"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixInstanceTransforms");
                row[0] = id;
                row[1] = propertyId;
                row[2] = productCode;
                if (null != productName)
                {
                    row[3] = productName;
                }
                if (null != upgradeCode)
                {
                    row[4] = upgradeCode;
                }
            }
        }

        /// <summary>
        /// Parses a category element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseCategoryElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string appData = null;
            string feature = null;
            string qualifier = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "AppData":
                            appData = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                            break;
                        case "Qualifier":
                            qualifier = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == qualifier)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Qualifier"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "PublishComponent");
                row[0] = id;
                row[1] = qualifier;
                row[2] = componentId;
                row[3] = appData;
                if (null == feature)
                {
                    row[4] = Guid.Empty.ToString("B");
                }
                else
                {
                    row[4] = feature;
                }
            }
        }

        /// <summary>
        /// Parses a class element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="advertise">Optional Advertise State for the parent AppId element (if any).</param>
        /// <param name="fileServer">Optional file identifier for CLSID when not advertised.</param>
        /// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
        /// <param name="typeLibVersion">Optional TypeLib Version for CLSID Interfaces (if any).</param>
        /// <param name="parentAppId">Optional parent AppId.</param>
        private void ParseClassElement(XElement node, string componentId, YesNoType advertise, string fileServer, string typeLibId, string typeLibVersion, string parentAppId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string appId = null;
            string argument = null;
            bool class16bit = false;
            bool class32bit = false;
            string classId = null;
            YesNoType classAdvertise = YesNoType.NotSet;
            string[] contexts = null;
            string formattedContextString = null;
            bool control = false;
            string defaultInprocHandler = null;
            string defaultProgId = null;
            string description = null;
            string fileTypeMask = null;
            string foreignServer = null;
            string icon = null;
            int iconIndex = CompilerConstants.IntegerNotSet;
            string insertable = null;
            string localFileServer = null;
            bool programmable = false;
            YesNoType relativePath = YesNoType.NotSet;
            bool safeForInit = false;
            bool safeForScripting = false;
            bool shortServerPath = false;
            string threadingModel = null;
            string version = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            classId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Advertise":
                            classAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AppId":
                            appId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Argument":
                            argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Context":
                            contexts = this.core.GetAttributeValue(sourceLineNumbers, attrib).Split("\r\n\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            break;
                        case "Control":
                            control = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Handler":
                            defaultInprocHandler = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Icon":
                            icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "IconIndex":
                            iconIndex = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "RelativePath":
                            relativePath = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;

                        // The following attributes result in rows always added to the Registry table rather than the Class table
                        case "Insertable":
                            insertable = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? "Insertable" : "NotInsertable";
                            break;
                        case "Programmable":
                            programmable = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SafeForInitializing":
                            safeForInit = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SafeForScripting":
                            safeForScripting = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ForeignServer":
                            foreignServer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Server":
                            localFileServer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortPath":
                            shortServerPath = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ThreadingModel":
                            threadingModel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == classId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            HashSet<string> uniqueContexts = new HashSet<string>();
            foreach (string context in contexts)
            {
                if (uniqueContexts.Contains(context))
                {
                    this.core.OnMessage(WixErrors.DuplicateContextValue(sourceLineNumbers, context));
                }
                else
                {
                    uniqueContexts.Add(context);
                }

                if (context.EndsWith("32", StringComparison.Ordinal))
                {
                    class32bit = true;
                }
                else
                {
                    class16bit = true;
                }
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == classAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == classAdvertise))
            {
                this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, classAdvertise.ToString(), advertise.ToString()));
            }
            else
            {
                advertise = classAdvertise;
            }

            // If the advertise state has not been set, default to non-advertised.
            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            if (YesNoType.Yes == advertise && 0 == contexts.Length)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Context", "Advertise", "yes"));
            }

            if (!String.IsNullOrEmpty(parentAppId) && !String.IsNullOrEmpty(appId))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "AppId", node.Parent.Name.LocalName));
            }

            if (!String.IsNullOrEmpty(localFileServer))
            {
                this.core.CreateSimpleReference(sourceLineNumbers, "File", localFileServer);
            }

            // Local variables used strictly for child node processing.
            int fileTypeMaskIndex = 0;
            YesNoType firstProgIdForClass = YesNoType.Yes;

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "FileTypeMask":
                            if (YesNoType.Yes == advertise)
                            {
                                fileTypeMask = String.Concat(fileTypeMask, null == fileTypeMask ? String.Empty : ";", this.ParseFileTypeMaskElement(child));
                            }
                            else if (YesNoType.No == advertise)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.CreateRegistryRow(childSourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("FileType\\", classId, "\\", fileTypeMaskIndex.ToString()), String.Empty, this.ParseFileTypeMaskElement(child), componentId);
                                fileTypeMaskIndex++;
                            }
                            break;
                        case "Interface":
                            this.ParseInterfaceElement(child, componentId, class16bit ? classId : null, class32bit ? classId : null, typeLibId, typeLibVersion);
                            break;
                        case "ProgId":
                            {
                                bool foundExtension = false;
                                string progId = this.ParseProgIdElement(child, componentId, advertise, classId, description, null, ref foundExtension, firstProgIdForClass);
                                if (null == defaultProgId)
                                {
                                    defaultProgId = progId;
                                }
                                firstProgIdForClass = YesNoType.No;
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            // If this Class is being advertised.
            if (YesNoType.Yes == advertise)
            {
                if (null != fileServer || null != localFileServer)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Server", "Advertise", "yes"));
                }

                if (null != foreignServer)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Advertise", "yes"));
                }

                if (null == appId && null != parentAppId)
                {
                    appId = parentAppId;
                }

                // add a Class row for each context
                if (!this.core.EncounteredError)
                {
                    foreach (string context in contexts)
                    {
                        Row row = this.core.CreateRow(sourceLineNumbers, "Class");
                        row[0] = classId;
                        row[1] = context;
                        row[2] = componentId;
                        row[3] = defaultProgId;
                        row[4] = description;
                        if (null != appId)
                        {
                            row[5] = appId;
                            this.core.CreateSimpleReference(sourceLineNumbers, "AppId", appId);
                        }
                        row[6] = fileTypeMask;
                        if (null != icon)
                        {
                            row[7] = icon;
                            this.core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                        }
                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            row[8] = iconIndex;
                        }
                        row[9] = defaultInprocHandler;
                        row[10] = argument;
                        row[11] = Guid.Empty.ToString("B");
                        if (YesNoType.Yes == relativePath)
                        {
                            row[12] = MsiInterop.MsidbClassAttributesRelativePath;
                        }
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (null == fileServer && null == localFileServer && null == foreignServer)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Server"));
                }

                if (null != fileServer && null != foreignServer)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "File"));
                }
                else if (null != localFileServer && null != foreignServer)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Server"));
                }
                else if (null == fileServer)
                {
                    fileServer = localFileServer;
                }

                if (null != appId) // need to use nesting (not a reference) for the unadvertised Class elements
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "AppId", "Advertise", "no"));
                }

                // add the core registry keys for each context in the class
                foreach (string context in contexts)
                {
                    if (context.StartsWith("InprocServer", StringComparison.Ordinal)) // dll server
                    {
                        if (null != argument)
                        {
                            this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Arguments", "Context", context));
                        }

                        if (null != fileServer)
                        {
                            formattedContextString = String.Concat("[", shortServerPath ? "!" : "#", fileServer, "]");
                        }
                        else if (null != foreignServer)
                        {
                            formattedContextString = foreignServer;
                        }
                    }
                    else if (context.StartsWith("LocalServer", StringComparison.Ordinal)) // exe server (quote the long path)
                    {
                        if (null != fileServer)
                        {
                            if (shortServerPath)
                            {
                                formattedContextString = String.Concat("[!", fileServer, "]");
                            }
                            else
                            {
                                formattedContextString = String.Concat("\"[#", fileServer, "]\"");
                            }
                        }
                        else if (null != foreignServer)
                        {
                            formattedContextString = foreignServer;
                        }

                        if (null != argument)
                        {
                            formattedContextString = String.Concat(formattedContextString, " ", argument);
                        }
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Context", context, "InprocServer", "InprocServer32", "LocalServer", "LocalServer32"));
                    }

                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context), String.Empty, formattedContextString, componentId); // ClassId context

                    if (null != icon) // ClassId default icon
                    {
                        this.core.CreateSimpleReference(sourceLineNumbers, "File", icon);

                        icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            icon = String.Concat(icon, ",", iconIndex);
                        }
                        this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context, "\\DefaultIcon"), String.Empty, icon, componentId);
                    }
                }

                if (null != parentAppId) // ClassId AppId (must be specified via nesting, not with the AppId attribute)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), "AppID", parentAppId, componentId);
                }

                if (null != description) // ClassId description
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), String.Empty, description, componentId);
                }

                if (null != defaultInprocHandler)
                {
                    switch (defaultInprocHandler) // ClassId Default Inproc Handler
                    {
                        case "1":
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
                            break;
                        case "2":
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                            break;
                        case "3":
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                            break;
                        default:
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, defaultInprocHandler, componentId);
                            break;
                    }
                }

                if (YesNoType.NotSet != relativePath) // ClassId's RelativePath
                {
                    this.core.OnMessage(WixErrors.RelativePathForRegistryElement(sourceLineNumbers));
                }
            }

            if (null != threadingModel)
            {
                threadingModel = Compiler.UppercaseFirstChar(threadingModel);

                // add a threading model for each context in the class
                foreach (string context in contexts)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context), "ThreadingModel", threadingModel, componentId);
                }
            }

            if (null != typeLibId)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\TypeLib"), null, typeLibId, componentId);
            }

            if (null != version)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Version"), null, version, componentId);
            }

            if (null != insertable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", insertable), "*", null, componentId);
            }

            if (control)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Control"), "*", null, componentId);
            }

            if (programmable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Programmable"), "*", null, componentId);
            }

            if (safeForInit)
            {
                this.RegisterImplementedCategories(sourceLineNumbers, "{7DD95802-9882-11CF-9FA9-00AA006C42C4}", classId, componentId);
            }

            if (safeForScripting)
            {
                this.RegisterImplementedCategories(sourceLineNumbers, "{7DD95801-9882-11CF-9FA9-00AA006C42C4}", classId, componentId);
            }
        }

        /// <summary>
        /// Parses an Interface element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="proxyId">16-bit proxy for interface.</param>
        /// <param name="proxyId32">32-bit proxy for interface.</param>
        /// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
        /// <param name="typelibVersion">Version of the TypeLib to which this interface belongs.  Required if typeLibId is specified</param>
        private void ParseInterfaceElement(XElement node, string componentId, string proxyId, string proxyId32, string typeLibId, string typelibVersion)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string baseInterface = null;
            string interfaceId = null;
            string name = null;
            int numMethods = CompilerConstants.IntegerNotSet;
            bool versioned = true;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            interfaceId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "BaseInterface":
                            baseInterface = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "NumMethods":
                            numMethods = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "ProxyStubClassId":
                            proxyId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProxyStubClassId32":
                            proxyId32 = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Versioned":
                            versioned = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == interfaceId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.core.ParseForExtensionElements(node);

            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId), null, name, componentId);
            if (null != typeLibId)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), null, typeLibId, componentId);
                if (versioned)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), "Version", typelibVersion, componentId);
                }
            }

            if (null != baseInterface)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\BaseInterface"), null, baseInterface, componentId);
            }

            if (CompilerConstants.IntegerNotSet != numMethods)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\NumMethods"), null, numMethods.ToString(), componentId);
            }

            if (null != proxyId)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid"), null, proxyId, componentId);
            }

            if (null != proxyId32)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid32"), null, proxyId32, componentId);
            }
        }

        /// <summary>
        /// Parses a CLSID's file type mask element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>String representing the file type mask elements.</returns>
        private string ParseFileTypeMaskElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int cb = 0;
            int offset = CompilerConstants.IntegerNotSet;
            string mask = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Mask":
                            mask = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Offset":
                            offset = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }


            if (null == mask)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Mask"));
            }

            if (CompilerConstants.IntegerNotSet == offset)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Offset"));
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                if (mask.Length != value.Length)
                {
                    this.core.OnMessage(WixErrors.ValueAndMaskMustBeSameLength(sourceLineNumbers));
                }
                cb = mask.Length / 2;
            }

            return String.Concat(offset.ToString(CultureInfo.InvariantCulture.NumberFormat), ",", cb.ToString(CultureInfo.InvariantCulture.NumberFormat), ",", mask, ",", value);
        }

        /// <summary>
        /// Parses a product search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private void ParseProductSearchElement(XElement node, string propertyId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string upgradeCode = null;
            string language = null;
            string maximum = null;
            string minimum = null;
            int options = MsiInterop.MsidbUpgradeAttributesVersionMinInclusive | MsiInterop.MsidbUpgradeAttributesOnlyDetect;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ExcludeLanguages":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesLanguagesExclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                            }
                            break;
                        case "IncludeMinimum": // this is "yes" by default
                            if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                            }
                            break;
                        case "Language":
                            language = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minimum = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maximum = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == minimum && null == maximum)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Minimum", "Maximum"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Upgrade");
                row[0] = upgradeCode;
                row[1] = minimum;
                row[2] = maximum;
                row[3] = language;
                row[4] = options;
                row[6] = propertyId;
            }
        }

        /// <summary>
        /// Parses a registry search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private string ParseRegistrySearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool explicitWin64 = false;
            Identifier id = null;
            string key = null;
            string name = null;
            string signature = null;
            int root = CompilerConstants.IntegerNotSet;
            int type = CompilerConstants.IntegerNotSet;
            bool search64bit = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                Wix.RegistrySearch.TypeType typeType = Wix.RegistrySearch.ParseTypeType(typeValue);
                                switch (typeType)
                                {
                                    case Wix.RegistrySearch.TypeType.directory:
                                        type = 0;
                                        break;
                                    case Wix.RegistrySearch.TypeType.file:
                                        type = 1;
                                        break;
                                    case Wix.RegistrySearch.TypeType.raw:
                                        type = 2;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "raw"));
                                        break;
                                }
                            }
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            search64bit = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!explicitWin64 && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                search64bit = true;
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("reg", root.ToString(), key, name, type.ToString(), search64bit.ToString());
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (CompilerConstants.IntegerNotSet == type)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }

            signature = id.Id;
            bool oneChild = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "RegLocator", id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = search64bit ? (type | 16) : type;
            }

            return signature;
        }

        /// <summary>
        /// Parses a registry search reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature of referenced search element.</returns>
        private string ParseRegistrySearchRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "RegLocator", id);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            return id; // the id of the RegistrySearchRef element is its signature
        }

        /// <summary>
        /// Parses child elements for search signatures.
        /// </summary>
        /// <param name="node">Node whose children we are parsing.</param>
        /// <returns>Returns list of string signatures.</returns>
        private List<string> ParseSearchSignatures(XElement node)
        {
            List<string> signatures = new List<string>();

            foreach (XElement child in node.Elements())
            {
                string signature = null;
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComplianceDrive":
                            signature = this.ParseComplianceDriveElement(child);
                            break;
                        case "ComponentSearch":
                            signature = this.ParseComponentSearchElement(child);
                            break;
                        case "DirectorySearch":
                            signature = this.ParseDirectorySearchElement(child, String.Empty);
                            break;
                        case "DirectorySearchRef":
                            signature = this.ParseDirectorySearchRefElement(child, String.Empty);
                            break;
                        case "IniFileSearch":
                            signature = this.ParseIniFileSearchElement(child);
                            break;
                        case "ProductSearch":
                            // handled in ParsePropertyElement
                            break;
                        case "RegistrySearch":
                            signature = this.ParseRegistrySearchElement(child);
                            break;
                        case "RegistrySearchRef":
                            signature = this.ParseRegistrySearchRefElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }


                if (!String.IsNullOrEmpty(signature))
                {
                    signatures.Add(signature);
                }
            }

            return signatures;
        }

        /// <summary>
        /// Parses a compliance drive element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature of nested search elements.</returns>
        private string ParseComplianceDriveElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string signature = null;

            bool oneChild = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, "CCP_DRIVE");
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, "CCP_DRIVE");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null == signature)
            {
                this.core.OnMessage(WixErrors.SearchElementRequired(sourceLineNumbers, node.Name.LocalName));
            }

            return signature;
        }

        /// <summary>
        /// Parses a compilance check element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseComplianceCheckElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            string signature = null;

            // see if this property is used for appSearch
            List<string> signatures = this.ParseSearchSignatures(node);
            foreach (string sig in signatures)
            {
                // if we haven't picked a signature for this ComplianceCheck pick
                // this one
                if (null == signature)
                {
                    signature = sig;
                }
                else if (signature != sig)
                {
                    // all signatures under a ComplianceCheck must be the same
                    this.core.OnMessage(WixErrors.MultipleIdentifiersFound(sourceLineNumbers, node.Name.LocalName, sig, signature));
                }
            }

            if (null == signature)
            {
                this.core.OnMessage(WixErrors.SearchElementRequired(sourceLineNumbers, node.Name.LocalName));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CCPSearch");
                row[0] = signature;
            }
        }

        /// <summary>
        /// Parses a component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Type of component's complex reference parent.  Will be Uknown if there is no parent.</param>
        /// <param name="parentId">Optional identifier for component's primary parent.</param>
        /// <param name="parentLanguage">Optional string for component's parent's language.</param>
        /// <param name="diskId">Optional disk id inherited from parent directory.</param>
        /// <param name="directoryId">Optional identifier for component's directory.</param>
        /// <param name="srcPath">Optional source path for files up to this point.</param>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseComponentElement(XElement node, ComplexReferenceParentType parentType, string parentId, string parentLanguage, int diskId, string directoryId, string srcPath)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            int bits = 0;
            int comPlusBits = CompilerConstants.IntegerNotSet;
            string condition = null;
            bool encounteredODBCDataSource = false;
            bool explicitWin64 = false;
            int files = 0;
            string guid = "*";
            string componentIdPlaceholder = String.Format(Compiler.DefaultComponentIdPlaceholderFormat, this.componentIdPlaceholdersResolver.VariableCount); // placeholder id for defaulting Component/@Id to keypath id.
            string componentIdPlaceholderWixVariable = String.Format(Compiler.DefaultComponentIdPlaceholderWixVariableFormat, componentIdPlaceholder);
            Identifier id = new Identifier(componentIdPlaceholderWixVariable, AccessModifier.Private);
            int keyBits = 0;
            bool keyFound = false;
            string keyPath = null;
            bool shouldAddCreateFolder = false;
            bool win64 = false;
            bool multiInstance = false;
            List<string> symbols = new List<string>();
            string feature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ComPlusFlags":
                            comPlusBits = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "DisableRegistryReflection":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesDisableRegistryReflection;
                            }
                            break;
                        case "Directory":
                            directoryId = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Feature":
                            feature = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            guid = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true, true);
                            break;
                        case "KeyPath":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                keyFound = true;
                                keyPath = null;
                                keyBits = 0;
                                shouldAddCreateFolder = true;
                            }
                            break;
                        case "Location":
                            string location = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < location.Length)
                            {
                                Wix.Component.LocationType locationType = Wix.Component.ParseLocationType(location);
                                switch (locationType)
                                {
                                    case Wix.Component.LocationType.either:
                                        bits |= MsiInterop.MsidbComponentAttributesOptional;
                                        break;
                                    case Wix.Component.LocationType.local: // this is the default
                                        break;
                                    case Wix.Component.LocationType.source:
                                        bits |= MsiInterop.MsidbComponentAttributesSourceOnly;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "either", "local", "source"));
                                        break;
                                }
                            }
                            break;
                        case "MultiInstance":
                            multiInstance = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "NeverOverwrite":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesNeverOverwrite;
                            }
                            break;
                        case "Permanent":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesPermanent;
                            }
                            break;
                        case "Shared":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesShared;
                            }
                            break;
                        case "SharedDllRefCount":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesSharedDllRefCount;
                            }
                            break;
                        case "Transitive":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesTransitive;
                            }
                            break;
                        case "UninstallWhenSuperseded":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesUninstallOnSupersedence;
                            }
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributes64bit;
                                win64 = true;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!explicitWin64 && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                bits |= MsiInterop.MsidbComponentAttributes64bit;
                win64 = true;
            }

            if (null == directoryId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Directory"));
            }

            if (String.IsNullOrEmpty(guid) && MsiInterop.MsidbComponentAttributesShared == (bits & MsiInterop.MsidbComponentAttributesShared))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Shared", "yes", "Guid", ""));
            }

            if (String.IsNullOrEmpty(guid) && MsiInterop.MsidbComponentAttributesPermanent == (bits & MsiInterop.MsidbComponentAttributesPermanent))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Permanent", "yes", "Guid", ""));
            }

            if (null != feature)
            {
                if (this.compilingModule)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeInMergeModule(sourceLineNumbers, node.Name.LocalName, "Feature"));
                }
                else
                {
                    if (ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.FeatureGroup == parentType)
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Feature", node.Parent.Name.LocalName));
                    }
                    else
                    {
                        this.core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, null, ComplexReferenceChildType.Component, id.Id, true);
                    }
                }
            }

            foreach (XElement child in node.Elements())
            {
                YesNoType keyPathSet = YesNoType.NotSet;
                string keyPossible = null;
                int keyBit = 0;

                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "AppId":
                            this.ParseAppIdElement(child, id.Id, YesNoType.NotSet, null, null, null);
                            break;
                        case "Category":
                            this.ParseCategoryElement(child, id.Id);
                            break;
                        case "Class":
                            this.ParseClassElement(child, id.Id, YesNoType.NotSet, null, null, null, null);
                            break;
                        case "Condition":
                            if (null != condition)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            condition = this.ParseConditionElement(child, node.Name.LocalName, null, null);
                            break;
                        case "CopyFile":
                            this.ParseCopyFileElement(child, id.Id, null);
                            break;
                        case "CreateFolder":
                            string createdFolder = this.ParseCreateFolderElement(child, id.Id, directoryId, win64);
                            if (directoryId == createdFolder)
                            {
                                shouldAddCreateFolder = false;
                            }
                            break;
                        case "Environment":
                            this.ParseEnvironmentElement(child, id.Id);
                            break;
                        case "Extension":
                            this.ParseExtensionElement(child, id.Id, YesNoType.NotSet, null);
                            break;
                        case "File":
                            keyPathSet = this.ParseFileElement(child, id.Id, directoryId, diskId, srcPath, out keyPossible, win64, guid);
                            if (null != keyPossible)
                            {
                                keyBit = 0;
                            }
                            files++;
                            break;
                        case "IniFile":
                            this.ParseIniFileElement(child, id.Id);
                            break;
                        case "Interface":
                            this.ParseInterfaceElement(child, id.Id, null, null, null, null);
                            break;
                        case "IsolateComponent":
                            this.ParseIsolateComponentElement(child, id.Id);
                            break;
                        case "ODBCDataSource":
                            keyPathSet = this.ParseODBCDataSource(child, id.Id, null, out keyPossible);
                            keyBit = MsiInterop.MsidbComponentAttributesODBCDataSource;
                            encounteredODBCDataSource = true;
                            break;
                        case "ODBCDriver":
                            this.ParseODBCDriverOrTranslator(child, id.Id, null, this.tableDefinitions["ODBCDriver"]);
                            break;
                        case "ODBCTranslator":
                            this.ParseODBCDriverOrTranslator(child, id.Id, null, this.tableDefinitions["ODBCTranslator"]);
                            break;
                        case "ProgId":
                            bool foundExtension = false;
                            this.ParseProgIdElement(child, id.Id, YesNoType.NotSet, null, null, null, ref foundExtension, YesNoType.NotSet);
                            break;
                        case "RegistryKey":
                            keyPathSet = this.ParseRegistryKeyElement(child, id.Id, CompilerConstants.IntegerNotSet, null, win64, out keyPossible);
                            keyBit = MsiInterop.MsidbComponentAttributesRegistryKeyPath;
                            break;
                        case "RegistryValue":
                            keyPathSet = this.ParseRegistryValueElement(child, id.Id, CompilerConstants.IntegerNotSet, null, win64, out keyPossible);
                            keyBit = MsiInterop.MsidbComponentAttributesRegistryKeyPath;
                            break;
                        case "RemoveFile":
                            this.ParseRemoveFileElement(child, id.Id, directoryId);
                            break;
                        case "RemoveFolder":
                            this.ParseRemoveFolderElement(child, id.Id, directoryId);
                            break;
                        case "RemoveRegistryKey":
                            this.ParseRemoveRegistryKeyElement(child, id.Id);
                            break;
                        case "RemoveRegistryValue":
                            this.ParseRemoveRegistryValueElement(child, id.Id);
                            break;
                        case "ReserveCost":
                            this.ParseReserveCostElement(child, id.Id, directoryId);
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(child, id.Id, null);
                            break;
                        case "ServiceConfigFailureActions":
                            this.ParseServiceConfigFailureActionsElement(child, id.Id, null);
                            break;
                        case "ServiceControl":
                            this.ParseServiceControlElement(child, id.Id);
                            break;
                        case "ServiceInstall":
                            this.ParseServiceInstallElement(child, id.Id, win64);
                            break;
                        case "Shortcut":
                            this.ParseShortcutElement(child, id.Id, node.Name.LocalName, directoryId, YesNoType.No);
                            break;
                        case "SymbolPath":
                            symbols.Add(this.ParseSymbolPathElement(child));
                            break;
                        case "TypeLib":
                            this.ParseTypeLibElement(child, id.Id, null, win64);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ComponentId", id.Id }, { "DirectoryId", directoryId }, { "Win64", win64.ToString() }, };
                    ComponentKeyPath possibleKeyPath = this.core.ParsePossibleKeyPathExtensionElement(node, child, context);
                    if (null != possibleKeyPath)
                    {
                        if (ComponentKeyPathType.None == possibleKeyPath.Type)
                        {
                            keyPathSet = YesNoType.No;
                        }
                        else
                        {
                            keyPathSet = possibleKeyPath.Explicit ? YesNoType.Yes : YesNoType.NotSet;

                            if (!String.IsNullOrEmpty(possibleKeyPath.Id))
                            {
                                keyPossible = possibleKeyPath.Id;
                            }

                            if (ComponentKeyPathType.Registry == possibleKeyPath.Type || ComponentKeyPathType.RegistryFormatted == possibleKeyPath.Type)
                            {
                                keyBit = MsiInterop.MsidbComponentAttributesRegistryKeyPath;
                            }
                        }
                    }
                }

                // Verify that either the key path is not set, or it is set along with a key path ID.
                Debug.Assert(YesNoType.Yes != keyPathSet || (YesNoType.Yes == keyPathSet && null != keyPossible));

                if (keyFound && YesNoType.Yes == keyPathSet)
                {
                    this.core.OnMessage(WixErrors.ComponentMultipleKeyPaths(sourceLineNumbers, node.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
                }

                // if a possible KeyPath has been found and that value was explicitly set as
                // the KeyPath of the component, set it now.  Alternatively, if a possible
                // KeyPath has been found and no KeyPath has been previously set, use this
                // value as the default KeyPath of the component
                if (!String.IsNullOrEmpty(keyPossible) && (YesNoType.Yes == keyPathSet || (YesNoType.NotSet == keyPathSet && String.IsNullOrEmpty(keyPath) && !keyFound)))
                {
                    keyFound = YesNoType.Yes == keyPathSet;
                    keyPath = keyPossible;
                    keyBits = keyBit;
                }
            }


            if (shouldAddCreateFolder)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CreateFolder");
                row[0] = directoryId;
                row[1] = id.Id;
            }

            // check for conditions that exclude this component from using generated guids
            bool isGeneratableGuidOk = "*" == guid;
            if (isGeneratableGuidOk)
            {
                if (encounteredODBCDataSource)
                {
                    this.core.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers));
                    isGeneratableGuidOk = false;
                }

                if (0 != files && MsiInterop.MsidbComponentAttributesRegistryKeyPath == keyBits)
                {
                    this.core.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers, true));
                    isGeneratableGuidOk = false;
                }
            }

            // check for implicit KeyPath which can easily be accidentally changed
            if (this.showPedanticMessages && !keyFound && !isGeneratableGuidOk)
            {
                this.core.OnMessage(WixErrors.ImplicitComponentKeyPath(sourceLineNumbers, id.Id));
            }

            // if there isn't an @Id attribute value, replace the placeholder with the id of the keypath.
            // either an explicit KeyPath="yes" attribute must be specified or requirements for 
            // generatable guid must be met.
            if (componentIdPlaceholderWixVariable == id.Id)
            {
                if (isGeneratableGuidOk || keyFound && !String.IsNullOrEmpty(keyPath))
                {
                    this.componentIdPlaceholdersResolver.AddVariable(componentIdPlaceholder, keyPath);

                    id = new Identifier(keyPath, AccessModifier.Private);
                }
                else
                {
                    this.core.OnMessage(WixErrors.CannotDefaultComponentId(sourceLineNumbers));
                }
            }

            // If an id was not determined by now, we have to error.
            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // finally add the Component table row
            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Component", id);
                row[1] = guid;
                row[2] = directoryId;
                row[3] = bits | keyBits;
                row[4] = condition;
                row[5] = keyPath;

                if (multiInstance)
                {
                    Row instanceComponentRow = this.core.CreateRow(sourceLineNumbers, "WixInstanceComponent");
                    instanceComponentRow[0] = id;
                }

                if (0 < symbols.Count)
                {
                    WixDeltaPatchSymbolPathsRow symbolRow = (WixDeltaPatchSymbolPathsRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchSymbolPaths", id);
                    symbolRow.Type = SymbolPathType.Component;
                    symbolRow.SymbolPaths = String.Join(";", symbols);
                }

                // Complus
                if (CompilerConstants.IntegerNotSet != comPlusBits)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "Complus");
                    row[0] = id;
                    row[1] = comPlusBits;
                }

                // if this is a module, automatically add this component to the references to ensure it gets in the ModuleComponents table
                if (this.compilingModule)
                {
                    this.core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage, ComplexReferenceChildType.Component, id.Id, false);
                }
                else if (ComplexReferenceParentType.Unknown != parentType && null != parentId) // if parent was provided, add a complex reference to that.
                {
                    // If the Component is defined directly under a feature, then mark the complex reference primary.
                    this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.Component, id.Id, ComplexReferenceParentType.Feature == parentType);
                }
            }
        }

        /// <summary>
        /// Parses a component group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseComponentGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string directoryId = null;
            string source = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            // If the inline syntax is invalid it returns null. Use a static error identifier so the null
                            // directory identifier here doesn't trickle down false errors into child elements.
                            directoryId = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null) ?? "ErrorParsingInlineSyntax";
                            break;
                        case "Source":
                            source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            if (!String.IsNullOrEmpty(source) && !source.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                source = String.Concat(source, Path.DirectorySeparatorChar);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComponentGroupRef":
                            this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.ComponentGroup, id.Id, null);
                            break;
                        case "ComponentRef":
                            this.ParseComponentRefElement(child, ComplexReferenceParentType.ComponentGroup, id.Id, null);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.ComponentGroup, id.Id, null, CompilerConstants.IntegerNotSet, directoryId, source);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixComponentGroup", id);

                // Add this componentGroup and its parent in WixGroup.
                this.core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.ComponentGroup, id.Id);
            }
        }

        /// <summary>
        /// Parses a component group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element.</param>
        /// <param name="parentId">Identifier of parent element (usually a Feature or Module).</param>
        /// <param name="parentLanguage">Optional language of parent (only useful for Modules).</param>
        private void ParseComponentGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId, string parentLanguage)
        {
            Debug.Assert(ComplexReferenceParentType.ComponentGroup == parentType || ComplexReferenceParentType.FeatureGroup == parentType || ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.Module == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType primary = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixComponentGroup", id);
                            break;
                        case "Primary":
                            primary = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.ComponentGroup, id, (YesNoType.Yes == primary));
        }

        /// <summary>
        /// Parses a component reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element.</param>
        /// <param name="parentId">Identifier of parent element (usually a Feature or Module).</param>
        /// <param name="parentLanguage">Optional language of parent (only useful for Modules).</param>
        private void ParseComponentRefElement(XElement node, ComplexReferenceParentType parentType, string parentId, string parentLanguage)
        {
            Debug.Assert(ComplexReferenceParentType.FeatureGroup == parentType || ComplexReferenceParentType.ComponentGroup == parentType || ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.Module == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType primary = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Component", id);
                            break;
                        case "Primary":
                            primary = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.Component, id, (YesNoType.Yes == primary));
        }

        /// <summary>
        /// Parses a component search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private string ParseComponentSearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string componentId = null;
            int type = MsiInterop.MsidbLocatorTypeFileName;
            string signature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            componentId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                Wix.ComponentSearch.TypeType typeType = Wix.ComponentSearch.ParseTypeType(typeValue);
                                switch (typeType)
                                {
                                    case Wix.ComponentSearch.TypeType.directory:
                                        type = MsiInterop.MsidbLocatorTypeDirectory;
                                        break;
                                    case Wix.ComponentSearch.TypeType.file:
                                        type = MsiInterop.MsidbLocatorTypeFileName;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue, "directory", "file"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("cmp", componentId, type.ToString());
            }

            signature = id.Id;
            bool oneChild = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CompLocator", id);
                row[1] = componentId;
                row[2] = type;
            }

            return signature;
        }

        /// <summary>
        /// Parses a create folder element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="directoryId">Default identifier for directory to create.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        /// <returns>Identifier for the directory that will be created</returns>
        private string ParseCreateFolderElement(XElement node, string componentId, string directoryId, bool win64Component)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Directory":
                            directoryId = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Shortcut":
                            this.ParseShortcutElement(child, componentId, node.Name.LocalName, directoryId, YesNoType.No);
                            break;
                        case "Permission":
                            this.ParsePermissionElement(child, directoryId, "CreateFolder");
                            break;
                        case "PermissionEx":
                            this.ParsePermissionExElement(child, directoryId, "CreateFolder");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "DirectoryId", directoryId }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CreateFolder");
                row[0] = directoryId;
                row[1] = componentId;
            }

            return directoryId;
        }

        /// <summary>
        /// Parses a copy file element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of file to copy (null if moving the file).</param>
        private void ParseCopyFileElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            bool delete = false;
            string destinationDirectory = null;
            string destinationName = null;
            string destinationShortName = null;
            string destinationProperty = null;
            string sourceDirectory = null;
            string sourceFolder = null;
            string sourceName = null;
            string sourceProperty = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Delete":
                            delete = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DestinationDirectory":
                            destinationDirectory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "DestinationName":
                            destinationName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "DestinationProperty":
                            destinationProperty = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "DestinationShortName":
                            destinationShortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "FileId":
                            if (null != fileId)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            fileId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", fileId);
                            break;
                        case "SourceDirectory":
                            sourceDirectory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "SourceName":
                            sourceName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourceProperty":
                            sourceProperty = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != sourceFolder && null != sourceDirectory) // SourceFolder and SourceDirectory cannot coexist
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "SourceDirectory"));
            }

            if (null != sourceFolder && null != sourceProperty) // SourceFolder and SourceProperty cannot coexist
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "SourceProperty"));
            }

            if (null != sourceDirectory && null != sourceProperty) // SourceDirectory and SourceProperty cannot coexist
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceProperty", "SourceDirectory"));
            }

            if (null != destinationDirectory && null != destinationProperty) // DestinationDirectory and DestinationProperty cannot coexist
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DestinationProperty", "DestinationDirectory"));
            }

            // generate a short file name
            if (null == destinationShortName && (null != destinationName && !this.core.IsValidShortFilename(destinationName, false)))
            {
                destinationShortName = this.core.CreateShortName(destinationName, true, false, node.Name.LocalName, componentId);
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("cf", sourceFolder, sourceDirectory, sourceProperty, destinationDirectory, destinationProperty, destinationName);
            }

            this.core.ParseForExtensionElements(node);

            if (null == fileId)
            {
                // DestinationDirectory or DestinationProperty must be specified
                if (null == destinationDirectory && null == destinationProperty)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributesWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DestinationDirectory", "DestinationProperty", "FileId"));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MoveFile", id);
                    row[1] = componentId;
                    row[2] = sourceName;
                    row[3] = String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : GetMsiFilenameValue(destinationShortName, destinationName);
                    if (null != sourceDirectory)
                    {
                        row[4] = sourceDirectory;
                    }
                    else if (null != sourceProperty)
                    {
                        row[4] = sourceProperty;
                    }
                    else
                    {
                        row[4] = sourceFolder;
                    }

                    if (null != destinationDirectory)
                    {
                        row[5] = destinationDirectory;
                    }
                    else
                    {
                        row[5] = destinationProperty;
                    }
                    row[6] = delete ? 1 : 0;
                }
            }
            else // copy the file
            {
                if (null != sourceDirectory)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceDirectory", "FileId"));
                }

                if (null != sourceFolder)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "FileId"));
                }

                if (null != sourceName)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceName", "FileId"));
                }

                if (null != sourceProperty)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceProperty", "FileId"));
                }

                if (delete)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Delete", "FileId"));
                }

                if (null == destinationName && null == destinationDirectory && null == destinationProperty)
                {
                    this.core.OnMessage(WixWarnings.CopyFileFileIdUseless(sourceLineNumbers));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "DuplicateFile", id);
                    row[1] = componentId;
                    row[2] = fileId;
                    row[3] = String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : GetMsiFilenameValue(destinationShortName, destinationName);
                    if (null != destinationDirectory)
                    {
                        row[4] = destinationDirectory;
                    }
                    else
                    {
                        row[4] = destinationProperty;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a CustomAction element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseCustomActionElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int bits = 0;
            int extendedBits = 0;
            bool inlineScript = false;
            string innerText = null;
            string source = null;
            int sourceBits = 0;
            YesNoType suppressModularization = YesNoType.NotSet;
            string target = null;
            int targetBits = 0;
            bool explicitWin64 = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinaryKey":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeBinaryData;
                            this.core.CreateSimpleReference(sourceLineNumbers, "Binary", source); // add a reference to the appropriate Binary
                            break;
                        case "Directory":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                            break;
                        case "DllEntry":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            targetBits = MsiInterop.MsidbCustomActionTypeDll;
                            break;
                        case "Error":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            targetBits = MsiInterop.MsidbCustomActionTypeTextData | MsiInterop.MsidbCustomActionTypeSourceFile;

                            bool errorReference = true;

                            try
                            {
                                // The target can be either a formatted error string or a literal 
                                // error number. Try to convert to error number to determine whether
                                // to add a reference. No need to look at the value.
                                Convert.ToInt32(target, CultureInfo.InvariantCulture.NumberFormat);
                            }
                            catch (FormatException)
                            {
                                errorReference = false;
                            }
                            catch (OverflowException)
                            {
                                errorReference = false;
                            }

                            if (errorReference)
                            {
                                this.core.CreateSimpleReference(sourceLineNumbers, "Error", target);
                            }
                            break;
                        case "ExeCommand":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeExe;
                            break;
                        case "Execute":
                            string execute = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < execute.Length)
                            {
                                Wix.CustomAction.ExecuteType executeType = Wix.CustomAction.ParseExecuteType(execute);
                                switch (executeType)
                                {
                                    case Wix.CustomAction.ExecuteType.commit:
                                        bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeCommit;
                                        break;
                                    case Wix.CustomAction.ExecuteType.deferred:
                                        bits |= MsiInterop.MsidbCustomActionTypeInScript;
                                        break;
                                    case Wix.CustomAction.ExecuteType.firstSequence:
                                        bits |= MsiInterop.MsidbCustomActionTypeFirstSequence;
                                        break;
                                    case Wix.CustomAction.ExecuteType.immediate:
                                        break;
                                    case Wix.CustomAction.ExecuteType.oncePerProcess:
                                        bits |= MsiInterop.MsidbCustomActionTypeOncePerProcess;
                                        break;
                                    case Wix.CustomAction.ExecuteType.rollback:
                                        bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeRollback;
                                        break;
                                    case Wix.CustomAction.ExecuteType.secondSequence:
                                        bits |= MsiInterop.MsidbCustomActionTypeClientRepeat;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, execute, "commit", "deferred", "firstSequence", "immediate", "oncePerProcess", "rollback", "secondSequence"));
                                        break;
                                }
                            }
                            break;
                        case "FileKey":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeSourceFile;
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", source); // add a reference to the appropriate File
                            break;
                        case "HideTarget":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeHideTarget;
                            }
                            break;
                        case "Impersonate":
                            if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeNoImpersonate;
                            }
                            break;
                        case "JScriptCall":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeJScript;
                            break;
                        case "PatchUninstall":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                extendedBits |= MsiInterop.MsidbCustomActionTypePatchUninstall;
                            }
                            break;
                        case "Property":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeProperty;
                            break;
                        case "Return":
                            string returnValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < returnValue.Length)
                            {
                                Wix.CustomAction.ReturnType returnType = Wix.CustomAction.ParseReturnType(returnValue);
                                switch (returnType)
                                {
                                    case Wix.CustomAction.ReturnType.asyncNoWait:
                                        bits |= MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue;
                                        break;
                                    case Wix.CustomAction.ReturnType.asyncWait:
                                        bits |= MsiInterop.MsidbCustomActionTypeAsync;
                                        break;
                                    case Wix.CustomAction.ReturnType.check:
                                        break;
                                    case Wix.CustomAction.ReturnType.ignore:
                                        bits |= MsiInterop.MsidbCustomActionTypeContinue;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, returnValue, "asyncNoWait", "asyncWait", "check", "ignore"));
                                        break;
                                }
                            }
                            break;
                        case "Script":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }

                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }

                            // set the source and target to empty string for error messages when the user sets multiple sources or targets
                            source = string.Empty;
                            target = string.Empty;

                            inlineScript = true;

                            string script = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < script.Length)
                            {
                                Wix.CustomAction.ScriptType scriptType = Wix.CustomAction.ParseScriptType(script);
                                switch (scriptType)
                                {
                                    case Wix.CustomAction.ScriptType.jscript:
                                        sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                                        targetBits = MsiInterop.MsidbCustomActionTypeJScript;
                                        break;
                                    case Wix.CustomAction.ScriptType.vbscript:
                                        sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                                        targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, script, "jscript", "vbscript"));
                                        break;
                                }
                            }
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TerminalServerAware":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeTSAware;
                            }
                            break;
                        case "Value":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeTextData;
                            break;
                        case "VBScriptCall":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionType64BitScript;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            if (!explicitWin64 && (MsiInterop.MsidbCustomActionTypeVBScript == targetBits || MsiInterop.MsidbCustomActionTypeJScript == targetBits) && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                bits |= MsiInterop.MsidbCustomActionType64BitScript;
            }

            // get the inner text if any exists
            innerText = this.core.GetTrimmedInnerText(node);

            // if we have an in-lined Script CustomAction ensure no source or target attributes were provided
            if (inlineScript)
            {
                target = innerText;
            }
            else if (MsiInterop.MsidbCustomActionTypeVBScript == targetBits) // non-inline vbscript
            {
                if (null == source)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "Directory"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeJScript == targetBits) // non-inline jscript
            {
                if (null == source)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "Directory"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeExe == targetBits) // exe-command
            {
                if (null == source)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ExeCommand", "BinaryKey", "Directory", "FileKey", "Property"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeTextData == (bits | sourceBits | targetBits))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Value", "Directory", "Property"));
            }
            else if (!String.IsNullOrEmpty(innerText)) // inner text cannot be specified with non-script CAs
            {
                this.core.OnMessage(WixErrors.CustomActionIllegalInnerText(sourceLineNumbers, node.Name.LocalName, innerText, "Script"));
            }

            if (MsiInterop.MsidbCustomActionType64BitScript == (bits & MsiInterop.MsidbCustomActionType64BitScript) && MsiInterop.MsidbCustomActionTypeVBScript != targetBits && MsiInterop.MsidbCustomActionTypeJScript != targetBits)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Win64", "Script", "VBScriptCall", "JScriptCall"));
            }

            if ((MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue) == (bits & (MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue)) && MsiInterop.MsidbCustomActionTypeExe != targetBits)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Return", "asyncNoWait", "ExeCommand"));
            }

            if (MsiInterop.MsidbCustomActionTypeTSAware == (bits & MsiInterop.MsidbCustomActionTypeTSAware))
            {
                // TS-aware CAs are valid only when deferred so require the in-script Type bit...
                if (0 == (bits & MsiInterop.MsidbCustomActionTypeInScript))
                {
                    this.core.OnMessage(WixErrors.IllegalTerminalServerCustomActionAttributes(sourceLineNumbers));
                }
            }

            // MSI doesn't support in-script property setting, so disallow it
            if (MsiInterop.MsidbCustomActionTypeProperty == sourceBits &&
                MsiInterop.MsidbCustomActionTypeTextData == targetBits &&
                0 != (bits & MsiInterop.MsidbCustomActionTypeInScript))
            {
                this.core.OnMessage(WixErrors.IllegalPropertyCustomActionAttributes(sourceLineNumbers));
            }

            if (0 == targetBits)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CustomAction", id);
                row[1] = bits | sourceBits | targetBits;
                row[2] = source;
                row[3] = target;
                if (0 != extendedBits)
                {
                    row[4] = extendedBits;
                }

                if (YesNoType.Yes == suppressModularization)
                {
                    this.core.CreateRow(sourceLineNumbers, "WixSuppressModularization", id);
                }

                // For deferred CAs that specify HideTarget we should also hide the CA data property for the action.
                if (MsiInterop.MsidbCustomActionTypeHideTarget == (bits & MsiInterop.MsidbCustomActionTypeHideTarget) &&
                    MsiInterop.MsidbCustomActionTypeInScript == (bits & MsiInterop.MsidbCustomActionTypeInScript))
                {
                    this.AddWixPropertyRow(sourceLineNumbers, id, false, false, true);
                }
            }
        }

        /// <summary>
        /// Parses a simple reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table which contains the target of the simple reference.</param>
        /// <returns>Id of the referenced element.</returns>
        private string ParseSimpleRefElement(XElement node, string table)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, table, id);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            return id;
        }

        /// <summary>
        /// Parses a PatchFamilyRef element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The parent type.</param>
        /// <param name="parentId">The ID of the parent.</param>
        /// <returns>Id of the referenced element.</returns>
        private void ParsePatchFamilyRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string[] primaryKeys = new string[2];

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            primaryKeys[0] = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            primaryKeys[1] = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == primaryKeys[0])
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.CreateSimpleReference(sourceLineNumbers, "MsiPatchSequence", primaryKeys);

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, primaryKeys[0], true);
            }
        }

        /// <summary>
        /// Parses a PatchFamilyGroup element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParsePatchFamilyGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PatchFamily":
                            this.ParsePatchFamilyElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
                            break;
                        case "PatchFamilyRef":
                            this.ParsePatchFamilyRefElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
                            break;
                        case "PatchFamilyGroupRef":
                            this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixPatchFamilyGroup", id);

                //Add this PatchFamilyGroup and its parent in WixGroup.
                this.core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PatchFamilyGroup, id.Id);
            }
        }

        /// <summary>
        /// Parses a PatchFamilyGroup reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The type of parent.</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private void ParsePatchFamilyGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            Debug.Assert(ComplexReferenceParentType.PatchFamilyGroup == parentType || ComplexReferenceParentType.Patch == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixPatchFamilyGroup", id);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamilyGroup, id, true);
            }
        }

        /// <summary>
        /// Parses an ensure table element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseEnsureTableElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (31 < id.Length)
            {
                this.core.OnMessage(WixErrors.TableNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id));
            }

            this.core.ParseForExtensionElements(node);

            this.core.EnsureTable(sourceLineNumbers, id);
        }

        /// <summary>
        /// Parses a custom table element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <remarks>not cleaned</remarks>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the WixCustomTable table is generated. Furthermore, there is no security hole here, as the strings won't need to " +
                         "make a round trip")]
        private void ParseCustomTableElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string tableId = null;

            string categories = null;
            int columnCount = 0;
            string columnNames = null;
            string columnTypes = null;
            string descriptions = null;
            string keyColumns = null;
            string keyTables = null;
            string maxValues = null;
            string minValues = null;
            string modularizations = null;
            string primaryKeys = null;
            string sets = null;
            bool bootstrapperApplicationData = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            tableId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "BootstrapperApplicationData":
                            bootstrapperApplicationData = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == tableId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (31 < tableId.Length)
            {
                this.core.OnMessage(WixErrors.CustomTableNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", tableId));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "Column":
                            ++columnCount;

                            string category = String.Empty;
                            string columnName = null;
                            string columnType = null;
                            string description = String.Empty;
                            int keyColumn = CompilerConstants.IntegerNotSet;
                            string keyTable = String.Empty;
                            bool localizable = false;
                            long maxValue = CompilerConstants.LongNotSet;
                            long minValue = CompilerConstants.LongNotSet;
                            string modularization = "None";
                            bool nullable = false;
                            bool primaryKey = false;
                            string setValues = String.Empty;
                            string typeName = null;
                            int width = 0;

                            foreach (XAttribute childAttrib in child.Attributes())
                            {
                                switch (childAttrib.Name.LocalName)
                                {
                                    case "Id":
                                        columnName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Category":
                                        category = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Description":
                                        description = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "KeyColumn":
                                        keyColumn = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 1, 32);
                                        break;
                                    case "KeyTable":
                                        keyTable = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Localizable":
                                        localizable = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "MaxValue":
                                        maxValue = this.core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, int.MinValue + 1, int.MaxValue);
                                        break;
                                    case "MinValue":
                                        minValue = this.core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, int.MinValue + 1, int.MaxValue);
                                        break;
                                    case "Modularize":
                                        modularization = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Nullable":
                                        nullable = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "PrimaryKey":
                                        primaryKey = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Set":
                                        setValues = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Type":
                                        string typeValue = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        if (0 < typeValue.Length)
                                        {
                                            Wix.Column.TypeType typeType = Wix.Column.ParseTypeType(typeValue);
                                            switch (typeType)
                                            {
                                                case Wix.Column.TypeType.binary:
                                                    typeName = "OBJECT";
                                                    break;
                                                case Wix.Column.TypeType.@int:
                                                    typeName = "SHORT";
                                                    break;
                                                case Wix.Column.TypeType.@string:
                                                    typeName = "CHAR";
                                                    break;
                                                default:
                                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Type", typeValue, "binary", "int", "string"));
                                                    break;
                                            }
                                        }
                                        break;
                                    case "Width":
                                        width = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 0, int.MaxValue);
                                        break;
                                    default:
                                        this.core.UnexpectedAttribute(child, childAttrib);
                                        break;
                                }
                            }

                            if (null == columnName)
                            {
                                this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Id"));
                            }

                            if (null == typeName)
                            {
                                this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Type"));
                            }
                            else if ("SHORT" == typeName)
                            {
                                if (2 != width && 4 != width)
                                {
                                    this.core.OnMessage(WixErrors.CustomTableIllegalColumnWidth(childSourceLineNumbers, child.Name.LocalName, "Width", width));
                                }
                                columnType = String.Concat(nullable ? "I" : "i", width);
                            }
                            else if ("CHAR" == typeName)
                            {
                                string typeChar = localizable ? "l" : "s";
                                columnType = String.Concat(nullable ? typeChar.ToUpper(CultureInfo.InvariantCulture) : typeChar.ToLower(CultureInfo.InvariantCulture), width);
                            }
                            else if ("OBJECT" == typeName)
                            {
                                if ("Binary" != category)
                                {
                                    this.core.OnMessage(WixErrors.ExpectedBinaryCategory(childSourceLineNumbers));
                                }
                                columnType = String.Concat(nullable ? "V" : "v", width);
                            }

                            this.core.ParseForExtensionElements(child);

                            columnNames = String.Concat(columnNames, null == columnNames ? String.Empty : "\t", columnName);
                            columnTypes = String.Concat(columnTypes, null == columnTypes ? String.Empty : "\t", columnType);
                            if (primaryKey)
                            {
                                primaryKeys = String.Concat(primaryKeys, null == primaryKeys ? String.Empty : "\t", columnName);
                            }

                            minValues = String.Concat(minValues, null == minValues ? String.Empty : "\t", CompilerConstants.LongNotSet != minValue ? minValue.ToString(CultureInfo.InvariantCulture) : String.Empty);
                            maxValues = String.Concat(maxValues, null == maxValues ? String.Empty : "\t", CompilerConstants.LongNotSet != maxValue ? maxValue.ToString(CultureInfo.InvariantCulture) : String.Empty);
                            keyTables = String.Concat(keyTables, null == keyTables ? String.Empty : "\t", keyTable);
                            keyColumns = String.Concat(keyColumns, null == keyColumns ? String.Empty : "\t", CompilerConstants.IntegerNotSet != keyColumn ? keyColumn.ToString(CultureInfo.InvariantCulture) : String.Empty);
                            categories = String.Concat(categories, null == categories ? String.Empty : "\t", category);
                            sets = String.Concat(sets, null == sets ? String.Empty : "\t", setValues);
                            descriptions = String.Concat(descriptions, null == descriptions ? String.Empty : "\t", description);
                            modularizations = String.Concat(modularizations, null == modularizations ? String.Empty : "\t", modularization);

                            break;
                        case "Row":
                            string dataValue = null;

                            foreach (XAttribute childAttrib in child.Attributes())
                            {
                                this.core.ParseExtensionAttribute(child, childAttrib);
                            }

                            foreach (XElement data in child.Elements())
                            {
                                SourceLineNumber dataSourceLineNumbers = Preprocessor.GetSourceLineNumbers(data);
                                switch (data.Name.LocalName)
                                {
                                    case "Data":
                                        columnName = null;
                                        foreach (XAttribute dataAttrib in data.Attributes())
                                        {
                                            switch (dataAttrib.Name.LocalName)
                                            {
                                                case "Column":
                                                    columnName = this.core.GetAttributeValue(dataSourceLineNumbers, dataAttrib);
                                                    break;
                                                default:
                                                    this.core.UnexpectedAttribute(data, dataAttrib);
                                                    break;
                                            }
                                        }

                                        if (null == columnName)
                                        {
                                            this.core.OnMessage(WixErrors.ExpectedAttribute(dataSourceLineNumbers, data.Name.LocalName, "Column"));
                                        }

                                        dataValue = String.Concat(dataValue, null == dataValue ? String.Empty : Common.CustomRowFieldSeparator.ToString(), columnName, ":", Common.GetInnerText(data));
                                        break;
                                }
                            }

                            this.core.CreateSimpleReference(sourceLineNumbers, "WixCustomTable", tableId);

                            if (!this.core.EncounteredError)
                            {
                                Row rowRow = this.core.CreateRow(childSourceLineNumbers, "WixCustomRow");
                                rowRow[0] = tableId;
                                rowRow[1] = dataValue;
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (0 < columnCount)
            {
                if (null == primaryKeys || 0 == primaryKeys.Length)
                {
                    this.core.OnMessage(WixErrors.CustomTableMissingPrimaryKey(sourceLineNumbers));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "WixCustomTable");
                    row[0] = tableId;
                    row[1] = columnCount;
                    row[2] = columnNames;
                    row[3] = columnTypes;
                    row[4] = primaryKeys;
                    row[5] = minValues;
                    row[6] = maxValues;
                    row[7] = keyTables;
                    row[8] = keyColumns;
                    row[9] = categories;
                    row[10] = sets;
                    row[11] = descriptions;
                    row[12] = modularizations;
                    row[13] = bootstrapperApplicationData ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Parses a directory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <param name="diskId">Disk id inherited from parent directory.</param>
        /// <param name="fileSource">Path to source file as of yet.</param>
        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
        private void ParseDirectoryElement(XElement node, string parentId, int diskId, string fileSource)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string componentGuidGenerationSeed = null;
            bool fileSourceAttribSet = false;
            bool nameHasValue = false;
            string name = "."; // default to parent directory.
            string[] inlineSyntax = null;
            string shortName = null;
            string sourceName = null;
            string shortSourceName = null;
            string defaultDir = null;
            string symbols = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ComponentGuidGenerationSeed":
                            componentGuidGenerationSeed = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FileSource":
                            fileSource = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            fileSourceAttribSet = true;
                            break;
                        case "Name":
                            nameHasValue = true;
                            if (attrib.Value.Equals("."))
                            {
                                name = attrib.Value;
                            }
                            else
                            {
                                inlineSyntax = this.core.GetAttributeInlineDirectorySyntax(sourceLineNumbers, attrib);
                            }
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "ShortSourceName":
                            shortSourceName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "SourceName":
                            if ("." == attrib.Value)
                            {
                                sourceName = attrib.Value;
                            }
                            else
                            {
                                sourceName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Create the directory rows for the inline.
            if (null != inlineSyntax)
            {
                // Special case the single entry in the inline syntax since it is the most common case
                // and needs no extra processing. It's just the name of the directory.
                if (1 == inlineSyntax.Length)
                {
                    name = inlineSyntax[0];
                }
                else
                {
                    int pathStartsAt = 0;
                    if (inlineSyntax[0].EndsWith(":"))
                    {
                        parentId = inlineSyntax[0].TrimEnd(':');
                        this.core.CreateSimpleReference(sourceLineNumbers, "Directory", parentId);

                        pathStartsAt = 1;
                    }

                    for (int i = pathStartsAt; i < inlineSyntax.Length - 1; ++i)
                    {
                        Identifier inlineId = this.core.CreateDirectoryRow(sourceLineNumbers, null, parentId, inlineSyntax[i]);
                        parentId = inlineId.Id;
                    }

                    name = inlineSyntax[inlineSyntax.Length - 1];
                }
            }

            if (!nameHasValue)
            {
                if (!String.IsNullOrEmpty(shortName))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name"));
                }

                if (null == parentId)
                {
                    this.core.OnMessage(WixErrors.DirectoryRootWithoutName(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }
            else if (!String.IsNullOrEmpty(name))
            {
                if (String.IsNullOrEmpty(shortName))
                {
                    if (!name.Equals(".") && !name.Equals("SourceDir") && !this.core.IsValidShortFilename(name, false))
                    {
                        shortName = this.core.CreateShortName(name, false, false, "Directory", parentId);
                    }
                }
                else if (name.Equals("."))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name", name));
                }
                else if (name.Equals(shortName))
                {
                    this.core.OnMessage(WixWarnings.DirectoryRedundantNames(sourceLineNumbers, node.Name.LocalName, "Name", "ShortName", name));
                }
            }

            if (String.IsNullOrEmpty(sourceName))
            {
                if (!String.IsNullOrEmpty(shortSourceName))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ShortSourceName", "SourceName"));
                }
            }
            else
            {
                if (String.IsNullOrEmpty(shortSourceName))
                {
                    if (!sourceName.Equals(".") && !this.core.IsValidShortFilename(sourceName, false))
                    {
                        shortSourceName = this.core.CreateShortName(sourceName, false, false, "Directory", parentId);
                    }
                }
                else if (sourceName.Equals("."))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortSourceName", "SourceName", sourceName));
                }
                else if (sourceName.Equals(shortSourceName))
                {
                    this.core.OnMessage(WixWarnings.DirectoryRedundantNames(sourceLineNumbers, node.Name.LocalName, "SourceName", "ShortSourceName", sourceName));
                }
            }

            // Update the file source path appropriately.
            if (fileSourceAttribSet)
            {
                if (!fileSource.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    fileSource = String.Concat(fileSource, Path.DirectorySeparatorChar);
                }
            }
            else // add the appropriate part of this directory element to the file source.
            {
                string append = null;
                if (this.useShortFileNames)
                {
                    append = !String.IsNullOrEmpty(shortSourceName) ? shortSourceName : shortName;
                }

                if (String.IsNullOrEmpty(append))
                {
                    append = !String.IsNullOrEmpty(sourceName) ? sourceName : name;
                }

                if (!String.IsNullOrEmpty(append))
                {
                    fileSource = String.Concat(fileSource, append, Path.DirectorySeparatorChar);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("dir", parentId, name, shortName, sourceName, shortSourceName);
            }

            // Calculate the DefaultDir for the directory row.
            defaultDir = String.IsNullOrEmpty(shortName) ? name : String.Concat(shortName, "|", name);
            if (!String.IsNullOrEmpty(sourceName))
            {
                defaultDir = String.Concat(defaultDir, ":", String.IsNullOrEmpty(shortSourceName) ? sourceName : String.Concat(shortSourceName, "|", sourceName));
            }

            if ("TARGETDIR".Equals(id.Id) && !"SourceDir".Equals(defaultDir))
            {
                this.core.OnMessage(WixErrors.IllegalTargetDirDefaultDir(sourceLineNumbers, defaultDir));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, diskId, id.Id, fileSource);
                            break;
                        case "Directory":
                            this.ParseDirectoryElement(child, id.Id, diskId, fileSource);
                            break;
                        case "Merge":
                            this.ParseMergeElement(child, id.Id, diskId);
                            break;
                        case "SymbolPath":
                            if (null != symbols)
                            {
                                symbols += ";" + this.ParseSymbolPathElement(child);
                            }
                            else
                            {
                                symbols = this.ParseSymbolPathElement(child);
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Directory", id);
                row[1] = parentId;
                row[2] = defaultDir;

                if (null != componentGuidGenerationSeed)
                {
                    Row wixRow = this.core.CreateRow(sourceLineNumbers, "WixDirectory");
                    wixRow[0] = id.Id;
                    wixRow[1] = componentGuidGenerationSeed;
                }

                if (null != symbols)
                {
                    WixDeltaPatchSymbolPathsRow symbolRow = (WixDeltaPatchSymbolPathsRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchSymbolPaths", id);
                    symbolRow.Type = SymbolPathType.Directory;
                    symbolRow.SymbolPaths = symbols;
                }
            }
        }

        /// <summary>
        /// Parses a directory reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseDirectoryRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int diskId = CompilerConstants.IntegerNotSet;
            string fileSource = String.Empty;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Directory", id);
                            break;
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FileSource":
                            fileSource = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!String.IsNullOrEmpty(fileSource) && !fileSource.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fileSource = String.Concat(fileSource, Path.DirectorySeparatorChar);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, diskId, id, fileSource);
                            break;
                        case "Directory":
                            this.ParseDirectoryElement(child, id, diskId, fileSource);
                            break;
                        case "Merge":
                            this.ParseMergeElement(child, id, diskId);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses a directory search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentSignature">Signature of parent search element.</param>
        /// <returns>Signature of search element.</returns>
        private string ParseDirectorySearchElement(XElement node, string parentSignature)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int depth = CompilerConstants.IntegerNotSet;
            string path = null;
            bool assignToProperty = false;
            string signature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Depth":
                            depth = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Path":
                            path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "AssignToProperty":
                            assignToProperty = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("dir", path, depth.ToString());
            }

            signature = id.Id;

            bool oneChild = false;
            bool hasFileSearch = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            hasFileSearch = true;
                            signature = this.ParseFileSearchElement(child, id.Id, assignToProperty, depth);
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseSimpleRefElement(child, "Signature");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }

                    // If AssignToProperty is set, only a FileSearch
                    // or no child element can be nested.
                    if (assignToProperty)
                    {
                        if (!hasFileSearch)
                        {
                            this.core.OnMessage(WixErrors.IllegalParentAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "AssignToProperty", child.Name.LocalName));
                        }
                        else if (!oneChild)
                        {
                            // This a normal directory search.
                            assignToProperty = false;
                        }
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Identifier rowId = id;

                // If AssignToProperty is set, the DrLocator row created by
                // ParseFileSearchElement creates the directory entry to return
                // and the row created here is for the file search.
                if (assignToProperty)
                {
                    rowId = new Identifier(signature, AccessModifier.Private);

                    // The property should be set to the directory search Id.
                    signature = id.Id;
                }

                Row row = this.core.CreateRow(sourceLineNumbers, "DrLocator", rowId);
                row[1] = parentSignature;
                row[2] = path;
                if (CompilerConstants.IntegerNotSet != depth)
                {
                    row[3] = depth;
                }
            }

            return signature;
        }

        /// <summary>
        /// Parses a directory search reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentSignature">Signature of parent search element.</param>
        /// <returns>Signature of search element.</returns>
        private string ParseDirectorySearchRefElement(XElement node, string parentSignature)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            Identifier parent = null;
            string path = null;
            string signature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Parent":
                            parent = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != parent)
            {
                if (!String.IsNullOrEmpty(parentSignature))
                {
                    this.core.OnMessage(WixErrors.CanNotHaveTwoParents(sourceLineNumbers, id.Id, parent.Id, parentSignature));
                }
                else
                {
                    parentSignature = parent.Id;
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("dsr", parentSignature, path);
            }

            signature = id.Id;

            bool oneChild = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseSimpleRefElement(child, "Signature");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            this.core.CreateSimpleReference(sourceLineNumbers, "DrLocator", id.Id, parentSignature, path);

            return signature;
        }

        /// <summary>
        /// Parses a feature element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The type of parent.</param>
        /// <param name="parentId">Optional identifer for parent feature.</param>
        /// <param name="lastDisplay">Display value for last feature used to get the features to display in the same order as specified 
        /// in the source code.</param>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseFeatureElement(XElement node, ComplexReferenceParentType parentType, string parentId, ref int lastDisplay)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string allowAdvertise = null;
            int bits = 0;
            string configurableDirectory = null;
            string description = null;
            string display = "collapse";
            YesNoType followParent = YesNoType.NotSet;
            string installDefault = null;
            int level = 1;
            string title = null;
            string typicalDefault = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Absent":
                            string absent = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < absent.Length)
                            {
                                Wix.Feature.AbsentType absentType = Wix.Feature.ParseAbsentType(absent);
                                switch (absentType)
                                {
                                    case Wix.Feature.AbsentType.allow: // this is the default
                                        break;
                                    case Wix.Feature.AbsentType.disallow:
                                        bits = bits | MsiInterop.MsidbFeatureAttributesUIDisallowAbsent;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, absent, "allow", "disallow"));
                                        break;
                                }
                            }
                            break;
                        case "AllowAdvertise":
                            allowAdvertise = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < allowAdvertise.Length)
                            {
                                Wix.Feature.AllowAdvertiseType allowAdvertiseType = Wix.Feature.ParseAllowAdvertiseType(allowAdvertise);
                                switch (allowAdvertiseType)
                                {
                                    case Wix.Feature.AllowAdvertiseType.no:
                                        bits |= MsiInterop.MsidbFeatureAttributesDisallowAdvertise;
                                        break;
                                    case Wix.Feature.AllowAdvertiseType.system:
                                        bits |= MsiInterop.MsidbFeatureAttributesNoUnsupportedAdvertise;
                                        break;
                                    case Wix.Feature.AllowAdvertiseType.yes: // this is the default
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, allowAdvertise, "no", "system", "yes"));
                                        break;
                                }
                            }
                            break;
                        case "ConfigurableDirectory":
                            configurableDirectory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Display":
                            display = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDefault":
                            installDefault = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < installDefault.Length)
                            {
                                Wix.Feature.InstallDefaultType installDefaultType = Wix.Feature.ParseInstallDefaultType(installDefault);
                                switch (installDefaultType)
                                {
                                    case Wix.Feature.InstallDefaultType.followParent:
                                        if (ComplexReferenceParentType.Product == parentType)
                                        {
                                            this.core.OnMessage(WixErrors.RootFeatureCannotFollowParent(sourceLineNumbers));
                                        }
                                        bits = bits | MsiInterop.MsidbFeatureAttributesFollowParent;
                                        break;
                                    case Wix.Feature.InstallDefaultType.local: // this is the default
                                        break;
                                    case Wix.Feature.InstallDefaultType.source:
                                        bits = bits | MsiInterop.MsidbFeatureAttributesFavorSource;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installDefault, "followParent", "local", "source"));
                                        break;
                                }
                            }
                            break;
                        case "Level":
                            level = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Title":
                            title = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-FEATURE-TITLE-HERE" == title)
                            {
                                this.core.OnMessage(WixWarnings.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, title));
                            }
                            break;
                        case "TypicalDefault":
                            typicalDefault = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typicalDefault.Length)
                            {
                                Wix.Feature.TypicalDefaultType typicalDefaultType = Wix.Feature.ParseTypicalDefaultType(typicalDefault);
                                switch (typicalDefaultType)
                                {
                                    case Wix.Feature.TypicalDefaultType.advertise:
                                        bits = bits | MsiInterop.MsidbFeatureAttributesFavorAdvertise;
                                        break;
                                    case Wix.Feature.TypicalDefaultType.install: // this is the default
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typicalDefault, "advertise", "install"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (38 < id.Id.Length)
            {
                this.core.OnMessage(WixErrors.FeatureNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
            }

            if (null != configurableDirectory && configurableDirectory.ToUpper(CultureInfo.InvariantCulture) != configurableDirectory)
            {
                this.core.OnMessage(WixErrors.FeatureConfigurableDirectoryNotUppercase(sourceLineNumbers, node.Name.LocalName, "ConfigurableDirectory", configurableDirectory));
            }

            if ("advertise" == typicalDefault && "no" == allowAdvertise)
            {
                this.core.OnMessage(WixErrors.FeatureCannotFavorAndDisallowAdvertise(sourceLineNumbers, node.Name.LocalName, "TypicalDefault", typicalDefault, "AllowAdvertise", allowAdvertise));
            }

            if (YesNoType.Yes == followParent && ("local" == installDefault || "source" == installDefault))
            {
                this.core.OnMessage(WixErrors.FeatureCannotFollowParentAndFavorLocalOrSource(sourceLineNumbers, node.Name.LocalName, "InstallDefault", "FollowParent", "yes"));
            }

            int childDisplay = 0;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComponentGroupRef":
                            this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.Feature, id.Id, null);
                            break;
                        case "ComponentRef":
                            this.ParseComponentRefElement(child, ComplexReferenceParentType.Feature, id.Id, null);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Feature, id.Id, null, CompilerConstants.IntegerNotSet, null, null);
                            break;
                        case "Condition":
                            this.ParseConditionElement(child, node.Name.LocalName, id.Id, null);
                            break;
                        case "Feature":
                            this.ParseFeatureElement(child, ComplexReferenceParentType.Feature, id.Id, ref childDisplay);
                            break;
                        case "FeatureGroupRef":
                            this.ParseFeatureGroupRefElement(child, ComplexReferenceParentType.Feature, id.Id);
                            break;
                        case "FeatureRef":
                            this.ParseFeatureRefElement(child, ComplexReferenceParentType.Feature, id.Id);
                            break;
                        case "MergeRef":
                            this.ParseMergeRefElement(child, ComplexReferenceParentType.Feature, id.Id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Feature", id);
                row[1] = null; // this column is set in the linker
                row[2] = title;
                row[3] = description;
                if (0 < display.Length)
                {
                    switch (display)
                    {
                        case "collapse":
                            lastDisplay = (lastDisplay | 1) + 1;
                            row[4] = lastDisplay;
                            break;
                        case "expand":
                            lastDisplay = (lastDisplay + 1) | 1;
                            row[4] = lastDisplay;
                            break;
                        case "hidden":
                            row[4] = 0;
                            break;
                        default:
                            int value;
                            if (!Int32.TryParse(display, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Display", display, "collapse", "expand", "hidden"));
                            }
                            else
                            {
                                row[4] = value;
                                // save the display value of this row (if its not hidden) for subsequent rows
                                if (0 != (int)row[4])
                                {
                                    lastDisplay = (int)row[4];
                                }
                            }
                            break;
                    }
                }
                row[5] = level;
                row[6] = configurableDirectory;
                row[7] = bits;

                if (ComplexReferenceParentType.Unknown != parentType)
                {
                    this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Feature, id.Id, false);
                }
            }
        }

        /// <summary>
        /// Parses a feature reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The type of parent.</param>
        /// <param name="parentId">Optional identifier for parent feature.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseFeatureRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType ignoreParent = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Feature", id);
                            break;
                        case "IgnoreParent":
                            ignoreParent = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }


            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            int lastDisplay = 0;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComponentGroupRef":
                            this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.Feature, id, null);
                            break;
                        case "ComponentRef":
                            this.ParseComponentRefElement(child, ComplexReferenceParentType.Feature, id, null);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Feature, id, null, CompilerConstants.IntegerNotSet, null, null);
                            break;
                        case "Feature":
                            this.ParseFeatureElement(child, ComplexReferenceParentType.Feature, id, ref lastDisplay);
                            break;
                        case "FeatureGroup":
                            this.ParseFeatureGroupElement(child, ComplexReferenceParentType.Feature, id);
                            break;
                        case "FeatureGroupRef":
                            this.ParseFeatureGroupRefElement(child, ComplexReferenceParentType.Feature, id);
                            break;
                        case "FeatureRef":
                            this.ParseFeatureRefElement(child, ComplexReferenceParentType.Feature, id);
                            break;
                        case "MergeRef":
                            this.ParseMergeRefElement(child, ComplexReferenceParentType.Feature, id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                if (ComplexReferenceParentType.Unknown != parentType && YesNoType.Yes != ignoreParent)
                {
                    this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Feature, id, false);
                }
            }
        }

        /// <summary>
        /// Parses a feature group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseFeatureGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            int lastDisplay = 0;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComponentGroupRef":
                            this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.FeatureGroup, id.Id, null);
                            break;
                        case "ComponentRef":
                            this.ParseComponentRefElement(child, ComplexReferenceParentType.FeatureGroup, id.Id, null);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.FeatureGroup, id.Id, null, CompilerConstants.IntegerNotSet, null, null);
                            break;
                        case "Feature":
                            this.ParseFeatureElement(child, ComplexReferenceParentType.FeatureGroup, id.Id, ref lastDisplay);
                            break;
                        case "FeatureGroupRef":
                            this.ParseFeatureGroupRefElement(child, ComplexReferenceParentType.FeatureGroup, id.Id);
                            break;
                        case "FeatureRef":
                            this.ParseFeatureRefElement(child, ComplexReferenceParentType.FeatureGroup, id.Id);
                            break;
                        case "MergeRef":
                            this.ParseMergeRefElement(child, ComplexReferenceParentType.FeatureGroup, id.Id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixFeatureGroup", id);

                //Add this FeatureGroup and its parent in WixGroup.
                this.core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.FeatureGroup, id.Id);
            }
        }

        /// <summary>
        /// Parses a feature group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The type of parent.</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private void ParseFeatureGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            Debug.Assert(ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.FeatureGroup == parentType || ComplexReferenceParentType.ComponentGroup == parentType || ComplexReferenceParentType.Product == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType ignoreParent = YesNoType.NotSet;
            YesNoType primary = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixFeatureGroup", id);
                            break;
                        case "IgnoreParent":
                            ignoreParent = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Primary":
                            primary = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                if (YesNoType.Yes != ignoreParent)
                {
                    this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.FeatureGroup, id, (YesNoType.Yes == primary));
                }
            }
        }

        /// <summary>
        /// Parses an environment element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseEnvironmentElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string action = null;
            string name = null;
            Wix.Environment.PartType partType = Wix.Environment.PartType.NotSet;
            string part = null;
            bool permanent = false;
            string separator = ";"; // default to ';'
            bool system = false;
            string text = null;
            string uninstall = "-"; // default to remove at uninstall

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            string value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < value.Length)
                            {
                                Wix.Environment.ActionType actionType = Wix.Environment.ParseActionType(value);
                                switch (actionType)
                                {
                                    case Wix.Environment.ActionType.create:
                                        action = "+";
                                        break;
                                    case Wix.Environment.ActionType.set:
                                        action = "=";
                                        break;
                                    case Wix.Environment.ActionType.remove:
                                        action = "!";
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "create", "set", "remove"));
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Part":
                            part = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Wix.Environment.TryParsePartType(part, out partType))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Part", part, "all", "first", "last"));
                            }
                            break;
                        case "Permanent":
                            permanent = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Separator":
                            separator = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "System":
                            system = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("env", action, name, part, system.ToString());
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (Wix.Environment.PartType.NotSet != partType)
            {
                if ("+" == action)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Part", "Action", "create"));
                }

                switch (partType)
                {
                    case Wix.Environment.PartType.all:
                        break;
                    case Wix.Environment.PartType.first:
                        text = String.Concat(text, separator, "[~]");
                        break;
                    case Wix.Environment.PartType.last:
                        text = String.Concat("[~]", separator, text);
                        break;
                }
            }

            if (permanent)
            {
                uninstall = null;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Environment", id);
                row[1] = String.Concat(action, uninstall, system ? "*" : String.Empty, name);
                row[2] = text;
                row[3] = componentId;
            }
        }

        /// <summary>
        /// Parses an error element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseErrorElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int id = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = CompilerConstants.IllegalInteger;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Error");
                row[0] = id;
                row[1] = Common.GetInnerText(node); // TODO: *
            }
        }

        /// <summary>
        /// Parses an extension element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="advertise">Flag if this extension is advertised.</param>
        /// <param name="progId">ProgId for extension.</param>
        private void ParseExtensionElement(XElement node, string componentId, YesNoType advertise, string progId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string extension = null;
            string mime = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            extension = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            YesNoType extensionAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if ((YesNoType.No == advertise && YesNoType.Yes == extensionAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == extensionAdvertise))
                            {
                                this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, extensionAdvertise.ToString(), advertise.ToString()));
                            }
                            advertise = extensionAdvertise;
                            break;
                        case "ContentType":
                            mime = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ProgId", progId }, { "ComponentId", componentId } };
                    this.core.ParseExtensionAttribute(node, attrib, context);
                }
            }

            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Verb":
                            this.ParseVerbElement(child, extension, progId, componentId, advertise);
                            break;
                        case "MIME":
                            string newMime = this.ParseMIMEElement(child, extension, componentId, advertise);
                            if (null != newMime && null == mime)
                            {
                                mime = newMime;
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (YesNoType.Yes == advertise)
            {
                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "Extension");
                    row[0] = extension;
                    row[1] = componentId;
                    row[2] = progId;
                    row[3] = mime;
                    row[4] = Guid.Empty.ToString("B");

                    this.core.EnsureTable(sourceLineNumbers, "Verb");
                }
            }
            else if (YesNoType.No == advertise)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), String.Empty, progId, componentId); // Extension
                if (null != mime)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), "Content Type", mime, componentId); // Extension's MIME ContentType
                }
            }
        }


        /// <summary>
        /// Parses a file element.
        /// </summary>
        /// <param name="node">File element to parse.</param>
        /// <param name="componentId">Parent's component id.</param>
        /// <param name="directoryId">Ancestor's directory id.</param>
        /// <param name="diskId">Disk id inherited from parent component.</param>
        /// <param name="sourcePath">Default source path of parent directory.</param>
        /// <param name="possibleKeyPath">This will be set with the possible keyPath for the parent component.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private YesNoType ParseFileElement(XElement node, string componentId, string directoryId, int diskId, string sourcePath, out string possibleKeyPath, bool win64Component, string componentGuid)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            FileAssemblyType assemblyType = FileAssemblyType.NotAnAssembly;
            string assemblyApplication = null;
            string assemblyManifest = null;
            string bindPath = null;
            int bits = MsiInterop.MsidbFileAttributesVital; // assume all files are vital.
            string companionFile = null;
            string defaultLanguage = null;
            int defaultSize = 0;
            string defaultVersion = null;
            string fontTitle = null;
            bool generatedShortFileName = false;
            YesNoType keyPath = YesNoType.NotSet;
            string name = null;
            int patchGroup = CompilerConstants.IntegerNotSet;
            bool patchIgnore = false;
            bool patchIncludeWholeFile = false;
            bool patchAllowIgnoreOnError = false;

            string ignoreLengths = null;
            string ignoreOffsets = null;
            string protectLengths = null;
            string protectOffsets = null;
            string symbols = null;

            string procArch = null;
            int selfRegCost = CompilerConstants.IntegerNotSet;
            string shortName = null;
            string source = sourcePath;   // assume we'll use the parents as the source for this file
            bool sourceSet = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Assembly":
                            string assemblyValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < assemblyValue.Length)
                            {
                                Wix.File.AssemblyType parsedAssemblyType = Wix.File.ParseAssemblyType(assemblyValue);
                                switch (parsedAssemblyType)
                                {
                                    case Wix.File.AssemblyType.net:
                                        assemblyType = FileAssemblyType.DotNetAssembly;
                                        break;
                                    case Wix.File.AssemblyType.no:
                                        assemblyType = FileAssemblyType.NotAnAssembly;
                                        break;
                                    case Wix.File.AssemblyType.win32:
                                        assemblyType = FileAssemblyType.Win32Assembly;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "File", "Assembly", assemblyValue, "no", "win32", ".net"));
                                        break;
                                }
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", assemblyApplication);
                            break;
                        case "AssemblyManifest":
                            assemblyManifest = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", assemblyManifest);
                            break;
                        case "BindPath":
                            bindPath = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Checksum":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbFileAttributesChecksum;
                            }
                            break;
                        case "CompanionFile":
                            companionFile = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", companionFile);
                            break;
                        case "Compressed":
                            YesNoDefaultType compressed = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            if (YesNoDefaultType.Yes == compressed)
                            {
                                bits |= MsiInterop.MsidbFileAttributesCompressed;
                            }
                            else if (YesNoDefaultType.No == compressed)
                            {
                                bits |= MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            break;
                        case "DefaultLanguage":
                            defaultLanguage = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultSize":
                            defaultSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "DefaultVersion":
                            defaultVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FontTitle":
                            fontTitle = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbFileAttributesHidden;
                            }
                            break;
                        case "KeyPath":
                            keyPath = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "PatchGroup":
                            patchGroup = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "PatchIgnore":
                            patchIgnore = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PatchWholeFile":
                            patchIncludeWholeFile = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PatchAllowIgnoreOnError":
                            patchAllowIgnoreOnError = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ProcessorArchitecture":
                            string procArchValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < procArchValue.Length)
                            {
                                Wix.File.ProcessorArchitectureType procArchType = Wix.File.ParseProcessorArchitectureType(procArchValue);
                                switch (procArchType)
                                {
                                    case Wix.File.ProcessorArchitectureType.msil:
                                        procArch = "MSIL";
                                        break;
                                    case Wix.File.ProcessorArchitectureType.x86:
                                        procArch = "x86";
                                        break;
                                    case Wix.File.ProcessorArchitectureType.x64:
                                        procArch = "amd64";
                                        break;
                                    case Wix.File.ProcessorArchitectureType.ia64:
                                        procArch = "ia64";
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "File", "ProcessorArchitecture", procArchValue, "msil", "x86", "x64", "ia64"));
                                        break;
                                }
                            }
                            break;
                        case "ReadOnly":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbFileAttributesReadOnly;
                            }
                            break;
                        case "SelfRegCost":
                            selfRegCost = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Source":
                            source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            sourceSet = true;
                            break;
                        case "System":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbFileAttributesSystem;
                            }
                            break;
                        case "TrueType":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                fontTitle = String.Empty;
                            }
                            break;
                        case "Vital":
                            YesNoType isVital = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == isVital)
                            {
                                bits |= MsiInterop.MsidbFileAttributesVital;
                            }
                            else if (YesNoType.No == isVital)
                            {
                                bits &= ~MsiInterop.MsidbFileAttributesVital;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != companionFile)
            {
                // the companion file cannot be the key path of a component
                if (YesNoType.Yes == keyPath)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "CompanionFile", "KeyPath", "yes"));
                }
            }

            if (sourceSet && !source.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) && null == name)
            {
                name = Path.GetFileName(source);
                if (!this.core.IsValidLongFilename(name, false))
                {
                    this.core.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            // generate a short file name
            if (null == shortName && (null != name && !this.core.IsValidShortFilename(name, false)))
            {
                shortName = this.core.CreateShortName(name, true, false, node.Name.LocalName, directoryId);
                generatedShortFileName = true;
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("fil", directoryId, name ?? shortName);
            }

            if (!this.compilingModule && CompilerConstants.IntegerNotSet == diskId)
            {
                diskId = 1; // default to first Media
            }

            if (null != defaultVersion && null != companionFile)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DefaultVersion", "CompanionFile", companionFile));
            }

            if (FileAssemblyType.NotAnAssembly == assemblyType)
            {
                if (null != assemblyManifest)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", "AssemblyManifest"));
                }

                if (null != assemblyApplication)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", "AssemblyApplication"));
                }
            }
            else
            {
                if (FileAssemblyType.Win32Assembly == assemblyType && null == assemblyManifest)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AssemblyManifest", "Assembly", "win32"));
                }

                // allow "*" guid components to omit explicit KeyPath as they can have only one file and therefore this file is the keypath
                if (YesNoType.Yes != keyPath && "*" != componentGuid)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", (FileAssemblyType.DotNetAssembly == assemblyType ? ".net" : "win32"), "KeyPath", "yes"));
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "AppId":
                            this.ParseAppIdElement(child, componentId, YesNoType.NotSet, id.Id, null, null);
                            break;
                        case "AssemblyName":
                            this.ParseAssemblyName(child, componentId);
                            break;
                        case "Class":
                            this.ParseClassElement(child, componentId, YesNoType.NotSet, id.Id, null, null, null);
                            break;
                        case "CopyFile":
                            this.ParseCopyFileElement(child, componentId, id.Id);
                            break;
                        case "IgnoreRange":
                            this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
                            break;
                        case "ODBCDriver":
                            this.ParseODBCDriverOrTranslator(child, componentId, id.Id, this.tableDefinitions["ODBCDriver"]);
                            break;
                        case "ODBCTranslator":
                            this.ParseODBCDriverOrTranslator(child, componentId, id.Id, this.tableDefinitions["ODBCTranslator"]);
                            break;
                        case "Permission":
                            this.ParsePermissionElement(child, id.Id, "File");
                            break;
                        case "PermissionEx":
                            this.ParsePermissionExElement(child, id.Id, "File");
                            break;
                        case "ProtectRange":
                            this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                            break;
                        case "Shortcut":
                            this.ParseShortcutElement(child, componentId, node.Name.LocalName, id.Id, keyPath);
                            break;
                        case "SymbolPath":
                            if (null != symbols)
                            {
                                symbols += ";" + this.ParseSymbolPathElement(child);
                            }
                            else
                            {
                                symbols = this.ParseSymbolPathElement(child);
                            }
                            break;
                        case "TypeLib":
                            this.ParseTypeLibElement(child, componentId, id.Id, win64Component);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "FileId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }


            if (!this.core.EncounteredError)
            {
                PatchAttributeType patchAttributes = PatchAttributeType.None;
                if (patchIgnore)
                {
                    patchAttributes |= PatchAttributeType.Ignore;
                }
                if (patchIncludeWholeFile)
                {
                    patchAttributes |= PatchAttributeType.IncludeWholeFile;
                }
                if (patchAllowIgnoreOnError)
                {
                    patchAttributes |= PatchAttributeType.AllowIgnoreOnError;
                }

                if (String.IsNullOrEmpty(source))
                {
                    if (!this.useShortFileNames && null != name)
                    {
                        source = name;
                    }
                    else
                    {
                        source = shortName;
                    }
                }
                else if (source.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) // if source relies on parent directories, append the file name
                {
                    if (!this.useShortFileNames && null != name)
                    {
                        source = Path.Combine(source, name);
                    }
                    else
                    {
                        source = Path.Combine(source, shortName);
                    }
                }

                FileRow fileRow = (FileRow)this.core.CreateRow(sourceLineNumbers, "File", id);
                fileRow[1] = componentId;
                fileRow[2] = GetMsiFilenameValue(shortName, name);
                fileRow[3] = defaultSize;
                if (null != companionFile)
                {
                    fileRow[4] = companionFile;
                }
                else if (null != defaultVersion)
                {
                    fileRow[4] = defaultVersion;
                }
                fileRow[5] = defaultLanguage;
                fileRow[6] = bits;

                // the Sequence row is set in the binder

                WixFileRow wixFileRow = (WixFileRow)this.core.CreateRow(sourceLineNumbers, "WixFile", id);
                wixFileRow.AssemblyType = assemblyType;
                wixFileRow.AssemblyManifest = assemblyManifest;
                wixFileRow.AssemblyApplication = assemblyApplication;
                wixFileRow.Directory = directoryId;
                wixFileRow.DiskId = (CompilerConstants.IntegerNotSet == diskId) ? 0 : diskId;
                wixFileRow.Source = source;
                wixFileRow.ProcessorArchitecture = procArch;
                wixFileRow.PatchGroup = (CompilerConstants.IntegerNotSet != patchGroup ? patchGroup : -1);
                wixFileRow.Attributes = (generatedShortFileName ? 0x1 : 0x0);
                wixFileRow.PatchAttributes = patchAttributes;

                // Always create a delta patch row for this file since other elements (like Component and Media) may
                // want to add symbol paths to it.
                WixDeltaPatchFileRow deltaPatchFileRow = (WixDeltaPatchFileRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchFile", id);
                deltaPatchFileRow.RetainLengths = protectLengths;
                deltaPatchFileRow.IgnoreOffsets = ignoreOffsets;
                deltaPatchFileRow.IgnoreLengths = ignoreLengths;
                deltaPatchFileRow.RetainOffsets = protectOffsets;

                if (null != symbols)
                {
                    WixDeltaPatchSymbolPathsRow symbolRow = (WixDeltaPatchSymbolPathsRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchSymbolPaths", id);
                    symbolRow.Type = SymbolPathType.File;
                    symbolRow.SymbolPaths = symbols;
                }

                if (FileAssemblyType.NotAnAssembly != assemblyType)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiAssembly");
                    row[0] = componentId;
                    row[1] = Guid.Empty.ToString("B");
                    row[2] = assemblyManifest;
                    row[3] = assemblyApplication;
                    row[4] = (FileAssemblyType.DotNetAssembly == assemblyType) ? 0 : 1;
                }

                if (null != bindPath)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "BindImage");
                    row[0] = id.Id;
                    row[1] = bindPath;

                    // TODO: technically speaking each of the properties in the "bindPath" should be added as references, but how much do we really care about BindImage?
                }

                if (CompilerConstants.IntegerNotSet != selfRegCost)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "SelfReg");
                    row[0] = id.Id;
                    row[1] = selfRegCost;
                }

                if (null != fontTitle)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "Font");
                    row[0] = id.Id;
                    row[1] = fontTitle;
                }
            }

            this.core.CreateSimpleReference(sourceLineNumbers, "Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));

            // If this component does not have a companion file this file is a possible keypath.
            possibleKeyPath = null;
            if (null == companionFile)
            {
                possibleKeyPath = id.Id;
            }

            return keyPath;
        }

        /// <summary>
        /// Parses a file search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentSignature">Signature of parent search element.</param>
        /// <param name="parentDirectorySearch">Whether this search element is used to search for the parent directory.</param>
        /// <param name="parentDepth">The depth specified by the parent search element.</param>
        /// <returns>Signature of search element.</returns>
        private string ParseFileSearchElement(XElement node, string parentSignature, bool parentDirectorySearch, int parentDepth)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string languages = null;
            int minDate = CompilerConstants.IntegerNotSet;
            int maxDate = CompilerConstants.IntegerNotSet;
            int maxSize = CompilerConstants.IntegerNotSet;
            int minSize = CompilerConstants.IntegerNotSet;
            string maxVersion = null;
            string minVersion = null;
            string name = null;
            string shortName = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "MinVersion":
                            minVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxVersion":
                            maxVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinSize":
                            minSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "MaxSize":
                            maxSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "MinDate":
                            minDate = this.core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxDate":
                            maxDate = this.core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            languages = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Using both ShortName and Name will not always work due to a Windows Installer bug.
            if (null != shortName && null != name)
            {
                this.core.OnMessage(WixWarnings.FileSearchFileNameIssue(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name"));
            }
            else if (null == shortName && null == name) // at least one name must be specified.
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (this.core.IsValidShortFilename(name, false))
            {
                if (null == shortName)
                {
                    shortName = name;
                    name = null;
                }
                else
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                }
            }

            if (null == id)
            {
                if (String.IsNullOrEmpty(parentSignature))
                {
                    id = this.core.CreateIdentifier("fs", name ?? shortName);
                }
                else // reuse parent signature in the Signature table
                {
                    id = new Identifier(parentSignature, AccessModifier.Private);
                }
            }

            bool isSameId = String.Equals(id.Id, parentSignature, StringComparison.Ordinal);
            if (parentDirectorySearch)
            {
                // If searching for the parent directory, the Id attribute
                // value must be specified and unique.
                if (isSameId)
                {
                    this.core.OnMessage(WixErrors.UniqueFileSearchIdRequired(sourceLineNumbers, parentSignature, node.Name.LocalName));
                }
            }
            else if (parentDepth > 1)
            {
                // Otherwise, if the depth > 1 the Id must be absent or the same
                // as the parent DirectorySearch if AssignToProperty is not set.
                if (!isSameId)
                {
                    this.core.OnMessage(WixErrors.IllegalSearchIdForParentDepth(sourceLineNumbers, id.Id, parentSignature));
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Signature", id);
                row[1] = name ?? shortName;
                row[2] = minVersion;
                row[3] = maxVersion;

                if (CompilerConstants.IntegerNotSet != minSize)
                {
                    row[4] = minSize;
                }
                if (CompilerConstants.IntegerNotSet != maxSize)
                {
                    row[5] = maxSize;
                }
                if (CompilerConstants.IntegerNotSet != minDate)
                {
                    row[6] = minDate;
                }
                if (CompilerConstants.IntegerNotSet != maxDate)
                {
                    row[7] = maxDate;
                }
                row[8] = languages;

                // Create a DrLocator row to associate the file with a directory
                // when a different identifier is specified for the FileSearch.
                if (!isSameId)
                {
                    if (parentDirectorySearch)
                    {
                        // Creates the DrLocator row for the directory search while
                        // the parent DirectorySearch creates the file locator row.
                        row = this.core.CreateRow(sourceLineNumbers, "DrLocator");
                        row[0] = parentSignature;
                        row[1] = id;
                    }
                    else
                    {
                        row = this.core.CreateRow(sourceLineNumbers, "DrLocator", id);
                        row[1] = parentSignature;
                    }
                }
            }

            return id.Id; // the id of the FileSearch element is its signature
        }


        /// <summary>
        /// Parses a fragment element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseFragmentElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            this.activeName = null;
            this.activeLanguage = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // NOTE: Id is not required for Fragments, this is a departure from the normal run of the mill processing.

            this.core.CreateActiveSection(id, SectionType.Fragment, 0);

            int featureDisplay = 0;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "_locDefinition":
                            break;
                        case "AdminExecuteSequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "AdminUISequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "AdvertiseExecuteSequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "InstallExecuteSequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "InstallUISequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "AppId":
                            this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
                            break;
                        case "Binary":
                            this.ParseBinaryElement(child);
                            break;
                        case "BootstrapperApplication":
                            this.ParseBootstrapperApplicationElement(child);
                            break;
                        case "BootstrapperApplicationRef":
                            this.ParseBootstrapperApplicationRefElement(child);
                            break;
                        case "ComplianceCheck":
                            this.ParseComplianceCheckElement(child);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, CompilerConstants.IntegerNotSet, null, null);
                            break;
                        case "ComponentGroup":
                            this.ParseComponentGroupElement(child, ComplexReferenceParentType.Unknown, id);
                            break;
                        case "Condition":
                            this.ParseConditionElement(child, node.Name.LocalName, null, null);
                            break;
                        case "Container":
                            this.ParseContainerElement(child);
                            break;
                        case "CustomAction":
                            this.ParseCustomActionElement(child);
                            break;
                        case "CustomActionRef":
                            this.ParseSimpleRefElement(child, "CustomAction");
                            break;
                        case "CustomTable":
                            this.ParseCustomTableElement(child);
                            break;
                        case "Directory":
                            this.ParseDirectoryElement(child, null, CompilerConstants.IntegerNotSet, String.Empty);
                            break;
                        case "DirectoryRef":
                            this.ParseDirectoryRefElement(child);
                            break;
                        case "EmbeddedChainer":
                            this.ParseEmbeddedChainerElement(child);
                            break;
                        case "EmbeddedChainerRef":
                            this.ParseSimpleRefElement(child, "MsiEmbeddedChainer");
                            break;
                        case "EnsureTable":
                            this.ParseEnsureTableElement(child);
                            break;
                        case "Feature":
                            this.ParseFeatureElement(child, ComplexReferenceParentType.Unknown, null, ref featureDisplay);
                            break;
                        case "FeatureGroup":
                            this.ParseFeatureGroupElement(child, ComplexReferenceParentType.Unknown, id);
                            break;
                        case "FeatureRef":
                            this.ParseFeatureRefElement(child, ComplexReferenceParentType.Unknown, null);
                            break;
                        case "Icon":
                            this.ParseIconElement(child);
                            break;
                        case "IgnoreModularization":
                            this.ParseIgnoreModularizationElement(child);
                            break;
                        case "Media":
                            this.ParseMediaElement(child, null);
                            break;
                        case "MediaTemplate":
                            this.ParseMediaTemplateElement(child, null);
                            break;
                        case "PackageGroup":
                            this.ParsePackageGroupElement(child);
                            break;
                        case "PackageCertificates":
                        case "PatchCertificates":
                            this.ParseCertificatesElement(child);
                            break;
                        case "PatchFamily":
                            this.ParsePatchFamilyElement(child, ComplexReferenceParentType.Unknown, id);
                            break;
                        case "PatchFamilyGroup":
                            this.ParsePatchFamilyGroupElement(child, ComplexReferenceParentType.Unknown, id);
                            break;
                        case "PatchFamilyGroupRef":
                            this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.Unknown, id);
                            break;
                        case "PayloadGroup":
                            this.ParsePayloadGroupElement(child, ComplexReferenceParentType.Unknown, null);
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        case "PropertyRef":
                            this.ParseSimpleRefElement(child, "Property");
                            break;
                        case "RelatedBundle":
                            this.ParseRelatedBundleElement(child);
                            break;
                        case "SetDirectory":
                            this.ParseSetDirectoryElement(child);
                            break;
                        case "SetProperty":
                            this.ParseSetPropertyElement(child);
                            break;
                        case "SFPCatalog":
                            string parentName = null;
                            this.ParseSFPCatalogElement(child, ref parentName);
                            break;
                        case "UI":
                            this.ParseUIElement(child);
                            break;
                        case "UIRef":
                            this.ParseSimpleRefElement(child, "WixUI");
                            break;
                        case "Upgrade":
                            this.ParseUpgradeElement(child);
                            break;
                        case "Variable":
                            this.ParseVariableElement(child);
                            break;
                        case "WixVariable":
                            this.ParseWixVariableElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError && null != id)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixFragment");
                row[0] = id;
            }
        }


        /// <summary>
        /// Parses a condition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentElementLocalName">LocalName of the parent element.</param>
        /// <param name="id">Id of the parent element.</param>
        /// <param name="dialog">Dialog of the parent element if its a Control.</param>
        /// <returns>The condition if one was found.</returns>
        private string ParseConditionElement(XElement node, string parentElementLocalName, string id, string dialog)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            string condition = null;
            int level = CompilerConstants.IntegerNotSet;
            string message = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            if ("Control" == parentElementLocalName)
                            {
                                action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                                if (0 < action.Length)
                                {
                                    Wix.Condition.ActionType actionType;
                                    if (Wix.Condition.TryParseActionType(action, out actionType))
                                    {
                                        action = Compiler.UppercaseFirstChar(action);
                                    }
                                    else
                                    {
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "default", "disable", "enable", "hide", "show"));
                                    }
                                }
                            }
                            else
                            {
                                this.core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        case "Level":
                            if ("Feature" == parentElementLocalName)
                            {
                                level = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            }
                            else
                            {
                                this.core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        case "Message":
                            if ("Fragment" == parentElementLocalName || "Product" == parentElementLocalName)
                            {
                                message = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // get the condition from the inner text of the element
            condition = this.core.GetConditionInnerText(node);

            this.core.ParseForExtensionElements(node);

            // the condition should not be empty
            if (null == condition || 0 == condition.Length)
            {
                condition = null;
                this.core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, node.Name.LocalName));
            }

            switch (parentElementLocalName)
            {
                case "Control":
                    if (null == action)
                    {
                        this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
                    }

                    if (!this.core.EncounteredError)
                    {
                        Row row = this.core.CreateRow(sourceLineNumbers, "ControlCondition");
                        row[0] = dialog;
                        row[1] = id;
                        row[2] = action;
                        row[3] = condition;
                    }
                    break;
                case "Feature":
                    if (CompilerConstants.IntegerNotSet == level)
                    {
                        this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Level"));
                        level = CompilerConstants.IllegalInteger;
                    }

                    if (!this.core.EncounteredError)
                    {
                        Row row = this.core.CreateRow(sourceLineNumbers, "Condition");
                        row[0] = id;
                        row[1] = level;
                        row[2] = condition;
                    }
                    break;
                case "Fragment":
                case "Product":
                    if (null == message)
                    {
                        this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Message"));
                    }

                    if (!this.core.EncounteredError)
                    {
                        Row row = this.core.CreateRow(sourceLineNumbers, "LaunchCondition");
                        row[0] = condition;
                        row[1] = message;
                    }
                    break;
            }

            return condition;
        }

        /// <summary>
        /// Parses a IniFile element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of the parent component.</param>
        private void ParseIniFileElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int action = CompilerConstants.IntegerNotSet;
            string directory = null;
            string key = null;
            string name = null;
            string section = null;
            string shortName = null;
            string tableName = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            string actionValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < actionValue.Length)
                            {
                                Wix.IniFile.ActionType actionType = Wix.IniFile.ParseActionType(actionValue);
                                switch (actionType)
                                {
                                    case Wix.IniFile.ActionType.addLine:
                                        action = MsiInterop.MsidbIniFileActionAddLine;
                                        break;
                                    case Wix.IniFile.ActionType.addTag:
                                        action = MsiInterop.MsidbIniFileActionAddTag;
                                        break;
                                    case Wix.IniFile.ActionType.createLine:
                                        action = MsiInterop.MsidbIniFileActionCreateLine;
                                        break;
                                    case Wix.IniFile.ActionType.removeLine:
                                        action = MsiInterop.MsidbIniFileActionRemoveLine;
                                        break;
                                    case Wix.IniFile.ActionType.removeTag:
                                        action = MsiInterop.MsidbIniFileActionRemoveTag;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", actionValue, "addLine", "addTag", "createLine", "removeLine", "removeTag"));
                                        break;
                                }
                            }
                            break;
                        case "Directory":
                            directory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Section":
                            section = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == action)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
                action = CompilerConstants.IllegalInteger;
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else // generate a short file name.
                {
                    if (null == shortName)
                    {
                        shortName = this.core.CreateShortName(name, true, false, node.Name.LocalName, componentId);
                    }
                }
            }

            if (null == section)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Section"));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("ini", directory, name ?? shortName, section, key, name);
            }

            this.core.ParseForExtensionElements(node);

            if (MsiInterop.MsidbIniFileActionRemoveLine == action || MsiInterop.MsidbIniFileActionRemoveTag == action)
            {
                tableName = "RemoveIniFile";
            }
            else
            {
                if (null == value)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
                }

                tableName = "IniFile";
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, tableName, id);
                row[1] = GetMsiFilenameValue(shortName, name);
                row[2] = directory;
                row[3] = section;
                row[4] = key;
                row[5] = value;
                row[6] = action;
                row[7] = componentId;
            }
        }

        /// <summary>
        /// Parses an IniFile search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private string ParseIniFileSearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int field = CompilerConstants.IntegerNotSet;
            string key = null;
            string name = null;
            string section = null;
            string shortName = null;
            string signature = null;
            int type = 1; // default is file

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Field":
                            field = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Section":
                            section = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                Wix.IniFileSearch.TypeType typeType = Wix.IniFileSearch.ParseTypeType(typeValue);
                                switch (typeType)
                                {
                                    case Wix.IniFileSearch.TypeType.directory:
                                        type = 0;
                                        break;
                                    case Wix.IniFileSearch.TypeType.file:
                                        type = 1;
                                        break;
                                    case Wix.IniFileSearch.TypeType.raw:
                                        type = 2;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "registry"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.core.CreateShortName(name, true, false, node.Name.LocalName);
                }
            }

            if (null == section)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Section"));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("ini", name, section, key, field.ToString(), type.ToString());
            }

            signature = id.Id;

            bool oneChild = false;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "DirectorySearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "IniLocator", id);
                row[1] = GetMsiFilenameValue(shortName, name);
                row[2] = section;
                row[3] = key;
                if (CompilerConstants.IntegerNotSet != field)
                {
                    row[4] = field;
                }
                row[5] = type;
            }

            return signature;
        }

        /// <summary>
        /// Parses an isolated component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseIsolateComponentElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string shared = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Shared":
                            shared = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Component", shared);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == shared)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Shared"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "IsolatedComponent");
                row[0] = shared;
                row[1] = componentId;
            }
        }

        /// <summary>
        /// Parses a PatchCertificates or PackageCertificates element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseCertificatesElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // no attributes are supported for this element
            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    this.core.UnexpectedAttribute(node, attrib);
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "DigitalCertificate":
                            string name = this.ParseDigitalCertificateElement(child);

                            if (!this.core.EncounteredError)
                            {
                                Row row = this.core.CreateRow(sourceLineNumbers, "PatchCertificates" == node.Name.LocalName ? "MsiPatchCertificate" : "MsiPackageCertificate");
                                row[0] = name;
                                row[1] = name;
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses an digital certificate element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>The identifier of the certificate.</returns>
        private string ParseDigitalCertificateElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (40 < id.Id.Length)
            {
                this.core.OnMessage(WixErrors.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 40));

                // No need to check for modularization problems since DigitalSignature and thus DigitalCertificate
                // currently have no usage in merge modules.
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiDigitalCertificate", id);
                row[1] = sourceFile;
            }

            return id.Id;
        }

        /// <summary>
        /// Parses an digital signature element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="diskId">Disk id inherited from parent media.</param>
        private void ParseDigitalSignatureElement(XElement node, string diskId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string certificateId = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // sanity check for debug to ensure the stream name will not be a problem
            if (null != sourceFile)
            {
                Debug.Assert(62 >= "MsiDigitalSignature.Media.".Length + diskId.Length);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "DigitalCertificate":
                            certificateId = this.ParseDigitalCertificateElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null == certificateId)
            {
                this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "DigitalCertificate"));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiDigitalSignature");
                row[0] = "Media";
                row[1] = diskId;
                row[2] = certificateId;
                row[3] = sourceFile;
            }
        }

        /// <summary>
        /// Parses a MajorUpgrade element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="parentElement">The parent element.</param>
        private void ParseMajorUpgradeElement(XElement node, IDictionary<string, string> contextValues)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int options = MsiInterop.MsidbUpgradeAttributesMigrateFeatures;
            bool allowDowngrades = false;
            bool allowSameVersionUpgrades = false;
            bool blockUpgrades = false;
            string downgradeErrorMessage = null;
            string disallowUpgradeErrorMessage = null;
            string removeFeatures = null;
            string schedule = null;

            string upgradeCode = contextValues["UpgradeCode"];
            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.core.OnMessage(WixErrors.ParentElementAttributeRequired(sourceLineNumbers, "Product", "UpgradeCode", node.Name.LocalName));
            }

            string productVersion = contextValues["ProductVersion"];
            if (String.IsNullOrEmpty(productVersion))
            {
                this.core.OnMessage(WixErrors.ParentElementAttributeRequired(sourceLineNumbers, "Product", "Version", node.Name.LocalName));
            }

            string productLanguage = contextValues["ProductLanguage"];

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AllowDowngrades":
                            allowDowngrades = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowSameVersionUpgrades":
                            allowSameVersionUpgrades = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Disallow":
                            blockUpgrades = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DowngradeErrorMessage":
                            downgradeErrorMessage = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisallowUpgradeErrorMessage":
                            disallowUpgradeErrorMessage = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MigrateFeatures":
                            if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesMigrateFeatures;
                            }
                            break;
                        case "IgnoreLanguage":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                productLanguage = null;
                            }
                            break;
                        case "IgnoreRemoveFailure":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesIgnoreRemoveFailure;
                            }
                            break;
                        case "RemoveFeatures":
                            removeFeatures = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Schedule":
                            schedule = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!allowDowngrades && String.IsNullOrEmpty(downgradeErrorMessage))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DowngradeErrorMessage", "AllowDowngrades", "yes", true));
            }

            if (allowDowngrades && !String.IsNullOrEmpty(downgradeErrorMessage))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DowngradeErrorMessage", "AllowDowngrades", "yes"));
            }

            if (allowDowngrades && allowSameVersionUpgrades)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "AllowSameVersionUpgrades", "AllowDowngrades", "yes"));
            }

            if (blockUpgrades && String.IsNullOrEmpty(disallowUpgradeErrorMessage))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisallowUpgradeErrorMessage", "Disallow", "yes", true));
            }

            if (!blockUpgrades && !String.IsNullOrEmpty(disallowUpgradeErrorMessage))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DisallowUpgradeErrorMessage", "Disallow", "yes"));
            }

            if (!this.core.EncounteredError)
            {
                // create the row that performs the upgrade (or downgrade)
                Row row = this.core.CreateRow(sourceLineNumbers, "Upgrade");
                row[0] = upgradeCode;
                if (allowDowngrades)
                {
                    row[1] = "0"; // let any version satisfy
                    // row[2] = maximum version; omit so we don't have to fake a version like "255.255.65535";
                    row[3] = productLanguage;
                    row[4] = options | MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                }
                else
                {
                    // row[1] = minimum version; skip it so we detect all prior versions.
                    row[2] = productVersion;
                    row[3] = productLanguage;
                    row[4] = allowSameVersionUpgrades ? (options | MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive) : options;
                }

                row[5] = removeFeatures;
                row[6] = Compiler.UpgradeDetectedProperty;

                // Ensure the action property is secure.
                this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Compiler.UpgradeDetectedProperty, AccessModifier.Public), false, true, false);

                // Add launch condition that blocks upgrades
                if (blockUpgrades)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "LaunchCondition");
                    row[0] = Compiler.UpgradePreventedCondition;
                    row[1] = disallowUpgradeErrorMessage;
                }

                // now create the Upgrade row and launch conditions to prevent downgrades (unless explicitly permitted)
                if (!allowDowngrades)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "Upgrade");
                    row[0] = upgradeCode;
                    row[1] = productVersion;
                    // row[2] = maximum version; skip it so we detect all future versions.
                    row[3] = productLanguage;
                    row[4] = MsiInterop.MsidbUpgradeAttributesOnlyDetect;
                    // row[5] = removeFeatures;
                    row[6] = Compiler.DowngradeDetectedProperty;

                    // Ensure the action property is secure.
                    this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Compiler.DowngradeDetectedProperty, AccessModifier.Public), false, true, false);

                    row = this.core.CreateRow(sourceLineNumbers, "LaunchCondition");
                    row[0] = Compiler.DowngradePreventedCondition;
                    row[1] = downgradeErrorMessage;
                }

                // finally, schedule RemoveExistingProducts
                row = this.core.CreateRow(sourceLineNumbers, "WixAction");
                row[0] = "InstallExecuteSequence";
                row[1] = "RemoveExistingProducts";
                // row[2] = condition;
                // row[3] = sequence;
                row[6] = 0; // overridable

                switch (schedule)
                {
                    case null:
                    case "afterInstallValidate":
                        // row[4] = beforeAction;
                        row[5] = "InstallValidate";
                        break;
                    case "afterInstallInitialize":
                        // row[4] = beforeAction;
                        row[5] = "InstallInitialize";
                        break;
                    case "afterInstallExecute":
                        // row[4] = beforeAction;
                        row[5] = "InstallExecute";
                        break;
                    case "afterInstallExecuteAgain":
                        // row[4] = beforeAction;
                        row[5] = "InstallExecuteAgain";
                        break;
                    case "afterInstallFinalize":
                        // row[4] = beforeAction;
                        row[5] = "InstallFinalize";
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a media element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="patchId">Set to the PatchId if parsing Patch/Media element otherwise null.</param>
        private void ParseMediaElement(XElement node, string patchId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int id = CompilerConstants.IntegerNotSet;
            string cabinet = null;
            CompressionLevel? compressionLevel = null;
            string diskPrompt = null;
            string layout = null;
            bool patch = null != patchId;
            string volumeLabel = null;
            string source = null;
            string symbols = null;

            YesNoType embedCab = patch ? YesNoType.Yes : YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Cabinet":
                            cabinet = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CompressionLevel":
                            string compressionLevelString = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < compressionLevelString.Length)
                            {
                                Wix.CompressionLevelType compressionLevelType;
                                if (!Wix.Enums.TryParseCompressionLevelType(compressionLevelString, out compressionLevelType))
                                {
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, compressionLevelString, "high", "low", "medium", "mszip", "none"));
                                }
                                else
                                {
                                    compressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), compressionLevelString, true);
                                }
                            }
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
                            break;
                        case "EmbedCab":
                            embedCab = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Layout":
                        case "src":
                            if (null != layout)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Layout", "src"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Layout"));
                            }
                            layout = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Source":
                            source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = CompilerConstants.IllegalInteger;
            }

            if (YesNoType.IllegalValue != embedCab)
            {
                if (YesNoType.Yes == embedCab)
                {
                    if (null == cabinet)
                    {
                        this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Cabinet", "EmbedCab", "yes"));
                    }
                    else
                    {
                        if (62 < cabinet.Length)
                        {
                            this.core.OnMessage(WixErrors.MediaEmbeddedCabinetNameTooLong(sourceLineNumbers, node.Name.LocalName, "Cabinet", cabinet, cabinet.Length));
                        }

                        cabinet = String.Concat("#", cabinet);
                    }
                }
                else // external cabinet file
                {
                    // external cabinet files must use 8.3 filenames
                    if (!String.IsNullOrEmpty(cabinet) && !this.core.IsValidShortFilename(cabinet, false))
                    {
                        // WiX variables in the name will trip the "not a valid 8.3 name" switch, so let them through
                        if (!Common.WixVariableRegex.Match(cabinet).Success)
                        {
                            this.core.OnMessage(WixWarnings.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "Cabinet", cabinet));
                        }
                    }
                }
            }

            if (null != compressionLevel && null == cabinet)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Cabinet", "CompressionLevel"));
            }

            if (patch)
            {
                // Default Source to a form of the Patch Id if none is specified.
                if (null == source)
                {
                    source = String.Concat("_", new Guid(patchId).ToString("N", CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture));
                }
            }

            foreach (XElement child in node.Elements())
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "DigitalSignature":
                            if (YesNoType.Yes == embedCab)
                            {
                                this.core.OnMessage(WixErrors.SignedEmbeddedCabinet(childSourceLineNumbers));
                            }
                            else if (null == cabinet)
                            {
                                this.core.OnMessage(WixErrors.ExpectedSignedCabinetName(childSourceLineNumbers));
                            }
                            else
                            {
                                this.ParseDigitalSignatureElement(child, id.ToString(CultureInfo.InvariantCulture.NumberFormat));
                            }
                            break;
                        case "PatchBaseline":
                            if (patch)
                            {
                                this.ParsePatchBaselineElement(child, id);
                            }
                            else
                            {
                                this.core.UnexpectedElement(node, child);
                            }
                            break;
                        case "SymbolPath":
                            if (null != symbols)
                            {
                                symbols += "" + this.ParseSymbolPathElement(child);
                            }
                            else
                            {
                                symbols = this.ParseSymbolPathElement(child);
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }



            // add the row to the section
            if (!this.core.EncounteredError)
            {
                MediaRow mediaRow = (MediaRow)this.core.CreateRow(sourceLineNumbers, "Media");
                mediaRow.DiskId = id;
                mediaRow.LastSequence = 0; // this is set in the binder
                mediaRow.DiskPrompt = diskPrompt;
                mediaRow.Cabinet = cabinet;
                mediaRow.VolumeLabel = volumeLabel;
                mediaRow.Source = source;

                // the Source column is only set when creating a patch

                if (null != compressionLevel || null != layout)
                {
                    WixMediaRow row = (WixMediaRow)this.core.CreateRow(sourceLineNumbers, "WixMedia");
                    row.DiskId = id;
                    row.CompressionLevel = compressionLevel;
                    row.Layout = layout;
                }

                if (null != symbols)
                {
                    WixDeltaPatchSymbolPathsRow symbolRow = (WixDeltaPatchSymbolPathsRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchSymbolPaths");
                    symbolRow.Id = id.ToString(CultureInfo.InvariantCulture);
                    symbolRow.Type = SymbolPathType.Media;
                    symbolRow.SymbolPaths = symbols;
                }
            }
        }

        /// <summary>
        /// Parses a media template element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="patchId">Set to the PatchId if parsing Patch/Media element otherwise null.</param>
        private void ParseMediaTemplateElement(XElement node, string patchId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string cabinetTemplate = "cab{0}.cab";
            string compressionLevel = null; // this defaults to mszip in Binder
            string diskPrompt = null;
            bool patch = null != patchId;
            string volumeLabel = null;
            int maximumUncompressedMediaSize = CompilerConstants.IntegerNotSet;
            int maximumCabinetSizeForLargeFileSplitting = CompilerConstants.IntegerNotSet;
            Wix.CompressionLevelType compressionLevelType = Wix.CompressionLevelType.NotSet;

            YesNoType embedCab = patch ? YesNoType.Yes : YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CabinetTemplate":
                            string authoredCabinetTemplateValue = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            if (!String.IsNullOrEmpty(authoredCabinetTemplateValue))
                            {
                                cabinetTemplate = authoredCabinetTemplateValue;
                            }

                            // Create an example cabinet name using the maximum number of cabinets supported, 999.
                            string exampleCabinetName = String.Format(cabinetTemplate, "###");
                            if (!this.core.IsValidLocIdentifier(exampleCabinetName))
                            {
                                // The example name should not match the authored template since that would nullify the
                                // reason for having multiple cabients. External cabinet files must also be valid file names.
                                if (exampleCabinetName.Equals(authoredCabinetTemplateValue) || !this.core.IsValidLongFilename(exampleCabinetName, false))
                                {
                                    this.core.OnMessage(WixErrors.InvalidCabinetTemplate(sourceLineNumbers, cabinetTemplate));
                                }
                                else if (!this.core.IsValidShortFilename(exampleCabinetName, false) && !Common.WixVariableRegex.Match(exampleCabinetName).Success) // ignore short names with wix variables because it rarely works out.
                                {
                                    this.core.OnMessage(WixWarnings.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "CabinetTemplate", cabinetTemplate));
                                }
                            }
                            break;
                        case "CompressionLevel":
                            compressionLevel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < compressionLevel.Length)
                            {
                                if (!Wix.Enums.TryParseCompressionLevelType(compressionLevel, out compressionLevelType))
                                {
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, compressionLevel, "high", "low", "medium", "mszip", "none"));
                                }
                            }
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
                            this.core.OnMessage(WixWarnings.ReservedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "EmbedCab":
                            embedCab = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.OnMessage(WixWarnings.ReservedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "MaximumUncompressedMediaSize":
                            maximumUncompressedMediaSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "MaximumCabinetSizeForLargeFileSplitting":
                            maximumCabinetSizeForLargeFileSplitting = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, CompilerCore.MinValueOfMaxCabSizeForLargeFileSplitting, CompilerCore.MaxValueOfMaxCabSizeForLargeFileSplitting);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (YesNoType.IllegalValue != embedCab)
            {
                if (YesNoType.Yes == embedCab)
                {
                    cabinetTemplate = String.Concat("#", cabinetTemplate);
                }
            }

            if (!this.core.EncounteredError)
            {
                MediaRow temporaryMediaRow = (MediaRow)this.core.CreateRow(sourceLineNumbers, "Media");
                temporaryMediaRow.DiskId = 1;
                WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)this.core.CreateRow(sourceLineNumbers, "WixMediaTemplate");
                mediaTemplateRow.CabinetTemplate = cabinetTemplate;
                mediaTemplateRow.VolumeLabel = volumeLabel;
                mediaTemplateRow.DiskPrompt = diskPrompt;
                mediaTemplateRow.VolumeLabel = volumeLabel;

                if (maximumUncompressedMediaSize != CompilerConstants.IntegerNotSet)
                {
                    mediaTemplateRow.MaximumUncompressedMediaSize = maximumUncompressedMediaSize;
                }
                else
                {
                    mediaTemplateRow.MaximumUncompressedMediaSize = CompilerCore.DefaultMaximumUncompressedMediaSize;
                }

                if (maximumCabinetSizeForLargeFileSplitting != CompilerConstants.IntegerNotSet)
                {
                    mediaTemplateRow.MaximumCabinetSizeForLargeFileSplitting = maximumCabinetSizeForLargeFileSplitting;
                }
                else
                {
                    mediaTemplateRow.MaximumCabinetSizeForLargeFileSplitting = 0; // Default value of 0 corresponds to max size of 2048 MB (i.e. 2 GB)
                }

                switch (compressionLevelType)
                {
                    case Wix.CompressionLevelType.high:
                        mediaTemplateRow.CompressionLevel = CompressionLevel.High;
                        break;
                    case Wix.CompressionLevelType.low:
                        mediaTemplateRow.CompressionLevel = CompressionLevel.Low;
                        break;
                    case Wix.CompressionLevelType.medium:
                        mediaTemplateRow.CompressionLevel = CompressionLevel.Medium;
                        break;
                    case Wix.CompressionLevelType.none:
                        mediaTemplateRow.CompressionLevel = CompressionLevel.None;
                        break;
                    case Wix.CompressionLevelType.mszip:
                        mediaTemplateRow.CompressionLevel = CompressionLevel.Mszip;
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a merge element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="directoryId">Identifier for parent directory.</param>
        /// <param name="diskId">Disk id inherited from parent directory.</param>
        private void ParseMergeElement(XElement node, string directoryId, int diskId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string configData = String.Empty;
            YesNoType fileCompression = YesNoType.NotSet;
            string language = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                            break;
                        case "FileCompression":
                            fileCompression = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            language = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == language)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            if (CompilerConstants.IntegerNotSet == diskId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "DiskId", "Directory"));
                diskId = CompilerConstants.IllegalInteger;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ConfigurationData":
                            if (0 == configData.Length)
                            {
                                configData = this.ParseConfigurationDataElement(child);
                            }
                            else
                            {
                                configData = String.Concat(configData, ",", this.ParseConfigurationDataElement(child));
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixMerge", id);
                row[1] = language;
                row[2] = directoryId;
                row[3] = sourceFile;
                row[4] = diskId;
                if (YesNoType.Yes == fileCompression)
                {
                    row[5] = 1;
                }
                else if (YesNoType.No == fileCompression)
                {
                    row[5] = 0;
                }
                else // YesNoType.NotSet == fileCompression
                {
                    // and we leave the column null
                }
                row[6] = configData;
                row[7] = Guid.Empty.ToString("B");
            }
        }

        /// <summary>
        /// Parses a configuration data element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>String in format "name=value" with '%', ',' and '=' hex encoded.</returns>
        private string ParseConfigurationDataElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else // need to hex encode these characters
            {
                name = name.Replace("%", "%25");
                name = name.Replace("=", "%3D");
                name = name.Replace(",", "%2C");
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }
            else // need to hex encode these characters
            {
                value = value.Replace("%", "%25");
                value = value.Replace("=", "%3D");
                value = value.Replace(",", "%2C");
            }

            this.core.ParseForExtensionElements(node);

            return String.Concat(name, "=", value);
        }

        /// <summary>
        /// Parses a merge reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Parents complex reference type.</param>
        /// <param name="parentId">Identifier for parent feature or feature group.</param>
        private void ParseMergeRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType primary = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixMerge", id);
                            break;
                        case "Primary":
                            primary = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Module, id, (YesNoType.Yes == primary));
        }

        /// <summary>
        /// Parses a mime element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="extension">Identifier for parent extension.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="parentAdvertised">Flag if the parent element is advertised.</param>
        /// <returns>Content type if this is the default for the MIME type.</returns>
        private string ParseMIMEElement(XElement node, string extension, string componentId, YesNoType parentAdvertised)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string classId = null;
            string contentType = null;
            YesNoType advertise = parentAdvertised;
            YesNoType returnContentType = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Advertise":
                            advertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Class":
                            classId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ContentType":
                            contentType = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Default":
                            returnContentType = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == contentType)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ContentType"));
            }

            // if the advertise state has not been set, default to non-advertised
            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            this.core.ParseForExtensionElements(node);

            if (YesNoType.Yes == advertise)
            {
                if (YesNoType.Yes != parentAdvertised)
                {
                    this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, advertise.ToString(), parentAdvertised.ToString()));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MIME");
                    row[0] = contentType;
                    row[1] = extension;
                    row[2] = classId;
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (YesNoType.Yes == returnContentType && YesNoType.Yes == parentAdvertised)
                {
                    this.core.OnMessage(WixErrors.CannotDefaultMismatchedAdvertiseStates(sourceLineNumbers));
                }

                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "Extension", String.Concat(".", extension), componentId);
                if (null != classId)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "CLSID", classId, componentId);
                }
            }

            return YesNoType.Yes == returnContentType ? contentType : null;
        }

        /// <summary>
        /// Parses a module element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseModuleElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int codepage = 0;
            string moduleId = null;
            string version = null;

            this.activeName = null;
            this.activeLanguage = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            this.activeName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-MODULE-NAME-HERE" == this.activeName)
                            {
                                this.core.OnMessage(WixWarnings.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, this.activeName));
                            }
                            else
                            {
                                this.activeName = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "Codepage":
                            codepage = this.core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            moduleId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            this.core.OnMessage(WixWarnings.DeprecatedModuleGuidAttribute(sourceLineNumbers));
                            break;
                        case "Language":
                            this.activeLanguage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Version":
                            version = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == this.activeName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == this.activeLanguage)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == version)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidModuleOrBundleVersion(version))
            {
                this.core.OnMessage(WixWarnings.InvalidModuleOrBundleVersion(sourceLineNumbers, "Module", version));
            }

            try
            {
                this.compilingModule = true; // notice that we are actually building a Merge Module here
                this.core.CreateActiveSection(this.activeName, SectionType.Module, codepage);

                foreach (XElement child in node.Elements())
                {
                    if (CompilerCore.WixNamespace == child.Name.Namespace)
                    {
                        switch (child.Name.LocalName)
                        {
                            case "AdminExecuteSequence":
                            case "AdminUISequence":
                            case "AdvertiseExecuteSequence":
                            case "InstallExecuteSequence":
                            case "InstallUISequence":
                                this.ParseSequenceElement(child, child.Name.LocalName);
                                break;
                            case "AppId":
                                this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
                                break;
                            case "Binary":
                                this.ParseBinaryElement(child);
                                break;
                            case "Component":
                                this.ParseComponentElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage, CompilerConstants.IntegerNotSet, null, null);
                                break;
                            case "ComponentGroupRef":
                                this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage);
                                break;
                            case "ComponentRef":
                                this.ParseComponentRefElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage);
                                break;
                            case "Configuration":
                                this.ParseConfigurationElement(child);
                                break;
                            case "CustomAction":
                                this.ParseCustomActionElement(child);
                                break;
                            case "CustomActionRef":
                                this.ParseSimpleRefElement(child, "CustomAction");
                                break;
                            case "CustomTable":
                                this.ParseCustomTableElement(child);
                                break;
                            case "Dependency":
                                this.ParseDependencyElement(child);
                                break;
                            case "Directory":
                                this.ParseDirectoryElement(child, null, CompilerConstants.IntegerNotSet, String.Empty);
                                break;
                            case "DirectoryRef":
                                this.ParseDirectoryRefElement(child);
                                break;
                            case "EmbeddedChainer":
                                this.ParseEmbeddedChainerElement(child);
                                break;
                            case "EmbeddedChainerRef":
                                this.ParseSimpleRefElement(child, "MsiEmbeddedChainer");
                                break;
                            case "EnsureTable":
                                this.ParseEnsureTableElement(child);
                                break;
                            case "Exclusion":
                                this.ParseExclusionElement(child);
                                break;
                            case "Icon":
                                this.ParseIconElement(child);
                                break;
                            case "IgnoreModularization":
                                this.ParseIgnoreModularizationElement(child);
                                break;
                            case "IgnoreTable":
                                this.ParseIgnoreTableElement(child);
                                break;
                            case "Package":
                                this.ParsePackageElement(child, null, moduleId);
                                break;
                            case "Property":
                                this.ParsePropertyElement(child);
                                break;
                            case "PropertyRef":
                                this.ParseSimpleRefElement(child, "Property");
                                break;
                            case "SetDirectory":
                                this.ParseSetDirectoryElement(child);
                                break;
                            case "SetProperty":
                                this.ParseSetPropertyElement(child);
                                break;
                            case "SFPCatalog":
                                string parentName = null;
                                this.ParseSFPCatalogElement(child, ref parentName);
                                break;
                            case "Substitution":
                                this.ParseSubstitutionElement(child);
                                break;
                            case "UI":
                                this.ParseUIElement(child);
                                break;
                            case "UIRef":
                                this.ParseSimpleRefElement(child, "WixUI");
                                break;
                            case "WixVariable":
                                this.ParseWixVariableElement(child);
                                break;
                            default:
                                this.core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.core.ParseExtensionElement(node, child);
                    }
                }


                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "ModuleSignature");
                    row[0] = this.activeName;
                    row[1] = this.activeLanguage;
                    row[2] = version;
                }
            }
            finally
            {
                this.compilingModule = false; // notice that we are no longer building a Merge Module here
            }
        }

        /// <summary>
        /// Parses a patch creation element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchCreationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool clean = true; // Default is to clean
            int codepage = 0;
            string outputPath = null;
            bool productMismatches = false;
            string replaceGuids = String.Empty;
            string sourceList = null;
            string symbolFlags = null;
            string targetProducts = String.Empty;
            bool versionMismatches = false;
            bool wholeFiles = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            this.activeName = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "AllowMajorVersionMismatches":
                            versionMismatches = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowProductCodeMismatches":
                            productMismatches = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "CleanWorkingFolder":
                            clean = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Codepage":
                            codepage = this.core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "OutputPath":
                            outputPath = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourceList":
                            sourceList = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SymbolFlags":
                            symbolFlags = String.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, uint.MaxValue));
                            break;
                        case "WholeFilesOnly":
                            wholeFiles = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == this.activeName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.CreateActiveSection(this.activeName, SectionType.PatchCreation, codepage);

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Family":
                            this.ParseFamilyElement(child);
                            break;
                        case "PatchInformation":
                            this.ParsePatchInformationElement(child);
                            break;
                        case "PatchMetadata":
                            this.ParsePatchMetadataElement(child);
                            break;
                        case "PatchProperty":
                            this.ParsePatchPropertyElement(child, false);
                            break;
                        case "PatchSequence":
                            this.ParsePatchSequenceElement(child);
                            break;
                        case "ReplacePatch":
                            replaceGuids = String.Concat(replaceGuids, this.ParseReplacePatchElement(child));
                            break;
                        case "TargetProductCode":
                            string targetProduct = this.ParseTargetProductCodeElement(child);
                            if (0 < targetProducts.Length)
                            {
                                targetProducts = String.Concat(targetProducts, ";");
                            }
                            targetProducts = String.Concat(targetProducts, targetProduct);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            this.ProcessProperties(sourceLineNumbers, "PatchGUID", this.activeName);
            this.ProcessProperties(sourceLineNumbers, "AllowProductCodeMismatches", productMismatches ? "1" : "0");
            this.ProcessProperties(sourceLineNumbers, "AllowProductVersionMajorMismatches", versionMismatches ? "1" : "0");
            this.ProcessProperties(sourceLineNumbers, "DontRemoveTempFolderWhenFinished", clean ? "0" : "1");
            this.ProcessProperties(sourceLineNumbers, "IncludeWholeFilesOnly", wholeFiles ? "1" : "0");

            if (null != symbolFlags)
            {
                this.ProcessProperties(sourceLineNumbers, "ApiPatchingSymbolFlags", symbolFlags);
            }

            if (0 < replaceGuids.Length)
            {
                this.ProcessProperties(sourceLineNumbers, "ListOfPatchGUIDsToReplace", replaceGuids);
            }

            if (0 < targetProducts.Length)
            {
                this.ProcessProperties(sourceLineNumbers, "ListOfTargetProductCodes", targetProducts);
            }

            if (null != outputPath)
            {
                this.ProcessProperties(sourceLineNumbers, "PatchOutputPath", outputPath);
            }

            if (null != sourceList)
            {
                this.ProcessProperties(sourceLineNumbers, "PatchSourceList", sourceList);
            }
        }

        /// <summary>
        /// Parses a family element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseFamilyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int diskId = CompilerConstants.IntegerNotSet;
            string diskPrompt = null;
            string mediaSrcProp = null;
            string name = null;
            int sequenceStart = CompilerConstants.IntegerNotSet;
            string volumeLabel = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "DiskId":
                            diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MediaSrcProp":
                            mediaSrcProp = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SequenceStart":
                            sequenceStart = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (8 < name.Length) // check the length
                {
                    this.core.OnMessage(WixErrors.FamilyNameTooLong(sourceLineNumbers, node.Name.LocalName, "Name", name, name.Length));
                }
                else // check for illegal characters
                {
                    foreach (char character in name)
                    {
                        if (!Char.IsLetterOrDigit(character) && '_' != character)
                        {
                            this.core.OnMessage(WixErrors.IllegalFamilyName(sourceLineNumbers, node.Name.LocalName, "Name", name));
                        }
                    }
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "UpgradeImage":
                            this.ParseUpgradeImageElement(child, name);
                            break;
                        case "ExternalFile":
                            this.ParseExternalFileElement(child, name);
                            break;
                        case "ProtectFile":
                            this.ParseProtectFileElement(child, name);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ImageFamilies");
                row[0] = name;
                row[1] = mediaSrcProp;
                if (CompilerConstants.IntegerNotSet != diskId)
                {
                    row[2] = diskId;
                }

                if (CompilerConstants.IntegerNotSet != sequenceStart)
                {
                    row[3] = sequenceStart;
                }
                row[4] = diskPrompt;
                row[5] = volumeLabel;
            }
        }

        /// <summary>
        /// Parses an upgrade image element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseUpgradeImageElement(XElement node, string family)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string sourceFile = null;
            string sourcePatch = null;
            List<string> symbols = new List<string>();
            string upgrade = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            upgrade = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (13 < upgrade.Length)
                            {
                                this.core.OnMessage(WixErrors.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", upgrade, 13));
                            }
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourcePatch":
                        case "srcPatch":
                            if (null != sourcePatch)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "srcPatch", "SourcePatch"));
                            }

                            if ("srcPatch" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourcePatch"));
                            }
                            sourcePatch = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == upgrade)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "SymbolPath":
                            symbols.Add(this.ParseSymbolPathElement(child));
                            break;
                        case "TargetImage":
                            this.ParseTargetImageElement(child, upgrade, family);
                            break;
                        case "UpgradeFile":
                            this.ParseUpgradeFileElement(child, upgrade);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedImages");
                row[0] = upgrade;
                row[1] = sourceFile;
                row[2] = sourcePatch;
                row[3] = String.Join(";", symbols);
                row[4] = family;
            }
        }

        /// <summary>
        /// Parses an upgrade file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="upgrade">The upgrade key for this element.</param>
        private void ParseUpgradeFileElement(XElement node, string upgrade)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool allowIgnoreOnError = false;
            string file = null;
            bool ignore = false;
            List<string> symbols = new List<string>();
            bool wholeFile = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AllowIgnoreOnError":
                            allowIgnoreOnError = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Ignore":
                            ignore = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "WholeFile":
                            wholeFile = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "SymbolPath":
                            symbols.Add(this.ParseSymbolPathElement(child));
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                if (ignore)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedFilesToIgnore");
                    row[0] = upgrade;
                    row[1] = file;
                }
                else
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedFiles_OptionalData");
                    row[0] = upgrade;
                    row[1] = file;
                    row[2] = String.Join(";", symbols);
                    row[3] = allowIgnoreOnError ? 1 : 0;
                    row[4] = wholeFile ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Parses a target image element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="upgrade">The upgrade key for this element.</param>
        /// <param name="family">The family key for this element.</param>
        private void ParseTargetImageElement(XElement node, string upgrade, string family)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool ignore = false;
            int order = CompilerConstants.IntegerNotSet;
            string sourceFile = null;
            string symbols = null;
            string target = null;
            string validation = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (target.Length > 13)
                            {
                                this.core.OnMessage(WixErrors.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", target, 13));
                            }
                            break;
                        case "IgnoreMissingFiles":
                            ignore = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Order":
                            order = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Validation":
                            validation = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == target)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "SymbolPath":
                            if (null != symbols)
                            {
                                symbols = String.Concat(symbols, ";", this.ParseSymbolPathElement(child));
                            }
                            else
                            {
                                symbols = this.ParseSymbolPathElement(child);
                            }
                            break;
                        case "TargetFile":
                            this.ParseTargetFileElement(child, target, family);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "TargetImages");
                row[0] = target;
                row[1] = sourceFile;
                row[2] = symbols;
                row[3] = upgrade;
                row[4] = order;
                row[5] = validation;
                row[6] = ignore ? 1 : 0;
            }
        }

        /// <summary>
        /// Parses an upgrade file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="target">The upgrade key for this element.</param>
        /// <param name="family">The family key for this element.</param>
        private void ParseTargetFileElement(XElement node, string target, string family)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            string protectLengths = null;
            string protectOffsets = null;
            string symbols = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "IgnoreRange":
                            this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
                            break;
                        case "ProtectRange":
                            this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                            break;
                        case "SymbolPath":
                            symbols = this.ParseSymbolPathElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "TargetFiles_OptionalData");
                row[0] = target;
                row[1] = file;
                row[2] = symbols;
                row[3] = ignoreOffsets;
                row[4] = ignoreLengths;

                if (null != protectOffsets)
                {
                    row[5] = protectOffsets;

                    Row row2 = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
                    row2[0] = family;
                    row2[1] = file;
                    row2[2] = protectOffsets;
                    row2[3] = protectLengths;
                }
            }
        }

        /// <summary>
        /// Parses an external file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseExternalFileElement(XElement node, string family)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            int order = CompilerConstants.IntegerNotSet;
            string protectLengths = null;
            string protectOffsets = null;
            string source = null;
            string symbols = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Order":
                            order = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "Source":
                        case "src":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "Source"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Source"));
                            }
                            source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (null == source)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Source"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "IgnoreRange":
                            this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
                            break;
                        case "ProtectRange":
                            this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                            break;
                        case "SymbolPath":
                            symbols = this.ParseSymbolPathElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ExternalFiles");
                row[0] = family;
                row[1] = file;
                row[2] = source;
                row[3] = symbols;
                row[4] = ignoreOffsets;
                row[5] = ignoreLengths;
                if (null != protectOffsets)
                {
                    row[6] = protectOffsets;
                }

                if (CompilerConstants.IntegerNotSet != order)
                {
                    row[7] = order;
                }

                if (null != protectOffsets)
                {
                    Row row2 = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
                    row2[0] = family;
                    row2[1] = file;
                    row2[2] = protectOffsets;
                    row2[3] = protectLengths;
                }
            }
        }

        /// <summary>
        /// Parses a protect file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseProtectFileElement(XElement node, string family)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string protectLengths = null;
            string protectOffsets = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ProtectRange":
                            this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null == protectOffsets || null == protectLengths)
            {
                this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "ProtectRange"));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
                row[0] = family;
                row[1] = file;
                row[2] = protectOffsets;
                row[3] = protectLengths;
            }
        }

        /// <summary>
        /// Parses a range element (ProtectRange, IgnoreRange, etc).
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="offsets">Reference to the offsets string.</param>
        /// <param name="lengths">Reference to the lengths string.</param>
        private void ParseRangeElement(XElement node, ref string offsets, ref string lengths)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string length = null;
            string offset = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Length":
                            length = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Offset":
                            offset = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == length)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Length"));
            }

            if (null == offset)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Offset"));
            }

            this.core.ParseForExtensionElements(node);

            if (null != lengths)
            {
                lengths = String.Concat(lengths, ",", length);
            }
            else
            {
                lengths = length;
            }

            if (null != offsets)
            {
                offsets = String.Concat(offsets, ",", offset);
            }
            else
            {
                offsets = offset;
            }
        }

        /// <summary>
        /// Parses a patch property element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="patch">True if parsing an patch element.</param>
        private void ParsePatchPropertyElement(XElement node, bool patch)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string company = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Company":
                            company = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (patch)
            {
                // /Patch/PatchProperty goes directly into MsiPatchMetadata table
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                row[0] = company;
                row[1] = name;
                row[2] = value;
            }
            else
            {
                if (null != company)
                {
                    this.core.OnMessage(WixErrors.UnexpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
                }
                this.ProcessProperties(sourceLineNumbers, name, value);
            }
        }

        /// <summary>
        /// Parses a patch sequence element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchSequenceElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string family = null;
            string target = null;
            string sequence = null;
            int attributes = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "PatchFamily":
                            family = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "TargetImage"));
                            }
                            target = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Target":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "TargetImage", "ProductCode"));
                            }
                            this.core.OnMessage(WixWarnings.DeprecatedPatchSequenceTargetAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetImage":
                            if (null != target)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "ProductCode"));
                            }
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "TargetImages", target);
                            break;
                        case "Sequence":
                            sequence = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Supersede":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == family)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PatchFamily"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "PatchSequence");
                row[0] = family;
                row[1] = target;
                if (!String.IsNullOrEmpty(sequence))
                {
                    row[2] = sequence;
                }
                row[3] = attributes;
            }
        }

        /// <summary>
        /// Parses a TargetProductCode element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <returns>The id from the node.</returns>
        private string ParseTargetProductCodeElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (id.Length > 0 && "*" != id)
                            {
                                id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            return id;
        }

        /// <summary>
        /// Parses a TargetProductCodes element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseTargetProductCodesElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool replace = false;
            List<string> targetProductCodes = new List<string>();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Replace":
                            replace = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "TargetProductCode":
                            string id = this.ParseTargetProductCodeElement(child);
                            if (0 == String.CompareOrdinal("*", id))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValueWhenNested(sourceLineNumbers, child.Name.LocalName, "Id", id, node.Name.LocalName));
                            }
                            else
                            {
                                targetProductCodes.Add(id);
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                // By default, target ProductCodes should be added.
                if (!replace)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "WixPatchTarget");
                    row[0] = "*";
                }

                foreach (string targetProductCode in targetProductCodes)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "WixPatchTarget");
                    row[0] = targetProductCode;
                }
            }
        }

        /// <summary>
        /// Parses a ReplacePatch element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <returns>The id from the node.</returns>
        private string ParseReplacePatchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            return id;
        }

        /// <summary>
        /// Parses a symbol path element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <returns>The path from the node.</returns>
        private string ParseSymbolPathElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string path = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Path":
                            path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == path)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            this.core.ParseForExtensionElements(node);

            return path;
        }

        /// <summary>
        /// Parses an patch element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string patchId = null;
            int codepage = 0;
            ////bool versionMismatches = false;
            ////bool productMismatches = false;
            bool allowRemoval = false;
            string classification = null;
            string clientPatchId = null;
            string description = null;
            string displayName = null;
            string comments = null;
            string manufacturer = null;
            YesNoType minorUpdateTargetRTM = YesNoType.NotSet;
            string moreInfoUrl = null;
            int optimizeCA = CompilerConstants.IntegerNotSet;
            YesNoType optimizedInstallMode = YesNoType.NotSet;
            string targetProductName = null;
            // string replaceGuids = String.Empty;
            int apiPatchingSymbolFlags = 0;
            bool optimizePatchSizeForLargeFiles = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            patchId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Codepage":
                            codepage = this.core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowMajorVersionMismatches":
                            ////versionMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "AllowProductCodeMismatches":
                            ////productMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "AllowRemoval":
                            allowRemoval = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "Classification":
                            classification = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ClientPatchId":
                            clientPatchId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Comments":
                            comments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            manufacturer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinorUpdateTargetRTM":
                            minorUpdateTargetRTM = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "MoreInfoURL":
                            moreInfoUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OptimizedInstallMode":
                            optimizedInstallMode = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetProductName":
                            targetProductName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ApiPatchingSymbolNoImagehlpFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_NO_IMAGEHLP : 0;
                            break;
                        case "ApiPatchingSymbolNoFailuresFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_NO_FAILURES : 0;
                            break;
                        case "ApiPatchingSymbolUndecoratedTooFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_UNDECORATED_TOO : 0;
                            break;
                        case "OptimizePatchSizeForLargeFiles":
                            optimizePatchSizeForLargeFiles = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (patchId == null || patchId == "*")
            {
                // auto-generate at compile time, since this value gets dispersed to several locations
                patchId = Common.GenerateGuid();
            }
            this.activeName = patchId;

            if (null == this.activeName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            if (null == classification)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }
            if (null == clientPatchId)
            {
                clientPatchId = String.Concat("_", new Guid(patchId).ToString("N", CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture));
            }
            if (null == description)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }
            if (null == displayName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }
            if (null == manufacturer)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            this.core.CreateActiveSection(this.activeName, SectionType.Patch, codepage);

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PatchInformation":
                            this.ParsePatchInformationElement(child);
                            break;
                        case "Media":
                            this.ParseMediaElement(child, patchId);
                            break;
                        case "OptimizeCustomActions":
                            optimizeCA = this.ParseOptimizeCustomActionsElement(child);
                            break;
                        case "PatchFamily":
                            this.ParsePatchFamilyElement(child, ComplexReferenceParentType.Patch, patchId);
                            break;
                        case "PatchFamilyRef":
                            this.ParsePatchFamilyRefElement(child, ComplexReferenceParentType.Patch, patchId);
                            break;
                        case "PatchFamilyGroup":
                            this.ParsePatchFamilyGroupElement(child, ComplexReferenceParentType.Patch, patchId);
                            break;
                        case "PatchFamilyGroupRef":
                            this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.Patch, patchId);
                            break;
                        case "PatchProperty":
                            this.ParsePatchPropertyElement(child, true);
                            break;
                        case "TargetProductCodes":
                            this.ParseTargetProductCodesElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                Row patchIdRow = this.core.CreateRow(sourceLineNumbers, "WixPatchId");
                patchIdRow[0] = patchId;
                patchIdRow[1] = clientPatchId;
                patchIdRow[2] = optimizePatchSizeForLargeFiles ? 1 : 0;
                patchIdRow[3] = apiPatchingSymbolFlags;

                if (allowRemoval)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "AllowRemoval";
                    row[2] = allowRemoval ? "1" : "0";
                }

                if (null != classification)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "Classification";
                    row[2] = classification;
                }

                // always generate the CreationTimeUTC
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "CreationTimeUTC";
                    row[2] = DateTime.UtcNow.ToString("MM-dd-yy HH:mm", CultureInfo.InvariantCulture);
                }

                if (null != description)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "Description";
                    row[2] = description;
                }

                if (null != displayName)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "DisplayName";
                    row[2] = displayName;
                }

                if (null != manufacturer)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "ManufacturerName";
                    row[2] = manufacturer;
                }

                if (YesNoType.NotSet != minorUpdateTargetRTM)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "MinorUpdateTargetRTM";
                    row[2] = YesNoType.Yes == minorUpdateTargetRTM ? "1" : "0";
                }

                if (null != moreInfoUrl)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "MoreInfoURL";
                    row[2] = moreInfoUrl;
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "OptimizeCA";
                    row[2] = optimizeCA.ToString(CultureInfo.InvariantCulture);
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "OptimizedInstallMode";
                    row[2] = YesNoType.Yes == optimizedInstallMode ? "1" : "0";
                }

                if (null != targetProductName)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchMetadata");
                    row[0] = null;
                    row[1] = "TargetProductName";
                    row[2] = targetProductName;
                }

                if (null != comments)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "WixPatchMetadata");
                    row[0] = "Comments";
                    row[1] = comments;
                }
            }
            // TODO: do something with versionMismatches and productMismatches
        }

        /// <summary>
        /// Parses a PatchFamily element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchFamilyElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string productCode = null;
            string version = null;
            int attributes = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version":
                            version = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Supersede":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            if (String.IsNullOrEmpty(version))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidProductVersion(version))
            {
                this.core.OnMessage(WixErrors.InvalidProductVersion(sourceLineNumbers, version));
            }

            // find unexpected child elements
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "All":
                            this.ParseAllElement(child);
                            break;
                        case "BinaryRef":
                            this.ParsePatchChildRefElement(child, "Binary");
                            break;
                        case "ComponentRef":
                            this.ParsePatchChildRefElement(child, "Component");
                            break;
                        case "CustomActionRef":
                            this.ParsePatchChildRefElement(child, "CustomAction");
                            break;
                        case "DirectoryRef":
                            this.ParsePatchChildRefElement(child, "Directory");
                            break;
                        case "DigitalCertificateRef":
                            this.ParsePatchChildRefElement(child, "MsiDigitalCertificate");
                            break;
                        case "FeatureRef":
                            this.ParsePatchChildRefElement(child, "Feature");
                            break;
                        case "IconRef":
                            this.ParsePatchChildRefElement(child, "Icon");
                            break;
                        case "PropertyRef":
                            this.ParsePatchChildRefElement(child, "Property");
                            break;
                        case "UIRef":
                            this.ParsePatchChildRefElement(child, "WixUI");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchSequence", id);
                row[1] = productCode;
                row[2] = version;
                row[3] = attributes;

                if (ComplexReferenceParentType.Unknown != parentType)
                {
                    this.core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, id.Id, ComplexReferenceParentType.Patch == parentType);
                }
            }
        }

        /// <summary>
        /// Parses the All element under a PatchFamily.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseAllElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // find unexpected attributes
            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    this.core.UnexpectedAttribute(node, attrib);
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.core.ParseForExtensionElements(node);

            // Always warn when using the All element.
            this.core.OnMessage(WixWarnings.AllChangesIncludedInPatch(sourceLineNumbers));

            if (!this.core.EncounteredError)
            {
                this.core.CreatePatchFamilyChildReference(sourceLineNumbers, "*", "*");
            }
        }

        /// <summary>
        /// Parses all reference elements under a PatchFamily.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="tableName">Table that reference was made to.</param>
        private void ParsePatchChildRefElement(XElement node, string tableName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                this.core.CreatePatchFamilyChildReference(sourceLineNumbers, tableName, id);
            }
        }

        /// <summary>
        /// Parses a PatchBaseline element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="diskId">Media index from parent element.</param>
        private void ParsePatchBaselineElement(XElement node, int diskId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            bool parsedValidate = false;
            TransformFlags validationFlags = TransformFlags.PatchTransformDefault;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (27 < id.Id.Length)
            {
                this.core.OnMessage(WixErrors.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, 27));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Validate":
                            if (parsedValidate)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            else
                            {
                                this.ParseValidateElement(child, ref validationFlags);
                                parsedValidate = true;
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixPatchBaseline", id);
                row[1] = diskId;
                row[2] = (int)validationFlags;
            }
        }

        /// <summary>
        /// Parses a Validate element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="validationFlags">TransformValidation flags to use when creating the authoring patch transform.</param>
        private void ParseValidateElement(XElement node, ref TransformFlags validationFlags)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ProductId":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateProduct;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateProduct;
                            }
                            break;
                        case "ProductLanguage":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateLanguage;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateLanguage;
                            }
                            break;
                        case "ProductVersion":
                            string check = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            validationFlags &= ~TransformFlags.ProductVersionMask;
                            Wix.Validate.ProductVersionType productVersionType = Wix.Validate.ParseProductVersionType(check);
                            switch (productVersionType)
                            {
                                case Wix.Validate.ProductVersionType.Major:
                                    validationFlags |= TransformFlags.ValidateMajorVersion;
                                    break;
                                case Wix.Validate.ProductVersionType.Minor:
                                    validationFlags |= TransformFlags.ValidateMinorVersion;
                                    break;
                                case Wix.Validate.ProductVersionType.Update:
                                    validationFlags |= TransformFlags.ValidateUpdateVersion;
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Version", check, "Major", "Minor", "Update"));
                                    break;
                            }
                            break;
                        case "ProductVersionOperator":
                            string op = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            validationFlags &= ~TransformFlags.ProductVersionOperatorMask;
                            Wix.Validate.ProductVersionOperatorType opType = Wix.Validate.ParseProductVersionOperatorType(op);
                            switch (opType)
                            {
                                case Wix.Validate.ProductVersionOperatorType.Lesser:
                                    validationFlags |= TransformFlags.ValidateNewLessBaseVersion;
                                    break;
                                case Wix.Validate.ProductVersionOperatorType.LesserOrEqual:
                                    validationFlags |= TransformFlags.ValidateNewLessEqualBaseVersion;
                                    break;
                                case Wix.Validate.ProductVersionOperatorType.Equal:
                                    validationFlags |= TransformFlags.ValidateNewEqualBaseVersion;
                                    break;
                                case Wix.Validate.ProductVersionOperatorType.GreaterOrEqual:
                                    validationFlags |= TransformFlags.ValidateNewGreaterEqualBaseVersion;
                                    break;
                                case Wix.Validate.ProductVersionOperatorType.Greater:
                                    validationFlags |= TransformFlags.ValidateNewGreaterBaseVersion;
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Operator", op, "Lesser", "LesserOrEqual", "Equal", "GreaterOrEqual", "Greater"));
                                    break;
                            }
                            break;
                        case "UpgradeCode":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateUpgradeCode;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateUpgradeCode;
                            }
                            break;
                        case "IgnoreAddExistingRow":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorAddExistingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorAddExistingRow;
                            }
                            break;
                        case "IgnoreAddExistingTable":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorAddExistingTable;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorAddExistingTable;
                            }
                            break;
                        case "IgnoreDeleteMissingRow":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorDeleteMissingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorDeleteMissingRow;
                            }
                            break;
                        case "IgnoreDeleteMissingTable":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorDeleteMissingTable;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorDeleteMissingTable;
                            }
                            break;
                        case "IgnoreUpdateMissingRow":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorUpdateMissingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorUpdateMissingRow;
                            }
                            break;
                        case "IgnoreChangingCodePage":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorChangeCodePage;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorChangeCodePage;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

        }

        /// <summary>
        /// Adds a row to the properties table.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        private void ProcessProperties(SourceLineNumber sourceLineNumbers, string name, string value)
        {
            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Properties");
                row[0] = name;
                row[1] = value;
            }
        }

        /// <summary>
        /// Parses a dependency element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseDependencyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string requiredId = null;
            int requiredLanguage = CompilerConstants.IntegerNotSet;
            string requiredVersion = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "RequiredId":
                            requiredId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "RequiredLanguage":
                            requiredLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RequiredVersion":
                            requiredVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == requiredId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredId"));
                requiredId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet == requiredLanguage)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredLanguage"));
                requiredLanguage = CompilerConstants.IllegalInteger;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ModuleDependency");
                row[0] = this.activeName;
                row[1] = this.activeLanguage;
                row[2] = requiredId;
                row[3] = requiredLanguage.ToString(CultureInfo.InvariantCulture);
                row[4] = requiredVersion;
            }
        }

        /// <summary>
        /// Parses an exclusion element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseExclusionElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string excludedId = null;
            int excludeExceptLanguage = CompilerConstants.IntegerNotSet;
            int excludeLanguage = CompilerConstants.IntegerNotSet;
            string excludedLanguageField = "0";
            string excludedMaxVersion = null;
            string excludedMinVersion = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ExcludedId":
                            excludedId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ExcludeExceptLanguage":
                            excludeExceptLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ExcludeLanguage":
                            excludeLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ExcludedMaxVersion":
                            excludedMaxVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ExcludedMinVersion":
                            excludedMinVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == excludedId)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ExcludedId"));
                excludedId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet != excludeExceptLanguage && CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                this.core.OnMessage(WixErrors.IllegalModuleExclusionLanguageAttributes(sourceLineNumbers));
            }
            else if (CompilerConstants.IntegerNotSet != excludeExceptLanguage)
            {
                excludedLanguageField = Convert.ToString(-excludeExceptLanguage, CultureInfo.InvariantCulture);
            }
            else if (CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                excludedLanguageField = Convert.ToString(excludeLanguage, CultureInfo.InvariantCulture);
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ModuleExclusion");
                row[0] = this.activeName;
                row[1] = this.activeLanguage;
                row[2] = excludedId;
                row[3] = excludedLanguageField;
                row[4] = excludedMinVersion;
                row[5] = excludedMaxVersion;
            }
        }

        /// <summary>
        /// Parses a configuration element for a configurable merge module.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseConfigurationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int attributes = 0;
            string contextData = null;
            string defaultValue = null;
            string description = null;
            string displayName = null;
            int format = CompilerConstants.IntegerNotSet;
            string helpKeyword = null;
            string helpLocation = null;
            string name = null;
            string type = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ContextData":
                            contextData = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultValue":
                            defaultValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Format":
                            string formatStr = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < formatStr.Length)
                            {
                                Wix.Configuration.FormatType formatType = Wix.Configuration.ParseFormatType(formatStr);
                                switch (formatType)
                                {
                                    case Wix.Configuration.FormatType.Text:
                                        format = 0;
                                        break;
                                    case Wix.Configuration.FormatType.Key:
                                        format = 1;
                                        break;
                                    case Wix.Configuration.FormatType.Integer:
                                        format = 2;
                                        break;
                                    case Wix.Configuration.FormatType.Bitfield:
                                        format = 3;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Format", formatStr, "Text", "Key", "Integer", "Bitfield"));
                                        break;
                                }
                            }
                            break;
                        case "HelpKeyword":
                            helpKeyword = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HelpLocation":
                            helpLocation = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyNoOrphan":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbMsmConfigurableOptionKeyNoOrphan;
                            }
                            break;
                        case "NonNullable":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbMsmConfigurableOptionNonNullable;
                            }
                            break;
                        case "Type":
                            type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                name = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet == format)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Format"));
                format = CompilerConstants.IllegalInteger;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ModuleConfiguration");
                row[0] = name;
                row[1] = format;
                row[2] = type;
                row[3] = contextData;
                row[4] = defaultValue;
                row[5] = attributes;
                row[6] = displayName;
                row[7] = description;
                row[8] = helpLocation;
                row[9] = helpKeyword;
            }
        }

        /// <summary>
        /// Parses a substitution element for a configurable merge module.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseSubstitutionElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string column = null;
            string rowKeys = null;
            string table = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Column":
                            column = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Row":
                            rowKeys = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Table":
                            table = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == column)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Column"));
                column = String.Empty;
            }

            if (null == table)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Table"));
                table = String.Empty;
            }

            if (null == rowKeys)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Row"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ModuleSubstitution");
                row[0] = table;
                row[1] = rowKeys;
                row[2] = column;
                row[3] = value;
            }
        }

        /// <summary>
        /// Parses an IgnoreTable element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseIgnoreTableElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ModuleIgnoreTable");
                row[0] = id;
            }
        }

        /// <summary>
        /// Parses an odbc driver or translator element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Default identifer for driver/translator file.</param>
        /// <param name="table">Table we're processing for.</param>
        private void ParseODBCDriverOrTranslator(XElement node, string componentId, string fileId, TableDefinition table)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string driver = fileId;
            string name = null;
            string setup = fileId;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            driver = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", driver);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SetupFile":
                            setup = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", setup);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("odb", name, fileId, setup);
            }

            // drivers have a few possible children
            if ("ODBCDriver" == table.Name)
            {
                // process any data sources for the driver
                foreach (XElement child in node.Elements())
                {
                    if (CompilerCore.WixNamespace == child.Name.Namespace)
                    {
                        switch (child.Name.LocalName)
                        {
                            case "ODBCDataSource":
                                string ignoredKeyPath = null;
                                this.ParseODBCDataSource(child, componentId, name, out ignoredKeyPath);
                                break;
                            case "Property":
                                this.ParseODBCProperty(child, id.Id, "ODBCAttribute");
                                break;
                            default:
                                this.core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.core.ParseExtensionElement(node, child);
                    }
                }
            }
            else
            {
                this.core.ParseForExtensionElements(node);
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, table.Name, id);
                row[1] = componentId;
                row[2] = name;
                row[3] = driver;
                row[4] = setup;
            }
        }

        /// <summary>
        /// Parses a Property element underneath an ODBC driver or translator.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Identifier of parent driver or translator.</param>
        /// <param name="tableName">Name of the table to create property in.</param>
        private void ParseODBCProperty(XElement node, string parentId, string tableName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string propertyValue = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            propertyValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, tableName);
                row[0] = parentId;
                row[1] = id;
                row[2] = propertyValue;
            }
        }

        /// <summary>
        /// Parse an odbc data source element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="driverName">Default name of driver.</param>
        /// <param name="possibleKeyPath">Identifier of this element in case it is a keypath.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        private YesNoType ParseODBCDataSource(XElement node, string componentId, string driverName, out string possibleKeyPath)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            YesNoType keyPath = YesNoType.NotSet;
            string name = null;
            int registration = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DriverName":
                            driverName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyPath":
                            keyPath = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Registration":
                            string registrationValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < registrationValue.Length)
                            {
                                Wix.ODBCDataSource.RegistrationType registrationType = Wix.ODBCDataSource.ParseRegistrationType(registrationValue);
                                switch (registrationType)
                                {
                                    case Wix.ODBCDataSource.RegistrationType.machine:
                                        registration = 0;
                                        break;
                                    case Wix.ODBCDataSource.RegistrationType.user:
                                        registration = 1;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Registration", registrationValue, "machine", "user"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == registration)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Registration"));
                registration = CompilerConstants.IllegalInteger;
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("odc", name, driverName, registration.ToString());
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Property":
                            this.ParseODBCProperty(child, id.Id, "ODBCSourceAttribute");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ODBCDataSource", id);
                row[1] = componentId;
                row[2] = name;
                row[3] = driverName;
                row[4] = registration;
            }

            possibleKeyPath = id.Id;
            return keyPath;
        }

        /// <summary>
        /// Parses a package element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="productAuthor">Default package author.</param>
        /// <param name="moduleId">The module guid - this is necessary until Module/@Guid is removed.</param>
        private void ParsePackageElement(XElement node, string productAuthor, string moduleId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string codepage = "1252";
            string comments = String.Format(CultureInfo.InvariantCulture, "This installer database contains the logic and data required to install {0}.", this.activeName);
            string keywords = "Installer";
            int msiVersion = 100; // lowest released version, really should be specified
            string packageAuthor = productAuthor;
            string packageCode = null;
            string packageLanguages = this.activeLanguage;
            string packageName = this.activeName;
            string platform = null;
            string platformValue = null;
            YesNoDefaultType security = YesNoDefaultType.Default;
            int sourceBits = (this.compilingModule ? 2 : 0);
            Row row;
            bool installPrivilegeSeen = false;
            bool installScopeSeen = false;

            switch (this.CurrentPlatform)
            {
                case Platform.X86:
                    platform = "Intel";
                    break;
                case Platform.X64:
                    platform = "x64";
                    msiVersion = 200;
                    break;
                case Platform.IA64:
                    platform = "Intel64";
                    msiVersion = 200;
                    break;
                case Platform.ARM:
                    platform = "Arm";
                    msiVersion = 500;
                    break;
                default:
                    throw new ArgumentException(WixStrings.EXP_UnknownPlatformEnum, this.CurrentPlatform.ToString());
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            packageCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, this.compilingProduct);
                            break;
                        case "AdminImage":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 4;
                            }
                            break;
                        case "Comments":
                            comments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            // merge modules must always be compressed, so this attribute is invalid
                            if (this.compilingModule)
                            {
                                this.core.OnMessage(WixWarnings.DeprecatedPackageCompressedAttribute(sourceLineNumbers));
                                // this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Compressed", "Module"));
                            }
                            else if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 2;
                            }
                            break;
                        case "Description":
                            packageName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallPrivileges":
                            string installPrivileges = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < installPrivileges.Length)
                            {
                                installPrivilegeSeen = true;
                                Wix.Package.InstallPrivilegesType installPrivilegesType = Wix.Package.ParseInstallPrivilegesType(installPrivileges);
                                switch (installPrivilegesType)
                                {
                                    case Wix.Package.InstallPrivilegesType.elevated:
                                        // this is the default setting
                                        break;
                                    case Wix.Package.InstallPrivilegesType.limited:
                                        sourceBits = sourceBits | 8;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installPrivileges, "elevated", "limited"));
                                        break;
                                }
                            }
                            break;
                        case "InstallScope":
                            string installScope = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < installScope.Length)
                            {
                                installScopeSeen = true;
                                Wix.Package.InstallScopeType installScopeType = Wix.Package.ParseInstallScopeType(installScope);
                                switch (installScopeType)
                                {
                                    case Wix.Package.InstallScopeType.perMachine:
                                        row = this.core.CreateRow(sourceLineNumbers, "Property");
                                        row[0] = "ALLUSERS";
                                        row[1] = "1";
                                        break;
                                    case Wix.Package.InstallScopeType.perUser:
                                        sourceBits = sourceBits | 8;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installScope, "perMachine", "perUser"));
                                        break;
                                }
                            }
                            break;
                        case "InstallerVersion":
                            msiVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Keywords":
                            keywords = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            packageLanguages = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            packageAuthor = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-COMPANY-NAME-HERE" == packageAuthor)
                            {
                                this.core.OnMessage(WixWarnings.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, packageAuthor));
                            }
                            break;
                        case "Platform":
                            if (null != platformValue)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platforms"));
                            }

                            platformValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            Wix.Package.PlatformType platformType = Wix.Package.ParsePlatformType(platformValue);
                            switch (platformType)
                            {
                                case Wix.Package.PlatformType.intel:
                                    this.core.OnMessage(WixWarnings.DeprecatedAttributeValue(sourceLineNumbers, platformValue, node.Name.LocalName, attrib.Name.LocalName, "x86"));
                                    goto case Wix.Package.PlatformType.x86;
                                case Wix.Package.PlatformType.x86:
                                    platform = "Intel";
                                    break;
                                case Wix.Package.PlatformType.x64:
                                    platform = "x64";
                                    break;
                                case Wix.Package.PlatformType.intel64:
                                    this.core.OnMessage(WixWarnings.DeprecatedAttributeValue(sourceLineNumbers, platformValue, node.Name.LocalName, attrib.Name.LocalName, "ia64"));
                                    goto case Wix.Package.PlatformType.ia64;
                                case Wix.Package.PlatformType.ia64:
                                    platform = "Intel64";
                                    break;
                                case Wix.Package.PlatformType.arm:
                                    platform = "Arm";
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.InvalidPlatformValue(sourceLineNumbers, platformValue));
                                    break;
                            }
                            break;
                        case "Platforms":
                            if (null != platformValue)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platform"));
                            }

                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platform"));
                            platformValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            platform = platformValue;
                            break;
                        case "ReadOnly":
                            security = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortNames":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 1;
                                this.useShortFileNames = true;
                            }
                            break;
                        case "SummaryCodepage":
                            codepage = this.core.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (installPrivilegeSeen && installScopeSeen)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "InstallPrivileges", "InstallScope"));
            }

            if ((0 != String.Compare(platform, "Intel", StringComparison.OrdinalIgnoreCase)) && 200 > msiVersion)
            {
                msiVersion = 200;
                this.core.OnMessage(WixWarnings.RequiresMsi200for64bitPackage(sourceLineNumbers));
            }

            if ((0 == String.Compare(platform, "Arm", StringComparison.OrdinalIgnoreCase)) && 500 > msiVersion)
            {
                msiVersion = 500;
                this.core.OnMessage(WixWarnings.RequiresMsi500forArmPackage(sourceLineNumbers));
            }

            if (null == packageAuthor)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            if (this.compilingModule)
            {
                if (null == packageCode)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                }

                // merge modules use the modularization guid as the package code
                if (null != moduleId)
                {
                    packageCode = moduleId;
                }

                // merge modules are always compressed
                sourceBits = 2;
            }
            else // product
            {
                if (null == packageCode)
                {
                    packageCode = "*";
                }

                if ("*" != packageCode)
                {
                    this.core.OnMessage(WixWarnings.PackageCodeSet(sourceLineNumbers));
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 1;
                row[1] = codepage;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 2;
                row[1] = "Installation Database";

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 3;
                row[1] = packageName;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 4;
                row[1] = packageAuthor;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 5;
                row[1] = keywords;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 6;
                row[1] = comments;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 7;
                row[1] = String.Format(CultureInfo.InvariantCulture, "{0};{1}", platform, packageLanguages);

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 9;
                row[1] = packageCode;

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 14;
                row[1] = msiVersion.ToString(CultureInfo.InvariantCulture);

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 15;
                row[1] = sourceBits.ToString(CultureInfo.InvariantCulture);

                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 19;
                switch (security)
                {
                    case YesNoDefaultType.No: // no restriction
                        row[1] = "0";
                        break;
                    case YesNoDefaultType.Default: // read-only recommended
                        row[1] = "2";
                        break;
                    case YesNoDefaultType.Yes: // read-only enforced
                        row[1] = "4";
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a patch metadata element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParsePatchMetadataElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            YesNoType allowRemoval = YesNoType.NotSet;
            string classification = null;
            string creationTimeUtc = null;
            string description = null;
            string displayName = null;
            string manufacturerName = null;
            string minorUpdateTargetRTM = null;
            string moreInfoUrl = null;
            int optimizeCA = CompilerConstants.IntegerNotSet;
            YesNoType optimizedInstallMode = YesNoType.NotSet;
            string targetProductName = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AllowRemoval":
                            allowRemoval = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Classification":
                            classification = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreationTimeUTC":
                            creationTimeUtc = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ManufacturerName":
                            manufacturerName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinorUpdateTargetRTM":
                            minorUpdateTargetRTM = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MoreInfoURL":
                            moreInfoUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OptimizedInstallMode":
                            optimizedInstallMode = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetProductName":
                            targetProductName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (YesNoType.NotSet == allowRemoval)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AllowRemoval"));
            }

            if (null == classification)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }

            if (null == description)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (null == displayName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }

            if (null == manufacturerName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ManufacturerName"));
            }

            if (null == moreInfoUrl)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "MoreInfoURL"));
            }

            if (null == targetProductName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetProductName"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "CustomProperty":
                            this.ParseCustomPropertyElement(child);
                            break;
                        case "OptimizeCustomActions":
                            optimizeCA = this.ParseOptimizeCustomActionsElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                if (YesNoType.NotSet != allowRemoval)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "AllowRemoval";
                    row[2] = YesNoType.Yes == allowRemoval ? "1" : "0";
                }

                if (null != classification)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "Classification";
                    row[2] = classification;
                }

                if (null != creationTimeUtc)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "CreationTimeUTC";
                    row[2] = creationTimeUtc;
                }

                if (null != description)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "Description";
                    row[2] = description;
                }

                if (null != displayName)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "DisplayName";
                    row[2] = displayName;
                }

                if (null != manufacturerName)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "ManufacturerName";
                    row[2] = manufacturerName;
                }

                if (null != minorUpdateTargetRTM)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "MinorUpdateTargetRTM";
                    row[2] = minorUpdateTargetRTM;
                }

                if (null != moreInfoUrl)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "MoreInfoURL";
                    row[2] = moreInfoUrl;
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "OptimizeCA";
                    row[2] = optimizeCA.ToString(CultureInfo.InvariantCulture);
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "OptimizedInstallMode";
                    row[2] = YesNoType.Yes == optimizedInstallMode ? "1" : "0";
                }

                if (null != targetProductName)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                    row[0] = null;
                    row[1] = "TargetProductName";
                    row[2] = targetProductName;
                }
            }
        }

        /// <summary>
        /// Parses a custom property element for the PatchMetadata table.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseCustomPropertyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string company = null;
            string property = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Company":
                            company = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == company)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
            }

            if (null == property)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
                row[0] = company;
                row[1] = property;
                row[2] = value;
            }
        }

        /// <summary>
        /// Parses the OptimizeCustomActions element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>The combined integer value for callers to store as appropriate.</returns>
        private int ParseOptimizeCustomActionsElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            OptimizeCA optimizeCA = OptimizeCA.None;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "SkipAssignment":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipAssignment;
                            }
                            break;
                        case "SkipImmediate":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipImmediate;
                            }
                            break;
                        case "SkipDeferred":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipDeferred;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            return (int)optimizeCA;
        }

        /// <summary>
        /// Parses a patch information element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParsePatchInformationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string codepage = "1252";
            string comments = null;
            string keywords = "Installer,Patching,PCP,Database";
            int msiVersion = 1; // Should always be 1 for patches
            string packageAuthor = null;
            string packageName = this.activeName;
            YesNoDefaultType security = YesNoDefaultType.Default;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AdminImage":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Comments":
                            comments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Description":
                            packageName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Keywords":
                            keywords = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Manufacturer":
                            packageAuthor = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Platforms":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "ReadOnly":
                            security = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortNames":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "SummaryCodepage":
                            codepage = this.core.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                // PID_CODEPAGE
                Row row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 1;
                row[1] = codepage;

                // PID_TITLE
                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 2;
                row[1] = "Patch";

                // PID_SUBJECT
                if (null != packageName)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                    row[0] = 3;
                    row[1] = packageName;
                }

                // PID_AUTHOR
                if (null != packageAuthor)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                    row[0] = 4;
                    row[1] = packageAuthor;
                }

                // PID_KEYWORDS
                if (null != keywords)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                    row[0] = 5;
                    row[1] = keywords;
                }

                // PID_COMMENTS
                if (null != comments)
                {
                    row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                    row[0] = 6;
                    row[1] = comments;
                }

                // PID_PAGECOUNT
                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 14;
                row[1] = msiVersion.ToString(CultureInfo.InvariantCulture);

                // PID_WORDCOUNT
                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 15;
                row[1] = "0";

                // PID_SECURITY
                row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
                row[0] = 19;
                switch (security)
                {
                    case YesNoDefaultType.No: // no restriction
                        row[1] = "0";
                        break;
                    case YesNoDefaultType.Default: // read-only recommended
                        row[1] = "2";
                        break;
                    case YesNoDefaultType.Yes: // read-only enforced
                        row[1] = "4";
                        break;
                }
            }
        }

        /// <summary>
        /// Parses an ignore modularization element.
        /// </summary>
        /// <param name="node">XmlNode on an IgnoreModulatization element.</param>
        private void ParseIgnoreModularizationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;

            this.core.OnMessage(WixWarnings.DeprecatedIgnoreModularizationElement(sourceLineNumbers));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            // this is actually not used
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixSuppressModularization");
                row[0] = name;
            }
        }

        /// <summary>
        /// Parses a permission element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="objectId">Identifier of object to be secured.</param>
        /// <param name="tableName">Name of table that contains objectId.</param>
        private void ParsePermissionElement(XElement node, string objectId, string tableName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            BitArray bits = new BitArray(32);
            string domain = null;
            int permission = 0;
            string[] specialPermissions = null;
            string user = null;

            switch (tableName)
            {
                case "CreateFolder":
                    specialPermissions = Common.FolderPermissions;
                    break;
                case "File":
                    specialPermissions = Common.FilePermissions;
                    break;
                case "Registry":
                    specialPermissions = Common.RegistryPermissions;
                    break;
                default:
                    this.core.UnexpectedElement(node.Parent, node);
                    return; // stop processing this element since no valid permissions are available
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Domain":
                            domain = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "User":
                            user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FileAllRights":
                            // match the WinNT.h mask FILE_ALL_ACCESS for value 0x001F01FF (aka 1 1111 0000 0001 1111 1111 or 2032127)
                            bits[0] = bits[1] = bits[2] = bits[3] = bits[4] = bits[5] = bits[6] = bits[7] = bits[8] = bits[16] = bits[17] = bits[18] = bits[19] = bits[20] = true;
                            break;
                        case "SpecificRightsAll":
                            // match the WinNT.h mask SPECIFIC_RIGHTS_ALL for value 0x0000FFFF (aka 1111 1111 1111 1111)
                            bits[0] = bits[1] = bits[2] = bits[3] = bits[4] = bits[5] = bits[6] = bits[7] = bits[8] = bits[9] = bits[10] = bits[11] = bits[12] = bits[13] = bits[14] = bits[15] = true;
                            break;
                        default:
                            YesNoType attribValue = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.core.TrySetBitFromName(Common.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.core.TrySetBitFromName(Common.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.core.TrySetBitFromName(specialPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.core.UnexpectedAttribute(node, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            permission = this.core.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "User"));
            }

            if (int.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.core.OnMessage(WixErrors.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "LockPermissions");
                row[0] = objectId;
                row[1] = tableName;
                row[2] = domain;
                row[3] = user;
                row[4] = permission;
            }
        }

        /// <summary>
        /// Parses an extended permission element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="objectId">Identifier of object to be secured.</param>
        /// <param name="tableName">Name of table that contains objectId.</param>
        private void ParsePermissionExElement(XElement node, string objectId, string tableName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = null;
            Identifier id = null;
            string sddl = null;

            switch (tableName)
            {
                case "CreateFolder":
                case "File":
                case "Registry":
                case "ServiceInstall":
                    break;
                default:
                    this.core.UnexpectedElement(node.Parent, node);
                    return; // stop processing this element since nothing will be valid.
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Sddl":
                            sddl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == sddl)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Sddl"));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("pme", objectId, tableName, sddl);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Condition":
                            if (null != condition)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }

                            condition = this.ParseConditionElement(child, node.Name.LocalName, null, null);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiLockPermissionsEx", id);
                row[1] = objectId;
                row[2] = tableName;
                row[3] = sddl;
                row[4] = condition;
            }
        }

        /// <summary>
        /// Parses a product element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void ParseProductElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int codepage = 65001;
            string productCode = null;
            string upgradeCode = null;
            string manufacturer = null;
            string version = null;
            string symbols = null;

            this.activeName = null;
            this.activeLanguage = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            productCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Codepage":
                            codepage = this.core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            this.activeLanguage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Manufacturer":
                            manufacturer = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.MustHaveNonWhitespaceCharacters);
                            if ("PUT-COMPANY-NAME-HERE" == manufacturer)
                            {
                                this.core.OnMessage(WixWarnings.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, manufacturer));
                            }
                            break;
                        case "Name":
                            this.activeName = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.MustHaveNonWhitespaceCharacters);
                            if ("PUT-PRODUCT-NAME-HERE" == this.activeName)
                            {
                                this.core.OnMessage(WixWarnings.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, this.activeName));
                            }
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version": // if the attribute is valid version, use the attribute value as is (so "1.0000.01.01" would *not* get translated to "1.0.1.1").
                            string verifiedVersion = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            if (!String.IsNullOrEmpty(verifiedVersion))
                            {
                                version = attrib.Value;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == productCode)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == this.activeLanguage)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == manufacturer)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            if (null == this.activeName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == upgradeCode)
            {
                this.core.OnMessage(WixWarnings.MissingUpgradeCode(sourceLineNumbers));
            }

            if (null == version)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidProductVersion(version))
            {
                this.core.OnMessage(WixErrors.InvalidProductVersion(sourceLineNumbers, version));
            }

            if (this.core.EncounteredError)
            {
                return;
            }

            try
            {
                this.compilingProduct = true;
                this.core.CreateActiveSection(productCode, SectionType.Product, codepage);

                this.AddProperty(sourceLineNumbers, new Identifier("Manufacturer", AccessModifier.Public), manufacturer, false, false, false, true);
                this.AddProperty(sourceLineNumbers, new Identifier("ProductCode", AccessModifier.Public), productCode, false, false, false, true);
                this.AddProperty(sourceLineNumbers, new Identifier("ProductLanguage", AccessModifier.Public), this.activeLanguage, false, false, false, true);
                this.AddProperty(sourceLineNumbers, new Identifier("ProductName", AccessModifier.Public), this.activeName, false, false, false, true);
                this.AddProperty(sourceLineNumbers, new Identifier("ProductVersion", AccessModifier.Public), version, false, false, false, true);
                if (null != upgradeCode)
                {
                    this.AddProperty(sourceLineNumbers, new Identifier("UpgradeCode", AccessModifier.Public), upgradeCode, false, false, false, true);
                }

                Dictionary<string, string> contextValues = new Dictionary<string, string>();
                contextValues["ProductLanguage"] = this.activeLanguage;
                contextValues["ProductVersion"] = version;
                contextValues["UpgradeCode"] = upgradeCode;

                int featureDisplay = 0;
                foreach (XElement child in node.Elements())
                {
                    if (CompilerCore.WixNamespace == child.Name.Namespace)
                    {
                        switch (child.Name.LocalName)
                        {
                            case "_locDefinition":
                                break;
                            case "AdminExecuteSequence":
                            case "AdminUISequence":
                            case "AdvertiseExecuteSequence":
                            case "InstallExecuteSequence":
                            case "InstallUISequence":
                                this.ParseSequenceElement(child, child.Name.LocalName);
                                break;
                            case "AppId":
                                this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
                                break;
                            case "Binary":
                                this.ParseBinaryElement(child);
                                break;
                            case "ComplianceCheck":
                                this.ParseComplianceCheckElement(child);
                                break;
                            case "Component":
                                this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, CompilerConstants.IntegerNotSet, null, null);
                                break;
                            case "ComponentGroup":
                                this.ParseComponentGroupElement(child, ComplexReferenceParentType.Unknown, null);
                                break;
                            case "Condition":
                                this.ParseConditionElement(child, node.Name.LocalName, null, null);
                                break;
                            case "CustomAction":
                                this.ParseCustomActionElement(child);
                                break;
                            case "CustomActionRef":
                                this.ParseSimpleRefElement(child, "CustomAction");
                                break;
                            case "CustomTable":
                                this.ParseCustomTableElement(child);
                                break;
                            case "Directory":
                                this.ParseDirectoryElement(child, null, CompilerConstants.IntegerNotSet, String.Empty);
                                break;
                            case "DirectoryRef":
                                this.ParseDirectoryRefElement(child);
                                break;
                            case "EmbeddedChainer":
                                this.ParseEmbeddedChainerElement(child);
                                break;
                            case "EmbeddedChainerRef":
                                this.ParseSimpleRefElement(child, "MsiEmbeddedChainer");
                                break;
                            case "EnsureTable":
                                this.ParseEnsureTableElement(child);
                                break;
                            case "Feature":
                                this.ParseFeatureElement(child, ComplexReferenceParentType.Product, productCode, ref featureDisplay);
                                break;
                            case "FeatureRef":
                                this.ParseFeatureRefElement(child, ComplexReferenceParentType.Product, productCode);
                                break;
                            case "FeatureGroupRef":
                                this.ParseFeatureGroupRefElement(child, ComplexReferenceParentType.Product, productCode);
                                break;
                            case "Icon":
                                this.ParseIconElement(child);
                                break;
                            case "InstanceTransforms":
                                this.ParseInstanceTransformsElement(child);
                                break;
                            case "MajorUpgrade":
                                this.ParseMajorUpgradeElement(child, contextValues);
                                break;
                            case "Media":
                                this.ParseMediaElement(child, null);
                                break;
                            case "MediaTemplate":
                                this.ParseMediaTemplateElement(child, null);
                                break;
                            case "Package":
                                this.ParsePackageElement(child, manufacturer, null);
                                break;
                            case "PackageCertificates":
                            case "PatchCertificates":
                                this.ParseCertificatesElement(child);
                                break;
                            case "Property":
                                this.ParsePropertyElement(child);
                                break;
                            case "PropertyRef":
                                this.ParseSimpleRefElement(child, "Property");
                                break;
                            case "SetDirectory":
                                this.ParseSetDirectoryElement(child);
                                break;
                            case "SetProperty":
                                this.ParseSetPropertyElement(child);
                                break;
                            case "SFPCatalog":
                                string parentName = null;
                                this.ParseSFPCatalogElement(child, ref parentName);
                                break;
                            case "SymbolPath":
                                if (null != symbols)
                                {
                                    symbols += ";" + this.ParseSymbolPathElement(child);
                                }
                                else
                                {
                                    symbols = this.ParseSymbolPathElement(child);
                                }
                                break;
                            case "UI":
                                this.ParseUIElement(child);
                                break;
                            case "UIRef":
                                this.ParseSimpleRefElement(child, "WixUI");
                                break;
                            case "Upgrade":
                                this.ParseUpgradeElement(child);
                                break;
                            case "WixVariable":
                                this.ParseWixVariableElement(child);
                                break;
                            default:
                                this.core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.core.ParseExtensionElement(node, child);
                    }
                }

                if (!this.core.EncounteredError)
                {
                    if (null != symbols)
                    {
                        WixDeltaPatchSymbolPathsRow symbolRow = (WixDeltaPatchSymbolPathsRow)this.core.CreateRow(sourceLineNumbers, "WixDeltaPatchSymbolPaths");
                        symbolRow.Id = productCode;
                        symbolRow.Type = SymbolPathType.Product;
                        symbolRow.SymbolPaths = symbols;
                    }
                }
            }
            finally
            {
                this.compilingProduct = false;
            }
        }

        /// <summary>
        /// Parses a progid element
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="advertise">Flag if progid is advertised.</param>
        /// <param name="classId">CLSID related to ProgId.</param>
        /// <param name="description">Default description of ProgId</param>
        /// <param name="parent">Optional parent ProgId</param>
        /// <param name="foundExtension">Set to true if an extension is found; used for error-checking.</param>
        /// <param name="firstProgIdForClass">Whether or not this ProgId is the first one found in the parent class.</param>
        /// <returns>This element's Id.</returns>
        private string ParseProgIdElement(XElement node, string componentId, YesNoType advertise, string classId, string description, string parent, ref bool foundExtension, YesNoType firstProgIdForClass)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string icon = null;
            int iconIndex = CompilerConstants.IntegerNotSet;
            string noOpen = null;
            string progId = null;
            YesNoType progIdAdvertise = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            progId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            progIdAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Icon":
                            icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "IconIndex":
                            iconIndex = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "NoOpen":
                            noOpen = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == progIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == progIdAdvertise))
            {
                this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, advertise.ToString(), progIdAdvertise.ToString()));
            }
            else
            {
                advertise = progIdAdvertise;
            }

            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            if (null != parent && (null != icon || CompilerConstants.IntegerNotSet != iconIndex))
            {
                this.core.OnMessage(WixErrors.VersionIndependentProgIdsCannotHaveIcons(sourceLineNumbers));
            }

            YesNoType firstProgIdForNestedClass = YesNoType.Yes;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Extension":
                            this.ParseExtensionElement(child, componentId, advertise, progId);
                            foundExtension = true;
                            break;
                        case "ProgId":
                            // Only allow one nested ProgId.  If we have a child, we should not have a parent.
                            if (null == parent)
                            {
                                if (YesNoType.Yes == advertise)
                                {
                                    this.ParseProgIdElement(child, componentId, advertise, null, description, progId, ref foundExtension, firstProgIdForNestedClass);
                                }
                                else if (YesNoType.No == advertise)
                                {
                                    this.ParseProgIdElement(child, componentId, advertise, classId, description, progId, ref foundExtension, firstProgIdForNestedClass);
                                }

                                firstProgIdForNestedClass = YesNoType.No; // any ProgId after this one is definitely not the first.
                            }
                            else
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.ProgIdNestedTooDeep(childSourceLineNumbers));
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (YesNoType.Yes == advertise)
            {
                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "ProgId");
                    row[0] = progId;
                    row[1] = parent;
                    row[2] = classId;
                    row[3] = description;
                    if (null != icon)
                    {
                        row[4] = icon;
                        this.core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                    }

                    if (CompilerConstants.IntegerNotSet != iconIndex)
                    {
                        row[5] = iconIndex;
                    }

                    this.core.EnsureTable(sourceLineNumbers, "Class");
                }
            }
            else if (YesNoType.No == advertise)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, progId, String.Empty, description, componentId);
                if (null != classId)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CLSID"), String.Empty, classId, componentId);
                    if (null != parent)   // if this is a version independent ProgId
                    {
                        if (YesNoType.Yes == firstProgIdForClass)
                        {
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\VersionIndependentProgID"), String.Empty, progId, componentId);
                        }

                        this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CurVer"), String.Empty, parent, componentId);
                    }
                    else
                    {
                        if (YesNoType.Yes == firstProgIdForClass)
                        {
                            this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\ProgID"), String.Empty, progId, componentId);
                        }
                    }
                }

                if (null != icon)   // ProgId's Default Icon
                {
                    this.core.CreateSimpleReference(sourceLineNumbers, "File", icon);

                    icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                    if (CompilerConstants.IntegerNotSet != iconIndex)
                    {
                        icon = String.Concat(icon, ",", iconIndex);
                    }

                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\DefaultIcon"), String.Empty, icon, componentId);
                }
            }

            if (null != noOpen)
            {
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, progId, "NoOpen", noOpen, componentId); // ProgId NoOpen name
            }

            // raise an error for an orphaned ProgId
            if (YesNoType.Yes == advertise && !foundExtension && null == parent && null == classId)
            {
                this.core.OnMessage(WixWarnings.OrphanedProgId(sourceLineNumbers, progId));
            }

            return progId;
        }

        /// <summary>
        /// Parses a property element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParsePropertyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            bool admin = false;
            bool complianceCheck = false;
            bool hidden = false;
            bool secure = false;
            YesNoType suppressModularization = YesNoType.NotSet;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Admin":
                            admin = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ComplianceCheck":
                            complianceCheck = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Hidden":
                            hidden = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Secure":
                            secure = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if ("ProductID" == id.Id)
            {
                this.core.OnMessage(WixWarnings.ProductIdAuthored(sourceLineNumbers));
            }
            else if ("SecureCustomProperties" == id.Id || "AdminProperties" == id.Id || "MsiHiddenProperties" == id.Id)
            {
                this.core.OnMessage(WixErrors.CannotAuthorSpecialProperties(sourceLineNumbers, id.Id));
            }

            string innerText = this.core.GetTrimmedInnerText(node);
            if (null != value)
            {
                // cannot specify both the value attribute and inner text
                if (!String.IsNullOrEmpty(innerText))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }
            else // value attribute not specified, use inner text if any.
            {
                value = innerText;
            }

            if ("ErrorDialog" == id.Id)
            {
                this.core.CreateSimpleReference(sourceLineNumbers, "Dialog", value);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    {
                        switch (child.Name.LocalName)
                        {
                            case "ProductSearch":
                                this.ParseProductSearchElement(child, id.Id);
                                secure = true;
                                break;
                            default:
                                // let ParseSearchSignatures handle standard AppSearch children and unknown elements
                                break;
                        }
                    }
                }
            }

            // see if this property is used for appSearch
            List<string> signatures = this.ParseSearchSignatures(node);

            // If we're doing CCP then there must be a signature.
            if (complianceCheck && 0 == signatures.Count)
            {
                this.core.OnMessage(WixErrors.SearchElementRequiredWithAttribute(sourceLineNumbers, node.Name.LocalName, "ComplianceCheck", "yes"));
            }

            foreach (string sig in signatures)
            {
                if (complianceCheck && !this.core.EncounteredError)
                {
                    this.core.CreateRow(sourceLineNumbers, "CCPSearch", new Identifier(sig, AccessModifier.Private));
                }

                this.AddAppSearch(sourceLineNumbers, id, sig);
            }

            // If we're doing AppSearch get that setup.
            if (0 < signatures.Count)
            {
                this.AddProperty(sourceLineNumbers, id, value, admin, secure, hidden, false);
            }
            else // just a normal old property.
            {
                // If the property value is empty and none of the flags are set, print out a warning that we're ignoring
                // the element.
                if (String.IsNullOrEmpty(value) && !admin && !secure && !hidden)
                {
                    this.core.OnMessage(WixWarnings.PropertyUseless(sourceLineNumbers, id.Id));
                }
                else // there is a value and/or a flag set, do that.
                {
                    this.AddProperty(sourceLineNumbers, id, value, admin, secure, hidden, false);
                }
            }

            if (!this.core.EncounteredError && YesNoType.Yes == suppressModularization)
            {
                this.core.OnMessage(WixWarnings.PropertyModularizationSuppressed(sourceLineNumbers));

                this.core.CreateRow(sourceLineNumbers, "WixSuppressModularization", id);
            }
        }

        /// <summary>
        /// Parses a RegistryKey element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="root">Root specified when element is nested under another Registry element, otherwise CompilerConstants.IntegerNotSet.</param>
        /// <param name="parentKey">Parent key for this Registry element when nested.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        /// <param name="possibleKeyPath">Identifier of this registry key since it could be the component's keypath.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the Registry table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        private YesNoType ParseRegistryKeyElement(XElement node, string componentId, int root, string parentKey, bool win64Component, out string possibleKeyPath)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = parentKey; // default to parent key path
            string action = null;
            bool forceCreateOnInstall = false;
            bool forceDeleteOnUninstall = false;
            Wix.RegistryKey.ActionType actionType = Wix.RegistryKey.ActionType.NotSet;
            YesNoType keyPath = YesNoType.NotSet;

            possibleKeyPath = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            this.core.OnMessage(WixWarnings.DeprecatedRegistryKeyActionAttribute(sourceLineNumbers));
                            action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < action.Length)
                            {
                                actionType = Wix.RegistryKey.ParseActionType(action);
                                switch (actionType)
                                {
                                    case Wix.RegistryKey.ActionType.create:
                                        forceCreateOnInstall = true;
                                        break;
                                    case Wix.RegistryKey.ActionType.createAndRemoveOnUninstall:
                                        forceCreateOnInstall = true;
                                        forceDeleteOnUninstall = true;
                                        break;
                                    case Wix.RegistryKey.ActionType.none:
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "create", "createAndRemoveOnUninstall", "none"));
                                        break;
                                }
                            }
                            break;
                        case "ForceCreateOnInstall":
                            forceCreateOnInstall = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ForceDeleteOnUninstall":
                            forceDeleteOnUninstall = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (null != parentKey)
                            {
                                key = Path.Combine(parentKey, key);
                            }
                            break;
                        case "Root":
                            if (CompilerConstants.IntegerNotSet != root)
                            {
                                this.core.OnMessage(WixErrors.RegistryRootInvalid(sourceLineNumbers));
                            }

                            root = this.core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            string name = forceCreateOnInstall ? (forceDeleteOnUninstall ? "*" : "+") : (forceDeleteOnUninstall ? "-" : null);

            if (forceCreateOnInstall || forceDeleteOnUninstall) // generates a Registry row, so an Id must be present
            {
                // generate the identifier if it wasn't provided
                if (null == id)
                {
                    id = this.core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
                }
            }
            else // does not generate a Registry row, so no Id should be present
            {
                if (null != id)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Id", "ForceCreateOnInstall", "ForceDeleteOnUninstall", "yes", true));
                }
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
                root = CompilerConstants.IllegalInteger;
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
                key = String.Empty; // set the key to something to prevent null reference exceptions
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    string possibleChildKeyPath = null;

                    switch (child.Name.LocalName)
                    {
                        case "RegistryKey":
                            if (YesNoType.Yes == this.ParseRegistryKeyElement(child, componentId, root, key, win64Component, out possibleChildKeyPath))
                            {
                                if (YesNoType.Yes == keyPath)
                                {
                                    this.core.OnMessage(WixErrors.ComponentMultipleKeyPaths(sourceLineNumbers, child.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
                                }

                                possibleKeyPath = possibleChildKeyPath; // the child is the key path
                                keyPath = YesNoType.Yes;
                            }
                            else if (null == possibleKeyPath && null != possibleChildKeyPath)
                            {
                                possibleKeyPath = possibleChildKeyPath;
                            }
                            break;
                        case "RegistryValue":
                            if (YesNoType.Yes == this.ParseRegistryValueElement(child, componentId, root, key, win64Component, out possibleChildKeyPath))
                            {
                                if (YesNoType.Yes == keyPath)
                                {
                                    this.core.OnMessage(WixErrors.ComponentMultipleKeyPaths(sourceLineNumbers, child.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
                                }

                                possibleKeyPath = possibleChildKeyPath; // the child is the key path
                                keyPath = YesNoType.Yes;
                            }
                            else if (null == possibleKeyPath && null != possibleChildKeyPath)
                            {
                                possibleKeyPath = possibleChildKeyPath;
                            }
                            break;
                        case "Permission":
                            if (!forceCreateOnInstall)
                            {
                                this.core.OnMessage(WixErrors.UnexpectedElementWithAttributeValue(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "ForceCreateOnInstall", "yes"));
                            }
                            this.ParsePermissionElement(child, id.Id, "Registry");
                            break;
                        case "PermissionEx":
                            if (!forceCreateOnInstall)
                            {
                                this.core.OnMessage(WixErrors.UnexpectedElementWithAttributeValue(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "ForceCreateOnInstall", "yes"));
                            }
                            this.ParsePermissionExElement(child, id.Id, "Registry");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "RegistryId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }


            if (!this.core.EncounteredError && null != name)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Registry", id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = null;
                row[5] = componentId;
            }

            return keyPath;
        }

        /// <summary>
        /// Parses a RegistryValue element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="root">Root specified when element is nested under a RegistryKey element, otherwise CompilerConstants.IntegerNotSet.</param>
        /// <param name="parentKey">Root specified when element is nested under a RegistryKey element, otherwise CompilerConstants.IntegerNotSet.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        /// <param name="possibleKeyPath">Identifier of this registry key since it could be the component's keypath.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the Registry table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        private YesNoType ParseRegistryValueElement(XElement node, string componentId, int root, string parentKey, bool win64Component, out string possibleKeyPath)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = parentKey; // default to parent key path
            string name = null;
            string value = null;
            string type = null;
            Wix.RegistryValue.TypeType typeType = Wix.RegistryValue.TypeType.NotSet;
            string action = null;
            Wix.RegistryValue.ActionType actionType = Wix.RegistryValue.ActionType.NotSet;
            YesNoType keyPath = YesNoType.NotSet;
            bool couldBeKeyPath = true; // assume that this is a regular registry key that could become the key path

            possibleKeyPath = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < action.Length)
                            {
                                if (!Wix.RegistryValue.TryParseActionType(action, out actionType))
                                {
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "append", "prepend", "write"));
                                }
                            }
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (null != parentKey)
                            {
                                if (parentKey.EndsWith("\\", StringComparison.Ordinal))
                                {
                                    key = String.Concat(parentKey, key);
                                }
                                else
                                {
                                    key = String.Concat(parentKey, "\\", key);
                                }
                            }
                            break;
                        case "KeyPath":
                            keyPath = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            if (CompilerConstants.IntegerNotSet != root)
                            {
                                this.core.OnMessage(WixErrors.RegistryRootInvalid(sourceLineNumbers));
                            }

                            root = this.core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Type":
                            type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < type.Length)
                            {
                                if (!Wix.RegistryValue.TryParseTypeType(type, out typeType))
                                {
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, type, "binary", "expandable", "integer", "multiString", "string"));
                                }
                            }
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if ((Wix.RegistryValue.ActionType.append == actionType || Wix.RegistryValue.ActionType.prepend == actionType) &&
                Wix.RegistryValue.TypeType.multiString != typeType)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Action", action, "Type", "multiString"));
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (null == type)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MultiStringValue":
                            if (Wix.RegistryValue.TypeType.multiString != typeType && null != value)
                            {
                                this.core.OnMessage(WixErrors.RegistryMultipleValuesWithoutMultiString(sourceLineNumbers, node.Name.LocalName, "Value", child.Name.LocalName, "Type"));
                            }
                            else if (null == value)
                            {
                                value = Common.GetInnerText(child);
                            }
                            else
                            {
                                value = String.Concat(value, "[~]", Common.GetInnerText(child));
                            }
                            break;
                        case "Permission":
                            this.ParsePermissionElement(child, id.Id, "Registry");
                            break;
                        case "PermissionEx":
                            this.ParsePermissionExElement(child, id.Id, "Registry");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "RegistryId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }


            switch (typeType)
            {
                case Wix.RegistryValue.TypeType.binary:
                    value = String.Concat("#x", value);
                    break;
                case Wix.RegistryValue.TypeType.expandable:
                    value = String.Concat("#%", value);
                    break;
                case Wix.RegistryValue.TypeType.integer:
                    value = String.Concat("#", value);
                    break;
                case Wix.RegistryValue.TypeType.multiString:
                    switch (actionType)
                    {
                        case Wix.RegistryValue.ActionType.append:
                            value = String.Concat("[~]", value);
                            break;
                        case Wix.RegistryValue.ActionType.prepend:
                            value = String.Concat(value, "[~]");
                            break;
                        case Wix.RegistryValue.ActionType.write:
                        default:
                            if (null != value && -1 == value.IndexOf("[~]", StringComparison.Ordinal))
                            {
                                value = String.Format(CultureInfo.InvariantCulture, "[~]{0}[~]", value);
                            }
                            break;
                    }
                    break;
                case Wix.RegistryValue.TypeType.@string:
                    // escape the leading '#' character for string registry keys
                    if (null != value && value.StartsWith("#", StringComparison.Ordinal))
                    {
                        value = String.Concat("#", value);
                    }
                    break;
            }

            // value may be set by child MultiStringValue elements, so it must be checked here
            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }
            else if (0 == value.Length && ("+" == name || "-" == name || "*" == name)) // prevent accidental authoring of special name values
            {
                this.core.OnMessage(WixErrors.RegistryNameValueIncorrect(sourceLineNumbers, node.Name.LocalName, "Name", name));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Registry", id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = value;
                row[5] = componentId;
            }

            // If this was just a regular registry key (that could be the key path)
            // and no child registry key set the possible key path, let's make this
            // Registry/@Id a possible key path.
            if (couldBeKeyPath && null == possibleKeyPath)
            {
                possibleKeyPath = id.Id;
            }

            return keyPath;
        }

        /// <summary>
        /// Parses a RemoveRegistryKey element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">The component identifier of the parent element.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the Registry table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        private void ParseRemoveRegistryKeyElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string action = null;
            Wix.RemoveRegistryKey.ActionType actionType = Wix.RemoveRegistryKey.ActionType.NotSet;
            string key = null;
            string name = "-";
            int root = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < action.Length)
                            {
                                if (!Wix.RemoveRegistryKey.TryParseActionType(action, out actionType))
                                {
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "removeOnInstall", "removeOnUninstall"));
                                }
                            }
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if (null == action)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, (Wix.RemoveRegistryKey.ActionType.removeOnUninstall == actionType ? "Registry" : "RemoveRegistry"), id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                if (Wix.RemoveRegistryKey.ActionType.removeOnUninstall == actionType) // Registry table
                {
                    row[4] = null;
                    row[5] = componentId;
                }
                else // RemoveRegistry table
                {
                    row[4] = componentId;
                }
            }
        }

        /// <summary>
        /// Parses a RemoveRegistryValue element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">The component identifier of the parent element.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the Registry table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        private void ParseRemoveRegistryValueElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = null;
            string name = null;
            int root = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "RemoveRegistry", id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = componentId;
            }
        }

        /// <summary>
        /// Parses a remove file element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentDirectory">Identifier of the parent component's directory.</param>
        private void ParseRemoveFileElement(XElement node, string componentId, string parentDirectory)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string directory = null;
            string name = null;
            int on = CompilerConstants.IntegerNotSet;
            string property = null;
            string shortName = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, parentDirectory);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, true);
                            break;
                        case "On":
                            Wix.InstallUninstallType onValue = this.core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
                            switch (onValue)
                            {
                                case Wix.InstallUninstallType.install:
                                    on = 1;
                                    break;
                                case Wix.InstallUninstallType.uninstall:
                                    on = 2;
                                    break;
                                case Wix.InstallUninstallType.both:
                                    on = 3;
                                    break;
                                default:
                                    on = CompilerConstants.IllegalInteger;
                                    break;
                            }
                            break;
                        case "Property":
                            property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.core.IsValidShortFilename(name, true))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.core.CreateShortName(name, true, true, node.Name.LocalName, componentId);
                }
            }

            if (CompilerConstants.IntegerNotSet == on)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "On"));
                on = CompilerConstants.IllegalInteger;
            }

            if (null != directory && null != property)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "Directory", directory));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("rmf", directory ?? property ?? parentDirectory, LowercaseOrNull(shortName), LowercaseOrNull(name), on.ToString());
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "RemoveFile", id);
                row[1] = componentId;
                row[2] = GetMsiFilenameValue(shortName, name);
                if (null != directory)
                {
                    row[3] = directory;
                }
                else if (null != property)
                {
                    row[3] = property;
                }
                else
                {
                    row[3] = parentDirectory;
                }
                row[4] = on;
            }
        }

        /// <summary>
        /// Parses a RemoveFolder element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentDirectory">Identifier of parent component's directory.</param>
        private void ParseRemoveFolderElement(XElement node, string componentId, string parentDirectory)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string directory = null;
            int on = CompilerConstants.IntegerNotSet;
            string property = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, parentDirectory);
                            break;
                        case "On":
                            Wix.InstallUninstallType onValue = this.core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
                            switch (onValue)
                            {
                                case Wix.InstallUninstallType.install:
                                    on = 1;
                                    break;
                                case Wix.InstallUninstallType.uninstall:
                                    on = 2;
                                    break;
                                case Wix.InstallUninstallType.both:
                                    on = 3;
                                    break;
                                default:
                                    on = CompilerConstants.IllegalInteger;
                                    break;
                            }
                            break;
                        case "Property":
                            property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == on)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "On"));
                on = CompilerConstants.IllegalInteger;
            }

            if (null != directory && null != property)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "Directory", directory));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("rmf", directory ?? property ?? parentDirectory, on.ToString());
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "RemoveFile", id);
                row[1] = componentId;
                row[2] = null;
                if (null != directory)
                {
                    row[3] = directory;
                }
                else if (null != property)
                {
                    row[3] = property;
                }
                else
                {
                    row[3] = parentDirectory;
                }
                row[4] = on;
            }
        }

        /// <summary>
        /// Parses a reserve cost element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="directoryId">Optional and default identifier of referenced directory.</param>
        private void ParseReserveCostElement(XElement node, string componentId, string directoryId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int runFromSource = CompilerConstants.IntegerNotSet;
            int runLocal = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directoryId = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        case "RunFromSource":
                            runFromSource = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "RunLocal":
                            runLocal = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("rc", componentId, directoryId);
            }

            if (CompilerConstants.IntegerNotSet == runFromSource)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RunFromSource"));
            }

            if (CompilerConstants.IntegerNotSet == runLocal)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RunLocal"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ReserveCost", id);
                row[1] = componentId;
                row[2] = directoryId;
                row[3] = runLocal;
                row[4] = runFromSource;
            }
        }

        /// <summary>
        /// Parses a sequence element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="sequenceTable">Name of sequence table.</param>
        private void ParseSequenceElement(XElement node, string sequenceTable)
        {
            // use the proper table name internally
            if ("AdvertiseExecuteSequence" == sequenceTable)
            {
                sequenceTable = "AdvtExecuteSequence";
            }

            // Parse each action in the sequence.
            foreach (XElement child in node.Elements())
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                string actionName = child.Name.LocalName;
                string afterAction = null;
                string beforeAction = null;
                string condition = null;
                bool customAction = "Custom" == actionName;
                bool overridable = false;
                int exitSequence = CompilerConstants.IntegerNotSet;
                int sequence = CompilerConstants.IntegerNotSet;
                bool showDialog = "Show" == actionName;
                bool specialAction = "InstallExecute" == actionName || "InstallExecuteAgain" == actionName || "RemoveExistingProducts" == actionName || "DisableRollback" == actionName || "ScheduleReboot" == actionName || "ForceReboot" == actionName || "ResolveSource" == actionName;
                bool specialStandardAction = "AppSearch" == actionName || "CCPSearch" == actionName || "RMCCPSearch" == actionName || "LaunchConditions" == actionName || "FindRelatedProducts" == actionName;
                bool suppress = false;

                foreach (XAttribute attrib in child.Attributes())
                {
                    if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                    {
                        switch (attrib.Name.LocalName)
                        {
                            case "Action":
                                if (customAction)
                                {
                                    actionName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.core.CreateSimpleReference(childSourceLineNumbers, "CustomAction", actionName);
                                }
                                else
                                {
                                    this.core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "After":
                                if (customAction || showDialog || specialAction || specialStandardAction)
                                {
                                    afterAction = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.core.CreateSimpleReference(childSourceLineNumbers, "WixAction", sequenceTable, afterAction);
                                }
                                else
                                {
                                    this.core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Before":
                                if (customAction || showDialog || specialAction || specialStandardAction)
                                {
                                    beforeAction = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.core.CreateSimpleReference(childSourceLineNumbers, "WixAction", sequenceTable, beforeAction);
                                }
                                else
                                {
                                    this.core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Dialog":
                                if (showDialog)
                                {
                                    actionName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.core.CreateSimpleReference(childSourceLineNumbers, "Dialog", actionName);
                                }
                                else
                                {
                                    this.core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "OnExit":
                                if (customAction || showDialog || specialAction)
                                {
                                    Wix.ExitType exitValue = this.core.GetAttributeExitValue(childSourceLineNumbers, attrib);
                                    switch (exitValue)
                                    {
                                        case Wix.ExitType.success:
                                            exitSequence = -1;
                                            break;
                                        case Wix.ExitType.cancel:
                                            exitSequence = -2;
                                            break;
                                        case Wix.ExitType.error:
                                            exitSequence = -3;
                                            break;
                                        case Wix.ExitType.suspend:
                                            exitSequence = -4;
                                            break;
                                    }
                                }
                                else
                                {
                                    this.core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Overridable":
                                overridable = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, attrib);
                                break;
                            case "Sequence":
                                sequence = this.core.GetAttributeIntegerValue(childSourceLineNumbers, attrib, 1, short.MaxValue);
                                break;
                            case "Suppress":
                                suppress = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, attrib);
                                break;
                            default:
                                this.core.UnexpectedAttribute(node, attrib);
                                break;
                        }
                    }
                    else
                    {
                        this.core.ParseExtensionAttribute(node, attrib);
                    }
                }


                // Get the condition from the inner text of the element.
                condition = this.core.GetConditionInnerText(child);

                if (customAction && "Custom" == actionName)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Action"));
                }
                else if (showDialog && "Show" == actionName)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Dialog"));
                }

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    if (CompilerConstants.IntegerNotSet != exitSequence)
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "Sequence", "OnExit"));
                    }
                    else if (null != beforeAction || null != afterAction)
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "Sequence", "Before", "After"));
                    }
                }
                else // sequence not specified use OnExit (which may also be not set).
                {
                    sequence = exitSequence;
                }

                if (null != beforeAction && null != afterAction)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "After", "Before"));
                }
                else if ((customAction || showDialog || specialAction) && !suppress && CompilerConstants.IntegerNotSet == sequence && null == beforeAction && null == afterAction)
                {
                    this.core.OnMessage(WixErrors.NeedSequenceBeforeOrAfter(childSourceLineNumbers, child.Name.LocalName));
                }

                // action that is scheduled to occur before/after itself
                if (beforeAction == actionName)
                {
                    this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name.LocalName, "Before", beforeAction));
                }
                else if (afterAction == actionName)
                {
                    this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name.LocalName, "After", afterAction));
                }

                // normal standard actions cannot be set overridable by the user (since they are overridable by default)
                if (overridable && WindowsInstallerStandard.IsStandardAction(actionName) && !specialAction)
                {
                    this.core.OnMessage(WixErrors.UnexpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Overridable"));
                }

                // suppress cannot be specified at the same time as Before, After, or Sequence
                if (suppress && (null != afterAction || null != beforeAction || CompilerConstants.IntegerNotSet != sequence || overridable))
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(childSourceLineNumbers, child.Name.LocalName, "Suppress", "Before", "After", "Sequence", "Overridable"));
                }

                this.core.ParseForExtensionElements(child);

                // add the row and any references needed
                if (!this.core.EncounteredError)
                {
                    if (suppress)
                    {
                        Row row = this.core.CreateRow(childSourceLineNumbers, "WixSuppressAction");
                        row[0] = sequenceTable;
                        row[1] = actionName;
                    }
                    else
                    {
                        Row row = this.core.CreateRow(childSourceLineNumbers, "WixAction");
                        row[0] = sequenceTable;
                        row[1] = actionName;
                        row[2] = condition;
                        if (CompilerConstants.IntegerNotSet != sequence)
                        {
                            row[3] = sequence;
                        }
                        row[4] = beforeAction;
                        row[5] = afterAction;
                        row[6] = overridable ? 1 : 0;
                    }
                }
            }
        }


        /// <summary>
        /// Parses a service config element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="serviceName">Optional element containing parent's service name.</param>
        private void ParseServiceConfigElement(XElement node, string componentId, string serviceName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string delayedAutoStart = null;
            string failureActionsWhen = null;
            int events = 0;
            string name = serviceName;
            string preShutdownDelay = null;
            string requiredPrivileges = null;
            string sid = null;

            this.core.OnMessage(WixWarnings.ServiceConfigFamilyNotSupported(sourceLineNumbers, node.Name.LocalName));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DelayedAutoStart":
                            delayedAutoStart = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < delayedAutoStart.Length)
                            {
                                switch (delayedAutoStart)
                                {
                                    case "no":
                                        delayedAutoStart = "0";
                                        break;
                                    case "yes":
                                        delayedAutoStart = "1";
                                        break;
                                    default:
                                        // allow everything else to pass through that are hopefully "formatted" Properties.
                                        break;
                                }
                            }
                            break;
                        case "FailureActionsWhen":
                            failureActionsWhen = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < failureActionsWhen.Length)
                            {
                                switch (failureActionsWhen)
                                {
                                    case "failedToStop":
                                        failureActionsWhen = "0";
                                        break;
                                    case "failedToStopOrReturnedError":
                                        failureActionsWhen = "1";
                                        break;
                                    default:
                                        // allow everything else to pass through that are hopefully "formatted" Properties.
                                        break;
                                }
                            }
                            break;
                        case "OnInstall":
                            YesNoType install = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == install)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventInstall;
                            }
                            break;
                        case "OnReinstall":
                            YesNoType reinstall = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == reinstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventReinstall;
                            }
                            break;
                        case "OnUninstall":
                            YesNoType uninstall = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == uninstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventUninstall;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                        case "PreShutdownDelay":
                            preShutdownDelay = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "ServiceName":
                            if (!String.IsNullOrEmpty(serviceName))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ServiceInstall"));
                            }

                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceSid":
                            sid = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < sid.Length)
                            {
                                switch (sid)
                                {
                                    case "none":
                                        sid = "0";
                                        break;
                                    case "restricted":
                                        sid = "3";
                                        break;
                                    case "unrestricted":
                                        sid = "1";
                                        break;
                                    default:
                                        // allow everything else to pass through that are hopefully "formatted" Properties.
                                        break;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Get the ServiceConfig required privilegs.
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RequiredPrivilege":
                            string privilege = this.core.GetTrimmedInnerText(child);
                            switch (privilege)
                            {
                                case "assignPrimaryToken":
                                    privilege = "SeAssignPrimaryTokenPrivilege";
                                    break;
                                case "audit":
                                    privilege = "SeAuditPrivilege";
                                    break;
                                case "backup":
                                    privilege = "SeBackupPrivilege";
                                    break;
                                case "changeNotify":
                                    privilege = "SeChangeNotifyPrivilege";
                                    break;
                                case "createGlobal":
                                    privilege = "SeCreateGlobalPrivilege";
                                    break;
                                case "createPagefile":
                                    privilege = "SeCreatePagefilePrivilege";
                                    break;
                                case "createPermanent":
                                    privilege = "SeCreatePermanentPrivilege";
                                    break;
                                case "createSymbolicLink":
                                    privilege = "SeCreateSymbolicLinkPrivilege";
                                    break;
                                case "createToken":
                                    privilege = "SeCreateTokenPrivilege";
                                    break;
                                case "debug":
                                    privilege = "SeDebugPrivilege";
                                    break;
                                case "enableDelegation":
                                    privilege = "SeEnableDelegationPrivilege";
                                    break;
                                case "impersonate":
                                    privilege = "SeImpersonatePrivilege";
                                    break;
                                case "increaseBasePriority":
                                    privilege = "SeIncreaseBasePriorityPrivilege";
                                    break;
                                case "increaseQuota":
                                    privilege = "SeIncreaseQuotaPrivilege";
                                    break;
                                case "increaseWorkingSet":
                                    privilege = "SeIncreaseWorkingSetPrivilege";
                                    break;
                                case "loadDriver":
                                    privilege = "SeLoadDriverPrivilege";
                                    break;
                                case "lockMemory":
                                    privilege = "SeLockMemoryPrivilege";
                                    break;
                                case "machineAccount":
                                    privilege = "SeMachineAccountPrivilege";
                                    break;
                                case "manageVolume":
                                    privilege = "SeManageVolumePrivilege";
                                    break;
                                case "profileSingleProcess":
                                    privilege = "SeProfileSingleProcessPrivilege";
                                    break;
                                case "relabel":
                                    privilege = "SeRelabelPrivilege";
                                    break;
                                case "remoteShutdown":
                                    privilege = "SeRemoteShutdownPrivilege";
                                    break;
                                case "restore":
                                    privilege = "SeRestorePrivilege";
                                    break;
                                case "security":
                                    privilege = "SeSecurityPrivilege";
                                    break;
                                case "shutdown":
                                    privilege = "SeShutdownPrivilege";
                                    break;
                                case "syncAgent":
                                    privilege = "SeSyncAgentPrivilege";
                                    break;
                                case "systemEnvironment":
                                    privilege = "SeSystemEnvironmentPrivilege";
                                    break;
                                case "systemProfile":
                                    privilege = "SeSystemProfilePrivilege";
                                    break;
                                case "systemTime":
                                case "modifySystemTime":
                                    privilege = "SeSystemtimePrivilege";
                                    break;
                                case "takeOwnership":
                                    privilege = "SeTakeOwnershipPrivilege";
                                    break;
                                case "tcb":
                                case "trustedComputerBase":
                                    privilege = "SeTcbPrivilege";
                                    break;
                                case "timeZone":
                                case "modifyTimeZone":
                                    privilege = "SeTimeZonePrivilege";
                                    break;
                                case "trustedCredManAccess":
                                case "trustedCredentialManagerAccess":
                                    privilege = "SeTrustedCredManAccessPrivilege";
                                    break;
                                case "undock":
                                    privilege = "SeUndockPrivilege";
                                    break;
                                case "unsolicitedInput":
                                    privilege = "SeUnsolicitedInputPrivilege";
                                    break;
                                default:
                                    // allow everything else to pass through that are hopefully "formatted" Properties.
                                    break;
                            }

                            if (null != requiredPrivileges)
                            {
                                requiredPrivileges = String.Concat(requiredPrivileges, "[~]");
                            }
                            requiredPrivileges = String.Concat(requiredPrivileges, privilege);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ServiceName"));
            }
            else if (null == id)
            {
                id = this.core.CreateIdentifierFromFilename(name);
            }

            if (0 == events)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "OnInstall", "OnReinstall", "OnUninstall"));
            }

            if (String.IsNullOrEmpty(delayedAutoStart) && String.IsNullOrEmpty(failureActionsWhen) && String.IsNullOrEmpty(preShutdownDelay) && String.IsNullOrEmpty(requiredPrivileges) && String.IsNullOrEmpty(sid))
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DelayedAutoStart", "FailureActionsWhen", "PreShutdownDelay", "ServiceSid", "RequiredPrivilege"));
            }

            if (!this.core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(delayedAutoStart))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfig", new Identifier(String.Concat(id.Id, ".DS"), id.Access));
                    row[1] = name;
                    row[2] = events;
                    row[3] = 3;
                    row[4] = delayedAutoStart;
                    row[5] = componentId;
                }

                if (!String.IsNullOrEmpty(failureActionsWhen))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfig", new Identifier(String.Concat(id.Id, ".FA"), id.Access));
                    row[1] = name;
                    row[2] = events;
                    row[3] = 4;
                    row[4] = failureActionsWhen;
                    row[5] = componentId;
                }

                if (!String.IsNullOrEmpty(sid))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfig", new Identifier(String.Concat(id.Id, ".SS"), id.Access));
                    row[1] = name;
                    row[2] = events;
                    row[3] = 5;
                    row[4] = sid;
                    row[5] = componentId;
                }

                if (!String.IsNullOrEmpty(requiredPrivileges))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfig", new Identifier(String.Concat(id.Id, ".RP"), id.Access));
                    row[1] = name;
                    row[2] = events;
                    row[3] = 6;
                    row[4] = requiredPrivileges;
                    row[5] = componentId;
                }

                if (!String.IsNullOrEmpty(preShutdownDelay))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfig", new Identifier(String.Concat(id.Id, ".PD"), id.Access));
                    row[1] = name;
                    row[2] = events;
                    row[3] = 7;
                    row[4] = preShutdownDelay;
                    row[5] = componentId;
                }
            }
        }

        /// <summary>
        /// Parses a service config failure actions element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="serviceName">Optional element containing parent's service name.</param>
        private void ParseServiceConfigFailureActionsElement(XElement node, string componentId, string serviceName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int events = 0;
            string name = serviceName;
            int resetPeriod = CompilerConstants.IntegerNotSet;
            string rebootMessage = null;
            string command = null;
            string actions = null;
            string actionsDelays = null;

            this.core.OnMessage(WixWarnings.ServiceConfigFamilyNotSupported(sourceLineNumbers, node.Name.LocalName));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Command":
                            command = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "OnInstall":
                            YesNoType install = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == install)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventInstall;
                            }
                            break;
                        case "OnReinstall":
                            YesNoType reinstall = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == reinstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventReinstall;
                            }
                            break;
                        case "OnUninstall":
                            YesNoType uninstall = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == uninstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventUninstall;
                            }
                            break;
                        case "RebootMessage":
                            rebootMessage = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "ResetPeriod":
                            resetPeriod = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "ServiceName":
                            if (!String.IsNullOrEmpty(serviceName))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ServiceInstall"));
                            }

                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Get the ServiceConfigFailureActions actions.
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Failure":
                            string action = null;
                            string delay = null;
                            SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                            foreach (XAttribute childAttrib in child.Attributes())
                            {
                                if (String.IsNullOrEmpty(childAttrib.Name.NamespaceName) || CompilerCore.WixNamespace == childAttrib.Name.Namespace)
                                {
                                    switch (childAttrib.Name.LocalName)
                                    {
                                        case "Action":
                                            action = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                            switch (action)
                                            {
                                                case "none":
                                                    action = "0";
                                                    break;
                                                case "restartComputer":
                                                    action = "2";
                                                    break;
                                                case "restartService":
                                                    action = "1";
                                                    break;
                                                case "runCommand":
                                                    action = "3";
                                                    break;
                                                default:
                                                    // allow everything else to pass through that are hopefully "formatted" Properties.
                                                    break;
                                            }
                                            break;
                                        case "Delay":
                                            delay = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                            break;
                                        default:
                                            this.core.UnexpectedAttribute(child, childAttrib);
                                            break;
                                    }
                                }
                            }

                            if (String.IsNullOrEmpty(action))
                            {
                                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, child.Name.LocalName, "Action"));
                            }

                            if (String.IsNullOrEmpty(delay))
                            {
                                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, child.Name.LocalName, "Delay"));
                            }

                            if (!String.IsNullOrEmpty(actions))
                            {
                                actions = String.Concat(actions, "[~]");
                            }
                            actions = String.Concat(actions, action);

                            if (!String.IsNullOrEmpty(actionsDelays))
                            {
                                actionsDelays = String.Concat(actionsDelays, "[~]");
                            }
                            actionsDelays = String.Concat(actionsDelays, delay);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ServiceName"));
            }
            else if (null == id)
            {
                id = this.core.CreateIdentifierFromFilename(name);
            }

            if (0 == events)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "OnInstall", "OnReinstall", "OnUninstall"));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiServiceConfigFailureActions", id);
                row[1] = name;
                row[2] = events;
                if (CompilerConstants.IntegerNotSet != resetPeriod)
                {
                    row[3] = resetPeriod;
                }
                row[4] = rebootMessage ?? "[~]";
                row[5] = command ?? "[~]";
                row[6] = actions;
                row[7] = actionsDelays;
                row[8] = componentId;
            }
        }

        /// <summary>
        /// Parses a service control element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseServiceControlElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string arguments = null;
            int events = 0; // default is to do nothing
            Identifier id = null;
            string name = null;
            YesNoType wait = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Remove":
                            Wix.InstallUninstallType removeValue = this.core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
                            switch (removeValue)
                            {
                                case Wix.InstallUninstallType.install:
                                    events |= MsiInterop.MsidbServiceControlEventDelete;
                                    break;
                                case Wix.InstallUninstallType.uninstall:
                                    events |= MsiInterop.MsidbServiceControlEventUninstallDelete;
                                    break;
                                case Wix.InstallUninstallType.both:
                                    events |= MsiInterop.MsidbServiceControlEventDelete | MsiInterop.MsidbServiceControlEventUninstallDelete;
                                    break;
                            }
                            break;
                        case "Start":
                            Wix.InstallUninstallType startValue = this.core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
                            switch (startValue)
                            {
                                case Wix.InstallUninstallType.install:
                                    events |= MsiInterop.MsidbServiceControlEventStart;
                                    break;
                                case Wix.InstallUninstallType.uninstall:
                                    events |= MsiInterop.MsidbServiceControlEventUninstallStart;
                                    break;
                                case Wix.InstallUninstallType.both:
                                    events |= MsiInterop.MsidbServiceControlEventStart | MsiInterop.MsidbServiceControlEventUninstallStart;
                                    break;
                            }
                            break;
                        case "Stop":
                            Wix.InstallUninstallType stopValue = this.core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
                            switch (stopValue)
                            {
                                case Wix.InstallUninstallType.install:
                                    events |= MsiInterop.MsidbServiceControlEventStop;
                                    break;
                                case Wix.InstallUninstallType.uninstall:
                                    events |= MsiInterop.MsidbServiceControlEventUninstallStop;
                                    break;
                                case Wix.InstallUninstallType.both:
                                    events |= MsiInterop.MsidbServiceControlEventStop | MsiInterop.MsidbServiceControlEventUninstallStop;
                                    break;
                            }
                            break;
                        case "Wait":
                            wait = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifierFromFilename(name);
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            // get the ServiceControl arguments
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ServiceArgument":
                            if (null != arguments)
                            {
                                arguments = String.Concat(arguments, "[~]");
                            }
                            arguments = String.Concat(arguments, this.core.GetTrimmedInnerText(child));
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ServiceControl", id);
                row[1] = name;
                row[2] = events;
                row[3] = arguments;
                if (YesNoType.NotSet != wait)
                {
                    row[4] = YesNoType.Yes == wait ? 1 : 0;
                }
                row[5] = componentId;
            }
        }

        /// <summary>
        /// Parses a service dependency element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Parsed sevice dependency name.</returns>
        private string ParseServiceDependencyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string dependency = null;
            bool group = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            dependency = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Group":
                            group = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == dependency)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            return group ? String.Concat("+", dependency) : dependency;
        }

        /// <summary>
        /// Parses a service install element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseServiceInstallElement(XElement node, string componentId, bool win64Component)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string account = null;
            string arguments = null;
            string dependencies = null;
            string description = null;
            string displayName = null;
            bool eraseDescription = false;
            int errorbits = 0;
            string loadOrderGroup = null;
            string name = null;
            string password = null;
            int startType = 0;
            int typebits = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Account":
                            account = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Arguments":
                            arguments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EraseDescription":
                            eraseDescription = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ErrorControl":
                            string errorControlValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < errorControlValue.Length)
                            {
                                Wix.ServiceInstall.ErrorControlType errorControlType = Wix.ServiceInstall.ParseErrorControlType(errorControlValue);
                                switch (errorControlType)
                                {
                                    case Wix.ServiceInstall.ErrorControlType.ignore:
                                        errorbits |= MsiInterop.MsidbServiceInstallErrorIgnore;
                                        break;
                                    case Wix.ServiceInstall.ErrorControlType.normal:
                                        errorbits |= MsiInterop.MsidbServiceInstallErrorNormal;
                                        break;
                                    case Wix.ServiceInstall.ErrorControlType.critical:
                                        errorbits |= MsiInterop.MsidbServiceInstallErrorCritical;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, errorControlValue, "ignore", "normal", "critical"));
                                        break;
                                }
                            }
                            break;
                        case "Interactive":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typebits |= MsiInterop.MsidbServiceInstallInteractive;
                            }
                            break;
                        case "LoadOrderGroup":
                            loadOrderGroup = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            password = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Start":
                            string startValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < startValue.Length)
                            {
                                Wix.ServiceInstall.StartType start = Wix.ServiceInstall.ParseStartType(startValue);
                                switch (start)
                                {
                                    case Wix.ServiceInstall.StartType.auto:
                                        startType = MsiInterop.MsidbServiceInstallAutoStart;
                                        break;
                                    case Wix.ServiceInstall.StartType.demand:
                                        startType = MsiInterop.MsidbServiceInstallDemandStart;
                                        break;
                                    case Wix.ServiceInstall.StartType.disabled:
                                        startType = MsiInterop.MsidbServiceInstallDisabled;
                                        break;
                                    case Wix.ServiceInstall.StartType.boot:
                                    case Wix.ServiceInstall.StartType.system:
                                        this.core.OnMessage(WixErrors.ValueNotSupported(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, startValue));
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, startValue, "auto", "demand", "disabled"));
                                        break;
                                }
                            }
                            break;
                        case "Type":
                            string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                Wix.ServiceInstall.TypeType typeType = Wix.ServiceInstall.ParseTypeType(typeValue);
                                switch (typeType)
                                {
                                    case Wix.ServiceInstall.TypeType.ownProcess:
                                        typebits |= MsiInterop.MsidbServiceInstallOwnProcess;
                                        break;
                                    case Wix.ServiceInstall.TypeType.shareProcess:
                                        typebits |= MsiInterop.MsidbServiceInstallShareProcess;
                                        break;
                                    case Wix.ServiceInstall.TypeType.kernelDriver:
                                    case Wix.ServiceInstall.TypeType.systemDriver:
                                        this.core.OnMessage(WixErrors.ValueNotSupported(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue));
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, node.Name.LocalName, typeValue, "ownProcess", "shareProcess"));
                                        break;
                                }
                            }
                            break;
                        case "Vital":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                errorbits |= MsiInterop.MsidbServiceInstallErrorControlVital;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (null == id)
            {
                id = this.core.CreateIdentifierFromFilename(name);
            }

            if (0 == startType)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Start"));
            }

            if (eraseDescription)
            {
                description = "[~]";
            }

            // get the ServiceInstall dependencies and config
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(child, id.Id, "ServiceInstall");
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(child, componentId, name);
                            break;
                        case "ServiceConfigFailureActions":
                            this.ParseServiceConfigFailureActionsElement(child, componentId, name);
                            break;
                        case "ServiceDependency":
                            dependencies = String.Concat(dependencies, this.ParseServiceDependencyElement(child), "[~]");
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ServiceInstallId", id.Id }, { "ServiceInstallName", name }, { "ServiceInstallComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }

            if (null != dependencies)
            {
                dependencies = String.Concat(dependencies, "[~]");
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ServiceInstall", id);
                row[1] = name;
                row[2] = displayName;
                row[3] = typebits;
                row[4] = startType;
                row[5] = errorbits;
                row[6] = loadOrderGroup;
                row[7] = dependencies;
                row[8] = account;
                row[9] = password;
                row[10] = arguments;
                row[11] = componentId;
                row[12] = description;
            }
        }

        /// <summary>
        /// Parses a SetDirectory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseSetDirectoryElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string actionName = null;
            string id = null;
            string condition = null;
            string[] sequences = new string[] { "InstallUISequence", "InstallExecuteSequence" }; // default to "both"
            int extraBits = 0;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            actionName = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Directory", id);
                            break;
                        case "Sequence":
                            string sequenceValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < sequenceValue.Length)
                            {
                                Wix.SequenceType sequenceType = Wix.Enums.ParseSequenceType(sequenceValue);
                                switch (sequenceType)
                                {
                                    case Wix.SequenceType.execute:
                                        sequences = new string[] { "InstallExecuteSequence" };
                                        break;
                                    case Wix.SequenceType.ui:
                                        sequences = new string[] { "InstallUISequence" };
                                        break;
                                    case Wix.SequenceType.first:
                                        extraBits = MsiInterop.MsidbCustomActionTypeFirstSequence;
                                        // default puts it in both sequence which is what we want
                                        break;
                                    case Wix.SequenceType.both:
                                        // default so no work necessary.
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, sequenceValue, "execute", "ui", "both"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.core.GetConditionInnerText(node);

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (String.IsNullOrEmpty(actionName))
            {
                actionName = String.Concat("Set", id);
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            // add the row and any references needed
            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "CustomAction");
                row[0] = actionName;
                row[1] = MsiInterop.MsidbCustomActionTypeProperty | MsiInterop.MsidbCustomActionTypeTextData | extraBits;
                row[2] = id;
                row[3] = value;

                foreach (string sequence in sequences)
                {
                    Row sequenceRow = this.core.CreateRow(sourceLineNumbers, "WixAction");
                    sequenceRow[0] = sequence;
                    sequenceRow[1] = actionName;
                    sequenceRow[2] = condition;
                    // no explicit sequence
                    // no before action
                    sequenceRow[5] = "CostInitialize";
                    sequenceRow[6] = 0; // not overridable
                }
            }
        }

        /// <summary>
        /// Parses a SetProperty element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseSetPropertyElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string actionName = null;
            string id = null;
            string afterAction = null;
            string beforeAction = null;
            string condition = null;
            string[] sequences = new string[] { "InstallUISequence", "InstallExecuteSequence" }; // default to "both"
            int extraBits = 0;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            actionName = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            afterAction = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Before":
                            beforeAction = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            string sequenceValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < sequenceValue.Length)
                            {
                                Wix.SequenceType sequenceType = Wix.Enums.ParseSequenceType(sequenceValue);
                                switch (sequenceType)
                                {
                                    case Wix.SequenceType.execute:
                                        sequences = new string[] { "InstallExecuteSequence" };
                                        break;
                                    case Wix.SequenceType.ui:
                                        sequences = new string[] { "InstallUISequence" };
                                        break;
                                    case Wix.SequenceType.first:
                                        extraBits = MsiInterop.MsidbCustomActionTypeFirstSequence;
                                        // default puts it in both sequence which is what we want
                                        break;
                                    case Wix.SequenceType.both:
                                        // default so no work necessary.
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, sequenceValue, "execute", "ui", "both"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.core.GetConditionInnerText(node);

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (String.IsNullOrEmpty(actionName))
            {
                actionName = String.Concat("Set", id);
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (null != beforeAction && null != afterAction)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "After", "Before"));
            }
            else if (null == beforeAction && null == afterAction)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "After", "Before", "Id"));
            }

            this.core.ParseForExtensionElements(node);

            // add the row and any references needed
            if (!this.core.EncounteredError)
            {
                // action that is scheduled to occur before/after itself
                if (beforeAction == actionName)
                {
                    this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(sourceLineNumbers, node.Name.LocalName, "Before", beforeAction));
                }
                else if (afterAction == actionName)
                {
                    this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(sourceLineNumbers, node.Name.LocalName, "After", afterAction));
                }

                Row row = this.core.CreateRow(sourceLineNumbers, "CustomAction");
                row[0] = actionName;
                row[1] = MsiInterop.MsidbCustomActionTypeProperty | MsiInterop.MsidbCustomActionTypeTextData | extraBits;
                row[2] = id;
                row[3] = value;

                foreach (string sequence in sequences)
                {
                    Row sequenceRow = this.core.CreateRow(sourceLineNumbers, "WixAction");
                    sequenceRow[0] = sequence;
                    sequenceRow[1] = actionName;
                    sequenceRow[2] = condition;
                    // no explicit sequence
                    sequenceRow[4] = beforeAction;
                    sequenceRow[5] = afterAction;
                    sequenceRow[6] = 0; // not overridable

                    if (null != beforeAction)
                    {
                        if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                        {
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, beforeAction);
                        }
                        else
                        {
                            this.core.CreateSimpleReference(sourceLineNumbers, "CustomAction", beforeAction);
                        }
                    }

                    if (null != afterAction)
                    {
                        if (WindowsInstallerStandard.IsStandardAction(afterAction))
                        {
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, afterAction);
                        }
                        else
                        {
                            this.core.CreateSimpleReference(sourceLineNumbers, "CustomAction", afterAction);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parses a SFP catalog element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentSFPCatalog">Parent SFPCatalog.</param>
        private void ParseSFPFileElement(XElement node, string parentSFPCatalog)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "FileSFPCatalog");
                row[0] = id;
                row[1] = parentSFPCatalog;
            }
        }

        /// <summary>
        /// Parses a SFP catalog element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentSFPCatalog">Parent SFPCatalog.</param>
        private void ParseSFPCatalogElement(XElement node, ref string parentSFPCatalog)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string parentName = null;
            string dependency = null;
            string name = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Dependency":
                            dependency = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            parentSFPCatalog = name;
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "SFPCatalog":
                            this.ParseSFPCatalogElement(child, ref parentName);
                            if (null != dependency && parentName == dependency)
                            {
                                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dependency"));
                            }
                            dependency = parentName;
                            break;
                        case "SFPFile":
                            this.ParseSFPFileElement(child, name);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null == dependency)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dependency"));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "SFPCatalog");
                row[0] = name;
                row[1] = sourceFile;
                row[2] = dependency;
            }
        }

        /// <summary>
        /// Parses a shortcut element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifer for parent component.</param>
        /// <param name="parentElementLocalName">Local name of parent element.</param>
        /// <param name="defaultTarget">Default identifier of parent (which is usually the target).</param>
        /// <param name="parentKeyPath">Flag to indicate whether the parent element is the keypath of a component or not (will only be true for file parent elements).</param>
        private void ParseShortcutElement(XElement node, string componentId, string parentElementLocalName, string defaultTarget, YesNoType parentKeyPath)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            bool advertise = false;
            string arguments = null;
            string description = null;
            string descriptionResourceDll = null;
            int descriptionResourceId = CompilerConstants.IntegerNotSet;
            string directory = null;
            string displayResourceDll = null;
            int displayResourceId = CompilerConstants.IntegerNotSet;
            int hotkey = CompilerConstants.IntegerNotSet;
            string icon = null;
            int iconIndex = CompilerConstants.IntegerNotSet;
            string name = null;
            string shortName = null;
            int show = CompilerConstants.IntegerNotSet;
            string target = null;
            string workingDirectory = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            advertise = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Arguments":
                            arguments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DescriptionResourceDll":
                            descriptionResourceDll = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DescriptionResourceId":
                            descriptionResourceId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Directory":
                            directory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "DisplayResourceDll":
                            displayResourceDll = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayResourceId":
                            displayResourceId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Hotkey":
                            hotkey = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Icon":
                            icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                            break;
                        case "IconIndex":
                            iconIndex = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "ShortName":
                            shortName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Show":
                            string showValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (showValue.Length == 0)
                            {
                                show = CompilerConstants.IllegalInteger;
                            }
                            else
                            {
                                Wix.Shortcut.ShowType showType = Wix.Shortcut.ParseShowType(showValue);
                                switch (showType)
                                {
                                    case Wix.Shortcut.ShowType.normal:
                                        show = 1;
                                        break;
                                    case Wix.Shortcut.ShowType.maximized:
                                        show = 3;
                                        break;
                                    case Wix.Shortcut.ShowType.minimized:
                                        show = 7;
                                        break;
                                    default:
                                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Show", showValue, "normal", "maximized", "minimized"));
                                        show = CompilerConstants.IllegalInteger;
                                        break;
                                }
                            }
                            break;
                        case "Target":
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WorkingDirectory":
                            workingDirectory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (advertise && null != target)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "Advertise", "yes"));
            }

            if (null == directory)
            {
                if ("Component" == parentElementLocalName)
                {
                    directory = defaultTarget;
                }
                else
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.Name.LocalName, "Directory", "Component"));
                }
            }

            if (null != descriptionResourceDll)
            {
                if (CompilerConstants.IntegerNotSet == descriptionResourceId)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DescriptionResourceDll", "DescriptionResourceId"));
                }
            }
            else
            {
                if (CompilerConstants.IntegerNotSet != descriptionResourceId)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DescriptionResourceId", "DescriptionResourceDll"));
                }
            }

            if (null != displayResourceDll)
            {
                if (CompilerConstants.IntegerNotSet == displayResourceId)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayResourceDll", "DisplayResourceId"));
                }
            }
            else
            {
                if (CompilerConstants.IntegerNotSet != displayResourceId)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayResourceId", "DisplayResourceDll"));
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.core.CreateShortName(name, true, false, node.Name.LocalName, componentId, directory);
                }
            }

            if ("Component" != parentElementLocalName && null != target)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Target", parentElementLocalName));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("sct", directory, LowercaseOrNull(name) ?? LowercaseOrNull(shortName));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Icon":
                            icon = this.ParseIconElement(child);
                            break;
                        case "ShortcutProperty":
                            this.ParseShortcutPropertyElement(child, id.Id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Shortcut", id);
                row[1] = directory;
                row[2] = GetMsiFilenameValue(shortName, name);
                row[3] = componentId;
                if (advertise)
                {
                    if (YesNoType.Yes != parentKeyPath && "Component" != parentElementLocalName)
                    {
                        this.core.OnMessage(WixWarnings.UnclearShortcut(sourceLineNumbers, id.Id, componentId, defaultTarget));
                    }
                    row[4] = Guid.Empty.ToString("B");
                }
                else if (null != target)
                {
                    row[4] = target;
                }
                else if ("Component" == parentElementLocalName || "CreateFolder" == parentElementLocalName)
                {
                    row[4] = String.Format(CultureInfo.InvariantCulture, "[{0}]", defaultTarget);
                }
                else if ("File" == parentElementLocalName)
                {
                    row[4] = String.Format(CultureInfo.InvariantCulture, "[#{0}]", defaultTarget);
                }
                row[5] = arguments;
                row[6] = description;
                if (CompilerConstants.IntegerNotSet != hotkey)
                {
                    row[7] = hotkey;
                }
                row[8] = icon;
                if (CompilerConstants.IntegerNotSet != iconIndex)
                {
                    row[9] = iconIndex;
                }

                if (CompilerConstants.IntegerNotSet != show)
                {
                    row[10] = show;
                }
                row[11] = workingDirectory;
                row[12] = displayResourceDll;
                if (CompilerConstants.IntegerNotSet != displayResourceId)
                {
                    row[13] = displayResourceId;
                }
                row[14] = descriptionResourceDll;
                if (CompilerConstants.IntegerNotSet != descriptionResourceId)
                {
                    row[15] = descriptionResourceId;
                }
            }
        }

        /// <summary>
        /// Parses a shortcut property element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseShortcutPropertyElement(XElement node, string shortcutId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(key))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }
            else if (null == id)
            {
                id = this.core.CreateIdentifier("scp", shortcutId, key.ToUpperInvariant());
            }

            string innerText = this.core.GetTrimmedInnerText(node);
            if (!String.IsNullOrEmpty(innerText))
            {
                if (String.IsNullOrEmpty(value))
                {
                    value = innerText;
                }
                else // cannot specify both the value attribute and inner text
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }

            if (String.IsNullOrEmpty(value))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiShortcutProperty", id);
                row[1] = shortcutId;
                row[2] = key;
                row[3] = value;
            }
        }

        /// <summary>
        /// Parses a typelib element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileServer">Identifier of file that acts as typelib server.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        private void ParseTypeLibElement(XElement node, string componentId, string fileServer, bool win64Component)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoType advertise = YesNoType.NotSet;
            int cost = CompilerConstants.IntegerNotSet;
            string description = null;
            int flags = 0;
            string helpDirectory = null;
            int language = CompilerConstants.IntegerNotSet;
            int majorVersion = CompilerConstants.IntegerNotSet;
            int minorVersion = CompilerConstants.IntegerNotSet;
            long resourceId = CompilerConstants.LongNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Advertise":
                            advertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Control":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 2;
                            }
                            break;
                        case "Cost":
                            cost = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HasDiskImage":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 8;
                            }
                            break;
                        case "HelpDirectory":
                            helpDirectory = this.core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 4;
                            }
                            break;
                        case "Language":
                            language = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "MajorVersion":
                            majorVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, ushort.MaxValue);
                            break;
                        case "MinorVersion":
                            minorVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
                            break;
                        case "ResourceId":
                            resourceId = this.core.GetAttributeLongValue(sourceLineNumbers, attrib, int.MinValue, int.MaxValue);
                            break;
                        case "Restricted":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 1;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (CompilerConstants.IntegerNotSet == language)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
                language = CompilerConstants.IllegalInteger;
            }

            // build up the typelib version string for the registry if the major or minor version was specified
            string registryVersion = null;
            if (CompilerConstants.IntegerNotSet != majorVersion || CompilerConstants.IntegerNotSet != minorVersion)
            {
                if (CompilerConstants.IntegerNotSet != majorVersion)
                {
                    registryVersion = majorVersion.ToString("x", CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    registryVersion = "0";
                }

                if (CompilerConstants.IntegerNotSet != minorVersion)
                {
                    registryVersion = String.Concat(registryVersion, ".", minorVersion.ToString("x", CultureInfo.InvariantCulture.NumberFormat));
                }
                else
                {
                    registryVersion = String.Concat(registryVersion, ".0");
                }
            }

            // if the advertise state has not been set, default to non-advertised
            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "AppId":
                            this.ParseAppIdElement(child, componentId, YesNoType.NotSet, fileServer, id, registryVersion);
                            break;
                        case "Class":
                            this.ParseClassElement(child, componentId, YesNoType.NotSet, fileServer, id, registryVersion, null);
                            break;
                        case "Interface":
                            this.ParseInterfaceElement(child, componentId, null, null, id, registryVersion);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (YesNoType.Yes == advertise)
            {
                if (CompilerConstants.LongNotSet != resourceId)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "ResourceId"));
                }

                if (0 != flags)
                {
                    if (0x1 == (flags & 0x1))
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Restricted", "Advertise", "yes"));
                    }

                    if (0x2 == (flags & 0x2))
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Control", "Advertise", "yes"));
                    }

                    if (0x4 == (flags & 0x4))
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Hidden", "Advertise", "yes"));
                    }

                    if (0x8 == (flags & 0x8))
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "HasDiskImage", "Advertise", "yes"));
                    }
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "TypeLib");
                    row[0] = id;
                    row[1] = language;
                    row[2] = componentId;
                    if (CompilerConstants.IntegerNotSet != majorVersion || CompilerConstants.IntegerNotSet != minorVersion)
                    {
                        row[3] = (CompilerConstants.IntegerNotSet != majorVersion ? majorVersion * 256 : 0) + (CompilerConstants.IntegerNotSet != minorVersion ? minorVersion : 0);
                    }
                    row[4] = description;
                    row[5] = helpDirectory;
                    row[6] = Guid.Empty.ToString("B");
                    if (CompilerConstants.IntegerNotSet != cost)
                    {
                        row[7] = cost;
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (CompilerConstants.IntegerNotSet != cost && CompilerConstants.IllegalInteger != cost)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Cost", "Advertise", "no"));
                }

                if (null == fileServer)
                {
                    this.core.OnMessage(WixErrors.MissingTypeLibFile(sourceLineNumbers, node.Name.LocalName, "File"));
                }

                if (null == registryVersion)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "MajorVersion", "MinorVersion", "Advertise", "no"));
                }

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion], (Default) = [Description]
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}", id, registryVersion), null, description, componentId);

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\[Language]\[win16|win32|win64], (Default) = [TypeLibPath]\[ResourceId]
                string path = String.Concat("[#", fileServer, "]");
                if (CompilerConstants.LongNotSet != resourceId)
                {
                    path = String.Concat(path, Path.DirectorySeparatorChar, resourceId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                }
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\{2}\{3}", id, registryVersion, language, (win64Component ? "win64" : "win32")), null, path, componentId);

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\FLAGS, (Default) = [TypeLibFlags]
                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\FLAGS", id, registryVersion), null, flags.ToString(CultureInfo.InvariantCulture.NumberFormat), componentId);

                if (null != helpDirectory)
                {
                    // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\HELPDIR, (Default) = [HelpDirectory]
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\HELPDIR", id, registryVersion), null, String.Concat("[", helpDirectory, "]"), componentId);
                }
            }
        }

        /// <summary>
        /// Parses an EmbeddedChaniner element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseEmbeddedChainerElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string commandLine = null;
            string condition = null;
            string source = null;
            int type = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinarySource":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "FileSource", "PropertySource"));
                            }
                            source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeBinaryData;
                            this.core.CreateSimpleReference(sourceLineNumbers, "Binary", source); // add a reference to the appropriate Binary
                            break;
                        case "CommandLine":
                            commandLine = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FileSource":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "PropertySource"));
                            }
                            source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeSourceFile;
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", source); // add a reference to the appropriate File
                            break;
                        case "PropertySource":
                            if (null != source)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "FileSource"));
                            }
                            source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeProperty;
                            // cannot add a reference to a Property because it may be created at runtime.
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Get the condition from the inner text of the element.
            condition = this.core.GetConditionInnerText(node);

            if (null == id)
            {
                id = this.core.CreateIdentifier("mec", source, type.ToString());
            }

            if (null == source)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "BinarySource", "FileSource", "PropertySource"));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiEmbeddedChainer", id);
                row[1] = condition;
                row[2] = commandLine;
                row[3] = source;
                row[4] = type;
            }
        }

        /// <summary>
        /// Parses UI elements.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseUIElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int embeddedUICount = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "BillboardAction":
                            this.ParseBillboardActionElement(child);
                            break;
                        case "ComboBox":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ComboBox"], "ListItem");
                            break;
                        case "Dialog":
                            this.ParseDialogElement(child);
                            break;
                        case "DialogRef":
                            this.ParseSimpleRefElement(child, "Dialog");
                            break;
                        case "EmbeddedUI":
                            if (0 < embeddedUICount) // there can be only one embedded UI
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            this.ParseEmbeddedUIElement(child);
                            ++embeddedUICount;
                            break;
                        case "Error":
                            this.ParseErrorElement(child);
                            break;
                        case "ListBox":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ListBox"], "ListItem");
                            break;
                        case "ListView":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ListView"], "ListItem");
                            break;
                        case "ProgressText":
                            this.ParseActionTextElement(child);
                            break;
                        case "Publish":
                            int order = 0;
                            this.ParsePublishElement(child, null, null, ref order);
                            break;
                        case "RadioButtonGroup":
                            RadioButtonType radioButtonType = this.ParseRadioButtonGroupElement(child, null, RadioButtonType.NotSet);
                            if (RadioButtonType.Bitmap == radioButtonType || RadioButtonType.Icon == radioButtonType)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.RadioButtonBitmapAndIconDisallowed(childSourceLineNumbers));
                            }
                            break;
                        case "TextStyle":
                            this.ParseTextStyleElement(child);
                            break;
                        case "UIText":
                            this.ParseUITextElement(child);
                            break;

                        // the following are available indentically under the UI and Product elements for document organization use only
                        case "AdminUISequence":
                        case "InstallUISequence":
                            this.ParseSequenceElement(child, child.Name.LocalName);
                            break;
                        case "Binary":
                            this.ParseBinaryElement(child);
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        case "PropertyRef":
                            this.ParseSimpleRefElement(child, "Property");
                            break;
                        case "UIRef":
                            this.ParseSimpleRefElement(child, "WixUI");
                            break;

                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null != id && !this.core.EncounteredError)
            {
                this.core.CreateRow(sourceLineNumbers, "WixUI", id);
            }
        }

        /// <summary>
        /// Parses a list item element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table to add row to.</param>
        /// <param name="property">Identifier of property referred to by list item.</param>
        /// <param name="order">Relative order of list items.</param>
        private void ParseListItemElement(XElement node, TableDefinition table, string property, ref int order)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string icon = null;
            string text = null;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Icon":
                            if ("ListView" == table.Name)
                            {
                                icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                                this.core.CreateSimpleReference(sourceLineNumbers, "Binary", icon);
                            }
                            else
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeExceptOnElement(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ListView"));
                            }
                            break;
                        case "Text":
                            text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, table.Name);
                row[0] = property;
                row[1] = ++order;
                row[2] = value;
                row[3] = text;
                if (null != icon)
                {
                    row[4] = icon;
                }
            }
        }

        /// <summary>
        /// Parses a radio button element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="property">Identifier of property referred to by radio button.</param>
        /// <param name="order">Relative order of radio buttons.</param>
        /// <returns>Type of this radio button.</returns>
        private RadioButtonType ParseRadioButtonElement(XElement node, string property, ref int order)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            RadioButtonType type = RadioButtonType.NotSet;
            string value = null;
            string x = null;
            string y = null;
            string width = null;
            string height = null;
            string text = null;
            string tooltip = null;
            string help = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Bitmap":
                            if (RadioButtonType.NotSet != type)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Icon", "Text"));
                            }
                            text = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                            type = RadioButtonType.Bitmap;
                            break;
                        case "Height":
                            height = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Help":
                            help = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Icon":
                            if (RadioButtonType.NotSet != type)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Text"));
                            }
                            text = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                            type = RadioButtonType.Icon;
                            break;
                        case "Text":
                            if (RadioButtonType.NotSet != type)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Icon"));
                            }
                            text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            type = RadioButtonType.Text;
                            break;
                        case "ToolTip":
                            tooltip = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Y":
                            y = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (null == x)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == width)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == height)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "RadioButton");
                row[0] = property;
                row[1] = ++order;
                row[2] = value;
                row[3] = x;
                row[4] = y;
                row[5] = width;
                row[6] = height;
                row[7] = text;
                if (null != tooltip || null != help)
                {
                    row[8] = String.Concat(tooltip, "|", help);
                }
            }

            return type;
        }

        /// <summary>
        /// Parses a billboard element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseBillboardActionElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            int order = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            action = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixAction", "InstallExecuteSequence", action);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Billboard":
                            order = order + 1;
                            this.ParseBillboardElement(child, action, order);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses a billboard element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="action">Action for the billboard.</param>
        /// <param name="order">Order of the billboard.</param>
        private void ParseBillboardElement(XElement node, string action, int order)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string feature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("bil", action, order.ToString(), feature);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Control":
                            // These are all thrown away.
                            Row lastTabRow = null;
                            string firstControl = null;
                            string defaultControl = null;
                            string cancelControl = null;

                            this.ParseControlElement(child, id.Id, this.tableDefinitions["BBControl"], ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, false);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Billboard", id);
                row[1] = feature;
                row[2] = action;
                row[3] = order;
            }
        }

        /// <summary>
        /// Parses a control group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table referred to by control group.</param>
        /// <param name="childTag">Expected child elements.</param>
        private void ParseControlGroupElement(XElement node, TableDefinition table, string childTag)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int order = 0;
            string property = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Property":
                            property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    if (childTag != child.Name.LocalName)
                    {
                        this.core.UnexpectedElement(node, child);
                    }

                    switch (child.Name.LocalName)
                    {
                        case "ListItem":
                            this.ParseListItemElement(child, table, property, ref order);
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

        }

        /// <summary>
        /// Parses a radio button control group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="property">Property associated with this radio button group.</param>
        /// <param name="groupType">Specifies the current type of radio buttons in the group.</param>
        /// <returns>The current type of radio buttons in the group.</returns>
        private RadioButtonType ParseRadioButtonGroupElement(XElement node, string property, RadioButtonType groupType)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int order = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Property":
                            property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Property", property);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RadioButton":
                            RadioButtonType type = this.ParseRadioButtonElement(child, property, ref order);
                            if (RadioButtonType.NotSet == groupType)
                            {
                                groupType = type;
                            }
                            else if (groupType != type)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.RadioButtonTypeInconsistent(childSourceLineNumbers));
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            return groupType;
        }

        /// <summary>
        /// Parses an action text element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseActionTextElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            string template = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Template":
                            template = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ActionText");
                row[0] = action;
                row[1] = Common.GetInnerText(node);
                row[2] = template;
            }
        }

        /// <summary>
        /// Parses an ui text element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseUITextElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string text = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            text = Common.GetInnerText(node);

            if (null == id)
            {
                id = this.core.CreateIdentifier("txt", text);
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "UIText", id);
                row[1] = text;
            }
        }

        /// <summary>
        /// Parses a text style element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseTextStyleElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int bits = 0;
            int color = CompilerConstants.IntegerNotSet;
            string faceName = null;
            string size = "0";

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        // RGB Values
                        case "Red":
                            int redColor = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
                            if (CompilerConstants.IllegalInteger != redColor)
                            {
                                if (CompilerConstants.IntegerNotSet == color)
                                {
                                    color = redColor;
                                }
                                else
                                {
                                    color += redColor;
                                }
                            }
                            break;
                        case "Green":
                            int greenColor = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
                            if (CompilerConstants.IllegalInteger != greenColor)
                            {
                                if (CompilerConstants.IntegerNotSet == color)
                                {
                                    color = greenColor * 256;
                                }
                                else
                                {
                                    color += greenColor * 256;
                                }
                            }
                            break;
                        case "Blue":
                            int blueColor = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
                            if (CompilerConstants.IllegalInteger != blueColor)
                            {
                                if (CompilerConstants.IntegerNotSet == color)
                                {
                                    color = blueColor * 65536;
                                }
                                else
                                {
                                    color += blueColor * 65536;
                                }
                            }
                            break;

                        // Style values
                        case "Bold":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsBold;
                            }
                            break;
                        case "Italic":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsItalic;
                            }
                            break;
                        case "Strike":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsStrike;
                            }
                            break;
                        case "Underline":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsUnderline;
                            }
                            break;

                        // Font values
                        case "FaceName":
                            faceName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            size = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.CreateIdentifier("txs", faceName, size.ToString(), color.ToString(), bits.ToString());
            }

            if (null == faceName)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "FaceName"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "TextStyle", id);
                row[1] = faceName;
                row[2] = size;
                if (0 <= color)
                {
                    row[3] = color;
                }

                if (0 < bits)
                {
                    row[4] = bits;
                }
            }
        }

        /// <summary>
        /// Parses a dialog element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseDialogElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int bits = MsiInterop.MsidbDialogAttributesVisible | MsiInterop.MsidbDialogAttributesModal | MsiInterop.MsidbDialogAttributesMinimize;
            int height = 0;
            string title = null;
            bool trackDiskSpace = false;
            int width = 0;
            int x = 50;
            int y = 50;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Height":
                            height = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Title":
                            title = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;
                        case "Y":
                            y = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;

                        case "CustomPalette":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesUseCustomPalette;
                            }
                            break;
                        case "ErrorDialog":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesError;
                            }
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesVisible;
                            }
                            break;
                        case "KeepModeless":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesKeepModeless;
                            }
                            break;
                        case "LeftScroll":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesLeftScroll;
                            }
                            break;
                        case "Modeless":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesModal;
                            }
                            break;
                        case "NoMinimize":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesMinimize;
                            }
                            break;
                        case "RightAligned":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesRightAligned;
                            }
                            break;
                        case "RightToLeft":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesRTLRO;
                            }
                            break;
                        case "SystemModal":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesSysModal;
                            }
                            break;
                        case "TrackDiskSpace":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesTrackDiskSpace;
                                trackDiskSpace = true;
                            }
                            break;

                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            Row lastTabRow = null;
            string cancelControl = null;
            string defaultControl = null;
            string firstControl = null;

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Control":
                            this.ParseControlElement(child, id.Id, this.tableDefinitions["Control"], ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, trackDiskSpace);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (null != lastTabRow && null != lastTabRow[1])
            {
                if (firstControl != lastTabRow[1].ToString())
                {
                    lastTabRow[10] = firstControl;
                }
            }

            if (null == firstControl)
            {
                this.core.OnMessage(WixErrors.NoFirstControlSpecified(sourceLineNumbers, id.Id));
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Dialog", id);
                row[1] = x;
                row[2] = y;
                row[3] = width;
                row[4] = height;
                row[5] = bits;
                row[6] = title;
                row[7] = firstControl;
                row[8] = defaultControl;
                row[9] = cancelControl;
            }
        }

        /// <summary>
        /// Parses an EmbeddedUI element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseEmbeddedUIElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            int attributes = MsiInterop.MsidbEmbeddedUI; // by default this is the primary DLL that does not support basic UI.
            int messageFilter = MsiInterop.INSTALLLOGMODE_FATALEXIT | MsiInterop.INSTALLLOGMODE_ERROR | MsiInterop.INSTALLLOGMODE_WARNING | MsiInterop.INSTALLLOGMODE_USER
                                    | MsiInterop.INSTALLLOGMODE_INFO | MsiInterop.INSTALLLOGMODE_FILESINUSE | MsiInterop.INSTALLLOGMODE_RESOLVESOURCE
                                    | MsiInterop.INSTALLLOGMODE_OUTOFDISKSPACE | MsiInterop.INSTALLLOGMODE_ACTIONSTART | MsiInterop.INSTALLLOGMODE_ACTIONDATA
                                    | MsiInterop.INSTALLLOGMODE_PROGRESS | MsiInterop.INSTALLLOGMODE_COMMONDATA | MsiInterop.INSTALLLOGMODE_INITIALIZE
                                    | MsiInterop.INSTALLLOGMODE_TERMINATE | MsiInterop.INSTALLLOGMODE_SHOWDIALOG | MsiInterop.INSTALLLOGMODE_RMFILESINUSE
                                    | MsiInterop.INSTALLLOGMODE_INSTALLSTART | MsiInterop.INSTALLLOGMODE_INSTALLEND;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "IgnoreFatalExit":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_FATALEXIT;
                            }
                            break;
                        case "IgnoreError":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ERROR;
                            }
                            break;
                        case "IgnoreWarning":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_WARNING;
                            }
                            break;
                        case "IgnoreUser":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_USER;
                            }
                            break;
                        case "IgnoreInfo":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INFO;
                            }
                            break;
                        case "IgnoreFilesInUse":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_FILESINUSE;
                            }
                            break;
                        case "IgnoreResolveSource":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_RESOLVESOURCE;
                            }
                            break;
                        case "IgnoreOutOfDiskSpace":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_OUTOFDISKSPACE;
                            }
                            break;
                        case "IgnoreActionStart":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ACTIONSTART;
                            }
                            break;
                        case "IgnoreActionData":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ACTIONDATA;
                            }
                            break;
                        case "IgnoreProgress":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_PROGRESS;
                            }
                            break;
                        case "IgnoreCommonData":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_COMMONDATA;
                            }
                            break;
                        case "IgnoreInitialize":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INITIALIZE;
                            }
                            break;
                        case "IgnoreTerminate":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_TERMINATE;
                            }
                            break;
                        case "IgnoreShowDialog":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_SHOWDIALOG;
                            }
                            break;
                        case "IgnoreRMFilesInUse":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_RMFILESINUSE;
                            }
                            break;
                        case "IgnoreInstallStart":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INSTALLSTART;
                            }
                            break;
                        case "IgnoreInstallEnd":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INSTALLEND;
                            }
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportBasicUI":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbEmbeddedHandlesBasic;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = Path.GetFileName(sourceFile);
                if (!this.core.IsValidLongFilename(name, false))
                {
                    this.core.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.core.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = id.Id;
            }

            if (!name.Contains("."))
            {
                this.core.OnMessage(WixErrors.InvalidEmbeddedUIFileName(sourceLineNumbers, name));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "EmbeddedUIResource":
                            this.ParseEmbeddedUIResourceElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiEmbeddedUI", id);
                row[1] = name;
                row[2] = attributes;
                row[3] = messageFilter;
                row[4] = sourceFile;
            }
        }

        /// <summary>
        /// Parses a embedded UI resource element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Identifier of parent EmbeddedUI element.</param>
        private void ParseEmbeddedUIResourceElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = Path.GetFileName(sourceFile);
                if (!this.core.IsValidLongFilename(name, false))
                {
                    this.core.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.core.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = id.Id;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "MsiEmbeddedUI", id);
                row[1] = name;
                row[2] = 0; // embedded UI resources always set this to 0
                row[3] = null;
                row[4] = sourceFile;
            }
        }

        /// <summary>
        /// Parses a control element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier for parent dialog.</param>
        /// <param name="table">Table control belongs in.</param>
        /// <param name="lastTabRow">Last row in the tab order.</param>
        /// <param name="firstControl">Name of the first control in the tab order.</param>
        /// <param name="defaultControl">Name of the default control.</param>
        /// <param name="cancelControl">Name of the candle control.</param>
        /// <param name="trackDiskSpace">True if the containing dialog tracks disk space.</param>
        private void ParseControlElement(XElement node, string dialog, TableDefinition table, ref Row lastTabRow, ref string firstControl, ref string defaultControl, ref string cancelControl, bool trackDiskSpace)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            BitArray bits = new BitArray(32);
            int attributes = 0;
            string checkBoxPropertyRef = null;
            string checkboxValue = null;
            string controlType = null;
            bool disabled = false;
            string height = null;
            string help = null;
            bool isCancel = false;
            bool isDefault = false;
            bool notTabbable = false;
            string property = null;
            int publishOrder = 0;
            string[] specialAttributes = null;
            string sourceFile = null;
            string text = null;
            string tooltip = null;
            RadioButtonType radioButtonsType = RadioButtonType.NotSet;
            string width = null;
            string x = null;
            string y = null;

            // The rest of the method relies on the control's Type, so we have to get that first.
            XAttribute typeAttribute = node.Attribute("Type");
            if (null == typeAttribute)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }
            else
            {
                controlType = this.core.GetAttributeValue(sourceLineNumbers, typeAttribute);
            }

            switch (controlType)
            {
                case "Billboard":
                    specialAttributes = null;
                    notTabbable = true;
                    disabled = true;

                    this.core.EnsureTable(sourceLineNumbers, "Billboard");
                    break;
                case "Bitmap":
                    specialAttributes = MsiInterop.BitmapControlAttributes;
                    notTabbable = true;
                    disabled = true;
                    break;
                case "CheckBox":
                    specialAttributes = MsiInterop.CheckboxControlAttributes;
                    break;
                case "ComboBox":
                    specialAttributes = MsiInterop.ComboboxControlAttributes;
                    break;
                case "DirectoryCombo":
                    specialAttributes = MsiInterop.VolumeControlAttributes;
                    break;
                case "DirectoryList":
                    specialAttributes = null;
                    break;
                case "Edit":
                    specialAttributes = MsiInterop.EditControlAttributes;
                    break;
                case "GroupBox":
                    specialAttributes = null;
                    notTabbable = true;
                    break;
                case "Hyperlink":
                    specialAttributes = MsiInterop.HyperlinkControlAttributes;
                    break;
                case "Icon":
                    specialAttributes = MsiInterop.IconControlAttributes;
                    notTabbable = true;
                    disabled = true;
                    break;
                case "Line":
                    specialAttributes = null;
                    notTabbable = true;
                    disabled = true;
                    break;
                case "ListBox":
                    specialAttributes = MsiInterop.ListboxControlAttributes;
                    break;
                case "ListView":
                    specialAttributes = MsiInterop.ListviewControlAttributes;
                    break;
                case "MaskedEdit":
                    specialAttributes = MsiInterop.EditControlAttributes;
                    break;
                case "PathEdit":
                    specialAttributes = MsiInterop.EditControlAttributes;
                    break;
                case "ProgressBar":
                    specialAttributes = MsiInterop.ProgressControlAttributes;
                    notTabbable = true;
                    disabled = true;
                    break;
                case "PushButton":
                    specialAttributes = MsiInterop.ButtonControlAttributes;
                    break;
                case "RadioButtonGroup":
                    specialAttributes = MsiInterop.RadioControlAttributes;
                    break;
                case "ScrollableText":
                    specialAttributes = null;
                    break;
                case "SelectionTree":
                    specialAttributes = null;
                    break;
                case "Text":
                    specialAttributes = MsiInterop.TextControlAttributes;
                    notTabbable = true;
                    break;
                case "VolumeCostList":
                    specialAttributes = MsiInterop.VolumeControlAttributes;
                    notTabbable = true;
                    break;
                case "VolumeSelectCombo":
                    specialAttributes = MsiInterop.VolumeControlAttributes;
                    break;
                default:
                    specialAttributes = null;
                    notTabbable = true;
                    break;
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Type": // already processed
                            break;
                        case "Cancel":
                            isCancel = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "CheckBoxPropertyRef":
                            checkBoxPropertyRef = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CheckBoxValue":
                            checkboxValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Default":
                            isDefault = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Height":
                            height = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Help":
                            help = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconSize":
                            string iconSizeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (null != specialAttributes)
                            {
                                if (0 < iconSizeValue.Length)
                                {
                                    Wix.Control.IconSizeType iconsSizeType = Wix.Control.ParseIconSizeType(iconSizeValue);
                                    switch (iconsSizeType)
                                    {
                                        case Wix.Control.IconSizeType.Item16:
                                            this.core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                            break;
                                        case Wix.Control.IconSizeType.Item32:
                                            this.core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                            break;
                                        case Wix.Control.IconSizeType.Item48:
                                            this.core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                            this.core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                            break;
                                        default:
                                            this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "16", "32", "48"));
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "Type"));
                            }
                            break;
                        case "Property":
                            property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TabSkip":
                            notTabbable = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Text":
                            text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ToolTip":
                            tooltip = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Y":
                            y = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            YesNoType attribValue = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.core.TrySetBitFromName(MsiInterop.CommonControlAttributes, attrib.Name.LocalName, attribValue, bits, 0))
                            {
                                if (null == specialAttributes || !this.core.TrySetBitFromName(specialAttributes, attrib.Name.LocalName, attribValue, bits, 16))
                                {
                                    this.core.UnexpectedAttribute(node, attrib);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            attributes = this.core.CreateIntegerFromBitArray(bits);

            if (disabled)
            {
                attributes |= MsiInterop.MsidbControlAttributesEnabled; // bit will be inverted when stored
            }

            if (null == height)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            if (null == width)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == x)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("ctl", dialog, x, y, height, width);
            }

            if (isCancel)
            {
                cancelControl = id.Id;
            }

            if (isDefault)
            {
                defaultControl = id.Id;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "Binary":
                            this.ParseBinaryElement(child);
                            break;
                        case "ComboBox":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ComboBox"], "ListItem");
                            break;
                        case "Condition":
                            this.ParseConditionElement(child, node.Name.LocalName, id.Id, dialog);
                            break;
                        case "ListBox":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ListBox"], "ListItem");
                            break;
                        case "ListView":
                            this.ParseControlGroupElement(child, this.tableDefinitions["ListView"], "ListItem");
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        case "Publish":
                            this.ParsePublishElement(child, dialog ?? String.Empty, id.Id, ref publishOrder);
                            break;
                        case "RadioButtonGroup":
                            radioButtonsType = this.ParseRadioButtonGroupElement(child, property, radioButtonsType);
                            break;
                        case "Subscribe":
                            this.ParseSubscribeElement(child, dialog, id.Id);
                            break;
                        case "Text":
                            foreach (XAttribute attrib in child.Attributes())
                            {
                                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                                {
                                    switch (attrib.Name.LocalName)
                                    {
                                        case "SourceFile":
                                            sourceFile = this.core.GetAttributeValue(childSourceLineNumbers, attrib);
                                            break;
                                        default:
                                            this.core.UnexpectedAttribute(child, attrib);
                                            break;
                                    }
                                }
                                else
                                {
                                    this.core.ParseExtensionAttribute(child, attrib);
                                }
                            }

                            text = Common.GetInnerText(child);
                            if (!String.IsNullOrEmpty(text) && null != sourceFile)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWithInnerText(childSourceLineNumbers, child.Name.LocalName, "SourceFile"));
                            }
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            // If the radio buttons have icons, then we need to add the icon attribute.
            switch (radioButtonsType)
            {
                case RadioButtonType.Bitmap:
                    attributes |= MsiInterop.MsidbControlAttributesBitmap;
                    break;
                case RadioButtonType.Icon:
                    attributes |= MsiInterop.MsidbControlAttributesIcon;
                    break;
                case RadioButtonType.Text:
                    // Text is the default so nothing needs to be added bits
                    break;
            }

            // If we're tracking disk space, and this is a non-FormatSize Text control, and the text attribute starts with 
            // '[' and ends with ']', add a space. It is not necessary for the whole string to be a property, just 
            // those two characters matter.
            if (trackDiskSpace && "Text" == controlType &&
                MsiInterop.MsidbControlAttributesFormatSize != (attributes & MsiInterop.MsidbControlAttributesFormatSize) &&
                null != text && text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
            {
                text = String.Concat(text, " ");
            }

            // the logic for creating control rows is a little tricky because of the way tabable controls are set
            Row row = null;
            if (!this.core.EncounteredError)
            {
                if ("CheckBox" == controlType)
                {
                    if (String.IsNullOrEmpty(property) && String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef", true));
                    }
                    else if (!String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef"));
                    }
                    else if (!String.IsNullOrEmpty(property))
                    {
                        row = this.core.CreateRow(sourceLineNumbers, "CheckBox");
                        row[0] = property;
                        row[1] = checkboxValue;
                    }
                    else
                    {
                        this.core.CreateSimpleReference(sourceLineNumbers, "CheckBox", checkBoxPropertyRef);
                    }
                }

                row = this.core.CreateRow(sourceLineNumbers, table.Name);
                row.Access = id.Access;
                row[0] = dialog;
                row[1] = id.Id;
                row[2] = controlType;
                row[3] = x;
                row[4] = y;
                row[5] = width;
                row[6] = height;
                row[7] = attributes ^ (MsiInterop.MsidbControlAttributesVisible | MsiInterop.MsidbControlAttributesEnabled);
                if ("BBControl" == table.Name)
                {
                    row[8] = text; // BBControl.Text

                    if (null != sourceFile)
                    {
                        Row wixBBControlRow = this.core.CreateRow(sourceLineNumbers, "WixBBControl");
                        wixBBControlRow.Access = id.Access;
                        wixBBControlRow[0] = dialog;
                        wixBBControlRow[1] = id.Id;
                        wixBBControlRow[2] = sourceFile;
                    }
                }
                else
                {
                    row[8] = !String.IsNullOrEmpty(property) ? property : checkBoxPropertyRef;
                    row[9] = text;
                    if (null != tooltip || null != help)
                    {
                        row[11] = String.Concat(tooltip, "|", help); // Separator is required, even if only one is non-null.
                    }

                    if (null != sourceFile)
                    {
                        Row wixControlRow = this.core.CreateRow(sourceLineNumbers, "WixControl");
                        wixControlRow.Access = id.Access;
                        wixControlRow[0] = dialog;
                        wixControlRow[1] = id.Id;
                        wixControlRow[2] = sourceFile;
                    }
                }
            }

            if (!notTabbable)
            {
                if ("BBControl" == table.Name)
                {
                    this.core.OnMessage(WixErrors.TabbableControlNotAllowedInBillboard(sourceLineNumbers, node.Name.LocalName, controlType));
                }

                if (null == firstControl)
                {
                    firstControl = id.Id;
                }

                if (null != lastTabRow)
                {
                    lastTabRow[10] = id.Id;
                }
                lastTabRow = row;
            }

            // bitmap and icon controls contain a foreign key into the binary table in the text column;
            // add a reference if the identifier of the binary entry is known during compilation
            if (("Bitmap" == controlType || "Icon" == controlType) && Common.IsIdentifier(text))
            {
                this.core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
            }
        }

        /// <summary>
        /// Parses a publish control event element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier of parent dialog.</param>
        /// <param name="control">Identifier of parent control.</param>
        /// <param name="order">Relative order of controls.</param>
        private void ParsePublishElement(XElement node, string dialog, string control, ref int order)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string argument = null;
            string condition = null;
            string controlEvent = null;
            string property = null;

            // give this control event a unique ordering
            order++;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Control":
                            if (null != control)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            control = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Dialog":
                            if (null != dialog)
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            dialog = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "Dialog", dialog);
                            break;
                        case "Event":
                            controlEvent = Compiler.UppercaseFirstChar(this.core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Order":
                            order = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 2147483647);
                            break;
                        case "Property":
                            property = String.Concat("[", this.core.GetAttributeValue(sourceLineNumbers, attrib), "]");
                            break;
                        case "Value":
                            argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.core.GetConditionInnerText(node);

            if (null == control)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Control"));
            }

            if (null == dialog)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dialog"));
            }

            if (null == controlEvent && null == property) // need to specify at least one
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }
            else if (null != controlEvent && null != property) // cannot specify both
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }

            if (null == argument)
            {
                if (null != controlEvent)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value", "Event"));
                }
                else if (null != property)
                {
                    // if this is setting a property to null, put a special value in the argument column
                    argument = "{}";
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "ControlEvent");
                row[0] = dialog;
                row[1] = control;
                row[2] = (null != controlEvent ? controlEvent : property);
                row[3] = argument;
                row[4] = condition;
                row[5] = order;
            }

            if ("DoAction" == controlEvent && null != argument)
            {
                // if we're not looking at a standard action or a formatted string then create a reference 
                // to the custom action.
                if (!WindowsInstallerStandard.IsStandardAction(argument) && !Common.ContainsProperty(argument))
                {
                    this.core.CreateSimpleReference(sourceLineNumbers, "CustomAction", argument);
                }
            }

            // if we're referring to a dialog but not through a property, add it to the references
            if (("NewDialog" == controlEvent || "SpawnDialog" == controlEvent || "SpawnWaitDialog" == controlEvent || "SelectionBrowse" == controlEvent) && Common.IsIdentifier(argument))
            {
                this.core.CreateSimpleReference(sourceLineNumbers, "Dialog", argument);
            }
        }

        /// <summary>
        /// Parses a control subscription element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier of dialog.</param>
        /// <param name="control">Identifier of control.</param>
        private void ParseSubscribeElement(XElement node, string dialog, string control)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string controlAttribute = null;
            string eventMapping = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Attribute":
                            controlAttribute = Compiler.UppercaseFirstChar(this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                            break;
                        case "Event":
                            eventMapping = Compiler.UppercaseFirstChar(this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "EventMapping");
                row[0] = dialog;
                row[1] = control;
                row[2] = eventMapping;
                row[3] = controlAttribute;
            }
        }

        /// <summary>
        /// Parses an upgrade element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseUpgradeElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // process the UpgradeVersion children here
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                    switch (child.Name.LocalName)
                    {
                        case "Property":
                            this.ParsePropertyElement(child);
                            this.core.OnMessage(WixWarnings.DeprecatedUpgradeProperty(childSourceLineNumbers));
                            break;
                        case "UpgradeVersion":
                            this.ParseUpgradeVersionElement(child, id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            // No rows created here. All row creation is done in ParseUpgradeVersionElement.
        }

        /// <summary>
        /// Parse upgrade version element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="upgradeId">Upgrade code.</param>
        private void ParseUpgradeVersionElement(XElement node, string upgradeId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string actionProperty = null;
            string language = null;
            string maximum = null;
            string minimum = null;
            int options = 256;
            string removeFeatures = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ExcludeLanguages":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesLanguagesExclusive;
                            }
                            break;
                        case "IgnoreRemoveFailure":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesIgnoreRemoveFailure;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                            }
                            break;
                        case "IncludeMinimum": // this is "yes" by default
                            if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                            }
                            break;
                        case "Language":
                            language = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minimum = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maximum = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "MigrateFeatures":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesMigrateFeatures;
                            }
                            break;
                        case "OnlyDetect":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesOnlyDetect;
                            }
                            break;
                        case "Property":
                            actionProperty = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "RemoveFeatures":
                            removeFeatures = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == actionProperty)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }
            else if (actionProperty.ToUpper(CultureInfo.InvariantCulture) != actionProperty)
            {
                this.core.OnMessage(WixErrors.SecurePropertyNotUppercase(sourceLineNumbers, node.Name.LocalName, "Property", actionProperty));
            }

            if (null == minimum && null == maximum)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Minimum", "Maximum"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "Upgrade");
                row[0] = upgradeId;
                row[1] = minimum;
                row[2] = maximum;
                row[3] = language;
                row[4] = options;
                row[5] = removeFeatures;
                row[6] = actionProperty;

                // Ensure the action property is secure.
                this.AddWixPropertyRow(sourceLineNumbers, new Identifier(actionProperty, AccessModifier.Private), false, true, false);

                // Ensure that RemoveExistingProducts is authored in InstallExecuteSequence
                // if at least one row in Upgrade table lacks the OnlyDetect attribute.
                if (0 == (options & MsiInterop.MsidbUpgradeAttributesOnlyDetect))
                {
                    this.core.CreateSimpleReference(sourceLineNumbers, "WixAction", "InstallExecuteSequence", "RemoveExistingProducts");
                }
            }
        }

        /// <summary>
        /// Parses a verb element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="extension">Extension verb is releated to.</param>
        /// <param name="progId">Optional progId for extension.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="advertise">Flag if verb is advertised.</param>
        private void ParseVerbElement(XElement node, string extension, string progId, string componentId, YesNoType advertise)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string argument = null;
            string command = null;
            int sequence = CompilerConstants.IntegerNotSet;
            string target = null;
            string targetFile = null;
            string targetProperty = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Argument":
                            argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Command":
                            command = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Target":
                            target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "TargetFile", "TargetProperty"));
                            break;
                        case "TargetFile":
                            targetFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "File", targetFile);
                            break;
                        case "TargetProperty":
                            targetProperty = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null != target && null != targetFile)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "TargetFile"));
            }

            if (null != target && null != targetProperty)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "TargetProperty"));
            }

            if (null != targetFile && null != targetProperty)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "TargetFile", "TargetProperty"));
            }

            this.core.ParseForExtensionElements(node);

            if (YesNoType.Yes == advertise)
            {
                if (null != target)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "Target"));
                }

                if (null != targetFile)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "TargetFile"));
                }

                if (null != targetProperty)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "TargetProperty"));
                }

                if (!this.core.EncounteredError)
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "Verb");
                    row[0] = extension;
                    row[1] = id;
                    if (CompilerConstants.IntegerNotSet != sequence)
                    {
                        row[2] = sequence;
                    }
                    row[3] = command;
                    row[4] = argument;
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Sequence", "Advertise", "no"));
                }

                if (null == target && null == targetFile && null == targetProperty)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "TargetFile", "TargetProperty", "Advertise", "no"));
                }

                if (null == target)
                {
                    if (null != targetFile)
                    {
                        target = String.Concat("\"[#", targetFile, "]\"");
                    }

                    if (null != targetProperty)
                    {
                        target = String.Concat("\"[", targetProperty, "]\"");
                    }
                }

                if (null != argument)
                {
                    target = String.Concat(target, " ", argument);
                }

                string prefix = (null != progId ? progId : String.Concat(".", extension));

                if (null != command)
                {
                    this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(prefix, "\\shell\\", id), String.Empty, command, componentId);
                }

                this.core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(prefix, "\\shell\\", id, "\\command"), String.Empty, target, componentId);
            }
        }


        /// <summary>
        /// Parses an ApprovedExeForElevation element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseApprovedExeForElevation(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = null;
            string valueName = null;
            YesNoType win64 = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            valueName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Win64":
                            win64 = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == key)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            BundleApprovedExeForElevationAttributes attributes = BundleApprovedExeForElevationAttributes.None;

            if (win64 == YesNoType.Yes)
            {
                attributes |= BundleApprovedExeForElevationAttributes.Win64;
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixApprovedExeForElevationRow wixApprovedExeForElevationRow = (WixApprovedExeForElevationRow)this.core.CreateRow(sourceLineNumbers, "WixApprovedExeForElevation", id);
                wixApprovedExeForElevationRow.Key = key;
                wixApprovedExeForElevationRow.ValueName = valueName;
                wixApprovedExeForElevationRow.Attributes = attributes;
            }
        }

        /// <summary>
        /// Parses a Bundle element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBundleElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string copyright = null;
            string aboutUrl = null;
            YesNoDefaultType compressed = YesNoDefaultType.Default;
            int disableModify = -1;
            YesNoType disableRemove = YesNoType.NotSet;
            string helpTelephone = null;
            string helpUrl = null;
            string manufacturer = null;
            string name = null;
            string tag = null;
            string updateUrl = null;
            string upgradeCode = null;
            string version = null;
            string condition = null;
            string parentName = null;

            string fileSystemSafeBundleName = null;
            string logVariablePrefixAndExtension = null;
            string iconSourceFile = null;
            string splashScreenSourceFile = null;

            // Process only standard attributes until the active section is initialized.
            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AboutUrl":
                            aboutUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            compressed = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Copyright":
                            copyright = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisableModify":
                            string value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (value)
                            {
                                case "button":
                                    disableModify = 2;
                                    break;
                                case "yes":
                                    disableModify = 1;
                                    break;
                                case "no":
                                    disableModify = 0;
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "button", "yes", "no"));
                                    break;
                            }
                            break;
                        case "DisableRemove":
                            disableRemove = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DisableRepair":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "HelpTelephone":
                            helpTelephone = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HelpUrl":
                            helpUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            manufacturer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconSourceFile":
                            iconSourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParentName":
                            parentName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SplashScreenSourceFile":
                            splashScreenSourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Tag":
                            tag = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpdateUrl":
                            updateUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version":
                            version = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(version))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidModuleOrBundleVersion(version))
            {
                this.core.OnMessage(WixWarnings.InvalidModuleOrBundleVersion(sourceLineNumbers, "Bundle", version));
            }

            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UpgradeCode"));
            }

            if (String.IsNullOrEmpty(copyright))
            {
                if (String.IsNullOrEmpty(manufacturer))
                {
                    copyright = "Copyright (c). All rights reserved.";
                }
                else
                {
                    copyright = String.Format("Copyright (c) {0}. All rights reserved.", manufacturer);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:Setup.log");
            }
            else
            {
                // Ensure only allowable path characters are in "name" (and change spaces to underscores).
                fileSystemSafeBundleName = CompilerCore.MakeValidLongFileName(name.Replace(' ', '_'), "_");
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:", fileSystemSafeBundleName, ".log");
            }

            this.activeName = String.IsNullOrEmpty(name) ? Common.GenerateGuid() : name;
            this.core.CreateActiveSection(this.activeName, SectionType.Bundle, 0);

            // Now that the active section is initialized, process only extension attributes.
            foreach (XAttribute attrib in node.Attributes())
            {
                if (!String.IsNullOrEmpty(attrib.Name.NamespaceName) && CompilerCore.WixNamespace != attrib.Name.Namespace)
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            bool baSeen = false;
            bool chainSeen = false;
            bool logSeen = false;

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ApprovedExeForElevation":
                            this.ParseApprovedExeForElevation(child);
                            break;
                        case "BootstrapperApplication":
                            if (baSeen)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "BootstrapperApplication"));
                            }
                            this.ParseBootstrapperApplicationElement(child);
                            baSeen = true;
                            break;
                        case "BootstrapperApplicationRef":
                            this.ParseBootstrapperApplicationRefElement(child);
                            break;
                        case "OptionalUpdateRegistration":
                            this.ParseOptionalUpdateRegistrationElement(child, manufacturer, parentName, name);
                            break;
                        case "Catalog":
                            this.ParseCatalogElement(child);
                            break;
                        case "Chain":
                            if (chainSeen)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Chain"));
                            }
                            this.ParseChainElement(child);
                            chainSeen = true;
                            break;
                        case "Container":
                            this.ParseContainerElement(child);
                            break;
                        case "ContainerRef":
                            this.ParseSimpleRefElement(child, "WixBundleContainer");
                            break;
                        case "Log":
                            if (logSeen)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Log"));
                            }
                            logVariablePrefixAndExtension = this.ParseLogElement(child, fileSystemSafeBundleName);
                            logSeen = true;
                            break;
                        case "PayloadGroup":
                            this.ParsePayloadGroupElement(child, ComplexReferenceParentType.Layout, "BundleLayoutOnlyPayloads");
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Layout, "BundleLayoutOnlyPayloads", ComplexReferenceChildType.Unknown, null);
                            break;
                        case "RelatedBundle":
                            this.ParseRelatedBundleElement(child);
                            break;
                        case "Update":
                            this.ParseUpdateElement(child);
                            break;
                        case "Variable":
                            this.ParseVariableElement(child);
                            break;
                        case "WixVariable":
                            this.ParseWixVariableElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!chainSeen)
            {
                this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Chain"));
            }

            if (!this.core.EncounteredError)
            {
                if (null != upgradeCode)
                {
                    Row relatedBundleRow = this.core.CreateRow(sourceLineNumbers, "WixRelatedBundle");
                    relatedBundleRow[0] = upgradeCode;
                    relatedBundleRow[1] = (int)Wix.RelatedBundle.ActionType.Upgrade;
                }

                WixBundleContainerRow containerRow = (WixBundleContainerRow)this.core.CreateRow(sourceLineNumbers, "WixBundleContainer");
                containerRow.Id = Compiler.BurnDefaultAttachedContainerId;
                containerRow.Name = "bundle-attached.cab";
                containerRow.Type = ContainerType.Attached;

                Row row = this.core.CreateRow(sourceLineNumbers, "WixBundle");
                row[0] = version;
                row[1] = copyright;
                row[2] = name;
                row[3] = aboutUrl;
                if (-1 != disableModify)
                {
                    row[4] = disableModify;
                }
                if (YesNoType.NotSet != disableRemove)
                {
                    row[5] = (YesNoType.Yes == disableRemove) ? 1 : 0;
                }
                // row[6] - (deprecated) "disable repair"
                row[7] = helpTelephone;
                row[8] = helpUrl;
                row[9] = manufacturer;
                row[10] = updateUrl;
                if (YesNoDefaultType.Default != compressed)
                {
                    row[11] = (YesNoDefaultType.Yes == compressed) ? 1 : 0;
                }

                row[12] = logVariablePrefixAndExtension;
                row[13] = iconSourceFile;
                row[14] = splashScreenSourceFile;
                row[15] = condition;
                row[16] = tag;
                row[17] = this.CurrentPlatform.ToString();
                row[18] = parentName;
                row[19] = upgradeCode;

                // Ensure that the bundle stores the well-known persisted values.
                WixBundleVariableRow bundleNameWellKnownVariable = (WixBundleVariableRow)this.core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                bundleNameWellKnownVariable.Id = Compiler.BURN_BUNDLE_NAME;
                bundleNameWellKnownVariable.Hidden = false;
                bundleNameWellKnownVariable.Persisted = true;

                WixBundleVariableRow bundleOriginalSourceWellKnownVariable = (WixBundleVariableRow)this.core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                bundleOriginalSourceWellKnownVariable.Id = Compiler.BURN_BUNDLE_ORIGINAL_SOURCE;
                bundleOriginalSourceWellKnownVariable.Hidden = false;
                bundleOriginalSourceWellKnownVariable.Persisted = true;

                WixBundleVariableRow bundleOriginalSourceFolderWellKnownVariable = (WixBundleVariableRow)this.core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                bundleOriginalSourceFolderWellKnownVariable.Id = Compiler.BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER;
                bundleOriginalSourceFolderWellKnownVariable.Hidden = false;
                bundleOriginalSourceFolderWellKnownVariable.Persisted = true;

                WixBundleVariableRow bundleLastUsedSourceWellKnownVariable = (WixBundleVariableRow)this.core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                bundleLastUsedSourceWellKnownVariable.Id = Compiler.BURN_BUNDLE_LAST_USED_SOURCE;
                bundleLastUsedSourceWellKnownVariable.Hidden = false;
                bundleLastUsedSourceWellKnownVariable.Persisted = true;
            }
        }

        /// <summary>
        /// Parse a Container element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private string ParseLogElement(XElement node, string fileSystemSafeBundleName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            YesNoType disableLog = YesNoType.NotSet;
            string variable = "WixBundleLog";
            string logPrefix = fileSystemSafeBundleName ?? "Setup";
            string logExtension = ".log";

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Disable":
                            disableLog = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PathVariable":
                            variable = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Prefix":
                            logPrefix = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            logExtension = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!logExtension.StartsWith(".", StringComparison.Ordinal))
            {
                logExtension = String.Concat(".", logExtension);
            }

            this.core.ParseForExtensionElements(node);

            return YesNoType.Yes == disableLog ? null : String.Concat(variable, ":", logPrefix, logExtension);
        }

        /// <summary>
        /// Parse a Catalog element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseCatalogElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.core.ParseForExtensionElements(node);

            // Create catalog row
            if (!this.core.EncounteredError)
            {
                this.CreatePayloadRow(sourceLineNumbers, id, Path.GetFileName(sourceFile), sourceFile, null, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, ComplexReferenceChildType.Unknown, null, YesNoDefaultType.Yes, YesNoType.Yes, null, null, null);

                WixBundleCatalogRow wixCatalogRow = (WixBundleCatalogRow)this.core.CreateRow(sourceLineNumbers, "WixBundleCatalog", id);
                wixCatalogRow.Payload = id.Id;
            }
        }

        /// <summary>
        /// Parse a Container element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseContainerElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string downloadUrl = null;
            string name = null;
            ContainerType type = ContainerType.Detached;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeString = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Enum.TryParse<ContainerType>(typeString, out type))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Type", typeString, "attached, detached"));
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.core.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (null == name)
            {
                name = id.Id;
            }

            if (!String.IsNullOrEmpty(downloadUrl) && ContainerType.Detached != type)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "Type", "attached"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PackageGroupRef":
                            this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.Container, id.Id);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                WixBundleContainerRow row = (WixBundleContainerRow)this.core.CreateRow(sourceLineNumbers, "WixBundleContainer", id);
                row.Name = name;
                row.Type = type;
                row.DownloadUrl = downloadUrl;
            }
        }

        /// <summary>
        /// Parse the BoostrapperApplication element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string previousId = null;
            ComplexReferenceChildType previousType = ComplexReferenceChildType.Unknown;

            // The BootstrapperApplication element acts like a Payload element so delegate to the "Payload" attribute parsing code to parse and create a Payload entry.
            id = this.ParsePayloadElementContent(node, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId, false);
            if (null != id)
            {
                previousId = id;
                previousType = ComplexReferenceChildType.Payload;
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Payload;
                            break;
                        case "PayloadGroupRef":
                            previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.PayloadGroup;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }

            if (null == previousId)
            {
                // We need *either* <Payload> or <PayloadGroupRef> or even just @SourceFile on the BA...
                // but we just say there's a missing <Payload>.
                // TODO: Is there a better message for this?
                this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Payload"));
            }

            // Add the application as an attached container and if an Id was provided add that too.
            if (!this.core.EncounteredError)
            {
                WixBundleContainerRow containerRow = (WixBundleContainerRow)this.core.CreateRow(sourceLineNumbers, "WixBundleContainer");
                containerRow.Id = Compiler.BurnUXContainerId;
                containerRow.Name = "bundle-ux.cab";
                containerRow.Type = ContainerType.Attached;

                if (!String.IsNullOrEmpty(id))
                {
                    Row row = this.core.CreateRow(sourceLineNumbers, "WixBootstrapperApplication");
                    row[0] = id;
                }
            }
        }

        /// <summary>
        /// Parse the BoostrapperApplicationRef element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string previousId = null;
            ComplexReferenceChildType previousType = ComplexReferenceChildType.Unknown;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Payload;
                            break;
                        case "PayloadGroupRef":
                            previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.PayloadGroup;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (String.IsNullOrEmpty(id))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else
            {
                this.core.CreateSimpleReference(sourceLineNumbers, "WixBootstrapperApplication", id);
            }
        }

        /// <summary>
        /// Parse the OptionalUpdateRegistration element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="defaultManufacturer">The manufacturer.</param>
        /// <param name="defaultProductFamily">The product family.</param>
        /// <param name="defaultName">The bundle name.</param>
        private void ParseOptionalUpdateRegistrationElement(XElement node, string defaultManufacturer, string defaultProductFamily, string defaultName)
        {
            const string defaultClassification = "Update";

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string manufacturer = null;
            string department = null;
            string productFamily = null;
            string name = null;
            string classification = defaultClassification;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Manufacturer":
                            manufacturer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Department":
                            department = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductFamily":
                            productFamily = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Classification":
                            classification = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(manufacturer))
            {
                if (!String.IsNullOrEmpty(defaultManufacturer))
                {
                    manufacturer = defaultManufacturer;
                }
                else
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Manufacturer", node.Parent.Name.LocalName));
                }
            }

            if (String.IsNullOrEmpty(productFamily))
            {
                if (!String.IsNullOrEmpty(defaultProductFamily))
                {
                    productFamily = defaultProductFamily;
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                if (!String.IsNullOrEmpty(defaultName))
                {
                    name = defaultName;
                }
                else
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Name", node.Parent.Name.LocalName));
                }
            }

            if (String.IsNullOrEmpty(classification))
            {
                this.core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, node.Name.LocalName, "Classification", defaultClassification));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixUpdateRegistration");
                row[0] = manufacturer;
                row[1] = department;
                row[2] = productFamily;
                row[3] = name;
                row[4] = classification;
            }
        }

        /// <summary>
        /// Parse Payload element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element. (BA or PayloadGroup)</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private string ParsePayloadElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PayloadGroup == previousType || ComplexReferenceChildType.Payload == previousType);

            string id = ParsePayloadElementContent(node, parentType, parentId, previousType, previousId, true);
            Dictionary<string, string> context = new Dictionary<string, string>();
            context["Id"] = id;

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child, context);
                }
            }

            return id;
        }

        /// <summary>
        /// Parse the attributes of the Payload element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element.</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private string ParsePayloadElementContent(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId, bool required)
        {
            Debug.Assert(ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            YesNoDefaultType compressed = YesNoDefaultType.Default;
            YesNoType enableSignatureVerification = YesNoType.No;
            Identifier id = null;
            string name = null;
            string sourceFile = null;
            string downloadUrl = null;
            Wix.RemotePayload remotePayload = null;

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            List<XAttribute> extensionAttributes = new List<XAttribute>();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            compressed = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EnableSignatureVerification":
                            enableSignatureVerification = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    extensionAttributes.Add(attrib);
                }
            }

            if (!required && null == sourceFile)
            {
                // Nothing left to do!
                return null;
            }

            if (null == id)
            {
                id = this.core.CreateIdentifier("pay", (null != sourceFile) ? sourceFile.ToUpperInvariant() : String.Empty);
            }

            // Now that the PayloadId is known, we can parse the extension attributes.
            Dictionary<string, string> context = new Dictionary<string, string>();
            context["Id"] = id.Id;

            foreach (XAttribute extensionAttribute in extensionAttributes)
            {
                this.core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            // We only handle the elements we care about.  Let caller handle other children.
            foreach (XElement child in node.Elements(CompilerCore.WixNamespace + "RemotePayload"))
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (CompilerCore.WixNamespace == node.Name.Namespace && node.Name.LocalName != "ExePackage")
                {
                    this.core.OnMessage(WixErrors.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (null != sourceFile && null != remotePayload)
            {
                this.core.OnMessage(WixErrors.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (null == sourceFile && null == remotePayload)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributeOrElement(sourceLineNumbers, node.Name.LocalName, "SourceFile", "RemotePayload"));
            }
            else if (null == sourceFile)
            {
                sourceFile = String.Empty;
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (Compiler.BurnUXContainerId == parentId)
            {
                if (compressed == YesNoDefaultType.No)
                {
                    core.OnMessage(WixWarnings.UxPayloadsOnlySupportEmbedding(sourceLineNumbers, sourceFile));
                }

                compressed = YesNoDefaultType.Yes;
            }

            this.CreatePayloadRow(sourceLineNumbers, id, name, sourceFile, downloadUrl, parentType, parentId, previousType, previousId, compressed, enableSignatureVerification, null, null, remotePayload);

            return id.Id;
        }

        private Wix.RemotePayload ParseRemotePayloadElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Wix.RemotePayload remotePayload = new Wix.RemotePayload();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CertificatePublicKey":
                            remotePayload.CertificatePublicKey = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CertificateThumbprint":
                            remotePayload.CertificateThumbprint = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            remotePayload.Description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Hash":
                            remotePayload.Hash = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductName":
                            remotePayload.ProductName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            remotePayload.Size = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Version":
                            remotePayload.Version = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(remotePayload.ProductName))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductName"));
            }

            if (String.IsNullOrEmpty(remotePayload.Description))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (String.IsNullOrEmpty(remotePayload.Hash))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Hash"));
            }

            if (0 == remotePayload.Size)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Size"));
            }

            if (String.IsNullOrEmpty(remotePayload.Version))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            return remotePayload;
        }

        /// <summary>
        /// Creates the row for a Payload.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private WixBundlePayloadRow CreatePayloadRow(SourceLineNumber sourceLineNumbers, Identifier id, string name, string sourceFile, string downloadUrl, ComplexReferenceParentType parentType,
            string parentId, ComplexReferenceChildType previousType, string previousId, YesNoDefaultType compressed, YesNoType enableSignatureVerification, string displayName, string description,
            Wix.RemotePayload remotePayload)
        {
            WixBundlePayloadRow row = null;

            if (!this.core.EncounteredError)
            {
                row = (WixBundlePayloadRow)this.core.CreateRow(sourceLineNumbers, "WixBundlePayload", id);
                row.Name = String.IsNullOrEmpty(name) ? Path.GetFileName(sourceFile) : name;
                row.SourceFile = sourceFile;
                row.DownloadUrl = downloadUrl;
                row.Compressed = compressed;
                row.UnresolvedSourceFile = sourceFile; // duplicate of sourceFile but in a string column so it won't get resolved to a full path during binding.
                row.DisplayName = displayName;
                row.Description = description;
                row.EnableSignatureValidation = (YesNoType.Yes == enableSignatureVerification);

                if (null != remotePayload)
                {
                    row.Description = remotePayload.Description;
                    row.DisplayName = remotePayload.ProductName;
                    row.Hash = remotePayload.Hash;
                    row.PublicKey = remotePayload.CertificatePublicKey;
                    row.Thumbprint = remotePayload.CertificateThumbprint;
                    row.FileSize = remotePayload.Size;
                    row.Version = remotePayload.Version;
                }

                this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Payload, id.Id, previousType, previousId);
            }

            return row;
        }

        /// <summary>
        /// Parse PayloadGroup element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Optional ComplexReferenceParentType of parent element. (typically another PayloadGroup)</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private void ParsePayloadGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            Debug.Assert(ComplexReferenceParentType.Unknown == parentType || ComplexReferenceParentType.Layout == parentType || ComplexReferenceParentType.PayloadGroup == parentType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            ComplexReferenceChildType previousType = ComplexReferenceChildType.Unknown;
            string previousId = null;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.PayloadGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Payload;
                            break;
                        case "PayloadGroupRef":
                            previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.PayloadGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.PayloadGroup;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                this.core.CreateRow(sourceLineNumbers, "WixBundlePayloadGroup", id);

                this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PayloadGroup, id.Id, ComplexReferenceChildType.Unknown, null);
            }
        }

        /// <summary>
        /// Parses a payload group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (BA or PayloadGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private string ParsePayloadGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.Layout == parentType || ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PayloadGroup == previousType || ComplexReferenceChildType.Payload == previousType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixBundlePayloadGroup", id);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PayloadGroup, id, previousType, previousId);

            return id;
        }

        /// <summary>
        /// Creates group and ordering information.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="type">Type of this item.</param>
        /// <param name="id">Identifier for this item.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        private void CreateGroupAndOrderingRows(SourceLineNumber sourceLineNumbers,
            ComplexReferenceParentType parentType, string parentId,
            ComplexReferenceChildType type, string id,
            ComplexReferenceChildType previousType, string previousId)
        {
            if (ComplexReferenceParentType.Unknown != parentType && null != parentId)
            {
                this.core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, type, id);
            }

            if (ComplexReferenceChildType.Unknown != previousType && null != previousId)
            {
                this.CreateWixOrderingRow(sourceLineNumbers, type, id, previousType, previousId);
            }
        }

        /// <summary>
        /// Parse ExitCode element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseExitCodeElement(XElement node, string packageId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int value = CompilerConstants.IntegerNotSet;
            ExitCodeBehaviorType behavior = ExitCodeBehaviorType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            value = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "Behavior":
                            string behaviorString = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Enum.TryParse<ExitCodeBehaviorType>(behaviorString, true, out behavior))
                            {
                                this.core.OnMessage(WixErrors.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Behavior", behaviorString, "success, error, scheduleReboot, forceReboot"));
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (ExitCodeBehaviorType.NotSet == behavior)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Behavior"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixBundlePackageExitCodeRow row = (WixBundlePackageExitCodeRow)this.core.CreateRow(sourceLineNumbers, "WixBundlePackageExitCode");
                row.ChainPackageId = packageId;
                row.Code = value;
                row.Behavior = behavior;
            }
        }

        /// <summary>
        /// Parse Chain element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseChainElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            WixChainAttributes attributes = WixChainAttributes.None;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "DisableRollback":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableRollback;
                            }
                            break;
                        case "DisableSystemRestore":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableSystemRestore;
                            }
                            break;
                        case "ParallelCache":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.ParallelCache;
                            }
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Ensure there is always a rollback boundary at the beginning of the chain.
            this.CreateRollbackBoundary(sourceLineNumbers, new Identifier("WixDefaultBoundary", AccessModifier.Public), YesNoType.Yes, YesNoType.No, ComplexReferenceParentType.PackageGroup, "WixChain", ComplexReferenceChildType.Unknown, null);

            string previousId = "WixDefaultBoundary";
            ComplexReferenceChildType previousType = ComplexReferenceChildType.Package;

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MsiPackage":
                            previousId = this.ParseMsiPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MspPackage":
                            previousId = this.ParseMspPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MsuPackage":
                            previousId = this.ParseMsuPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "ExePackage":
                            previousId = this.ParseExePackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "RollbackBoundary":
                            previousId = this.ParseRollbackBoundaryElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "PackageGroupRef":
                            previousId = this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                            previousType = ComplexReferenceChildType.PackageGroup;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (null == previousId)
            {
                this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "MsiPackage", "ExePackage", "PackageGroupRef"));
            }

            if (!this.core.EncounteredError)
            {
                WixChainRow row = (WixChainRow)this.core.CreateRow(sourceLineNumbers, "WixChain");
                row.Attributes = attributes;
            }
        }

        /// <summary>
        /// Parse MsiPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMsiPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return ParseChainPackage(node, WixBundlePackageType.Msi, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse MspPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMspPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return ParseChainPackage(node, WixBundlePackageType.Msp, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse MsuPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMsuPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return ParseChainPackage(node, WixBundlePackageType.Msu, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse ExePackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseExePackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return ParseChainPackage(node, WixBundlePackageType.Exe, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse RollbackBoundary element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseRollbackBoundaryElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PackageGroup == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            YesNoType vital = YesNoType.Yes;
            YesNoType transaction = YesNoType.No;

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            List<XAttribute> extensionAttributes = new List<XAttribute>();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    bool allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Vital":
                            vital = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Transaction":
                            transaction = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    // Save the extension attributes for later...
                    extensionAttributes.Add(attrib);
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(previousId))
                {
                    id = this.core.CreateIdentifier("rba", previousId);
                }

                if (null == id)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.core.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }

            // Now that the rollback identifier is known, we can parse the extension attributes...
            Dictionary<string, string> contextValues = new Dictionary<string, string>();
            contextValues["RollbackBoundaryId"] = id.Id;
            foreach (XAttribute attribute in extensionAttributes)
            {
                this.core.ParseExtensionAttribute(node, attribute, contextValues);
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                this.CreateRollbackBoundary(sourceLineNumbers, id, vital, transaction, parentType, parentId, previousType, previousId);
            }

            return id.Id;
        }

        /// <summary>
        /// Parses one of the ChainPackage elements
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageType">Type of package to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        /// <remarks>This method contains the shared logic for parsing all of the ChainPackage
        /// types, as there is more in common between them than different.</remarks>
        private string ParseChainPackage(XElement node, WixBundlePackageType packageType, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PackageGroup == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            string sourceFile = null;
            string downloadUrl = null;
            string after = null;
            string installCondition = null;
            YesNoAlwaysType cache = YesNoAlwaysType.Yes; // the default is to cache everything in tradeoff for stability over disk space.
            string cacheId = null;
            string description = null;
            string displayName = null;
            string logPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            string rollbackPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            YesNoType permanent = YesNoType.NotSet;
            YesNoType visible = YesNoType.NotSet;
            YesNoType vital = YesNoType.Yes;
            string installCommand = null;
            string repairCommand = null;
            YesNoType repairable = YesNoType.NotSet;
            string uninstallCommand = null;
            YesNoDefaultType perMachine = YesNoDefaultType.NotSet;
            string detectCondition = null;
            string protocol = null;
            int installSize = CompilerConstants.IntegerNotSet;
            string msuKB = null;
            YesNoType suppressLooseFilePayloadGeneration = YesNoType.NotSet;
            YesNoType enableSignatureVerification = YesNoType.No;
            YesNoDefaultType compressed = YesNoDefaultType.Default;
            YesNoType displayInternalUI = YesNoType.NotSet;
            YesNoType enableFeatureSelection = YesNoType.NotSet;
            YesNoType forcePerMachine = YesNoType.NotSet;
            Wix.RemotePayload remotePayload = null;
            YesNoType slipstream = YesNoType.NotSet;

            string[] expectedNetFx4Args = new string[] { "/q", "/norestart", "/chainingpackage" };

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            List<XAttribute> extensionAttributes = new List<XAttribute>();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    bool allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            if (!this.core.IsValidLongFilename(name, false, true))
                            {
                                this.core.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Name", name));
                            }
                            break;
                        case "SourceFile":
                            sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallCondition":
                            installCondition = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Cache":
                            cache = this.core.GetAttributeYesNoAlwaysValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheId":
                            cacheId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayInternalUI":
                            displayInternalUI = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi || packageType == WixBundlePackageType.Msp);
                            break;
                        case "EnableFeatureSelection":
                            enableFeatureSelection = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "ForcePerMachine":
                            forcePerMachine = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "LogPathVariable":
                            logPathVariable = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "RollbackLogPathVariable":
                            rollbackPathVariable = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Permanent":
                            permanent = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Visible":
                            visible = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "Vital":
                            vital = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallCommand":
                            installCommand = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "RepairCommand":
                            repairCommand = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            repairable = YesNoType.Yes;
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "UninstallCommand":
                            uninstallCommand = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "PerMachine":
                            perMachine = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msp);
                            break;
                        case "DetectCondition":
                            detectCondition = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msu);
                            break;
                        case "Protocol":
                            protocol = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "InstallSize":
                            installSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "KB":
                            msuKB = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msu);
                            break;
                        case "Compressed":
                            compressed = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressLooseFilePayloadGeneration":
                            this.core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            suppressLooseFilePayloadGeneration = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "EnableSignatureVerification":
                            enableSignatureVerification = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Slipstream":
                            slipstream = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msp);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    // Save the extension attributes for later...
                    extensionAttributes.Add(attrib);
                }
            }

            // We need to handle RemotePayload up front because it effects value of sourceFile which is used in Id generation.  Id is needed by other child elements.
            foreach (XElement child in node.Elements(CompilerCore.WixNamespace + "RemotePayload"))
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (CompilerCore.WixNamespace == node.Name.Namespace && node.Name.LocalName != "ExePackage" && node.Name.LocalName != "MsuPackage")
                {
                    this.core.OnMessage(WixErrors.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.core.OnMessage(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile"));
                }
                else if (null == remotePayload)
                {
                    sourceFile = Path.Combine("SourceDir", name);
                }
                else
                {
                    sourceFile = String.Empty;  // SourceFile is required it cannot be null.
                }
            }
            else if (null != remotePayload)
            {
                this.core.OnMessage(WixErrors.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (sourceFile.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile", sourceFile));
                }
                else
                {
                    sourceFile = Path.Combine(sourceFile, Path.GetFileName(name));
                }
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.core.OnMessage(WixErrors.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (YesNoDefaultType.No != compressed && null != remotePayload)
            {
                compressed = YesNoDefaultType.No;
                this.core.OnMessage(WixWarnings.RemotePayloadsMustNotAlsoBeCompressed(sourceLineNumbers, node.Name.LocalName));
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.core.CreateIdentifierFromFilename(Path.GetFileName(name));
                }
                else if (!String.IsNullOrEmpty(sourceFile))
                {
                    id = this.core.CreateIdentifierFromFilename(Path.GetFileName(sourceFile));
                }

                if (null == id)
                {
                    this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.core.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }

            if (null == logPathVariable)
            {
                logPathVariable = String.Concat("WixBundleLog_", id.Id);
            }

            if (null == rollbackPathVariable)
            {
                rollbackPathVariable = String.Concat("WixBundleRollbackLog_", id.Id);
            }

            if (!String.IsNullOrEmpty(protocol) && !protocol.Equals("burn", StringComparison.Ordinal) && !protocol.Equals("netfx4", StringComparison.Ordinal) && !protocol.Equals("none", StringComparison.Ordinal))
            {
                this.core.OnMessage(WixErrors.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Protocol", protocol, "none, burn, netfx4"));
            }

            if (!String.IsNullOrEmpty(protocol) && protocol.Equals("netfx4", StringComparison.Ordinal))
            {
                foreach (string expectedArgument in expectedNetFx4Args)
                {
                    if (null == installCommand || -1 == installCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.core.OnMessage(WixWarnings.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "InstallCommand", installCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (null == repairCommand || -1 == repairCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.core.OnMessage(WixWarnings.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "RepairCommand", repairCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (null == uninstallCommand || -1 == uninstallCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.core.OnMessage(WixWarnings.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "UninstallCommand", uninstallCommand, expectedArgument, "Protocol", "netfx4"));
                    }
                }
            }

            // Only set default scope for EXEs and MSPs if not already set.
            if ((WixBundlePackageType.Exe == packageType || WixBundlePackageType.Msp == packageType) && YesNoDefaultType.NotSet == perMachine)
            {
                perMachine = YesNoDefaultType.Default;
            }

            // Now that the package ID is known, we can parse the extension attributes...
            Dictionary<string, string> contextValues = new Dictionary<string, string>() { { "PackageId", id.Id } };
            foreach (XAttribute attribute in extensionAttributes)
            {
                this.core.ParseExtensionAttribute(node, attribute, contextValues);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    bool allowed = true;
                    switch (child.Name.LocalName)
                    {
                        case "SlipstreamMsp":
                            allowed = (packageType == WixBundlePackageType.Msi);
                            if (allowed)
                            {
                                this.ParseSlipstreamMspElement(child, id.Id);
                            }
                            break;
                        case "MsiProperty":
                            allowed = (packageType == WixBundlePackageType.Msi || packageType == WixBundlePackageType.Msp);
                            if (allowed)
                            {
                                this.ParseMsiPropertyElement(child, id.Id);
                            }
                            break;
                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.Package, id.Id, ComplexReferenceChildType.Unknown, null);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Package, id.Id, ComplexReferenceChildType.Unknown, null);
                            break;
                        case "ExitCode":
                            allowed = (packageType == WixBundlePackageType.Exe);
                            if (allowed)
                            {
                                this.ParseExitCodeElement(child, id.Id);
                            }
                            break;
                        case "CommandLine":
                            allowed = (packageType == WixBundlePackageType.Exe);
                            if (allowed)
                            {
                                this.ParseCommandLineElement(child, id.Id);
                            }
                            break;
                        case "RemotePayload":
                            // Handled previously
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.core.UnexpectedElement(node, child);
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "Id", id.Id } };
                    this.core.ParseExtensionElement(node, child, context);
                }
            }

            if (!this.core.EncounteredError)
            {
                // We create the package contents as a payload with this package as the parent
                this.CreatePayloadRow(sourceLineNumbers, id, name, sourceFile, downloadUrl, ComplexReferenceParentType.Package, id.Id,
                    ComplexReferenceChildType.Unknown, null, compressed, enableSignatureVerification, displayName, description, remotePayload);

                WixChainItemRow chainItemRow = (WixChainItemRow)this.core.CreateRow(sourceLineNumbers, "WixChainItem", id);

                WixBundlePackageAttributes attributes = 0;
                attributes |= (YesNoType.Yes == permanent) ? WixBundlePackageAttributes.Permanent : 0;
                attributes |= (YesNoType.Yes == visible) ? WixBundlePackageAttributes.Visible : 0;

                WixBundlePackageRow chainPackageRow = (WixBundlePackageRow)this.core.CreateRow(sourceLineNumbers, "WixBundlePackage", id);
                chainPackageRow.Type = packageType;
                chainPackageRow.PackagePayload = id.Id;
                chainPackageRow.Attributes = attributes;

                chainPackageRow.InstallCondition = installCondition;

                if (YesNoAlwaysType.NotSet != cache)
                {
                    chainPackageRow.Cache = cache;
                }

                chainPackageRow.CacheId = cacheId;

                if (YesNoType.NotSet != vital)
                {
                    chainPackageRow.Vital = vital;
                }

                if (YesNoDefaultType.NotSet != perMachine)
                {
                    chainPackageRow.PerMachine = perMachine;
                }

                chainPackageRow.LogPathVariable = logPathVariable;
                chainPackageRow.RollbackLogPathVariable = rollbackPathVariable;

                if (CompilerConstants.IntegerNotSet != installSize)
                {
                    chainPackageRow.InstallSize = installSize;
                }

                switch (packageType)
                {
                    case WixBundlePackageType.Exe:
                        WixBundleExePackageAttributes exeAttributes = 0;
                        exeAttributes |= (YesNoType.Yes == repairable) ? WixBundleExePackageAttributes.Repairable : 0;

                        WixBundleExePackageRow exeRow = (WixBundleExePackageRow)this.core.CreateRow(sourceLineNumbers, "WixBundleExePackage", id);
                        exeRow.Attributes = exeAttributes;
                        exeRow.DetectCondition = detectCondition;
                        exeRow.InstallCommand = installCommand;
                        exeRow.RepairCommand = repairCommand;
                        exeRow.UninstallCommand = uninstallCommand;
                        exeRow.ExeProtocol = protocol;
                        break;

                    case WixBundlePackageType.Msi:
                        WixBundleMsiPackageAttributes msiAttributes = 0;
                        msiAttributes |= (YesNoType.Yes == displayInternalUI) ? WixBundleMsiPackageAttributes.DisplayInternalUI : 0;
                        msiAttributes |= (YesNoType.Yes == enableFeatureSelection) ? WixBundleMsiPackageAttributes.EnableFeatureSelection : 0;
                        msiAttributes |= (YesNoType.Yes == forcePerMachine) ? WixBundleMsiPackageAttributes.ForcePerMachine : 0;
                        msiAttributes |= (YesNoType.Yes == suppressLooseFilePayloadGeneration) ? WixBundleMsiPackageAttributes.SuppressLooseFilePayloadGeneration : 0;

                        WixBundleMsiPackageRow msiRow = (WixBundleMsiPackageRow)this.core.CreateRow(sourceLineNumbers, "WixBundleMsiPackage", id);
                        msiRow.Attributes = msiAttributes;
                        break;

                    case WixBundlePackageType.Msp:
                        WixBundleMspPackageAttributes mspAttributes = 0;
                        mspAttributes |= (YesNoType.Yes == displayInternalUI) ? WixBundleMspPackageAttributes.DisplayInternalUI : 0;
                        mspAttributes |= (YesNoType.Yes == slipstream) ? WixBundleMspPackageAttributes.Slipstream : 0;

                        WixBundleMspPackageRow mspRow = (WixBundleMspPackageRow)this.core.CreateRow(sourceLineNumbers, "WixBundleMspPackage", id);
                        mspRow.Attributes = mspAttributes;
                        break;

                    case WixBundlePackageType.Msu:
                        WixBundleMsuPackageRow msuRow = (WixBundleMsuPackageRow)this.core.CreateRow(sourceLineNumbers, "WixBundleMsuPackage", id);
                        msuRow.DetectCondition = detectCondition;
                        msuRow.MsuKB = msuKB;
                        break;
                }

                this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Package, id.Id, previousType, previousId, after);
            }

            return id.Id;
        }

        /// <summary>
        /// Parse CommandLine element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseCommandLineElement(XElement node, string packageId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string installArgument = null;
            string uninstallArgument = null;
            string repairArgument = null;
            string condition = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "InstallArgument":
                            installArgument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UninstallArgument":
                            uninstallArgument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RepairArgument":
                            repairArgument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(condition))
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Condition"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixBundlePackageCommandLineRow row = (WixBundlePackageCommandLineRow)this.core.CreateRow(sourceLineNumbers, "WixBundlePackageCommandLine");
                row.ChainPackageId = packageId;
                row.InstallArgument = installArgument;
                row.UninstallArgument = uninstallArgument;
                row.RepairArgument = repairArgument;
                row.Condition = condition;
            }
        }

        /// <summary>
        /// Parse PackageGroup element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParsePackageGroupElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            ComplexReferenceChildType previousType = ComplexReferenceChildType.Unknown;
            string previousId = null;
            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MsiPackage":
                            previousId = this.ParseMsiPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MspPackage":
                            previousId = this.ParseMspPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MsuPackage":
                            previousId = this.ParseMsuPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "ExePackage":
                            previousId = this.ParseExePackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "RollbackBoundary":
                            previousId = this.ParseRollbackBoundaryElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "PackageGroupRef":
                            previousId = this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.PackageGroup;
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }


            if (!this.core.EncounteredError)
            {
                this.core.CreateRow(sourceLineNumbers, "WixBundlePackageGroup", id);
            }
        }

        /// <summary>
        /// Parses a package group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (Unknown or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <returns>Identifier for package group element.</rereturns>
        private string ParsePackageGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            return this.ParsePackageGroupRefElement(node, parentType, parentId, ComplexReferenceChildType.Unknown, null);
        }

        /// <summary>
        /// Parses a package group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (Unknown or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <param name="parentType">ComplexReferenceParentType of previous element (Unknown, Package, or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <returns>Identifier for package group element.</rereturns>
        private string ParsePackageGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.Unknown == parentType || ComplexReferenceParentType.PackageGroup == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string after = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixBundlePackageGroup", id);
                            break;
                        case "After":
                            after = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);

                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null != after && ComplexReferenceParentType.Container == parentType)
            {
                this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "After", parentId));
            }

            this.core.ParseForExtensionElements(node);

            if (ComplexReferenceParentType.Container == parentType)
            {
                this.core.CreateWixGroupRow(sourceLineNumbers, ComplexReferenceParentType.Container, parentId, ComplexReferenceChildType.PackageGroup, id);
            }
            else
            {
                this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PackageGroup, id, previousType, previousId, after);
            }

            return id;
        }

        /// <summary>
        /// Creates rollback boundary.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="id">Identifier for the rollback boundary.</param>
        /// <param name="vital">Indicates whether the rollback boundary is vital or not.</param>
        /// <param name="parentType">Type of parent group.</param>
        /// <param name="parentId">Identifier of parent group.</param>
        /// <param name="previousType">Type of previous item, if any.</param>
        /// <param name="previousId">Identifier of previous item, if any.</param>
        private void CreateRollbackBoundary(SourceLineNumber sourceLineNumbers, Identifier id, YesNoType vital, YesNoType transaction, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            WixChainItemRow row = (WixChainItemRow)this.core.CreateRow(sourceLineNumbers, "WixChainItem", id);

            WixBundleRollbackBoundaryRow rollbackBoundary = (WixBundleRollbackBoundaryRow)this.core.CreateRow(sourceLineNumbers, "WixBundleRollbackBoundary", id);

            if (YesNoType.NotSet != vital)
            {
                rollbackBoundary.Vital = vital;
            }
            if (YesNoType.NotSet != transaction)
            {
                rollbackBoundary.Transaction = transaction;
            }

            this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Package, id.Id, previousType, previousId, null);
        }

        /// <summary>
        /// Creates group and ordering information for packages
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="type">Type of this item.</param>
        /// <param name="id">Identifier for this item.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <param name="afterId">Identifier of explicit 'After' attribute, if given.</param>
        private void CreateChainPackageMetaRows(SourceLineNumber sourceLineNumbers,
            ComplexReferenceParentType parentType, string parentId,
            ComplexReferenceChildType type, string id,
            ComplexReferenceChildType previousType, string previousId, string afterId)
        {
            // If there's an explicit 'After' attribute, it overrides the inferred previous item.
            if (null != afterId)
            {
                previousType = ComplexReferenceChildType.Package;
                previousId = afterId;
            }

            this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, type, id, previousType, previousId);
        }

        // TODO: Should we define our own enum for this, just to ensure there's no "cross-contamination"?
        // TODO: Also, we could potentially include an 'Attributes' field to track things like
        // 'before' vs. 'after', and explicit vs. inferred dependencies.
        private void CreateWixOrderingRow(SourceLineNumber sourceLineNumbers,
            ComplexReferenceChildType itemType, string itemId,
            ComplexReferenceChildType dependsOnType, string dependsOnId)
        {
            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixOrdering");
                row[0] = itemType.ToString();
                row[1] = itemId;
                row[2] = dependsOnType.ToString();
                row[3] = dependsOnId;
            }
        }

        /// <summary>
        /// Parse MsiProperty element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseMsiPropertyElement(XElement node, string packageId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string value = null;
            string condition = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.core.GetAttributeMsiPropertyNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixBundleMsiPropertyRow row = (WixBundleMsiPropertyRow)this.core.CreateRow(sourceLineNumbers, "WixBundleMsiProperty");
                row.ChainPackageId = packageId;
                row.Name = name;
                row.Value = value;

                if (!String.IsNullOrEmpty(condition))
                {
                    row.Condition = condition;
                }
            }
        }

        /// <summary>
        /// Parse SlipstreamMsp element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseSlipstreamMspElement(XElement node, string packageId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.core.CreateSimpleReference(sourceLineNumbers, "WixBundlePackage", id);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixBundleSlipstreamMspRow row = (WixBundleSlipstreamMspRow)this.core.CreateRow(sourceLineNumbers, "WixBundleSlipstreamMsp");
                row.ChainPackageId = packageId;
                row.MspPackageId = id;
            }
        }

        /// <summary>
        /// Parse RelatedBundle element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseRelatedBundleElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string action = null;
            Wix.RelatedBundle.ActionType actionType = Wix.RelatedBundle.ActionType.Detect;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Action":
                            action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!String.IsNullOrEmpty(action))
            {
                actionType = Wix.RelatedBundle.ParseActionType(action);
                switch (actionType)
                {
                    case Wix.RelatedBundle.ActionType.Detect:
                        break;
                    case Wix.RelatedBundle.ActionType.Upgrade:
                        break;
                    case Wix.RelatedBundle.ActionType.Addon:
                        break;
                    case Wix.RelatedBundle.ActionType.Patch:
                        break;
                    default:
                        this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", action, "Detect", "Upgrade", "Addon", "Patch"));
                        break;
                }
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixRelatedBundle");
                row[0] = id;
                row[1] = (int)actionType;
            }
        }

        /// <summary>
        /// Parse Update element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseUpdateElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string location = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Location":
                            location = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == location)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Location"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                Row row = this.core.CreateRow(sourceLineNumbers, "WixBundleUpdate");
                row[0] = location;
            }
        }

        /// <summary>
        /// Parse Variable element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseVariableElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool hidden = false;
            string name = null;
            bool persisted = false;
            string value = null;
            string type = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Hidden":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                hidden = true;
                            }
                            break;
                        case "Name":
                            name = this.core.GetAttributeBundleVariableValue(sourceLineNumbers, attrib);
                            break;
                        case "Persisted":
                            if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                persisted = true;
                            }
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Type":
                            type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (name.StartsWith("Wix", StringComparison.OrdinalIgnoreCase))
            {
                this.core.OnMessage(WixErrors.ReservedNamespaceViolation(sourceLineNumbers, node.Name.LocalName, "Name", "Wix"));
            }

            if (null == type && null != value)
            {
                // Infer the type from the current value... 
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    // Version constructor does not support simple "v#" syntax so check to see if the value is
                    // non-negative real quick.
                    Int32 number;
                    if (Int32.TryParse(value.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out number))
                    {
                        type = "version";
                    }
                    else
                    {
                        // Sadly, Version doesn't have a TryParse() method until .NET 4, so we have to try/catch to see if it parses.
                        try
                        {
                            Version version = new Version(value.Substring(1));
                            type = "version";
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                // Not a version, check for numeric.
                if (null == type)
                {
                    Int64 number;
                    if (Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out number))
                    {
                        type = "numeric";
                    }
                    else
                    {
                        type = "string";
                    }
                }
            }

            if (null == value && null != type)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, "Variable", "Value", "Type"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixBundleVariableRow row = (WixBundleVariableRow)this.core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                row.Id = name;
                row.Value = value;
                row.Type = type;
                row.Hidden = hidden;
                row.Persisted = persisted;
            }
        }



        /// <summary>
        /// Parses a Wix element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string requiredVersion = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "RequiredVersion":
                            requiredVersion = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != requiredVersion)
            {
                this.core.VerifyRequiredVersion(sourceLineNumbers, requiredVersion);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Bundle":
                            this.ParseBundleElement(child);
                            break;
                        case "Fragment":
                            this.ParseFragmentElement(child);
                            break;
                        case "Module":
                            this.ParseModuleElement(child);
                            break;
                        case "PatchCreation":
                            this.ParsePatchCreationElement(child);
                            break;
                        case "Product":
                            this.ParseProductElement(child);
                            break;
                        case "Patch":
                            this.ParsePatchElement(child);
                            break;
                        default:
                            this.core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses a WixVariable element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixVariableElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            bool overridable = false;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Overridable":
                            overridable = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "Value":
                            value = this.core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == value)
            {
                this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.core.ParseForExtensionElements(node);

            if (!this.core.EncounteredError)
            {
                WixVariableRow wixVariableRow = (WixVariableRow)this.core.CreateRow(sourceLineNumbers, "WixVariable", id);
                wixVariableRow.Value = value;
                wixVariableRow.Overridable = overridable;
            }
        }
    }
}
