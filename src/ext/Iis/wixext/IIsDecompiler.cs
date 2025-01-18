// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Security;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The decompiler for the WiX Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsDecompiler : BaseWindowsInstallerDecompilerExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => IisTableDefinitions.All;

        private IParseHelper ParseHelper { get; set; }

        internal static XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/iis";
        internal static XName CertificateName => Namespace + "Certificate";
        internal static XName IIsWebSiteCertificates => Namespace + "IIsWebSiteCertificates";
        internal static XName IIsAppPool => Namespace + "IIsAppPool";
        internal static XName IIsAppPoolRecycleTime => Namespace + "RecycleTime";
        internal static XName IIsMimeMap => Namespace + "IIsMimeMap";
        internal static XName IIsProperty => Namespace + "IIsProperty";
        internal static XName IIsWebDirProperties => Namespace + "IIsWebDirProperties";
        internal static XName IIsWebAddress => Namespace + "IIsWebAddress";
        internal static XName IIsFilter => Namespace + "IIsFilter";
        internal static XName IIsHttpHeader => Namespace + "IIsHttpHeader";
        internal static XName IIsWebApplication => Namespace + "IIsWebApplication";
        internal static XName IIsWebApplicationExtension => Namespace + "IIsWebApplicationExtension";
        internal static XName IIsWebDir => Namespace + "IIsWebDir";
        internal static XName IIsWebError => Namespace + "IIsWebError";
        internal static XName IIsWebLog => Namespace + "IIsWebLog";
        internal static XName IIsWebServiceExtension => Namespace + "IIsWebServiceExtension";
        internal static XName IIsWebSite => Namespace + "IIsWebSite";
        internal static XName IIsWebVirtualDir => Namespace + "IIsWebVirtualDir";

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="context">The context for the decompilation.</param>
        /// <param name="helper">The decompiler helper reference.</param>
        public override void PreDecompile(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper helper)
        {
            base.PreDecompile(context, helper);
            this.ParseHelper = context.ServiceProvider.GetService<IParseHelper>();
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PreDecompileTables(TableIndexedCollection tables)
        {
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override bool TryDecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "Certificate":
                case "Wix4Certificate":
                    this.DecompileCertificateTable(table);
                    break;
                case "CertificateHash":
                case "Wix4CertificateHash":
                    // There is nothing to do for this table, it contains no authored data
                    // to be decompiled.
                    break;
                case "IIsWebSiteCertificates":
                case "Wix4IIsWebSiteCertificates":
                    this.DecompileIIsWebSiteCertificatesTable(table);
                    break;
                case "IIsAppPool":
                case "Wix4IIsAppPool":
                    this.DecompileIIsAppPoolTable(table);
                    break;
                case "IIsMimeMap":
                case "Wix4IIsMimeMap":
                    this.DecompileIIsMimeMapTable(table);
                    break;
                case "IIsProperty":
                case "Wix4IIsProperty":
                    this.DecompileIIsPropertyTable(table);
                    break;
                case "IIsWebDirProperties":
                case "Wix4IIsWebDirProperties":
                    this.DecompileIIsWebDirPropertiesTable(table);
                    break;
                case "IIsWebAddress":
                case "Wix4IIsWebAddress":
                    this.DecompileIIsWebAddressTable(table);
                    break;
                case "IIsFilter":
                case "Wix4IIsFilter":
                    this.DecompileIIsFilterTable(table);
                    break;
                case "IIsHttpHeader":
                case "Wix4IIsHttpHeader":
                    this.DecompileIIsHttpHeaderTable(table);
                    break;
                case "IIsWebApplication":
                case "Wix4IIsWebApplication":
                    this.DecompileIIsWebApplicationTable(table);
                    break;
                case "IIsWebApplicationExtension":
                case "Wix4IIsWebApplicationExtension":
                    throw new NotImplementedException("Decompiling Wix4IIsWebApplicationExtension table is not implemented.");
                case "IIsWebDir":
                case "Wix4IIsWebDir":
                    throw new NotImplementedException("Decompiling Wix4IIsWebDir table is not implemented.");
                case "IIsWebError":
                case "Wix4IIsWebError":
                    this.DecompileIIsWebErrorTable(table);
                    break;
                case "IIsWebLog":
                case "Wix4IIsWebLog":
                    this.DecompileIIsWebLogTable(table);
                    break;
                case "IIsWebServiceExtension":
                case "Wix4IIsWebServiceExtension":
                    this.DecompileIIsWebServiceExtensionTable(table);
                    break;
                case "IIsWebSite":
                case "Wix4IIsWebSite":
                    this.DecompileIIsWebSiteTable(table);
                    break;
                case "IIsWebVirtualDir":
                case "Wix4IIsWebVirtualDir":
                    this.DecompileIIsWebVirtualDirTable(table);
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PostDecompileTables(TableIndexedCollection tables)
        {
            this.FinalizeIIsMimeMapTable(tables);
            this.FinalizeIIsHttpHeaderTable(tables);
            this.FinalizeIIsWebApplicationTable(tables);
            this.FinalizeIIsWebErrorTable(tables);
            this.FinalizeIIsWebVirtualDirTable(tables);
            this.FinalizeIIsWebSiteCertificatesTable(tables);
            this.FinalizeWebAddressTable(tables);
        }

        /// <summary>
        /// Decompile the Certificate table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileCertificateTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var certificate = new XElement(CertificateName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2))
                    );

                switch ((int)row[3])
                {
                    case 1:
                        certificate.Add(new XAttribute("StoreLocation", "currentUser"));
                        break;
                    case 2:
                        certificate.Add(new XAttribute("StoreLocation", "localMachine"));
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                switch ((string)row[4])
                {
                    case "CA":
                        certificate.Add(new XAttribute("StoreName", "ca"));
                        break;
                    case "MY":
                        certificate.Add(new XAttribute("StoreName", "my"));
                        break;
                    case "REQUEST":
                        certificate.Add(new XAttribute("StoreName", "request"));
                        break;
                    case "Root":
                        certificate.Add(new XAttribute("StoreName", "root"));
                        break;
                    case "AddressBook":
                        certificate.Add(new XAttribute("StoreName", "otherPeople"));
                        break;
                    case "TrustedPeople":
                        certificate.Add(new XAttribute("StoreName", "trustedPeople"));
                        break;
                    case "TrustedPublisher":
                        certificate.Add(new XAttribute("StoreName", "trustedPublisher"));
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                int attribute = (int)row[5];

                if (0x1 == (attribute & 0x1))
                {
                    certificate.Add(new XAttribute("Request", "yes"));
                }

                if (0x2 == (attribute & 0x2))
                {
                    if (null != row[6])
                    {
                        // we'll validate this in the Finalizer
                        certificate.Add(new XAttribute("BinaryRef", (string)row[6]));
                    }
                    else
                    {
                        // TODO: warn about expected value in row 5/6
                    }
                }
                else if (null != row[7])
                {
                    certificate.Add(new XAttribute("CertificatePath", (string)row[7]));
                }

                if (0x4 == (attribute & 0x4))
                {
                    certificate.Add(new XAttribute("Overwrite", "yes"));
                }

                if (null != row[8])
                {
                    certificate.Add(new XAttribute("PFXPassword", (string)row[8]));
                }

                // component link to be done in FinalizeCertificateTable
            }
        }

        /// <summary>
        /// Decompile the IIsAppPool table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsAppPoolTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webAppPool = new XElement(IIsAppPool,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(1))
                    );

                switch ((int)row[3] & 0x1F)
                {
                    case 1:
                        webAppPool.Add(new XAttribute("Identity", "networkService"));
                        break;
                    case 2:
                        webAppPool.Add(new XAttribute("Identity", "localService"));
                        break;
                    case 4:
                        webAppPool.Add(new XAttribute("Identity", "localSystem"));
                        break;
                    case 8:
                        webAppPool.Add(new XAttribute("Identity", "other"));
                        break;
                    case 0x10:
                        webAppPool.Add(new XAttribute("Identity", "applicationPoolIdentity"));
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                if (null != row[4])
                {
                    webAppPool.Add(new XAttribute("User", (string)row[4]));
                }

                if (null != row[5])
                {
                    webAppPool.Add(new XAttribute("RecycleMinutes", (int)row[5]));
                }

                if (null != row[6])
                {
                    webAppPool.Add(new XAttribute("RecycleRequests", (int)row[6]));
                }

                if (null != row[7])
                {
                    string[] recycleTimeValues = ((string)row[7]).Split(',');

                    foreach (string recycleTimeValue in recycleTimeValues)
                    {
                        var recycleTime = new XElement(IIsAppPoolRecycleTime);
                        recycleTime.Add(new XAttribute("Value", recycleTimeValue));
                        webAppPool.Add(recycleTime);
                    }
                }

                if (null != row[8])
                {
                    webAppPool.Add(new XAttribute("IdleTimeout", (int)row[8]));
                }

                if (null != row[9])
                {
                    webAppPool.Add(new XAttribute("QueueLimit", (int)row[9]));
                }

                if (null != row[10])
                {
                    string[] cpuMon = ((string)row[10]).Split(',');

                    if (0 < cpuMon.Length && "0" != cpuMon[0])
                    {
                        webAppPool.Add(new XAttribute("MaxCpuUsage", Convert.ToInt32(cpuMon[0], CultureInfo.InvariantCulture)));
                    }

                    if (1 < cpuMon.Length)
                    {
                        webAppPool.Add(new XAttribute("RefreshCpu", Convert.ToInt32(cpuMon[1], CultureInfo.InvariantCulture)));
                    }

                    if (2 < cpuMon.Length)
                    {
                        switch (Convert.ToInt32(cpuMon[2], CultureInfo.InvariantCulture))
                        {
                            case 0:
                                webAppPool.Add(new XAttribute("CpuAction", "none"));
                                break;
                            case 1:
                                webAppPool.Add(new XAttribute("CpuAction", "shutdown"));
                                break;
                            default:
                                // TODO: warn
                                break;
                        }
                    }

                    if (3 < cpuMon.Length)
                    {
                        // TODO: warn
                    }
                }

                if (null != row[11])
                {
                    webAppPool.Add(new XAttribute("MaxWorkerProcesses", (int)row[11]));
                }

                if (null != row[12])
                {
                    webAppPool.Add(new XAttribute("VirtualMemory", (int)row[12]));
                }

                if (null != row[13])
                {
                    webAppPool.Add(new XAttribute("PrivateMemory", (int)row[13]));
                }

                if (null != row[14])
                {
                    webAppPool.Add(new XAttribute("ManagedRuntimeVersion", (string)row[14]));
                }

                if (null != row[15])
                {
                    webAppPool.Add(new XAttribute("ManagedPipelineMode", (string)row[15]));
                }

                if (null != row[2])
                {
                    var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[2]);

                    if (null != component)
                    {
                        component.Add(webAppPool);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
                else
                {
                    this.DecompilerHelper.AddElementToRoot(webAppPool);
                }
            }
        }

        /// <summary>
        /// Decompile the IIsProperty table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsPropertyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webProperty = new XElement(IIsProperty,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(1))
                    );

                switch ((string)row[0])
                {
                    case "ETagChangeNumber":
                        webProperty.Add(new XAttribute("Id", "ETagChangeNumber"));
                        break;
                    case "IIs5IsolationMode":
                        webProperty.Add(new XAttribute("Id", "IIs5IsolationMode"));
                        break;
                    case "LogInUTF8":
                        webProperty.Add(new XAttribute("Id", "LogInUTF8"));
                        break;
                    case "MaxGlobalBandwidth":
                        webProperty.Add(new XAttribute("Id", "MaxGlobalBandwidth"));
                        break;
                }

                if (0 != (int)row[2])
                {
                    // TODO: warn about value in unused column
                }

                if (null != row[3])
                {
                    webProperty.Add(new XAttribute("Value", (string)row[3]));
                }

                var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.Add(webProperty);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IIsHttpHeader table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsHttpHeaderTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var httpHeader = new XElement(IIsHttpHeader,
                    new XAttribute("Name", (string)row[3]),
                    new XAttribute("Value", (string)row[4])
                    );
                // the ParentType and Parent columns are handled in FinalizeIIsHttpHeaderTable

                this.DecompilerHelper.IndexElement(row, httpHeader);
            }
        }

        /// <summary>
        /// Decompile the IIsMimeMap table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsMimeMapTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var mimeMap = new XElement(IIsMimeMap,
                    new XAttribute("Id", (string)row[0]),
                    new XAttribute("Type", (string)row[3]),
                    new XAttribute("Extension", (string)row[4])
                    );

                // the ParentType and ParentValue columns are handled in FinalizeIIsMimeMapTable

                this.DecompilerHelper.IndexElement(row, mimeMap);
            }
        }

        /// <summary>
        /// Decompile the IIsWebAddress table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebAddressTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webAddress = new XElement(IIsWebAddress,
                    new XAttribute("Id", (string)row[0])
                    );

                if (null != row[2])
                {
                    webAddress.Add(new XAttribute("IP", (string)row[2]));
                }

                webAddress.Add(new XAttribute("Port", (string)row[3]));

                if (null != row[4])
                {
                    webAddress.Add(new XAttribute("Header", (string)row[4]));
                }

                if (null != row[5] && 1 == (int)row[5])
                {
                    webAddress.Add(new XAttribute("Secure", "yes"));
                }

                this.DecompilerHelper.IndexElement(row, webAddress);
            }
        }

        /// <summary>
        /// Decompile the IIsWebApplication table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebApplicationTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webApplication = new XElement(IIsWebApplication,
                    new XAttribute("Id", (string)row[0]),
                    new XAttribute("Name", (string)row[1])
                    );

                // these are not listed incorrectly - the order is low, high, medium
                switch ((int)row[2])
                {
                    case 0:
                        webApplication.Add(new XAttribute("Isolation", "low"));
                        break;
                    case 1:
                        webApplication.Add(new XAttribute("Isolation", "high"));
                        break;
                    case 2:
                        webApplication.Add(new XAttribute("Isolation", "medium"));
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                if (null != row[3])
                {
                    switch ((int)row[3])
                    {
                        case 0:
                            webApplication.Add(new XAttribute("AllowSessions", "no"));
                            break;
                        case 1:
                            webApplication.Add(new XAttribute("AllowSessions", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[4])
                {
                    webApplication.Add(new XAttribute("SessionTimeout", (int)row[4]));
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            webApplication.Add(new XAttribute("Buffer", "no"));
                            break;
                        case 1:
                            webApplication.Add(new XAttribute("Buffer", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    switch ((int)row[6])
                    {
                        case 0:
                            webApplication.Add(new XAttribute("ParentPaths", "no"));
                            break;
                        case 1:
                            webApplication.Add(new XAttribute("ParentPaths", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[7])
                {
                    switch ((string)row[7])
                    {
                        case "JScript":
                            webApplication.Add(new XAttribute("DefaultScript", "JScript"));
                            break;
                        case "VBScript":
                            webApplication.Add(new XAttribute("DefaultScript", "VBScript"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[8])
                {
                    webApplication.Add(new XAttribute("ScriptTimeout", (int)row[8]));
                }

                if (null != row[9])
                {
                    switch ((int)row[9])
                    {
                        case 0:
                            webApplication.Add(new XAttribute("ServerDebugging", "no"));
                            break;
                        case 1:
                            webApplication.Add(new XAttribute("ServerDebugging", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[10])
                {
                    switch ((int)row[10])
                    {
                        case 0:
                            webApplication.Add(new XAttribute("ClientDebugging", "no"));
                            break;
                        case 1:
                            webApplication.Add(new XAttribute("ClientDebugging", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[11])
                {
                    webApplication.Add(new XAttribute("WebAppPool", (string)row[11]));
                }

                this.DecompilerHelper.IndexElement(row, webApplication);
            }
        }

        /// <summary>
        /// Decompile the IIsWebDirProperties table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebDirPropertiesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webDirProperties = new XElement(IIsWebDirProperties,
                    new XAttribute("Id", (string)row[0])
                    );

                if (null != row[1])
                {
                    int access = (int)row[1];

                    if (0x1 == (access & 0x1))
                    {
                        webDirProperties.Add(new XAttribute("Read", "yes"));
                    }

                    if (0x2 == (access & 0x2))
                    {
                        webDirProperties.Add(new XAttribute("Write", "yes"));
                    }

                    if (0x4 == (access & 0x4))
                    {
                        webDirProperties.Add(new XAttribute("Execute", "yes"));
                    }

                    if (0x200 == (access & 0x200))
                    {
                        webDirProperties.Add(new XAttribute("Script", "yes"));
                    }
                }

                if (null != row[2])
                {
                    int authorization = (int)row[2];

                    if (0x1 == (authorization & 0x1))
                    {
                        webDirProperties.Add(new XAttribute("AnonymousAccess", "yes"));
                    }
                    else // set one of the properties to 'no' to force the output value to be '0' if not other attributes are set
                    {
                        webDirProperties.Add(new XAttribute("AnonymousAccess", "no"));
                    }

                    if (0x2 == (authorization & 0x2))
                    {
                        webDirProperties.Add(new XAttribute("BasicAuthentication", "yes"));
                    }

                    if (0x4 == (authorization & 0x4))
                    {
                        webDirProperties.Add(new XAttribute("WindowsAuthentication", "yes"));
                    }

                    if (0x10 == (authorization & 0x10))
                    {
                        webDirProperties.Add(new XAttribute("DigestAuthentication", "yes"));
                    }

                    if (0x40 == (authorization & 0x40))
                    {
                        webDirProperties.Add(new XAttribute("PassportAuthentication", "yes"));
                    }
                }

                if (null != row[3])
                {
                    webDirProperties.Add(new XAttribute("AnonymousUser", (string)row[3]));
                }

                if (null != row[4] && 1 == (int)row[4])
                {
                    webDirProperties.Add(new XAttribute("IIsControlledPassword", "yes"));
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            webDirProperties.Add(new XAttribute("LogVisits", "no"));
                            break;
                        case 1:
                            webDirProperties.Add(new XAttribute("LogVisits", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    switch ((int)row[6])
                    {
                        case 0:
                            webDirProperties.Add(new XAttribute("Index", "no"));
                            break;
                        case 1:
                            webDirProperties.Add(new XAttribute("Index", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[7])
                {
                    webDirProperties.Add(new XAttribute("DefaultDocuments", (string)row[7]));
                }

                if (null != row[8])
                {
                    switch ((int)row[8])
                    {
                        case 0:
                            webDirProperties.Add(new XAttribute("AspDetailedError", "no"));
                            break;
                        case 1:
                            webDirProperties.Add(new XAttribute("AspDetailedError", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[9])
                {
                    webDirProperties.Add(new XAttribute("HttpExpires", (string)row[9]));
                }

                if (null != row[10])
                {
                    // force the value to be a positive number
                    webDirProperties.Add(new XAttribute("CacheControlMaxAge", unchecked((uint)(int)row[10])));
                }

                if (null != row[11])
                {
                    webDirProperties.Add(new XAttribute("CacheControlCustom", (string)row[11]));
                }

                if (null != row[12])
                {
                    switch ((int)row[8])
                    {
                        case 0:
                            webDirProperties.Add(new XAttribute("ClearCustomError", "no"));
                            break;
                        case 1:
                            webDirProperties.Add(new XAttribute("ClearCustomError", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[13])
                {
                    int accessSSLFlags = (int)row[13];

                    if (0x8 == (accessSSLFlags & 0x8))
                    {
                        webDirProperties.Add(new XAttribute("AccessSSL", "yes"));
                    }

                    if (0x20 == (accessSSLFlags & 0x20))
                    {
                        webDirProperties.Add(new XAttribute("AccessSSLNegotiateCert", "yes"));
                    }

                    if (0x40 == (accessSSLFlags & 0x40))
                    {
                        webDirProperties.Add(new XAttribute("AccessSSLRequireCert", "yes"));
                    }

                    if (0x80 == (accessSSLFlags & 0x80))
                    {
                        webDirProperties.Add(new XAttribute("AccessSSLMapCert", "yes"));
                    }

                    if (0x100 == (accessSSLFlags & 0x100))
                    {
                        webDirProperties.Add(new XAttribute("AccessSSL128", "yes"));
                    }
                }

                if (null != row[14])
                {
                    webDirProperties.Add(new XAttribute("AuthenticationProviders", (string)row[14]));
                }

                this.DecompilerHelper.AddElementToRoot(webDirProperties);
            }
        }

        /// <summary>
        /// Decompile the IIsWebError table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebErrorTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webError = new XElement(IIsWebError,
                    new XAttribute("ErrorCode", (string)row[0]),
                    new XAttribute("SubCode", (string)row[1])
                    );

                // the ParentType and ParentValue columns are handled in FinalizeIIsWebErrorTable

                if (null != row[4])
                {
                    webError.Add(new XAttribute("File", (string)row[4]));
                }

                if (null != row[5])
                {
                    webError.Add(new XAttribute("URL", (string)row[5]));
                }

                this.DecompilerHelper.IndexElement(row, webError);
            }
        }

        /// <summary>
        /// Decompile the IIsFilter table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsFilterTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webFilter = new XElement(IIsFilter,
                    new XAttribute("Id", (string)row[0]),
                    new XAttribute("Name", (string)row[1])
                    );

                if (null != row[3])
                {
                    webFilter.Add(new XAttribute("Path", (string)row[3]));
                }

                if (null != row[5])
                {
                    webFilter.Add(new XAttribute("Description", (string)row[5]));
                }

                webFilter.Add(new XAttribute("Flags", (int)row[6]));

                if (null != row[7])
                {
                    switch ((int)row[7])
                    {
                        case (-1):
                            webFilter.Add(new XAttribute("LoadOrder", "last"));
                            break;
                        case 0:
                            webFilter.Add(new XAttribute("LoadOrder", "first"));
                            break;
                        default:
                            webFilter.Add(new XAttribute("LoadOrder", Convert.ToString((int)row[7], CultureInfo.InvariantCulture)));
                            break;
                    }
                }

                if (null != row[4])
                {
                    var webSite = this.DecompilerHelper.GetIndexedElement("IIsWebSite", (string)row[4]);

                    if (null != webSite)
                    {
                        webSite.Add(webFilter);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Web_", (string)row[4], "IIsWebSite"));
                    }
                }
                else // Component parent
                {
                    var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[2]);

                    if (null != component)
                    {
                        component.Add(webFilter);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the IIsWebLog table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebLogTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webLog = new XElement(IIsWebLog,
                    new XAttribute("Id", (string)row[0])
                    );

                switch ((string)row[1])
                {
                    case "Microsoft IIS Log File Format":
                        webLog.Add(new XAttribute("Type", "IIS"));
                        break;
                    case "NCSA Common Log File Format":
                        webLog.Add(new XAttribute("Type", "NCSA"));
                        break;
                    case "none":
                        webLog.Add(new XAttribute("Type", "none"));
                        break;
                    case "ODBC Logging":
                        webLog.Add(new XAttribute("Type", "ODBC"));
                        break;
                    case "W3C Extended Log File Format":
                        webLog.Add(new XAttribute("Type", "W3C"));
                        break;
                    default:
                        // TODO: warn
                        break;
                }

                this.DecompilerHelper.AddElementToRoot(webLog);
            }
        }

        /// <summary>
        /// Decompile the IIsWebServiceExtension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebServiceExtensionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webServiceExtension = new XElement(IIsWebServiceExtension,
                    new XAttribute("Id", (string)row[0]),
                    new XAttribute("File", (string)row[2])
                    );

                if (null != row[3])
                {
                    webServiceExtension.Add(new XAttribute("Description", (string)row[3]));
                }

                if (null != row[4])
                {
                    webServiceExtension.Add(new XAttribute("Group", (string)row[4]));
                }

                webServiceExtension.Add(new XAttribute("Group", (string)row[4]));
                int attributes = (int)row[5];

                if (0x1 == (attributes & 0x1))
                {
                    webServiceExtension.Add(new XAttribute("Allow", "yes"));
                }
                else
                {
                    webServiceExtension.Add(new XAttribute("Allow", "no"));
                }

                if (0x2 == (attributes & 0x2))
                {
                    webServiceExtension.Add(new XAttribute("UIDeletable", "yes"));
                }

                var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.Add(webServiceExtension);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the IIsWebSite table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebSiteTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webSite = new XElement(IIsWebSite,
                    new XAttribute("Id", (string)row[0])
                    );

                if (null != row[2])
                {
                    webSite.Add(new XAttribute("Description", (string)row[2]));
                }

                if (null != row[3])
                {
                    webSite.Add(new XAttribute("ConnectionTimeout", (int)row[3]));
                }

                if (null != row[4])
                {
                    webSite.Add(new XAttribute("Directory", (string)row[4]));
                }

                if (null != row[5])
                {
                    switch ((int)row[5])
                    {
                        case 0:
                            // this is the default
                            break;
                        case 1:
                            webSite.Add(new XAttribute("StartOnInstall", "yes"));
                            break;
                        case 2:
                            webSite.Add(new XAttribute("AutoStart", "yes"));
                            break;
                        default:
                            // TODO: warn
                            break;
                    }
                }

                if (null != row[6])
                {
                    int attributes = (int)row[6];

                    if (0x2 == (attributes & 0x2))
                    {
                        webSite.Add(new XAttribute("ConfigureIfExists", "no"));
                    }
                }

                // the KeyAddress_ column is handled in FinalizeWebAddressTable

                if (null != row[8])
                {
                    webSite.Add(new XAttribute("DirProperties", (string)row[8]));
                }

                // the Application_ column is handled in FinalizeIIsWebApplicationTable

                if (null != row[10])
                {
                    if (-1 != (int)row[10])
                    {
                        webSite.Add(new XAttribute("Sequence", (int)row[10]));
                    }
                }

                if (null != row[11])
                {
                    webSite.Add(new XAttribute("WebLog", (string)row[11]));
                }

                if (null != row[12])
                {
                    webSite.Add(new XAttribute("SiteId", (string)row[12]));
                }

                if (null != row[1])
                {
                    var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[1]);

                    if (null != component)
                    {
                        component.Add(webSite);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                    }
                }
                else
                {
                    this.DecompilerHelper.AddElementToRoot(webSite);
                }
                this.DecompilerHelper.IndexElement(row, webSite);
            }
        }

        /// <summary>
        /// Decompile the IIsWebVirtualDir table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebVirtualDirTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var webVirtualDir = new XElement(IIsWebVirtualDir,
                    new XAttribute("Id", (string)row[0])
                    );

                // the Component_ and Web_ columns are handled in FinalizeIIsWebVirtualDirTable

                webVirtualDir.Add(new XAttribute("Alias", (string)row[3]));

                webVirtualDir.Add(new XAttribute("Directory", (string)row[4]));

                if (null != row[5])
                {
                    webVirtualDir.Add(new XAttribute("DirProperties", (string)row[5]));
                }

                // the Application_ column is handled in FinalizeIIsWebApplicationTable

                this.DecompilerHelper.IndexElement(row, webVirtualDir);
            }
        }

        /// <summary>
        /// Decompile the IIsWebSiteCertificates table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileIIsWebSiteCertificatesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var certificateRef = new XElement(IIsWebSiteCertificates,
                    new XAttribute("Id", (string)row[1])
                    );

                this.DecompilerHelper.IndexElement(row, certificateRef);
            }
        }

        /// <summary>
        /// Finalize the IIsHttpHeader table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The IIsHttpHeader table supports multiple parent types so no foreign key
        /// is declared and thus nesting must be done late.
        /// </remarks>
        private void FinalizeIIsHttpHeaderTable(TableIndexedCollection tables)
        {
            Table iisHttpHeaderTable;
            if (tables.TryGetTable("IIsHttpHeader", out iisHttpHeaderTable)
                || tables.TryGetTable("Wix4IIsHttpHeader", out iisHttpHeaderTable))
            {
                foreach (Row row in iisHttpHeaderTable.Rows)
                {
                    var httpHeader = this.DecompilerHelper.GetIndexedElement(row);

                    if (1 == (int)row[1])
                    {
                        var webVirtualDir = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebVirtualDir", (string)row[2]);
                        if (null != webVirtualDir)
                        {
                            webVirtualDir.Add(httpHeader);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisHttpHeaderTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebVirtualDir"));
                        }
                    }
                    else if (2 == (int)row[1])
                    {
                        var webSite = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebSite", (string)row[2]);
                        if (null != webSite)
                        {
                            webSite.Add(httpHeader);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisHttpHeaderTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebSite"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsMimeMap table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The IIsMimeMap table supports multiple parent types so no foreign key
        /// is declared and thus nesting must be done late.
        /// </remarks>
        private void FinalizeIIsMimeMapTable(TableIndexedCollection tables)
        {
            Table iisMimeMapTable;
            if (tables.TryGetTable("IIsMimeMap", out iisMimeMapTable)
                || tables.TryGetTable("Wix4IIsMimeMap", out iisMimeMapTable))
            {
                foreach (Row row in iisMimeMapTable.Rows)
                {
                    var mimeMap = this.DecompilerHelper.GetIndexedElement(row);

                    if (2 < (int)row[1] || 0 >= (int)row[1])
                    {
                        // TODO: warn about unknown parent type
                    }

                    var webVirtualDir = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebVirtualDir", (string)row[2]);
                    var webSite = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebSite", (string)row[2]);
                    if (null != webVirtualDir)
                    {
                        webVirtualDir.Add(mimeMap);
                    }
                    else if (null != webSite)
                    {
                        webSite.Add(mimeMap);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisMimeMapTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ParentValue", (string)row[2], "IIsWebVirtualDir"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebApplication table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since WebApplication elements may nest under a specific WebSite or
        /// WebVirtualDir (or just the root element), the nesting must be done late.
        /// </remarks>
        private void FinalizeIIsWebApplicationTable(TableIndexedCollection tables)
        {
            Table iisWebApplicationTable;
            _ = tables.TryGetTable("IIsWebApplication", out iisWebApplicationTable)
                || tables.TryGetTable("Wix4IIsWebApplication", out iisWebApplicationTable);

            Table iisWebVirtualDirTable;
            _ = tables.TryGetTable("IIsWebVirtualDir", out iisWebVirtualDirTable)
                || tables.TryGetTable("Wix4IIsWebVirtualDir", out iisWebVirtualDirTable);

            Hashtable addedWebApplications = new Hashtable();

            Table iisWebSiteTable;
            if (tables.TryGetTable("IIsWebSite", out iisWebSiteTable)
                || tables.TryGetTable("Wix4IIsWebSite", out iisWebSiteTable))
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    if (null != row[9])
                    {
                        var webSite = this.DecompilerHelper.GetIndexedElement(row);

                        var webApplication = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebApplication", (string)row[9]);
                        if (null != webApplication)
                        {
                            webSite.Add(webApplication);
                            addedWebApplications[webApplication] = null;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebSiteTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Application_", (string)row[9], "IIsWebApplication"));
                        }
                    }
                }
            }

            if (null != iisWebVirtualDirTable)
            {
                foreach (Row row in iisWebVirtualDirTable.Rows)
                {
                    if (null != row[6])
                    {
                        var webVirtualDir = this.DecompilerHelper.GetIndexedElement(row);

                        var webApplication = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebApplication", (string)row[6]);
                        if (null != webApplication)
                        {
                            webVirtualDir.Add(webApplication);
                            addedWebApplications[webApplication] = null;
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Application_", (string)row[6], "IIsWebApplication"));
                        }
                    }
                }
            }

            if (null != iisWebApplicationTable)
            {
                foreach (Row row in iisWebApplicationTable.Rows)
                {
                    var webApplication = this.DecompilerHelper.GetIndexedElement(row);

                    if (!addedWebApplications.Contains(webApplication))
                    {
                        this.DecompilerHelper.AddElementToRoot(webApplication);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebError table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since there is no foreign key relationship declared for this table
        /// (because it takes various parent types), it must be nested late.
        /// </remarks>
        private void FinalizeIIsWebErrorTable(TableIndexedCollection tables)
        {
            Table iisWebErrorTable;
            if (tables.TryGetTable("IIsWebError", out iisWebErrorTable)
                || tables.TryGetTable("Wix4IIsWebError", out iisWebErrorTable))
            {
                foreach (Row row in iisWebErrorTable.Rows)
                {
                    var webError = this.DecompilerHelper.GetIndexedElement(row);

                    if (1 == (int)row[2]) // WebVirtualDir parent
                    {
                        var webVirtualDir = this.DecompilerHelper.GetIndexedElement("IIsWebVirtualDir", (string)row[3]);

                        if (null != webVirtualDir)
                        {
                            webVirtualDir.Add(webError);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebErrorTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ParentValue", (string)row[3], "IIsWebVirtualDir"));
                        }
                    }
                    else if (2 == (int)row[2]) // WebSite parent
                    {
                        var webSite = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebSite", (string)row[3]);

                        if (null != webSite)
                        {
                            webSite.Add(webError);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebErrorTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "ParentValue", (string)row[3], "IIsWebSite"));
                        }
                    }
                    else
                    {
                        // TODO: warn unknown parent type
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebVirtualDir table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// WebVirtualDir elements nest under either a WebSite or component
        /// depending upon whether the component in the IIsWebVirtualDir row
        /// is the same as the one in the parent IIsWebSite row.
        /// </remarks>
        private void FinalizeIIsWebVirtualDirTable(TableIndexedCollection tables)
        {
            Hashtable iisWebSiteRows = new Hashtable();

            // index the IIsWebSite rows by their primary keys
            Table iisWebSiteTable;
            if (tables.TryGetTable("IIsWebSite", out iisWebSiteTable)
                || tables.TryGetTable("Wix4IIsWebSite", out iisWebSiteTable))
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    iisWebSiteRows.Add(row[0], row);
                }
            }

            Table iisWebVirtualDirTable;
            if (tables.TryGetTable("IIsWebVirtualDir", out iisWebVirtualDirTable)
                || tables.TryGetTable("Wix4IIsWebVirtualDir", out iisWebVirtualDirTable))
            {
                foreach (Row row in iisWebVirtualDirTable.Rows)
                {
                    var webVirtualDir = this.DecompilerHelper.GetIndexedElement(row);
                    Row iisWebSiteRow = (Row)iisWebSiteRows[row[2]];

                    if (null != iisWebSiteRow)
                    {
                        if ((string)iisWebSiteRow[1] == (string)row[1])
                        {
                            var webSite = this.DecompilerHelper.GetIndexedElement(iisWebSiteRow);

                            webSite.Add(webVirtualDir);
                        }
                        else
                        {
                            var component = this.DecompilerHelper.GetIndexedElement("Component", (string)row[1]);

                            if (null != component)
                            {
                                webVirtualDir.Add(new XAttribute("WebSite", (string)row[2]));
                                component.Add(webVirtualDir);
                            }
                            else
                            {
                                this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                            }
                        }
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebVirtualDirTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Web_", (string)row[2], "IIsWebSite"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the IIsWebSiteCertificates table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// This table creates CertificateRef elements which nest under WebSite
        /// elements.
        /// </remarks>
        private void FinalizeIIsWebSiteCertificatesTable(TableIndexedCollection tables)
        {
            Table IIsWebSiteCertificatesTable;
            if (tables.TryGetTable("IIsWebSiteCertificates", out IIsWebSiteCertificatesTable)
                || tables.TryGetTable("Wix4IIsWebSiteCertificates", out IIsWebSiteCertificatesTable))
            {
                foreach (Row row in IIsWebSiteCertificatesTable.Rows)
                {
                    var certificateRef = this.DecompilerHelper.GetIndexedElement(row);
                    var webSite = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebSite", (string)row[0]);

                    if (null != webSite)
                    {
                        webSite.Add(certificateRef);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, IIsWebSiteCertificatesTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Web_", (string)row[0], "IIsWebSite"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the WebAddress table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// There is a circular dependency between the WebAddress and WebSite
        /// tables, so nesting must be handled here.
        /// </remarks>
        private void FinalizeWebAddressTable(TableIndexedCollection tables)
        {
            Hashtable addedWebAddresses = new Hashtable();

            Table iisWebSiteTable;
            if (tables.TryGetTable("IIsWebSite", out iisWebSiteTable)
                || tables.TryGetTable("Wix4IIsWebSite", out iisWebSiteTable))
            {
                foreach (Row row in iisWebSiteTable.Rows)
                {
                    var webSite = this.DecompilerHelper.GetIndexedElement(row);

                    var webAddress = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebAddress", (string)row[7]);
                    if (null != webAddress)
                    {
                        webSite.Add(webAddress);
                        addedWebAddresses[webAddress] = null;
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebSiteTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "KeyAddress_", (string)row[7], "IIsWebAddress"));
                    }
                }
            }

            Table iisWebAddressTable;
            if (tables.TryGetTable("IIsWebAddress", out iisWebAddressTable)
                || tables.TryGetTable("Wix4IIsWebAddress", out iisWebAddressTable))
            {
                foreach (Row row in iisWebAddressTable.Rows)
                {
                    var webAddress = this.DecompilerHelper.GetIndexedElement(row);

                    if (!addedWebAddresses.Contains(webAddress))
                    {
                        var webSite = this.DecompilerHelper.GetIndexedElement("Wix4IIsWebSite", (string)row[1]);

                        if (null != webSite)
                        {
                            webSite.Add(webAddress);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, iisWebAddressTable.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Web_", (string)row[1], "IIsWebSite"));
                        }
                    }
                }
            }
        }
    }
}
