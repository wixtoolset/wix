// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Common utilities for Wix applications.
    /// </summary>
    public static class AppCommon
    {
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
