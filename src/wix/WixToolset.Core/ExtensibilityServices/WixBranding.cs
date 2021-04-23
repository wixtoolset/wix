// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Resources;

[assembly: NeutralResourcesLanguage("en-US")]

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Branding strings.
    /// </summary>
    internal class WixBranding : IWixBranding
    {
        /// <summary>
        /// News URL for the distribution.
        /// </summary>
        public static string NewsUrl = "http://wixtoolset.org/news/";

        /// <summary>
        /// Short product name for the distribution.
        /// </summary>
        public static string ShortProduct = "WiX Toolset";

        /// <summary>
        /// Support URL for the distribution.
        /// </summary>
        public static string SupportUrl = "http://wixtoolset.org/";

        /// <summary>
        /// Telemetry URL format for the distribution.
        /// </summary>
        public static string TelemetryUrlFormat = "http://wixtoolset.org/telemetry/v{0}/?r={1}";

        /// <summary>
        /// VS Extensions Landing page Url for the distribution.
        /// </summary>
        public static string VSExtensionsLandingUrl = "http://wixtoolset.org/releases/";

        public string GetCreatingApplication()
        {
            return this.ReplacePlaceholders("[AssemblyProduct] ([FileVersion])");
        }

        public string ReplacePlaceholders(string original, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = typeof(WixBranding).Assembly;
            }

            var commonVersionPath = Path.Combine(Path.GetDirectoryName(typeof(WixBranding).Assembly.Location), "wixver.dll");
            if (File.Exists(commonVersionPath))
            {
                var commonFileVersion = FileVersionInfo.GetVersionInfo(commonVersionPath);

                original = original.Replace("[FileCopyright]", commonFileVersion.LegalCopyright);
                original = original.Replace("[FileVersion]", commonFileVersion.FileVersion);
            }

            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

            original = original.Replace("[FileComments]", fileVersion.Comments);
            original = original.Replace("[FileCopyright]", fileVersion.LegalCopyright);
            original = original.Replace("[FileProductName]", fileVersion.ProductName);
            original = original.Replace("[FileVersion]", fileVersion.FileVersion);

            if (original.Contains("[FileVersionMajorMinor]"))
            {
                var version = new Version(fileVersion.FileVersion);
                original = original.Replace("[FileVersionMajorMinor]", String.Concat(version.Major, ".", version.Minor));
            }

            if (TryGetAttribute(assembly, out AssemblyCompanyAttribute company))
            {
                original = original.Replace("[AssemblyCompany]", company.Company);
            }

            if (TryGetAttribute(assembly, out AssemblyCopyrightAttribute copyright))
            {
                original = original.Replace("[AssemblyCopyright]", copyright.Copyright);
            }

            if (TryGetAttribute(assembly, out AssemblyDescriptionAttribute description))
            {
                original = original.Replace("[AssemblyDescription]", description.Description);
            }

            if (TryGetAttribute(assembly, out AssemblyProductAttribute product))
            {
                original = original.Replace("[AssemblyProduct]", product.Product);
            }

            if (TryGetAttribute(assembly, out AssemblyTitleAttribute title))
            {
                original = original.Replace("[AssemblyTitle]", title.Title);
            }

            original = original.Replace("[NewsUrl]", NewsUrl);
            original = original.Replace("[ShortProduct]", ShortProduct);
            original = original.Replace("[SupportUrl]", SupportUrl);

            return original;
        }

        private static bool TryGetAttribute<T>(Assembly assembly, out T attribute) where T : Attribute
        {
            attribute = null;

            var customAttributes = assembly.GetCustomAttributes(typeof(T), false);
            if (null != customAttributes && 0 < customAttributes.Length)
            {
                attribute = customAttributes[0] as T;
            }

            return null != attribute;
        }
    }
}
