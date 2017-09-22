// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Common utilities for Wix applications.
    /// </summary>
    public static class AppCommon
    {
        /// <summary>
        /// Read the configuration file (*.exe.config).
        /// </summary>
        /// <param name="extensions">Extensions to load.</param>
        public static void ReadConfiguration(StringCollection extensions)
        {
            if (null == extensions)
            {
                throw new ArgumentNullException("extensions");
            }

#if REDO_IN_NETCORE
            // Don't use the default AppSettings reader because
            // the tool may be called from within another process.
            // Instead, read the .exe.config file from the tool location.
            string toolPath = (new System.Uri(Assembly.GetCallingAssembly().CodeBase)).LocalPath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(toolPath);
            if (config.HasFile)
            {
                KeyValueConfigurationElement configVal = config.AppSettings.Settings["extensions"];
                if (configVal != null)
                {
                    string extensionTypes = configVal.Value;
                    foreach (string extensionType in extensionTypes.Split(";".ToCharArray()))
                    {
                        extensions.Add(extensionType);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Gets a unique temporary location or uses the provided temporary location.
        /// </summary>
        /// <param name="tempLocation">Optional temporary location to use.</param>
        /// <returns>Temporary location.</returns>
        public static string GetTempLocation(string tempLocation = null)
        {
            if (String.IsNullOrEmpty(tempLocation))
            {
                tempLocation = Environment.GetEnvironmentVariable("WIX_TEMP") ?? Path.GetTempPath();

                do
                {
                    tempLocation = Path.Combine(tempLocation, DateTime.Now.ToString("wixyyMMddTHHmmssffff"));
                } while (Directory.Exists(tempLocation));
            }

            return tempLocation;
        }

        /// <summary>
        /// Delete a directory with retries and best-effort cleanup.
        /// </summary>
        /// <param name="path">The directory to delete.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public static bool DeleteDirectory(string path, IMessageHandler messageHandler)
        {
            return Common.DeleteTempFiles(path, messageHandler);
        }

        /// <summary>
        /// Prepares the console for localization.
        /// </summary>
        public static void PrepareConsoleForLocalization()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture.GetConsoleFallbackUICulture();
            if ((Console.OutputEncoding.CodePage != Encoding.UTF8.CodePage) &&
                (Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.OEMCodePage) &&
                (Console.OutputEncoding.CodePage != Thread.CurrentThread.CurrentUICulture.TextInfo.ANSICodePage))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }
        }

        /// <summary>
        /// Handler for display message events.
        /// </summary>
        /// <param name="sender">Sender of message.</param>
        /// <param name="e">Event arguments containing message to display.</param>
        public static void ConsoleDisplayMessage(object sender, DisplayEventArgs e)
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

        /// <summary>
        /// Creates and returns the string for CreatingApplication field (MSI Summary Information Stream).
        /// </summary>
        /// <remarks>It reads the AssemblyProductAttribute and AssemblyVersionAttribute of executing assembly
        /// and builds the CreatingApplication string of the form "[ProductName] ([ProductVersion])".</remarks>
        /// <returns>Returns value for PID_APPNAME."</returns>
        public static string GetCreatingApplicationString()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return WixDistribution.ReplacePlaceholders("[AssemblyProduct] ([FileVersion])", assembly);
        }

        /// <summary>
        /// Displays help message header on Console for caller tool.
        /// </summary>
        public static void DisplayToolHeader()
        {
            var assembly = Assembly.GetCallingAssembly();
            Console.WriteLine(WixDistribution.ReplacePlaceholders("[AssemblyProduct] [AssemblyDescription] version [FileVersion]\r\n[AssemblyCopyright]", assembly));
        }

        /// <summary>
        /// Displays help message header on Console for caller tool.
        /// </summary>
        public static void DisplayToolFooter()
        {
            var assembly = Assembly.GetCallingAssembly();
            Console.Write(WixDistribution.ReplacePlaceholders("\r\nFor more information see: [SupportUrl]", assembly));
        }
    }
}
