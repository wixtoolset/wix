// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Wix Toolset Command-Line Interface.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The main entry point for wix command-line interface.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            Messaging.Instance.InitializeAppName("WIX", "wix.exe");

            Messaging.Instance.Display += DisplayMessage;

            var command = CommandLine.ParseStandardCommandLine(args);

            return command?.Execute() ?? 1;
        }

        private static void DisplayMessage(object sender, DisplayEventArgs e)
        {
            switch (e.Level)
            {
                case MessageLevel.Warning:
                case MessageLevel.Error:
                    Console.Error.WriteLine(e.Message);
                    break;
                default:
                    Console.WriteLine(e.Message);
                    break;
            }
        }
    }
}
