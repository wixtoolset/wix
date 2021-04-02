// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Binds the summary information table of a database.
    /// </summary>
    internal class BindSummaryInfoCommand
    {
        public BindSummaryInfoCommand(IntermediateSection section, int? summaryInformationCodepage, string productLanguage, IBackendHelper backendHelper, IWixBranding branding)
        {
            this.Section = section;
            this.SummaryInformationCodepage = summaryInformationCodepage;
            this.ProductLanguage = productLanguage;
            this.BackendHelper = backendHelper;
            this.Branding = branding;
        }

        private IntermediateSection Section { get; }

        private int? SummaryInformationCodepage { get; }

        private string ProductLanguage { get; }

        private IBackendHelper BackendHelper { get; }

        private IWixBranding Branding { get; }

        /// <summary>
        /// Returns a flag indicating if files are compressed by default.
        /// </summary>
        public bool Compressed { get; private set; }

        /// <summary>
        /// Returns a flag indicating if uncompressed files use long filenames.
        /// </summary>
        public bool LongNames { get; private set; }

        public int InstallerVersion { get; private set; }

        public Platform Platform { get; private set; }

        /// <summary>
        /// Modularization guid, or null if the output is not a module.
        /// </summary>
        public string ModularizationSuffix { get; private set; }

        public void Execute()
        {
            this.Compressed = false;
            this.LongNames = false;
            this.InstallerVersion = 0;
            this.ModularizationSuffix = null;

            SummaryInformationSymbol summaryInformationCodepageSymbol = null;
            SummaryInformationSymbol platformAndLanguageSymbol = null;
            var foundCreateDateTime = false;
            var foundLastSaveDataTime = false;
            var foundCreatingApplication = false;
            var foundPackageCode = false;
            var now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            foreach (var summaryInformationSymbol in this.Section.Symbols.OfType<SummaryInformationSymbol>())
            {
                switch (summaryInformationSymbol.PropertyId)
                {
                    case SummaryInformationType.Codepage: // PID_CODEPAGE
                        summaryInformationCodepageSymbol = summaryInformationSymbol;
                        break;

                    case SummaryInformationType.PlatformAndLanguage:
                        platformAndLanguageSymbol = summaryInformationSymbol;
                        break;

                    case SummaryInformationType.PackageCode: // PID_REVNUMBER
                        foundPackageCode = true;
                        var packageCode = summaryInformationSymbol.Value;

                        if (SectionType.Module == this.Section.Type)
                        {
                            this.ModularizationSuffix = "." + packageCode.Substring(1, 36).Replace('-', '_');
                        }
                        break;
                    case SummaryInformationType.Created:
                        foundCreateDateTime = true;
                        break;
                    case SummaryInformationType.LastSaved:
                        foundLastSaveDataTime = true;
                        break;
                    case SummaryInformationType.WindowsInstallerVersion:
                        this.InstallerVersion = summaryInformationSymbol[SummaryInformationSymbolFields.Value].AsNumber();
                        break;
                    case SummaryInformationType.WordCount:
                        if (SectionType.Patch == this.Section.Type)
                        {
                            this.LongNames = true;
                            this.Compressed = true;
                        }
                        else
                        {
                            var attributes = summaryInformationSymbol[SummaryInformationSymbolFields.Value].AsNumber();
                            this.LongNames = (0 == (attributes & 1));
                            this.Compressed = (2 == (attributes & 2));
                        }
                        break;
                    case SummaryInformationType.CreatingApplication: // PID_APPNAME
                        foundCreatingApplication = true;
                        break;
                }
            }

            // Ensure the codepage is set properly.
            if (summaryInformationCodepageSymbol == null)
            {
                summaryInformationCodepageSymbol = this.Section.AddSymbol(new SummaryInformationSymbol(null)
                {
                    PropertyId = SummaryInformationType.Codepage
                });
            }

            var codepage = summaryInformationCodepageSymbol.Value;

            if (String.IsNullOrEmpty(codepage))
            {
                codepage = this.SummaryInformationCodepage?.ToString(CultureInfo.InvariantCulture) ?? "1252";
            }

            summaryInformationCodepageSymbol.Value = this.BackendHelper.GetValidCodePage(codepage, onlyAnsi: true).ToString(CultureInfo.InvariantCulture);

            // Ensure the language is set properly and figure out what platform we are targeting.
            if (platformAndLanguageSymbol != null)
            {
                this.Platform = EnsureLanguageAndGetPlatformFromSummaryInformation(platformAndLanguageSymbol, this.ProductLanguage);
            }

            // Set the revision number (package/patch code) if it should be automatically generated.
            if (!foundPackageCode)
            {
                this.Section.AddSymbol(new SummaryInformationSymbol(null)
                {
                    PropertyId = SummaryInformationType.PackageCode,
                    Value = this.BackendHelper.CreateGuid(),
                });
            }

            // add a summary information row for the create time/date property if its not already set
            if (!foundCreateDateTime)
            {
                this.Section.AddSymbol(new SummaryInformationSymbol(null)
                {
                    PropertyId = SummaryInformationType.Created,
                    Value = now,
                });
            }

            // add a summary information row for the last save time/date property if its not already set
            if (!foundLastSaveDataTime)
            {
                this.Section.AddSymbol(new SummaryInformationSymbol(null)
                {
                    PropertyId = SummaryInformationType.LastSaved,
                    Value = now,
                });
            }

            // add a summary information row for the creating application property if its not already set
            if (!foundCreatingApplication)
            {
                this.Section.AddSymbol(new SummaryInformationSymbol(null)
                {
                    PropertyId = SummaryInformationType.CreatingApplication,
                    Value = this.Branding.GetCreatingApplication(),
                });
            }
        }

        private static Platform EnsureLanguageAndGetPlatformFromSummaryInformation(SummaryInformationSymbol symbol, string language)
        {
            var value = symbol.Value;
            var separatorIndex = value.IndexOf(';');
            var platformValue = separatorIndex > 0 ? value.Substring(0, separatorIndex) : value;

            // If the language was provided and there was language value after the separator
            // (or the separator was absent) then use the provided language.
            if (!String.IsNullOrEmpty(language) && (separatorIndex < 0 || separatorIndex + 1 == value.Length))
            {
                symbol.Value = platformValue + ';' + language;
            }

            switch (platformValue)
            {
                case "x64":
                    return Platform.X64;

                case "Arm64":
                    return Platform.ARM64;

                case "Intel":
                default:
                    return Platform.X86;
            }
        }
    }
}
