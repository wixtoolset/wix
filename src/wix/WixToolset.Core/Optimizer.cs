// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class Optimizer : IOptimizer
    {
        internal Optimizer(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public void Optimize(IOptimizeContext context)
        {
            foreach (var extension in context.Extensions)
            {
                extension.PreOptimize(context);
            }

            // TODO: Fill with useful optimization features.

            foreach (var extension in context.Extensions)
            {
                extension.PostOptimize(context);
            }
        }
    }
}
