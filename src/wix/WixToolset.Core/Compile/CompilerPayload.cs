// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;

    internal class CompilerPayload
    {
        public CompilerPayload(CompilerCore core, SourceLineNumber sourceLineNumbers, XElement element)
        {
            this.Core = core;
            this.Element = element;
            this.SourceLineNumbers = sourceLineNumbers;
        }

        public CompilerCore Core { get; }

        public XElement Element { get; }

        public SourceLineNumber SourceLineNumbers { get; }

        public YesNoDefaultType Compressed { get; set; } = YesNoDefaultType.Default;

        public string Description { get; set; }

        public string DownloadUrl { get; set; }

        public string CertificatePublicKey { get; set; }

        public string CertificateThumbprint { get; set; }

        public string Hash { get; set; }

        public Identifier Id { get; set; }

        public bool IsRemoteAllowed { get; set; }

        public bool IsRequired { get; set; } = true;

        public string Name { get; set; }

        public string ProductName { get; set; }

        public long? Size { get; set; }

        public string SourceFile { get; set; }

        public string Version { get; set; }

        private void CalculateAndVerifyFields()
        {
            var isRemote = this.IsRemoteAllowed && (!String.IsNullOrEmpty(this.CertificatePublicKey) || !String.IsNullOrEmpty(this.CertificateThumbprint) || !String.IsNullOrEmpty(this.Hash));

            if (String.IsNullOrEmpty(this.SourceFile))
            {
                if (!String.IsNullOrEmpty(this.Name) && !isRemote)
                {
                    this.SourceFile = Path.Combine("SourceDir", this.Name);
                }
            }
            else if (this.SourceFile.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                if (String.IsNullOrEmpty(this.Name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Name", "SourceFile", this.SourceFile));
                }
                else
                {
                    this.SourceFile = Path.Combine(this.SourceFile, Path.GetFileName(this.Name));
                }
            }

            if (String.IsNullOrEmpty(this.SourceFile) && !isRemote)
            {
                if (this.IsRequired)
                {
                    if (!this.IsRemoteAllowed)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributes(this.SourceLineNumbers, this.Element.Name.LocalName, "Name", "SourceFile"));
                    }
                    else
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributes(this.SourceLineNumbers, this.Element.Name.LocalName, "SourceFile", "CertificatePublicKey", "Hash"));
                    }
                }
            }
            else if (this.IsRemoteAllowed)
            {
                var isLocal = !String.IsNullOrEmpty(this.SourceFile);

                if (isLocal)
                {
                    if (isRemote)
                    {
                        if (!String.IsNullOrEmpty(this.Hash))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Hash", "SourceFile"));
                        }

                        if (!String.IsNullOrEmpty(this.CertificatePublicKey))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "CertificatePublicKey", "SourceFile"));
                        }

                        if (!String.IsNullOrEmpty(this.CertificateThumbprint))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "CertificateThumbprint", "SourceFile"));
                        }
                    }

                    if (!String.IsNullOrEmpty(this.Description))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Description", "SourceFile"));
                    }

                    if (!String.IsNullOrEmpty(this.ProductName))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "ProductName", "SourceFile"));
                    }

                    if (this.Size.HasValue)
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Size", "SourceFile"));
                    }

                    if (!String.IsNullOrEmpty(this.Version))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Version", "SourceFile"));
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(this.DownloadUrl))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "DownloadUrl", "SourceFile"));
                    }

                    if (String.IsNullOrEmpty(this.Name))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Name", "SourceFile"));
                    }

                    // If remote payload is being verified by a certificate.
                    if (!String.IsNullOrEmpty(this.CertificatePublicKey) || !String.IsNullOrEmpty(this.CertificateThumbprint))
                    {
                        var oneOfCertificateAttributeNames = !String.IsNullOrEmpty(this.CertificatePublicKey) ? "CertificatePublicKey" : "CertificateThumbprint";

                        if (String.IsNullOrEmpty(this.CertificateThumbprint))
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "CertificateThumbprint", "CertificatePublicKey"));
                        }
                        else if (String.IsNullOrEmpty(this.CertificatePublicKey))
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "CertificatePublicKey", "CertificateThumbprint"));
                        }

                        if (!String.IsNullOrEmpty(this.Hash))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Hash", oneOfCertificateAttributeNames));
                        }
                    }
                    else // payload is being verified by hash.
                    {
                        if (String.IsNullOrEmpty(this.Hash))
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Hash", "SourceFile"));
                        }

                        if (!this.Size.HasValue)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Size", "SourceFile"));
                        }
                    }

                    if (YesNoDefaultType.Yes == this.Compressed)
                    {
                        this.Core.Write(WarningMessages.RemotePayloadsMustNotAlsoBeCompressed(this.SourceLineNumbers, this.Element.Name.LocalName));
                    }

                    this.Compressed = YesNoDefaultType.No;
                }
            }
        }

        public WixBundlePayloadSymbol CreatePayloadSymbol(ComplexReferenceParentType parentType, string parentId)
        {
            WixBundlePayloadSymbol symbol = null;

            if (parentType == ComplexReferenceParentType.Container && parentId == BurnConstants.BurnUXContainerName)
            {
                if (this.Compressed == YesNoDefaultType.No)
                {
                    this.Core.Write(WarningMessages.UxPayloadsOnlySupportEmbedding(this.SourceLineNumbers, this.SourceFile));
                }

                if (!String.IsNullOrEmpty(this.DownloadUrl))
                {
                    this.Core.Write(WarningMessages.DownloadUrlNotSupportedForBAPayloads(this.SourceLineNumbers, this.Id.Id));
                }

                this.Compressed = YesNoDefaultType.Yes;
                this.DownloadUrl = null;
            }

            if (!this.Core.EncounteredError)
            {
                symbol = this.Core.AddSymbol(new WixBundlePayloadSymbol(this.SourceLineNumbers, this.Id)
                {
                    Name = String.IsNullOrEmpty(this.Name) ? Path.GetFileName(this.SourceFile) : this.Name,
                    SourceFile = new IntermediateFieldPathValue { Path = this.SourceFile },
                    DownloadUrl = this.DownloadUrl,
                    Compressed = (this.Compressed == YesNoDefaultType.Yes) ? true : (this.Compressed == YesNoDefaultType.No) ? (bool?)false : null,
                    UnresolvedSourceFile = this.SourceFile, // duplicate of sourceFile but in a string column so it won't get resolved to a full path during binding.
                    DisplayName = this.ProductName,
                    Description = this.Description,
                    Hash = this.Hash,
                    FileSize = this.Size,
                    Version = this.Version,
                    CertificatePublicKey = this.CertificatePublicKey,
                    CertificateThumbprint = this.CertificateThumbprint
                });

                this.Core.CreateGroupAndOrderingRows(this.SourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Payload, symbol.Id.Id, ComplexReferenceChildType.Unknown, null);
            }

            return symbol;
        }

        public void FinishCompilingPackage()
        {
            this.CalculateAndVerifyFields();
            this.GenerateIdFromFilename();

            if (this.Id == null)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Id"));
                this.Id = Identifier.Invalid;
            }
        }

        public void FinishCompilingPackagePayload()
        {
            this.CalculateAndVerifyFields();
            this.GenerateIdFromFilename();
        }

        public void FinishCompilingPayload(string parentId)
        {
            this.CalculateAndVerifyFields();
            this.GenerateIdFromPrefix("pay", parentId);
        }

        private void GenerateIdFromFilename()
        {
            if (this.Id == null)
            {
                if (!String.IsNullOrEmpty(this.Name))
                {
                    this.Id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(this.Name));
                }
                else if (!String.IsNullOrEmpty(this.SourceFile))
                {
                    this.Id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(this.SourceFile));
                }
                else // if Name and SourceFile were not specified an error was already reported.
                {
                    this.Id = Identifier.Invalid;
                }
            }
        }

        private void GenerateIdFromPrefix(string prefix, string parentId)
        {
            if (this.Id == null)
            {
                this.Id = this.Core.CreateIdentifier(prefix, parentId, this.SourceFile?.ToUpperInvariant() ?? this.Name?.ToUpperInvariant());
            }
        }

        public void ParseCompressed(XAttribute attrib)
        {
            this.Compressed = this.Core.GetAttributeYesNoDefaultValue(this.SourceLineNumbers, attrib);
        }

        public void ParseDescription(XAttribute attrib)
        {
            this.Description = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseDownloadUrl(XAttribute attrib)
        {
            this.DownloadUrl = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseCertificatePublicKey(XAttribute attrib)
        {
            this.CertificatePublicKey = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseCertificateThumbprint(XAttribute attrib)
        {
            this.CertificateThumbprint = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseHash(XAttribute attrib)
        {
            this.Hash = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseId(XAttribute attrib)
        {
            this.Id = this.Core.GetAttributeIdentifier(this.SourceLineNumbers, attrib);
        }

        public void ParseName(XAttribute attrib)
        {
            this.Name = this.Core.GetAttributeLongFilename(this.SourceLineNumbers, attrib, false, true);
            if (!this.Core.IsValidLongFilename(this.Name, false, true))
            {
                this.Core.Write(ErrorMessages.IllegalLongFilename(this.SourceLineNumbers, this.Element.Name.LocalName, "Name", this.Name));
            }
        }

        public void ParseProductName(XAttribute attrib)
        {
            this.ProductName = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseSize(XAttribute attrib)
        {
            this.Size = this.Core.GetAttributeLongValue(this.SourceLineNumbers, attrib, 1, Int64.MaxValue);
        }

        public void ParseSourceFile(XAttribute attrib)
        {
            this.SourceFile = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseVersion(XAttribute attrib)
        {
            this.Version = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }
    }
}
