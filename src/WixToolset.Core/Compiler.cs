// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        public const string DefaultComponentIdPlaceholderFormat = "WixComponentIdPlaceholder{0}";
        public const string DefaultComponentIdPlaceholderWixVariableFormat = "!(wix.{0})";
        // If these are true you know you are building a module or product
        // but if they are false you cannot not be sure they will not end
        // up a product or module.  Use these flags carefully.
        private bool compilingModule;
        private bool compilingProduct;

        private bool useShortFileNames;
        private string activeName;
        private string activeLanguage;

        // TODO: Implement this differently to not require the VariableResolver.
        private VariableResolver componentIdPlaceholdersResolver;

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
        public Intermediate Compile(ICompileContext context)
        {
            var target = new Intermediate();

            if (String.IsNullOrEmpty(context.CompilationId))
            {
                context.CompilationId = target.Id;
            }

            this.Context = context;

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
                this.componentIdPlaceholdersResolver = new VariableResolver(this.ServiceProvider);

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
                var regex = new Regex(@"\[(?<identifier>[a-zA-Z_][a-zA-Z0-9_\.]*)]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                var matches = regex.Matches(value);

                foreach (Match match in matches)
                {
                    var group = match.Groups["identifier"];
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
                    var id = String.Concat(this.Core.ActiveSection.Id, ".", property.Id);

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
            this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Implemented Categories\\", categoryId), "*", null, componentId);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string appId = null;
            string remoteServerName = null;
            string localService = null;
            string serviceParameters = null;
            string dllSurrogate = null;
            var activateAtStorage = YesNoType.NotSet;
            var appIdAdvertise = YesNoType.NotSet;
            var runAsInteractiveUser = YesNoType.NotSet;
            string description = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), null, description, componentId);
                }
                else
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "+", null, componentId);
                }

                if (null != remoteServerName)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "RemoteServerName", remoteServerName, componentId);
                }

                if (null != localService)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "LocalService", localService, componentId);
                }

                if (null != serviceParameters)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "ServiceParameters", serviceParameters, componentId);
                }

                if (null != dllSurrogate)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "DllSurrogate", dllSurrogate, componentId);
                }

                if (YesNoType.Yes == activateAtStorage)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "ActivateAtStorage", "Y", componentId);
                }

                if (YesNoType.Yes == runAsInteractiveUser)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "RunAs", "Interactive User", componentId);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string value = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;
            var suppressModularization = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string property = null;

            foreach (var attrib in node.Attributes())
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
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Instance":
                        this.ParseInstanceElement(child, property);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string productCode = null;
            string productName = null;
            string upgradeCode = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string appData = null;
            string feature = null;
            string qualifier = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string appId = null;
            string argument = null;
            var class16bit = false;
            var class32bit = false;
            string classId = null;
            var classAdvertise = YesNoType.NotSet;
            var contexts = new string[0];
            string formattedContextString = null;
            var control = false;
            string defaultInprocHandler = null;
            string defaultProgId = null;
            string description = null;
            string fileTypeMask = null;
            string foreignServer = null;
            string icon = null;
            var iconIndex = CompilerConstants.IntegerNotSet;
            string insertable = null;
            string localFileServer = null;
            var programmable = false;
            var relativePath = YesNoType.NotSet;
            var safeForInit = false;
            var safeForScripting = false;
            var shortServerPath = false;
            string threadingModel = null;
            string version = null;

            foreach (var attrib in node.Attributes())
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
                        iconIndex = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int16.MinValue + 1, Int16.MaxValue);
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

            var uniqueContexts = new HashSet<string>();
            foreach (var context in contexts)
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
            var fileTypeMaskIndex = 0;
            var firstProgIdForClass = YesNoType.Yes;

            foreach (var child in node.Elements())
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
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            this.Core.CreateRegistryRow(childSourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("FileType\\", classId, "\\", fileTypeMaskIndex.ToString()), String.Empty, this.ParseFileTypeMaskElement(child), componentId);
                            fileTypeMaskIndex++;
                        }
                        break;
                    case "Interface":
                        this.ParseInterfaceElement(child, componentId, class16bit ? classId : null, class32bit ? classId : null, typeLibId, typeLibVersion);
                        break;
                    case "ProgId":
                    {
                        var foundExtension = false;
                        var progId = this.ParseProgIdElement(child, componentId, advertise, classId, description, null, ref foundExtension, firstProgIdForClass);
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
                    foreach (var context in contexts)
                    {
                        var tuple = new ClassTuple(sourceLineNumbers)
                        {
                            CLSID = classId,
                            Context = context,
                            Component_ = componentId,
                            ProgId_Default = defaultProgId,
                            Description = description,
                            FileTypeMask = fileTypeMask,
                            DefInprocHandler = defaultInprocHandler,
                            Argument = argument,
                            Feature_ = Guid.Empty.ToString("B"),
                            RelativePath = YesNoType.Yes == relativePath,
                        };

                        if (null != appId)
                        {
                            tuple.AppId_ = appId;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "AppId", appId);
                        }

                        if (null != icon)
                        {
                            tuple.Icon_ = icon;
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Icon", icon);
                        }

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            tuple.IconIndex = iconIndex;
                        }

                        this.Core.AddTuple(tuple);
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
                foreach (var context in contexts)
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

                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", context), String.Empty, formattedContextString, componentId); // ClassId context

                    if (null != icon) // ClassId default icon
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "File", icon);

                        icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            icon = String.Concat(icon, ",", iconIndex);
                        }
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\DefaultIcon"), String.Empty, icon, componentId);
                    }
                }

                if (null != parentAppId) // ClassId AppId (must be specified via nesting, not with the AppId attribute)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId), "AppID", parentAppId, componentId);
                }

                if (null != description) // ClassId description
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId), String.Empty, description, componentId);
                }

                if (null != defaultInprocHandler)
                {
                    switch (defaultInprocHandler) // ClassId Default Inproc Handler
                    {
                    case "1":
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole2.dll", componentId);
                        break;
                    case "2":
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                        break;
                    case "3":
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole2.dll", componentId);
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                        break;
                    default:
                        this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, defaultInprocHandler, componentId);
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
                foreach (var context in contexts)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", context), "ThreadingModel", threadingModel, componentId);
                }
            }

            if (null != typeLibId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\TypeLib"), null, typeLibId, componentId);
            }

            if (null != version)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Version"), null, version, componentId);
            }

            if (null != insertable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", insertable), "*", null, componentId);
            }

            if (control)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Control"), "*", null, componentId);
            }

            if (programmable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Programmable"), "*", null, componentId);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string baseInterface = null;
            string interfaceId = null;
            string name = null;
            var numMethods = CompilerConstants.IntegerNotSet;
            var versioned = true;

            foreach (var attrib in node.Attributes())
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
                        numMethods = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "ProxyStubClassId":
                        proxyId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib);
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

            this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId), null, name, componentId);
            if (null != typeLibId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), null, typeLibId, componentId);
                if (versioned)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), "Version", typelibVersion, componentId);
                }
            }

            if (null != baseInterface)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\BaseInterface"), null, baseInterface, componentId);
            }

            if (CompilerConstants.IntegerNotSet != numMethods)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\NumMethods"), null, numMethods.ToString(), componentId);
            }

            if (null != proxyId)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid"), null, proxyId, componentId);
            }

            if (null != proxyId32)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid32"), null, proxyId32, componentId);
            }
        }

        /// <summary>
        /// Parses a CLSID's file type mask element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>String representing the file type mask elements.</returns>
        private string ParseFileTypeMaskElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var cb = 0;
            var offset = CompilerConstants.IntegerNotSet;
            string mask = null;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Mask":
                        mask = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Offset":
                        offset = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string upgradeCode = null;
            string language = null;
            string maximum = null;
            string minimum = null;
            var excludeLanguages = false;
            var maxInclusive = false; 
            var minInclusive = true;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "ExcludeLanguages":
                        excludeLanguages = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "IncludeMaximum":
                        maxInclusive = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "IncludeMinimum":
                        minInclusive = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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
                var tuple = new UpgradeTuple(sourceLineNumbers)
                {
                    UpgradeCode = upgradeCode,
                    VersionMin = minimum,
                    VersionMax = maximum,
                    Language = language,
                    ActionProperty = propertyId,
                    OnlyDetect = true,
                    ExcludeLanguages = excludeLanguages,
                    VersionMaxInclusive = maxInclusive,
                    VersionMinInclusive = minInclusive,
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a registry search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private string ParseRegistrySearchElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var explicitWin64 = false;
            Identifier id = null;
            string key = null;
            string name = null;
            string signature = null;
            RegistryRootType? root = null;
            var type = CompilerConstants.IntegerNotSet;
            var search64bit = false;

            foreach (var attrib in node.Attributes())
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
                        root = this.Core.GetAttributeRegistryRootValue(sourceLineNumbers, attrib, false);
                        break;
                    case "Type":
                        var typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (typeValue)
                        {
                        case "directory":
                            type = 0;
                            break;
                        case "file":
                            type = 1;
                            break;
                        case "raw":
                            type = 2;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "raw"));
                            break;
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

            if (!root.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (CompilerConstants.IntegerNotSet == type)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }

            signature = id.Id;
            var oneChild = false;
            foreach (var child in node.Elements())
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
                        var newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
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
                row.Set(1, (int)root);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var signatures = new List<string>();

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string signature = null;

            var oneChild = false;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (var attrib in node.Attributes())
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
            var signatures = this.ParseSearchSignatures(node);
            foreach (var sig in signatures)
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            var comPlusBits = CompilerConstants.IntegerNotSet;
            string condition = null;
            var encounteredODBCDataSource = false;
            var files = 0;
            var guid = "*";
            var componentIdPlaceholder = String.Format(Compiler.DefaultComponentIdPlaceholderFormat, this.componentIdPlaceholdersResolver.VariableCount); // placeholder id for defaulting Component/@Id to keypath id.
            var componentIdPlaceholderWixVariable = String.Format(Compiler.DefaultComponentIdPlaceholderWixVariableFormat, componentIdPlaceholder);
            var id = new Identifier(componentIdPlaceholderWixVariable, AccessModifier.Private);
            var keyFound = false;
            string keyPath = null;
            var shouldAddCreateFolder = false;

            var keyPathType = ComponentKeyPathType.Directory;
            var location = ComponentLocation.LocalOnly;
            var disableRegistryReflection = false;

            var neverOverwrite = false;
            var permanent = false;
            var shared = false;
            var sharedDllRefCount = false;
            var transitive = false;
            var uninstallWhenSuperseded = false;
            var explicitWin64 = false;
            var win64 = false;

            var multiInstance = false;
            var symbols = new List<string>();
            string feature = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "ComPlusFlags":
                        comPlusBits = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "DisableRegistryReflection":
                        disableRegistryReflection = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesDisableRegistryReflection;
                        //}
                        break;
                    case "Directory":
                        directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, directoryId);
                        break;
                    case "DiskId":
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
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
                            shouldAddCreateFolder = true;
                        }
                        break;
                    case "Location":
                        var locationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (locationValue)
                        {
                        case "either":
                            location = ComponentLocation.Either;
                            //bits |= MsiInterop.MsidbComponentAttributesOptional;
                            break;
                        case "local": // this is the default
                            location = ComponentLocation.LocalOnly;
                            break;
                        case "source":
                            location = ComponentLocation.SourceOnly;
                            //bits |= MsiInterop.MsidbComponentAttributesSourceOnly;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, locationValue, "either", "local", "source"));
                            break;
                        }
                        break;
                    case "MultiInstance":
                        multiInstance = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "NeverOverwrite":
                        neverOverwrite = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesNeverOverwrite;
                        //}
                        break;
                    case "Permanent":
                        permanent = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesPermanent;
                        //}
                        break;
                    case "Shared":
                        shared = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesShared;
                        //}
                        break;
                    case "SharedDllRefCount":
                        sharedDllRefCount = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesSharedDllRefCount;
                        //}
                        break;
                    case "Transitive":
                        transitive = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesTransitive;
                        //}
                        break;
                    case "UninstallWhenSuperseded":
                        uninstallWhenSuperseded = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributesUninstallOnSupersedence;
                        //}
                        break;
                    case "Win64":
                        explicitWin64 = true;
                        win64 = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbComponentAttributes64bit;
                        //    win64 = true;
                        //}
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
                //bits |= MsiInterop.MsidbComponentAttributes64bit;
                win64 = true;
            }

            if (null == directoryId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Directory"));
            }

            if (String.IsNullOrEmpty(guid) && shared /*MsiInterop.MsidbComponentAttributesShared == (bits & MsiInterop.MsidbComponentAttributesShared)*/)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Shared", "yes", "Guid", ""));
            }

            if (String.IsNullOrEmpty(guid) && permanent /*MsiInterop.MsidbComponentAttributesPermanent == (bits & MsiInterop.MsidbComponentAttributesPermanent)*/)
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

            foreach (var child in node.Elements())
            {
                var keyPathSet = YesNoType.NotSet;
                string keyPossible = null;
                ComponentKeyPathType? keyBit = null;

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
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                            this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                        }
                        condition = this.ParseConditionElement(child, node.Name.LocalName, null, null);
                        break;
                    case "CopyFile":
                        this.ParseCopyFileElement(child, id.Id, null);
                        break;
                    case "CreateFolder":
                        var createdFolder = this.ParseCreateFolderElement(child, id.Id, directoryId, win64);
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
                        keyBit = ComponentKeyPathType.File;
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
                        keyBit = ComponentKeyPathType.OdbcDataSource;
                        encounteredODBCDataSource = true;
                        break;
                    case "ODBCDriver":
                        this.ParseODBCDriverOrTranslator(child, id.Id, null, TupleDefinitionType.ODBCDriver);
                        break;
                    case "ODBCTranslator":
                        this.ParseODBCDriverOrTranslator(child, id.Id, null, TupleDefinitionType.ODBCTranslator);
                        break;
                    case "ProgId":
                        var foundExtension = false;
                        this.ParseProgIdElement(child, id.Id, YesNoType.NotSet, null, null, null, ref foundExtension, YesNoType.NotSet);
                        break;
                    case "RegistryKey":
                        keyPathSet = this.ParseRegistryKeyElement(child, id.Id, null, null, win64, out keyPossible);
                        keyBit = ComponentKeyPathType.Registry;
                        break;
                    case "RegistryValue":
                        keyPathSet = this.ParseRegistryValueElement(child, id.Id, null, null, win64, out keyPossible);
                        keyBit = ComponentKeyPathType.Registry;
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
                    var context = new Dictionary<string, string>() { { "ComponentId", id.Id }, { "DirectoryId", directoryId }, { "Win64", win64.ToString() }, };
                    var possibleKeyPath = this.Core.ParsePossibleKeyPathExtensionElement(node, child, context);
                    if (null != possibleKeyPath)
                    {
                        if (PossibleKeyPathType.None == possibleKeyPath.Type)
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

                            if (PossibleKeyPathType.Registry == possibleKeyPath.Type || PossibleKeyPathType.RegistryFormatted == possibleKeyPath.Type)
                            {
                                keyBit = ComponentKeyPathType.Registry; //MsiInterop.MsidbComponentAttributesRegistryKeyPath;
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
                    keyPathType = keyBit.Value;
                }
            }

            if (shouldAddCreateFolder)
            {
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CreateFolder, new Identifier(AccessModifier.Public, directoryId, id.Id));
                row.Set(0, directoryId);
                row.Set(1, id.Id);
            }

            // check for conditions that exclude this component from using generated guids
            var isGeneratableGuidOk = "*" == guid;
            if (isGeneratableGuidOk)
            {
                if (encounteredODBCDataSource)
                {
                    this.Core.Write(ErrorMessages.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers));
                    isGeneratableGuidOk = false;
                }

                if (0 < files && ComponentKeyPathType.Registry == keyPathType)
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
                var tuple = new ComponentTuple(sourceLineNumbers, id)
                {
                    Component = id.Id,
                    ComponentId = guid,
                    Directory_ = directoryId,
                    Location = location,
                    Condition = condition,
                    KeyPath = keyPath,
                    KeyPathType = keyPathType,
                    DisableRegistryReflection = disableRegistryReflection,
                    NeverOverwrite = neverOverwrite,
                    Permanent = permanent,
                    SharedDllRefCount = sharedDllRefCount,
                    Transitive = transitive,
                    UninstallWhenSuperseded = uninstallWhenSuperseded,
                    Win64 = win64,
                };

                this.Core.AddTuple(tuple);

                //var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Component, id);
                //row.Set(1, guid);
                //row.Set(2, directoryId);
                //row.Set(3, bits | keyBits);
                //row.Set(4, condition);
                //row.Set(5, keyPath);

                if (multiInstance)
                {
                    //var instanceComponentRow = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixInstanceComponent, id);

                    var instanceComponentTuple = new WixInstanceComponentTuple(sourceLineNumbers, id)
                    {
                        Component_ = id.Id,
                    };

                    this.Core.AddTuple(instanceComponentTuple);
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
                    var complusTuple = new ComplusTuple(sourceLineNumbers)
                    {
                        Component_ = id.Id,
                        ExpType = comPlusBits,
                    };

                    this.Core.AddTuple(complusTuple);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string directoryId = null;
            string source = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var primary = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var primary = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string componentId = null;
            var type = LocatorType.Filename;

            foreach (var attrib in node.Attributes())
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
                        var typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (typeValue)
                        {
                        case "directory":
                            type = LocatorType.Directory;
                            break;
                        case "file":
                            type = LocatorType.Filename;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue, "directory", "file"));
                            break;
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

            var signature = id.Id;
            var oneChild = false;
            foreach (var child in node.Elements())
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
                        var newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
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
                var tuple = new CompLocatorTuple(sourceLineNumbers, id)
                {
                    ComponentId = componentId,
                    Type = type,
                };

                this.Core.AddTuple(tuple);

                //var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CompLocator, id);
                //row.Set(1, componentId);
                //row.Set(2, type);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
                    var context = new Dictionary<string, string>() { { "DirectoryId", directoryId }, { "ComponentId", componentId }, { "Win64", win64Component.ToString() } };
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var delete = false;
            string destinationDirectory = null;
            string destinationName = null;
            string destinationShortName = null;
            string destinationProperty = null;
            string sourceDirectory = null;
            string sourceFolder = null;
            string sourceName = null;
            string sourceProperty = null;

            foreach (var attrib in node.Attributes())
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
                    row.Set(3, String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : this.GetMsiFilenameValue(destinationShortName, destinationName));
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
                    row.Set(3, String.IsNullOrEmpty(destinationShortName) && String.IsNullOrEmpty(destinationName) ? null : this.GetMsiFilenameValue(destinationShortName, destinationName));
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var inlineScript = false;
            var suppressModularization = YesNoType.NotSet;
            string source = null;
            string target = null;
            var explicitWin64 = false;

            CustomActionSourceType? sourceType = null;
            CustomActionTargetType? targetType = null;
            var executionType = CustomActionExecutionType.Immediate;
            var hidden = false;
            var impersonate = true;
            var patchUninstall = false;
            var tsAware = false;
            var win64 = false;
            var async = false;
            var ignoreResult = false;

            foreach (var attrib in node.Attributes())
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
                        //sourceBits = MsiInterop.MsidbCustomActionTypeBinaryData;
                        sourceType = CustomActionSourceType.Binary;
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", source); // add a reference to the appropriate Binary
                        break;
                    case "Directory":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                        }
                        source = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                        //sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                        sourceType = CustomActionSourceType.Directory;
                        break;
                    case "DllEntry":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        //targetBits = MsiInterop.MsidbCustomActionTypeDll;
                        targetType = CustomActionTargetType.Dll;
                        break;
                    case "Error":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        //targetBits = MsiInterop.MsidbCustomActionTypeTextData | MsiInterop.MsidbCustomActionTypeSourceFile;
                        sourceType = CustomActionSourceType.File;
                        targetType = CustomActionTargetType.TextData;

                        // The target can be either a formatted error string or a literal 
                        // error number. Try to convert to error number to determine whether
                        // to add a reference. No need to look at the value.
                        if (Int32.TryParse(target, out var ignored))
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
                        //targetBits = MsiInterop.MsidbCustomActionTypeExe;
                        targetType = CustomActionTargetType.Exe;
                        break;
                    case "Execute":
                        var execute = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (execute)
                        {
                        case "commit":
                            //bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeCommit;
                            executionType = CustomActionExecutionType.Commit;
                            break;
                        case "deferred":
                            //bits |= MsiInterop.MsidbCustomActionTypeInScript;
                            executionType = CustomActionExecutionType.Deferred;
                            break;
                        case "firstSequence":
                            //bits |= MsiInterop.MsidbCustomActionTypeFirstSequence;
                            executionType = CustomActionExecutionType.FirstSequence;
                            break;
                        case "immediate":
                            executionType = CustomActionExecutionType.Immediate;
                            break;
                        case "oncePerProcess":
                            //bits |= MsiInterop.MsidbCustomActionTypeOncePerProcess;
                            executionType = CustomActionExecutionType.OncePerProcess;
                            break;
                        case "rollback":
                            //bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeRollback;
                            executionType = CustomActionExecutionType.Rollback;
                            break;
                        case "secondSequence":
                            //bits |= MsiInterop.MsidbCustomActionTypeClientRepeat;
                            executionType = CustomActionExecutionType.ClientRepeat;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, execute, "commit", "deferred", "firstSequence", "immediate", "oncePerProcess", "rollback", "secondSequence"));
                            break;
                        }
                        break;
                    case "FileKey":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        //sourceBits = MsiInterop.MsidbCustomActionTypeSourceFile;
                        sourceType = CustomActionSourceType.File;
                        this.Core.CreateSimpleReference(sourceLineNumbers, "File", source); // add a reference to the appropriate File
                        break;
                    case "HideTarget":
                        hidden = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbCustomActionTypeHideTarget;
                        //}
                        break;
                    case "Impersonate":
                        impersonate = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbCustomActionTypeNoImpersonate;
                        //}
                        break;
                    case "JScriptCall":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        //targetBits = MsiInterop.MsidbCustomActionTypeJScript;
                        targetType = CustomActionTargetType.JScript;
                        break;
                    case "PatchUninstall":
                        patchUninstall = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    extendedBits |= MsiInterop.MsidbCustomActionTypePatchUninstall;
                        //}
                        break;
                    case "Property":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        //sourceBits = MsiInterop.MsidbCustomActionTypeProperty;
                        sourceType = CustomActionSourceType.Property;
                        break;
                    case "Return":
                        var returnValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (returnValue)
                        {
                        case "asyncNoWait":
                            //bits |= MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue;
                            async = true;
                            ignoreResult = true;
                            break;
                        case "asyncWait":
                            //bits |= MsiInterop.MsidbCustomActionTypeAsync;
                            async = true;
                            break;
                        case "check":
                            break;
                        case "ignore":
                            //bits |= MsiInterop.MsidbCustomActionTypeContinue;
                            ignoreResult = true;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, returnValue, "asyncNoWait", "asyncWait", "check", "ignore"));
                            break;
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
                        source = String.Empty;
                        target = String.Empty;

                        inlineScript = true;

                        var script = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (script)
                        {
                        case "jscript":
                            //sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                            sourceType = CustomActionSourceType.Directory;
                            //targetBits = MsiInterop.MsidbCustomActionTypeJScript;
                            targetType = CustomActionTargetType.JScript;
                            break;
                        case "vbscript":
                            //sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
                            sourceType = CustomActionSourceType.Directory;
                            //targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
                            targetType = CustomActionTargetType.VBScript;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, script, "jscript", "vbscript"));
                            break;
                        }
                        break;
                    case "SuppressModularization":
                        suppressModularization = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "TerminalServerAware":
                        tsAware = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbCustomActionTypeTSAware;
                        //}
                        break;
                    case "Value":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        //targetBits = MsiInterop.MsidbCustomActionTypeTextData;
                        targetType = CustomActionTargetType.TextData;
                        break;
                    case "VBScriptCall":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        //targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
                        targetType = CustomActionTargetType.VBScript;
                        break;
                    case "Win64":
                        explicitWin64 = true;
                        win64 = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        //if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        //{
                        //    bits |= MsiInterop.MsidbCustomActionType64BitScript;
                        //}
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

            if (!explicitWin64 && (CustomActionTargetType.VBScript == targetType || CustomActionTargetType.JScript == targetType) && (Platform.IA64 == this.CurrentPlatform || Platform.X64 == this.CurrentPlatform))
            {
                win64 = true;
            }

            // get the inner text if any exists
            var innerText = this.Core.GetTrimmedInnerText(node);

            // if we have an in-lined Script CustomAction ensure no source or target attributes were provided
            if (inlineScript)
            {
                target = innerText;
            }
            else if (CustomActionTargetType.VBScript == targetType) // non-inline vbscript
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (CustomActionSourceType.Directory == sourceType)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "Directory"));
                }
            }
            else if (CustomActionTargetType.JScript == targetType) // non-inline jscript
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "BinaryKey", "FileKey", "Property"));
                }
                else if (CustomActionSourceType.Directory == sourceType)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "Directory"));
                }
            }
            else if (CustomActionTargetType.Exe == targetType) // exe-command
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ExeCommand", "BinaryKey", "Directory", "FileKey", "Property"));
                }
            }
            else if (CustomActionTargetType.TextData == targetType && CustomActionSourceType.Directory != sourceType && CustomActionSourceType.Property != sourceType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Value", "Directory", "Property"));
            }
            else if (!String.IsNullOrEmpty(innerText)) // inner text cannot be specified with non-script CAs
            {
                this.Core.Write(ErrorMessages.CustomActionIllegalInnerText(sourceLineNumbers, node.Name.LocalName, innerText, "Script"));
            }

            if (win64 && CustomActionTargetType.VBScript != targetType && CustomActionTargetType.JScript != targetType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Win64", "Script", "VBScriptCall", "JScriptCall"));
            }

            if (async && ignoreResult && CustomActionTargetType.Exe != targetType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Return", "asyncNoWait", "ExeCommand"));
            }

            // TS-aware CAs are valid only when deferred.
            if (tsAware &
                CustomActionExecutionType.Deferred != executionType &&
                CustomActionExecutionType.Rollback != executionType &&
                CustomActionExecutionType.Commit != executionType)
            {
                this.Core.Write(ErrorMessages.IllegalTerminalServerCustomActionAttributes(sourceLineNumbers));
            }

            // MSI doesn't support in-script property setting, so disallow it
            if (CustomActionSourceType.Property == sourceType &&
                CustomActionTargetType.TextData == targetType &&
                (CustomActionExecutionType.Deferred == executionType ||
                 CustomActionExecutionType.Rollback == executionType ||
                 CustomActionExecutionType.Commit == executionType))
            {
                this.Core.Write(ErrorMessages.IllegalPropertyCustomActionAttributes(sourceLineNumbers));
            }

            if (!targetType.HasValue /*0 == targetBits*/)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new CustomActionTuple(sourceLineNumbers, id)
                {
                    ExecutionType = executionType,
                    Source = source,
                    SourceType = sourceType.Value,
                    Target = target,
                    TargetType = targetType.Value,
                    Async = async,
                    IgnoreResult = ignoreResult,
                    Impersonate = impersonate,
                    PatchUninstall = patchUninstall,
                    TSAware = tsAware,
                    Win64 = win64,
                };
                //var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.CustomAction, id);
                //row.Set(1, bits | sourceBits | targetBits);
                //row.Set(2, source);
                //row.Set(3, target);
                //if (0 != extendedBits)
                //{
                //    row.Set(4, extendedBits);
                //}

                this.Core.AddTuple(tuple);

                if (YesNoType.Yes == suppressModularization)
                {
                    this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.WixSuppressModularization, id);
                }

                // For deferred CAs that specify HideTarget we should also hide the CA data property for the action.
                if (hidden &&
                    (CustomActionExecutionType.Deferred == executionType ||
                     CustomActionExecutionType.Commit == executionType ||
                     CustomActionExecutionType.Rollback == executionType))
                {
                    this.AddWixPropertyRow(sourceLineNumbers, id, false, false, hidden);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var primaryKeys = new string[2];

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string tableId = null;

            string categories = null;
            var columnCount = 0;
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
            var bootstrapperApplicationData = false;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                    case "Column":
                        ++columnCount;

                        var category = String.Empty;
                        string columnName = null;
                        string columnType = null;
                        var description = String.Empty;
                        var keyColumn = CompilerConstants.IntegerNotSet;
                        var keyTable = String.Empty;
                        var localizable = false;
                        var maxValue = CompilerConstants.LongNotSet;
                        var minValue = CompilerConstants.LongNotSet;
                        var modularization = "None";
                        var nullable = false;
                        var primaryKey = false;
                        var setValues = String.Empty;
                        string typeName = null;
                        var width = 0;

                        foreach (var childAttrib in child.Attributes())
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
                                maxValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, Int32.MinValue + 1, Int32.MaxValue);
                                break;
                            case "MinValue":
                                minValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, Int32.MinValue + 1, Int32.MaxValue);
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
                                var typeValue = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                                switch (typeValue)
                                {
                                case "binary":
                                    typeName = "OBJECT";
                                    break;
                                case "int":
                                    typeName = "SHORT";
                                    break;
                                case "string":
                                    typeName = "CHAR";
                                    break;
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Type", typeValue, "binary", "int", "string"));
                                    break;
                                }
                                break;
                            case "Width":
                                width = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 0, Int32.MaxValue);
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
                            var typeChar = localizable ? "l" : "s";
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

                        foreach (var childAttrib in child.Attributes())
                        {
                            this.Core.ParseExtensionAttribute(child, childAttrib);
                        }

                        foreach (var data in child.Elements())
                        {
                            var dataSourceLineNumbers = Preprocessor.GetSourceLineNumbers(data);
                            switch (data.Name.LocalName)
                            {
                            case "Data":
                                columnName = null;
                                foreach (var dataAttrib in data.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string componentGuidGenerationSeed = null;
            var fileSourceAttribSet = false;
            var nameHasValue = false;
            var name = "."; // default to parent directory.
            string[] inlineSyntax = null;
            string shortName = null;
            string sourceName = null;
            string shortSourceName = null;
            string defaultDir = null;
            string symbols = null;

            foreach (var attrib in node.Attributes())
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
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
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
                    var pathStartsAt = 0;
                    if (inlineSyntax[0].EndsWith(":"))
                    {
                        parentId = inlineSyntax[0].TrimEnd(':');
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", parentId);

                        pathStartsAt = 1;
                    }

                    for (var i = pathStartsAt; i < inlineSyntax.Length - 1; ++i)
                    {
                        var inlineId = this.Core.CreateDirectoryRow(sourceLineNumbers, null, parentId, inlineSyntax[i]);
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var diskId = CompilerConstants.IntegerNotSet;
            var fileSource = String.Empty;

            foreach (var attrib in node.Attributes())
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
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var depth = CompilerConstants.IntegerNotSet;
            string path = null;
            var assignToProperty = false;
            string signature = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Depth":
                        depth = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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

            var oneChild = false;
            var hasFileSearch = false;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            Identifier parent = null;
            string path = null;
            string signature = null;

            foreach (var attrib in node.Attributes())
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

            var oneChild = false;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string configurableDirectory = null;
            string description = null;
            var displayValue = "collapse";
            var level = 1;
            string title = null;

            var installDefault = FeatureInstallDefault.Local;
            var typicalDefault = FeatureTypicalDefault.Install;
            var disallowAbsent = false;
            var disallowAdvertise = false;
            var display = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Absent":
                        var absentValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (absentValue)
                        {
                        case "allow": // this is the default
                            break;
                        case "disallow":
                            //bits |= MsiInterop.MsidbFeatureAttributesUIDisallowAbsent;
                            disallowAbsent = true;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, absentValue, "allow", "disallow"));
                            break;
                        }
                        break;
                    case "AllowAdvertise":
                        var advertiseValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (advertiseValue)
                        {
                        case "disallow":
                        case "no":
                            //bits |= MsiInterop.MsidbFeatureAttributesDisallowAdvertise;
                            disallowAdvertise = true;
                            break;
                        case "allow":
                        case "yes": // this is the default
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, advertiseValue, "no", "system", "yes"));
                            break;
                        }
                        break;
                    case "ConfigurableDirectory":
                        configurableDirectory = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, attrib, null);
                        break;
                    case "Description":
                        description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Display":
                        displayValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "InstallDefault":
                        var installDefaultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (installDefaultValue)
                        {
                        case "followParent":
                            if (ComplexReferenceParentType.Product == parentType)
                            {
                                this.Core.Write(ErrorMessages.RootFeatureCannotFollowParent(sourceLineNumbers));
                            }
                            //bits = bits | MsiInterop.MsidbFeatureAttributesFollowParent;
                            installDefault = FeatureInstallDefault.FollowParent;
                            break;
                        case "local": // this is the default
                            installDefault = FeatureInstallDefault.Local;
                            break;
                        case "source":
                            //bits = bits | MsiInterop.MsidbFeatureAttributesFavorSource;
                            installDefault = FeatureInstallDefault.Source;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, installDefaultValue, "followParent", "local", "source"));
                            break;
                        }
                        break;
                    case "Level":
                        level = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Title":
                        title = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if ("PUT-FEATURE-TITLE-HERE" == title)
                        {
                            this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, title));
                        }
                        break;
                    case "TypicalDefault":
                        var typicalValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (typicalValue)
                        {
                        case "advertise":
                            //bits |= MsiInterop.MsidbFeatureAttributesFavorAdvertise;
                            typicalDefault = FeatureTypicalDefault.Advertise;
                            break;
                        case "install": // this is the default
                            typicalDefault = FeatureTypicalDefault.Install;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typicalValue, "advertise", "install"));
                            break;
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

            if (FeatureTypicalDefault.Advertise == typicalDefault && disallowAdvertise)
            {
                this.Core.Write(ErrorMessages.FeatureCannotFavorAndDisallowAdvertise(sourceLineNumbers, node.Name.LocalName, "TypicalDefault", "advertise", "AllowAdvertise", "no"));
            }

            var childDisplay = 0;
            foreach (var child in node.Elements())
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

            switch (displayValue)
            {
            case "collapse":
                lastDisplay = (lastDisplay | 1) + 1;
                display = lastDisplay;
                break;
            case "expand":
                lastDisplay = (lastDisplay + 1) | 1;
                display = lastDisplay;
                break;
            case "hidden":
                display = 0;
                break;
            default:
                if (!Int32.TryParse(displayValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out display))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Display", displayValue, "collapse", "expand", "hidden"));
                }
                else
                {
                    // Save the display value (if its not hidden) for subsequent rows
                    if (0 != display)
                    {
                        lastDisplay = display;
                    }
                }
                break;
            }

            if (!this.Core.EncounteredError)
            {
                var tuple = new FeatureTuple(sourceLineNumbers, id)
                {
                    Title = title,
                    Description = description,
                    Display = display,
                    Level = level,
                    Directory_ = configurableDirectory,
                    DisallowAbsent = disallowAbsent,
                    DisallowAdvertise = disallowAdvertise,
                    InstallDefault = installDefault,
                    TypicalDefault = typicalDefault,
                };

                this.Core.AddTuple(tuple);
                //var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Feature, id);
                //// row.Set(1, null); - this column is set in the linker
                //row.Set(2, title);
                //row.Set(3, description);
                //if (0 < display.Length)
                //{
                //    switch (display)
                //    {
                //    case "collapse":
                //        lastDisplay = (lastDisplay | 1) + 1;
                //        row.Set(4, lastDisplay);
                //        break;
                //    case "expand":
                //        lastDisplay = (lastDisplay + 1) | 1;
                //        row.Set(4, lastDisplay);
                //        break;
                //    case "hidden":
                //        row.Set(4, 0);
                //        break;
                //    default:
                //        int value;
                //        if (!Int32.TryParse(display, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                //        {
                //            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Display", display, "collapse", "expand", "hidden"));
                //        }
                //        else
                //        {
                //            row.Set(4, value);
                //            // save the display value of this row (if its not hidden) for subsequent rows
                //            if (0 != (int)row[4])
                //            {
                //                lastDisplay = (int)row[4];
                //            }
                //        }
                //        break;
                //    }
                //}
                //row.Set(5, level);
                //row.Set(6, configurableDirectory);
                //row.Set(7, bits);

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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var ignoreParent = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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

            var lastDisplay = 0;
            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (var attrib in node.Attributes())
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

            var lastDisplay = 0;
            foreach (var child in node.Elements())
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

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var ignoreParent = YesNoType.NotSet;
            var primary = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            EnvironmentActionType? action = null;
            EnvironmentPartType? part = null;
            var permanent = false;
            var separator = ";"; // default to ';'
            var system = false;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Action":
                        var actionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (actionValue)
                        {
                        case "create":
                            action = EnvironmentActionType.Create;
                            break;
                        case "set":
                            action = EnvironmentActionType.Set;
                            break;
                        case "remove":
                            action = EnvironmentActionType.Remove;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "create", "set", "remove"));
                            break;
                        }
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Part":
                        var partValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (partValue)
                        {
                        case "all":
                            part = EnvironmentPartType.All;
                            break;
                        case "first":
                            part = EnvironmentPartType.First;
                            break;
                        case "last":
                            part = EnvironmentPartType.Last;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Part", partValue, "all", "first", "last"));
                            break;
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
                id = this.Core.CreateIdentifier("env", ((int?)action)?.ToString(), name, ((int?)part)?.ToString(), system.ToString());
            }

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (!part.HasValue && action == EnvironmentActionType.Create)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Part", "Action", "create"));
            }

            //if (Wix.Environment.PartType.NotSet != partType)
            //{
            //    if ("+" == action)
            //    {
            //        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Part", "Action", "create"));
            //    }

            //    switch (partType)
            //    {
            //    case Wix.Environment.PartType.all:
            //        break;
            //    case Wix.Environment.PartType.first:
            //        text = String.Concat(text, separator, "[~]");
            //        break;
            //    case Wix.Environment.PartType.last:
            //        text = String.Concat("[~]", separator, text);
            //        break;
            //    }
            //}

            //if (permanent)
            //{
            //    uninstall = null;
            //}

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new EnvironmentTuple(sourceLineNumbers, id)
                {
                    Name = name,
                    Value = value,
                    Separator = separator,
                    Action = action,
                    Part = part,
                    Permanent = permanent,
                    System = system,
                    Component_ = componentId
                };

                this.Core.AddTuple(tuple);

                //var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Environment, id);
                //row.Set(1, String.Concat(action, uninstall, system ? "*" : String.Empty, name));
                //row.Set(2, text);
                //row.Set(3, componentId);
            }
        }

        /// <summary>
        /// Parses an error element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseErrorElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var id = CompilerConstants.IntegerNotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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
                var row = this.Core.CreateRow(sourceLineNumbers, TupleDefinitionType.Error, new Identifier(AccessModifier.Public, id));
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string extension = null;
            string mime = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        extension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Advertise":
                        var extensionAdvertise = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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
                    var context = new Dictionary<string, string>() { { "ProgId", progId }, { "ComponentId", componentId } };
                    this.Core.ParseExtensionAttribute(node, attrib, context);
                }
            }

            if (YesNoType.NotSet == advertise)
            {
                advertise = YesNoType.No;
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Verb":
                        this.ParseVerbElement(child, extension, progId, componentId, advertise);
                        break;
                    case "MIME":
                        var newMime = this.ParseMIMEElement(child, extension, componentId, advertise);
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
                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat(".", extension), String.Empty, progId, componentId); // Extension
                if (null != mime)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat(".", extension), "Content Type", mime, componentId); // Extension's MIME ContentType
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
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private YesNoType ParseFileElement(XElement node, string componentId, string directoryId, int diskId, string sourcePath, out string possibleKeyPath, bool win64Component, string componentGuid)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var assemblyType = FileAssemblyType.NotAnAssembly;
            string assemblyApplication = null;
            string assemblyManifest = null;
            string bindPath = null;

            //int bits = MsiInterop.MsidbFileAttributesVital;
            var readOnly = false;
            var checksum = false;
            bool? compressed = null;
            var hidden = false;
            var system = false;
            var vital = true; // assume all files are vital.

            string companionFile = null;
            string defaultLanguage = null;
            var defaultSize = 0;
            string defaultVersion = null;
            string fontTitle = null;
            var generatedShortFileName = false;
            var keyPath = YesNoType.NotSet;
            string name = null;
            var patchGroup = CompilerConstants.IntegerNotSet;
            var patchIgnore = false;
            var patchIncludeWholeFile = false;
            var patchAllowIgnoreOnError = false;

            string ignoreLengths = null;
            string ignoreOffsets = null;
            string protectLengths = null;
            string protectOffsets = null;
            string symbols = null;

            string procArch = null;
            var selfRegCost = CompilerConstants.IntegerNotSet;
            string shortName = null;
            var source = sourcePath;   // assume we'll use the parents as the source for this file
            var sourceSet = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Assembly":
                        var assemblyValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (assemblyValue)
                        {
                        case ".net":
                            assemblyType = FileAssemblyType.DotNetAssembly;
                            break;
                        case "no":
                            assemblyType = FileAssemblyType.NotAnAssembly;
                            break;
                        case "win32":
                            assemblyType = FileAssemblyType.Win32Assembly;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "Assembly", assemblyValue, "no", "win32", ".net"));
                            break;
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
                        var compressedValue = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
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
                        defaultSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "DefaultVersion":
                        defaultVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DiskId":
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
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
                        patchGroup = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int32.MaxValue);
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
                        var procArchValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (procArchValue)
                        {
                        case "msil":
                            procArch = "MSIL";
                            break;
                        case "x86":
                            procArch = "x86";
                            break;
                        case "x64":
                            procArch = "amd64";
                            break;
                        case "ia64":
                            procArch = "ia64";
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "ProcessorArchitecture", procArchValue, "msil", "x86", "x64", "ia64"));
                            break;
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
                        selfRegCost = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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
                        var isVital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            foreach (var child in node.Elements())
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
                    var context = new Dictionary<string, string>() { { "FileId", id.Id }, { "ComponentId", componentId }, { "DirectoryId", directoryId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var patchAttributes = PatchAttributeType.None;
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string languages = null;
            var minDate = CompilerConstants.IntegerNotSet;
            var maxDate = CompilerConstants.IntegerNotSet;
            var maxSize = CompilerConstants.IntegerNotSet;
            var minSize = CompilerConstants.IntegerNotSet;
            string maxVersion = null;
            string minVersion = null;
            string name = null;
            string shortName = null;

            foreach (var attrib in node.Attributes())
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
                        minSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "MaxSize":
                        maxSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
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

            var isSameId = String.Equals(id.Id, parentSignature, StringComparison.Ordinal);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            this.activeName = null;
            this.activeLanguage = null;

            foreach (var attrib in node.Attributes())
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

            var featureDisplay = 0;
            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            string condition = null;
            var level = CompilerConstants.IntegerNotSet;
            string message = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Action":
                        if ("Control" == parentElementLocalName)
                        {
                            action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (action)
                            {
                            case "default":
                                action = "Default";
                                break;
                            case "disnable":
                                action = "Disable";
                                break;
                            case "enable":
                                action = "Enable";
                                break;
                            case "hide":
                                action = "Hide";
                                break;
                            case "show":
                                action = "Show";
                                break;
                            case "":
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, action, "default", "disable", "enable", "hide", "show"));
                                break;
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
                            level = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            InifFileActionType? action = null;
            string directory = null;
            string key = null;
            string name = null;
            string section = null;
            string shortName = null;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Action":
                        var actionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (actionValue)
                        {
                            case "addLine":
                                action = InifFileActionType.AddLine;
                                break;
                            case "addTag":
                                action = InifFileActionType.AddTag;
                                break;
                            case "removeLine":
                                action = InifFileActionType.RemoveLine;
                                break;
                            case "removeTag":
                                action = InifFileActionType.RemoveTag;
                                break;
                            case "": // error case handled by GetAttributeValue()
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", actionValue, "addLine", "addTag", "createLine", "removeLine", "removeTag"));
                                break;
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

            if (!action.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }
            else if (InifFileActionType.AddLine == action || InifFileActionType.AddTag == action || InifFileActionType.CreateLine == action)
            {
                if (null == value)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
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

            if (!this.Core.EncounteredError)
            {
                var tuple = new IniFileTuple(sourceLineNumbers, id)
                {
                    FileName = this.GetMsiFilenameValue(shortName, name),
                    DirProperty = directory,
                    Section = section,
                    Key = key,
                    Value = value,
                    Action = action.Value,
                    Component_ = componentId
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses an IniFile search element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Signature for search element.</returns>
        private string ParseIniFileSearchElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var field = CompilerConstants.IntegerNotSet;
            string key = null;
            string name = null;
            string section = null;
            string shortName = null;
            string signature = null;
            var type = 1; // default is file

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Field":
                        field = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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
                        var typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (typeValue)
                        {
                        case "directory":
                            type = 0;
                            break;
                        case "file":
                            type = 1;
                            break;
                        case "raw":
                            type = 2;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "registry"));
                            break;
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

            var oneChild = false;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
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
                        var newId = this.ParseSimpleRefElement(child, "Signature"); // FileSearch signatures override parent signatures
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
                row.Set(1, this.GetMsiFilenameValue(shortName, name));
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string shared = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // no attributes are supported for this element
            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "DigitalCertificate":
                        var name = this.ParseDigitalCertificateElement(child);

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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string certificateId = null;
            string sourceFile = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var migrateFeatures = true;
            var ignoreRemoveFailure = false;
            var allowDowngrades = false;
            var allowSameVersionUpgrades = false;
            var blockUpgrades = false;
            string downgradeErrorMessage = null;
            string disallowUpgradeErrorMessage = null;
            string removeFeatures = null;
            string schedule = null;

            var upgradeCode = contextValues["UpgradeCode"];
            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Product", "UpgradeCode", node.Name.LocalName));
            }

            var productVersion = contextValues["ProductVersion"];
            if (String.IsNullOrEmpty(productVersion))
            {
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Product", "Version", node.Name.LocalName));
            }

            var productLanguage = contextValues["ProductLanguage"];

            foreach (var attrib in node.Attributes())
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
                        migrateFeatures = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                        break;
                    case "IgnoreLanguage":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            productLanguage = null;
                        }
                        break;
                    case "IgnoreRemoveFailure":
                        ignoreRemoveFailure = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
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
                var tuple = new UpgradeTuple(sourceLineNumbers)
                {
                    UpgradeCode = upgradeCode,
                    Remove = removeFeatures,
                    MigrateFeatures = migrateFeatures,
                    IgnoreRemoveFailures = ignoreRemoveFailure,
                    ActionProperty = Common.UpgradeDetectedProperty
                };

                if (allowDowngrades)
                {
                    tuple.VersionMin = "0";
                    tuple.Language = productLanguage;
                    tuple.VersionMinInclusive = true;
                }
                else
                {
                    tuple.VersionMax = productVersion;
                    tuple.Language = productLanguage;
                    tuple.VersionMaxInclusive = allowSameVersionUpgrades;
                }

                this.Core.AddTuple(tuple);

                // Ensure the action property is secure.
                this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Common.UpgradeDetectedProperty, AccessModifier.Public), false, true, false);

                // Add launch condition that blocks upgrades
                if (blockUpgrades)
                {
                    var conditionTuple = new LaunchConditionTuple(sourceLineNumbers)
                    {
                        Condition = Common.UpgradePreventedCondition,
                        Description = downgradeErrorMessage
                    };

                    this.Core.AddTuple(conditionTuple);
                }

                // now create the Upgrade row and launch conditions to prevent downgrades (unless explicitly permitted)
                if (!allowDowngrades)
                {
                    var upgradeTuple = new UpgradeTuple(sourceLineNumbers)
                    {
                        UpgradeCode = upgradeCode,
                        VersionMin = productVersion,
                        Language = productLanguage,
                        OnlyDetect = true,
                        MigrateFeatures = migrateFeatures,
                        IgnoreRemoveFailures = ignoreRemoveFailure,
                        ActionProperty = Common.DowngradeDetectedProperty
                    };

                    this.Core.AddTuple(upgradeTuple);

                    // Ensure the action property is secure.
                    this.AddWixPropertyRow(sourceLineNumbers, new Identifier(Common.DowngradeDetectedProperty, AccessModifier.Public), false, true, false);

                    var conditionTuple = new LaunchConditionTuple(sourceLineNumbers)
                    {
                        Condition = Common.DowngradePreventedCondition,
                        Description = downgradeErrorMessage
                    };

                    this.Core.AddTuple(conditionTuple);
                }

                // finally, schedule RemoveExistingProducts
                string after = null;
                switch (schedule)
                {
                    case null:
                    case "afterInstallValidate":
                        after = "InstallValidate";
                        break;
                    case "afterInstallInitialize":
                        after = "InstallInitialize";
                        break;
                    case "afterInstallExecute":
                        after = "InstallExecute";
                        break;
                    case "afterInstallExecuteAgain":
                        after = "InstallExecuteAgain";
                        break;
                    case "afterInstallFinalize":
                        after = "InstallFinalize";
                        break;
                }

                this.Core.ScheduleActionTuple(sourceLineNumbers, AccessModifier.Public, SequenceTable.InstallExecuteSequence, "RemoveExistingProducts", afterAction: after);
            }
        }

        /// <summary>
        /// Parses a media element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="patchId">Set to the PatchId if parsing Patch/Media element otherwise null.</param>
        private void ParseMediaElement(XElement node, string patchId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var id = CompilerConstants.IntegerNotSet;
            string cabinet = null;
            CompressionLevel? compressionLevel = null;
            string diskPrompt = null;
            string layout = null;
            var patch = null != patchId;
            string volumeLabel = null;
            string source = null;
            string symbols = null;

            var embedCab = patch ? YesNoType.Yes : YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
                        break;
                    case "Cabinet":
                        cabinet = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CompressionLevel":
                        compressionLevel = this.ParseCompressionLevel(sourceLineNumbers, node, attrib);
                        break;
                    case "DiskPrompt":
                        diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
                        break;
                    case "EmbedCab":
                        embedCab = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Layout":
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

            if (!compressionLevel.HasValue && null == cabinet)
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

            foreach (var child in node.Elements())
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var cabinetTemplate = "cab{0}.cab";
            string diskPrompt = null;
            var patch = null != patchId;
            string volumeLabel = null;
            var maximumUncompressedMediaSize = CompilerConstants.IntegerNotSet;
            var maximumCabinetSizeForLargeFileSplitting = CompilerConstants.IntegerNotSet;
            CompressionLevel? compressionLevel = null; // this defaults to mszip in Binder

            var embedCab = patch ? YesNoType.Yes : YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "CabinetTemplate":
                        var authoredCabinetTemplateValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                        if (!String.IsNullOrEmpty(authoredCabinetTemplateValue))
                        {
                            cabinetTemplate = authoredCabinetTemplateValue;
                        }

                        // Create an example cabinet name using the maximum number of cabinets supported, 999.
                        var exampleCabinetName = String.Format(cabinetTemplate, "###");
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
                        compressionLevel = this.ParseCompressionLevel(sourceLineNumbers, node, attrib);
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
                        maximumUncompressedMediaSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int32.MaxValue);
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

                if (compressionLevel.HasValue)
                {
                    mediaTemplateRow.CompressionLevel = compressionLevel.Value;
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var configData = String.Empty;
            var fileCompression = YesNoType.NotSet;
            string language = null;
            string sourceFile = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "DiskId":
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                        break;
                    case "FileCompression":
                        fileCompression = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Language":
                        language = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string value = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            var primary = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string classId = null;
            string contentType = null;
            var advertise = parentAdvertised;
            var returnContentType = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
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

                this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "Extension", String.Concat(".", extension), componentId);
                if (null != classId)
                {
                    this.Core.CreateRegistryRow(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "CLSID", classId, componentId);
                }
            }

            return YesNoType.Yes == returnContentType ? contentType : null;
        }

        /// <summary>
        /// Parses a patch creation element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchCreationElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var clean = true; // Default is to clean
            var codepage = 0;
            string outputPath = null;
            var productMismatches = false;
            var replaceGuids = String.Empty;
            string sourceList = null;
            string symbolFlags = null;
            var targetProducts = String.Empty;
            var versionMismatches = false;
            var wholeFiles = false;

            foreach (var attrib in node.Attributes())
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
                        symbolFlags = String.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, UInt32.MaxValue));
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

            foreach (var child in node.Elements())
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
                        var targetProduct = this.ParseTargetProductCodeElement(child);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var diskId = CompilerConstants.IntegerNotSet;
            string diskPrompt = null;
            string mediaSrcProp = null;
            string name = null;
            var sequenceStart = CompilerConstants.IntegerNotSet;
            string volumeLabel = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "DiskId":
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
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
                        sequenceStart = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int32.MaxValue);
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
                    foreach (var character in name)
                    {
                        if (!Char.IsLetterOrDigit(character) && '_' != character)
                        {
                            this.Core.Write(ErrorMessages.IllegalFamilyName(sourceLineNumbers, node.Name.LocalName, "Name", name));
                        }
                    }
                }
            }

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string sourceFile = null;
            string sourcePatch = null;
            var symbols = new List<string>();
            string upgrade = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var allowIgnoreOnError = false;
            string file = null;
            var ignore = false;
            var symbols = new List<string>();
            var wholeFile = false;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var ignore = false;
            var order = CompilerConstants.IntegerNotSet;
            string sourceFile = null;
            string symbols = null;
            string target = null;
            string validation = null;

            foreach (var attrib in node.Attributes())
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
                        order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int32.MinValue + 2, Int32.MaxValue);
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            string protectLengths = null;
            string protectOffsets = null;
            string symbols = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            var order = CompilerConstants.IntegerNotSet;
            string protectLengths = null;
            string protectOffsets = null;
            string source = null;
            string symbols = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "File":
                        file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Order":
                        order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int32.MinValue + 2, Int32.MaxValue);
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string protectLengths = null;
            string protectOffsets = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string length = null;
            string offset = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string company = null;
            string value = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string family = null;
            string target = null;
            string sequence = null;
            var attributes = 0;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var replace = false;
            var targetProductCodes = new List<string>();

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "TargetProductCode":
                        var id = this.ParseTargetProductCodeElement(child);
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

                foreach (var targetProductCode in targetProductCodes)
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string path = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string patchId = null;
            var codepage = 0;
            ////bool versionMismatches = false;
            ////bool productMismatches = false;
            var allowRemoval = false;
            string classification = null;
            string clientPatchId = null;
            string description = null;
            string displayName = null;
            string comments = null;
            string manufacturer = null;
            var minorUpdateTargetRTM = YesNoType.NotSet;
            string moreInfoUrl = null;
            var optimizeCA = CompilerConstants.IntegerNotSet;
            var optimizedInstallMode = YesNoType.NotSet;
            string targetProductName = null;
            // string replaceGuids = String.Empty;
            var apiPatchingSymbolFlags = 0;
            var optimizePatchSizeForLargeFiles = false;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string productCode = null;
            string version = null;
            var attributes = 0;

            foreach (var attrib in node.Attributes())
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
            foreach (var child in node.Elements())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // find unexpected attributes
            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var parsedValidate = false;
            var validationFlags = TransformFlags.PatchTransformDefault;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Validate":
                        if (parsedValidate)
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
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
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            foreach (var attrib in node.Attributes())
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
                        var check = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        validationFlags &= ~TransformFlags.ProductVersionMask;
                        switch (check)
                        {
                        case "Major":
                        case "major":
                            validationFlags |= TransformFlags.ValidateMajorVersion;
                            break;
                        case "Minor":
                        case "minor":
                            validationFlags |= TransformFlags.ValidateMinorVersion;
                            break;
                        case "Update":
                        case "update":
                            validationFlags |= TransformFlags.ValidateUpdateVersion;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Version", check, "Major", "Minor", "Update"));
                            break;
                        }
                        break;
                    case "ProductVersionOperator":
                        var op = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        validationFlags &= ~TransformFlags.ProductVersionOperatorMask;
                        switch (op)
                        {
                        case "Lesser":
                        case "lesser":
                            validationFlags |= TransformFlags.ValidateNewLessBaseVersion;
                            break;
                        case "LesserOrEqual":
                        case "lesserOrEqual":
                            validationFlags |= TransformFlags.ValidateNewLessEqualBaseVersion;
                            break;
                        case "Equal":
                        case "equal":
                            validationFlags |= TransformFlags.ValidateNewEqualBaseVersion;
                            break;
                        case "GreaterOrEqual":
                        case "greaterOrEqual":
                            validationFlags |= TransformFlags.ValidateNewGreaterEqualBaseVersion;
                            break;
                        case "Greater":
                        case "greater":
                            validationFlags |= TransformFlags.ValidateNewGreaterBaseVersion;
                            break;
                        case "":
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
    }
}
