// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

[assembly: WixToolset.Mba.Core.BootstrapperApplicationFactory(typeof(Example.EarliestCoreMBA.EarliestCoreBAFactory))]
namespace Example.EarliestCoreMBA
{
    using WixToolset.Mba.Core;

    public class EarliestCoreBAFactory : BaseBootstrapperApplicationFactory
    {
        private static int loadCount = 0;

        protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
        {
            if (loadCount > 0)
            {
                engine.Log(LogLevel.Standard, $"Reloaded {loadCount} time(s)");
            }
            ++loadCount;
            return new EarliestCoreBA(engine);
        }
    }
}
