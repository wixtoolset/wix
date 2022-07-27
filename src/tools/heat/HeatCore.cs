// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Extensibility;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    internal class HeatCore : IHeatCore
    {
        /// <summary>
        /// Instantiates a new HeatCore.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="extensionArgument">The extension argument.</param>
        public HeatCore(IServiceProvider serviceProvider, string extensionArgument)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            var harvesterCore = new HarvesterCore
            {
                ExtensionArgument = extensionArgument,
                Messaging = this.Messaging,
                ParseHelper = serviceProvider.GetService<IParseHelper>(),
            };

            this.Harvester = new Harvester
            {
                Core = harvesterCore,
            };
            this.Mutator = new Mutator
            {
                Core = harvesterCore,
            };
        }

        public IHarvester Harvester { get; }

        public IMessaging Messaging { get; }

        public IMutator Mutator { get; }
    }
}
