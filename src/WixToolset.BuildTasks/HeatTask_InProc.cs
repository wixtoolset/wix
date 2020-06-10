// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters;

    public partial class HeatTask
    {
        protected sealed override string TaskShortName => "HEAT";

        protected sealed override Task<int> ExecuteCoreAsync(IWixToolsetCoreServiceProvider serviceProvider, string commandLineString, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
#endif
