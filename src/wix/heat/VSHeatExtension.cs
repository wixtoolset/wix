// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections;
    using WixToolset.Data;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;

    /// <summary>
    /// Defines generated element types.
    /// </summary>
    internal enum GenerateType
    {
        /// <summary>Generate Components.</summary>
        Components,

        /// <summary>Generate a Container with Payloads.</summary>
        Container,

        /// <summary>Generate a Bundle PackageGroups.</summary>
        PackageGroup,

        /// <summary>Generate a PayloadGroup with Payloads.</summary>
        PayloadGroup,
    }

    /// <summary>
    /// VS-related extensions for the WiX Toolset Harvester application.
    /// </summary>
    internal class VSHeatExtension : BaseHeatExtension
    {
        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        public override HeatCommandLineOption[] CommandLineTypes
        {
            get
            {
                return new HeatCommandLineOption[]
                {
                    new HeatCommandLineOption("project", "harvest outputs of a VS project"),
                    new HeatCommandLineOption("-configuration", "configuration to set when harvesting the project"),
                    new HeatCommandLineOption("-directoryid", "overridden directory id for generated directory elements"),
                    new HeatCommandLineOption("-generate", Environment.NewLine +
                        "            specify what elements to generate, one of:" + Environment.NewLine + 
                        "                components, container, payloadgroup, packagegroup" + Environment.NewLine +
                        "                (default is components)"),
                    new HeatCommandLineOption("-msbuildbinpath", "msbuild bin directory path"),
                    new HeatCommandLineOption("-platform", "platform to set when harvesting the project"),
                    new HeatCommandLineOption("-pog", Environment.NewLine +
                        "            specify output group of VS project, one of:" + Environment.NewLine +
                        "                " + String.Join(",", VSProjectHarvester.GetOutputGroupNames()) + Environment.NewLine +
                        "              This option may be repeated for multiple output groups."),
                    new HeatCommandLineOption("-projectname", "overridden project name to use in variables"),
                    new HeatCommandLineOption("-usetoolsversion", "ignore msbuildbinpath if project specifies known msbuild version"),
                    new HeatCommandLineOption("-wixvar", "generate binder variables instead of preprocessor variables"),
                };
            }
        }

        /// <summary>
        /// Parse the command line options for this extension.
        /// </summary>
        /// <param name="type">The active harvester type.</param>
        /// <param name="args">The option arguments.</param>
        public override void ParseOptions(string type, string[] args)
        {
            if ("project" == type)
            {
                string[] allOutputGroups = VSProjectHarvester.GetOutputGroupNames();
                bool suppressUniqueId = false;
                bool generateWixVars = false;
                bool useToolsVersion = false;
                GenerateType generateType = GenerateType.Components;
                string directoryIds = null;
                string msbuildBinPath = null;
                string projectName = null;
                string configuration = null;
                string platform = null;
                ArrayList outputGroups = new ArrayList();

                for (int i = 0; i < args.Length; i++)
                {
                    if ("-configuration" == args[i])
                    {
                        configuration = args[++i];
                    }
                    else if ("-directoryid" == args[i])
                    {
                        if (!IsValidArg(args, ++i))
                        {
                            throw new WixException(HarvesterErrors.InvalidDirectoryId(args[i]));
                        }
                        
                        directoryIds = args[i];
                    }
                    else if ("-generate" == args[i])
                    {
                        if (!IsValidArg(args, ++i))
                        {
                            throw new WixException(HarvesterErrors.InvalidProjectOutputType(args[i]));
                        }

                        string genType = args[i].ToUpperInvariant();
                        switch(genType)
                        {
                            case "CONTAINER":
                                generateType = GenerateType.Container;
                                break;
                            case "COMPONENTS":
                                generateType = GenerateType.Components;
                                break;
                            case "PACKAGEGROUP":
                                generateType = GenerateType.PackageGroup;
                                break;
                            case "PAYLOADGROUP":
                                generateType = GenerateType.PayloadGroup;
                                break;
                            default:
                                throw new WixException(HarvesterErrors.InvalidProjectOutputType(genType));
                        }
                    }
                    else if ("-msbuildbinpath" == args[i])
                    {
                        if (!IsValidArg(args, ++i))
                        {
                            throw new WixException(HarvesterErrors.ArgumentRequiresValue(args[i-1]));
                        }

                        msbuildBinPath = args[i];
                    }
                    else if ("-platform" == args[i])
                    {
                        platform = args[++i];
                    }
                    else if ("-pog" == args[i])
                    {
                        if (!IsValidArg(args, ++i))
                        {
                            throw new WixException(HarvesterErrors.InvalidOutputGroup(args[i]));
                        }

                        string pogName = args[i];
                        bool found = false;
                        foreach (string availableOutputGroup in allOutputGroups)
                        {
                            if (String.Equals(pogName, availableOutputGroup, StringComparison.Ordinal))
                            {
                                outputGroups.Add(availableOutputGroup);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new WixException(HarvesterErrors.InvalidOutputGroup(pogName));
                        }
                    }
                    else if (args[i].StartsWith("-pog:", StringComparison.Ordinal))
                    {
                        this.Core.Messaging.Write(WarningMessages.DeprecatedCommandLineSwitch("pog:", "pog"));

                        string pogName = args[i].Substring(5);
                        bool found = false;
                        foreach (string availableOutputGroup in allOutputGroups)
                        {
                            if (String.Equals(pogName, availableOutputGroup, StringComparison.Ordinal))
                            {
                                outputGroups.Add(availableOutputGroup);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new WixException(HarvesterErrors.InvalidOutputGroup(pogName));
                        }
                    }
                    else if ("-projectname" == args[i])
                    {
                        if (!IsValidArg(args, ++i))
                        {
                            throw new WixException(HarvesterErrors.InvalidProjectName(args[i]));
                        }
                        
                        projectName = args[i];
                    }
                    else if ("-suid" == args[i])
                    {
                        suppressUniqueId = true;
                    }
                    else if ("-usetoolsversion" == args[i])
                    {
                        useToolsVersion = true;
                    }
                    else if ("-wixvar" == args[i])
                    {
                        generateWixVars = true;
                    }
                }

                if (outputGroups.Count == 0)
                {
                    throw new WixException(HarvesterErrors.NoOutputGroupSpecified());
                }

                VSProjectHarvester harvester = new VSProjectHarvester(
                    (string[]) outputGroups.ToArray(typeof(string)));

                harvester.SetUniqueIdentifiers = !suppressUniqueId;
                harvester.GenerateWixVars = generateWixVars;
                harvester.GenerateType = generateType;
                harvester.DirectoryIds = directoryIds;
                harvester.MsbuildBinPath = msbuildBinPath;
                harvester.ProjectName = projectName;
                harvester.Configuration = configuration;
                harvester.Platform = platform;
                harvester.UseToolsVersion = String.IsNullOrEmpty(msbuildBinPath) || useToolsVersion;

                this.Core.Harvester.Extension = harvester;
            }
        }
    }
}
