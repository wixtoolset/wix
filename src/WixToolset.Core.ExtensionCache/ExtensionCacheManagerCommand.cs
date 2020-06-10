// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extension cache manager command.
    /// </summary>
    internal class ExtensionCacheManagerCommand : ICommandLineCommand
    {
        private enum CacheSubcommand
        {
            Add,
            Remove,
            List
        }

        public ExtensionCacheManagerCommand(IWixToolsetServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionReferences = new List<string>();
        }

        private IMessaging Messaging { get; }

        public bool ShowLogo { get; private set; }

        public bool StopParsing { get; private set; }

        private bool ShowHelp { get; set; }

        private bool Global { get; set; }

        private CacheSubcommand? Subcommand { get; set; }

        private List<string> ExtensionReferences { get; }

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.ShowHelp || !this.Subcommand.HasValue)
            {
                DisplayHelp();
                return 1;
            }

            var success = false;
            var cacheManager = new ExtensionCacheManager();

            switch (this.Subcommand)
            {
                case CacheSubcommand.Add:
                    success = await this.AddExtensions(cacheManager, cancellationToken);
                    break;

                case CacheSubcommand.Remove:
                    success = await this.RemoveExtensions(cacheManager, cancellationToken);
                    break;

                case CacheSubcommand.List:
                    success = await this.ListExtensions(cacheManager, cancellationToken);
                    break;
            }

            return success ? 0 : 2;
        }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (!parser.IsSwitch(argument))
            {
                if (!this.Subcommand.HasValue)
                {
                    if (!Enum.TryParse(argument, true, out CacheSubcommand subcommand))
                    {
                        return false;
                    }

                    this.Subcommand = subcommand;
                }
                else
                {
                    this.ExtensionReferences.Add(argument);
                }

                return true;
            }

            var parameter = argument.Substring(1);
            switch (parameter.ToLowerInvariant())
            {
                case "?":
                    this.ShowHelp = true;
                    this.ShowLogo = true;
                    this.StopParsing = true;
                    return true;

                case "nologo":
                    this.ShowLogo = false;
                    return true;

                case "g":
                case "-global":
                    this.Global = true;
                    return true;
            }

            return false;
        }

        private async Task<bool> AddExtensions(ExtensionCacheManager cacheManager, CancellationToken cancellationToken)
        {
            var success = false;

            foreach (var extensionRef in this.ExtensionReferences)
            {
                var added = await cacheManager.AddAsync(this.Global, extensionRef, cancellationToken);
                success |= added;
            }

            return success;
        }

        private async Task<bool> RemoveExtensions(ExtensionCacheManager cacheManager, CancellationToken cancellationToken)
        {
            var success = false;

            foreach (var extensionRef in this.ExtensionReferences)
            {
                var removed = await cacheManager.RemoveAsync(this.Global, extensionRef, cancellationToken);
                success |= removed;
            }

            return success;
        }

        private async Task<bool> ListExtensions(ExtensionCacheManager cacheManager, CancellationToken cancellationToken)
        {
            var found = false;
            var extensionRef = this.ExtensionReferences.FirstOrDefault();

            var extensions = await cacheManager.ListAsync(this.Global, extensionRef, cancellationToken);

            foreach (var extension in extensions)
            {
                this.Messaging.Write($"{extension.Id} {extension.Version}{(extension.Damaged ? " (damaged)" : String.Empty)}");
                found = true;
            }

            return found;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine(" usage:  wix.exe extension add|remove|list [extensionRef]");
            Console.WriteLine();
            Console.WriteLine("   -g       add/remove the extension for the current user");
            Console.WriteLine("   -nologo  suppress displaying the logo information");
            Console.WriteLine("   -?       this help information");
            Console.WriteLine();
            Console.WriteLine("   extensionRef format: extensionId/version (the version is optional)");
        }
    }
}
