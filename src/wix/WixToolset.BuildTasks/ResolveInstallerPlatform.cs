// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task calculates the InstallerPlatform from the RuntimeIdentifier,
    /// InitialInstallerPlatform and Platform properties.
    /// </summary>
    public class ResolveInstallerPlatform : Task
    {
        private readonly ILogger logger;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ResolveInstallerPlatform()
        {
            this.logger = new MSBuildLoggerAdapter(this.Log);
        }

        /// <summary>
        /// Constructor for dependency injection of logger used by unit tests.
        /// </summary>
        public ResolveInstallerPlatform(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// The optional RuntimeIdentifier property.
        /// </summary>
        public string RuntimeIdentifier { private get; set; }

        /// <summary>
        /// The optional InitialInstallerPlatform property.
        /// </summary>
        public string InitialInstallerPlatform { private get; set; }

        /// <summary>
        /// The InstallerPlatform property.
        /// </summary>
        public string InstallerPlatform { private get; set; }

        /// <summary>
        /// The optional Platform property.
        /// </summary>
        public string Platform { private get; set; }

        /// <summary>
        /// The resolved InstallerPlatform.
        /// </summary>
        [Output]
        public string ResolvedInstallerPlatform { get; private set; }

        /// <summary>
        /// The optionally resolved Platform.
        /// </summary>
        [Output]
        public string ResolvedPlatform { get; private set; }

        /// <summary>
        /// Convert the input properties into output items.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            if (String.IsNullOrEmpty(this.RuntimeIdentifier))
            {
                this.ResolvedInstallerPlatform = this.InstallerPlatform;
            }
            else if (this.ValidateWindowsRuntimeIdentifier(this.RuntimeIdentifier, out var platform))
            {
                if (!String.IsNullOrEmpty(this.InitialInstallerPlatform) && !String.Equals(this.InitialInstallerPlatform, platform, StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogError($"The RuntimeIdentifier '{this.RuntimeIdentifier}' resolves to platform '{platform}', which conflicts with the provided InstallerPlatform '{this.InitialInstallerPlatform}'.");
                }
                else
                {
                    this.ResolvedInstallerPlatform = platform;
                }
            }

            // If Platform was a generic value, resolve it to the resolved installer platform.
            if (String.IsNullOrEmpty(this.Platform)
                || this.Platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase)
                || this.Platform.Equals("Any CPU", StringComparison.OrdinalIgnoreCase)
                || this.Platform.Equals("Win32", StringComparison.OrdinalIgnoreCase))
            {
                this.ResolvedPlatform = this.ResolvedInstallerPlatform;
            }
            else if (!this.Platform.Equals(this.ResolvedInstallerPlatform, StringComparison.OrdinalIgnoreCase))
            {
                this.logger.LogWarning($"The provided Platform '{this.Platform}' does not match the resolved InstallerPlatform '{this.ResolvedInstallerPlatform}'. The output will be built using '{this.ResolvedInstallerPlatform}'.");
            }

            return !this.logger.HasLoggedErrors;
        }

        private bool ValidateWindowsRuntimeIdentifier(string runtimeIdentifier, out string platform)
        {
            platform = null;

            var ridParts = runtimeIdentifier.Split('-');
            if (ridParts.Length < 2)
            {
                this.logger.LogError($"The RuntimeIdentifier '{runtimeIdentifier}' is not valid.");

                return false;
            }

            var os = ridParts[0];

            if (!os.StartsWith("win", StringComparison.OrdinalIgnoreCase) || (os.Length > 3 && !Int32.TryParse(os.Substring(3), out var _)))
            {
                this.logger.LogError($"The RuntimeIdentifier '{runtimeIdentifier}' is not a valid Windows RuntimeIdentifier.");

                return false;
            }

            // Ensure there is only one platform specified in the RID.
            foreach (var part in ridParts.Skip(1))
            {
                string platformPart;
                switch (part.ToLowerInvariant())
                {
                    case "x86":
                    case "win32":
                        platformPart = "x86";
                        break;

                    case "x64":
                    case "amd64":
                        platformPart = "x64";
                        break;

                    case "arm":
                    case "arm32":
                        platformPart = "arm";
                        break;

                    case "arm64":
                        platformPart = "arm64";
                        break;

                    default:
                        continue;
                }

                if (String.IsNullOrEmpty(platform))
                {
                    platform = platformPart;
                }
                else // there can be only one platform in the RID.
                {
                    this.logger.LogError($"The RuntimeIdentifier '{runtimeIdentifier}' specifies multiple platforms which is not supported.");
                }
            }

            if (String.IsNullOrEmpty(platform))
            {
                this.logger.LogError($"The RuntimeIdentifier '{runtimeIdentifier}' does not specify a valid platform.");

                return false;
            }

            return true;
        }
    }
}
