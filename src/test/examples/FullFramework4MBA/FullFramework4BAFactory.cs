// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

[assembly: WixToolset.Mba.Core.BootstrapperApplicationFactory(typeof(Example.FullFramework4MBA.FullFramework4BAFactory))]
namespace Example.FullFramework4MBA
{
    using WixToolset.Mba.Core;

    public class FullFramework4BAFactory : BaseBootstrapperApplicationFactory
    {
        protected override IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand)
        {
            return new FullFramework4BA(engine);
        }
    }
}
