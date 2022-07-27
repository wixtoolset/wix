// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;

    /// <summary>
    /// Extension methods to use Harvesters services.
    /// </summary>
    public class HeatCommandLineFactory
    {
        /// <summary>
        /// Creates <see cref="IHeatCommandLine"/> service.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="heatExtensions"></param>
        /// <returns></returns>
        public static IHeatCommandLine CreateCommandLine(IServiceProvider serviceProvider, IEnumerable<IHeatExtension> heatExtensions = null)
        {
            return new HeatCommandLine(serviceProvider, heatExtensions);
        }
    }
}
