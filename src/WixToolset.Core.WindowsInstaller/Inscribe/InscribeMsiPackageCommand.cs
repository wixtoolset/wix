// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Inscribe
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using WixToolset.Core.WindowsInstaller.Bind;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class InscribeMsiPackageCommand
    {
        public InscribeMsiPackageCommand(IInscribeContext context)
        {
            this.Context = context;
            this.Messaging = context.ServiceProvider.GetService<IMessaging>();
            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerTableDefinitions.All);
        }

        private IInscribeContext Context { get; }

        private IMessaging Messaging { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        public bool Execute()
        {
            // Keeps track of whether we've encountered at least one signed cab or not - we'll throw a warning if no signed cabs were encountered
            var foundUnsignedExternals = false;
            var shouldCommit = false;

            var attributes = File.GetAttributes(this.Context.InputFilePath);
            if (FileAttributes.ReadOnly == (attributes & FileAttributes.ReadOnly))
            {
                this.Messaging.Write(ErrorMessages.ReadOnlyOutputFile(this.Context.InputFilePath));
                return shouldCommit;
            }

            using (var database = new Database(this.Context.InputFilePath, OpenDatabase.Transact))
            {
                // Just use the English codepage, because the tables we're importing only have binary streams / MSI identifiers / other non-localizable content
                var codepage = 1252;

                // list of certificates for this database (hash/identifier)
                var certificates = new Dictionary<string, string>();

                // Reset the in-memory tables for this new database
                var digitalSignatureTable = new Table(this.TableDefinitions["MsiDigitalSignature"]);
                var digitalCertificateTable = new Table(this.TableDefinitions["MsiDigitalCertificate"]);

                // If any digital signature records exist that are not of the media type, preserve them
                if (database.TableExists("MsiDigitalSignature"))
                {
                    using (var digitalSignatureView = database.OpenExecuteView("SELECT `Table`, `SignObject`, `DigitalCertificate_`, `Hash` FROM `MsiDigitalSignature` WHERE `Table` <> 'Media'"))
                    {
                        foreach (var digitalSignatureRecord in digitalSignatureView.Records)
                        {
                            Row digitalSignatureRow = null;
                            digitalSignatureRow = digitalSignatureTable.CreateRow(null);

                            var table = digitalSignatureRecord.GetString(0);
                            var signObject = digitalSignatureRecord.GetString(1);

                            digitalSignatureRow[0] = table;
                            digitalSignatureRow[1] = signObject;
                            digitalSignatureRow[2] = digitalSignatureRecord.GetString(2);

                            if (false == digitalSignatureRecord.IsNull(3))
                            {
                                // Export to a file, because the MSI API's require us to provide a file path on disk
                                var hashPath = Path.Combine(this.Context.IntermediateFolder, "MsiDigitalSignature");
                                var hashFileName = String.Concat(table, ".", signObject, ".bin");

                                Directory.CreateDirectory(hashPath);
                                hashPath = Path.Combine(hashPath, hashFileName);

                                using (var fs = File.Create(hashPath))
                                {
                                    int bytesRead;
                                    var buffer = new byte[1024 * 4];

                                    while (0 != (bytesRead = digitalSignatureRecord.GetStream(3, buffer, buffer.Length)))
                                    {
                                        fs.Write(buffer, 0, bytesRead);
                                    }
                                }

                                digitalSignatureRow[3] = hashFileName;
                            }
                        }
                    }
                }

                // If any digital certificates exist, extract and preserve them
                if (database.TableExists("MsiDigitalCertificate"))
                {
                    using (var digitalCertificateView = database.OpenExecuteView("SELECT * FROM `MsiDigitalCertificate`"))
                    {
                        foreach (var digitalCertificateRecord in digitalCertificateView.Records)
                        {
                            var certificateId = digitalCertificateRecord.GetString(1); // get the identifier of the certificate

                            // Export to a file, because the MSI API's require us to provide a file path on disk
                            var certPath = Path.Combine(this.Context.IntermediateFolder, "MsiDigitalCertificate");
                            Directory.CreateDirectory(certPath);
                            certPath = Path.Combine(certPath, String.Concat(certificateId, ".cer"));

                            using (var fs = File.Create(certPath))
                            {
                                int bytesRead;
                                var buffer = new byte[1024 * 4];

                                while (0 != (bytesRead = digitalCertificateRecord.GetStream(2, buffer, buffer.Length)))
                                {
                                    fs.Write(buffer, 0, bytesRead);
                                }
                            }

                            // Add it to our "add to MsiDigitalCertificate" table dictionary
                            var digitalCertificateRow = digitalCertificateTable.CreateRow(null);
                            digitalCertificateRow[0] = certificateId;

                            // Now set the file path on disk where this binary stream will be picked up at import time
                            digitalCertificateRow[1] = String.Concat(certificateId, ".cer");

                            // Load the cert to get it's thumbprint
                            var cert = X509Certificate.CreateFromCertFile(certPath);
                            var cert2 = new X509Certificate2(cert);

                            certificates.Add(cert2.Thumbprint, certificateId);
                        }
                    }
                }

                using (var mediaView = database.OpenExecuteView("SELECT * FROM `Media`"))
                {
                    foreach (var mediaRecord in mediaView.Records)
                    {
                        X509Certificate2 cert2 = null;
                        Row digitalSignatureRow = null;

                        var cabName = mediaRecord.GetString(4); // get the name of the cab
                                                                   // If there is no cabinet or it's an internal cab, skip it.
                        if (String.IsNullOrEmpty(cabName) || cabName.StartsWith("#", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var cabId = mediaRecord.GetString(1); // get the ID of the cab
                        var cabPath = Path.Combine(Path.GetDirectoryName(this.Context.InputFilePath), cabName);

                        // If the cabs aren't there, throw an error but continue to catch the other errors
                        if (!File.Exists(cabPath))
                        {
                            this.Messaging.Write(ErrorMessages.WixFileNotFound(cabPath));
                            continue;
                        }

                        try
                        {
                            // Get the certificate from the cab
                            var signedFileCert = X509Certificate.CreateFromSignedFile(cabPath);
                            cert2 = new X509Certificate2(signedFileCert);
                        }
                        catch (System.Security.Cryptography.CryptographicException e)
                        {
                            var HResult = unchecked((uint)Marshal.GetHRForException(e));

                            // If the file has no cert, continue, but flag that we found at least one so we can later give a warning
                            if (0x80092009 == HResult) // CRYPT_E_NO_MATCH
                            {
                                foundUnsignedExternals = true;
                                continue;
                            }

                            // todo: exactly which HRESULT corresponds to this issue?
                            // If it's one of these exact platforms, warn the user that it may be due to their OS.
                            if ((5 == Environment.OSVersion.Version.Major && 2 == Environment.OSVersion.Version.Minor) || // W2K3
                                (5 == Environment.OSVersion.Version.Major && 1 == Environment.OSVersion.Version.Minor)) // XP
                            {
                                this.Messaging.Write(ErrorMessages.UnableToGetAuthenticodeCertOfFileDownlevelOS(cabPath, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", HResult)));
                            }
                            else // otherwise, generic error
                            {
                                this.Messaging.Write(ErrorMessages.UnableToGetAuthenticodeCertOfFile(cabPath, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", HResult)));
                            }
                        }

                        // If we haven't added this cert to the MsiDigitalCertificate table, set it up to be added
                        if (!certificates.ContainsKey(cert2.Thumbprint))
                        {
                            // generate a stable identifier
                            var certificateGeneratedId = Common.GenerateIdentifier("cer", cert2.Thumbprint);

                            // Add it to our "add to MsiDigitalCertificate" table dictionary
                            var digitalCertificateRow = digitalCertificateTable.CreateRow(null);
                            digitalCertificateRow[0] = certificateGeneratedId;

                            // Export to a file, because the MSI API's require us to provide a file path on disk
                            var certPath = Path.Combine(this.Context.IntermediateFolder, "MsiDigitalCertificate");
                            Directory.CreateDirectory(certPath);
                            certPath = Path.Combine(certPath, String.Concat(cert2.Thumbprint, ".cer"));
                            File.Delete(certPath);

                            using (var writer = new BinaryWriter(File.Open(certPath, FileMode.Create)))
                            {
                                writer.Write(cert2.RawData);
                                writer.Close();
                            }

                            // Now set the file path on disk where this binary stream will be picked up at import time
                            digitalCertificateRow[1] = String.Concat(cert2.Thumbprint, ".cer");

                            certificates.Add(cert2.Thumbprint, certificateGeneratedId);
                        }

                        digitalSignatureRow = digitalSignatureTable.CreateRow(null);

                        digitalSignatureRow[0] = "Media";
                        digitalSignatureRow[1] = cabId;
                        digitalSignatureRow[2] = certificates[cert2.Thumbprint];
                    }
                }

                if (digitalCertificateTable.Rows.Count > 0)
                {
                    var command = new CreateIdtFileCommand(this.Messaging, digitalCertificateTable, codepage, this.Context.IntermediateFolder, true);
                    command.Execute();

                    database.Import(command.IdtPath);
                    shouldCommit = true;
                }

                if (digitalSignatureTable.Rows.Count > 0)
                {
                    var command = new CreateIdtFileCommand(this.Messaging, digitalSignatureTable, codepage, this.Context.IntermediateFolder, true);
                    command.Execute();

                    database.Import(command.IdtPath);
                    shouldCommit = true;
                }

                // TODO: if we created the table(s), then we should add the _Validation records for them.

                certificates = null;

                // If we did find external cabs but not all of them were signed, give a warning
                if (foundUnsignedExternals)
                {
                    this.Messaging.Write(WarningMessages.ExternalCabsAreNotSigned(this.Context.InputFilePath));
                }

                if (shouldCommit)
                {
                    database.Commit();
                }
            }

            return shouldCommit;
        }
    }
}
