// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data.WindowsInstaller;

    public static class IisTableDefinitions
    {
        public static readonly TableDefinition Certificate = new TableDefinition(
            "Certificate",
            IisSymbolDefinitions.Certificate,
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

        public static readonly TableDefinition CertificateHash = new TableDefinition(
            "CertificateHash",
            IisSymbolDefinitions.CertificateHash,
            new[]
            {
                new ColumnDefinition("Certificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyColumn: 1, description: "Foreign key to certificate in Certificate table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Hash", ColumnType.String, 0, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Base64 encoded SHA1 hash of certificate populated at run-time."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IIsWebSiteCertificates = new TableDefinition(
            "IIsWebSiteCertificates",
            IisSymbolDefinitions.IIsWebSiteCertificates,
            new[]
            {
                new ColumnDefinition("Web_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebSite", keyColumn: 1, description: "The index into the IIsWebSite table.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Certificate_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Text, keyTable: "Certificate", keyColumn: 1, description: "The index into the Certificate table.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IIsAppPool = new TableDefinition(
            "IIsAppPool",
            IisSymbolDefinitions.IIsAppPool,
            new[]
            {
                new ColumnDefinition("AppPool", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token for apppool", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name to be used for the IIs AppPool.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the app pool", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Attributes of the AppPool"),
                new ColumnDefinition("User_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "User", keyColumn: 1, description: "User account to run the app pool as", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("RecycleMinutes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Number of minutes between recycling app pool"),
                new ColumnDefinition("RecycleRequests", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Number of requests between recycling app pool"),
                new ColumnDefinition("RecycleTimes", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Times to recycle app pool (comma delimited - i.e. 1:45,13:30)"),
                new ColumnDefinition("IdleTimeout", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Amount of idle time before shutting down"),
                new ColumnDefinition("QueueLimit", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Reject requests after queue gets how large"),
                new ColumnDefinition("CPUMon", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "CPUMon is a comma delimeted list of the following format: <percent CPU usage>,<refress minutes>,<Action>. The values for Action are 1 (Shutdown) and 0 (No Action)."),
                new ColumnDefinition("MaxProc", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Maximum number of processes to use"),
                new ColumnDefinition("VirtualMemory", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Amount of virtual memory (in KB) that a worker process can use before the worker process recycles. The maximum value supported for this field is 4,294,967 KB."),
                new ColumnDefinition("PrivateMemory", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Amount of private memory (in KB) that a worker process can use before the worker process recycles. The maximum value supported for this field is 4,294,967 KB."),
                new ColumnDefinition("ManagedRuntimeVersion", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Specifies the .NET Framework version to be used by the application pool."),
                new ColumnDefinition("ManagedPipelineMode", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Specifies the request-processing mode that is used to process requests for managed content."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsMimeMap = new TableDefinition(
            "IIsMimeMap",
            IisSymbolDefinitions.IIsMimeMap,
            new[]
            {
                new ColumnDefinition("MimeMap", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token for Mime Map definitions", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ParentType", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "1;2", description: "Type of parent: 1=vdir 2=website"),
                new ColumnDefinition("ParentValue", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, description: "Name of the parent value.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("MimeType", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Mime-type covered by the MimeMap."),
                new ColumnDefinition("Extension", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Extension covered by the MimeMap."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsProperty = new TableDefinition(
            "IIsProperty",
            IisSymbolDefinitions.IIsProperty,
            new[]
            {
                new ColumnDefinition("Property", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Unique name of the IIsProperty"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Component that the property is linked to", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Attributes of the IIsProperty (unused)"),
                new ColumnDefinition("Value", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Value of the IIsProperty"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebDirProperties = new TableDefinition(
            "IIsWebDirProperties",
            IisSymbolDefinitions.IIsWebDirProperties,
            new[]
            {
                new ColumnDefinition("DirProperties", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token for Web Properties", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Access", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Access rights to the web server"),
                new ColumnDefinition("Authorization", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Authorization policy to web server (anonymous access, NTLM, etc)"),
                new ColumnDefinition("AnonymousUser_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "User", keyColumn: 1, description: "Foreign key, User used to log into database", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("IIsControlledPassword", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether IIs is allowed to set the AnonymousUser_ password"),
                new ColumnDefinition("LogVisits", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether IIs tracks all access to the directory"),
                new ColumnDefinition("Index", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether IIs searches the directory"),
                new ColumnDefinition("DefaultDoc", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Comma delimited list of file names to act as a default document"),
                new ColumnDefinition("AspDetailedError", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether detailed ASP errors are sent to browser"),
                new ColumnDefinition("HttpExpires", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Value to set the HttpExpires attribute to for a Web Dir in the metabase"),
                new ColumnDefinition("CacheControlMaxAge", ColumnType.Number, 4, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Integer value specifying the cache control maximum age value."),
                new ColumnDefinition("CacheControlCustom", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Custom HTTP 1.1 cache control directives."),
                new ColumnDefinition("NoCustomError", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether IIs will return custom errors for this directory."),
                new ColumnDefinition("AccessSSLFlags", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Specifies AccessSSLFlags IIS metabase property."),
                new ColumnDefinition("AuthenticationProviders", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Comma delimited list, in order of precedence, of Windows authentication providers that IIS will attempt to use: NTLM, Kerberos, Negotiate, and others."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebAddress = new TableDefinition(
            "IIsWebAddress",
            IisSymbolDefinitions.IIsWebAddress,
            new[]
            {
                new ColumnDefinition("Address", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Web_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebSite", keyColumn: 1, description: "Foreign key referencing Web that uses the address.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("IP", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "String representing IP address (#.#.#.#) or NT machine name (fooserver)", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Port", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Port web site listens on", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Header", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Special header information for the web site"),
                new ColumnDefinition("Secure", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether SSL is used to communicate with web site"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebSite = new TableDefinition(
            "IIsWebSite",
            IisSymbolDefinitions.IIsWebSite,
            new[]
            {
                new ColumnDefinition("Web", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the web site", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description displayed in IIS MMC applet"),
                new ColumnDefinition("ConnectionTimeout", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Time connection is maintained without activity (in seconds)"),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key referencing directory that the web site points at", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("State", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1;2", description: "Sets intial state of web site"),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "2", description: "Control the install behavior of web site"),
                new ColumnDefinition("KeyAddress_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebAddress", keyColumn: 1, description: "Foreign key referencing primary address for the web site", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DirProperties_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebDirProperties", keyColumn: 1, description: "Foreign key referencing possible security information for the web site", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebApplication", keyColumn: 1, description: "Foreign key referencing possible ASP application for the web site.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Allows ordering of web site install"),
                new ColumnDefinition("Log_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Unknown, keyTable: "IIsWebLog", keyColumn: 1, description: "Foreign key reference to IIsWebLog data", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Id", ColumnType.String, 74, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Optional number or formatted value that resolves to number that acts as the WebSite Id."),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebApplication = new TableDefinition(
            "IIsWebApplication",
            IisSymbolDefinitions.IIsWebApplication,
            new[]
            {
                new ColumnDefinition("Application", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token for ASP Application", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.Localized, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of application in IIS MMC applet", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Isolation", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, possibilities: "0;1;2", description: "Isolation level for ASP Application: 0 == Low, 2 == Medium, 1 == High"),
                new ColumnDefinition("AllowSessions", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether application may maintain session state"),
                new ColumnDefinition("SessionTimeout", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Time session state is maintained without user interaction"),
                new ColumnDefinition("Buffer", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether application buffers its output"),
                new ColumnDefinition("ParentPaths", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "What is this for anyway?"),
                new ColumnDefinition("DefaultScript", ColumnType.String, 26, primaryKey: false, nullable: true, ColumnCategory.Text, possibilities: "VBScript;JScript", description: "Default scripting language for ASP applications"),
                new ColumnDefinition("ScriptTimeout", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Time ASP application page is permitted to process"),
                new ColumnDefinition("ServerDebugging", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether to allow ASP server-side script debugging"),
                new ColumnDefinition("ClientDebugging", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "0;1", description: "Specifies whether to allow ASP client-side script debugging"),
                new ColumnDefinition("AppPool_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsAppPool", keyColumn: 1, description: "App Pool this application should run under", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebApplicationExtension = new TableDefinition(
            "IIsWebApplicationExtension",
            IisSymbolDefinitions.IIsWebApplicationExtension,
            new[]
            {
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebApplication", keyColumn: 1, description: "Foreign key referencing possible ASP application for the web site", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Extension", ColumnType.String, 255, primaryKey: true, nullable: true, ColumnCategory.Text, description: "Primary key, Extension that should be registered for this ASP application"),
                new ColumnDefinition("Verbs", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Text, description: "Comma delimited list of HTTP verbs the extension should be registered with"),
                new ColumnDefinition("Executable", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Path to extension (usually file property: [#file])", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, possibilities: "1;4;5", description: "Attributes for extension: 1 == Script, 4 == Check Path Info"),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IIsFilter = new TableDefinition(
            "IIsFilter",
            IisSymbolDefinitions.IIsFilter,
            new[]
            {
                new ColumnDefinition("Filter", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Unknown, description: "Name of the ISAPI Filter in IIS"),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the filter", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Path to filter (usually file property: [#file])", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Web_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebSite", keyColumn: 1, description: "Foreign key referencing web site that loads the filter (NULL == global filter", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Description", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description displayed in IIS MMC applet"),
                new ColumnDefinition("Flags", ColumnType.Number, 4, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 2147483647, description: "What do all these numbers mean?"),
                new ColumnDefinition("LoadOrder", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "-1 == last in order, 0 == first in order, # == place in order"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebDir = new TableDefinition(
            "IIsWebDir",
            IisSymbolDefinitions.IIsWebDir,
            new[]
            {
                new ColumnDefinition("WebDir", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Web_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebSite", keyColumn: 1, description: "Foreign key referencing web site that controls the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Path", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of web directory displayed in IIS MMC applet", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("DirProperties_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebDirProperties", keyColumn: 1, description: "Foreign key referencing possible security information for the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebApplication", keyColumn: 1, description: "Foreign key referencing possible ASP application for the virtual directory. This column is currently unused, but maintained for compatibility reasons.", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebError = new TableDefinition(
            "IIsWebError",
            IisSymbolDefinitions.IIsWebError,
            new[]
            {
                new ColumnDefinition("ErrorCode", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, minValue: 400, maxValue: 599, description: "HTTP status code indicating error."),
                new ColumnDefinition("SubCode", ColumnType.Number, 4, primaryKey: true, nullable: false, ColumnCategory.Unknown, description: "HTTP sub-status code indicating error."),
                new ColumnDefinition("ParentType", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "1;2", description: "Type of parent: 1=vdir, 2=web"),
                new ColumnDefinition("ParentValue", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the parent value.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Path to file for this custom error (usually file property: [#file]).  Must be null if URL is not null."),
                new ColumnDefinition("URL", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "URL for this custom error.  Must be null if File is not null."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IIsHttpHeader = new TableDefinition(
            "IIsHttpHeader",
            IisSymbolDefinitions.IIsHttpHeader,
            new[]
            {
                new ColumnDefinition("HttpHeader", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("ParentType", ColumnType.Number, 2, primaryKey: true, nullable: false, ColumnCategory.Unknown, possibilities: "1;2", description: "Type of parent: 1=vdir, 2=web"),
                new ColumnDefinition("ParentValue", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Name of the parent value.", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Name", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Name of the HTTP Header"),
                new ColumnDefinition("Value", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "URL for this custom error.  Must be null if File is not null."),
                new ColumnDefinition("Attributes", ColumnType.Number, 2, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 0, description: "Attributes for HTTP Header: none"),
                new ColumnDefinition("Sequence", ColumnType.Number, 2, primaryKey: false, nullable: true, ColumnCategory.Unknown, description: "Order to add the HTTP Headers."),
            },
            symbolIdIsPrimaryKey: false
        );

        public static readonly TableDefinition IIsWebServiceExtension = new TableDefinition(
            "IIsWebServiceExtension",
            IisSymbolDefinitions.IIsWebServiceExtension,
            new[]
            {
                new ColumnDefinition("WebServiceExtension", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the WebServiceExtension handler", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("File", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Path to handler (usually file property: [#file])", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Description", ColumnType.Localized, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "Description displayed in WebServiceExtension Wizard", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Group", ColumnType.String, 255, primaryKey: false, nullable: true, ColumnCategory.Formatted, description: "String used to identify groups of extensions.", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Attributes", ColumnType.Number, 1, primaryKey: false, nullable: false, ColumnCategory.Unknown, minValue: 0, maxValue: 3, description: "Attributes for WebServiceExtension: 1 = Allow, 2 = UIDeletable"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebVirtualDir = new TableDefinition(
            "IIsWebVirtualDir",
            IisSymbolDefinitions.IIsWebVirtualDir,
            new[]
            {
                new ColumnDefinition("VirtualDir", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Component_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Component", keyColumn: 1, description: "Foreign key referencing Component that controls the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Web_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "IIsWebSite", keyColumn: 1, description: "Foreign key referencing web site that controls the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Alias", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Formatted, description: "Name of virtual directory displayed in IIS MMC applet", modularizeType: ColumnModularizeType.Property),
                new ColumnDefinition("Directory_", ColumnType.String, 72, primaryKey: false, nullable: false, ColumnCategory.Identifier, keyTable: "Directory", keyColumn: 1, description: "Foreign key referencing directory that the virtual directory points at", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("DirProperties_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebDirProperties", keyColumn: 1, description: "Foreign key referencing possible security information for the virtual directory", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Application_", ColumnType.String, 72, primaryKey: false, nullable: true, ColumnCategory.Identifier, keyTable: "IIsWebApplication", keyColumn: 1, description: "Foreign key referencing possible ASP application for the virtual directory", modularizeType: ColumnModularizeType.Column),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition IIsWebLog = new TableDefinition(
            "IIsWebLog",
            IisSymbolDefinitions.IIsWebLog,
            new[]
            {
                new ColumnDefinition("Log", ColumnType.String, 72, primaryKey: true, nullable: false, ColumnCategory.Identifier, description: "Primary key, non-localized token", modularizeType: ColumnModularizeType.Column),
                new ColumnDefinition("Format", ColumnType.String, 255, primaryKey: false, nullable: false, ColumnCategory.Text, description: "Type of log format"),
            },
            symbolIdIsPrimaryKey: true
        );

        public static readonly TableDefinition[] All = new[]
        {
            Certificate,
            CertificateHash,
            IIsWebSiteCertificates,
            IIsAppPool,
            IIsMimeMap,
            IIsProperty,
            IIsWebDirProperties,
            IIsWebAddress,
            IIsWebSite,
            IIsWebApplication,
            IIsWebApplicationExtension,
            IIsFilter,
            IIsWebDir,
            IIsWebError,
            IIsHttpHeader,
            IIsWebServiceExtension,
            IIsWebVirtualDir,
            IIsWebLog,
        };
    }
}
