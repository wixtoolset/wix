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
        public YesNoDefaultType Compressed { get; set; } = YesNoDefaultType.Default;

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public string DownloadUrl { get; set; }

        public string Hash { get; set; }

        public Identifier Id { get; set; }

        public bool IsRemoteAllowed { get; set; }

        public bool IsRequired { get; set; } = true;

        public string Name { get; set; }

        public string ProductName { get; set; }

        public long? Size { get; set; }

        public string SourceFile { get; set; }

        public string Version { get; set; }

        public CompilerPayload(CompilerCore core, SourceLineNumber sourceLineNumbers, XElement element)
        {
            this.Core = core;
            this.Element = element;
            this.SourceLineNumbers = sourceLineNumbers;
        }

        private CompilerCore Core { get; }

        private XElement Element { get; }

        private SourceLineNumber SourceLineNumbers { get; }

        private void CalculateAndVerifyFields()
        {
            var isRemote = this.IsRemoteAllowed && !String.IsNullOrEmpty(this.Hash);

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
                        this.Core.Write(ErrorMessages.ExpectedAttributes(this.SourceLineNumbers, this.Element.Name.LocalName, "SourceFile", "Hash"));
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
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Hash", "SourceFile"));
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(this.DownloadUrl))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "DownloadUrl", "Hash"));
                    }

                    if (String.IsNullOrEmpty(this.Name))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, "Name", "Hash"));
                    }

                    if (YesNoDefaultType.Yes == this.Compressed)
                    {
                        this.Core.Write(WarningMessages.RemotePayloadsMustNotAlsoBeCompressed(this.SourceLineNumbers, this.Element.Name.LocalName));
                    }

                    this.Compressed = YesNoDefaultType.No;
                }

                VerifyValidValue("Description", !String.IsNullOrEmpty(this.Description));
                VerifyValidValue("ProductName", !String.IsNullOrEmpty(this.ProductName));
                VerifyValidValue("Size", this.Size.HasValue);
                VerifyValidValue("Version", !String.IsNullOrEmpty(this.Version));

                void VerifyValidValue(string attributeName, bool isSpecified)
                {
                    if (isLocal && isSpecified)
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, attributeName, "SourceFile"));
                    }
                    else if (!isLocal && !isSpecified)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(this.SourceLineNumbers, this.Element.Name.LocalName, attributeName, "Hash"));
                    }
                }
            }
        }

        public WixBundlePayloadSymbol CreatePayloadSymbol(ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType = ComplexReferenceChildType.Unknown, string previousId = null)
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
                    this.Core.Write(WarningMessages.DownloadUrlNotSupportedForEmbeddedPayloads(this.SourceLineNumbers, this.Id.Id));
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
                    DisplayName = this.DisplayName ?? this.ProductName,
                    Description = this.Description,
                    Hash = this.Hash,
                    FileSize = this.Size,
                    Version = this.Version,
                });

                this.Core.CreateGroupAndOrderingRows(this.SourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Payload, symbol.Id.Id, previousType, previousId);
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
            this.GenerateIdFromPrefix("ppy");
        }

        public void FinishCompilingPayload()
        {
            this.CalculateAndVerifyFields();
            this.GenerateIdFromPrefix("pay");
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
            }
        }

        private void GenerateIdFromPrefix(string prefix)
        {
            if (this.Id == null)
            {
                this.Id = this.Core.CreateIdentifier(prefix, this.SourceFile?.ToUpperInvariant() ?? String.Empty);
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

        public void ParseDisplayName(XAttribute attrib)
        {
            this.DisplayName = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
        }

        public void ParseDownloadUrl(XAttribute attrib)
        {
            this.DownloadUrl = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
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
