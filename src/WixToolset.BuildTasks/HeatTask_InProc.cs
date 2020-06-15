// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility.Services;

    public partial class HeatTask
    {
        public override bool RunAsSeparateProcess { get => true; }

        protected sealed override string TaskShortName => "HEAT";

        protected sealed override Task<int> ExecuteCoreAsync(IWixToolsetCoreServiceProvider coreProvider, string commandLineString, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
#endif
