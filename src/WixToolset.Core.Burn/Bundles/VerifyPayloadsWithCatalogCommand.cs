// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    internal class VerifyPayloadsWithCatalogCommand
    {
        public VerifyPayloadsWithCatalogCommand(IMessaging messaging, IEnumerable<WixBundleCatalogTuple> catalogs, IEnumerable<WixBundlePayloadTuple> payloads)
        {
            this.Messaging = messaging;
            this.Catalogs = catalogs;
            this.Payloads = payloads;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<WixBundleCatalogTuple> Catalogs { get; }

        private IEnumerable<WixBundlePayloadTuple> Payloads { get; }

        public void Execute()
        {
            var catalogIdsWithPaths = this.Catalogs
                .Join(this.Payloads,
                    catalog => catalog.PayloadRef,
                    payload => payload.Id.Id,
                    (catalog, payload) => new CatalogIdWithPath() { Id = catalog.Id.Id, FullPath = Path.GetFullPath(payload.SourceFile.Path) })
                .ToList();

            foreach (var payloadInfo in this.Payloads)
            {
                // Payloads that are not embedded should be verfied.
                if (String.IsNullOrEmpty(payloadInfo.EmbeddedId))
                {
                    var sourceFile = payloadInfo.SourceFile.Path;
                    var validated = false;

                    foreach (var catalog in catalogIdsWithPaths)
                    {
                        if (!validated)
                        {
                            // Get the file hash
                            uint cryptHashSize = 20;
                            byte[] cryptHashBytes = new byte[cryptHashSize];
                            int error;
                            using (var payloadStream = File.OpenRead(sourceFile))
                            {
                                // Get the file handle
                                var fileHandle = payloadStream.SafeFileHandle.DangerousGetHandle();

                                // 20 bytes is usually the hash size.  Future hashes may be bigger
                                if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                                {
                                    error = Marshal.GetLastWin32Error();

                                    if (VerifyInterop.ErrorInsufficientBuffer == error)
                                    {
                                        error = 0;
                                        cryptHashBytes = new byte[cryptHashSize];
                                        if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                                        {
                                            error = Marshal.GetLastWin32Error();
                                        }
                                    }

                                    if (0 != error)
                                    {
                                        this.Messaging.Write(ErrorMessages.CatalogFileHashFailed(sourceFile, error));
                                    }
                                }
                            }

                            VerifyInterop.WinTrustCatalogInfo catalogData = new VerifyInterop.WinTrustCatalogInfo();
                            VerifyInterop.WinTrustData trustData = new VerifyInterop.WinTrustData();
                            try
                            {
                                // Create WINTRUST_CATALOG_INFO structure
                                catalogData.cbStruct = (uint)Marshal.SizeOf(catalogData);
                                catalogData.cbCalculatedFileHash = cryptHashSize;
                                catalogData.pbCalculatedFileHash = Marshal.AllocCoTaskMem((int)cryptHashSize);
                                Marshal.Copy(cryptHashBytes, 0, catalogData.pbCalculatedFileHash, (int)cryptHashSize);

                                var hashString = new StringBuilder();
                                foreach (var hashByte in cryptHashBytes)
                                {
                                    hashString.Append(hashByte.ToString("X2"));
                                }
                                catalogData.pcwszMemberTag = hashString.ToString();

                                // The file names need to be lower case for older OSes
                                catalogData.pcwszMemberFilePath = sourceFile.ToLowerInvariant();
                                catalogData.pcwszCatalogFilePath = catalog.FullPath.ToLowerInvariant();

                                // Create WINTRUST_DATA structure
                                trustData.cbStruct = (uint)Marshal.SizeOf(trustData);
                                trustData.dwUIChoice = VerifyInterop.WTD_UI_NONE;
                                trustData.fdwRevocationChecks = VerifyInterop.WTD_REVOKE_NONE;
                                trustData.dwUnionChoice = VerifyInterop.WTD_CHOICE_CATALOG;
                                trustData.dwStateAction = VerifyInterop.WTD_STATEACTION_VERIFY;
                                trustData.dwProvFlags = VerifyInterop.WTD_REVOCATION_CHECK_NONE;

                                // Create the structure pointers for unmanaged
                                trustData.pCatalog = Marshal.AllocCoTaskMem(Marshal.SizeOf(catalogData));
                                Marshal.StructureToPtr(catalogData, trustData.pCatalog, false);

                                // Call WinTrustVerify to validate the file with the catalog
                                IntPtr noWindow = new IntPtr(-1);
                                Guid verifyGuid = new Guid(VerifyInterop.GenericVerify2);
                                long verifyResult = VerifyInterop.WinVerifyTrust(noWindow, ref verifyGuid, ref trustData);
                                if (0 == verifyResult)
                                {
                                    payloadInfo.CatalogRef = catalog.Id;
                                    validated = true;
                                    break;
                                }
                            }
                            finally
                            {
                                // Free the structure memory
                                if (IntPtr.Zero != trustData.pCatalog)
                                {
                                    Marshal.FreeCoTaskMem(trustData.pCatalog);
                                }

                                if (IntPtr.Zero != catalogData.pbCalculatedFileHash)
                                {
                                    Marshal.FreeCoTaskMem(catalogData.pbCalculatedFileHash);
                                }
                            }
                        }
                    }

                    // Error message if the file was not validated by one of the catalogs
                    if (!validated)
                    {
                        this.Messaging.Write(ErrorMessages.CatalogVerificationFailed(sourceFile));
                    }
                }
            }
        }

        private class CatalogIdWithPath
        {
            public string Id { get; set; }

            public string FullPath { get; set; }
        }
    }
}
