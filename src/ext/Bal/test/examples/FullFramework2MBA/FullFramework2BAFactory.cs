// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

[assembly: WixToolset.Mba.Core.BootstrapperApplicationFactory(typeof(Example.FullFramework2MBA.FullFramework2BAFactory))]
namespace Example.FullFramework2MBA
{
    using WixToolset.Mba.Core;

    public class FullFramework2BAFactory : BaseBootstrapperApplicationFactory
    {
        private static int loadCount = 0;

        protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
        {
            if (loadCount > 0)
            {
                engine.Log(LogLevel.Standard, $"Reloaded {loadCount} time(s)");
            }
            ++loadCount;
            return new FullFramework2BA(engine);
        }
    }
}
