// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Extensibility
{
    using System;
    using WixToolset.Harvesters.Data;

    /// <summary>
    /// An extension for the WiX Toolset Harvester application.
    /// </summary>
    public abstract class BaseHeatExtension : IHeatExtension
    {
        /// <summary>
        /// Gets or sets the heat core for the extension.
        /// </summary>
        /// <value>The heat core for the extension.</value>
        public IHeatCore Core { get; set; }

        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        public virtual HeatCommandLineOption[] CommandLineTypes
        {
            get { return null; }
        }

        /// <summary>
        /// Parse the command line options for this extension.
        /// </summary>
        /// <param name="type">The active harvester type.</param>
        /// <param name="args">The option arguments.</param>
        public virtual void ParseOptions(string type, string[] args)
        {
        }

        /// <summary>
        /// Determines if the index refers to an argument.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsValidArg(string[] args, int index)
        {
            if (args.Length <= index || String.IsNullOrEmpty(args[index]) || '/' == args[index][0] || '-' == args[index][0])
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
