// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        private const int MinValueOfMaxCabSizeForLargeFileSplitting = 20; // 20 MB
        private const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)

        private const char ComponentIdPlaceholderStart = (char)167;
        private const char ComponentIdPlaceholderEnd = (char)167;
        private Dictionary<string, string> componentIdPlaceholders;

        // If these are true you know you are building a module or product
        // but if they are false you cannot not be sure they will not end
        // up a product or module.  Use these flags carefully.
        private bool compilingModule;
        private bool compilingProduct;

        private string activeName;
        private string activeLanguage;

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
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

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
                var bundleValidator = this.Context.ServiceProvider.GetService<IBundleValidator>();
                var parseHelper = this.Context.ServiceProvider.GetService<IParseHelper>();

                this.Core = new CompilerCore(target, this.Messaging, bundleValidator, parseHelper, extensionsByNamespace);
                this.Core.ShowPedanticMessages = this.ShowPedanticMessages;
                this.componentIdPlaceholders = new Dictionary<string, string>();

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

            target.UpdateLevel(Data.IntermediateLevels.Compiled);

            return target;
        }

        /// <summary>
        /// Parses a Wix element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string requiredVersion = null;

            foreach (var attrib in node.Attributes())
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

            foreach (var child in node.Elements())
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
                    case "Package":
                        this.ParsePackageElement(child);
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

        private void ResolveComponentIdPlaceholders(Intermediate target)
        {
            if (0 < this.componentIdPlaceholders.Count)
            {
                foreach (var section in target.Sections)
                {
                    foreach (var symbol in section.Symbols)
                    {
                        foreach (var field in symbol.Fields)
                        {
                            if (field != null && field.Type == IntermediateFieldType.String)
                            {
                                var data = field.AsString();
                                if (!String.IsNullOrEmpty(data))
                                {
                                    var changed = false;
                                    var start = data.IndexOf(ComponentIdPlaceholderStart);
                                    while (start != -1)
                                    {
                                        var end = data.IndexOf(ComponentIdPlaceholderEnd, start + 1);
                                        if (end == -1)
                                        {
                                            break;
                                        }

                                        var placeholderId = data.Substring(start, end - start + 1);
                                        if (this.componentIdPlaceholders.TryGetValue(placeholderId, out var value))
                                        {
                                            var sb = new StringBuilder(data);
                                            sb.Remove(start, end - start + 1);
                                            sb.Insert(start, value);

                                            data = sb.ToString();
                                            changed = true;

                                            end = start + value.Length;
                                        }

                                        start = data.IndexOf(ComponentIdPlaceholderStart, end);
                                    }

                                    if (changed)
                                    {
                                        field.Overwrite(data);
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
            return s?.ToLowerInvariant();
        }

        /// <summary>
        /// Adds a search property to the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Current source/line number of processing.</param>
        /// <param name="propertyId">Property to add to search.</param>
        /// <param name="signature">Signature for search.</param>
        private void AddAppSearch(SourceLineNumber sourceLineNumbers, Identifier propertyId, string signature)
        {
            if (!this.Core.EncounteredError)
            {
                if (propertyId.Id != propertyId.Id.ToUpperInvariant())
                {
                    this.Core.Write(ErrorMessages.SearchPropertyNotUppercase(sourceLineNumbers, "Property", "Id", propertyId.Id));
                }

                this.Core.AddSymbol(new AppSearchSymbol(sourceLineNumbers, new Identifier(propertyId.Access, propertyId.Id, signature))
                {
                    PropertyRef = propertyId.Id,
                    SignatureRef = signature
                });
            }
        }

        /// <summary>
        /// Adds a property to the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Current source/line number of processing.</param>
        /// <param name="propertyId">Identifier of property to add.</param>
        /// <param name="value">Value of property.</param>
        /// <param name="admin">Flag if property is an admin property.</param>
        /// <param name="secure">Flag if property is a secure property.</param>
        /// <param name="hidden">Flag if property is to be hidden.</param>
        /// <param name="fragment">Adds the property to a new section.</param>
        private void AddProperty(SourceLineNumber sourceLineNumbers, Identifier propertyId, string value, bool admin, bool secure, bool hidden, bool fragment)
        {
            // properties without a valid identifier should not be processed any further
            if (null == propertyId || String.IsNullOrEmpty(propertyId.Id))
            {
                return;
            }

            if (!String.IsNullOrEmpty(value))
            {
                var start = value.IndexOf('[');
                while (start != -1 && start < value.Length)
                {
                    var end = value.IndexOf(']', start + 1);
                    if (end == -1)
                    {
                        break;
                    }

                    var id = value.Substring(start + 1, end - start - 1);
                    if (Common.IsIdentifier(id))
                    {
                        this.Core.Write(WarningMessages.PropertyValueContainsPropertyReference(sourceLineNumbers, propertyId.Id, id));
                    }

                    start = (end < value.Length) ? value.IndexOf('[', end + 1) : -1;
                }
            }

            if (!this.Core.EncounteredError)
            {
                var section = this.Core.ActiveSection;

                // Add the symbol to a separate section if requested.
                if (fragment)
                {
                    var id = String.Concat(this.Core.ActiveSection.Id, ".", propertyId.Id);

                    section = this.Core.CreateSection(id, SectionType.Fragment, this.Context.CompilationId);

                    // Reference the property in the active section.
                    this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Property, propertyId.Id);
                }

                // Allow symbol to exist with no value so that PropertyRefs can be made for *Search elements
                // the linker will remove these symbols before the final output is created.
                section.AddSymbol(new PropertySymbol(sourceLineNumbers, propertyId)
                {
                    Value = value,
                });

                if (admin || hidden || secure)
                {
                    this.AddWixPropertySymbol(sourceLineNumbers, propertyId, admin, secure, hidden, section);
                }
            }
        }

        private void AddWixPropertySymbol(SourceLineNumber sourceLineNumbers, Identifier property, bool admin, bool secure, bool hidden, IntermediateSection section = null)
        {
            if (secure && property.Id != property.Id.ToUpperInvariant())
            {
                this.Core.Write(ErrorMessages.SecurePropertyNotUppercase(sourceLineNumbers, "Property", "Id", property.Id));
            }

            if (null == section)
            {
                section = this.Core.ActiveSection;

                this.Core.EnsureTable(sourceLineNumbers, WindowsInstallerTableDefinitions.Property); // Property table is always required when using WixProperty table.
            }

            section.AddSymbol(new WixPropertySymbol(sourceLineNumbers)
            {
                PropertyRef = property.Id,
                Admin = admin,
                Hidden = hidden,
                Secure = secure
            });
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
            this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Implemented Categories\\", categoryId), "*", null, componentId);
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
            bool? activateAtStorage = null;
            var appIdAdvertise = YesNoType.NotSet;
            bool? runAsInteractiveUser = null;
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
                        activateAtStorage = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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
                        runAsInteractiveUser = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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
            else if (appIdAdvertise != YesNoType.NotSet)
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
                    this.Core.AddSymbol(new AppIdSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, appId))
                    {
                        AppId = appId,
                        RemoteServerName = remoteServerName,
                        LocalService = localService,
                        ServiceParameters = serviceParameters,
                        DllSurrogate = dllSurrogate,
                        ActivateAtStorage = activateAtStorage,
                        RunAsInteractiveUser = runAsInteractiveUser,
                    });
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (null != description)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), null, description, componentId);
                }
                else
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "+", null, componentId);
                }

                if (null != remoteServerName)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "RemoteServerName", remoteServerName, componentId);
                }

                if (null != localService)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "LocalService", localService, componentId);
                }

                if (null != serviceParameters)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "ServiceParameters", serviceParameters, componentId);
                }

                if (null != dllSurrogate)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "DllSurrogate", dllSurrogate, componentId);
                }

                if (true == activateAtStorage)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "ActivateAtStorage", "Y", componentId);
                }

                if (true == runAsInteractiveUser)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("AppID\\", appId), "RunAs", "Interactive User", componentId);
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
                this.Core.AddSymbol(new MsiAssemblyNameSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, componentId, id))
                {
                    ComponentRef = componentId,
                    Name = id,
                    Value = value,
                });
            }
        }

        /// <summary>
        /// Parses a binary element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Identifier for the new row.</returns>
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
                this.Core.AddSymbol(new BinarySymbol(sourceLineNumbers, id)
                {
                    Data = new IntermediateFieldPathValue { Path = sourceFile }
                });

                if (YesNoType.Yes == suppressModularization)
                {
                    this.Core.AddSymbol(new WixSuppressModularizationSymbol(sourceLineNumbers)
                    {
                        SuppressIdentifier = id.Id
                    });
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
                this.Core.AddSymbol(new IconSymbol(sourceLineNumbers, id)
                {
                    Data = new IntermediateFieldPathValue { Path = sourceFile },
                });
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Property, property);
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
        /// <param name="propertyId">Identifier of instance property.</param>
        private void ParseInstanceElement(XElement node, string propertyId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
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
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
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
                this.Core.AddSymbol(new WixInstanceTransformsSymbol(sourceLineNumbers, id)
                {
                    PropertyId = propertyId,
                    ProductCode = productCode,
                    ProductName = productName,
                    UpgradeCode = upgradeCode
                });
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Feature, feature);
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
                this.Core.AddSymbol(new PublishComponentSymbol(sourceLineNumbers)
                {
                    ComponentId = id,
                    Qualifier = qualifier,
                    ComponentRef = componentId,
                    AppData = appData,
                    FeatureRef = feature ?? Guid.Empty.ToString("B"),
                });
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
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, localFileServer);
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
                            this.Core.CreateRegistryStringSymbol(childSourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("FileType\\", classId, "\\", fileTypeMaskIndex.ToString()), String.Empty, this.ParseFileTypeMaskElement(child), componentId);
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
                        var symbol = this.Core.AddSymbol(new ClassSymbol(sourceLineNumbers)
                        {
                            CLSID = classId,
                            Context = context,
                            ComponentRef = componentId,
                            DefaultProgIdRef = defaultProgId,
                            Description = description,
                            FileTypeMask = fileTypeMask,
                            DefInprocHandler = defaultInprocHandler,
                            Argument = argument,
                            FeatureRef = Guid.Empty.ToString("B"),
                            RelativePath = YesNoType.Yes == relativePath,
                        });

                        if (null != appId)
                        {
                            symbol.AppIdRef = appId;
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.AppId, appId);
                        }

                        if (null != icon)
                        {
                            symbol.IconRef = icon;
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Icon, icon);
                        }

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            symbol.IconIndex = iconIndex;
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

                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", context), String.Empty, formattedContextString, componentId); // ClassId context

                    if (null != icon) // ClassId default icon
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, icon);

                        icon = String.Format(CultureInfo.InvariantCulture, "\"[#{0}]\"", icon);

                        if (CompilerConstants.IntegerNotSet != iconIndex)
                        {
                            icon = String.Concat(icon, ",", iconIndex);
                        }
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\DefaultIcon"), String.Empty, icon, componentId);
                    }
                }

                if (null != parentAppId) // ClassId AppId (must be specified via nesting, not with the AppId attribute)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId), "AppID", parentAppId, componentId);
                }

                if (null != description) // ClassId description
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId), String.Empty, description, componentId);
                }

                if (null != defaultInprocHandler)
                {
                    switch (defaultInprocHandler) // ClassId Default Inproc Handler
                    {
                    case "1":
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole2.dll", componentId);
                        break;
                    case "2":
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                        break;
                    case "3":
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole2.dll", componentId);
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
                        break;
                    default:
                        this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, defaultInprocHandler, componentId);
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
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", context), "ThreadingModel", threadingModel, componentId);
                }
            }

            if (null != typeLibId)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\TypeLib"), null, typeLibId, componentId);
            }

            if (null != version)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Version"), null, version, componentId);
            }

            if (null != insertable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\", insertable), "*", null, componentId);
            }

            if (control)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Control"), "*", null, componentId);
            }

            if (programmable)
            {
                // Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("CLSID\\", classId, "\\Programmable"), "*", null, componentId);
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

            this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId), null, name, componentId);
            if (null != typeLibId)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), null, typeLibId, componentId);
                if (versioned)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), "Version", typelibVersion, componentId);
                }
            }

            if (null != baseInterface)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\BaseInterface"), null, baseInterface, componentId);
            }

            if (CompilerConstants.IntegerNotSet != numMethods)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\NumMethods"), null, numMethods.ToString(), componentId);
            }

            if (null != proxyId)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid"), null, proxyId, componentId);
            }

            if (null != proxyId32)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid32"), null, proxyId32, componentId);
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
        /// <param name="propertyId"></param>
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
                this.Core.AddSymbol(new UpgradeSymbol(sourceLineNumbers)
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
                });
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
            Identifier id = null;
            string key = null;
            string name = null;
            RegistryRootType? root = null;
            RegLocatorType? type = null;
            var search64bit = this.Context.IsCurrentPlatform64Bit;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Bitness":
                        var bitnessValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (bitnessValue)
                        {
                        case "always32":
                            search64bit = false;
                            break;
                        case "always64":
                            search64bit = true;
                            break;
                        case "default":
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                            break;
                        }
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
                            type = RegLocatorType.Directory;
                            break;
                        case "file":
                            type = RegLocatorType.FileName;
                            break;
                        case "raw":
                            type = RegLocatorType.Raw;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "directory", "file", "raw"));
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

            if (!type.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
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
                        id = new Identifier(AccessModifier.Section, signature); // FileSearch signatures override parent signatures
                        break;
                    case "FileSearchRef":
                        if (oneChild)
                        {
                            this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                        }
                        oneChild = true;
                        var newId = this.ParseSimpleRefElement(child, SymbolDefinitions.Signature); // FileSearch signatures override parent signatures
                        id = new Identifier(AccessModifier.Section, newId);
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
                this.Core.AddSymbol(new RegLocatorSymbol(sourceLineNumbers, id)
                {
                    Root = root.Value,
                    Key = key,
                    Name = name,
                    Type = type.Value,
                    Win64 = search64bit,
                });
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.RegLocator, id);
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
                this.Core.AddSymbol(new CCPSearchSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, signature)));
            }
        }

        /// <summary>
        /// Parses a component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Type of component's complex reference parent.  Will be Unknown if there is no parent.</param>
        /// <param name="parentId">Optional identifier for component's primary parent.</param>
        /// <param name="parentLanguage">Optional string for component's parent's language.</param>
        /// <param name="diskId">Optional disk id inherited from parent directory.</param>
        /// <param name="directoryId">Optional identifier for component's directory.</param>
        /// <param name="srcPath">Optional source path for files up to this point.</param>
        private void ParseComponentElement(XElement node, ComplexReferenceParentType parentType, string parentId, string parentLanguage, int diskId, string directoryId, string srcPath)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            var comPlusBits = CompilerConstants.IntegerNotSet;
            string condition = null;
            string subdirectory = null;
            var encounteredODBCDataSource = false;
            var files = 0;
            var guid = "*";
            Identifier id = null;
            string componentIdPlaceholder = null;
            var keyFound = false;
            string keyPath = null;

            var keyPathType = ComponentKeyPathType.Directory;
            var location = ComponentLocation.LocalOnly;
            var disableRegistryReflection = false;

            var neverOverwrite = false;
            var permanent = false;
            var shared = false;
            var sharedDllRefCount = false;
            var transitive = false;
            var uninstallWhenSuperseded = false;
            var win64 = this.Context.IsCurrentPlatform64Bit;

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
                    case "Bitness":
                        var bitnessValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (bitnessValue)
                        {
                        case "always32":
                            win64 = false;
                            break;
                        case "always64":
                            win64 = true;
                            break;
                        case "default":
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                            break;
                        }
                        break;
                    case "ComPlusFlags":
                        comPlusBits = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "DisableRegistryReflection":
                        disableRegistryReflection = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Condition":
                        condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Directory":
                        directoryId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
                        break;
                    case "Subdirectory":
                        subdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
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
                        }
                        break;
                    case "Location":
                        var locationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (locationValue)
                        {
                        case "either":
                            location = ComponentLocation.Either;
                            break;
                        case "local": // this is the default
                            location = ComponentLocation.LocalOnly;
                            break;
                        case "source":
                            location = ComponentLocation.SourceOnly;
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
                        break;
                    case "Permanent":
                        permanent = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Shared":
                        shared = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "SharedDllRefCount":
                        sharedDllRefCount = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Transitive":
                        transitive = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "UninstallWhenSuperseded":
                        uninstallWhenSuperseded = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (id == null)
            {
                // Placeholder id for defaulting Component/@Id to keypath id.
                componentIdPlaceholder = String.Concat(Compiler.ComponentIdPlaceholderStart, this.componentIdPlaceholders.Count, Compiler.ComponentIdPlaceholderEnd);
                id = new Identifier(AccessModifier.Section, componentIdPlaceholder);
            }

            if (String.IsNullOrEmpty(directoryId))
            {
                directoryId = "INSTALLFOLDER";
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
            }

            if (!String.IsNullOrEmpty(subdirectory))
            {
                directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, directoryId, subdirectory);
            }

            if (String.IsNullOrEmpty(guid) && shared)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Shared", "yes", "Guid", ""));
            }

            if (String.IsNullOrEmpty(guid) && permanent)
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
                    case "CopyFile":
                        this.ParseCopyFileElement(child, id.Id, null);
                        break;
                    case "CreateFolder":
                        var createdFolder = this.ParseCreateFolderElement(child, id.Id, directoryId, win64);
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
                        this.ParseODBCDriverOrTranslator(child, id.Id, null, SymbolDefinitionType.ODBCDriver);
                        break;
                    case "ODBCTranslator":
                        this.ParseODBCDriverOrTranslator(child, id.Id, null, SymbolDefinitionType.ODBCTranslator);
                        break;
                    case "ProgId":
                        var foundExtension = false;
                        this.ParseProgIdElement(child, id.Id, YesNoType.NotSet, null, null, null, ref foundExtension, YesNoType.NotSet);
                        break;
                    case "Provides":
                        if (win64)
                        {
                            this.Messaging.Write(CompilerWarnings.Win64Component(sourceLineNumbers, id.Id));
                        }

                        keyPathSet = this.ParseProvidesElement(child, null, id.Id, out keyPossible);
                        keyBit = ComponentKeyPathType.Registry;
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
                    var context = new Dictionary<string, string>() { { "ComponentId", id?.Id }, { "DirectoryId", directoryId }, { "Win64", win64.ToString() }, };
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

                            switch (possibleKeyPath.Type)
                            {
                                case PossibleKeyPathType.File:
                                    keyBit = ComponentKeyPathType.File;
                                    keyPossible = possibleKeyPath.Id;
                                    break;

                                case PossibleKeyPathType.Directory:
                                    keyBit = ComponentKeyPathType.Directory;
                                    keyPossible = String.Empty;
                                    break;

                                case PossibleKeyPathType.OdbcDataSource:
                                    keyBit = ComponentKeyPathType.OdbcDataSource;
                                    keyPossible = possibleKeyPath.Id;
                                    break;

                                case PossibleKeyPathType.Registry:
                                case PossibleKeyPathType.RegistryFormatted:
                                    keyBit = ComponentKeyPathType.Registry;
                                    keyPossible = possibleKeyPath.Id;
                                    break;

                                case PossibleKeyPathType.None:
                                default:
                                    keyBit = null;
                                    keyPossible = null;
                                    break;
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

            // Check for conditions that exclude this component from using implicit ids and/or generated guids.
            var allowImplicitIds = true;
            if (encounteredODBCDataSource || ComponentKeyPathType.Directory == keyPathType)
            {
                allowImplicitIds = false;
                if (guid == "*")
                {
                    this.Core.Write(ErrorMessages.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers));
                }
            }
            else if (0 < files && ComponentKeyPathType.Registry == keyPathType)
            {
                allowImplicitIds = false;
                if (guid == "*")
                {
                    this.Core.Write(ErrorMessages.IllegalComponentWithAutoGeneratedGuid(sourceLineNumbers, true));
                }
            }

            // Check for implicit KeyPath which can easily be accidentally changed
            if (this.ShowPedanticMessages && !keyFound && !allowImplicitIds)
            {
                this.Core.Write(ErrorMessages.ImplicitComponentKeyPath(sourceLineNumbers, id.Id));
            }

            // If there isn't an @Id attribute value, replace the placeholder with the id of the keypath.
            // either an explicit KeyPath="yes" attribute must be specified or requirements for
            // generatable guid must be met.
            if (componentIdPlaceholder == id.Id)
            {
                if (allowImplicitIds || keyFound && !String.IsNullOrEmpty(keyPath))
                {
                    this.componentIdPlaceholders.Add(componentIdPlaceholder, keyPath);

                    id = new Identifier(AccessModifier.Section, keyPath);
                }
                else
                {
                    this.Core.Write(ErrorMessages.CannotDefaultComponentId(sourceLineNumbers));
                }
            }

            // finally add the Component table row
            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new ComponentSymbol(sourceLineNumbers, id)
                {
                    ComponentId = guid,
                    DirectoryRef = directoryId,
                    Location = location,
                    Condition = condition,
                    KeyPath = keyPath,
                    KeyPathType = keyPathType,
                    DisableRegistryReflection = disableRegistryReflection,
                    NeverOverwrite = neverOverwrite,
                    Permanent = permanent,
                    SharedDllRefCount = sharedDllRefCount,
                    Shared = shared,
                    Transitive = transitive,
                    UninstallWhenSuperseded = uninstallWhenSuperseded,
                    Win64 = win64,
                });

                if (multiInstance)
                {
                    this.Core.AddSymbol(new WixInstanceComponentSymbol(sourceLineNumbers, id)
                    {
                        ComponentRef = id.Id,
                    });
                }

                if (0 < symbols.Count)
                {
                    this.Core.AddSymbol(new WixDeltaPatchSymbolPathsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, SymbolPathType.Component, id.Id))
                    {
                        SymbolType = SymbolPathType.Component,
                        SymbolId = id.Id,
                        SymbolPaths = String.Join(";", symbols),
                    });
                }

                // Complus
                if (CompilerConstants.IntegerNotSet != comPlusBits)
                {
                    this.Core.AddSymbol(new ComplusSymbol(sourceLineNumbers)
                    {
                        ComponentRef = id.Id,
                        ExpType = comPlusBits,
                    });
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
        /// <param name="parentType">Type of complex reference parent. Will be Unknown if there is no parent.</param>
        /// <param name="parentId">Optional identifier for primary parent.</param>
        private void ParseComponentGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string directoryId = null;
            string subdirectory = null;
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
                        directoryId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
                        break;
                    case "Subdirectory":
                        subdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
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

            directoryId = this.HandleSubdirectory(sourceLineNumbers, node, directoryId, subdirectory, "Directory", "Subdirectory");

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
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.ComponentGroup, id.Id, directoryId, source);
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
                this.Core.AddSymbol(new WixComponentGroupSymbol(sourceLineNumbers, id)
                {
                    DirectoryRef = directoryId,
                    Source = source
                });

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
            Debug.Assert(ComplexReferenceParentType.ComponentGroup == parentType || ComplexReferenceParentType.FeatureGroup == parentType || ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.Module == parentType || ComplexReferenceParentType.Product == parentType);

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixComponentGroup, id);
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
            Debug.Assert(ComplexReferenceParentType.FeatureGroup == parentType || ComplexReferenceParentType.ComponentGroup == parentType || ComplexReferenceParentType.Feature == parentType || ComplexReferenceParentType.Module == parentType || ComplexReferenceParentType.Product == parentType);

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Component, id);
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
                        id = new Identifier(AccessModifier.Section, signature); // FileSearch signatures override parent signatures
                        break;
                    case "FileSearchRef":
                        if (oneChild)
                        {
                            this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                        }
                        oneChild = true;
                        var newId = this.ParseSimpleRefElement(child, SymbolDefinitions.Signature); // FileSearch signatures override parent signatures
                        id = new Identifier(AccessModifier.Section, newId);
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
                this.Core.AddSymbol(new CompLocatorSymbol(sourceLineNumbers, id)
                {
                    SignatureRef = id.Id,
                    ComponentId = componentId,
                    Type = type,
                });
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
            string subdirectory = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Directory":
                            directoryId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
                            break;
                        case "Subdirectory":
                            subdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
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

            directoryId = this.HandleSubdirectory(sourceLineNumbers, node, directoryId, subdirectory, "Directory", "Subdirectory");

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
                this.Core.AddSymbol(new CreateFolderSymbol(sourceLineNumbers)
                {
                    DirectoryRef = directoryId,
                    ComponentRef = componentId,
                });
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
            string destinationSubdirectory = null;
            string destinationName = null;
            string destinationShortName = null;
            string destinationProperty = null;
            string sourceDirectory = null;
            string sourceSubdirectory = null;
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
                        destinationDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, destinationDirectory);
                        break;
                    case "DestinationSubdirectory":
                        destinationSubdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
                        break;
                    case "DestinationName":
                        destinationName = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib);
                        break;
                    case "DestinationProperty":
                        destinationProperty = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "DestinationShortName":
                        destinationShortName = this.Core.GetAttributeShortFilename(sourceLineNumbers, attrib);
                        break;
                    case "FileId":
                        if (null != fileId)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                        }
                        fileId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, fileId);
                        break;
                    case "SourceDirectory":
                        sourceDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, sourceDirectory);
                        break;
                    case "SourceSubdirectory":
                        sourceSubdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
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

            sourceDirectory = this.HandleSubdirectory(sourceLineNumbers, node, sourceDirectory, sourceSubdirectory, "SourceDirectory", "SourceSubdirectory");

            if (null != destinationDirectory && null != destinationProperty) // DestinationDirectory and DestinationProperty cannot coexist
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DestinationProperty", "DestinationDirectory"));
            }

            destinationDirectory = this.HandleSubdirectory(sourceLineNumbers, node, destinationDirectory, destinationSubdirectory, "DestinationDirectory", "DestinationSubdirectory");

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
                    this.Core.AddSymbol(new MoveFileSymbol(sourceLineNumbers, id)
                    {
                        ComponentRef = componentId,
                        SourceName  = sourceName,
                        DestinationName = destinationName,
                        DestinationShortName = destinationShortName,
                        SourceFolder = sourceDirectory ?? sourceProperty,
                        DestFolder = destinationDirectory ?? destinationProperty,
                        Delete = delete,
                    });
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
                    this.Core.AddSymbol(new DuplicateFileSymbol(sourceLineNumbers, id)
                    {
                        ComponentRef = componentId,
                        FileRef = fileId,
                        DestinationName = destinationName,
                        DestinationShortName = destinationShortName,
                        DestinationFolder = destinationDirectory ?? destinationProperty,
                    });
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

            string scriptFile = null;
            string subdirectory = null;

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
                    case "BinaryRef":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryRef", "Directory", "FileRef", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        sourceType = CustomActionSourceType.Binary;
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Binary, source); // add a reference to the appropriate Binary
                        break;
                    case "Bitness":
                        var bitnessValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (bitnessValue)
                        {
                        case "always32":
                            explicitWin64 = true;
                            win64 = false;
                            break;
                        case "always64":
                            explicitWin64 = true;
                            win64 = true;
                            break;
                        case "default":
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                            break;
                        }
                        break;
                    case "Directory":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryKey", "Directory", "FileRef", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        sourceType = CustomActionSourceType.Directory;
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, source);
                        break;
                    case "DllEntry":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        targetType = CustomActionTargetType.Dll;
                        break;
                    case "Error":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        sourceType = CustomActionSourceType.File;
                        targetType = CustomActionTargetType.TextData;

                        // The target can be either a formatted error string or a literal
                        // error number. Try to convert to error number to determine whether
                        // to add a reference. No need to look at the value.
                        if (Int32.TryParse(target, out var ignored))
                        {
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Error, target);
                        }
                        break;
                    case "ExeCommand":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        targetType = CustomActionTargetType.Exe;
                        break;
                    case "Execute":
                        var execute = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (execute)
                        {
                        case "commit":
                            executionType = CustomActionExecutionType.Commit;
                            break;
                        case "deferred":
                            executionType = CustomActionExecutionType.Deferred;
                            break;
                        case "firstSequence":
                            executionType = CustomActionExecutionType.FirstSequence;
                            break;
                        case "immediate":
                            executionType = CustomActionExecutionType.Immediate;
                            break;
                        case "oncePerProcess":
                            executionType = CustomActionExecutionType.OncePerProcess;
                            break;
                        case "rollback":
                            executionType = CustomActionExecutionType.Rollback;
                            break;
                        case "secondSequence":
                            executionType = CustomActionExecutionType.ClientRepeat;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, execute, "commit", "deferred", "firstSequence", "immediate", "oncePerProcess", "rollback", "secondSequence"));
                            break;
                        }
                        break;
                    case "FileRef":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryRef", "Directory", "FileRef", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        sourceType = CustomActionSourceType.File;
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, source); // add a reference to the appropriate File
                        break;
                    case "HideTarget":
                        hidden = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Impersonate":
                        impersonate = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "JScriptCall":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        targetType = CustomActionTargetType.JScript;
                        break;
                    case "PatchUninstall":
                        patchUninstall = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Property":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryRef", "Directory", "FileRef", "Property", "Script"));
                        }
                        source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        sourceType = CustomActionSourceType.Property;
                        break;
                    case "Return":
                        var returnValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (returnValue)
                        {
                        case "asyncNoWait":
                            async = true;
                            ignoreResult = true;
                            break;
                        case "asyncWait":
                            async = true;
                            break;
                        case "check":
                            break;
                        case "ignore":
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
                            this.Core.Write(ErrorMessages.CustomActionMultipleSources(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinaryRef", "Directory", "FileRef", "Property", "Script"));
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
                            sourceType = CustomActionSourceType.Directory;
                            targetType = CustomActionTargetType.JScript;
                            break;
                        case "vbscript":
                            sourceType = CustomActionSourceType.Directory;
                            targetType = CustomActionTargetType.VBScript;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, script, "jscript", "vbscript"));
                            break;
                        }
                        break;
                    case "ScriptSourceFile":
                        scriptFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Subdirectory":
                        subdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
                        break;
                    case "SuppressModularization":
                        suppressModularization = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "TerminalServerAware":
                        tsAware = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        targetType = CustomActionTargetType.TextData;
                        break;
                    case "VBScriptCall":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.CustomActionMultipleTargets(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty); // one of the few cases where an empty string value is valid
                        targetType = CustomActionTargetType.VBScript;
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

            if (!explicitWin64 && this.Context.IsCurrentPlatform64Bit && (CustomActionTargetType.VBScript == targetType || CustomActionTargetType.JScript == targetType))
            {
                win64 = true;
            }

            if (!String.IsNullOrEmpty(subdirectory))
            {
                if (sourceType == CustomActionSourceType.Directory)
                {
                    source = this.HandleSubdirectory(sourceLineNumbers, node, source, subdirectory, "Directory", "Subdirectory");
                }
                else
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Subdirectory", "Directory"));
                }
            }

            if (targetType == CustomActionTargetType.VBScript)
            {
                this.Core.Write(WarningMessages.VBScriptIsDeprecated(sourceLineNumbers));
            }

            // if we have an in-lined Script CustomAction ensure no source or target attributes were provided
            if (inlineScript)
            {
                if (String.IsNullOrEmpty(scriptFile))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ScriptSourceFile", "Script"));
                }
            }
            else if (CustomActionTargetType.VBScript == targetType) // non-inline vbscript
            {
                if (null == source)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "VBScriptCall", "BinaryRef", "FileRef", "Property"));
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
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "JScriptCall", "BinaryRef", "FileRef", "Property"));
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
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ExeCommand", "BinaryRef", "Directory", "FileRef", "Property"));
                }
            }
            else if (CustomActionTargetType.TextData == targetType && CustomActionSourceType.Directory != sourceType && CustomActionSourceType.Property != sourceType && CustomActionSourceType.File != sourceType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Value", "Directory", "Property", "Error"));
            }

            if (!inlineScript && !String.IsNullOrEmpty(scriptFile))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ScriptSourceFile", "Script"));
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

            if (!targetType.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
            }

            if (!sourceType.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "BinaryRef", "Directory", "Error", "FileRef", "Property", "Script"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new CustomActionSymbol(sourceLineNumbers, id)
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
                    Hidden = hidden,
                    ScriptFile = new IntermediateFieldPathValue { Path = scriptFile }
                });

                if (YesNoType.Yes == suppressModularization)
                {
                    this.Core.AddSymbol(new WixSuppressModularizationSymbol(sourceLineNumbers)
                    {
                        SuppressIdentifier = id.Id
                    });
                }
            }
        }

        /// <summary>
        /// Parses a simple reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="symbolDefinition">Symbol which contains the target of the simple reference.</param>
        /// <returns>Id of the referenced element.</returns>
        private string ParseSimpleRefElement(XElement node, IntermediateSymbolDefinition symbolDefinition)
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, symbolDefinition.Name, id);
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

            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.MsiPatchFamily, primaryKeys);

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, primaryKeys[0], true);
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
        /// Parses a directory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <param name="diskId">Disk id inherited from parent directory.</param>
        /// <param name="fileSource">Path to source file as of yet.</param>
        private void ParseDirectoryElement(XElement node, string parentId, int diskId, string fileSource)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string componentGuidGenerationSeed = null;
            var fileSourceAttribSet = false;
            XAttribute nameAttribute = null;
            var name = "."; // default to parent directory.
            string shortName = null;
            string sourceName = null;
            string shortSourceName = null;
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
                        if ("." == attrib.Value)
                        {
                            name = attrib.Value;
                        }
                        else
                        {
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
                        }
                        nameAttribute = attrib;
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

            if (nameAttribute == null)
            {
                if (!String.IsNullOrEmpty(shortName))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name"));
                }
            }
            else if (!String.IsNullOrEmpty(name))
            {
                if (String.IsNullOrEmpty(shortName))
                {
                }
                else if (name == ".")
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortName", "Name", name));
                }
                else if (name.Equals(shortName, StringComparison.OrdinalIgnoreCase))
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
                }
                else if (sourceName == ".")
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ShortSourceName", "SourceName", sourceName));
                }
                else if (sourceName.Equals(shortSourceName, StringComparison.OrdinalIgnoreCase))
                {
                    this.Core.Write(WarningMessages.DirectoryRedundantNames(sourceLineNumbers, node.Name.LocalName, "SourceName", "ShortSourceName", sourceName));
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("d", parentId, name, shortName, sourceName, shortSourceName);
            }
            else if (WindowsInstallerStandard.IsStandardDirectory(id.Id))
            {
                if (String.IsNullOrEmpty(sourceName))
                {
                    this.Core.Write(CompilerWarnings.DefiningStandardDirectoryDeprecated(sourceLineNumbers, id.Id));
                }

                if (id.Id == "TARGETDIR" && name != "SourceDir" && shortName == null && shortSourceName == null && sourceName == null)
                {
                    this.Core.Write(ErrorMessages.IllegalTargetDirDefaultDir(sourceLineNumbers, name));
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
                string append = String.IsNullOrEmpty(sourceName) ? name : sourceName;

                if (!String.IsNullOrEmpty(append))
                {
                    fileSource = String.Concat(fileSource, append, Path.DirectorySeparatorChar);
                }
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
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.Unknown, null, id.Id, fileSource);
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
                this.Core.AddSymbol(new DirectorySymbol(sourceLineNumbers, id)
                {
                    ParentDirectoryRef = parentId,
                    Name = name,
                    ShortName = shortName,
                    SourceName = sourceName,
                    SourceShortName = shortSourceName,
                    ComponentGuidGenerationSeed = componentGuidGenerationSeed
                });

                if (null != symbols)
                {
                    this.Core.AddSymbol(new WixDeltaPatchSymbolPathsSymbol(sourceLineNumbers, id)
                    {
                        SymbolType = SymbolPathType.Directory,
                        SymbolId = id.Id,
                        SymbolPaths = symbols,
                    });
                }
            }
        }

        /// <summary>
        /// Parses a directory reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, id);
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
            else if (WindowsInstallerStandard.IsStandardDirectory(id))
            {
                this.Core.Write(CompilerWarnings.DirectoryRefStandardDirectoryDeprecated(sourceLineNumbers, id));
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
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.Unknown, null, id, fileSource);
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

            var signature = id.Id;

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
                        signature = this.ParseSimpleRefElement(child, SymbolDefinitions.Signature);
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
                    access = AccessModifier.Section;
                    rowId = signature;

                    // The property should be set to the directory search Id.
                    signature = id.Id;
                }

                var symbol = this.Core.AddSymbol(new DrLocatorSymbol(sourceLineNumbers, new Identifier(access, rowId, parentSignature, path))
                {
                    SignatureRef = rowId,
                    Parent = parentSignature,
                    Path = path,
                });

                if (CompilerConstants.IntegerNotSet != depth)
                {
                    symbol.Depth = depth;
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

            var signature = id.Id;

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
                        signature = this.ParseSimpleRefElement(child, SymbolDefinitions.Signature);
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


            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.DrLocator, id.Id, parentSignature, path);

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

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "AllowAbsent":
                        disallowAbsent = (this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.No);
                        break;
                    case "AllowAdvertise":
                        disallowAdvertise = (this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.No);
                        break;
                    case "ConfigurableDirectory":
                        configurableDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, configurableDirectory);
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
                    case "Feature":
                        this.ParseFeatureElement(child, ComplexReferenceParentType.Feature, id.Id, ref childDisplay);
                        break;
                    case "FeatureGroupRef":
                        this.ParseFeatureGroupRefElement(child, ComplexReferenceParentType.Feature, id.Id);
                        break;
                    case "FeatureRef":
                        this.ParseFeatureRefElement(child, ComplexReferenceParentType.Feature, id.Id);
                        break;
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.Feature, id.Id, null, null);
                        break;
                    case "Level":
                        this.ParseLevelElement(child, id.Id);
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

            int display;
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
                this.Core.AddSymbol(new FeatureSymbol(sourceLineNumbers, id)
                {
                    ParentFeatureRef = null, // this field is set in the linker
                    Title = title,
                    Description = description,
                    Display = display,
                    Level = level,
                    DirectoryRef = configurableDirectory,
                    DisallowAbsent = disallowAbsent,
                    DisallowAdvertise = disallowAdvertise,
                    InstallDefault = installDefault,
                    TypicalDefault = typicalDefault,
                });

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Feature, id);
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
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.Feature, id, null, null);
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
        /// <param name="parentType"></param>
        /// <param name="parentId"></param>
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
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.FeatureGroup, id.Id, null, null);
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
                this.Core.AddSymbol(new WixFeatureGroupSymbol(sourceLineNumbers, id));

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixFeatureGroup, id);
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

            if (part.HasValue && action == EnvironmentActionType.Create)
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
                this.Core.AddSymbol(new EnvironmentSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    Value = value,
                    Separator = separator,
                    Action = action,
                    Part = part,
                    Permanent = permanent,
                    System = system,
                    ComponentRef = componentId
                });
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
            string message = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Message":
                        message = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
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
                this.Core.AddSymbol(new ErrorSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, id))
                {
                    Message = message
                });
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
                    this.Core.AddSymbol(new ExtensionSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, extension, componentId))
                    {
                        Extension = extension,
                        ComponentRef = componentId,
                        ProgIdRef = progId,
                        MimeRef = mime,
                        FeatureRef = Guid.Empty.ToString("B"),
                    });

                    this.Core.EnsureTable(sourceLineNumbers, WindowsInstallerTableDefinitions.Verb);
                }
            }
            else if (YesNoType.No == advertise)
            {
                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat(".", extension), String.Empty, progId, componentId); // Extension
                if (null != mime)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat(".", extension), "Content Type", mime, componentId); // Extension's MIME ContentType
                }
            }
        }

        /// <summary>
        /// Parses a File element's attributes.
        /// </summary>
        /// <param name="node">File element to parse.</param>
        /// <param name="componentId">Parent's component id.</param>
        /// <param name="directoryId">Ancestor's directory id.</param>
        /// <param name="diskId">Disk id inherited from parent component.</param>
        /// <param name="sourcePath">Default source path of parent directory.</param>
        /// <param name="possibleKeyPath">This will be set with the possible keyPath for the parent component.</param>
        /// <param name="componentGuid">Component GUID (including `*`).</param>
        /// <param name="isNakedFile">Whether the File element being parsed is outside a Component element.</param>
        /// <param name="fileSymbol">Outgoing file symbol containing parsed attributes.</param>
        /// <param name="assemblySymbol">Outgoing assembly symbol containing parsed attributes.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        private YesNoType ParseFileElementAttributes(XElement node, string componentId, string directoryId, int diskId, string sourcePath, out string possibleKeyPath, string componentGuid, bool isNakedFile, out FileSymbol fileSymbol, out AssemblySymbol assemblySymbol)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var assemblyType = AssemblyType.NotAnAssembly;
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
            int? selfRegCost = null;
            string shortName = null;
            var source = sourcePath;   // assume we'll use the parents as the source for this file
            var sourceSet = false;

            fileSymbol = null;
            assemblySymbol = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Bitness":
                    case "Condition":
                    case "Directory":
                    case "Subdirectory":
                        // Naked files handle their attributes in ParseNakedFileElement.
                        if (!isNakedFile)
                        {
                            this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, attrib.Name.LocalName));
                        }
                        break;
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Assembly":
                        var assemblyValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (assemblyValue)
                        {
                        case ".net":
                            assemblyType = AssemblyType.DotNetAssembly;
                            break;
                        case "no":
                            assemblyType = AssemblyType.NotAnAssembly;
                            break;
                        case "win32":
                            assemblyType = AssemblyType.Win32Assembly;
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "Assembly", assemblyValue, "no", "win32", ".net"));
                            break;
                        }
                        break;
                    case "AssemblyApplication":
                        assemblyApplication = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, assemblyApplication);
                        break;
                    case "AssemblyManifest":
                        assemblyManifest = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, assemblyManifest);
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.File, companionFile);
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
                        case "arm64":
                            procArch = "arm64";
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "File", "ProcessorArchitecture", procArchValue, "msil", "x86", "x64"));
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

            if (name == null)
            {
                if (shortName == null)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
                else
                {
                    name = shortName;
                    shortName = null;
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("fil", directoryId, name);
            }

            if (null != defaultVersion && null != companionFile)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DefaultVersion", "CompanionFile", companionFile));
            }

            if (AssemblyType.NotAnAssembly == assemblyType)
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
                if (AssemblyType.Win32Assembly == assemblyType && null == assemblyManifest)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AssemblyManifest", "Assembly", "win32"));
                }

                // allow "*" guid components to omit explicit KeyPath as they can have only one file and therefore this file is the keypath
                if (YesNoType.Yes != keyPath && "*" != componentGuid)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Assembly", (AssemblyType.DotNetAssembly == assemblyType ? ".net" : "win32"), "KeyPath", "yes"));
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
                    source = name;
                }
                else if (source.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) // if source relies on parent directories, append the file name
                {
                    source = Path.Combine(source, name);
                }

                var attributes = FileSymbolAttributes.None;
                attributes |= readOnly ? FileSymbolAttributes.ReadOnly : 0;
                attributes |= hidden ? FileSymbolAttributes.Hidden : 0;
                attributes |= system ? FileSymbolAttributes.System : 0;
                attributes |= vital ? FileSymbolAttributes.Vital : 0;
                attributes |= checksum ? FileSymbolAttributes.Checksum : 0;
                attributes |= compressed.HasValue && compressed == true ? FileSymbolAttributes.Compressed : 0;
                attributes |= compressed.HasValue && compressed == false ? FileSymbolAttributes.Uncompressed : 0;

                fileSymbol = new FileSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    ShortName = shortName,
                    FileSize = defaultSize,
                    Version = companionFile ?? defaultVersion,
                    Language = defaultLanguage,
                    Attributes = attributes,

                    DirectoryRef = directoryId,
                    DiskId = (CompilerConstants.IntegerNotSet == diskId) ? null : (int?)diskId,
                    Source = new IntermediateFieldPathValue { Path = source },

                    FontTitle = fontTitle,
                    SelfRegCost = selfRegCost,
                    BindPath = bindPath,

                    PatchGroup = (CompilerConstants.IntegerNotSet == patchGroup) ? null : (int?)patchGroup,
                    PatchAttributes = patchAttributes,

                    // Delta patching information
                    RetainLengths = protectLengths,
                    IgnoreOffsets = ignoreOffsets,
                    IgnoreLengths = ignoreLengths,
                    RetainOffsets = protectOffsets,
                    SymbolPaths = symbols,
                };

                if (AssemblyType.NotAnAssembly != assemblyType)
                {
                    assemblySymbol = new AssemblySymbol(sourceLineNumbers, id)
                    {
                        ComponentRef = componentId,
                        FeatureRef = Guid.Empty.ToString("B"),
                        ManifestFileRef = assemblyManifest,
                        ApplicationFileRef = assemblyApplication,
                        Type = assemblyType,
                        ProcessorArchitecture = procArch,
                    };
                }
            }

            if (CompilerConstants.IntegerNotSet != diskId)
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Media, diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            // If this component does not have a companion file this file is a possible keypath.
            possibleKeyPath = null;
            if (null == companionFile)
            {
                possibleKeyPath = id.Id;
            }

            return keyPath;
        }

        /// <param name="node">File element to parse.</param>
        /// <param name="fileSymbol">The partially-parsed file symbol.</param>
        /// <param name="keyPath">Whether the file is the keypath of its component.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        private void ParseFileElementChildren(XElement node, FileSymbol fileSymbol, YesNoType keyPath, bool win64Component)
        {
            var directoryId = fileSymbol.DirectoryRef;
            var componentId = fileSymbol.ComponentRef;
            var id = fileSymbol.Id;
            var ignoreOffsets = fileSymbol.IgnoreOffsets;
            var ignoreLengths = fileSymbol.IgnoreLengths;
            var protectOffsets = fileSymbol.RetainOffsets;
            var protectLengths = fileSymbol.RetainLengths;
            var symbols = fileSymbol.SymbolPaths;

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
                        this.ParseODBCDriverOrTranslator(child, componentId, id.Id, SymbolDefinitionType.ODBCDriver);
                        break;
                    case "ODBCTranslator":
                        this.ParseODBCDriverOrTranslator(child, componentId, id.Id, SymbolDefinitionType.ODBCTranslator);
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
                    var context = new Dictionary<string, string>() { { "FileId", id?.Id }, { "ComponentId", componentId }, { "DirectoryId", directoryId }, { "Win64", win64Component.ToString() } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            fileSymbol.IgnoreOffsets = ignoreOffsets;
            fileSymbol.IgnoreLengths = ignoreLengths;
            fileSymbol.RetainOffsets = protectOffsets;
            fileSymbol.RetainLengths = protectLengths;
            fileSymbol.SymbolPaths = symbols;
        }


        /// <summary>
        /// Parses a File element.
        /// </summary>
        /// <param name="node">File element to parse.</param>
        /// <param name="componentId">Parent's component id.</param>
        /// <param name="directoryId">Ancestor's directory id.</param>
        /// <param name="diskId">Disk id inherited from parent component.</param>
        /// <param name="sourcePath">Default source path of parent directory.</param>
        /// <param name="possibleKeyPath">This will be set with the possible keyPath for the parent component.</param>
        /// <param name="win64Component">true if the component is 64-bit.</param>
        /// <param name="componentGuid">Component GUID (including `*`).</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        private YesNoType ParseFileElement(XElement node, string componentId, string directoryId, int diskId, string sourcePath, out string possibleKeyPath, bool win64Component, string componentGuid)
        {
            var keyPath = this.ParseFileElementAttributes(node, componentId, directoryId, diskId, sourcePath, out possibleKeyPath, componentGuid, isNakedFile: false, out var fileSymbol, out var assemblySymbol);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(fileSymbol);

                if (assemblySymbol != null)
                {
                    this.Core.AddSymbol(assemblySymbol);
                }

                this.ParseFileElementChildren(node, fileSymbol, keyPath, win64Component);
            }

            return keyPath;
        }

        /// <summary>
        /// Parses a file element outside a component.
        /// </summary>
        /// <param name="node">File element to parse.</param>
        /// <param name="parentType">Type of complex reference parent. Will be Unknown if there is no parent.</param>
        /// <param name="parentId">Optional identifier for primary parent.</param>
        /// <param name="directoryId">Ancestor's directory id.</param>
        /// <param name="sourcePath">Default source path of parent directory.</param>
        /// <returns>Yes if this element was marked as the parent component's key path, No if explicitly marked as not being a key path, or NotSet otherwise.</returns>
        private void ParseNakedFileElement(XElement node, ComplexReferenceParentType parentType, string parentId, string directoryId, string sourcePath)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var win64 = this.Context.IsCurrentPlatform64Bit;
            string condition = null;
            string subdirectory = null;

            var keyPath = this.ParseFileElementAttributes(node, "@WixTemporaryComponentId", directoryId, diskId: CompilerConstants.IntegerNotSet, sourcePath, out var _, componentGuid: "*", isNakedFile: true, out var fileSymbol, out var assemblySymbol);

            if (!this.Core.EncounteredError)
            {
                // Naked files have additional attributes to handle common component attributes.
                foreach (var attrib in node.Attributes())
                {
                    if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                    {
                        switch (attrib.Name.LocalName)
                        {
                        case "Bitness":
                            var bitnessValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (bitnessValue)
                            {
                                case "always32":
                                    win64 = false;
                                    break;
                                case "always64":
                                    win64 = true;
                                    break;
                                case "default":
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                                    break;
                            }
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directoryId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
                            break;
                        case "Subdirectory":
                            subdirectory = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, allowRelative: true);
                            break;
                        }
                    }
                }

                if (String.IsNullOrEmpty(directoryId))
                {
                    directoryId = "INSTALLFOLDER";
                    this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, directoryId);
                }

                directoryId = this.HandleSubdirectory(sourceLineNumbers, node, directoryId, subdirectory, "Directory", "Subdirectory");

                this.Core.AddSymbol(new ComponentSymbol(sourceLineNumbers, fileSymbol.Id)
                {
                    ComponentId = "*",
                    DirectoryRef = directoryId,
                    Location = ComponentLocation.LocalOnly,
                    Condition = condition,
                    KeyPath = fileSymbol.Id.Id,
                    KeyPathType = ComponentKeyPathType.File,
                    DisableRegistryReflection = false,
                    NeverOverwrite = false,
                    Permanent = false,
                    SharedDllRefCount = false,
                    Shared = false,
                    Transitive = false,
                    UninstallWhenSuperseded = false,
                    Win64 = win64,
                });

                fileSymbol.ComponentRef = fileSymbol.Id.Id;
                this.Core.AddSymbol(fileSymbol);

                if (assemblySymbol != null)
                {
                    this.Core.AddSymbol(assemblySymbol);
                }

                this.ParseFileElementChildren(node, fileSymbol, keyPath, win64);

                if (ComplexReferenceParentType.Unknown != parentType && null != parentId) // if parent was provided, add a complex reference to that.
                {
                    // If the naked file's component is defined directly under a feature, then mark the complex reference primary.
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.Component, fileSymbol.Id.Id, ComplexReferenceParentType.Feature == parentType);
                }
            }
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
                    id = new Identifier(AccessModifier.Section, parentSignature);
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
                var symbol = this.Core.AddSymbol(new SignatureSymbol(sourceLineNumbers, id)
                {
                    FileName = name ?? shortName,
                    MinVersion = minVersion,
                    MaxVersion = maxVersion,
                    Languages = languages
                });

                if (CompilerConstants.IntegerNotSet != minSize)
                {
                    symbol.MinSize = minSize;
                }

                if (CompilerConstants.IntegerNotSet != maxSize)
                {
                    symbol.MaxSize = maxSize;
                }

                if (CompilerConstants.IntegerNotSet != minDate)
                {
                    symbol.MinDate = minDate;
                }

                if (CompilerConstants.IntegerNotSet != maxDate)
                {
                    symbol.MaxDate = maxDate;
                }

                // Create a DrLocator row to associate the file with a directory
                // when a different identifier is specified for the FileSearch.
                if (!isSameId)
                {
                    if (parentDirectorySearch)
                    {
                        // Creates the DrLocator row for the directory search while
                        // the parent DirectorySearch creates the file locator row.
                        this.Core.AddSymbol(new DrLocatorSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, parentSignature, id.Id, String.Empty))
                        {
                            SignatureRef = parentSignature,
                            Parent = id.Id
                        });
                    }
                    else
                    {
                        this.Core.AddSymbol(new DrLocatorSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, id.Id, parentSignature, String.Empty))
                        {
                            SignatureRef = id.Id,
                            Parent = parentSignature
                        });
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
            Identifier id = null;

            this.activeName = null;
            this.activeLanguage = null;

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

            // NOTE: Id is not required for Fragments, this is a departure from the normal run of the mill processing.

            this.Core.CreateActiveSection(id?.Id, SectionType.Fragment, this.Context.CompilationId);

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
                        this.ParseSequenceElement(child, SequenceTable.AdminExecuteSequence);
                        break;
                    case "AdminUISequence":
                        this.ParseSequenceElement(child, SequenceTable.AdminUISequence);
                        break;
                    case "AdvertiseExecuteSequence":
                        this.ParseSequenceElement(child, SequenceTable.AdvertiseExecuteSequence);
                        break;
                    case "InstallExecuteSequence":
                        this.ParseSequenceElement(child, SequenceTable.InstallExecuteSequence);
                        break;
                    case "InstallUISequence":
                        this.ParseSequenceElement(child, SequenceTable.InstallUISequence);
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
                    case "BundleCustomData":
                        this.ParseBundleCustomDataElement(child);
                        break;
                    case "BundleCustomDataRef":
                        this.ParseBundleCustomDataRefElement(child);
                        break;
                    case "BundleExtension":
                        this.ParseBundleExtensionElement(child);
                        break;
                    case "BundleExtensionRef":
                        this.ParseSimpleRefElement(child, SymbolDefinitions.WixBundleExtension);
                        break;
                    case "ComplianceCheck":
                        this.ParseComplianceCheckElement(child);
                        break;
                    case "Component":
                        this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, CompilerConstants.IntegerNotSet, null, null);
                        break;
                    case "ComponentGroup":
                        this.ParseComponentGroupElement(child, ComplexReferenceParentType.Unknown, id?.Id);
                        break;
                    case "Container":
                        this.ParseContainerElement(child);
                        break;
                    case "CustomAction":
                        this.ParseCustomActionElement(child);
                        break;
                    case "CustomActionRef":
                        this.ParseSimpleRefElement(child, SymbolDefinitions.CustomAction);
                        break;
                    case "CustomTable":
                        this.ParseCustomTableElement(child);
                        break;
                    case "CustomTableRef":
                        this.ParseCustomTableRefElement(child);
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
                        this.ParseSimpleRefElement(child, SymbolDefinitions.MsiEmbeddedChainer);
                        break;
                    case "EnsureTable":
                        this.ParseEnsureTableElement(child);
                        break;
                    case "Feature":
                        this.ParseFeatureElement(child, ComplexReferenceParentType.Unknown, null, ref featureDisplay);
                        break;
                    case "FeatureGroup":
                        this.ParseFeatureGroupElement(child, ComplexReferenceParentType.Unknown, id?.Id);
                        break;
                    case "FeatureRef":
                        this.ParseFeatureRefElement(child, ComplexReferenceParentType.Unknown, null);
                        break;
                    case "File":
                        this.ParseNakedFileElement(child, ComplexReferenceParentType.Unknown, null, null, null);
                        break;
                    case "Icon":
                        this.ParseIconElement(child);
                        break;
                    case "Media":
                        this.ParseMediaElement(child, null);
                        break;
                    case "MediaTemplate":
                        this.ParseMediaTemplateElement(child, null);
                        break;
                    case "Launch":
                        this.ParseLaunchElement(child);
                        break;
                    case "PackageGroup":
                        this.ParsePackageGroupElement(child);
                        break;
                    case "PackageCertificates":
                    case "PatchCertificates":
                        this.ParseCertificatesElement(child);
                        break;
                    case "PatchFamily":
                        this.ParsePatchFamilyElement(child, ComplexReferenceParentType.Unknown, id?.Id);
                        break;
                    case "PatchFamilyGroup":
                        this.ParsePatchFamilyGroupElement(child, ComplexReferenceParentType.Unknown, id?.Id);
                        break;
                    case "PatchFamilyGroupRef":
                        this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.Unknown, id?.Id);
                        break;
                    case "PayloadGroup":
                        this.ParsePayloadGroupElement(child, ComplexReferenceParentType.Unknown, null);
                        break;
                    case "Property":
                        this.ParsePropertyElement(child);
                        break;
                    case "PropertyRef":
                        this.ParseSimpleRefElement(child, SymbolDefinitions.Property);
                        break;
                    case "RelatedBundle":
                        this.ParseRelatedBundleElement(child);
                        break;
                    case "Requires":
                        this.ParseRequiresElement(child, null);
                        break;
                    case "SetDirectory":
                        this.ParseSetDirectoryElement(child);
                        break;
                    case "SetProperty":
                        this.ParseSetPropertyElement(child);
                        break;
                    case "SetVariable":
                        this.ParseSetVariableElement(child);
                        break;
                    case "SetVariableRef":
                        this.ParseSimpleRefElement(child, SymbolDefinitions.WixSetVariable);
                        break;
                    case "SFPCatalog":
                        string parentName = null;
                        this.ParseSFPCatalogElement(child, ref parentName);
                        break;
                    case "StandardDirectory":
                        this.ParseStandardDirectoryElement(child);
                        break;
                    case "UI":
                        this.ParseUIElement(child);
                        break;
                    case "UIRef":
                        this.ParseSimpleRefElement(child, SymbolDefinitions.WixUI);
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
                this.Core.AddSymbol(new WixFragmentSymbol(sourceLineNumbers, id));
            }
        }

        /// <summary>
        /// Parses a launch condition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseLaunchElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = null;
            string message = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Message":
                            message = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(message))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Message"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new LaunchConditionSymbol(sourceLineNumbers)
                {
                    Condition = condition,
                    Description = message
                });
            }
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
            IniFileActionType? action = null;
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
                                action = IniFileActionType.AddLine;
                                break;
                            case "addTag":
                                action = IniFileActionType.AddTag;
                                break;
                            case "createLine":
                                action = IniFileActionType.CreateLine;
                                break;
                            case "removeLine":
                                action = IniFileActionType.RemoveLine;
                                break;
                            case "removeTag":
                                action = IniFileActionType.RemoveTag;
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
            else if (IniFileActionType.AddLine == action || IniFileActionType.AddTag == action || IniFileActionType.CreateLine == action)
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
                this.Core.AddSymbol(new IniFileSymbol(sourceLineNumbers, id)
                {
                    FileName = name,
                    ShortFileName = shortName,
                    DirProperty = directory,
                    Section = section,
                    Key = key,
                    Value = value,
                    Action = action.Value,
                    ComponentRef = componentId
                });
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

            if (null == section)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Section"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("ini", name, section, key, field.ToString(), type.ToString());
            }

            var signature = id.Id;

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
                        id = new Identifier(AccessModifier.Section, signature); // FileSearch signatures override parent signatures
                        break;
                    case "FileSearchRef":
                        if (oneChild)
                        {
                            this.Core.Write(ErrorMessages.TooManySearchElements(sourceLineNumbers, node.Name.LocalName));
                        }
                        oneChild = true;
                        var newId = this.ParseSimpleRefElement(child, SymbolDefinitions.Signature); // FileSearch signatures override parent signatures
                        id = new Identifier(AccessModifier.Section, newId);
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
                var symbol = this.Core.AddSymbol(new IniLocatorSymbol(sourceLineNumbers, id)
                {
                    FileName = name,
                    ShortFileName = shortName,
                    Section = section,
                    Key = key,
                    Type = type
                });

                if (CompilerConstants.IntegerNotSet != field)
                {
                    symbol.Field = field;
                }
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Component, shared);
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
                this.Core.AddSymbol(new IsolatedComponentSymbol(sourceLineNumbers)
                {
                    SharedComponentRef = shared,
                    ApplicationComponentRef = componentId
                });
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
                            if ("PatchCertificates" == node.Name.LocalName)
                            {
                                this.Core.AddSymbol(new MsiPatchCertificateSymbol(sourceLineNumbers)
                                {
                                    PatchCertificate = name,
                                    DigitalCertificateRef = name,
                                });
                            }
                            else
                            {
                                this.Core.AddSymbol(new MsiPackageCertificateSymbol(sourceLineNumbers)
                                {
                                    PackageCertificate = name,
                                    DigitalCertificateRef = name,
                                });
                            }
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
                this.Core.AddSymbol(new MsiDigitalCertificateSymbol(sourceLineNumbers, id)
                {
                    CertData = sourceFile
                });
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
                this.Core.AddSymbol(new MsiDigitalSignatureSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "Media", diskId))
                {
                    Table = "Media",
                    SignObject = diskId,
                    DigitalCertificateRef = certificateId,
                    Hash = sourceFile
                });
            }
        }

        /// <summary>
        /// Parses a MajorUpgrade element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="contextValues">The current context.</param>
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
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Package", "UpgradeCode", node.Name.LocalName));
            }

            var productVersion = contextValues["ProductVersion"];
            if (String.IsNullOrEmpty(productVersion))
            {
                this.Core.Write(ErrorMessages.ParentElementAttributeRequired(sourceLineNumbers, "Package", "Version", node.Name.LocalName));
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
                var symbol = this.Core.AddSymbol(new UpgradeSymbol(sourceLineNumbers)
                {
                    UpgradeCode = upgradeCode,
                    Remove = removeFeatures,
                    MigrateFeatures = migrateFeatures,
                    IgnoreRemoveFailures = ignoreRemoveFailure,
                    ActionProperty = WixUpgradeConstants.UpgradeDetectedProperty
                });

                if (allowDowngrades)
                {
                    symbol.VersionMin = "0";
                    symbol.Language = productLanguage;
                    symbol.VersionMinInclusive = true;
                }
                else
                {
                    symbol.VersionMax = productVersion;
                    symbol.Language = productLanguage;
                    symbol.VersionMaxInclusive = allowSameVersionUpgrades;
                }

                // Add launch condition that blocks upgrades
                if (blockUpgrades)
                {
                    this.Core.AddSymbol(new LaunchConditionSymbol(sourceLineNumbers)
                    {
                        Condition = WixUpgradeConstants.UpgradePreventedCondition,
                        Description = disallowUpgradeErrorMessage
                    });
                }

                // now create the Upgrade row and launch conditions to prevent downgrades (unless explicitly permitted)
                if (!allowDowngrades)
                {
                    this.Core.AddSymbol(new UpgradeSymbol(sourceLineNumbers)
                    {
                        UpgradeCode = upgradeCode,
                        VersionMin = productVersion,
                        Language = productLanguage,
                        OnlyDetect = true,
                        IgnoreRemoveFailures = ignoreRemoveFailure,
                        ActionProperty = WixUpgradeConstants.DowngradeDetectedProperty
                    });

                    this.Core.AddSymbol(new LaunchConditionSymbol(sourceLineNumbers)
                    {
                        Condition = WixUpgradeConstants.DowngradePreventedCondition,
                        Description = downgradeErrorMessage
                    });
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

                this.Core.ScheduleActionSymbol(sourceLineNumbers, AccessModifier.Global, SequenceTable.InstallExecuteSequence, "RemoveExistingProducts", afterAction: after);
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
                        compressionLevel = this.ParseCompressionLevel(sourceLineNumbers, attrib);
                        break;
                    case "DiskPrompt":
                        diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Property, "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
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
                    if (!String.IsNullOrEmpty(cabinet) && !this.Core.IsValidLongFilename(cabinet) && !Common.ContainsValidBinderVariable(cabinet))
                    {
                        this.Core.Write(WarningMessages.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "Cabinet", cabinet));
                    }
                }
            }

            if (compressionLevel.HasValue && String.IsNullOrEmpty(cabinet))
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
                this.Core.AddSymbol(new MediaSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, id))
                {
                    DiskId = id,
                    DiskPrompt = diskPrompt,
                    Cabinet = cabinet,
                    VolumeLabel = volumeLabel,
                    Source = source, // the Source column is only set when creating a patch
                    CompressionLevel = compressionLevel,
                    Layout = layout
                });

                if (null != symbols)
                {
                    this.Core.AddSymbol(new WixDeltaPatchSymbolPathsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, SymbolPathType.Media, id))
                    {
                        SymbolType = SymbolPathType.Media,
                        SymbolId = id.ToString(CultureInfo.InvariantCulture),
                        SymbolPaths = symbols
                    });
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
            int? maximumUncompressedMediaSize = null;
            int? maximumCabinetSizeForLargeFileSplitting = null;
            CompressionLevel? compressionLevel = null; // this defaults to 'medium' in the MSI and Burn backends

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
                            // reason for having multiple cabinets. External cabinet files must also be valid file names.
                            if (exampleCabinetName.Equals(authoredCabinetTemplateValue, StringComparison.OrdinalIgnoreCase) || !this.Core.IsValidLongFilename(exampleCabinetName, false))
                            {
                                this.Core.Write(ErrorMessages.InvalidCabinetTemplate(sourceLineNumbers, cabinetTemplate));
                            }
                            else if (!this.Core.IsValidLongFilename(exampleCabinetName) && !Common.ContainsValidBinderVariable(exampleCabinetName)) // ignore short names with wix variables because it rarely works out.
                            {
                                this.Core.Write(WarningMessages.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name.LocalName, "CabinetTemplate", cabinetTemplate));
                            }
                        }
                        break;
                    case "CompressionLevel":
                        compressionLevel = this.ParseCompressionLevel(sourceLineNumbers, attrib);
                        break;
                    case "DiskPrompt":
                        diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Property, "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
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
                        maximumCabinetSizeForLargeFileSplitting = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Compiler.MinValueOfMaxCabSizeForLargeFileSplitting, Compiler.MaxValueOfMaxCabSizeForLargeFileSplitting);
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

            if (YesNoType.Yes == embedCab)
            {
                cabinetTemplate = String.Concat("#", cabinetTemplate);
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new MediaSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, 1))
                {
                    DiskId = 1
                });

                this.Core.AddSymbol(new WixMediaTemplateSymbol(sourceLineNumbers)
                {
                    CabinetTemplate = cabinetTemplate,
                    VolumeLabel = volumeLabel,
                    DiskPrompt = diskPrompt,
                    MaximumUncompressedMediaSize = maximumUncompressedMediaSize,
                    MaximumCabinetSizeForLargeFileSplitting = maximumCabinetSizeForLargeFileSplitting,
                    CompressionLevel = compressionLevel
                });

                //else
                //{
                //    mediaTemplateRow.MaximumUncompressedMediaSize = CompilerCore.DefaultMaximumUncompressedMediaSize;
                //}

                //else
                //{
                //    mediaTemplateRow.MaximumCabinetSizeForLargeFileSplitting = 0; // Default value of 0 corresponds to max size of 2048 MB (i.e. 2 GB)
                //}
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
            FileSymbolAttributes attributes = 0;
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Media, diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
                        break;
                    case "FileCompression":
                        var compress = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        attributes |= compress == YesNoType.Yes ? FileSymbolAttributes.Compressed : 0;
                        attributes |= compress == YesNoType.No ? FileSymbolAttributes.Uncompressed : 0;
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
                var symbol = this.Core.AddSymbol(new WixMergeSymbol(sourceLineNumbers, id)
                {
                    DirectoryRef = directoryId,
                    SourceFile = sourceFile,
                    DiskId = diskId,
                    ConfigurationData = configData,
                    FileAttributes = attributes,
                    FeatureRef = Guid.Empty.ToString("B")
                });

                symbol.Set((int)WixMergeSymbolFields.Language, language);
            }
        }

        /// <summary>
        /// Parses a standard directory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseStandardDirectoryElement(XElement node)
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

            if (String.IsNullOrEmpty(id))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (!WindowsInstallerStandard.IsStandardDirectory(id))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Id", id, String.Join(", \"", WindowsInstallerStandard.StandardDirectoryIds())));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Unknown, null, null, diskId: CompilerConstants.IntegerNotSet, id, srcPath: String.Empty);
                            break;
                        case "Directory":
                            this.ParseDirectoryElement(child, id, diskId: CompilerConstants.IntegerNotSet, fileSource: String.Empty);
                            break;
                        case "File":
                            this.ParseNakedFileElement(child, ComplexReferenceParentType.Unknown, null, id, null);
                            break;
                        case "Merge":
                            this.ParseMergeElement(child, id, diskId: CompilerConstants.IntegerNotSet);
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
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, id);
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
        /// Parses a Level element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="featureId">Id of the parent Feature element.</param>
        private void ParseLevelElement(XElement node, string featureId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = null;
            int? level = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            level = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
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

            if (!level.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (String.IsNullOrEmpty(condition))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Condition"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                if (CompilerConstants.IntegerNotSet == level)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
                    level = CompilerConstants.IllegalInteger;
                }

                if (!this.Core.EncounteredError)
                {
                    this.Core.AddSymbol(new ConditionSymbol(sourceLineNumbers)
                    {
                        FeatureRef = featureId,
                        Level = level.Value,
                        Condition = condition
                    });
                }
            }
        }

        /// <summary>
        /// Parses a merge reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Parent's complex reference type.</param>
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixMerge, id);
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
                    this.Core.AddSymbol(new MIMESymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, contentType))
                    {
                        ContentType = contentType,
                        ExtensionRef = extension,
                        CLSID = classId
                    });
                }
            }
            else if (YesNoType.No == advertise)
            {
                if (YesNoType.Yes == returnContentType && YesNoType.Yes == parentAdvertised)
                {
                    this.Core.Write(ErrorMessages.CannotDefaultMismatchedAdvertiseStates(sourceLineNumbers));
                }

                this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "Extension", String.Concat(".", extension), componentId);
                if (null != classId)
                {
                    this.Core.CreateRegistryStringSymbol(sourceLineNumbers, RegistryRootType.ClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "CLSID", classId, componentId);
                }
            }

            return YesNoType.Yes == returnContentType ? contentType : null;
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
                this.Core.AddSymbol(new MsiPatchMetadataSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, company, name))
                {
                    Company = company,
                    Property = name,
                    Value = value
                });
            }
            else
            {
                if (null != company)
                {
                    this.Core.Write(ErrorMessages.UnexpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
                }
                this.AddPrivateProperty(sourceLineNumbers, name, value);
            }
        }

        /// <summary>
        /// Adds a row to the properties table.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        private void AddPrivateProperty(SourceLineNumber sourceLineNumbers, string name, string value)
        {
            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new PropertySymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, name))
                {
                    Value = value
                });
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
                this.Core.AddSymbol(new WixPatchRefSymbol(sourceLineNumbers)
                {
                    Table = "*",
                    PrimaryKeys = "*",
                });
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
                this.Core.AddSymbol(new WixPatchRefSymbol(sourceLineNumbers)
                {
                    Table = tableName,
                    PrimaryKeys = id
                });
            }
        }

        /// <summary>
        /// Parses a PatchBaseline element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="diskId">Media index from parent element.</param>
        private void ParsePatchBaselineElement(XElement node, int? diskId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var parsedValidate = false;
            var validationFlags = TransformFlags.PatchTransformDefault;
            string baselineFile = null;
            string updateFile = null;

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
                        break;
                    case "BaselineFile":
                        baselineFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "UpdateFile":
                        updateFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(baselineFile))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "BaselineFile"));
            }

            if (String.IsNullOrEmpty(updateFile))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UpdateFile"));
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
                this.Core.AddSymbol(new WixPatchBaselineSymbol(sourceLineNumbers, id)
                {
                    DiskId = diskId ?? 1,
                    ValidationFlags = validationFlags,
                    BaselineFile = new IntermediateFieldPathValue { Path = baselineFile },
                    UpdateFile = new IntermediateFieldPathValue { Path = updateFile },
                });
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

        private string HandleSubdirectory(SourceLineNumber sourceLineNumbers, XElement element, string directoryId, string subdirectory, string directoryAttributeName, string subdirectoryAttributename)
        {
            if (!String.IsNullOrEmpty(subdirectory))
            {
                if (String.IsNullOrEmpty(directoryId))
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, element.Name.LocalName, subdirectoryAttributename, directoryAttributeName));
                }
                else
                {
                    directoryId = this.Core.CreateDirectoryReferenceFromInlineSyntax(sourceLineNumbers, directoryId, subdirectory);
                }
            }

            return directoryId;
        }
    }
}
