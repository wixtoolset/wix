// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses an EmbeddedChaniner element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseEmbeddedChainerElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string commandLine = null;
            string condition = null;
            string source = null;
            var type = 0;

            foreach (var attrib in node.Attributes())
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
                        type = 0x2;
                        this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.Binary, source); // add a reference to the appropriate Binary
                        break;
                    case "CommandLine":
                        commandLine = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Condition":
                        condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "FileSource":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "PropertySource"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        type = 0x12;
                        this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.File, source); // add a reference to the appropriate File
                        break;
                    case "PropertySource":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BinarySource", "FileSource"));
                        }
                        source = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        type = 0x32;
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
                this.Core.AddTuple(new MsiEmbeddedChainerTuple(sourceLineNumbers, id)
                {
                    Condition = condition,
                    CommandLine = commandLine,
                    Source = source,
                    Type = type
                });
            }
        }

        /// <summary>
        /// Parses an EmbeddedUI element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseEmbeddedUIElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
            var supportsBasicUI = false;
            var messageFilter = WindowsInstallerConstants.INSTALLLOGMODE_FATALEXIT | WindowsInstallerConstants.INSTALLLOGMODE_ERROR | WindowsInstallerConstants.INSTALLLOGMODE_WARNING | WindowsInstallerConstants.INSTALLLOGMODE_USER
                                | WindowsInstallerConstants.INSTALLLOGMODE_INFO | WindowsInstallerConstants.INSTALLLOGMODE_FILESINUSE | WindowsInstallerConstants.INSTALLLOGMODE_RESOLVESOURCE
                                | WindowsInstallerConstants.INSTALLLOGMODE_OUTOFDISKSPACE | WindowsInstallerConstants.INSTALLLOGMODE_ACTIONSTART | WindowsInstallerConstants.INSTALLLOGMODE_ACTIONDATA
                                | WindowsInstallerConstants.INSTALLLOGMODE_PROGRESS | WindowsInstallerConstants.INSTALLLOGMODE_COMMONDATA | WindowsInstallerConstants.INSTALLLOGMODE_INITIALIZE
                                | WindowsInstallerConstants.INSTALLLOGMODE_TERMINATE | WindowsInstallerConstants.INSTALLLOGMODE_SHOWDIALOG | WindowsInstallerConstants.INSTALLLOGMODE_RMFILESINUSE
                                | WindowsInstallerConstants.INSTALLLOGMODE_INSTALLSTART | WindowsInstallerConstants.INSTALLLOGMODE_INSTALLEND;
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
                    case "Name":
                        name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                        break;
                    case "IgnoreFatalExit":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_FATALEXIT;
                        }
                        break;
                    case "IgnoreError":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_ERROR;
                        }
                        break;
                    case "IgnoreWarning":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_WARNING;
                        }
                        break;
                    case "IgnoreUser":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_USER;
                        }
                        break;
                    case "IgnoreInfo":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_INFO;
                        }
                        break;
                    case "IgnoreFilesInUse":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_FILESINUSE;
                        }
                        break;
                    case "IgnoreResolveSource":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_RESOLVESOURCE;
                        }
                        break;
                    case "IgnoreOutOfDiskSpace":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_OUTOFDISKSPACE;
                        }
                        break;
                    case "IgnoreActionStart":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_ACTIONSTART;
                        }
                        break;
                    case "IgnoreActionData":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_ACTIONDATA;
                        }
                        break;
                    case "IgnoreProgress":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_PROGRESS;
                        }
                        break;
                    case "IgnoreCommonData":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_COMMONDATA;
                        }
                        break;
                    case "IgnoreInitialize":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_INITIALIZE;
                        }
                        break;
                    case "IgnoreTerminate":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_TERMINATE;
                        }
                        break;
                    case "IgnoreShowDialog":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_SHOWDIALOG;
                        }
                        break;
                    case "IgnoreRMFilesInUse":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_RMFILESINUSE;
                        }
                        break;
                    case "IgnoreInstallStart":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_INSTALLSTART;
                        }
                        break;
                    case "IgnoreInstallEnd":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            messageFilter ^= WindowsInstallerConstants.INSTALLLOGMODE_INSTALLEND;
                        }
                        break;
                    case "SourceFile":
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SupportBasicUI":
                        supportsBasicUI = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    default:
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

            foreach (var child in node.Elements())
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
                this.Core.AddTuple(new MsiEmbeddedUITuple(sourceLineNumbers, id)
                {
                    FileName = name,
                    EntryPoint = true,
                    SupportsBasicUI = supportsBasicUI,
                    MessageFilter = messageFilter,
                    Source = sourceFile
                });
            }
        }

        /// <summary>
        /// Parses a embedded UI resource element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Identifier of parent EmbeddedUI element.</param>
        private void ParseEmbeddedUIResourceElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string name = null;
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
                this.Core.AddTuple(new MsiEmbeddedUITuple(sourceLineNumbers, id)
                {
                    FileName = name,
                    Source = sourceFile
                });
            }
        }
    }
}
