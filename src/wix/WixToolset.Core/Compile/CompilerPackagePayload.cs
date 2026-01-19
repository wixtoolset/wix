// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class CompilerPackagePayload
    {
        public CompilerPackagePayload(CompilerPayload compilerPayload, WixBundlePackageType packageType)
        {
            this.CompilerPayload = compilerPayload;
            this.PackageType = packageType;
        }

        private CompilerCore Core => this.CompilerPayload.Core;

        private XElement Element => this.CompilerPayload.Element;

        private SourceLineNumber SourceLineNumbers => this.CompilerPayload.SourceLineNumbers;

        public CompilerPayload CompilerPayload { get; }

        private WixBundlePackageType PackageType { get; }

        public BundlePackagePayloadGenerationType? PayloadGenerationType { get; set; }

        public IntermediateSymbol CreatePackagePayloadSymbol(ComplexReferenceParentType parentType, string parentId)
        {
            var payload = this.CompilerPayload.CreatePayloadSymbol(parentType, parentId);
            if (payload == null)
            {
                return null;
            }

            IntermediateSymbol packagePayload;

            switch (this.PackageType)
            {
                case WixBundlePackageType.Bundle:
                    packagePayload = this.Core.AddSymbol(new WixBundleBundlePackagePayloadSymbol(payload.SourceLineNumbers, payload.Id)
                    {
                        PayloadGeneration = this.PayloadGenerationType ?? BundlePackagePayloadGenerationType.ExternalWithoutDownloadUrl,
                    });
                    break;

                case WixBundlePackageType.Exe:
                    packagePayload = this.Core.AddSymbol(new WixBundleExePackagePayloadSymbol(payload.SourceLineNumbers, payload.Id));
                    break;

                case WixBundlePackageType.Msi:
                    packagePayload = this.Core.AddSymbol(new WixBundleMsiPackagePayloadSymbol(payload.SourceLineNumbers, payload.Id));
                    break;

                case WixBundlePackageType.Msp:
                    packagePayload = this.Core.AddSymbol(new WixBundleMspPackagePayloadSymbol(payload.SourceLineNumbers, payload.Id));
                    break;

                case WixBundlePackageType.Msu:
                    packagePayload = this.Core.AddSymbol(new WixBundleMsuPackagePayloadSymbol(payload.SourceLineNumbers, payload.Id));
                    break;

                default:
                    throw new NotImplementedException();
            }

            this.Core.CreateGroupAndOrderingRows(payload.SourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PackagePayload, payload.Id?.Id, ComplexReferenceChildType.Unknown, null);

            return packagePayload;
        }

        public bool ParsePayloadGeneration(XAttribute attrib)
        {
            if (this.PackageType != WixBundlePackageType.Bundle)
            {
                return false;
            }

            var value = this.Core.GetAttributeValue(this.SourceLineNumbers, attrib);
            switch (value)
            {
                case "none":
                    this.PayloadGenerationType = BundlePackagePayloadGenerationType.None;
                    break;
                case "externalWithoutDownloadUrl":
                    this.PayloadGenerationType = BundlePackagePayloadGenerationType.ExternalWithoutDownloadUrl;
                    break;
                case "external":
                    this.PayloadGenerationType = BundlePackagePayloadGenerationType.External;
                    break;
                case "all":
                    this.PayloadGenerationType = BundlePackagePayloadGenerationType.All;
                    break;
                case "":
                    break;
                default:
                    this.Core.Write(ErrorMessages.IllegalAttributeValue(this.SourceLineNumbers, this.Element.Name.LocalName, attrib.Name.LocalName, value, "none", "externalWithoutDownloadUrl", "external", "all"));
                    break;
            }

            return true;
        }
    }
}
