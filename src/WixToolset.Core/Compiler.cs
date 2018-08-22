// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
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
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal class Compiler
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

        // If these are true you know you are building a module or product
        // but if they are false you cannot not be sure they will not end
        // up a product or module.  Use these flags carefully.
        private bool compilingModule;
        private bool compilingProduct;

        private bool useShortFileNames;
        private string activeName;
        private string activeLanguage;

        private WixVariableResolver componentIdPlaceholdersResolver;

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

        internal Compiler(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        public IMessaging Messaging { get; }

        private ICompileContext Context { get; set; }

        private CompilerCore Core { get; set; }

        public string CompliationId { get; set; }

        public string OutputPath { get; set; }

        public Platform Platform { get; set; }

        public XDocument SourceDocument { get; set; }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        public Platform CurrentPlatform => this.Context.Platform;

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages { get; set; }

        /// <summary>
        /// Compiles the provided Xml document into an intermediate object
        /// </summary>
        /// <returns>Intermediate object representing compiled source document.</returns>
        /// <remarks>This method is not thread-safe.</remarks>
        public Intermediate Execute()
        {
            this.Context = this.ServiceProvider.GetService<ICompileContext>();
            this.Context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().Create<ICompilerExtension>();
            this.Context.CompilationId = this.CompliationId;
            this.Context.OutputPath = this.OutputPath;
            this.Context.Platform = this.Platform;
            this.Context.Source = this.SourceDocument;

            var target = new Intermediate();

            if (String.IsNullOrEmpty(this.Context.CompilationId))
            {
                this.Context.CompilationId = target.Id;
            }

            var extensionsByNamespace = new Dictionary<XNamespace, ICompilerExtension>();

            foreach (var extension in this.Context.Extensions)
            {
                if (!extensionsByNamespace.TryGetValue(extension.Namespace, out var collidingExtension))
                {
                    extensionsByNamespace.Add(extension.Namespace, extension);
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.DuplicateExtensionXmlSchemaNamespace(extension.GetType().ToString(), extension.Namespace.NamespaceName, collidingExtension.GetType().ToString()));
                }

                extension.PreCompile(this.Context);
            }

            // Try to compile it.
            try
            {
                var parseHelper = this.Context.ServiceProvider.GetService<IParseHelper>();

                this.Core = new CompilerCore(target, this.Messaging, parseHelper, extensionsByNamespace);
                this.Core.ShowPedanticMessages = this.ShowPedanticMessages;
                this.componentIdPlaceholdersResolver = new WixVariableResolver(this.Messaging);

                // parse the document
                var source = this.Context.Source;
                var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(source.Root);
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
                            this.Core.Write(ErrorMessages.InvalidWixXmlNamespace(sourceLineNumbers, "Wix", CompilerCore.WixNamespace.ToString()));
                        }
                        else
                        {
                            this.Core.Write(ErrorMessages.InvalidWixXmlNamespace(sourceLineNumbers, "Wix", source.Root.Name.NamespaceName, CompilerCore.WixNamespace.ToString()));
                        }
                    }
                }
                else
                {
                    this.Core.Write(ErrorMessages.InvalidDocumentElement(sourceLineNumbers, source.Root.Name.LocalName, "source", "Wix"));
                }

                // Resolve any Component Id placeholders compiled into the intermediate.
                this.ResolveComponentIdPlaceholders(target);
            }
            finally
            {
                foreach (var extension in this.Context.Extensions)
                {
                    extension.PostCompile(target);
                }

                this.Core = null;
            }

            return this.Messaging.EncounteredError ? null : target;
        }

        private void ResolveComponentIdPlaceholders(Intermediate target)
        {
            if (0 < this.componentIdPlaceholdersResolver.VariableCount)
            {
                foreach (var section in target.Sections)
                {
                    foreach (var tuple in section.Tuples)
                    {
                        foreach (var field in tuple.Fields)
                        {
                            if (field?.Type == IntermediateFieldType.String)
                            {
                                var data = field.AsString();
                                if (!String.IsNullOrEmpty(data))
                                {
                                    var resolved = this.componentIdPlaceholdersResolver.ResolveVariables(tuple.SourceLineNumbers, data, false, false);
                                    if (resolved.UpdatedValue)
                                    {
                                        field.Set(resolved.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
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

            return String.Concat(s.Substring(0, 1).ToUpperInvariant(), s.Substring(1));
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
                if (this.Core.IsValidShortFilename(longName, false))
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
            if (!this.Core.EncounteredError)
            {
                if (property.Id != property.Id.ToUpperInvariant())
                {
                    this.Core.Write(ErrorMessages.SearchPropertyNotUppercase(sourceLineNumbers, "Property", "Id", property.Id));
                }

                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.AppSearch, property);
                row.Set(1, signature);
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
                        this.Core.Write(WarningMessages.PropertyValueContainsPropertyReference(sourceLineNumbers, property.Id, group.Value));
                    }
                }
            }

            if (!this.Core.EncounteredError)
            {
                var section = this.Core.ActiveSection;

                // Add the row to a separate section if requested.
                if (fragment)
                {
                    string id = String.Concat(this.Core.ActiveSection.Id, ".", property.Id);

                    section = this.Core.CreateSection(id, SectionType.Fragment, this.Core.ActiveSection.Codepage, this.Context.CompilationId);

                    // Reference the property in the active section.
                    this.Core.CreateSimpleReference(sourceLineNumbers, "Property", property.Id);
                }

                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Property, section, property);

                // Allow row to exist with no value so that PropertyRefs can be made for *Search elements
                // the linker will remove these rows before the final output is created.
                if (null != value)
                {
                    row.Set(1, value);
                }

                if (admin || hidden || secure)
                {
                    this.AddWixPropertyRow(sourceLineNumbers, property, admin, secure, hidden, section);
                }
            }
        }

        private void AddWixPropertyRow(SourceLineNumber sourceLineNumbers, Identifier property, bool admin, bool secure, bool hidden, IntermediateSection section = null)
        {
            if (secure && property.Id != property.Id.ToUpperInvariant())
            {
                this.Core.Write(ErrorMessages.SecurePropertyNotUppercase(sourceLineNumbers, "Property", "Id", property.Id));
            }

            if (null == section)
            {
                section = this.Core.ActiveSection;

                this.Core.EnsureTable(sourceLineNumbers, "Property"); // Property table is always required when using WixProperty table.
            }

            var row = (WixPropertyTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixProperty, section, property);
            row.Admin = admin;
            row.Hidden = hidden;
            row.Secure = secure;
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
            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Implemented Categories\\", categoryId), "*", null, componentId);
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
                            appId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ActivateAtStorage":
                            activateAtStorage = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            appIdAdvertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DllSurrogate":
                            dllSurrogate = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "LocalService":
                            localService = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RemoteServerName":
                            remoteServerName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RunAsInteractiveUser":
                            runAsInteractiveUser = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceParameters":
                            serviceParameters = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == appId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == appIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == appIdAdvertise))
            {
                this.Core.Write(ErrorMessages.AppIdIncompatibleAdvertiseState(sourceLineNumbers, node.Name.LocalName, "Advertise", appIdAdvertise.ToString(), advertise.ToString()));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (YesNoType.Yes == advertise)
            {
                if (null != description)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "Description"));
                }

                if (!this.Core.EncounteredError)
                {
                    var id = new Identifier(appId, AccessModifier.Public);
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.AppId, id);
                    row.Set(1, remoteServerName);
                    row.Set(2, localService);
                    row.Set(3, serviceParameters);
                    row.Set(4, dllSurrogate);
                    if (YesNoType.Yes == activateAtStorage)
                    {
                        row.Set(5, 1);
                    }

                    if (YesNoType.Yes == runAsInteractiveUser)
                    {
                        row.Set(6, 1);
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (null != description)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), null, description, componentId);
                }
                else
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "+", null, componentId);
                }

                if (null != remoteServerName)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RemoteServerName", remoteServerName, componentId);
                }

                if (null != localService)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "LocalService", localService, componentId);
                }

                if (null != serviceParameters)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ServiceParameters", serviceParameters, componentId);
                }

                if (null != dllSurrogate)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "DllSurrogate", dllSurrogate, componentId);
                }

                if (YesNoType.Yes == activateAtStorage)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ActivateAtStorage", "Y", componentId);
                }

                if (YesNoType.Yes == runAsInteractiveUser)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RunAs", "Interactive User", componentId);
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
                            id = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiAssemblyName);
                row.Set(0, componentId);
                row.Set(1, id);
                row.Set(2, value);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile", "src"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (!String.IsNullOrEmpty(id.Id)) // only check legal values
            {
                if (55 < id.Id.Length)
                {
                    this.Core.Write(ErrorMessages.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 55));
                }
                else if (!this.compilingProduct) // if we're not doing a product then we can't be sure that a binary identifier will fit when modularized
                {
                    if (18 < id.Id.Length)
                    {
                        this.Core.Write(WarningMessages.IdentifierCannotBeModularized(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 18));
                    }
                }
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Binary, id);
                row.Set(1, sourceFile);

                if (YesNoType.Yes == suppressModularization)
                {
                    var wixSuppressModularizationRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixSuppressModularization, id);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (!String.IsNullOrEmpty(id.Id)) // only check legal values
            {
                if (57 < id.Id.Length)
                {
                    this.Core.Write(ErrorMessages.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 57));
                }
                else if (!this.compilingProduct) // if we're not doing a product then we can't be sure that a binary identifier will fit when modularized
                {
                    if (20 < id.Id.Length)
                    {
                        this.Core.Write(WarningMessages.IdentifierCannotBeModularized(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 20));
                    }
                }
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Icon, id);
                row.Set(1, sourceFile);
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
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Property", property);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "ProductName":
                            productName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == productCode)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductCode"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixInstanceTransforms);
                row.Set(0, id);
                row.Set(1, propertyId);
                row.Set(2, productCode);
                if (null != productName)
                {
                    row.Set(3, productName);
                }
                if (null != upgradeCode)
                {
                    row.Set(4, upgradeCode);
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
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "AppData":
                            appData = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                            break;
                        case "Qualifier":
                            qualifier = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == qualifier)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Qualifier"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PublishComponent);
                row.Set(0, id);
                row.Set(1, qualifier);
                row.Set(2, componentId);
                row.Set(3, appData);
                if (null == feature)
                {
                    row.Set(4, Guid.Empty.ToString("B"));
                }
                else
                {
                    row.Set(4, feature);
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
                            classId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Advertise":
                            classAdvertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AppId":
                            appId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Argument":
                            argument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Context":
                            contexts = this.Core.GetAttributeValue(sourceLineNumbers, attrib).Split("\r\n\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            break;
                        case "Control":
                            control = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Handler":
                            defaultInprocHandler = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Icon":
                            icon = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "IconIndex":
                            iconIndex = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "RelativePath":
                            relativePath = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;

                        // The following attributes result in rows always added to the Registry table rather than the Class table
                        case "Insertable":
                            insertable = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? "Insertable" : "NotInsertable";
                            break;
                        case "Programmable":
                            programmable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SafeForInitializing":
                            safeForInit = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SafeForScripting":
                            safeForScripting = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ForeignServer":
                            foreignServer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Server":
                            localFileServer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortPath":
                            shortServerPath = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ThreadingModel":
                            threadingModel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == classId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            HashSet<string> uniqueContexts = new HashSet<string>();
            if (contexts != null)
            {
                foreach (string context in contexts)
                {
                    if (uniqueContexts.Contains(context))
                    {
                        this.Core.Write(ErrorMessages.DuplicateContextValue(sourceLineNumbers, context));
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
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == classAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == classAdvertise))
            {
                this.Core.Write(ErrorMessages.AdvertiseStateMustMatch(sourceLineNumbers, classAdvertise.ToString(), advertise.ToString()));
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Context", "Advertise", "yes"));
            }

            if (!String.IsNullOrEmpty(parentAppId) && !String.IsNullOrEmpty(appId))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "AppId", node.Parent.Name.LocalName));
            }

            if (!String.IsNullOrEmpty(localFileServer))
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "File", localFileServer);
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
                                this.Core.CreateRegistryRow(childSourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("FileType\\", classId, "\\", fileTypeMaskIndex.ToString()), String.Empty, this.ParseFileTypeMaskElement(child), componentId);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            // If this Class is being advertised.
            if (YesNoType.Yes == advertise)
            {
                if (null != fileServer || null != localFileServer)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Server", "Advertise", "yes"));
                }

                if (null != foreignServer)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Advertise", "yes"));
                }

                if (null == appId && null != parentAppId)
                {
                    appId = parentAppId;
                }

                // add a Class row for each context
                if (!this.Core.EncounteredError)
                {
                    foreach (string context in contexts)
                    {
                        var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Class);
                        row.Set(0, classId);
                        row.Set(1, context);
                        row.Set(2, componentId);
                        row.Set(3, defaultProgId);
                        row.Set(4, description);
                        if (null != appId)
                        {
                            row.Set(5, appId);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "AppId", appId);
                        }
                        row.Set(6, fileTypeMask);
                        if (null != icon)
                        {
                            row.Set(7, icon);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                        }
                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            row.Set(8, iconIndex);
                        }
                        row.Set(9, defaultInprocHandler);
                        row.Set(10, argument);
                        row.Set(11, Guid.Empty.ToString("B"));
                        if (YesNoType.Yes == relativePath)
                        {
                            row.Set(12, MsiInterop.MsidbClassAttributesRelativePath);
                        }
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (null == fileServer && null == localFileServer && null == foreignServer)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Server"));
                }

                if (null != fileServer && null != foreignServer)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "File"));
                }
                else if (null != localFileServer && null != foreignServer)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ForeignServer", "Server"));
                }
                else if (null == fileServer)
                {
                    fileServer = localFileServer;
                }

                if (null != appId) // need to use nesting (not a reference) for the unadvertised Class elements
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "AppId", "Advertise", "no"));
                }

                // add the core registry keys for each context in the class
                foreach (string context in contexts)
                {
                    if (context.StartsWith("InprocServer", StringComparison.Ordinal)) // dll server
                    {
                        if (null != argument)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Arguments", "Context", context));
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
                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Context", context, "InprocServer", "InprocServer32", "LocalServer", "LocalServer32"));
                    }

                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context), String.Empty, formattedContextString, componentId); // ClassId context

                    if (null != icon) // ClassId default icon
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "File", icon);

                        icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            icon = String.Concat(icon, ",", iconIndex);
                        }
                        this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context, "\\DefaultIcon"), String.Empty, icon, componentId);
                    }
                }

                if (null != parentAppId) // ClassId AppId (must be specified via nesting, not with the AppId attribute)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), "AppID", parentAppId, componentId);
                }

                if (null != description) // ClassId description
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), String.Empty, description, componentId);
                }

                if (null != defaultInprocHandler)
                {
                    switch (defaultInprocHandler) // ClassId Default Inproc Handler
                    {
                        case "1":
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
                            break;
                        case "2":
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                            break;
                        case "3":
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                            break;
                        default:
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, defaultInprocHandler, componentId);
                            break;
                    }
                }

                if (YesNoType.NotSet != relativePath) // ClassId's RelativePath
                {
                    this.Core.Write(ErrorMessages.RelativePathForRegistryElement(sourceLineNumbers));
                }
            }

            if (null != threadingModel)
            {
                threadingModel = Compiler.UppercaseFirstChar(threadingModel);

                // add a threading model for each context in the class
                foreach (string context in contexts)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context), "ThreadingModel", threadingModel, componentId);
                }
            }

            if (null != typeLibId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\TypeLib"), null, typeLibId, componentId);
            }

            if (null != version)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Version"), null, version, componentId);
            }

            if (null != insertable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", insertable), "*", null, componentId);
            }

            if (control)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Control"), "*", null, componentId);
            }

            if (programmable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Programmable"), "*", null, componentId);
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
                            interfaceId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "BaseInterface":
                            baseInterface = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "NumMethods":
                            numMethods = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "ProxyStubClassId":
                            proxyId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProxyStubClassId32":
                            proxyId32 = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Versioned":
                            versioned = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == interfaceId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId), null, name, componentId);
            if (null != typeLibId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), null, typeLibId, componentId);
                if (versioned)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), "Version", typelibVersion, componentId);
                }
            }

            if (null != baseInterface)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\BaseInterface"), null, baseInterface, componentId);
            }

            if (CompilerConstants.IntegerNotSet != numMethods)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\NumMethods"), null, numMethods.ToString(), componentId);
            }

            if (null != proxyId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid"), null, proxyId, componentId);
            }

            if (null != proxyId32)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid32"), null, proxyId32, componentId);
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
                            mask = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Offset":
                            offset = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }


            if (null == mask)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Mask"));
            }

            if (CompilerConstants.IntegerNotSet == offset)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Offset"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                if (mask.Length != value.Length)
                {
                    this.Core.Write(ErrorMessages.ValueAndMaskMustBeSameLength(sourceLineNumbers));
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesLanguagesExclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                            }
                            break;
                        case "IncludeMinimum": // this is "yes" by default
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                            }
                            break;
                        case "Language":
                            language = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minimum = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maximum = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == minimum && null == maximum)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Minimum", "Maximum"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Upgrade);
                row.Set(0, upgradeCode);
                row.Set(1, minimum);
                row.Set(2, maximum);
                row.Set(3, language);
                row.Set(4, options);
                row.Set(6, propertyId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "raw"));
                                        break;
                                }
                            }
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            search64bit = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!explicitWin64 && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                search64bit = true;
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("reg", root.ToString(), key, name, type.ToString(), search64bit.ToString());
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (CompilerConstants.IntegerNotSet == type)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.RegLocator, id);
                row.Set(1, root);
                row.Set(2, key);
                row.Set(3, name);
                row.Set(4, search64bit ? (type | 16) : type);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "RegLocator", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, "CCP_DRIVE");
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, "CCP_DRIVE");
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null == signature)
            {
                this.Core.Write(ErrorMessages.SearchElementRequired(sourceLineNumbers, node.Name.LocalName));
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
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                    this.Core.Write(ErrorMessages.MultipleIdentifiersFound(sourceLineNumbers, node.Name.LocalName, sig, signature));
                }
            }

            if (null == signature)
            {
                this.Core.Write(ErrorMessages.SearchElementRequired(sourceLineNumbers, node.Name.LocalName));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CCPSearch);
                row.Set(0, signature);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ComPlusFlags":
                            comPlusBits = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "DisableRegistryReflection":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesDisableRegistryReflection;
                            }
                            break;
                        case "Directory":
                            directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        case "DiskId":
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            guid = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, true, true);
                            break;
                        case "KeyPath":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                keyFound = true;
                                keyPath = null;
                                keyBits = 0;
                                shouldAddCreateFolder = true;
                            }
                            break;
                        case "Location":
                            string location = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "either", "local", "source"));
                                        break;
                                }
                            }
                            break;
                        case "MultiInstance":
                            multiInstance = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "NeverOverwrite":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesNeverOverwrite;
                            }
                            break;
                        case "Permanent":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesPermanent;
                            }
                            break;
                        case "Shared":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesShared;
                            }
                            break;
                        case "SharedDllRefCount":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesSharedDllRefCount;
                            }
                            break;
                        case "Transitive":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesTransitive;
                            }
                            break;
                        case "UninstallWhenSuperseded":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributesUninstallOnSupersedence;
                            }
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbComponentAttributes64bit;
                                win64 = true;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!explicitWin64 && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                bits |= MsiInterop.MsidbComponentAttributes64bit;
                win64 = true;
            }

            if (null == directoryId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Directory"));
            }

            if (String.IsNullOrEmpty(guid) && MsiInterop.MsidbComponentAttributesShared == (bits & MsiInterop.MsidbComponentAttributesShared))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Shared", "yes", "Guid", ""));
            }

            if (String.IsNullOrEmpty(guid) && MsiInterop.MsidbComponentAttributesPermanent == (bits & MsiInterop.MsidbComponentAttributesPermanent))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Permanent", "yes", "Guid", ""));
            }

            if (null != feature)
            {
                if (this.compilingModule)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeInMergeModule(sourceLineNumbers, node.Name.LocalName, "Feature"));
                }
                else
                {
                    if (ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.FeatureGroup == parentType)
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Feature", node.Parent.Name.LocalName));
                    }
                    else
                    {
                        this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, null, ComplexReferenceChildType.Component, id.Id, true);
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
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
                            this.ParseODBCDriverOrTranslator(child, id.Id, null, TupleDefinitionType.ODBCDriver);
                            break;
                        case "ODBCTranslator":
                            this.ParseODBCDriverOrTranslator(child, id.Id, null, TupleDefinitionType.ODBCTranslator);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ComponentId", id.Id }, { "DirectoryId", directoryId }, { "Win64", win64.ToString() }, };
                    ComponentKeyPath possibleKeyPath = this.Core.ParsePossibleKeyPathExtensionElement(node, child, context);
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
                    this.Core.Write(ErrorMessages.ComponentMultipleKeyPaths(sourceLineNumbers, node.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
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
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CreateFolder, new Identifier(AccessModifier.Public, directoryId, id.Id));
                row.Set(0, directoryId);
                row.Set(1, id.Id);
            }

            // check for conditions that exclude this component from using generated guids
            bool isGeneratableGuidOk = "*" == guid;
            if (isGeneratableGuidOk)
            {
                if (encounteredODBCDataSource)
                {
                    this.Core.Write(ErrorMessages.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers));
                    isGeneratableGuidOk = false;
                }

                if (0 != files && MsiInterop.MsidbComponentAttributesRegistryKeyPath == keyBits)
                {
                    this.Core.Write(ErrorMessages.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers, true));
                    isGeneratableGuidOk = false;
                }
            }

            // check for implicit KeyPath which can easily be accidentally changed
            if (this.ShowPedanticMessages && !keyFound && !isGeneratableGuidOk)
            {
                this.Core.Write(ErrorMessages.ImplicitComponentKeyPath(sourceLineNumbers, id.Id));
            }

            // if there isn't an @Id attribute value, replace the placeholder with the id of the keypath.
            // either an explicit KeyPath="yes" attribute must be specified or requirements for 
            // generatable guid must be met.
            if (componentIdPlaceholderWixVariable == id.Id)
            {
                if (isGeneratableGuidOk || keyFound && !String.IsNullOrEmpty(keyPath))
                {
                    this.componentIdPlaceholdersResolver.AddVariable(sourceLineNumbers, componentIdPlaceholder, keyPath, false);

                    id = new Identifier(keyPath, AccessModifier.Private);
                }
                else
                {
                    this.Core.Write(ErrorMessages.CannotDefaultComponentId(sourceLineNumbers));
                }
            }

            // If an id was not determined by now, we have to error.
            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // finally add the Component table row
            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Component, id);
                row.Set(1, guid);
                row.Set(2, directoryId);
                row.Set(3, bits | keyBits);
                row.Set(4, condition);
                row.Set(5, keyPath);

                if (multiInstance)
                {
                    var instanceComponentRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixInstanceComponent, id);
                }

                if (0 < symbols.Count)
                {
                    var symbolRow = (WixDeltaPatchSymbolPathsTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchSymbolPaths, id);
                    symbolRow.Type = SymbolPathType.Component;
                    symbolRow.SymbolPaths = String.Join(";", symbols);
                }

                // Complus
                if (CompilerConstants.IntegerNotSet != comPlusBits)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Complus, id);
                    row.Set(1, comPlusBits);
                }

                // if this is a module, automatically add this component to the references to ensure it gets in the ModuleComponents table
                if (this.compilingModule)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage, ComplexReferenceChildType.Component, id.Id, false);
                }
                else if (ComplexReferenceParentType.Unknown != parentType && null != parentId) // if parent was provided, add a complex reference to that.
                {
                    // If the Component is defined directly under a feature, then mark the complex reference primary.
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.Component, id.Id, ComplexReferenceParentType.Feature == parentType);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            // If the inline syntax is invalid it returns null. Use a static error identifier so the null
                            // directory identifier here doesn't trickle down false errors into child elements.
                            directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null) ?? "ErrorParsingInlineSyntax";
                            break;
                        case "Source":
                            source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixComponentGroup, id);

                // Add this componentGroup and its parent in WixGroup.
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.ComponentGroup, id.Id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixComponentGroup", id);
                            break;
                        case "Primary":
                            primary = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.ComponentGroup, id, (YesNoType.Yes == primary));
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Component", id);
                            break;
                        case "Primary":
                            primary = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, parentLanguage, ComplexReferenceChildType.Component, id, (YesNoType.Yes == primary));
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            componentId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue, "directory", "file"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("cmp", componentId, type.ToString());
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CompLocator, id);
                row.Set(1, componentId);
                row.Set(2, type);
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
                            directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "DirectoryId", directoryId }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CreateFolder, new Identifier(AccessModifier.Public, directoryId, componentId));
                row.Set(0, directoryId);
                row.Set(1, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Delete":
                            delete = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DestinationDirectory":
                            destinationDirectory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "DestinationName":
                            destinationName = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "DestinationProperty":
                            destinationProperty = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "DestinationShortName":
                            destinationShortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "FileId":
                            if (null != fileId)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            fileId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", fileId);
                            break;
                        case "SourceDirectory":
                            sourceDirectory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "SourceName":
                            sourceName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourceProperty":
                            sourceProperty = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != sourceFolder && null != sourceDirectory) // SourceFolder and SourceDirectory cannot coexist
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "SourceDirectory"));
            }

            if (null != sourceFolder && null != sourceProperty) // SourceFolder and SourceProperty cannot coexist
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "SourceProperty"));
            }

            if (null != sourceDirectory && null != sourceProperty) // SourceDirectory and SourceProperty cannot coexist
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceProperty", "SourceDirectory"));
            }

            if (null != destinationDirectory && null != destinationProperty) // DestinationDirectory and DestinationProperty cannot coexist
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DestinationProperty", "DestinationDirectory"));
            }

            // generate a short file name
            if (null == destinationShortName && (null != destinationName && !this.Core.IsValidShortFilename(destinationName, false)))
            {
                destinationShortName = this.Core.CreateShortName(destinationName, true, false, node.Name.LocalName, componentId);
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("cf", sourceFolder, sourceDirectory, sourceProperty, destinationDirectory, destinationProperty, destinationName);
            }

            this.Core.ParseForExtensionElements(node);

            if (null == fileId)
            {
                // DestinationDirectory or DestinationProperty must be specified
                if (null == destinationDirectory && null == destinationProperty)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributesWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DestinationDirectory", "DestinationProperty", "FileId"));
                }

                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MoveFile, id);
                    row.Set(1, componentId);
                    row.Set(2, sourceName);
                    row.Set(3, String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : GetMsiFilenameValue(destinationShortName, destinationName));
                    if (null != sourceDirectory)
                    {
                        row.Set(4, sourceDirectory);
                    }
                    else if (null != sourceProperty)
                    {
                        row.Set(4, sourceProperty);
                    }
                    else
                    {
                        row.Set(4, sourceFolder);
                    }

                    if (null != destinationDirectory)
                    {
                        row.Set(5, destinationDirectory);
                    }
                    else
                    {
                        row.Set(5, destinationProperty);
                    }
                    row.Set(6, delete ? 1 : 0);
                }
            }
            else // copy the file
            {
                if (null != sourceDirectory)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceDirectory", "FileId"));
                }

                if (null != sourceFolder)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFolder", "FileId"));
                }

                if (null != sourceName)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceName", "FileId"));
                }

                if (null != sourceProperty)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "SourceProperty", "FileId"));
                }

                if (delete)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Delete", "FileId"));
                }

                if (null == destinationName && null == destinationDirectory && null == destinationProperty)
                {
                    this.Core.Write(WarningMessages.CopyFileFileIdUseless(sourceLineNumbers));
                }

                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.DuplicateFile, id);
                    row.Set(1, componentId);
                    row.Set(2, fileId);
                    row.Set(3, String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : GetMsiFilenameValue(destinationShortName, destinationName));
                    if (null != destinationDirectory)
                    {
                        row.Set(4, destinationDirectory);
                    }
                    else
                    {
                        row.Set(4, destinationProperty);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinaryKey":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeBinaryData;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", source); // add a reference to the appropriate Binary
                            break;
                        case "Directory":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                            break;
                        case "DllEntry":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            targetBits = MsiInterop.MsidbCustomActionTypeDll;
                            break;
                        case "Error":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                this.Core.CreateSimpleReference(sourceLineNumbers, "Error", target);
                            }
                            break;
                        case "ExeCommand":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeExe;
                            break;
                        case "Execute":
                            string execute = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, execute, "commit", "deferred", "firstSequence", "immediate", "oncePerProcess", "rollback", "secondSequence"));
                                        break;
                                }
                            }
                            break;
                        case "FileKey":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeSourceFile;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", source); // add a reference to the appropriate File
                            break;
                        case "HideTarget":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeHideTarget;
                            }
                            break;
                        case "Impersonate":
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeNoImpersonate;
                            }
                            break;
                        case "JScriptCall":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeJScript;
                            break;
                        case "PatchUninstall":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                extendedBits |= MsiInterop.MsidbCustomActionTypePatchUninstall;
                            }
                            break;
                        case "Property":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }
                            source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            sourceBits = MsiInterop.MsidbCustomActionTypeProperty;
                            break;
                        case "Return":
                            string returnValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, returnValue, "asyncNoWait", "asyncWait", "check", "ignore"));
                                        break;
                                }
                            }
                            break;
                        case "Script":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                            }

                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }

                            // set the source and target to empty string for error messages when the user sets multiple sources or targets
                            source = string.Empty;
                            target = string.Empty;

                            inlineScript = true;

                            string script = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, script, "jscript", "vbscript"));
                                        break;
                                }
                            }
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TerminalServerAware":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionTypeTSAware;
                            }
                            break;
                        case "Value":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeTextData;
                            break;
                        case "VBScriptCall":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                            targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
                            break;
                        case "Win64":
                            explicitWin64 = true;
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbCustomActionType64BitScript;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            if (!explicitWin64 && (MsiInterop.MsidbCustomActionTypeVBScript == targetBits || MsiInterop.MsidbCustomActionTypeJScript == targetBits) && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                bits |= MsiInterop.MsidbCustomActionType64BitScript;
            }

            // get the inner text if any exists
            innerText = this.Core.GetTrimmedInnerText(node);

            // if we have an in-lined Script CustomAction ensure no source or target attributes were provided
            if (inlineScript)
            {
                target = innerText;
            }
            else if (MsiInterop.MsidbCustomActionTypeVBScript == targetBits) // non-inline vbscript
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "Directory"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeJScript == targetBits) // non-inline jscript
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "Directory"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeExe == targetBits) // exe-command
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ExeCommand", "BinaryKey", "Directory", "FileKey", "Property"));
                }
            }
            else if (MsiInterop.MsidbCustomActionTypeTextData == (bits | sourceBits | targetBits))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Value", "Directory", "Property"));
            }
            else if (!String.IsNullOrEmpty(innerText)) // inner text cannot be specified with non-script CAs
            {
                this.Core.Write(ErrorMessages.CustomActionIllegalInnerText(sourceLineNumbers, node.Name.LocalName, innerText, "Script"));
            }

            if (MsiInterop.MsidbCustomActionType64BitScript == (bits & MsiInterop.MsidbCustomActionType64BitScript) && MsiInterop.MsidbCustomActionTypeVBScript != targetBits && MsiInterop.MsidbCustomActionTypeJScript != targetBits)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Win64", "Script", "VBScriptCall", "JScriptCall"));
            }

            if ((MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue) == (bits & (MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue)) && MsiInterop.MsidbCustomActionTypeExe != targetBits)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Return", "asyncNoWait", "ExeCommand"));
            }

            if (MsiInterop.MsidbCustomActionTypeTSAware == (bits & MsiInterop.MsidbCustomActionTypeTSAware))
            {
                // TS-aware CAs are valid only when deferred so require the in-script Type bit...
                if (0 == (bits & MsiInterop.MsidbCustomActionTypeInScript))
                {
                    this.Core.Write(ErrorMessages.IllegalTerminalServerCustomActionAttributes(sourceLineNumbers));
                }
            }

            // MSI doesn't support in-script property setting, so disallow it
            if (MsiInterop.MsidbCustomActionTypeProperty == sourceBits &&
                MsiInterop.MsidbCustomActionTypeTextData == targetBits &&
                0 != (bits & MsiInterop.MsidbCustomActionTypeInScript))
            {
                this.Core.Write(ErrorMessages.IllegalPropertyCustomActionAttributes(sourceLineNumbers));
            }

            if (0 == targetBits)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CustomAction, id);
                row.Set(1, bits | sourceBits | targetBits);
                row.Set(2, source);
                row.Set(3, target);
                if (0 != extendedBits)
                {
                    row.Set(4, extendedBits);
                }

                if (YesNoType.Yes == suppressModularization)
                {
                    this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixSuppressModularization, id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, table, id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            primaryKeys[0] = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            primaryKeys[1] = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == primaryKeys[0])
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.CreateSimpleReference(sourceLineNumbers, "MsiPatchSequence", primaryKeys);

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, primaryKeys[0], true);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchFamilyGroup, id);

                //Add this PatchFamilyGroup and its parent in WixGroup.
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PatchFamilyGroup, id.Id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixPatchFamilyGroup", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamilyGroup, id, true);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (31 < id.Length)
            {
                this.Core.Write(ErrorMessages.TableNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.EnsureTable(sourceLineNumbers, id);
        }

        /// <summary>
        /// Parses a custom table element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <remarks>not cleaned</remarks>
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
                            tableId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "BootstrapperApplicationData":
                            bootstrapperApplicationData = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == tableId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (31 < tableId.Length)
            {
                this.Core.Write(ErrorMessages.CustomTableNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", tableId));
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
                                        columnName = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Category":
                                        category = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Description":
                                        description = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "KeyColumn":
                                        keyColumn = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 1, 32);
                                        break;
                                    case "KeyTable":
                                        keyTable = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Localizable":
                                        localizable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "MaxValue":
                                        maxValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, int.MinValue + 1, int.MaxValue);
                                        break;
                                    case "MinValue":
                                        minValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, int.MinValue + 1, int.MaxValue);
                                        break;
                                    case "Modularize":
                                        modularization = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Nullable":
                                        nullable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "PrimaryKey":
                                        primaryKey = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Set":
                                        setValues = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                        break;
                                    case "Type":
                                        string typeValue = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
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
                                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Type", typeValue, "binary", "int", "string"));
                                                    break;
                                            }
                                        }
                                        break;
                                    case "Width":
                                        width = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 0, int.MaxValue);
                                        break;
                                    default:
                                        this.Core.UnexpectedAttribute(child, childAttrib);
                                        break;
                                }
                            }

                            if (null == columnName)
                            {
                                this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Id"));
                            }

                            if (null == typeName)
                            {
                                this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Type"));
                            }
                            else if ("SHORT" == typeName)
                            {
                                if (2 != width && 4 != width)
                                {
                                    this.Core.Write(ErrorMessages.CustomTableIllegalColumnWidth(childSourceLineNumbers, child.Name.LocalName, "Width", width));
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
                                    this.Core.Write(ErrorMessages.ExpectedBinaryCategory(childSourceLineNumbers));
                                }
                                columnType = String.Concat(nullable ? "V" : "v", width);
                            }

                            this.Core.ParseForExtensionElements(child);

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
                                this.Core.ParseExtensionAttribute(child, childAttrib);
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
                                                    columnName = this.Core.GetAttributeValue(dataSourceLineNumbers, dataAttrib);
                                                    break;
                                                default:
                                                    this.Core.UnexpectedAttribute(data, dataAttrib);
                                                    break;
                                            }
                                        }

                                        if (null == columnName)
                                        {
                                            this.Core.Write(ErrorMessages.ExpectedAttribute(dataSourceLineNumbers, data.Name.LocalName, "Column"));
                                        }

                                        dataValue = String.Concat(dataValue, null == dataValue ? String.Empty : Common.CustomRowFieldSeparator.ToString(), columnName, ":", Common.GetInnerText(data));
                                        break;
                                }
                            }

                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixCustomTable", tableId);

                            if (!this.Core.EncounteredError)
                            {
                                var rowRow = this.Core.CreateRow(childSourceLineNumbers, TupleDefinitionType.WixCustomRow);
                                rowRow.Set(0, tableId);
                                rowRow.Set(1, dataValue);
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (0 < columnCount)
            {
                if (null == primaryKeys || 0 == primaryKeys.Length)
                {
                    this.Core.Write(ErrorMessages.CustomTableMissingPrimaryKey(sourceLineNumbers));
                }

                if (!this.Core.EncounteredError)
                {
                    var id = new Identifier(tableId, AccessModifier.Public);
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixCustomTable, id);
                    row.Set(1, columnCount);
                    row.Set(2, columnNames);
                    row.Set(3, columnTypes);
                    row.Set(4, primaryKeys);
                    row.Set(5, minValues);
                    row.Set(6, maxValues);
                    row.Set(7, keyTables);
                    row.Set(8, keyColumns);
                    row.Set(9, categories);
                    row.Set(10, sets);
                    row.Set(11, descriptions);
                    row.Set(12, modularizations);
                    row.Set(13, bootstrapperApplicationData ? 1 : 0);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ComponentGuidGenerationSeed":
                            componentGuidGenerationSeed = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "DiskId":
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FileSource":
                            fileSource = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                inlineSyntax = this.Core.GetAttributeInlineDirectorySyntax(sourceLineNumbers, attrib);
                            }
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "ShortSourceName":
                            shortSourceName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "SourceName":
                            if ("." == attrib.Value)
                            {
                                sourceName = attrib.Value;
                            }
                            else
                            {
                                sourceName = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", parentId);

                        pathStartsAt = 1;
                    }

                    for (int i = pathStartsAt; i < inlineSyntax.Length - 1; ++i)
                    {
                        Identifier inlineId = this.Core.CreateDirectoryRow(sourceLineNumbers, null, parentId, inlineSyntax[i]);
                        parentId = inlineId.Id;
                    }

                    name = inlineSyntax[inlineSyntax.Length - 1];
                }
            }

            if (!nameHasValue)
            {
                if (!String.IsNullOrEmpty(shortName))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name"));
                }

                if (null == parentId)
                {
                    this.Core.Write(ErrorMessages.DirectoryRootWithoutName(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }
            else if (!String.IsNullOrEmpty(name))
            {
                if (String.IsNullOrEmpty(shortName))
                {
                    if (!name.Equals(".") && !name.Equals("SourceDir") && !this.Core.IsValidShortFilename(name, false))
                    {
                        shortName = this.Core.CreateShortName(name, false, false, "Directory", parentId);
                    }
                }
                else if (name.Equals("."))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name", name));
                }
                else if (name.Equals(shortName))
                {
                    this.Core.Write(WarningMessages.DirectoryRedundantNames(sourceLineNumbers, node.Name.LocalName, "Name", "ShortName", name));
                }
            }

            if (String.IsNullOrEmpty(sourceName))
            {
                if (!String.IsNullOrEmpty(shortSourceName))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ShortSourceName", "SourceName"));
                }
            }
            else
            {
                if (String.IsNullOrEmpty(shortSourceName))
                {
                    if (!sourceName.Equals(".") && !this.Core.IsValidShortFilename(sourceName, false))
                    {
                        shortSourceName = this.Core.CreateShortName(sourceName, false, false, "Directory", parentId);
                    }
                }
                else if (sourceName.Equals("."))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortSourceName", "SourceName", sourceName));
                }
                else if (sourceName.Equals(shortSourceName))
                {
                    this.Core.Write(WarningMessages.DirectoryRedundantNames(sourceLineNumbers, node.Name.LocalName, "SourceName", "ShortSourceName", sourceName));
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
                id = this.Core.CreateIdentifier("dir", parentId, name, shortName, sourceName, shortSourceName);
            }

            // Calculate the DefaultDir for the directory row.
            defaultDir = String.IsNullOrEmpty(shortName) ? name : String.Concat(shortName, "|", name);
            if (!String.IsNullOrEmpty(sourceName))
            {
                defaultDir = String.Concat(defaultDir, ":", String.IsNullOrEmpty(shortSourceName) ? sourceName : String.Concat(shortSourceName, "|", sourceName));
            }

            if ("TARGETDIR".Equals(id.Id) && !"SourceDir".Equals(defaultDir))
            {
                this.Core.Write(ErrorMessages.IllegalTargetDirDefaultDir(sourceLineNumbers, defaultDir));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Directory, id);
                row.Set(1, parentId);
                row.Set(2, defaultDir);

                if (null != componentGuidGenerationSeed)
                {
                    var wixRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDirectory);
                    wixRow.Set(0, id.Id);
                    wixRow.Set(1, componentGuidGenerationSeed);
                }

                if (null != symbols)
                {
                    var symbolRow = (WixDeltaPatchSymbolPathsTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchSymbolPaths, id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", id);
                            break;
                        case "DiskId":
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FileSource":
                            fileSource = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Depth":
                            depth = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "AssignToProperty":
                            assignToProperty = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("dir", path, depth.ToString());
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            hasFileSearch = true;
                            signature = this.ParseFileSearchElement(child, id.Id, assignToProperty, depth);
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseSimpleRefElement(child, "Signature");
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }

                    // If AssignToProperty is set, only a FileSearch
                    // or no child element can be nested.
                    if (assignToProperty)
                    {
                        if (!hasFileSearch)
                        {
                            this.Core.Write(ErrorMessages.IllegalParentAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "AssignToProperty", child.Name.LocalName));
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
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var access = id.Access;
                var rowId = id.Id;

                // If AssignToProperty is set, the DrLocator row created by
                // ParseFileSearchElement creates the directory entry to return
                // and the row created here is for the file search.
                if (assignToProperty)
                {
                    access = AccessModifier.Private;
                    rowId = signature;

                    // The property should be set to the directory search Id.
                    signature = id.Id;
                }

                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.DrLocator, new Identifier(access, rowId, parentSignature, path));
                row.Set(0, rowId);
                row.Set(1, parentSignature);
                row.Set(2, path);
                if (CompilerConstants.IntegerNotSet != depth)
                {
                    row.Set(3, depth);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Parent":
                            parent = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != parent)
            {
                if (!String.IsNullOrEmpty(parentSignature))
                {
                    this.Core.Write(ErrorMessages.CanNotHaveTwoParents(sourceLineNumbers, id.Id, parent.Id, parentSignature));
                }
                else
                {
                    parentSignature = parent.Id;
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("dsr", parentSignature, path);
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseSimpleRefElement(child, "Signature");
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            this.Core.CreateSimpleReference(sourceLineNumbers, "DrLocator", id.Id, parentSignature, path);

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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Absent":
                            string absent = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, absent, "allow", "disallow"));
                                        break;
                                }
                            }
                            break;
                        case "AllowAdvertise":
                            allowAdvertise = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, allowAdvertise, "no", "system", "yes"));
                                        break;
                                }
                            }
                            break;
                        case "ConfigurableDirectory":
                            configurableDirectory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Display":
                            display = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDefault":
                            installDefault = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < installDefault.Length)
                            {
                                Wix.Feature.InstallDefaultType installDefaultType = Wix.Feature.ParseInstallDefaultType(installDefault);
                                switch (installDefaultType)
                                {
                                    case Wix.Feature.InstallDefaultType.followParent:
                                        if (ComplexReferenceParentType.Product == parentType)
                                        {
                                            this.Core.Write(ErrorMessages.RootFeatureCannotFollowParent(sourceLineNumbers));
                                        }
                                        bits = bits | MsiInterop.MsidbFeatureAttributesFollowParent;
                                        break;
                                    case Wix.Feature.InstallDefaultType.local: // this is the default
                                        break;
                                    case Wix.Feature.InstallDefaultType.source:
                                        bits = bits | MsiInterop.MsidbFeatureAttributesFavorSource;
                                        break;
                                    default:
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installDefault, "followParent", "local", "source"));
                                        break;
                                }
                            }
                            break;
                        case "Level":
                            level = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Title":
                            title = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-FEATURE-TITLE-HERE" == title)
                            {
                                this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, title));
                            }
                            break;
                        case "TypicalDefault":
                            typicalDefault = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typicalDefault, "advertise", "install"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (38 < id.Id.Length)
            {
                this.Core.Write(ErrorMessages.FeatureNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
            }

            if (null != configurableDirectory && configurableDirectory.ToUpper(CultureInfo.InvariantCulture) != configurableDirectory)
            {
                this.Core.Write(ErrorMessages.FeatureConfigurableDirectoryNotUppercase(sourceLineNumbers, node.Name.LocalName, "ConfigurableDirectory", configurableDirectory));
            }

            if ("advertise" == typicalDefault && "no" == allowAdvertise)
            {
                this.Core.Write(ErrorMessages.FeatureCannotFavorAndDisallowAdvertise(sourceLineNumbers, node.Name.LocalName, "TypicalDefault", typicalDefault, "AllowAdvertise", allowAdvertise));
            }

            if (YesNoType.Yes == followParent && ("local" == installDefault || "source" == installDefault))
            {
                this.Core.Write(ErrorMessages.FeatureCannotFollowParentAndFavorLocalOrSource(sourceLineNumbers, node.Name.LocalName, "InstallDefault", "FollowParent", "yes"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Feature, id);
                // row.Set(1, null); - this column is set in the linker
                row.Set(2, title);
                row.Set(3, description);
                if (0 < display.Length)
                {
                    switch (display)
                    {
                        case "collapse":
                            lastDisplay = (lastDisplay | 1) + 1;
                            row.Set(4, lastDisplay);
                            break;
                        case "expand":
                            lastDisplay = (lastDisplay + 1) | 1;
                            row.Set(4, lastDisplay);
                            break;
                        case "hidden":
                            row.Set(4, 0);
                            break;
                        default:
                            int value;
                            if (!Int32.TryParse(display, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Display", display, "collapse", "expand", "hidden"));
                            }
                            else
                            {
                                row.Set(4, value);
                                // save the display value of this row (if its not hidden) for subsequent rows
                                if (0 != (int)row[4])
                                {
                                    lastDisplay = (int)row[4];
                                }
                            }
                            break;
                    }
                }
                row.Set(5, level);
                row.Set(6, configurableDirectory);
                row.Set(7, bits);

                if (ComplexReferenceParentType.Unknown != parentType)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Feature, id.Id, false);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", id);
                            break;
                        case "IgnoreParent":
                            ignoreParent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }


            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                if (ComplexReferenceParentType.Unknown != parentType && YesNoType.Yes != ignoreParent)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Feature, id, false);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixFeatureGroup, id);

                //Add this FeatureGroup and its parent in WixGroup.
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.FeatureGroup, id.Id);
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
                            id = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixFeatureGroup", id);
                            break;
                        case "IgnoreParent":
                            ignoreParent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Primary":
                            primary = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                if (YesNoType.Yes != ignoreParent)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.FeatureGroup, id, (YesNoType.Yes == primary));
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            string value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "create", "set", "remove"));
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Part":
                            part = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Wix.Environment.TryParsePartType(part, out partType))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Part", part, "all", "first", "last"));
                            }
                            break;
                        case "Permanent":
                            permanent = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Separator":
                            separator = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "System":
                            system = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("env", action, name, part, system.ToString());
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (Wix.Environment.PartType.NotSet != partType)
            {
                if ("+" == action)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Part", "Action", "create"));
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

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Environment, id);
                row.Set(1, String.Concat(action, uninstall, system ? "*" : String.Empty, name));
                row.Set(2, text);
                row.Set(3, componentId);
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
                            id = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = CompilerConstants.IllegalInteger;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Error);
                row.Set(0, id);
                row.Set(1, Common.GetInnerText(node)); // TODO: *
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
                            extension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            YesNoType extensionAdvertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if ((YesNoType.No == advertise && YesNoType.Yes == extensionAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == extensionAdvertise))
                            {
                                this.Core.Write(ErrorMessages.AdvertiseStateMustMatch(sourceLineNumbers, extensionAdvertise.ToString(), advertise.ToString()));
                            }
                            advertise = extensionAdvertise;
                            break;
                        case "ContentType":
                            mime = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ProgId", progId }, { "ComponentId", componentId } };
                    this.Core.ParseExtensionAttribute(node, attrib, context);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (YesNoType.Yes == advertise)
            {
                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Extension);
                    row.Set(0, extension);
                    row.Set(1, componentId);
                    row.Set(2, progId);
                    row.Set(3, mime);
                    row.Set(4, Guid.Empty.ToString("B"));

                    this.Core.EnsureTable(sourceLineNumbers, "Verb");
                }
            }
            else if (YesNoType.No == advertise)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), String.Empty, progId, componentId); // Extension
                if (null != mime)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), "Content Type", mime, componentId); // Extension's MIME ContentType
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

            //int bits = MsiInterop.MsidbFileAttributesVital;
            bool readOnly = false;
            bool checksum = false;
            bool? compressed = null;
            bool hidden = false;
            bool system = false;
            bool vital = true; // assume all files are vital.

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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Assembly":
                            string assemblyValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "Assembly", assemblyValue, "no", "win32", ".net"));
                                        break;
                                }
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", assemblyApplication);
                            break;
                        case "AssemblyManifest":
                            assemblyManifest = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", assemblyManifest);
                            break;
                        case "BindPath":
                            bindPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Checksum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                checksum = true;
                                //bits |= MsiInterop.MsidbFileAttributesChecksum;
                            }
                            break;
                        case "CompanionFile":
                            companionFile = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", companionFile);
                            break;
                        case "Compressed":
                            YesNoDefaultType compressedValue = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            if (YesNoDefaultType.Yes == compressedValue)
                            {
                                compressed = true;
                                //bits |= MsiInterop.MsidbFileAttributesCompressed;
                            }
                            else if (YesNoDefaultType.No == compressedValue)
                            {
                                compressed = false;
                                //bits |= MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            break;
                        case "DefaultLanguage":
                            defaultLanguage = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultSize":
                            defaultSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "DefaultVersion":
                            defaultVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DiskId":
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "FontTitle":
                            fontTitle = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                hidden = true;
                                //bits |= MsiInterop.MsidbFileAttributesHidden;
                            }
                            break;
                        case "KeyPath":
                            keyPath = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "PatchGroup":
                            patchGroup = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "PatchIgnore":
                            patchIgnore = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PatchWholeFile":
                            patchIncludeWholeFile = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PatchAllowIgnoreOnError":
                            patchAllowIgnoreOnError = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ProcessorArchitecture":
                            string procArchValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "ProcessorArchitecture", procArchValue, "msil", "x86", "x64", "ia64"));
                                        break;
                                }
                            }
                            break;
                        case "ReadOnly":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                readOnly = true;
                                //bits |= MsiInterop.MsidbFileAttributesReadOnly;
                            }
                            break;
                        case "SelfRegCost":
                            selfRegCost = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Source":
                            source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            sourceSet = true;
                            break;
                        case "System":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                system = true;
                                //bits |= MsiInterop.MsidbFileAttributesSystem;
                            }
                            break;
                        case "TrueType":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                fontTitle = String.Empty;
                            }
                            break;
                        case "Vital":
                            YesNoType isVital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == isVital)
                            {
                                vital = true;
                                //bits |= MsiInterop.MsidbFileAttributesVital;
                            }
                            else if (YesNoType.No == isVital)
                            {
                                vital = false;
                                //bits &= ~MsiInterop.MsidbFileAttributesVital;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != companionFile)
            {
                // the companion file cannot be the key path of a component
                if (YesNoType.Yes == keyPath)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "CompanionFile", "KeyPath", "yes"));
                }
            }

            if (sourceSet && !source.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) && null == name)
            {
                name = Path.GetFileName(source);
                if (!this.Core.IsValidLongFilename(name, false))
                {
                    this.Core.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            // generate a short file name
            if (null == shortName && (null != name && !this.Core.IsValidShortFilename(name, false)))
            {
                shortName = this.Core.CreateShortName(name, true, false, node.Name.LocalName, directoryId);
                generatedShortFileName = true;
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("fil", directoryId, name ?? shortName);
            }

            if (!this.compilingModule && CompilerConstants.IntegerNotSet == diskId)
            {
                diskId = 1; // default to first Media
            }

            if (null != defaultVersion && null != companionFile)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DefaultVersion", "CompanionFile", companionFile));
            }

            if (FileAssemblyType.NotAnAssembly == assemblyType)
            {
                if (null != assemblyManifest)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", "AssemblyManifest"));
                }

                if (null != assemblyApplication)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", "AssemblyApplication"));
                }
            }
            else
            {
                if (FileAssemblyType.Win32Assembly == assemblyType && null == assemblyManifest)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AssemblyManifest", "Assembly", "win32"));
                }

                // allow "*" guid components to omit explicit KeyPath as they can have only one file and therefore this file is the keypath
                if (YesNoType.Yes != keyPath && "*" != componentGuid)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", (FileAssemblyType.DotNetAssembly == assemblyType ? ".net" : "win32"), "KeyPath", "yes"));
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
                            this.ParseODBCDriverOrTranslator(child, componentId, id.Id, TupleDefinitionType.ODBCDriver);
                            break;
                        case "ODBCTranslator":
                            this.ParseODBCDriverOrTranslator(child, componentId, id.Id, TupleDefinitionType.ODBCTranslator);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "FileId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }


            if (!this.Core.EncounteredError)
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

                var fileRow = (FileTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.File, id);
                fileRow.Component_ = componentId;
                //fileRow.FileName = GetMsiFilenameValue(shortName, name);
                fileRow.ShortFileName = shortName;
                fileRow.LongFileName = name;
                fileRow.FileSize = defaultSize;
                if (null != companionFile)
                {
                    fileRow.Version = companionFile;
                }
                else if (null != defaultVersion)
                {
                    fileRow.Version = defaultVersion;
                }
                fileRow.Language = defaultLanguage;
                fileRow.ReadOnly = readOnly;
                fileRow.Checksum = checksum;
                fileRow.Compressed = compressed;
                fileRow.Hidden = hidden;
                fileRow.System = system;
                fileRow.Vital = vital;
                // the Sequence row is set in the binder

                var wixFileRow = (WixFileTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixFile, id);
                wixFileRow.AssemblyType = assemblyType;
                wixFileRow.File_AssemblyManifest = assemblyManifest;
                wixFileRow.File_AssemblyApplication = assemblyApplication;
                wixFileRow.Directory_ = directoryId;
                wixFileRow.DiskId = (CompilerConstants.IntegerNotSet == diskId) ? 0 : diskId;
                wixFileRow.Source = new IntermediateFieldPathValue { Path = source };
                wixFileRow.ProcessorArchitecture = procArch;
                wixFileRow.PatchGroup = (CompilerConstants.IntegerNotSet != patchGroup ? patchGroup : -1);
                wixFileRow.Attributes = (generatedShortFileName ? 0x1 : 0x0);
                wixFileRow.PatchAttributes = patchAttributes;

                // Always create a delta patch row for this file since other elements (like Component and Media) may
                // want to add symbol paths to it.
                var deltaPatchFileRow = (WixDeltaPatchFileTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchFile, id);
                deltaPatchFileRow.RetainLengths = protectLengths;
                deltaPatchFileRow.IgnoreOffsets = ignoreOffsets;
                deltaPatchFileRow.IgnoreLengths = ignoreLengths;
                deltaPatchFileRow.RetainOffsets = protectOffsets;

                if (null != symbols)
                {
                    var symbolRow = (WixDeltaPatchSymbolPathsTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchSymbolPaths, id);
                    symbolRow.Type = SymbolPathType.File;
                    symbolRow.SymbolPaths = symbols;
                }

                if (FileAssemblyType.NotAnAssembly != assemblyType)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiAssembly);
                    row.Set(0, componentId);
                    row.Set(1, Guid.Empty.ToString("B"));
                    row.Set(2, assemblyManifest);
                    row.Set(3, assemblyApplication);
                    row.Set(4, (FileAssemblyType.DotNetAssembly == assemblyType) ? 0 : 1);
                }

                if (null != bindPath)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.BindImage);
                    row.Set(0, id.Id);
                    row.Set(1, bindPath);

                    // TODO: technically speaking each of the properties in the "bindPath" should be added as references, but how much do we really care about BindImage?
                }

                if (CompilerConstants.IntegerNotSet != selfRegCost)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.SelfReg);
                    row.Set(0, id.Id);
                    row.Set(1, selfRegCost);
                }

                if (null != fontTitle)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Font);
                    row.Set(0, id.Id);
                    row.Set(1, fontTitle);
                }
            }

            this.Core.CreateSimpleReference(sourceLineNumbers, "Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));

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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "MinVersion":
                            minVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxVersion":
                            maxVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinSize":
                            minSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "MaxSize":
                            maxSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "MinDate":
                            minDate = this.Core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxDate":
                            maxDate = this.Core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            languages = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Using both ShortName and Name will not always work due to a Windows Installer bug.
            if (null != shortName && null != name)
            {
                this.Core.Write(WarningMessages.FileSearchFileNameIssue(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name"));
            }
            else if (null == shortName && null == name) // at least one name must be specified.
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (this.Core.IsValidShortFilename(name, false))
            {
                if (null == shortName)
                {
                    shortName = name;
                    name = null;
                }
                else
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                }
            }

            if (null == id)
            {
                if (String.IsNullOrEmpty(parentSignature))
                {
                    id = this.Core.CreateIdentifier("fs", name ?? shortName);
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
                    this.Core.Write(ErrorMessages.UniqueFileSearchIdRequired(sourceLineNumbers, parentSignature, node.Name.LocalName));
                }
            }
            else if (parentDepth > 1)
            {
                // Otherwise, if the depth > 1 the Id must be absent or the same
                // as the parent DirectorySearch if AssignToProperty is not set.
                if (!isSameId)
                {
                    this.Core.Write(ErrorMessages.IllegalSearchIdForParentDepth(sourceLineNumbers, id.Id, parentSignature));
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Signature, id);
                row.Set(1, name ?? shortName);
                row.Set(2, minVersion);
                row.Set(3, maxVersion);

                if (CompilerConstants.IntegerNotSet != minSize)
                {
                    row.Set(4, minSize);
                }
                if (CompilerConstants.IntegerNotSet != maxSize)
                {
                    row.Set(5, maxSize);
                }
                if (CompilerConstants.IntegerNotSet != minDate)
                {
                    row.Set(6, minDate);
                }
                if (CompilerConstants.IntegerNotSet != maxDate)
                {
                    row.Set(7, maxDate);
                }
                row.Set(8, languages);

                // Create a DrLocator row to associate the file with a directory
                // when a different identifier is specified for the FileSearch.
                if (!isSameId)
                {
                    if (parentDirectorySearch)
                    {
                        // Creates the DrLocator row for the directory search while
                        // the parent DirectorySearch creates the file locator row.
                        row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.DrLocator, new Identifier(AccessModifier.Public, parentSignature, id.Id, String.Empty));
                        row.Set(0, parentSignature);
                        row.Set(1, id.Id);
                    }
                    else
                    {
                        row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.DrLocator, new Identifier(AccessModifier.Public, id.Id, parentSignature, String.Empty));
                        row.Set(0, id.Id);
                        row.Set(1, parentSignature);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // NOTE: Id is not required for Fragments, this is a departure from the normal run of the mill processing.

            this.Core.CreateActiveSection(id, SectionType.Fragment, 0, this.Context.CompilationId);

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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError && null != id)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixFragment);
                row.Set(0, id);
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
                                action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                                if (0 < action.Length)
                                {
                                    Wix.Condition.ActionType actionType;
                                    if (Wix.Condition.TryParseActionType(action, out actionType))
                                    {
                                        action = Compiler.UppercaseFirstChar(action);
                                    }
                                    else
                                    {
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "default", "disable", "enable", "hide", "show"));
                                    }
                                }
                            }
                            else
                            {
                                this.Core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        case "Level":
                            if ("Feature" == parentElementLocalName)
                            {
                                level = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            }
                            else
                            {
                                this.Core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        case "Message":
                            if ("Fragment" == parentElementLocalName || "Product" == parentElementLocalName)
                            {
                                message = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.Core.UnexpectedAttribute(node, attrib);
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // get the condition from the inner text of the element
            condition = this.Core.GetConditionInnerText(node);

            this.Core.ParseForExtensionElements(node);

            // the condition should not be empty
            if (null == condition || 0 == condition.Length)
            {
                condition = null;
                this.Core.Write(ErrorMessages.ConditionExpected(sourceLineNumbers, node.Name.LocalName));
            }

            switch (parentElementLocalName)
            {
                case "Control":
                    if (null == action)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
                    }

                    if (!this.Core.EncounteredError)
                    {
                        var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ControlCondition);
                        row.Set(0, dialog);
                        row.Set(1, id);
                        row.Set(2, action);
                        row.Set(3, condition);
                    }
                    break;
                case "Feature":
                    if (CompilerConstants.IntegerNotSet == level)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Level"));
                        level = CompilerConstants.IllegalInteger;
                    }

                    if (!this.Core.EncounteredError)
                    {
                        var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Condition);
                        row.Set(0, id);
                        row.Set(1, level);
                        row.Set(2, condition);
                    }
                    break;
                case "Fragment":
                case "Product":
                    if (null == message)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Message"));
                    }

                    if (!this.Core.EncounteredError)
                    {
                        var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.LaunchCondition);
                        row.Set(0, condition);
                        row.Set(1, message);
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
            TupleDefinitionType tableName;
            string value = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            string actionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", actionValue, "addLine", "addTag", "createLine", "removeLine", "removeTag"));
                                        break;
                                }
                            }
                            break;
                        case "Directory":
                            directory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Section":
                            section = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
                action = CompilerConstants.IllegalInteger;
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.Core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else // generate a short file name.
                {
                    if (null == shortName)
                    {
                        shortName = this.Core.CreateShortName(name, true, false, node.Name.LocalName, componentId);
                    }
                }
            }

            if (null == section)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Section"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("ini", directory, name ?? shortName, section, key, name);
            }

            this.Core.ParseForExtensionElements(node);

            if (MsiInterop.MsidbIniFileActionRemoveLine == action || MsiInterop.MsidbIniFileActionRemoveTag == action)
            {
                tableName = TupleDefinitionType.RemoveIniFile;
            }
            else
            {
                if (null == value)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
                }

                tableName = TupleDefinitionType.IniFile;
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, tableName, id);
                row.Set(1, GetMsiFilenameValue(shortName, name));
                row.Set(2, directory);
                row.Set(3, section);
                row.Set(4, key);
                row.Set(5, value);
                row.Set(6, action);
                row.Set(7, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Field":
                            field = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Section":
                            section = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "registry"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.Core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.Core.CreateShortName(name, true, false, node.Name.LocalName);
                }
            }

            if (null == section)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Section"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("ini", name, section, key, field.ToString(), type.ToString());
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
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;

                            // directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
                            signature = this.ParseDirectorySearchElement(child, id.Id);
                            break;
                        case "DirectorySearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(childSourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseDirectorySearchRefElement(child, id.Id);
                            break;
                        case "FileSearch":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            signature = this.ParseFileSearchElement(child, id.Id, false, CompilerConstants.IntegerNotSet);
                            id = new Identifier(signature, AccessModifier.Private); // FileSearch signatures override parent signatures
                            break;
                        case "FileSearchRef":
                            if (oneChild)
                            {
                                this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                            }
                            oneChild = true;
                            string newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
                            id = new Identifier(newId, AccessModifier.Private);
                            signature = null;
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.IniLocator, id);
                row.Set(1, GetMsiFilenameValue(shortName, name));
                row.Set(2, section);
                row.Set(3, key);
                if (CompilerConstants.IntegerNotSet != field)
                {
                    row.Set(4, field);
                }
                row.Set(5, type);
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
                            shared = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Component", shared);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == shared)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Shared"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.IsolatedComponent);
                row.Set(0, shared);
                row.Set(1, componentId);
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
                    this.Core.UnexpectedAttribute(node, attrib);
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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

                            if (!this.Core.EncounteredError)
                            {
                                var row = this.Core.CreateRow(sourceLineNumbers, "PatchCertificates" == node.Name.LocalName ? TupleDefinitionType.MsiPatchCertificate : TupleDefinitionType.MsiPackageCertificate);
                                row.Set(0, name);
                                row.Set(1, name);
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (40 < id.Id.Length)
            {
                this.Core.Write(ErrorMessages.StreamNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, id.Id.Length, 40));

                // No need to check for modularization problems since DigitalSignature and thus DigitalCertificate
                // currently have no usage in merge modules.
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiDigitalCertificate, id);
                row.Set(1, sourceFile);
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
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null == certificateId)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "DigitalCertificate"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiDigitalSignature);
                row.Set(0, "Media");
                row.Set(1, diskId);
                row.Set(2, certificateId);
                row.Set(3, sourceFile);
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
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Product", "UpgradeCode", node.Name.LocalName));
            }

            string productVersion = contextValues["ProductVersion"];
            if (String.IsNullOrEmpty(productVersion))
            {
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Product", "Version", node.Name.LocalName));
            }

            string productLanguage = contextValues["ProductLanguage"];

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AllowDowngrades":
                            allowDowngrades = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowSameVersionUpgrades":
                            allowSameVersionUpgrades = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Disallow":
                            blockUpgrades = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DowngradeErrorMessage":
                            downgradeErrorMessage = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisallowUpgradeErrorMessage":
                            disallowUpgradeErrorMessage = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MigrateFeatures":
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesMigrateFeatures;
                            }
                            break;
                        case "IgnoreLanguage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                productLanguage = null;
                            }
                            break;
                        case "IgnoreRemoveFailure":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesIgnoreRemoveFailure;
                            }
                            break;
                        case "RemoveFeatures":
                            removeFeatures = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Schedule":
                            schedule = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!allowDowngrades && String.IsNullOrEmpty(downgradeErrorMessage))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DowngradeErrorMessage", "AllowDowngrades", "yes", true));
            }

            if (allowDowngrades && !String.IsNullOrEmpty(downgradeErrorMessage))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DowngradeErrorMessage", "AllowDowngrades", "yes"));
            }

            if (allowDowngrades && allowSameVersionUpgrades)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "AllowSameVersionUpgrades", "AllowDowngrades", "yes"));
            }

            if (blockUpgrades && String.IsNullOrEmpty(disallowUpgradeErrorMessage))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisallowUpgradeErrorMessage", "Disallow", "yes", true));
            }

            if (!blockUpgrades && !String.IsNullOrEmpty(disallowUpgradeErrorMessage))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DisallowUpgradeErrorMessage", "Disallow", "yes"));
            }

            if (!this.Core.EncounteredError)
            {
                // create the row that performs the upgrade (or downgrade)
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Upgrade);
                row.Set(0, upgradeCode);
                if (allowDowngrades)
                {
                    row.Set(1, "0"); // let any version satisfy
                    // row.Set(2, maximum version; omit so we don't have to fake a version like "255.255.65535";
                    row.Set(3, productLanguage);
                    row.Set(4, options | MsiInterop.MsidbUpgradeAttributesVersionMinInclusive);
                }
                else
                {
                    // row.Set(1, minimum version; skip it so we detect all prior versions.
                    row.Set(2, productVersion);
                    row.Set(3, productLanguage);
                    row.Set(4, allowSameVersionUpgrades ? (options | MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive) : options);
                }

                row.Set(5, removeFeatures);
                row.Set(6, Compiler.UpgradeDetectedProperty);

                // Ensure the action property is secure.
                this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Compiler.UpgradeDetectedProperty, AccessModifier.Public), false, true, false);

                // Add launch condition that blocks upgrades
                if (blockUpgrades)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.LaunchCondition);
                    row.Set(0, Compiler.UpgradePreventedCondition);
                    row.Set(1, disallowUpgradeErrorMessage);
                }

                // now create the Upgrade row and launch conditions to prevent downgrades (unless explicitly permitted)
                if (!allowDowngrades)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Upgrade);
                    row.Set(0, upgradeCode);
                    row.Set(1, productVersion);
                    // row.Set(2, maximum version; skip it so we detect all future versions.
                    row.Set(3, productLanguage);
                    row.Set(4, MsiInterop.MsidbUpgradeAttributesOnlyDetect);
                    // row.Set(5, removeFeatures);
                    row.Set(6, Compiler.DowngradeDetectedProperty);

                    // Ensure the action property is secure.
                    this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Compiler.DowngradeDetectedProperty, AccessModifier.Public), false, true, false);

                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.LaunchCondition);
                    row.Set(0, Compiler.DowngradePreventedCondition);
                    row.Set(1, downgradeErrorMessage);
                }

                // finally, schedule RemoveExistingProducts
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixAction, new Identifier(AccessModifier.Public, "InstallExecuteSequence", "RemoveExistingProducts"));
                row.Set(0, "InstallExecuteSequence");
                row.Set(1, "RemoveExistingProducts");
                // row.Set(2, condition);
                // row.Set(3, sequence);
                row.Set(6, false); // overridable

                switch (schedule)
                {
                    case null:
                    case "afterInstallValidate":
                        // row.Set(4, beforeAction;
                        row.Set(5, "InstallValidate");
                        break;
                    case "afterInstallInitialize":
                        // row.Set(4, beforeAction;
                        row.Set(5, "InstallInitialize");
                        break;
                    case "afterInstallExecute":
                        // row.Set(4, beforeAction;
                        row.Set(5, "InstallExecute");
                        break;
                    case "afterInstallExecuteAgain":
                        // row.Set(4, beforeAction;
                        row.Set(5, "InstallExecuteAgain");
                        break;
                    case "afterInstallFinalize":
                        // row.Set(4, beforeAction;
                        row.Set(5, "InstallFinalize");
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
                            id = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Cabinet":
                            cabinet = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CompressionLevel":
                            string compressionLevelString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < compressionLevelString.Length)
                            {
                                Wix.CompressionLevelType compressionLevelType;
                                if (!Wix.Enums.TryParseCompressionLevelType(compressionLevelString, out compressionLevelType))
                                {
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, compressionLevelString, "high", "low", "medium", "mszip", "none"));
                                }
                                else
                                {
                                    compressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), compressionLevelString, true);
                                }
                            }
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
                            break;
                        case "EmbedCab":
                            embedCab = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Layout":
                        case "src":
                            if (null != layout)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Layout", "src"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Layout"));
                            }
                            layout = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Source":
                            source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = CompilerConstants.IllegalInteger;
            }

            if (YesNoType.IllegalValue != embedCab)
            {
                if (YesNoType.Yes == embedCab)
                {
                    if (null == cabinet)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Cabinet", "EmbedCab", "yes"));
                    }
                    else
                    {
                        if (62 < cabinet.Length)
                        {
                            this.Core.Write(ErrorMessages.MediaEmbeddedCabinetNameTooLong(sourceLineNumbers, node.Name.LocalName, "Cabinet", cabinet, cabinet.Length));
                        }

                        cabinet = String.Concat("#", cabinet);
                    }
                }
                else // external cabinet file
                {
                    // external cabinet files must use 8.3 filenames
                    if (!String.IsNullOrEmpty(cabinet) && !this.Core.IsValidShortFilename(cabinet, false))
                    {
                        // WiX variables in the name will trip the "not a valid 8.3 name" switch, so let them through
                        if (!Common.WixVariableRegex.Match(cabinet).Success)
                        {
                            this.Core.Write(WarningMessages.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "Cabinet", cabinet));
                        }
                    }
                }
            }

            if (null != compressionLevel && null == cabinet)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Cabinet", "CompressionLevel"));
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
                                this.Core.Write(ErrorMessages.SignedEmbeddedCabinet(childSourceLineNumbers));
                            }
                            else if (null == cabinet)
                            {
                                this.Core.Write(ErrorMessages.ExpectedSignedCabinetName(childSourceLineNumbers));
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
                                this.Core.UnexpectedElement(node, child);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }



            // add the row to the section
            if (!this.Core.EncounteredError)
            {
                var mediaRow = (MediaTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Media, new Identifier(id, AccessModifier.Public));
                mediaRow.LastSequence = 0; // this is set in the binder
                mediaRow.DiskPrompt = diskPrompt;
                mediaRow.Cabinet = cabinet;
                mediaRow.VolumeLabel = volumeLabel;
                mediaRow.Source = source;

                // the Source column is only set when creating a patch

                if (null != compressionLevel || null != layout)
                {
                    var row = (WixMediaTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixMedia);
                    row.DiskId_ = id;
                    row.CompressionLevel = compressionLevel;
                    row.Layout = layout;
                }

                if (null != symbols)
                {
                    var symbolRow = (WixDeltaPatchSymbolPathsTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchSymbolPaths);
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
                            string authoredCabinetTemplateValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            if (!String.IsNullOrEmpty(authoredCabinetTemplateValue))
                            {
                                cabinetTemplate = authoredCabinetTemplateValue;
                            }

                            // Create an example cabinet name using the maximum number of cabinets supported, 999.
                            string exampleCabinetName = String.Format(cabinetTemplate, "###");
                            if (!this.Core.IsValidLocIdentifier(exampleCabinetName))
                            {
                                // The example name should not match the authored template since that would nullify the
                                // reason for having multiple cabients. External cabinet files must also be valid file names.
                                if (exampleCabinetName.Equals(authoredCabinetTemplateValue) || !this.Core.IsValidLongFilename(exampleCabinetName, false))
                                {
                                    this.Core.Write(ErrorMessages.InvalidCabinetTemplate(sourceLineNumbers, cabinetTemplate));
                                }
                                else if (!this.Core.IsValidShortFilename(exampleCabinetName, false) && !Common.WixVariableRegex.Match(exampleCabinetName).Success) // ignore short names with wix variables because it rarely works out.
                                {
                                    this.Core.Write(WarningMessages.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "CabinetTemplate", cabinetTemplate));
                                }
                            }
                            break;
                        case "CompressionLevel":
                            compressionLevel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < compressionLevel.Length)
                            {
                                if (!Wix.Enums.TryParseCompressionLevelType(compressionLevel, out compressionLevelType))
                                {
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, compressionLevel, "high", "low", "medium", "mszip", "none"));
                                }
                            }
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
                            this.Core.Write(WarningMessages.ReservedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "EmbedCab":
                            embedCab = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.Write(WarningMessages.ReservedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "MaximumUncompressedMediaSize":
                            maximumUncompressedMediaSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "MaximumCabinetSizeForLargeFileSplitting":
                            maximumCabinetSizeForLargeFileSplitting = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, CompilerCore.MinValueOfMaxCabSizeForLargeFileSplitting, CompilerCore.MaxValueOfMaxCabSizeForLargeFileSplitting);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (YesNoType.IllegalValue != embedCab)
            {
                if (YesNoType.Yes == embedCab)
                {
                    cabinetTemplate = String.Concat("#", cabinetTemplate);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var temporaryMediaRow = (MediaTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Media, new Identifier(1, AccessModifier.Public));

                var mediaTemplateRow = (WixMediaTemplateTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixMediaTemplate);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DiskId":
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                            break;
                        case "FileCompression":
                            fileCompression = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            language = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == language)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            if (CompilerConstants.IntegerNotSet == diskId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "DiskId", "Directory"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixMerge, id);
                row.Set(1, language);
                row.Set(2, directoryId);
                row.Set(3, sourceFile);
                row.Set(4, diskId);
                if (YesNoType.Yes == fileCompression)
                {
                    row.Set(5, true);
                }
                else if (YesNoType.No == fileCompression)
                {
                    row.Set(5, false);
                }
                else // YesNoType.NotSet == fileCompression
                {
                    // and we leave the column null
                }
                row.Set(6, configData);
                row.Set(7, Guid.Empty.ToString("B"));
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
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else // need to hex encode these characters
            {
                name = name.Replace("%", "%25");
                name = name.Replace("=", "%3D");
                name = name.Replace(",", "%2C");
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }
            else // need to hex encode these characters
            {
                value = value.Replace("%", "%25");
                value = value.Replace("=", "%3D");
                value = value.Replace(",", "%2C");
            }

            this.Core.ParseForExtensionElements(node);

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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixMerge", id);
                            break;
                        case "Primary":
                            primary = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Module, id, (YesNoType.Yes == primary));
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
                            advertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Class":
                            classId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ContentType":
                            contentType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Default":
                            returnContentType = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == contentType)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ContentType"));
            }

            // if the advertise state has not been set, default to non-advertised
            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            this.Core.ParseForExtensionElements(node);

            if (YesNoType.Yes == advertise)
            {
                if (YesNoType.Yes != parentAdvertised)
                {
                    this.Core.Write(ErrorMessages.AdvertiseStateMustMatch(sourceLineNumbers, advertise.ToString(), parentAdvertised.ToString()));
                }

                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MIME);
                    row.Set(0, contentType);
                    row.Set(1, extension);
                    row.Set(2, classId);
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (YesNoType.Yes == returnContentType && YesNoType.Yes == parentAdvertised)
                {
                    this.Core.Write(ErrorMessages.CannotDefaultMismatchedAdvertiseStates(sourceLineNumbers));
                }

                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "Extension", String.Concat(".", extension), componentId);
                if (null != classId)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "CLSID", classId, componentId);
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
                            this.activeName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-MODULE-NAME-HERE" == this.activeName)
                            {
                                this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, this.activeName));
                            }
                            else
                            {
                                this.activeName = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "Codepage":
                            codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "Guid":
                            moduleId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            this.Core.Write(WarningMessages.DeprecatedModuleGuidAttribute(sourceLineNumbers));
                            break;
                        case "Language":
                            this.activeLanguage = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == this.activeLanguage)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == version)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidModuleOrBundleVersion(version))
            {
                this.Core.Write(WarningMessages.InvalidModuleOrBundleVersion(sourceLineNumbers, "Module", version));
            }

            try
            {
                this.compilingModule = true; // notice that we are actually building a Merge Module here
                this.Core.CreateActiveSection(this.activeName, SectionType.Module, codepage, this.Context.CompilationId);

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
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.ParseExtensionElement(node, child);
                    }
                }


                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleSignature);
                    row.Set(0, this.activeName);
                    row.Set(1, this.activeLanguage);
                    row.Set(2, version);
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
                            this.activeName = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "AllowMajorVersionMismatches":
                            versionMismatches = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowProductCodeMismatches":
                            productMismatches = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "CleanWorkingFolder":
                            clean = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Codepage":
                            codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "OutputPath":
                            outputPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourceList":
                            sourceList = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SymbolFlags":
                            symbolFlags = String.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, uint.MaxValue));
                            break;
                        case "WholeFilesOnly":
                            wholeFiles = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.CreateActiveSection(this.activeName, SectionType.PatchCreation, codepage, this.Context.CompilationId);

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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "DiskPrompt":
                            diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MediaSrcProp":
                            mediaSrcProp = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SequenceStart":
                            sequenceStart = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, int.MaxValue);
                            break;
                        case "VolumeLabel":
                            volumeLabel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (8 < name.Length) // check the length
                {
                    this.Core.Write(ErrorMessages.FamilyNameTooLong(sourceLineNumbers, node.Name.LocalName, "Name", name, name.Length));
                }
                else // check for illegal characters
                {
                    foreach (char character in name)
                    {
                        if (!Char.IsLetterOrDigit(character) && '_' != character)
                        {
                            this.Core.Write(ErrorMessages.IllegalFamilyName(sourceLineNumbers, node.Name.LocalName, "Name", name));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ImageFamilies);
                row.Set(0, name);
                row.Set(1, mediaSrcProp);
                if (CompilerConstants.IntegerNotSet != diskId)
                {
                    row.Set(2, diskId);
                }

                if (CompilerConstants.IntegerNotSet != sequenceStart)
                {
                    row.Set(3, sequenceStart);
                }
                row.Set(4, diskPrompt);
                row.Set(5, volumeLabel);
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
                            upgrade = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (13 < upgrade.Length)
                            {
                                this.Core.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", upgrade, 13));
                            }
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SourcePatch":
                        case "srcPatch":
                            if (null != sourcePatch)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "srcPatch", "SourcePatch"));
                            }

                            if ("srcPatch" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourcePatch"));
                            }
                            sourcePatch = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == upgrade)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.UpgradedImages);
                row.Set(0, upgrade);
                row.Set(1, sourceFile);
                row.Set(2, sourcePatch);
                row.Set(3, String.Join(";", symbols));
                row.Set(4, family);
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
                            allowIgnoreOnError = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Ignore":
                            ignore = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "WholeFile":
                            wholeFile = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                if (ignore)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.UpgradedFilesToIgnore);
                    row.Set(0, upgrade);
                    row.Set(1, file);
                }
                else
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.UpgradedFiles_OptionalData);
                    row.Set(0, upgrade);
                    row.Set(1, file);
                    row.Set(2, String.Join(";", symbols));
                    row.Set(3, allowIgnoreOnError ? 1 : 0);
                    row.Set(4, wholeFile ? 1 : 0);
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
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (target.Length > 13)
                            {
                                this.Core.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", target, 13));
                            }
                            break;
                        case "IgnoreMissingFiles":
                            ignore = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Order":
                            order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "SourceFile":
                        case "src":
                            if (null != sourceFile)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                            }
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Validation":
                            validation = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == target)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.TargetImages);
                row.Set(0, target);
                row.Set(1, sourceFile);
                row.Set(2, symbols);
                row.Set(3, upgrade);
                row.Set(4, order);
                row.Set(5, validation);
                row.Set(6, ignore ? 1 : 0);
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
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.TargetFiles_OptionalData);
                row.Set(0, target);
                row.Set(1, file);
                row.Set(2, symbols);
                row.Set(3, ignoreOffsets);
                row.Set(4, ignoreLengths);

                if (null != protectOffsets)
                {
                    row.Set(5, protectOffsets);

                    var row2 = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.FamilyFileRanges);
                    row2.Set(0, family);
                    row2.Set(1, file);
                    row2.Set(2, protectOffsets);
                    row2.Set(3, protectLengths);
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
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Order":
                            order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "Source":
                        case "src":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "Source"));
                            }

                            if ("src" == attrib.Name.LocalName)
                            {
                                this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Source"));
                            }
                            source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (null == source)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Source"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ExternalFiles);
                row.Set(0, family);
                row.Set(1, file);
                row.Set(2, source);
                row.Set(3, symbols);
                row.Set(4, ignoreOffsets);
                row.Set(5, ignoreLengths);
                if (null != protectOffsets)
                {
                    row.Set(6, protectOffsets);
                }

                if (CompilerConstants.IntegerNotSet != order)
                {
                    row.Set(7, order);
                }

                if (null != protectOffsets)
                {
                    var row2 = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.FamilyFileRanges);
                    row2.Set(0, family);
                    row2.Set(1, file);
                    row2.Set(2, protectOffsets);
                    row2.Set(3, protectLengths);
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
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null == protectOffsets || null == protectLengths)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "ProtectRange"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.FamilyFileRanges);
                row.Set(0, family);
                row.Set(1, file);
                row.Set(2, protectOffsets);
                row.Set(3, protectLengths);
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
                            length = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Offset":
                            offset = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == length)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Length"));
            }

            if (null == offset)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Offset"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Company":
                            company = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (patch)
            {
                // /Patch/PatchProperty goes directly into MsiPatchMetadata table
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                row.Set(0, company);
                row.Set(1, name);
                row.Set(2, value);
            }
            else
            {
                if (null != company)
                {
                    this.Core.Write(ErrorMessages.UnexpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
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
                            family = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "TargetImage"));
                            }
                            target = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Target":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "TargetImage", "ProductCode"));
                            }
                            this.Core.Write(WarningMessages.DeprecatedPatchSequenceTargetAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetImage":
                            if (null != target)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "ProductCode"));
                            }
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "TargetImages", target);
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Supersede":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == family)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PatchFamily"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchSequence);
                row.Set(0, family);
                row.Set(1, target);
                if (!String.IsNullOrEmpty(sequence))
                {
                    row.Set(2, sequence);
                }
                row.Set(3, attributes);
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
                            id = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (id.Length > 0 && "*" != id)
                            {
                                id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            replace = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                                this.Core.Write(ErrorMessages.IllegalAttributeValueWhenNested(sourceLineNumbers, child.Name.LocalName, "Id", id, node.Name.LocalName));
                            }
                            else
                            {
                                targetProductCodes.Add(id);
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                // By default, target ProductCodes should be added.
                if (!replace)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchTarget);
                    row.Set(0, "*");
                }

                foreach (string targetProductCode in targetProductCodes)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchTarget);
                    row.Set(0, targetProductCode);
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
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == path)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            patchId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Codepage":
                            codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowMajorVersionMismatches":
                            ////versionMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "AllowProductCodeMismatches":
                            ////productMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "AllowRemoval":
                            allowRemoval = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "Classification":
                            classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ClientPatchId":
                            clientPatchId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Comments":
                            comments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinorUpdateTargetRTM":
                            minorUpdateTargetRTM = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "MoreInfoURL":
                            moreInfoUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OptimizedInstallMode":
                            optimizedInstallMode = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetProductName":
                            targetProductName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ApiPatchingSymbolNoImagehlpFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_NO_IMAGEHLP : 0;
                            break;
                        case "ApiPatchingSymbolNoFailuresFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_NO_FAILURES : 0;
                            break;
                        case "ApiPatchingSymbolUndecoratedTooFlag":
                            apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlagsType.PATCH_SYMBOL_UNDECORATED_TOO : 0;
                            break;
                        case "OptimizePatchSizeForLargeFiles":
                            optimizePatchSizeForLargeFiles = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            if (null == classification)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }
            if (null == clientPatchId)
            {
                clientPatchId = String.Concat("_", new Guid(patchId).ToString("N", CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture));
            }
            if (null == description)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }
            if (null == displayName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }
            if (null == manufacturer)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            this.Core.CreateActiveSection(this.activeName, SectionType.Patch, codepage, this.Context.CompilationId);

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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var patchIdRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchId);
                patchIdRow.Set(0, patchId);
                patchIdRow.Set(1, clientPatchId);
                patchIdRow.Set(2, optimizePatchSizeForLargeFiles);
                patchIdRow.Set(3, apiPatchingSymbolFlags);

                if (allowRemoval)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "AllowRemoval");
                    row.Set(2, allowRemoval ? "1" : "0");
                }

                if (null != classification)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "Classification");
                    row.Set(2, classification);
                }

                // always generate the CreationTimeUTC
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "CreationTimeUTC");
                    row.Set(2, DateTime.UtcNow.ToString("MM-dd-yy HH:mm", CultureInfo.InvariantCulture));
                }

                if (null != description)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "Description");
                    row.Set(2, description);
                }

                if (null != displayName)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "DisplayName");
                    row.Set(2, displayName);
                }

                if (null != manufacturer)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "ManufacturerName");
                    row.Set(2, manufacturer);
                }

                if (YesNoType.NotSet != minorUpdateTargetRTM)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "MinorUpdateTargetRTM");
                    row.Set(2, YesNoType.Yes == minorUpdateTargetRTM ? "1" : "0");
                }

                if (null != moreInfoUrl)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "MoreInfoURL");
                    row.Set(2, moreInfoUrl);
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "OptimizeCA");
                    row.Set(2, optimizeCA.ToString(CultureInfo.InvariantCulture));
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "OptimizedInstallMode");
                    row.Set(2, YesNoType.Yes == optimizedInstallMode ? "1" : "0");
                }

                if (null != targetProductName)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchMetadata);
                    row.Set(1, "TargetProductName");
                    row.Set(2, targetProductName);
                }

                if (null != comments)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchMetadata);
                    row.Set(0, "Comments");
                    row.Set(1, comments);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Supersede":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidProductVersion(version))
            {
                this.Core.Write(ErrorMessages.InvalidProductVersion(sourceLineNumbers, version));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiPatchSequence, id);
                row.Set(1, productCode);
                row.Set(2, version);
                row.Set(3, attributes);

                if (ComplexReferenceParentType.Unknown != parentType)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, id.Id, ComplexReferenceParentType.Patch == parentType);
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
                    this.Core.UnexpectedAttribute(node, attrib);
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            // Always warn when using the All element.
            this.Core.Write(WarningMessages.AllChangesIncludedInPatch(sourceLineNumbers));

            if (!this.Core.EncounteredError)
            {
                this.Core.CreatePatchFamilyChildReference(sourceLineNumbers, "*", "*");
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.CreatePatchFamilyChildReference(sourceLineNumbers, tableName, id);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if (27 < id.Id.Length)
            {
                this.Core.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", id.Id, 27));
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            else
                            {
                                this.ParseValidateElement(child, ref validationFlags);
                                parsedValidate = true;
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixPatchBaseline, id);
                row.Set(1, diskId);
                row.Set(2, (int)validationFlags);
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateProduct;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateProduct;
                            }
                            break;
                        case "ProductLanguage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateLanguage;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateLanguage;
                            }
                            break;
                        case "ProductVersion":
                            string check = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Version", check, "Major", "Minor", "Update"));
                                    break;
                            }
                            break;
                        case "ProductVersionOperator":
                            string op = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Operator", op, "Lesser", "LesserOrEqual", "Equal", "GreaterOrEqual", "Greater"));
                                    break;
                            }
                            break;
                        case "UpgradeCode":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ValidateUpgradeCode;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ValidateUpgradeCode;
                            }
                            break;
                        case "IgnoreAddExistingRow":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorAddExistingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorAddExistingRow;
                            }
                            break;
                        case "IgnoreAddExistingTable":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorAddExistingTable;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorAddExistingTable;
                            }
                            break;
                        case "IgnoreDeleteMissingRow":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorDeleteMissingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorDeleteMissingRow;
                            }
                            break;
                        case "IgnoreDeleteMissingTable":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorDeleteMissingTable;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorDeleteMissingTable;
                            }
                            break;
                        case "IgnoreUpdateMissingRow":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorUpdateMissingRow;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorUpdateMissingRow;
                            }
                            break;
                        case "IgnoreChangingCodePage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                validationFlags |= TransformFlags.ErrorChangeCodePage;
                            }
                            else
                            {
                                validationFlags &= ~TransformFlags.ErrorChangeCodePage;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Properties);
                row.Set(0, name);
                row.Set(1, value);
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
                            requiredId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "RequiredLanguage":
                            requiredLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RequiredVersion":
                            requiredVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == requiredId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredId"));
                requiredId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet == requiredLanguage)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredLanguage"));
                requiredLanguage = CompilerConstants.IllegalInteger;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleDependency);
                row.Set(0, this.activeName);
                row.Set(1, this.activeLanguage);
                row.Set(2, requiredId);
                row.Set(3, requiredLanguage.ToString(CultureInfo.InvariantCulture));
                row.Set(4, requiredVersion);
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
                            excludedId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ExcludeExceptLanguage":
                            excludeExceptLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ExcludeLanguage":
                            excludeLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ExcludedMaxVersion":
                            excludedMaxVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ExcludedMinVersion":
                            excludedMinVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == excludedId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ExcludedId"));
                excludedId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet != excludeExceptLanguage && CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                this.Core.Write(ErrorMessages.IllegalModuleExclusionLanguageAttributes(sourceLineNumbers));
            }
            else if (CompilerConstants.IntegerNotSet != excludeExceptLanguage)
            {
                excludedLanguageField = Convert.ToString(-excludeExceptLanguage, CultureInfo.InvariantCulture);
            }
            else if (CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                excludedLanguageField = Convert.ToString(excludeLanguage, CultureInfo.InvariantCulture);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleExclusion);
                row.Set(0, this.activeName);
                row.Set(1, this.activeLanguage);
                row.Set(2, excludedId);
                row.Set(3, excludedLanguageField);
                row.Set(4, excludedMinVersion);
                row.Set(5, excludedMaxVersion);
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
                            name = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ContextData":
                            contextData = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultValue":
                            defaultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Format":
                            string formatStr = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Format", formatStr, "Text", "Key", "Integer", "Bitfield"));
                                        break;
                                }
                            }
                            break;
                        case "HelpKeyword":
                            helpKeyword = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HelpLocation":
                            helpLocation = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyNoOrphan":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbMsmConfigurableOptionKeyNoOrphan;
                            }
                            break;
                        case "NonNullable":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbMsmConfigurableOptionNonNullable;
                            }
                            break;
                        case "Type":
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                name = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet == format)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Format"));
                format = CompilerConstants.IllegalInteger;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleConfiguration);
                row.Set(0, name);
                row.Set(1, format);
                row.Set(2, type);
                row.Set(3, contextData);
                row.Set(4, defaultValue);
                row.Set(5, attributes);
                row.Set(6, displayName);
                row.Set(7, description);
                row.Set(8, helpLocation);
                row.Set(9, helpKeyword);
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
                            column = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Row":
                            rowKeys = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Table":
                            table = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == column)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Column"));
                column = String.Empty;
            }

            if (null == table)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Table"));
                table = String.Empty;
            }

            if (null == rowKeys)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Row"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleSubstitution);
                row.Set(0, table);
                row.Set(1, rowKeys);
                row.Set(2, column);
                row.Set(3, value);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ModuleIgnoreTable);
                row.Set(0, id);
            }
        }

        /// <summary>
        /// Parses an odbc driver or translator element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Default identifer for driver/translator file.</param>
        /// <param name="table">Table we're processing for.</param>
        private void ParseODBCDriverOrTranslator(XElement node, string componentId, string fileId, TupleDefinitionType tableName)
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            driver = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", driver);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SetupFile":
                            setup = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", setup);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("odb", name, fileId, setup);
            }

            // drivers have a few possible children
            if (TupleDefinitionType.ODBCDriver == tableName)
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
                                this.ParseODBCProperty(child, id.Id, TupleDefinitionType.ODBCAttribute);
                                break;
                            default:
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.ParseExtensionElement(node, child);
                    }
                }
            }
            else
            {
                this.Core.ParseForExtensionElements(node);
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, tableName, id);
                row.Set(1, componentId);
                row.Set(2, name);
                row.Set(3, driver);
                row.Set(4, setup);
            }
        }

        /// <summary>
        /// Parses a Property element underneath an ODBC driver or translator.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Identifier of parent driver or translator.</param>
        /// <param name="tableName">Name of the table to create property in.</param>
        private void ParseODBCProperty(XElement node, string parentId, TupleDefinitionType tableName)
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
                            id = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            propertyValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, tableName);
                row.Set(0, parentId);
                row.Set(1, id);
                row.Set(2, propertyValue);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DriverName":
                            driverName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyPath":
                            keyPath = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Registration":
                            string registrationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Registration", registrationValue, "machine", "user"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == registration)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Registration"));
                registration = CompilerConstants.IllegalInteger;
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("odc", name, driverName, registration.ToString());
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Property":
                            this.ParseODBCProperty(child, id.Id, TupleDefinitionType.ODBCSourceAttribute);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ODBCDataSource, id);
                row.Set(1, componentId);
                row.Set(2, name);
                row.Set(3, driverName);
                row.Set(4, registration);
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
            IntermediateTuple row;
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
                            packageCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, this.compilingProduct);
                            break;
                        case "AdminImage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 4;
                            }
                            break;
                        case "Comments":
                            comments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            // merge modules must always be compressed, so this attribute is invalid
                            if (this.compilingModule)
                            {
                                this.Core.Write(WarningMessages.DeprecatedPackageCompressedAttribute(sourceLineNumbers));
                                // this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Compressed", "Module"));
                            }
                            else if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 2;
                            }
                            break;
                        case "Description":
                            packageName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallPrivileges":
                            string installPrivileges = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installPrivileges, "elevated", "limited"));
                                        break;
                                }
                            }
                            break;
                        case "InstallScope":
                            string installScope = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < installScope.Length)
                            {
                                installScopeSeen = true;
                                Wix.Package.InstallScopeType installScopeType = Wix.Package.ParseInstallScopeType(installScope);
                                switch (installScopeType)
                                {
                                    case Wix.Package.InstallScopeType.perMachine:
                                        {
                                            row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Property, new Identifier("ALLUSERS", AccessModifier.Public));
                                            row.Set(1, "1");
                                        }
                                        break;
                                    case Wix.Package.InstallScopeType.perUser:
                                        sourceBits = sourceBits | 8;
                                        break;
                                    default:
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installScope, "perMachine", "perUser"));
                                        break;
                                }
                            }
                            break;
                        case "InstallerVersion":
                            msiVersion = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Keywords":
                            keywords = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            packageLanguages = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            packageAuthor = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("PUT-COMPANY-NAME-HERE" == packageAuthor)
                            {
                                this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, packageAuthor));
                            }
                            break;
                        case "Platform":
                            if (null != platformValue)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platforms"));
                            }

                            platformValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            Wix.Package.PlatformType platformType = Wix.Package.ParsePlatformType(platformValue);
                            switch (platformType)
                            {
                                case Wix.Package.PlatformType.intel:
                                    this.Core.Write(WarningMessages.DeprecatedAttributeValue(sourceLineNumbers, platformValue, node.Name.LocalName, attrib.Name.LocalName, "x86"));
                                    goto case Wix.Package.PlatformType.x86;
                                case Wix.Package.PlatformType.x86:
                                    platform = "Intel";
                                    break;
                                case Wix.Package.PlatformType.x64:
                                    platform = "x64";
                                    break;
                                case Wix.Package.PlatformType.intel64:
                                    this.Core.Write(WarningMessages.DeprecatedAttributeValue(sourceLineNumbers, platformValue, node.Name.LocalName, attrib.Name.LocalName, "ia64"));
                                    goto case Wix.Package.PlatformType.ia64;
                                case Wix.Package.PlatformType.ia64:
                                    platform = "Intel64";
                                    break;
                                case Wix.Package.PlatformType.arm:
                                    platform = "Arm";
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.InvalidPlatformValue(sourceLineNumbers, platformValue));
                                    break;
                            }
                            break;
                        case "Platforms":
                            if (null != platformValue)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platform"));
                            }

                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Platform"));
                            platformValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            platform = platformValue;
                            break;
                        case "ReadOnly":
                            security = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortNames":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                sourceBits = sourceBits | 1;
                                this.useShortFileNames = true;
                            }
                            break;
                        case "SummaryCodepage":
                            codepage = this.Core.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (installPrivilegeSeen && installScopeSeen)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "InstallPrivileges", "InstallScope"));
            }

            if ((0 != String.Compare(platform, "Intel", StringComparison.OrdinalIgnoreCase)) && 200 > msiVersion)
            {
                msiVersion = 200;
                this.Core.Write(WarningMessages.RequiresMsi200for64bitPackage(sourceLineNumbers));
            }

            if ((0 == String.Compare(platform, "Arm", StringComparison.OrdinalIgnoreCase)) && 500 > msiVersion)
            {
                msiVersion = 500;
                this.Core.Write(WarningMessages.RequiresMsi500forArmPackage(sourceLineNumbers));
            }

            if (null == packageAuthor)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            if (this.compilingModule)
            {
                if (null == packageCode)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                    this.Core.Write(WarningMessages.PackageCodeSet(sourceLineNumbers));
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 1);
                row.Set(1, codepage);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 2);
                row.Set(1, "Installation Database");

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 3);
                row.Set(1, packageName);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 4);
                row.Set(1, packageAuthor);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 5);
                row.Set(1, keywords);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 6);
                row.Set(1, comments);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 7);
                row.Set(1, String.Format(CultureInfo.InvariantCulture, "{0};{1}", platform, packageLanguages));

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 9);
                row.Set(1, packageCode);

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 14);
                row.Set(1, msiVersion.ToString(CultureInfo.InvariantCulture));

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 15);
                row.Set(1, sourceBits.ToString(CultureInfo.InvariantCulture));

                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 19);
                switch (security)
                {
                    case YesNoDefaultType.No: // no restriction
                        row.Set(1, "0");
                        break;
                    case YesNoDefaultType.Default: // read-only recommended
                        row.Set(1, "2");
                        break;
                    case YesNoDefaultType.Yes: // read-only enforced
                        row.Set(1, "4");
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
                            allowRemoval = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Classification":
                            classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreationTimeUTC":
                            creationTimeUtc = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ManufacturerName":
                            manufacturerName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinorUpdateTargetRTM":
                            minorUpdateTargetRTM = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MoreInfoURL":
                            moreInfoUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OptimizedInstallMode":
                            optimizedInstallMode = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetProductName":
                            targetProductName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (YesNoType.NotSet == allowRemoval)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AllowRemoval"));
            }

            if (null == classification)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }

            if (null == description)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (null == displayName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }

            if (null == manufacturerName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ManufacturerName"));
            }

            if (null == moreInfoUrl)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "MoreInfoURL"));
            }

            if (null == targetProductName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetProductName"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                if (YesNoType.NotSet != allowRemoval)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "AllowRemoval");
                    row.Set(2, YesNoType.Yes == allowRemoval ? "1" : "0");
                }

                if (null != classification)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "Classification");
                    row.Set(2, classification);
                }

                if (null != creationTimeUtc)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "CreationTimeUTC");
                    row.Set(2, creationTimeUtc);
                }

                if (null != description)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "Description");
                    row.Set(2, description);
                }

                if (null != displayName)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "DisplayName");
                    row.Set(2, displayName);
                }

                if (null != manufacturerName)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "ManufacturerName");
                    row.Set(2, manufacturerName);
                }

                if (null != minorUpdateTargetRTM)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "MinorUpdateTargetRTM");
                    row.Set(2, minorUpdateTargetRTM);
                }

                if (null != moreInfoUrl)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "MoreInfoURL");
                    row.Set(2, moreInfoUrl);
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "OptimizeCA");
                    row.Set(2, optimizeCA.ToString(CultureInfo.InvariantCulture));
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "OptimizedInstallMode");
                    row.Set(2, YesNoType.Yes == optimizedInstallMode ? "1" : "0");
                }

                if (null != targetProductName)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                    row.Set(1, "TargetProductName");
                    row.Set(2, targetProductName);
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
                            company = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == company)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.PatchMetadata);
                row.Set(0, company);
                row.Set(1, property);
                row.Set(2, value);
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipAssignment;
                            }
                            break;
                        case "SkipImmediate":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipImmediate;
                            }
                            break;
                        case "SkipDeferred":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                optimizeCA |= OptimizeCA.SkipDeferred;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Comments":
                            comments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Description":
                            packageName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Keywords":
                            keywords = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Languages":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Manufacturer":
                            packageAuthor = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Platforms":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "ReadOnly":
                            security = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortNames":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "SummaryCodepage":
                            codepage = this.Core.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                // PID_CODEPAGE
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 1);
                row.Set(1, codepage);

                // PID_TITLE
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 2);
                row.Set(1, "Patch");

                // PID_SUBJECT
                if (null != packageName)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                    row.Set(0, 3);
                    row.Set(1, packageName);
                }

                // PID_AUTHOR
                if (null != packageAuthor)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                    row.Set(0, 4);
                    row.Set(1, packageAuthor);
                }

                // PID_KEYWORDS
                if (null != keywords)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                    row.Set(0, 5);
                    row.Set(1, keywords);
                }

                // PID_COMMENTS
                if (null != comments)
                {
                    row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                    row.Set(0, 6);
                    row.Set(1, comments);
                }

                // PID_PAGECOUNT
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 14);
                row.Set(1, msiVersion.ToString(CultureInfo.InvariantCulture));

                // PID_WORDCOUNT
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 15);
                row.Set(1, "0");

                // PID_SECURITY
                row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType._SummaryInformation);
                row.Set(0, 19);
                switch (security)
                {
                    case YesNoDefaultType.No: // no restriction
                        row.Set(1, "0");
                        break;
                    case YesNoDefaultType.Default: // read-only recommended
                        row.Set(1, "2");
                        break;
                    case YesNoDefaultType.Yes: // read-only enforced
                        row.Set(1, "4");
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

            this.Core.Write(WarningMessages.DeprecatedIgnoreModularizationElement(sourceLineNumbers));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            // this is actually not used
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixSuppressModularization);
                row.Set(0, name);
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
                    this.Core.UnexpectedElement(node.Parent, node);
                    return; // stop processing this element since no valid permissions are available
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Domain":
                            domain = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                            YesNoType attribValue = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.Core.TrySetBitFromName(Common.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.Core.TrySetBitFromName(Common.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.Core.TrySetBitFromName(specialPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.Core.UnexpectedAttribute(node, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            permission = this.Core.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "User"));
            }

            if (int.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.Core.Write(ErrorMessages.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.LockPermissions);
                row.Set(0, objectId);
                row.Set(1, tableName);
                row.Set(2, domain);
                row.Set(3, user);
                row.Set(4, permission);
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
                    this.Core.UnexpectedElement(node.Parent, node);
                    return; // stop processing this element since nothing will be valid.
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Sddl":
                            sddl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == sddl)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Sddl"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("pme", objectId, tableName, sddl);
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }

                            condition = this.ParseConditionElement(child, node.Name.LocalName, null, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiLockPermissionsEx, id);
                row.Set(1, objectId);
                row.Set(2, tableName);
                row.Set(3, sddl);
                row.Set(4, condition);
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
                            productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Codepage":
                            codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            this.activeLanguage = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Manufacturer":
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.MustHaveNonWhitespaceCharacters);
                            if ("PUT-COMPANY-NAME-HERE" == manufacturer)
                            {
                                this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, manufacturer));
                            }
                            break;
                        case "Name":
                            this.activeName = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.MustHaveNonWhitespaceCharacters);
                            if ("PUT-PRODUCT-NAME-HERE" == this.activeName)
                            {
                                this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, this.activeName));
                            }
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version": // if the attribute is valid version, use the attribute value as is (so "1.0000.01.01" would *not* get translated to "1.0.1.1").
                            string verifiedVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            if (!String.IsNullOrEmpty(verifiedVersion))
                            {
                                version = attrib.Value;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == productCode)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == this.activeLanguage)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == manufacturer)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == upgradeCode)
            {
                this.Core.Write(WarningMessages.MissingUpgradeCode(sourceLineNumbers));
            }

            if (null == version)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidProductVersion(version))
            {
                this.Core.Write(ErrorMessages.InvalidProductVersion(sourceLineNumbers, version));
            }

            if (this.Core.EncounteredError)
            {
                return;
            }

            try
            {
                this.compilingProduct = true;
                this.Core.CreateActiveSection(productCode, SectionType.Product, codepage, this.Context.CompilationId);

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
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.ParseExtensionElement(node, child);
                    }
                }

                if (!this.Core.EncounteredError)
                {
                    if (null != symbols)
                    {
                        var symbolRow = (WixDeltaPatchSymbolPathsTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixDeltaPatchSymbolPaths);
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
                            progId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            progIdAdvertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Icon":
                            icon = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "IconIndex":
                            iconIndex = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "NoOpen":
                            noOpen = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if ((YesNoType.No == advertise && YesNoType.Yes == progIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == progIdAdvertise))
            {
                this.Core.Write(ErrorMessages.AdvertiseStateMustMatch(sourceLineNumbers, advertise.ToString(), progIdAdvertise.ToString()));
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
                this.Core.Write(ErrorMessages.VersionIndependentProgIdsCannotHaveIcons(sourceLineNumbers));
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
                                this.Core.Write(ErrorMessages.ProgIdNestedTooDeep(childSourceLineNumbers));
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (YesNoType.Yes == advertise)
            {
                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ProgId);
                    row.Set(0, progId);
                    row.Set(1, parent);
                    row.Set(2, classId);
                    row.Set(3, description);
                    if (null != icon)
                    {
                        row.Set(4, icon);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                    }

                    if (CompilerConstants.IntegerNotSet != iconIndex)
                    {
                        row.Set(5, iconIndex);
                    }

                    this.Core.EnsureTable(sourceLineNumbers, "Class");
                }
            }
            else if (YesNoType.No == advertise)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, progId, String.Empty, description, componentId);
                if (null != classId)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CLSID"), String.Empty, classId, componentId);
                    if (null != parent)   // if this is a version independent ProgId
                    {
                        if (YesNoType.Yes == firstProgIdForClass)
                        {
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\VersionIndependentProgID"), String.Empty, progId, componentId);
                        }

                        this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CurVer"), String.Empty, parent, componentId);
                    }
                    else
                    {
                        if (YesNoType.Yes == firstProgIdForClass)
                        {
                            this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\ProgID"), String.Empty, progId, componentId);
                        }
                    }
                }

                if (null != icon)   // ProgId's Default Icon
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "File", icon);

                    icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                    if (CompilerConstants.IntegerNotSet != iconIndex)
                    {
                        icon = String.Concat(icon, ",", iconIndex);
                    }

                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\DefaultIcon"), String.Empty, icon, componentId);
                }
            }

            if (null != noOpen)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, progId, "NoOpen", noOpen, componentId); // ProgId NoOpen name
            }

            // raise an error for an orphaned ProgId
            if (YesNoType.Yes == advertise && !foundExtension && null == parent && null == classId)
            {
                this.Core.Write(WarningMessages.OrphanedProgId(sourceLineNumbers, progId));
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Admin":
                            admin = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ComplianceCheck":
                            complianceCheck = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Hidden":
                            hidden = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Secure":
                            secure = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressModularization":
                            suppressModularization = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }
            else if ("ProductID" == id.Id)
            {
                this.Core.Write(WarningMessages.ProductIdAuthored(sourceLineNumbers));
            }
            else if ("SecureCustomProperties" == id.Id || "AdminProperties" == id.Id || "MsiHiddenProperties" == id.Id)
            {
                this.Core.Write(ErrorMessages.CannotAuthorSpecialProperties(sourceLineNumbers, id.Id));
            }

            string innerText = this.Core.GetTrimmedInnerText(node);
            if (null != value)
            {
                // cannot specify both the value attribute and inner text
                if (!String.IsNullOrEmpty(innerText))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }
            else // value attribute not specified, use inner text if any.
            {
                value = innerText;
            }

            if ("ErrorDialog" == id.Id)
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "Dialog", value);
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
                this.Core.Write(ErrorMessages.SearchElementRequiredWithAttribute(sourceLineNumbers, node.Name.LocalName, "ComplianceCheck", "yes"));
            }

            foreach (string sig in signatures)
            {
                if (complianceCheck && !this.Core.EncounteredError)
                {
                    this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CCPSearch, new Identifier(sig, AccessModifier.Private));
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
                    this.Core.Write(WarningMessages.PropertyUseless(sourceLineNumbers, id.Id));
                }
                else // there is a value and/or a flag set, do that.
                {
                    this.AddProperty(sourceLineNumbers, id, value, admin, secure, hidden, false);
                }
            }

            if (!this.Core.EncounteredError && YesNoType.Yes == suppressModularization)
            {
                this.Core.Write(WarningMessages.PropertyModularizationSuppressed(sourceLineNumbers));

                this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixSuppressModularization, id);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            this.Core.Write(WarningMessages.DeprecatedRegistryKeyActionAttribute(sourceLineNumbers));
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "create", "createAndRemoveOnUninstall", "none"));
                                        break;
                                }
                            }
                            break;
                        case "ForceCreateOnInstall":
                            forceCreateOnInstall = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ForceDeleteOnUninstall":
                            forceDeleteOnUninstall = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (null != parentKey)
                            {
                                key = Path.Combine(parentKey, key);
                            }
                            break;
                        case "Root":
                            if (CompilerConstants.IntegerNotSet != root)
                            {
                                this.Core.Write(ErrorMessages.RegistryRootInvalid(sourceLineNumbers));
                            }

                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            string name = forceCreateOnInstall ? (forceDeleteOnUninstall ? "*" : "+") : (forceDeleteOnUninstall ? "-" : null);

            if (forceCreateOnInstall || forceDeleteOnUninstall) // generates a Registry row, so an Id must be present
            {
                // generate the identifier if it wasn't provided
                if (null == id)
                {
                    id = this.Core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
                }
            }
            else // does not generate a Registry row, so no Id should be present
            {
                if (null != id)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Id", "ForceCreateOnInstall", "ForceDeleteOnUninstall", "yes", true));
                }
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
                root = CompilerConstants.IllegalInteger;
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
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
                                    this.Core.Write(ErrorMessages.ComponentMultipleKeyPaths(sourceLineNumbers, child.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
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
                                    this.Core.Write(ErrorMessages.ComponentMultipleKeyPaths(sourceLineNumbers, child.Name.LocalName, "KeyPath", "yes", "File", "RegistryValue", "ODBCDataSource"));
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
                                this.Core.Write(ErrorMessages.UnexpectedElementWithAttributeValue(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "ForceCreateOnInstall", "yes"));
                            }
                            this.ParsePermissionElement(child, id.Id, "Registry");
                            break;
                        case "PermissionEx":
                            if (!forceCreateOnInstall)
                            {
                                this.Core.Write(ErrorMessages.UnexpectedElementWithAttributeValue(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "ForceCreateOnInstall", "yes"));
                            }
                            this.ParsePermissionExElement(child, id.Id, "Registry");
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "RegistryId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }


            if (!this.Core.EncounteredError && null != name)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Registry, id);
                row.Set(1, root);
                row.Set(2, key);
                row.Set(3, name);
                //row.Set(4, null);
                row.Set(5, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < action.Length)
                            {
                                if (!Wix.RegistryValue.TryParseActionType(action, out actionType))
                                {
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "append", "prepend", "write"));
                                }
                            }
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                            keyPath = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            if (CompilerConstants.IntegerNotSet != root)
                            {
                                this.Core.Write(ErrorMessages.RegistryRootInvalid(sourceLineNumbers));
                            }

                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Type":
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < type.Length)
                            {
                                if (!Wix.RegistryValue.TryParseTypeType(type, out typeType))
                                {
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, type, "binary", "expandable", "integer", "multiString", "string"));
                                }
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.Core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if ((Wix.RegistryValue.ActionType.append == actionType || Wix.RegistryValue.ActionType.prepend == actionType) &&
                Wix.RegistryValue.TypeType.multiString != typeType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Action", action, "Type", "multiString"));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (null == type)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
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
                                this.Core.Write(ErrorMessages.RegistryMultipleValuesWithoutMultiString(sourceLineNumbers, node.Name.LocalName, "Value", child.Name.LocalName, "Type"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "RegistryId", id.Id }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }
            else if (0 == value.Length && ("+" == name || "-" == name || "*" == name)) // prevent accidental authoring of special name values
            {
                this.Core.Write(ErrorMessages.RegistryNameValueIncorrect(sourceLineNumbers, node.Name.LocalName, "Name", name));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Registry, id);
                row.Set(1, root);
                row.Set(2, key);
                row.Set(3, name);
                row.Set(4, value);
                row.Set(5, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < action.Length)
                            {
                                if (!Wix.RemoveRegistryKey.TryParseActionType(action, out actionType))
                                {
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "removeOnInstall", "removeOnUninstall"));
                                }
                            }
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.Core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if (null == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, (Wix.RemoveRegistryKey.ActionType.removeOnUninstall == actionType ? TupleDefinitionType.Registry : TupleDefinitionType.RemoveRegistry), id);
                row.Set(1, root);
                row.Set(2, key);
                row.Set(3, name);
                if (Wix.RemoveRegistryKey.ActionType.removeOnUninstall == actionType) // Registry table
                {
                    //row.Set(4, null);
                    row.Set(5, componentId);
                }
                else // RemoveRegistry table
                {
                    row.Set(4, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Root":
                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // generate the identifier if it wasn't provided
            if (null == id)
            {
                id = this.Core.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), LowercaseOrNull(key), LowercaseOrNull(name));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.RemoveRegistry, id);
                row.Set(1, root);
                row.Set(2, key);
                row.Set(3, name);
                row.Set(4, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, parentDirectory);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, true);
                            break;
                        case "On":
                            Wix.InstallUninstallType onValue = this.Core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
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
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, true);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.Core.IsValidShortFilename(name, true))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.Core.CreateShortName(name, true, true, node.Name.LocalName, componentId);
                }
            }

            if (CompilerConstants.IntegerNotSet == on)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "On"));
                on = CompilerConstants.IllegalInteger;
            }

            if (null != directory && null != property)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "Directory", directory));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("rmf", directory ?? property ?? parentDirectory, LowercaseOrNull(shortName), LowercaseOrNull(name), on.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.RemoveFile, id);
                row.Set(1, componentId);
                row.Set(2, GetMsiFilenameValue(shortName, name));
                if (null != directory)
                {
                    row.Set(3, directory);
                }
                else if (null != property)
                {
                    row.Set(3, property);
                }
                else
                {
                    row.Set(3, parentDirectory);
                }
                row.Set(4, on);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, parentDirectory);
                            break;
                        case "On":
                            Wix.InstallUninstallType onValue = this.Core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
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
                            property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == on)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "On"));
                on = CompilerConstants.IllegalInteger;
            }

            if (null != directory && null != property)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "Directory", directory));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("rmf", directory ?? property ?? parentDirectory, on.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.RemoveFile, id);
                row.Set(1, componentId);
                //row.Set(2, null);
                if (null != directory)
                {
                    row.Set(3, directory);
                }
                else if (null != property)
                {
                    row.Set(3, property);
                }
                else
                {
                    row.Set(3, parentDirectory);
                }
                row.Set(4, on);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                            break;
                        case "RunFromSource":
                            runFromSource = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "RunLocal":
                            runLocal = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("rc", componentId, directoryId);
            }

            if (CompilerConstants.IntegerNotSet == runFromSource)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RunFromSource"));
            }

            if (CompilerConstants.IntegerNotSet == runLocal)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RunLocal"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ReserveCost, id);
                row.Set(1, componentId);
                row.Set(2, directoryId);
                row.Set(3, runLocal);
                row.Set(4, runFromSource);
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
                                    actionName = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.Core.CreateSimpleReference(childSourceLineNumbers, "CustomAction", actionName);
                                }
                                else
                                {
                                    this.Core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "After":
                                if (customAction || showDialog || specialAction || specialStandardAction)
                                {
                                    afterAction = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.Core.CreateSimpleReference(childSourceLineNumbers, "WixAction", sequenceTable, afterAction);
                                }
                                else
                                {
                                    this.Core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Before":
                                if (customAction || showDialog || specialAction || specialStandardAction)
                                {
                                    beforeAction = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.Core.CreateSimpleReference(childSourceLineNumbers, "WixAction", sequenceTable, beforeAction);
                                }
                                else
                                {
                                    this.Core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Dialog":
                                if (showDialog)
                                {
                                    actionName = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
                                    this.Core.CreateSimpleReference(childSourceLineNumbers, "Dialog", actionName);
                                }
                                else
                                {
                                    this.Core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "OnExit":
                                if (customAction || showDialog || specialAction)
                                {
                                    Wix.ExitType exitValue = this.Core.GetAttributeExitValue(childSourceLineNumbers, attrib);
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
                                    this.Core.UnexpectedAttribute(child, attrib);
                                }
                                break;
                            case "Overridable":
                                overridable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, attrib);
                                break;
                            case "Sequence":
                                sequence = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, attrib, 1, short.MaxValue);
                                break;
                            case "Suppress":
                                suppress = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, attrib);
                                break;
                            default:
                                this.Core.UnexpectedAttribute(node, attrib);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.ParseExtensionAttribute(node, attrib);
                    }
                }


                // Get the condition from the inner text of the element.
                condition = this.Core.GetConditionInnerText(child);

                if (customAction && "Custom" == actionName)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Action"));
                }
                else if (showDialog && "Show" == actionName)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Dialog"));
                }

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    if (CompilerConstants.IntegerNotSet != exitSequence)
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "Sequence", "OnExit"));
                    }
                    else if (null != beforeAction || null != afterAction)
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "Sequence", "Before", "After"));
                    }
                }
                else // sequence not specified use OnExit (which may also be not set).
                {
                    sequence = exitSequence;
                }

                if (null != beforeAction && null != afterAction)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name.LocalName, "After", "Before"));
                }
                else if ((customAction || showDialog || specialAction) && !suppress && CompilerConstants.IntegerNotSet == sequence && null == beforeAction && null == afterAction)
                {
                    this.Core.Write(ErrorMessages.NeedSequenceBeforeOrAfter(childSourceLineNumbers, child.Name.LocalName));
                }

                // action that is scheduled to occur before/after itself
                if (beforeAction == actionName)
                {
                    this.Core.Write(ErrorMessages.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name.LocalName, "Before", beforeAction));
                }
                else if (afterAction == actionName)
                {
                    this.Core.Write(ErrorMessages.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name.LocalName, "After", afterAction));
                }

                // normal standard actions cannot be set overridable by the user (since they are overridable by default)
                if (overridable && WindowsInstallerStandard.IsStandardAction(actionName) && !specialAction)
                {
                    this.Core.Write(ErrorMessages.UnexpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Overridable"));
                }

                // suppress cannot be specified at the same time as Before, After, or Sequence
                if (suppress && (null != afterAction || null != beforeAction || CompilerConstants.IntegerNotSet != sequence || overridable))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(childSourceLineNumbers, child.Name.LocalName, "Suppress", "Before", "After", "Sequence", "Overridable"));
                }

                this.Core.ParseForExtensionElements(child);

                // add the row and any references needed
                if (!this.Core.EncounteredError)
                {
                    if (suppress)
                    {
                        var row = this.Core.CreateRow(childSourceLineNumbers, TupleDefinitionType.WixSuppressAction);
                        row.Set(0, sequenceTable);
                        row.Set(1, actionName);
                    }
                    else
                    {
                        var row = this.Core.CreateRow(childSourceLineNumbers, TupleDefinitionType.WixAction, new Identifier(AccessModifier.Public, sequenceTable, actionName));
                        row.Set(0, sequenceTable);
                        row.Set(1, actionName);
                        row.Set(2, condition);
                        if (CompilerConstants.IntegerNotSet != sequence)
                        {
                            row.Set(3, sequence);
                        }
                        row.Set(4, beforeAction);
                        row.Set(5, afterAction);
                        row.Set(6, overridable ? 1 : 0);
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

            this.Core.Write(WarningMessages.ServiceConfigFamilyNotSupported(sourceLineNumbers, node.Name.LocalName));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DelayedAutoStart":
                            delayedAutoStart = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                            failureActionsWhen = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                            YesNoType install = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == install)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventInstall;
                            }
                            break;
                        case "OnReinstall":
                            YesNoType reinstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == reinstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventReinstall;
                            }
                            break;
                        case "OnUninstall":
                            YesNoType uninstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == uninstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventUninstall;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                        case "PreShutdownDelay":
                            preShutdownDelay = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "ServiceName":
                            if (!String.IsNullOrEmpty(serviceName))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ServiceInstall"));
                            }

                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceSid":
                            sid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            string privilege = this.Core.GetTrimmedInnerText(child);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ServiceName"));
            }
            else if (null == id)
            {
                id = this.Core.CreateIdentifierFromFilename(name);
            }

            if (0 == events)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "OnInstall", "OnReinstall", "OnUninstall"));
            }

            if (String.IsNullOrEmpty(delayedAutoStart) && String.IsNullOrEmpty(failureActionsWhen) && String.IsNullOrEmpty(preShutdownDelay) && String.IsNullOrEmpty(requiredPrivileges) && String.IsNullOrEmpty(sid))
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DelayedAutoStart", "FailureActionsWhen", "PreShutdownDelay", "ServiceSid", "RequiredPrivilege"));
            }

            if (!this.Core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(delayedAutoStart))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfig, new Identifier(String.Concat(id.Id, ".DS"), id.Access));
                    row.Set(1, name);
                    row.Set(2, events);
                    row.Set(3, 3);
                    row.Set(4, delayedAutoStart);
                    row.Set(5, componentId);
                }

                if (!String.IsNullOrEmpty(failureActionsWhen))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfig, new Identifier(String.Concat(id.Id, ".FA"), id.Access));
                    row.Set(1, name);
                    row.Set(2, events);
                    row.Set(3, 4);
                    row.Set(4, failureActionsWhen);
                    row.Set(5, componentId);
                }

                if (!String.IsNullOrEmpty(sid))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfig, new Identifier(String.Concat(id.Id, ".SS"), id.Access));
                    row.Set(1, name);
                    row.Set(2, events);
                    row.Set(3, 5);
                    row.Set(4, sid);
                    row.Set(5, componentId);
                }

                if (!String.IsNullOrEmpty(requiredPrivileges))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfig, new Identifier(String.Concat(id.Id, ".RP"), id.Access));
                    row.Set(1, name);
                    row.Set(2, events);
                    row.Set(3, 6);
                    row.Set(4, requiredPrivileges);
                    row.Set(5, componentId);
                }

                if (!String.IsNullOrEmpty(preShutdownDelay))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfig, new Identifier(String.Concat(id.Id, ".PD"), id.Access));
                    row.Set(1, name);
                    row.Set(2, events);
                    row.Set(3, 7);
                    row.Set(4, preShutdownDelay);
                    row.Set(5, componentId);
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

            this.Core.Write(WarningMessages.ServiceConfigFamilyNotSupported(sourceLineNumbers, node.Name.LocalName));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Command":
                            command = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "OnInstall":
                            YesNoType install = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == install)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventInstall;
                            }
                            break;
                        case "OnReinstall":
                            YesNoType reinstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == reinstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventReinstall;
                            }
                            break;
                        case "OnUninstall":
                            YesNoType uninstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (YesNoType.Yes == uninstall)
                            {
                                events |= MsiInterop.MsidbServiceConfigEventUninstall;
                            }
                            break;
                        case "RebootMessage":
                            rebootMessage = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "ResetPeriod":
                            resetPeriod = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "ServiceName":
                            if (!String.IsNullOrEmpty(serviceName))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ServiceInstall"));
                            }

                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                                            action = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
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
                                            delay = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                            break;
                                        default:
                                            this.Core.UnexpectedAttribute(child, childAttrib);
                                            break;
                                    }
                                }
                            }

                            if (String.IsNullOrEmpty(action))
                            {
                                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, child.Name.LocalName, "Action"));
                            }

                            if (String.IsNullOrEmpty(delay))
                            {
                                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, child.Name.LocalName, "Delay"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ServiceName"));
            }
            else if (null == id)
            {
                id = this.Core.CreateIdentifierFromFilename(name);
            }

            if (0 == events)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "OnInstall", "OnReinstall", "OnUninstall"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiServiceConfigFailureActions, id);
                row.Set(1, name);
                row.Set(2, events);
                if (CompilerConstants.IntegerNotSet != resetPeriod)
                {
                    row.Set(3, resetPeriod);
                }
                row.Set(4, rebootMessage ?? "[~]");
                row.Set(5, command ?? "[~]");
                row.Set(6, actions);
                row.Set(7, actionsDelays);
                row.Set(8, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Remove":
                            Wix.InstallUninstallType removeValue = this.Core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
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
                            Wix.InstallUninstallType startValue = this.Core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
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
                            Wix.InstallUninstallType stopValue = this.Core.GetAttributeInstallUninstallValue(sourceLineNumbers, attrib);
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
                            wait = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifierFromFilename(name);
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
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
                            arguments = String.Concat(arguments, this.Core.GetTrimmedInnerText(child));
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ServiceControl, id);
                row.Set(1, name);
                row.Set(2, events);
                row.Set(3, arguments);
                if (YesNoType.NotSet != wait)
                {
                    row.Set(4, YesNoType.Yes == wait ? 1 : 0);
                }
                row.Set(5, componentId);
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
                            dependency = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Group":
                            group = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == dependency)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Account":
                            account = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Arguments":
                            arguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EraseDescription":
                            eraseDescription = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ErrorControl":
                            string errorControlValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, errorControlValue, "ignore", "normal", "critical"));
                                        break;
                                }
                            }
                            break;
                        case "Interactive":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typebits |= MsiInterop.MsidbServiceInstallInteractive;
                            }
                            break;
                        case "LoadOrderGroup":
                            loadOrderGroup = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            password = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Start":
                            string startValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.ValueNotSupported(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, startValue));
                                        break;
                                    default:
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, startValue, "auto", "demand", "disabled"));
                                        break;
                                }
                            }
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.ValueNotSupported(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue));
                                        break;
                                    default:
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, node.Name.LocalName, typeValue, "ownProcess", "shareProcess"));
                                        break;
                                }
                            }
                            break;
                        case "Vital":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                errorbits |= MsiInterop.MsidbServiceInstallErrorControlVital;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (null == id)
            {
                id = this.Core.CreateIdentifierFromFilename(name);
            }

            if (0 == startType)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Start"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "ServiceInstallId", id.Id }, { "ServiceInstallName", name }, { "ServiceInstallComponentId", componentId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            if (null != dependencies)
            {
                dependencies = String.Concat(dependencies, "[~]");
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ServiceInstall, id);
                row.Set(1, name);
                row.Set(2, displayName);
                row.Set(3, typebits);
                row.Set(4, startType);
                row.Set(5, errorbits);
                row.Set(6, loadOrderGroup);
                row.Set(7, dependencies);
                row.Set(8, account);
                row.Set(9, password);
                row.Set(10, arguments);
                row.Set(11, componentId);
                row.Set(12, description);
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
                            actionName = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", id);
                            break;
                        case "Sequence":
                            string sequenceValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, sequenceValue, "execute", "ui", "both"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.Core.GetConditionInnerText(node);

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (String.IsNullOrEmpty(actionName))
            {
                actionName = String.Concat("Set", id);
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            // add the row and any references needed
            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CustomAction, new Identifier(AccessModifier.Public, actionName));
                row.Set(1, MsiInterop.MsidbCustomActionTypeProperty | MsiInterop.MsidbCustomActionTypeTextData | extraBits);
                row.Set(2, id);
                row.Set(3, value);

                foreach (string sequence in sequences)
                {
                    var sequenceRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixAction);
                    sequenceRow.Set(0, sequence);
                    sequenceRow.Set(1, actionName);
                    sequenceRow.Set(2, condition);
                    // no explicit sequence
                    // no before action
                    sequenceRow.Set(5, "CostInitialize");
                    sequenceRow.Set(6, 0); // not overridable
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
                            actionName = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            afterAction = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Before":
                            beforeAction = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            string sequenceValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, sequenceValue, "execute", "ui", "both"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.Core.GetConditionInnerText(node);

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (String.IsNullOrEmpty(actionName))
            {
                actionName = String.Concat("Set", id);
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (null != beforeAction && null != afterAction)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "After", "Before"));
            }
            else if (null == beforeAction && null == afterAction)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "After", "Before", "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            // add the row and any references needed
            if (!this.Core.EncounteredError)
            {
                // action that is scheduled to occur before/after itself
                if (beforeAction == actionName)
                {
                    this.Core.Write(ErrorMessages.ActionScheduledRelativeToItself(sourceLineNumbers, node.Name.LocalName, "Before", beforeAction));
                }
                else if (afterAction == actionName)
                {
                    this.Core.Write(ErrorMessages.ActionScheduledRelativeToItself(sourceLineNumbers, node.Name.LocalName, "After", afterAction));
                }

                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CustomAction, new Identifier(AccessModifier.Public, actionName));
                row.Set(1, MsiInterop.MsidbCustomActionTypeProperty | MsiInterop.MsidbCustomActionTypeTextData | extraBits);
                row.Set(2, id);
                row.Set(3, value);

                foreach (string sequence in sequences)
                {
                    var sequenceRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixAction, new Identifier(AccessModifier.Public, sequence, actionName));
                    sequenceRow.Set(0, sequence);
                    sequenceRow.Set(1, actionName);
                    sequenceRow.Set(2, condition);
                    // no explicit sequence
                    sequenceRow.Set(4, beforeAction);
                    sequenceRow.Set(5, afterAction);
                    sequenceRow.Set(6, 0); // not overridable

                    if (null != beforeAction)
                    {
                        if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                        {
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, beforeAction);
                        }
                        else
                        {
                            this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", beforeAction);
                        }
                    }

                    if (null != afterAction)
                    {
                        if (WindowsInstallerStandard.IsStandardAction(afterAction))
                        {
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, afterAction);
                        }
                        else
                        {
                            this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", afterAction);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.FileSFPCatalog);
                row.Set(0, id);
                row.Set(1, parentSFPCatalog);
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
                            dependency = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            parentSFPCatalog = name;
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
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
                                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dependency"));
                            }
                            dependency = parentName;
                            break;
                        case "SFPFile":
                            this.ParseSFPFileElement(child, name);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null == dependency)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dependency"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.SFPCatalog);
                row.Set(0, name);
                row.Set(1, sourceFile);
                row.Set(2, dependency);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Advertise":
                            advertise = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Arguments":
                            arguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DescriptionResourceDll":
                            descriptionResourceDll = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DescriptionResourceId":
                            descriptionResourceId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Directory":
                            directory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "DisplayResourceDll":
                            displayResourceDll = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayResourceId":
                            displayResourceId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Hotkey":
                            hotkey = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Icon":
                            icon = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                            break;
                        case "IconIndex":
                            iconIndex = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, short.MinValue + 1, short.MaxValue);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "ShortName":
                            shortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Show":
                            string showValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Show", showValue, "normal", "maximized", "minimized"));
                                        show = CompilerConstants.IllegalInteger;
                                        break;
                                }
                            }
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WorkingDirectory":
                            workingDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (advertise && null != target)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "Advertise", "yes"));
            }

            if (null == directory)
            {
                if ("Component" == parentElementLocalName)
                {
                    directory = defaultTarget;
                }
                else
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.Name.LocalName, "Directory", "Component"));
                }
            }

            if (null != descriptionResourceDll)
            {
                if (CompilerConstants.IntegerNotSet == descriptionResourceId)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DescriptionResourceDll", "DescriptionResourceId"));
                }
            }
            else
            {
                if (CompilerConstants.IntegerNotSet != descriptionResourceId)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DescriptionResourceId", "DescriptionResourceDll"));
                }
            }

            if (null != displayResourceDll)
            {
                if (CompilerConstants.IntegerNotSet == displayResourceId)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayResourceDll", "DisplayResourceId"));
                }
            }
            else
            {
                if (CompilerConstants.IntegerNotSet != displayResourceId)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayResourceId", "DisplayResourceDll"));
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (0 < name.Length)
            {
                if (this.Core.IsValidShortFilename(name, false))
                {
                    if (null == shortName)
                    {
                        shortName = name;
                        name = null;
                    }
                    else
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", name, "ShortName"));
                    }
                }
                else if (null == shortName) // generate a short file name.
                {
                    shortName = this.Core.CreateShortName(name, true, false, node.Name.LocalName, componentId, directory);
                }
            }

            if ("Component" != parentElementLocalName && null != target)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Target", parentElementLocalName));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("sct", directory, LowercaseOrNull(name) ?? LowercaseOrNull(shortName));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Shortcut, id);
                row.Set(1, directory);
                row.Set(2, GetMsiFilenameValue(shortName, name));
                row.Set(3, componentId);
                if (advertise)
                {
                    if (YesNoType.Yes != parentKeyPath && "Component" != parentElementLocalName)
                    {
                        this.Core.Write(WarningMessages.UnclearShortcut(sourceLineNumbers, id.Id, componentId, defaultTarget));
                    }
                    row.Set(4, Guid.Empty.ToString("B"));
                }
                else if (null != target)
                {
                    row.Set(4, target);
                }
                else if ("Component" == parentElementLocalName || "CreateFolder" == parentElementLocalName)
                {
                    row.Set(4, String.Format(CultureInfo.InvariantCulture, "[{0}]", defaultTarget));
                }
                else if ("File" == parentElementLocalName)
                {
                    row.Set(4, String.Format(CultureInfo.InvariantCulture, "[#{0}]", defaultTarget));
                }
                row.Set(5, arguments);
                row.Set(6, description);
                if (CompilerConstants.IntegerNotSet != hotkey)
                {
                    row.Set(7, hotkey);
                }
                row.Set(8, icon);
                if (CompilerConstants.IntegerNotSet != iconIndex)
                {
                    row.Set(9, iconIndex);
                }

                if (CompilerConstants.IntegerNotSet != show)
                {
                    row.Set(10, show);
                }
                row.Set(11, workingDirectory);
                row.Set(12, displayResourceDll);
                if (CompilerConstants.IntegerNotSet != displayResourceId)
                {
                    row.Set(13, displayResourceId);
                }
                row.Set(14, descriptionResourceDll);
                if (CompilerConstants.IntegerNotSet != descriptionResourceId)
                {
                    row.Set(15, descriptionResourceId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(key))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }
            else if (null == id)
            {
                id = this.Core.CreateIdentifier("scp", shortcutId, key.ToUpperInvariant());
            }

            string innerText = this.Core.GetTrimmedInnerText(node);
            if (!String.IsNullOrEmpty(innerText))
            {
                if (String.IsNullOrEmpty(value))
                {
                    value = innerText;
                }
                else // cannot specify both the value attribute and inner text
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }

            if (String.IsNullOrEmpty(value))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiShortcutProperty, id);
                row.Set(1, shortcutId);
                row.Set(2, key);
                row.Set(3, value);
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
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Advertise":
                            advertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Control":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 2;
                            }
                            break;
                        case "Cost":
                            cost = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HasDiskImage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 8;
                            }
                            break;
                        case "HelpDirectory":
                            helpDirectory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 4;
                            }
                            break;
                        case "Language":
                            language = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "MajorVersion":
                            majorVersion = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, ushort.MaxValue);
                            break;
                        case "MinorVersion":
                            minorVersion = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
                            break;
                        case "ResourceId":
                            resourceId = this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, int.MinValue, int.MaxValue);
                            break;
                        case "Restricted":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 1;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (CompilerConstants.IntegerNotSet == language)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (YesNoType.Yes == advertise)
            {
                if (CompilerConstants.LongNotSet != resourceId)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "ResourceId"));
                }

                if (0 != flags)
                {
                    if (0x1 == (flags & 0x1))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Restricted", "Advertise", "yes"));
                    }

                    if (0x2 == (flags & 0x2))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Control", "Advertise", "yes"));
                    }

                    if (0x4 == (flags & 0x4))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Hidden", "Advertise", "yes"));
                    }

                    if (0x8 == (flags & 0x8))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "HasDiskImage", "Advertise", "yes"));
                    }
                }

                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.TypeLib);
                    row.Set(0, id);
                    row.Set(1, language);
                    row.Set(2, componentId);
                    if (CompilerConstants.IntegerNotSet != majorVersion || CompilerConstants.IntegerNotSet != minorVersion)
                    {
                        row.Set(3, (CompilerConstants.IntegerNotSet != majorVersion ? majorVersion * 256 : 0) + (CompilerConstants.IntegerNotSet != minorVersion ? minorVersion : 0));
                    }
                    row.Set(4, description);
                    row.Set(5, helpDirectory);
                    row.Set(6, Guid.Empty.ToString("B"));
                    if (CompilerConstants.IntegerNotSet != cost)
                    {
                        row.Set(7, cost);
                    }
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (CompilerConstants.IntegerNotSet != cost && CompilerConstants.IllegalInteger != cost)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Cost", "Advertise", "no"));
                }

                if (null == fileServer)
                {
                    this.Core.Write(ErrorMessages.MissingTypeLibFile(sourceLineNumbers, node.Name.LocalName, "File"));
                }

                if (null == registryVersion)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "MajorVersion", "MinorVersion", "Advertise", "no"));
                }

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion], (Default) = [Description]
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}", id, registryVersion), null, description, componentId);

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\[Language]\[win16|win32|win64], (Default) = [TypeLibPath]\[ResourceId]
                string path = String.Concat("[#", fileServer, "]");
                if (CompilerConstants.LongNotSet != resourceId)
                {
                    path = String.Concat(path, Path.DirectorySeparatorChar, resourceId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                }
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\{2}\{3}", id, registryVersion, language, (win64Component ? "win64" : "win32")), null, path, componentId);

                // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\FLAGS, (Default) = [TypeLibFlags]
                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\FLAGS", id, registryVersion), null, flags.ToString(CultureInfo.InvariantCulture.NumberFormat), componentId);

                if (null != helpDirectory)
                {
                    // HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\HELPDIR, (Default) = [HelpDirectory]
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\HELPDIR", id, registryVersion), null, String.Concat("[", helpDirectory, "]"), componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinarySource":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "FileSource", "PropertySource"));
                            }
                            source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeBinaryData;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", source); // add a reference to the appropriate Binary
                            break;
                        case "CommandLine":
                            commandLine = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FileSource":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "PropertySource"));
                            }
                            source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeSourceFile;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", source); // add a reference to the appropriate File
                            break;
                        case "PropertySource":
                            if (null != source)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "FileSource"));
                            }
                            source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            type = MsiInterop.MsidbCustomActionTypeExe + MsiInterop.MsidbCustomActionTypeProperty;
                            // cannot add a reference to a Property because it may be created at runtime.
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Get the condition from the inner text of the element.
            condition = this.Core.GetConditionInnerText(node);

            if (null == id)
            {
                id = this.Core.CreateIdentifier("mec", source, type.ToString());
            }

            if (null == source)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "BinarySource", "FileSource", "PropertySource"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiEmbeddedChainer, id);
                row.Set(1, condition);
                row.Set(2, commandLine);
                row.Set(3, source);
                row.Set(4, type);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.ParseControlGroupElement(child, TupleDefinitionType.ComboBox, "ListItem");
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            this.ParseEmbeddedUIElement(child);
                            ++embeddedUICount;
                            break;
                        case "Error":
                            this.ParseErrorElement(child);
                            break;
                        case "ListBox":
                            this.ParseControlGroupElement(child, TupleDefinitionType.ListBox, "ListItem");
                            break;
                        case "ListView":
                            this.ParseControlGroupElement(child, TupleDefinitionType.ListView, "ListItem");
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
                                this.Core.Write(ErrorMessages.RadioButtonBitmapAndIconDisallowed(childSourceLineNumbers));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null != id && !this.Core.EncounteredError)
            {
                this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixUI, id);
            }
        }

        /// <summary>
        /// Parses a list item element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table to add row to.</param>
        /// <param name="property">Identifier of property referred to by list item.</param>
        /// <param name="order">Relative order of list items.</param>
        private void ParseListItemElement(XElement node, TupleDefinitionType tableName, string property, ref int order)
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
                            if (TupleDefinitionType.ListView == tableName)
                            {
                                icon = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                                this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", icon);
                            }
                            else
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeExceptOnElement(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ListView"));
                            }
                            break;
                        case "Text":
                            text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, tableName);
                row.Set(0, property);
                row.Set(1, ++order);
                row.Set(2, value);
                row.Set(3, text);
                if (null != icon)
                {
                    row.Set(4, icon);
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
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Icon", "Text"));
                            }
                            text = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                            type = RadioButtonType.Bitmap;
                            break;
                        case "Height":
                            height = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Help":
                            help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Icon":
                            if (RadioButtonType.NotSet != type)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Text"));
                            }
                            text = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                            type = RadioButtonType.Icon;
                            break;
                        case "Text":
                            if (RadioButtonType.NotSet != type)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Icon"));
                            }
                            text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            type = RadioButtonType.Text;
                            break;
                        case "ToolTip":
                            tooltip = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Y":
                            y = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (null == x)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == width)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == height)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.RadioButton);
                row.Set(0, property);
                row.Set(1, ++order);
                row.Set(2, value);
                row.Set(3, x);
                row.Set(4, y);
                row.Set(5, width);
                row.Set(6, height);
                row.Set(7, text);
                if (null != tooltip || null != help)
                {
                    row.Set(8, String.Concat(tooltip, "|", help));
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
                            action = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", "InstallExecuteSequence", action);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("bil", action, order.ToString(), feature);
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Control":
                            // These are all thrown away.
                            IntermediateTuple lastTabRow = null;
                            string firstControl = null;
                            string defaultControl = null;
                            string cancelControl = null;

                            this.ParseControlElement(child, id.Id, TupleDefinitionType.BBControl, ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, false);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Billboard, id);
                row.Set(1, feature);
                row.Set(2, action);
                row.Set(3, order);
            }
        }

        /// <summary>
        /// Parses a control group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table referred to by control group.</param>
        /// <param name="childTag">Expected child elements.</param>
        private void ParseControlGroupElement(XElement node, TupleDefinitionType tableName, string childTag)
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
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            foreach (XElement child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    if (childTag != child.Name.LocalName)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }

                    switch (child.Name.LocalName)
                    {
                        case "ListItem":
                            this.ParseListItemElement(child, tableName, property, ref order);
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Property", property);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
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
                                this.Core.Write(ErrorMessages.RadioButtonTypeInconsistent(childSourceLineNumbers));
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Template":
                            template = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ActionText);
                row.Set(0, action);
                row.Set(1, Common.GetInnerText(node));
                row.Set(2, template);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            text = Common.GetInnerText(node);

            if (null == id)
            {
                id = this.Core.CreateIdentifier("txt", text);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.UIText, id);
                row.Set(1, text);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        // RGB Values
                        case "Red":
                            int redColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
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
                            int greenColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
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
                            int blueColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, byte.MaxValue);
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsBold;
                            }
                            break;
                        case "Italic":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsItalic;
                            }
                            break;
                        case "Strike":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsStrike;
                            }
                            break;
                        case "Underline":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits |= MsiInterop.MsidbTextStyleStyleBitsUnderline;
                            }
                            break;

                        // Font values
                        case "FaceName":
                            faceName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            size = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.CreateIdentifier("txs", faceName, size.ToString(), color.ToString(), bits.ToString());
            }

            if (null == faceName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "FaceName"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.TextStyle, id);
                row.Set(1, faceName);
                row.Set(2, size);
                if (0 <= color)
                {
                    row.Set(3, color);
                }

                if (0 < bits)
                {
                    row.Set(4, bits);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Height":
                            height = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Title":
                            title = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;
                        case "Y":
                            y = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;

                        case "CustomPalette":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesUseCustomPalette;
                            }
                            break;
                        case "ErrorDialog":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesError;
                            }
                            break;
                        case "Hidden":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesVisible;
                            }
                            break;
                        case "KeepModeless":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesKeepModeless;
                            }
                            break;
                        case "LeftScroll":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesLeftScroll;
                            }
                            break;
                        case "Modeless":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesModal;
                            }
                            break;
                        case "NoMinimize":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesMinimize;
                            }
                            break;
                        case "RightAligned":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesRightAligned;
                            }
                            break;
                        case "RightToLeft":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesRTLRO;
                            }
                            break;
                        case "SystemModal":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesSysModal;
                            }
                            break;
                        case "TrackDiskSpace":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                bits ^= MsiInterop.MsidbDialogAttributesTrackDiskSpace;
                                trackDiskSpace = true;
                            }
                            break;

                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            IntermediateTuple lastTabRow = null;
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
                            this.ParseControlElement(child, id.Id, TupleDefinitionType.Control, ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, trackDiskSpace);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (null != lastTabRow && null != lastTabRow[1])
            {
                if (firstControl != lastTabRow[1].ToString())
                {
                    lastTabRow.Set(10, firstControl);
                }
            }

            if (null == firstControl)
            {
                this.Core.Write(ErrorMessages.NoFirstControlSpecified(sourceLineNumbers, id.Id));
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Dialog, id);
                row.Set(1, x);
                row.Set(2, y);
                row.Set(3, width);
                row.Set(4, height);
                row.Set(5, bits);
                row.Set(6, title);
                row.Set(7, firstControl);
                row.Set(8, defaultControl);
                row.Set(9, cancelControl);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "IgnoreFatalExit":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_FATALEXIT;
                            }
                            break;
                        case "IgnoreError":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ERROR;
                            }
                            break;
                        case "IgnoreWarning":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_WARNING;
                            }
                            break;
                        case "IgnoreUser":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_USER;
                            }
                            break;
                        case "IgnoreInfo":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INFO;
                            }
                            break;
                        case "IgnoreFilesInUse":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_FILESINUSE;
                            }
                            break;
                        case "IgnoreResolveSource":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_RESOLVESOURCE;
                            }
                            break;
                        case "IgnoreOutOfDiskSpace":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_OUTOFDISKSPACE;
                            }
                            break;
                        case "IgnoreActionStart":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ACTIONSTART;
                            }
                            break;
                        case "IgnoreActionData":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_ACTIONDATA;
                            }
                            break;
                        case "IgnoreProgress":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_PROGRESS;
                            }
                            break;
                        case "IgnoreCommonData":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_COMMONDATA;
                            }
                            break;
                        case "IgnoreInitialize":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INITIALIZE;
                            }
                            break;
                        case "IgnoreTerminate":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_TERMINATE;
                            }
                            break;
                        case "IgnoreShowDialog":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_SHOWDIALOG;
                            }
                            break;
                        case "IgnoreRMFilesInUse":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_RMFILESINUSE;
                            }
                            break;
                        case "IgnoreInstallStart":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INSTALLSTART;
                            }
                            break;
                        case "IgnoreInstallEnd":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                messageFilter ^= MsiInterop.INSTALLLOGMODE_INSTALLEND;
                            }
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportBasicUI":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= MsiInterop.MsidbEmbeddedHandlesBasic;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = Path.GetFileName(sourceFile);
                if (!this.Core.IsValidLongFilename(name, false))
                {
                    this.Core.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = id.Id;
            }

            if (!name.Contains("."))
            {
                this.Core.Write(ErrorMessages.InvalidEmbeddedUIFileName(sourceLineNumbers, name));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiEmbeddedUI, id);
                row.Set(1, name);
                row.Set(2, attributes);
                row.Set(3, messageFilter);
                row.Set(4, sourceFile);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = Path.GetFileName(sourceFile);
                if (!this.Core.IsValidLongFilename(name, false))
                {
                    this.Core.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Source", name));
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (String.IsNullOrEmpty(name))
            {
                name = id.Id;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.MsiEmbeddedUI, id);
                row.Set(1, name);
                row.Set(2, 0); // embedded UI resources always set this to 0
                //row.Set(3, null);
                row.Set(4, sourceFile);
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
        private void ParseControlElement(XElement node, string dialog, TupleDefinitionType tableName, ref IntermediateTuple lastTabRow, ref string firstControl, ref string defaultControl, ref string cancelControl, bool trackDiskSpace)
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }
            else
            {
                controlType = this.Core.GetAttributeValue(sourceLineNumbers, typeAttribute);
            }

            switch (controlType)
            {
                case "Billboard":
                    specialAttributes = null;
                    notTabbable = true;
                    disabled = true;

                    this.Core.EnsureTable(sourceLineNumbers, "Billboard");
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Type": // already processed
                            break;
                        case "Cancel":
                            isCancel = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "CheckBoxPropertyRef":
                            checkBoxPropertyRef = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CheckBoxValue":
                            checkboxValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Default":
                            isDefault = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Height":
                            height = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Help":
                            help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconSize":
                            string iconSizeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (null != specialAttributes)
                            {
                                if (0 < iconSizeValue.Length)
                                {
                                    Wix.Control.IconSizeType iconsSizeType = Wix.Control.ParseIconSizeType(iconSizeValue);
                                    switch (iconsSizeType)
                                    {
                                        case Wix.Control.IconSizeType.Item16:
                                            this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                            break;
                                        case Wix.Control.IconSizeType.Item32:
                                            this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                            break;
                                        case Wix.Control.IconSizeType.Item48:
                                            this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                            this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                            break;
                                        default:
                                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "16", "32", "48"));
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "Type"));
                            }
                            break;
                        case "Property":
                            property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TabSkip":
                            notTabbable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Text":
                            text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ToolTip":
                            tooltip = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Width":
                            width = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "X":
                            x = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Y":
                            y = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        default:
                            YesNoType attribValue = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.Core.TrySetBitFromName(MsiInterop.CommonControlAttributes, attrib.Name.LocalName, attribValue, bits, 0))
                            {
                                if (null == specialAttributes || !this.Core.TrySetBitFromName(specialAttributes, attrib.Name.LocalName, attribValue, bits, 16))
                                {
                                    this.Core.UnexpectedAttribute(node, attrib);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            attributes = this.Core.CreateIntegerFromBitArray(bits);

            if (disabled)
            {
                attributes |= MsiInterop.MsidbControlAttributesEnabled; // bit will be inverted when stored
            }

            if (null == height)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            if (null == width)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == x)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("ctl", dialog, x, y, height, width);
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
                            this.ParseControlGroupElement(child, TupleDefinitionType.ComboBox, "ListItem");
                            break;
                        case "Condition":
                            this.ParseConditionElement(child, node.Name.LocalName, id.Id, dialog);
                            break;
                        case "ListBox":
                            this.ParseControlGroupElement(child, TupleDefinitionType.ListBox, "ListItem");
                            break;
                        case "ListView":
                            this.ParseControlGroupElement(child, TupleDefinitionType.ListView, "ListItem");
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
                                            sourceFile = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                            break;
                                        default:
                                            this.Core.UnexpectedAttribute(child, attrib);
                                            break;
                                    }
                                }
                                else
                                {
                                    this.Core.ParseExtensionAttribute(child, attrib);
                                }
                            }

                            text = Common.GetInnerText(child);
                            if (!String.IsNullOrEmpty(text) && null != sourceFile)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWithInnerText(childSourceLineNumbers, child.Name.LocalName, "SourceFile"));
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
            IntermediateTuple row = null;
            if (!this.Core.EncounteredError)
            {
                if ("CheckBox" == controlType)
                {
                    if (String.IsNullOrEmpty(property) && String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef", true));
                    }
                    else if (!String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef"));
                    }
                    else if (!String.IsNullOrEmpty(property))
                    {
                        row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CheckBox);
                        row.Set(0, property);
                        row.Set(1, checkboxValue);
                    }
                    else
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CheckBox", checkBoxPropertyRef);
                    }
                }

                var dialogId = new Identifier(dialog, id.Access);

                row = this.Core.CreateRow(sourceLineNumbers, tableName, dialogId);
                row.Set(1, id.Id);
                row.Set(2, controlType);
                row.Set(3, x);
                row.Set(4, y);
                row.Set(5, width);
                row.Set(6, height);
                row.Set(7, attributes ^ (MsiInterop.MsidbControlAttributesVisible | MsiInterop.MsidbControlAttributesEnabled));
                if (TupleDefinitionType.BBControl == tableName)
                {
                    row.Set(8, text); // BBControl.Text

                    if (null != sourceFile)
                    {
                        var wixBBControlRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBBControl, dialogId);
                        wixBBControlRow.Set(1, id.Id);
                        wixBBControlRow.Set(2, sourceFile);
                    }
                }
                else
                {
                    row.Set(8, !String.IsNullOrEmpty(property) ? property : checkBoxPropertyRef);
                    row.Set(9, text);
                    if (null != tooltip || null != help)
                    {
                        row.Set(11, String.Concat(tooltip, "|", help)); // Separator is required, even if only one is non-null.
                    }

                    if (null != sourceFile)
                    {
                        var wixControlRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixControl, dialogId);
                        wixControlRow.Set(1, id.Id);
                        wixControlRow.Set(2, sourceFile);
                    }
                }
            }

            if (!notTabbable)
            {
                if (TupleDefinitionType.BBControl == tableName)
                {
                    this.Core.Write(ErrorMessages.TabbableControlNotAllowedInBillboard(sourceLineNumbers, node.Name.LocalName, controlType));
                }

                if (null == firstControl)
                {
                    firstControl = id.Id;
                }

                if (null != lastTabRow)
                {
                    lastTabRow.Set(10, id.Id);
                }
                lastTabRow = row;
            }

            // bitmap and icon controls contain a foreign key into the binary table in the text column;
            // add a reference if the identifier of the binary entry is known during compilation
            if (("Bitmap" == controlType || "Icon" == controlType) && Common.IsIdentifier(text))
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
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
                                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            control = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Dialog":
                            if (null != dialog)
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            dialog = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Dialog", dialog);
                            break;
                        case "Event":
                            controlEvent = Compiler.UppercaseFirstChar(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Order":
                            order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 2147483647);
                            break;
                        case "Property":
                            property = String.Concat("[", this.Core.GetAttributeValue(sourceLineNumbers, attrib), "]");
                            break;
                        case "Value":
                            argument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.Core.GetConditionInnerText(node);

            if (null == control)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Control"));
            }

            if (null == dialog)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dialog"));
            }

            if (null == controlEvent && null == property) // need to specify at least one
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }
            else if (null != controlEvent && null != property) // cannot specify both
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }

            if (null == argument)
            {
                if (null != controlEvent)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value", "Event"));
                }
                else if (null != property)
                {
                    // if this is setting a property to null, put a special value in the argument column
                    argument = "{}";
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.ControlEvent);
                row.Set(0, dialog);
                row.Set(1, control);
                row.Set(2, (null != controlEvent ? controlEvent : property));
                row.Set(3, argument);
                row.Set(4, condition);
                row.Set(5, order);
            }

            if ("DoAction" == controlEvent && null != argument)
            {
                // if we're not looking at a standard action or a formatted string then create a reference 
                // to the custom action.
                if (!WindowsInstallerStandard.IsStandardAction(argument) && !Common.ContainsProperty(argument))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", argument);
                }
            }

            // if we're referring to a dialog but not through a property, add it to the references
            if (("NewDialog" == controlEvent || "SpawnDialog" == controlEvent || "SpawnWaitDialog" == controlEvent || "SelectionBrowse" == controlEvent) && Common.IsIdentifier(argument))
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "Dialog", argument);
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
                            controlAttribute = Compiler.UppercaseFirstChar(this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                            break;
                        case "Event":
                            eventMapping = Compiler.UppercaseFirstChar(this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.EventMapping);
                row.Set(0, dialog);
                row.Set(1, control);
                row.Set(2, eventMapping);
                row.Set(3, controlAttribute);
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
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.Write(WarningMessages.DeprecatedUpgradeProperty(childSourceLineNumbers));
                            break;
                        case "UpgradeVersion":
                            this.ParseUpgradeVersionElement(child, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesLanguagesExclusive;
                            }
                            break;
                        case "IgnoreRemoveFailure":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesIgnoreRemoveFailure;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                            }
                            break;
                        case "IncludeMinimum": // this is "yes" by default
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options &= ~MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                            }
                            break;
                        case "Language":
                            language = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minimum = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maximum = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "MigrateFeatures":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesMigrateFeatures;
                            }
                            break;
                        case "OnlyDetect":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                options |= MsiInterop.MsidbUpgradeAttributesOnlyDetect;
                            }
                            break;
                        case "Property":
                            actionProperty = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "RemoveFeatures":
                            removeFeatures = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == actionProperty)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }
            else if (actionProperty.ToUpper(CultureInfo.InvariantCulture) != actionProperty)
            {
                this.Core.Write(ErrorMessages.SecurePropertyNotUppercase(sourceLineNumbers, node.Name.LocalName, "Property", actionProperty));
            }

            if (null == minimum && null == maximum)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Minimum", "Maximum"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Upgrade);
                row.Set(0, upgradeId);
                row.Set(1, minimum);
                row.Set(2, maximum);
                row.Set(3, language);
                row.Set(4, options);
                row.Set(5, removeFeatures);
                row.Set(6, actionProperty);

                // Ensure the action property is secure.
                this.AddWixPropertyRow(sourceLineNumbers, new Identifier(actionProperty, AccessModifier.Private), false, true, false);

                // Ensure that RemoveExistingProducts is authored in InstallExecuteSequence
                // if at least one row in Upgrade table lacks the OnlyDetect attribute.
                if (0 == (options & MsiInterop.MsidbUpgradeAttributesOnlyDetect))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", "InstallExecuteSequence", "RemoveExistingProducts");
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
                            id = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Argument":
                            argument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Command":
                            command = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "TargetFile", "TargetProperty"));
                            break;
                        case "TargetFile":
                            targetFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", targetFile);
                            break;
                        case "TargetProperty":
                            targetProperty = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null != target && null != targetFile)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "TargetFile"));
            }

            if (null != target && null != targetProperty)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "TargetProperty"));
            }

            if (null != targetFile && null != targetProperty)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "TargetFile", "TargetProperty"));
            }

            this.Core.ParseForExtensionElements(node);

            if (YesNoType.Yes == advertise)
            {
                if (null != target)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "Target"));
                }

                if (null != targetFile)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "TargetFile"));
                }

                if (null != targetProperty)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name.LocalName, "TargetProperty"));
                }

                if (!this.Core.EncounteredError)
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Verb);
                    row.Set(0, extension);
                    row.Set(1, id);
                    if (CompilerConstants.IntegerNotSet != sequence)
                    {
                        row.Set(2, sequence);
                    }
                    row.Set(3, command);
                    row.Set(4, argument);
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Sequence", "Advertise", "no"));
                }

                if (null == target && null == targetFile && null == targetProperty)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "TargetFile", "TargetProperty", "Advertise", "no"));
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
                    this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(prefix, "\\shell\\", id), String.Empty, command, componentId);
                }

                this.Core.CreateRegistryRow(sourceLineNumbers, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(prefix, "\\shell\\", id, "\\command"), String.Empty, target, componentId);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            valueName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Win64":
                            win64 = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            BundleApprovedExeForElevationAttributes attributes = BundleApprovedExeForElevationAttributes.None;

            if (win64 == YesNoType.Yes)
            {
                attributes |= BundleApprovedExeForElevationAttributes.Win64;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var wixApprovedExeForElevationRow = (WixApprovedExeForElevationTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixApprovedExeForElevation, id);
                wixApprovedExeForElevationRow.Key = key;
                wixApprovedExeForElevationRow.Value = valueName;
                wixApprovedExeForElevationRow.Attributes = (int)attributes;
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
                            aboutUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Copyright":
                            copyright = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisableModify":
                            string value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "button", "yes", "no"));
                                    break;
                            }
                            break;
                        case "DisableRemove":
                            disableRemove = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DisableRepair":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "HelpTelephone":
                            helpTelephone = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HelpUrl":
                            helpUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconSourceFile":
                            iconSourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParentName":
                            parentName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SplashScreenSourceFile":
                            splashScreenSourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Tag":
                            tag = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpdateUrl":
                            updateUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidModuleOrBundleVersion(version))
            {
                this.Core.Write(WarningMessages.InvalidModuleOrBundleVersion(sourceLineNumbers, "Bundle", version));
            }

            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UpgradeCode"));
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
            this.Core.CreateActiveSection(this.activeName, SectionType.Bundle, 0, this.Context.CompilationId);

            // Now that the active section is initialized, process only extension attributes.
            foreach (XAttribute attrib in node.Attributes())
            {
                if (!String.IsNullOrEmpty(attrib.Name.NamespaceName) && CompilerCore.WixNamespace != attrib.Name.Namespace)
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "BootstrapperApplication"));
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Chain"));
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
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Log"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!chainSeen)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Chain"));
            }

            if (!this.Core.EncounteredError)
            {
                if (null != upgradeCode)
                {
                    var relatedBundleRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixRelatedBundle);
                    relatedBundleRow.Set(0, upgradeCode);
                    relatedBundleRow.Set(1, (int)Wix.RelatedBundle.ActionType.Upgrade);
                }

                var containerRow = (WixBundleContainerTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleContainer);
                containerRow.WixBundleContainer = Compiler.BurnDefaultAttachedContainerId;
                containerRow.Name = "bundle-attached.cab";
                containerRow.Type = ContainerType.Attached;

                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundle);
                row.Set(0, version);
                row.Set(1, copyright);
                row.Set(2, name);
                row.Set(3, aboutUrl);
                if (-1 != disableModify)
                {
                    row.Set(4, disableModify);
                }
                if (YesNoType.NotSet != disableRemove)
                {
                    row.Set(5, (YesNoType.Yes == disableRemove) ? 1 : 0);
                }
                // row.Set(6] - (deprecated) "disable repair"
                row.Set(7, helpTelephone);
                row.Set(8, helpUrl);
                row.Set(9, manufacturer);
                row.Set(10, updateUrl);
                if (YesNoDefaultType.Default != compressed)
                {
                    row.Set(11, (YesNoDefaultType.Yes == compressed) ? 1 : 0);
                }

                row.Set(12, logVariablePrefixAndExtension);
                row.Set(13, iconSourceFile);
                row.Set(14, splashScreenSourceFile);
                row.Set(15, condition);
                row.Set(16, tag);
                row.Set(17, this.CurrentPlatform.ToString());
                row.Set(18, parentName);
                row.Set(19, upgradeCode);

                // Ensure that the bundle stores the well-known persisted values.
                var bundleNameWellKnownVariable = (WixBundleVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleVariable);
                bundleNameWellKnownVariable.WixBundleVariable = Compiler.BURN_BUNDLE_NAME;
                bundleNameWellKnownVariable.Hidden = false;
                bundleNameWellKnownVariable.Persisted = true;

                var bundleOriginalSourceWellKnownVariable = (WixBundleVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleVariable);
                bundleOriginalSourceWellKnownVariable.WixBundleVariable = Compiler.BURN_BUNDLE_ORIGINAL_SOURCE;
                bundleOriginalSourceWellKnownVariable.Hidden = false;
                bundleOriginalSourceWellKnownVariable.Persisted = true;

                var bundleOriginalSourceFolderWellKnownVariable = (WixBundleVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleVariable);
                bundleOriginalSourceFolderWellKnownVariable.WixBundleVariable = Compiler.BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER;
                bundleOriginalSourceFolderWellKnownVariable.Hidden = false;
                bundleOriginalSourceFolderWellKnownVariable.Persisted = true;

                var bundleLastUsedSourceWellKnownVariable = (WixBundleVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleVariable);
                bundleLastUsedSourceWellKnownVariable.WixBundleVariable = Compiler.BURN_BUNDLE_LAST_USED_SOURCE;
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
                            disableLog = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PathVariable":
                            variable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Prefix":
                            logPrefix = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            logExtension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (!logExtension.StartsWith(".", StringComparison.Ordinal))
            {
                logExtension = String.Concat(".", logExtension);
            }

            this.Core.ParseForExtensionElements(node);

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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.Core.ParseForExtensionElements(node);

            // Create catalog row
            if (!this.Core.EncounteredError)
            {
                this.CreatePayloadRow(sourceLineNumbers, id, Path.GetFileName(sourceFile), sourceFile, null, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, ComplexReferenceChildType.Unknown, null, YesNoDefaultType.Yes, YesNoType.Yes, null, null, null);

                var wixCatalogRow = (WixBundleCatalogTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleCatalog, id);
                wixCatalogRow.Payload_ = id.Id;
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Enum.TryParse<ContainerType>(typeString, out type))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Type", typeString, "attached, detached"));
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (null == name)
            {
                name = id.Id;
            }

            if (!String.IsNullOrEmpty(downloadUrl) && ContainerType.Detached != type)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "Type", "attached"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var row = (WixBundleContainerTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleContainer, id);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null == previousId)
            {
                // We need *either* <Payload> or <PayloadGroupRef> or even just @SourceFile on the BA...
                // but we just say there's a missing <Payload>.
                // TODO: Is there a better message for this?
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Payload"));
            }

            // Add the application as an attached container and if an Id was provided add that too.
            if (!this.Core.EncounteredError)
            {
                var containerRow = (WixBundleContainerTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleContainer);
                containerRow.WixBundleContainer = Compiler.BurnUXContainerId;
                containerRow.Name = "bundle-ux.cab";
                containerRow.Type = ContainerType.Attached;

                if (!String.IsNullOrEmpty(id))
                {
                    var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBootstrapperApplication);
                    row.Set(0, id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (String.IsNullOrEmpty(id))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "WixBootstrapperApplication", id);
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
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Department":
                            department = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductFamily":
                            productFamily = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Classification":
                            classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                    this.Core.Write(ErrorMessages.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Manufacturer", node.Parent.Name.LocalName));
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
                    this.Core.Write(ErrorMessages.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Name", node.Parent.Name.LocalName));
                }
            }

            if (String.IsNullOrEmpty(classification))
            {
                this.Core.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, node.Name.LocalName, "Classification", defaultClassification));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixUpdateRegistration);
                row.Set(0, manufacturer);
                row.Set(1, department);
                row.Set(2, productFamily);
                row.Set(3, name);
                row.Set(4, classification);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child, context);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EnableSignatureVerification":
                            enableSignatureVerification = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
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
                id = this.Core.CreateIdentifier("pay", (null != sourceFile) ? sourceFile.ToUpperInvariant() : String.Empty);
            }

            // Now that the PayloadId is known, we can parse the extension attributes.
            Dictionary<string, string> context = new Dictionary<string, string>();
            context["Id"] = id.Id;

            foreach (XAttribute extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            // We only handle the elements we care about.  Let caller handle other children.
            foreach (XElement child in node.Elements(CompilerCore.WixNamespace + "RemotePayload"))
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (CompilerCore.WixNamespace == node.Name.Namespace && node.Name.LocalName != "ExePackage")
                {
                    this.Core.Write(ErrorMessages.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (null != sourceFile && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (null == sourceFile && null == remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeOrElement(sourceLineNumbers, node.Name.LocalName, "SourceFile", "RemotePayload"));
            }
            else if (null == sourceFile)
            {
                sourceFile = String.Empty;
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (Compiler.BurnUXContainerId == parentId)
            {
                if (compressed == YesNoDefaultType.No)
                {
                    Core.Write(WarningMessages.UxPayloadsOnlySupportEmbedding(sourceLineNumbers, sourceFile));
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
                            remotePayload.CertificatePublicKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CertificateThumbprint":
                            remotePayload.CertificateThumbprint = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            remotePayload.Description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Hash":
                            remotePayload.Hash = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductName":
                            remotePayload.ProductName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            remotePayload.Size = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Version":
                            remotePayload.Version = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(remotePayload.ProductName))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductName"));
            }

            if (String.IsNullOrEmpty(remotePayload.Description))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (String.IsNullOrEmpty(remotePayload.Hash))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Hash"));
            }

            if (0 == remotePayload.Size)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Size"));
            }

            if (String.IsNullOrEmpty(remotePayload.Version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            return remotePayload;
        }

        /// <summary>
        /// Creates the row for a Payload.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private WixBundlePayloadTuple CreatePayloadRow(SourceLineNumber sourceLineNumbers, Identifier id, string name, string sourceFile, string downloadUrl, ComplexReferenceParentType parentType,
            string parentId, ComplexReferenceChildType previousType, string previousId, YesNoDefaultType compressed, YesNoType enableSignatureVerification, string displayName, string description,
            Wix.RemotePayload remotePayload)
        {
            WixBundlePayloadTuple row = null;

            if (!this.Core.EncounteredError)
            {
                row = (WixBundlePayloadTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePayload, id);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePayloadGroup, id);

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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixBundlePayloadGroup", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

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
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, type, id);
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
            var value = CompilerConstants.IntegerNotSet;
            var behavior = ExitCodeBehaviorType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            value = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, int.MinValue + 2, int.MaxValue);
                            break;
                        case "Behavior":
                            string behaviorString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Enum.TryParse<ExitCodeBehaviorType>(behaviorString, true, out behavior))
                            {
                                this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Behavior", behaviorString, "success, error, scheduleReboot, forceReboot"));
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (ExitCodeBehaviorType.NotSet == behavior)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Behavior"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = (WixBundlePackageExitCodeTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePackageExitCode);
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
            var attributes = WixChainAttributes.None;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "DisableRollback":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableRollback;
                            }
                            break;
                        case "DisableSystemRestore":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableSystemRestore;
                            }
                            break;
                        case "ParallelCache":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.ParallelCache;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (null == previousId)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "MsiPackage", "ExePackage", "PackageGroupRef"));
            }

            if (!this.Core.EncounteredError)
            {
                var row = (WixChainTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixChain);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Transaction":
                            transaction = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
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
                    id = this.Core.CreateIdentifier("rba", previousId);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }

            // Now that the rollback identifier is known, we can parse the extension attributes...
            Dictionary<string, string> contextValues = new Dictionary<string, string>();
            contextValues["RollbackBoundaryId"] = id.Id;
            foreach (XAttribute attribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, attribute, contextValues);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            if (!this.Core.IsValidLongFilename(name, false, true))
                            {
                                this.Core.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Name", name));
                            }
                            break;
                        case "SourceFile":
                            sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallCondition":
                            installCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Cache":
                            cache = this.Core.GetAttributeYesNoAlwaysValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheId":
                            cacheId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayInternalUI":
                            displayInternalUI = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi || packageType == WixBundlePackageType.Msp);
                            break;
                        case "EnableFeatureSelection":
                            enableFeatureSelection = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "ForcePerMachine":
                            forcePerMachine = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "LogPathVariable":
                            logPathVariable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "RollbackLogPathVariable":
                            rollbackPathVariable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Permanent":
                            permanent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Visible":
                            visible = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallCommand":
                            installCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "RepairCommand":
                            repairCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            repairable = YesNoType.Yes;
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "UninstallCommand":
                            uninstallCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "PerMachine":
                            perMachine = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msp);
                            break;
                        case "DetectCondition":
                            detectCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msu);
                            break;
                        case "Protocol":
                            protocol = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "InstallSize":
                            installSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "KB":
                            msuKB = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msu);
                            break;
                        case "Compressed":
                            compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressLooseFilePayloadGeneration":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            suppressLooseFilePayloadGeneration = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "EnableSignatureVerification":
                            enableSignatureVerification = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Slipstream":
                            slipstream = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msp);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
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
                    this.Core.Write(ErrorMessages.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile"));
                }
                else if (null == remotePayload)
                {
                    sourceFile = Path.Combine("SourceDir", name);
                }
            }
            else if (null != remotePayload)
            {
                this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (sourceFile.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile", sourceFile));
                }
                else
                {
                    sourceFile = Path.Combine(sourceFile, Path.GetFileName(name));
                }
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (YesNoDefaultType.No != compressed && null != remotePayload)
            {
                compressed = YesNoDefaultType.No;
                this.Core.Write(WarningMessages.RemotePayloadsMustNotAlsoBeCompressed(sourceLineNumbers, node.Name.LocalName));
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(name));
                }
                else if (!String.IsNullOrEmpty(sourceFile))
                {
                    id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(sourceFile));
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
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
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Protocol", protocol, "none, burn, netfx4"));
            }

            if (!String.IsNullOrEmpty(protocol) && protocol.Equals("netfx4", StringComparison.Ordinal))
            {
                foreach (string expectedArgument in expectedNetFx4Args)
                {
                    if (null == installCommand || -1 == installCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "InstallCommand", installCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (null == repairCommand || -1 == repairCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "RepairCommand", repairCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (null == uninstallCommand || -1 == uninstallCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "UninstallCommand", uninstallCommand, expectedArgument, "Protocol", "netfx4"));
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
                this.Core.ParseExtensionAttribute(node, attribute, contextValues);
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
                        this.Core.UnexpectedElement(node, child);
                    }
                }
                else
                {
                    Dictionary<string, string> context = new Dictionary<string, string>() { { "Id", id.Id } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            if (!this.Core.EncounteredError)
            {
                // We create the package contents as a payload with this package as the parent
                this.CreatePayloadRow(sourceLineNumbers, id, name, sourceFile, downloadUrl, ComplexReferenceParentType.Package, id.Id,
                    ComplexReferenceChildType.Unknown, null, compressed, enableSignatureVerification, displayName, description, remotePayload);

                var chainItemRow = (WixChainItemTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixChainItem, id);

                WixBundlePackageAttributes attributes = 0;
                attributes |= (YesNoType.Yes == permanent) ? WixBundlePackageAttributes.Permanent : 0;
                attributes |= (YesNoType.Yes == visible) ? WixBundlePackageAttributes.Visible : 0;

                var chainPackageRow = (WixBundlePackageTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePackage, id);
                chainPackageRow.Type = packageType;
                chainPackageRow.Payload_ = id.Id;
                chainPackageRow.Attributes = attributes;

                chainPackageRow.InstallCondition = installCondition;

                if (YesNoAlwaysType.NotSet != cache)
                {
                    chainPackageRow.Cache = cache;
                }

                chainPackageRow.CacheId = cacheId;

                if (YesNoType.NotSet != vital)
                {
                    chainPackageRow.Vital = (vital == YesNoType.Yes);
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

                        var exeRow = (WixBundleExePackageTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleExePackage, id);
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

                        var msiRow = (WixBundleMsiPackageTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleMsiPackage, id);
                        msiRow.Attributes = msiAttributes;
                        break;

                    case WixBundlePackageType.Msp:
                        WixBundleMspPackageAttributes mspAttributes = 0;
                        mspAttributes |= (YesNoType.Yes == displayInternalUI) ? WixBundleMspPackageAttributes.DisplayInternalUI : 0;
                        mspAttributes |= (YesNoType.Yes == slipstream) ? WixBundleMspPackageAttributes.Slipstream : 0;

                        var mspRow = (WixBundleMspPackageTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleMspPackage, id);
                        mspRow.Attributes = mspAttributes;
                        break;

                    case WixBundlePackageType.Msu:
                        var msuRow = (WixBundleMsuPackageTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleMsuPackage, id);
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
                            installArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UninstallArgument":
                            uninstallArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RepairArgument":
                            repairArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (String.IsNullOrEmpty(condition))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Condition"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = (WixBundlePackageCommandLineTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePackageCommandLine);
                row.WixBundlePackage_ = packageId;
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundlePackageGroup, id);
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixBundlePackageGroup", id);
                            break;
                        case "After":
                            after = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);

                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null != after && ComplexReferenceParentType.Container == parentType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "After", parentId));
            }

            this.Core.ParseForExtensionElements(node);

            if (ComplexReferenceParentType.Container == parentType)
            {
                this.Core.CreateWixGroupRow(sourceLineNumbers, ComplexReferenceParentType.Container, parentId, ComplexReferenceChildType.PackageGroup, id);
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
            var row = (WixChainItemTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixChainItem, id);

            var rollbackBoundary = (WixBundleRollbackBoundaryTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleRollbackBoundary, id);

            if (YesNoType.NotSet != vital)
            {
                rollbackBoundary.Vital = (vital == YesNoType.Yes);
            }
            if (YesNoType.NotSet != transaction)
            {
                rollbackBoundary.Transaction = (transaction == YesNoType.Yes);
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
            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixOrdering);
                row.Set(0, itemType.ToString());
                row.Set(1, itemId);
                row.Set(2, dependsOnType.ToString());
                row.Set(3, dependsOnId);
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
                            name = this.Core.GetAttributeMsiPropertyNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = (WixBundleMsiPropertyTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleMsiProperty);
                row.WixBundlePackage_ = packageId;
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
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixBundlePackage", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = (WixBundleSlipstreamMspTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleSlipstreamMsp);
                row.WixBundlePackage_ = packageId;
                row.WixBundlePackage_Msp = id;
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
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Action":
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
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
                        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", action, "Detect", "Upgrade", "Addon", "Patch"));
                        break;
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixRelatedBundle);
                row.Set(0, id);
                row.Set(1, (int)actionType);
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
                            location = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == location)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Location"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleUpdate);
                row.Set(0, location);
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
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                hidden = true;
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeBundleVariableValue(sourceLineNumbers, attrib);
                            break;
                        case "Persisted":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                persisted = true;
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Type":
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            else if (name.StartsWith("Wix", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.Write(ErrorMessages.ReservedNamespaceViolation(sourceLineNumbers, node.Name.LocalName, "Name", "Wix"));
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, "Variable", "Value", "Type"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var row = (WixBundleVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixBundleVariable);
                row.WixBundleVariable = name;
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
                            requiredVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != requiredVersion)
            {
                this.Core.VerifyRequiredVersion(sourceLineNumbers, requiredVersion);
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
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
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
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Overridable":
                            overridable = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var wixVariableRow = (WixVariableTuple)this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixVariable, id);
                wixVariableRow.Value = value;
                wixVariableRow.Overridable = overridable;
            }
        }
    }
}
