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
                new ColumnDefinition("Wix4HttpSniSslCert", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Host", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Host for the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Port for the  SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Thumbprint", ColumnType.String, 0, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "humbprint of the SNI SSL certificate to find.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("AppId", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional application id for the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Store", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional application id for the SNI SSL certificate.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("HandleExisting", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2, description: "The behavior when trying to install a SNI SSL certificate and it already exists."),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key into the Component table referencing the component that controls the URL reservation.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition WixHttpUrlReservation = new TableDefinition(
            "Wix4HttpUrlReservation",
            HttpSymbolDefinitions.WixHttpUrlReservation,
            new[]
            {
                new ColumnDefinition("Wix4HttpUrlReservation", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
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
                new ColumnDefinition("Wix4HttpUrlAce", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "The non-localized primary key for the table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("WixHttpUrlReservation_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "WixHttpUrlReservation", keyColumn: 1, description: "Foreign key into the WixHttpUrlReservation table.", modularizeType: ColumnModularizeType.Column),
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
        };
    }
}
