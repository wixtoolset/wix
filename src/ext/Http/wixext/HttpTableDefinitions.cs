// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data.WindowsInstaller;

    public static class HttpTableDefinitions
    {
        public static readonly TableDefinition WixHttpSniSslCert = new TableDefinition(
            "Wix4HttpSniSslCert",
            HttpSymbolDefinitions.WixHttpSniSslCert,
            new[]
            {
                new ColumnDefinition("WixHttpSniSslCert", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Host", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Host for the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Port for the  SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Thumbprint", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Thumbprint of the SNI SSL certificate to find.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("AppId", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional application id for the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Store", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Certificate store containing the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("HandleExisting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "The behavior when trying to install a SNI SSL certificate and it already exists."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing the component that controls the URL reservation.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpSslBinding = new TableDefinition(
            "Wix4HttpSslBinding",
            HttpSymbolDefinitions.WixHttpSslBinding,
            new[]
            {
                new ColumnDefinition("WixHttpSslBinding", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Host", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Host for the SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Port for the SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Thumbprint", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Thumbprint of the SSL certificate to find.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("AppId", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional application id for the SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Store", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Certificate store containing the SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("HandleExisting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "The behavior when trying to install a SSL certificate and it already exists."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing the component that controls the URL reservation.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpSslBindingCertificates = new TableDefinition(
            "Wix4HttpSslBindingCertificates",
            HttpSymbolDefinitions.WixHttpSslBindingCertificates,
            new[]
            {
                new ColumnDefinition("Binding_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4HttpSslBinding", keyColumn: 1, description: "The index into the SslBinding table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Certificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "Wix4HttpSslCertificate", keyColumn: 1, description: "The index into the Certificate table.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixHttpSslCertificate = new TableDefinition(
            "Wix4HttpSslCertificate",
            HttpSymbolDefinitions.WixHttpCertificate,
            new[]
            {
                new ColumnDefinition("Certificate", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyColumn: 1, description: "Identifier for the certificate in the package.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Foreign key into the Component table used to determine install state", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name to be used for the Certificate."),
                new ColumnDefinition("StoreLocation", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 1, maxValue: 2, description: "Location of the target certificate store (CurrentUser == 1, LocalMachine == 2)"),
                new ColumnDefinition("StoreName", ColumnType.String, 64, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of the target certificate store"),
                new ColumnDefinition("Attributes", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "Attributes of the certificate"),
                new ColumnDefinition("Binary_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Binary", keyColumn: 1, description: "Identifier to Binary table containing certificate.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("CertificatePath", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Property to path of certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("PFXPassword", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Hidden property to a pfx password", modularizeType: ColumnModularizeType.Property),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpSslCertificateHash = new TableDefinition(
            "Wix4HttpSslCertificateHash",
            HttpSymbolDefinitions.WixHttpCertificateHash,
            new[]
            {
                new ColumnDefinition("Certificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyColumn: 1, description: "Foreign key to certificate in Certificate table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Hash", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Base64 encoded SHA1 hash of certificate populated at run-time."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition WixHttpUrlReservation = new TableDefinition(
            "Wix4HttpUrlReservation",
            HttpSymbolDefinitions.WixHttpUrlReservation,
            new[]
            {
                new ColumnDefinition("WixHttpUrlReservation", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("HandleExisting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "The behavior when trying to install a URL reservation and it already exists."),
                new ColumnDefinition("Sddl", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Security descriptor for the URL reservation.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Url", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "URL to be reserved.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing the component that controls the URL reservation.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpUrlAce = new TableDefinition(
            "Wix4HttpUrlAce",
            HttpSymbolDefinitions.WixHttpUrlAce,
            new[]
            {
                new ColumnDefinition("WixHttpUrlAce", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("WixHttpUrlReservation_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Wix4HttpUrlReservation", keyColumn: 1, description: "Foreign key into the Wix4HttpUrlReservation table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("SecurityPrincipal", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "The security principal for this ACE.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Rights", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 1073741824, description: "The rights for this ACE."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            WixHttpSniSslCert,
            WixHttpUrlReservation,
            WixHttpUrlAce,
            WixHttpSslCertificate,
            WixHttpSslBinding,
            WixHttpSslBindingCertificates,
            WixHttpSslCertificateHash
        };
    }
}
