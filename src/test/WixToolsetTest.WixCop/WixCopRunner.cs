// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.WixCop
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core;
    using WixToolset.Core.TestPackage;
    using WixToolset.Extensibility;
    using WixToolset.Tools.WixCop;
    using WixToolset.Tools.WixCop.CommandLine;
    using WixToolset.Tools.WixCop.Interfaces;

    public class WixCopRunner
    {
        public bool FixErrors { get; set; }

        public List<string> SearchPatterns { get; } = new List<string>();

        public string SettingFile1 { get; set; }

        public WixCopRunnerResult Execute()
        {
            var argList = new List<string>();

            if (this.FixErrors)
            {
                argList.Add("-f");
            }

            if (!String.IsNullOrEmpty(this.SettingFile1))
            {
                argList.Add($"-set1{this.SettingFile1}");
            }

            foreach (var searchPattern in this.SearchPatterns)
            {
                argList.Add(searchPattern);
            }

            return WixCopRunner.Execute(argList.ToArray());
        }

        public static WixCopRunnerResult Execute(string[] args)
        {
            var listener = new TestMessageListener();

            var serviceProvider = new WixToolsetServiceProvider();
            serviceProvider.AddService<IMessageListener>((x, y) => listener);
            serviceProvider.AddService<IWixCopCommandLineParser>((x, y) => new WixCopCommandLineParser(x));

            var exitCode = Execute(serviceProvider, args);

            return new WixCopRunnerResult
            {
                ExitCode = exitCode,
                Messages = listener.Messages.ToArray()
            };
        }

        public static int Execute(IServiceProvider serviceProvider, string[] args)
        {
            var wixcop = new Program();
            return wixcop.Run(serviceProvider, args);
        }
    }
}
