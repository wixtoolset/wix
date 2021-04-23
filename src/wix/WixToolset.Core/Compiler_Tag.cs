// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses a Tag element for Software Id Tag registration under a Bundle element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseBundleTagElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string regid = null;
            string installPath = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDirectory":
                        case "Bitness":
                            this.Core.Write(ErrorMessages.ExpectedParentWithAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Package"));
                            break;
                        case "InstallPath":
                            installPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
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

            if (String.IsNullOrEmpty(name))
            {
                name = node.Parent?.Attribute("Name")?.Value;

                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name))
            {
                this.Core.Write(CompilerErrors.IllegalName(sourceLineNumbers, node.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.Write(CompilerErrors.ExampleRegid(sourceLineNumbers, regid));
            }

            if (String.IsNullOrEmpty(installPath))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "InstallPath"));
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundleTagSymbol(sourceLineNumbers)
                {
                    Filename = String.Concat(name, ".swidtag"),
                    Regid = regid,
                    Name = name,
                    InstallPath = installPath
                });
            }
        }

        /// <summary>
        /// Parses a Tag element for Software Id Tag registration under a Package element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePackageTagElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            string regid = null;
            string feature = null;
            string installDirectory = null;
            var win64 = this.Context.IsCurrentPlatform64Bit;

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
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDirectory":
                            installDirectory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallPath":
                            this.Core.Write(ErrorMessages.ExpectedParentWithAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bundle"));
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
                        default:
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

            if (String.IsNullOrEmpty(name))
            {
                name = node.Parent?.Attribute("Name")?.Value;

                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name))
            {
                this.Core.Write(CompilerErrors.IllegalName(sourceLineNumbers, node.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.Write(CompilerErrors.ExampleRegid(sourceLineNumbers, regid));
                return;
            }
            else if (id == null)
            {
                id = this.CreateTagId(regid);
            }

            if (String.IsNullOrEmpty(installDirectory))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "InstallDirectory"));
            }

            if (!this.Core.EncounteredError)
            {
                var fileName = String.Concat(name, ".swidtag");

                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Directory, installDirectory);
                this.Core.AddSymbol(new DirectorySymbol(sourceLineNumbers, id)
                {
                    Name = "swidtag",
                    ParentDirectoryRef = installDirectory,
                    ComponentGuidGenerationSeed = "4BAD0C8B-3AF0-BFE3-CC83-094749A1C4B1"
                });

                this.Core.AddSymbol(new ComponentSymbol(sourceLineNumbers, id)
                {
                    ComponentId = "*",
                    DirectoryRef = id.Id,
                    KeyPath = id.Id,
                    KeyPathType = ComponentKeyPathType.File,
                    Location = ComponentLocation.LocalOnly,
                    Win64 = win64
                });

                this.Core.AddSymbol(new FileSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = id.Id,
                    Name = fileName,
                    DiskId = 1,
                    Attributes = FileSymbolAttributes.ReadOnly,
                });

                if (!String.IsNullOrEmpty(feature))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Feature, feature);
                }
                else
                {
                    feature = "WixSwidTag";
                    this.Core.AddSymbol(new FeatureSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, feature))
                    {
                        Title = "ISO/IEC 19770-2",
                        Level = 1,
                        InstallDefault = FeatureInstallDefault.Local,
                        Display = 0,
                        DisallowAdvertise = true,
                        DisallowAbsent = true,
                    });
                }
                this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, null, ComplexReferenceChildType.Component, id.Id, true);

                this.Core.EnsureTable(sourceLineNumbers, "SoftwareIdentificationTag");
                this.Core.AddSymbol(new WixProductTagSymbol(sourceLineNumbers, id)
                {
                    FileRef = id.Id,
                    Regid = regid,
                    Name = name
                });
            }
        }

        /// <summary>
        /// Parses a TagRef element for Software Id Tag registration under a PatchFamily element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseTagRefElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string regid = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
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

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }
            else if (regid.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.Write(CompilerErrors.ExampleRegid(sourceLineNumbers, regid));
            }

            if (!this.Core.EncounteredError)
            {
                var id = this.CreateTagId(regid);

                this.Core.AddSymbol(new WixPatchRefSymbol(sourceLineNumbers, id)
                {
                    Table = SymbolDefinitions.Component.Name,
                    PrimaryKeys = id.Id
                });
            }
        }

        private Identifier CreateTagId(string regid) => this.Core.CreateIdentifier("tag", regid, ".product.tag");
    }
}
