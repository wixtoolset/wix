// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensionCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Extension cache manager command.
    /// </summary>
    internal class ExtensionCacheManagerCommand : BaseCommandLineCommand
    {
        private enum CacheSubcommand
        {
            Add,
            Remove,
            List
        }

        public ExtensionCacheManagerCommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ExtensionManager = serviceProvider.GetService<IExtensionManager>();
            this.ExtensionReferences = new List<string>();
        }

        private IMessaging Messaging { get; }

        private IExtensionManager ExtensionManager { get; }

        private bool Global { get; set; }

        private CacheSubcommand? Subcommand { get; set; }

        private List<string> ExtensionReferences { get; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Manage the extension cache.", "extension add|remove|list [options] [extensionRef]")
            {
                Switches = new[]
                {
                    new CommandLineHelpSwitch("--global", "-g", "Add/remove the extension for the current user."),
                },
                Commands = new[]
                {
                    new CommandLineHelpCommand("add", "Add extension to the cache."),
                    new CommandLineHelpCommand("list", "List extensions in the cache."),
                    new CommandLineHelpCommand("remove", "Remove extension from the cache."),
                },
                Notes = "  extensionRef format: extensionId/version (the version is optional)"
            };
        }

        public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!this.Subcommand.HasValue)
            {
                this.Messaging.Write(ErrorMessages.CommandLineCommandRequired("extension"));
                return this.Messaging.LastErrorNumber;
            }

            var success = false;
            var cacheManager = new ExtensionCacheManager(this.Messaging, this.ExtensionManager);

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

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
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
    }
}
