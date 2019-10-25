// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ProcessPayloadsCommand
    {
        private static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

        public ProcessPayloadsCommand(IServiceProvider serviceProvider, IBackendHelper backendHelper, IEnumerable<WixBundlePayloadTuple> payloads, PackagingType defaultPackaging, string layoutDirectory)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();

            this.BackendHelper = backendHelper;
            this.Payloads = payloads;
            this.DefaultPackaging = defaultPackaging;
            this.LayoutDirectory = layoutDirectory;
        }

        public IEnumerable<IFileTransfer> FileTransfers { get; private set; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IEnumerable<WixBundlePayloadTuple> Payloads { get; }

        private PackagingType DefaultPackaging { get; }

        private string LayoutDirectory { get; }

        public void Execute()
        {
            var fileTransfers = new List<IFileTransfer>();

            foreach (var payload in this.Payloads)
            {
                var normalizedPath = payload.Name.Replace('\\', '/');
                if (normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../"))
                {
                    this.Messaging.Write(ErrorMessages.PayloadMustBeRelativeToCache(payload.SourceLineNumbers, "Payload", "Name", payload.Name));
                }

                // Embedded files (aka: files from binary .wixlibs) are not content files (because they are hidden
                // in the .wixlib).
                var sourceFile = payload.SourceFile;
                payload.ContentFile = !sourceFile.Embed;

                this.UpdatePayloadPackagingType(payload);

                if (String.IsNullOrEmpty(sourceFile?.Path))
                {
                    // Remote payloads obviously cannot be embedded.
                    Debug.Assert(PackagingType.Embedded != payload.Packaging);
                }
                else // not a remote payload so we have a lot more to update.
                {
                    this.UpdatePayloadFileInformation(payload, sourceFile);

                    this.UpdatePayloadVersionInformation(payload, sourceFile);

                    // External payloads need to be transfered.
                    if (PackagingType.External == payload.Packaging)
                    {
                        var transfer = this.BackendHelper.CreateFileTransfer(sourceFile.Path, Path.Combine(this.LayoutDirectory, payload.Name), false, payload.SourceLineNumbers);
                        fileTransfers.Add(transfer);
                    }
                }
            }

            this.FileTransfers = fileTransfers;
        }

        private void UpdatePayloadPackagingType(WixBundlePayloadTuple payload)
        {
            if (PackagingType.Unknown == payload.Packaging)
            {
                if (!payload.Compressed.HasValue)
                {
                    payload.Packaging = this.DefaultPackaging;
                }
                else if (payload.Compressed.Value)
                {
                    payload.Packaging = PackagingType.Embedded;
                }
                else
                {
                    payload.Packaging = PackagingType.External;
                }
            }

            // Embedded payloads that are not assigned a container already are placed in the default attached
            // container.
            if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.ContainerRef))
            {
                payload.ContainerRef = BurnConstants.BurnDefaultAttachedContainerName;
            }
        }

        private void UpdatePayloadFileInformation(WixBundlePayloadTuple payload, IntermediateFieldPathValue sourceFile)
        {
            var fileInfo = new FileInfo(sourceFile.Path);

            if (null != fileInfo)
            {
                payload.FileSize = (int)fileInfo.Length;

                payload.Hash = BundleHashAlgorithm.Hash(fileInfo);

                // Try to get the certificate if the payload is a signed file and we're not suppressing signature validation.
                if (payload.EnableSignatureValidation)
                {
                    X509Certificate2 certificate = null;
                    try
                    {
                        certificate = new X509Certificate2(fileInfo.FullName);
                    }
                    catch (CryptographicException) // we don't care about non-signed files.
                    {
                    }

                    // If there is a certificate, remember its hashed public key identifier and thumbprint.
                    if (null != certificate)
                    {
                        byte[] publicKeyIdentifierHash = new byte[128];
                        uint publicKeyIdentifierHashSize = (uint)publicKeyIdentifierHash.Length;

                        Native.NativeMethods.HashPublicKeyInfo(certificate.Handle, publicKeyIdentifierHash, ref publicKeyIdentifierHashSize);

                        var sb = new StringBuilder(((int)publicKeyIdentifierHashSize + 1) * 2);
                        for (var i = 0; i < publicKeyIdentifierHashSize; ++i)
                        {
                            sb.AppendFormat("{0:X2}", publicKeyIdentifierHash[i]);
                        }

                        payload.PublicKey = sb.ToString();
                        payload.Thumbprint = certificate.Thumbprint;
                    }
                }
            }
        }

        private void UpdatePayloadVersionInformation(WixBundlePayloadTuple payload, IntermediateFieldPathValue sourceFile)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(sourceFile.Path);

            if (null != versionInfo)
            {
                // Use the fixed version info block for the file since the resource text may not be a dotted quad.
                var version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);

                if (ProcessPayloadsCommand.EmptyVersion != version)
                {
                    payload.Version = version.ToString();
                }

                payload.Description = versionInfo.FileDescription;
                payload.DisplayName = versionInfo.ProductName;
            }
        }
    }
}
