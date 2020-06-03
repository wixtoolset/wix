// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if !NETCOREAPP
namespace WixToolset.BuildTasks
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public partial class ToolsetTask
    {
        protected sealed override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            if (this.RunAsSeparateProcess)
            {
                return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }

            return this.ExecuteInProc($"{commandLineCommands} {responseFileCommands}");
        }

        private int ExecuteInProc(string commandLineString)
        {
            this.Log.LogMessage(MessageImportance.Normal, $"({this.ToolName}){commandLineString}");

            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var listener = new MsbuildMessageListener(this.Log, this.TaskShortName, this.BuildEngine.ProjectFileOfTaskNode);
            int exitCode = -1;

            try
            {
                exitCode = this.ExecuteCore(serviceProvider, listener, commandLineString);
            }
            catch (WixException e)
            {
                listener.Write(e.Error);
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, null);

                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            if (exitCode == 0 && this.Log.HasLoggedErrors)
            {
                exitCode = -1;
            }
            return exitCode;
        }

        protected sealed override void LogToolCommand(string message)
        {
            // Only log this if we're actually going to do it.
            if (this.RunAsSeparateProcess)
            {
                base.LogToolCommand(message);
            }
        }

        protected abstract int ExecuteCore(IWixToolsetServiceProvider serviceProvider, IMessageListener messageListener, string commandLineString);

        protected abstract string TaskShortName { get; }
    }
}
#endif
