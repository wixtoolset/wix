// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters;

    public partial class HeatTask
    {
        protected sealed override string TaskShortName => "HEAT";

        protected sealed override int ExecuteCore(IWixToolsetServiceProvider serviceProvider, IMessageListener listener, string commandLineString)
        {
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var arguments = serviceProvider.GetService<ICommandLineArguments>();
            arguments.Populate(commandLineString);

            var commandLine = HeatCommandLineFactory.CreateCommandLine(serviceProvider, true);
            var command = commandLine.ParseStandardCommandLine(arguments);
            return command?.Execute() ?? -1;
        }
    }
}
#endif
