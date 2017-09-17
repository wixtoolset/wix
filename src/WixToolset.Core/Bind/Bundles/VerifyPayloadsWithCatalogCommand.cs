// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bind.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class VerifyPayloadsWithCatalogCommand : ICommand
    {
        public IEnumerable<WixBundleCatalogRow> Catalogs { private get; set; }

        public IEnumerable<WixBundlePayloadRow> Payloads { private get; set; }

        public void Execute()
        {
            List<CatalogIdWithPath> catalogIdsWithPaths = this.Catalogs
                .Join(this.Payloads,
                    catalog => catalog.Payload,
                    payload => payload.Id,
                    (catalog, payload) => new CatalogIdWithPath() { Id = catalog.Id, FullPath = Path.GetFullPath(payload.SourceFile) })
                .ToList();

            foreach (WixBundlePayloadRow payloadInfo in this.Payloads)
            {
                // Payloads that are not embedded should be verfied.
                if (String.IsNullOrEmpty(payloadInfo.EmbeddedId))
                {
                    bool validated = false;

                    foreach (CatalogIdWithPath catalog in catalogIdsWithPaths)
                    {
                        if (!validated)
                        {
                            // Get the file hash
                            uint cryptHashSize = 20;
                            byte[] cryptHashBytes = new byte[cryptHashSize];
                            int error;
                            IntPtr fileHandle = IntPtr.Zero;
                            using (FileStream payloadStream = File.OpenRead(payloadInfo.FullFileName))
                            {
                                // Get the file handle
                                fileHandle = payloadStream.SafeFileHandle.DangerousGetHandle();

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
                                        Messaging.Instance.OnMessage(WixErrors.CatalogFileHashFailed(payloadInfo.FullFileName, error));
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

                                StringBuilder hashString = new StringBuilder();
                                foreach (byte hashByte in cryptHashBytes)
                                {
                                    hashString.Append(hashByte.ToString("X2"));
                                }
                                catalogData.pcwszMemberTag = hashString.ToString();

                                // The file names need to be lower case for older OSes
                                catalogData.pcwszMemberFilePath = payloadInfo.FullFileName.ToLowerInvariant();
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
                                    payloadInfo.Catalog = catalog.Id;
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
                        Messaging.Instance.OnMessage(WixErrors.CatalogVerificationFailed(payloadInfo.FullFileName));
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
