// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    public abstract class BaseBootstrapperApplicationFactory : IBootstrapperApplicationFactory
    {
        public IBootstrapperApplication Create(IBootstrapperEngine pEngine, ref Command command)
        {
            IEngine engine = new Engine(pEngine);
            IBootstrapperCommand bootstrapperCommand = command.GetBootstrapperCommand();
            return this.Create(engine, bootstrapperCommand);
        }

        protected abstract IBootstrapperApplication Create(IEngine engine, IBootstrapperCommand bootstrapperCommand);
    }
}
