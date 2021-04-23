// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;

    /// <summary>
    /// A utility heat extension for the WiX Toolset Harvester application.
    /// </summary>
    internal class UtilHeatExtension : BaseHeatExtension
    {
        public UtilHeatExtension(IServiceProvider serviceProvider)
        {
            this.PayloadHarvester = serviceProvider.GetService<IPayloadHarvester>();
        }

        private IPayloadHarvester PayloadHarvester { get; }

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
                    new HeatCommandLineOption("dir", "harvest a directory"),
                    new HeatCommandLineOption("file", "harvest a file"),
                    new HeatCommandLineOption("exepackagepayload", "harvest a bundle payload as ExePackagePayload"),
                    new HeatCommandLineOption("msupackagepayload", "harvest a bundle payload as MsuPackagePayload"),
                    new HeatCommandLineOption("perf", "harvest performance counters"),
                    new HeatCommandLineOption("reg", "harvest a .reg file"),
                    new HeatCommandLineOption("-ag", "autogenerate component guids at compile time"),
                    new HeatCommandLineOption("-cg <ComponentGroupName>", "component group name (cannot contain spaces e.g -cg MyComponentGroup)"),
                    new HeatCommandLineOption("-dr <DirectoryName>", "directory reference to root directories (cannot contain spaces e.g. -dr MyAppDirRef)"),
                    new HeatCommandLineOption("-var <VariableName>", "substitute File/@Source=\"SourceDir\" with a preprocessor or a wix variable" + Environment.NewLine +
                                                      "(e.g. -var var.MySource will become File/@Source=\"$(var.MySource)\\myfile.txt\" and " + Environment.NewLine + 
                                                      "-var wix.MySource will become File/@Source=\"!(wix.MySource)\\myfile.txt\""),
                    new HeatCommandLineOption("-gg", "generate guids now"),
                    new HeatCommandLineOption("-g1", "generated guids are not in brackets"),
                    new HeatCommandLineOption("-ke", "keep empty directories"),
                    new HeatCommandLineOption("-scom", "suppress COM elements"),
                    new HeatCommandLineOption("-sfrag", "suppress fragments"),
                    new HeatCommandLineOption("-srd", "suppress harvesting the root directory as an element"),
                    new HeatCommandLineOption("-svb6", "suppress VB6 COM elements"),
                    new HeatCommandLineOption("-sreg", "suppress registry harvesting"),
                    new HeatCommandLineOption("-suid", "suppress unique identifiers for files, components, & directories"),
                    new HeatCommandLineOption("-t", "transform harvested output with XSL file"),
                    new HeatCommandLineOption("-template", "use template, one of: fragment,module,product"),
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
            bool active = false;
            IHarvesterExtension harvesterExtension = null;
            bool suppressHarvestingRegistryValues = false;
            UtilFinalizeHarvesterMutator utilFinalizeHarvesterMutator = new UtilFinalizeHarvesterMutator();
            UtilMutator utilMutator = new UtilMutator();
            List<UtilTransformMutator> transformMutators = new List<UtilTransformMutator>();
            GenerateType generateType = GenerateType.Components;

            // select the harvester
            switch (type)
            {
                case "dir":
                    harvesterExtension = new DirectoryHarvester();
                    active = true;
                    break;
                case "file":
                    harvesterExtension = new FileHarvester();
                    active = true;
                    break;
                case "exepackagepayload":
                    harvesterExtension = new PayloadHarvester(this.PayloadHarvester, WixBundlePackageType.Exe);
                    active = true;
                    break;
                case "msupackagepayload":
                    harvesterExtension = new PayloadHarvester(this.PayloadHarvester, WixBundlePackageType.Msu);
                    active = true;
                    break;
                case "perf":
                    harvesterExtension = new PerformanceCategoryHarvester();
                    active = true;
                    break;
                case "reg":
                    harvesterExtension = new RegFileHarvester();
                    active = true;
                    break;
            }

            // set default settings
            utilMutator.CreateFragments = true;
            utilMutator.SetUniqueIdentifiers = true;

            // parse the options
            for (int i = 0; i < args.Length; i++)
            {
                string commandSwitch = args[i];

                if (null == commandSwitch || 0 == commandSwitch.Length) // skip blank arguments
                {
                    continue;
                }

                if ('-' == commandSwitch[0] || '/' == commandSwitch[0])
                {
                    string truncatedCommandSwitch = commandSwitch.Substring(1);

                    if ("ag" == truncatedCommandSwitch)
                    {
                        utilMutator.AutogenerateGuids = true;
                    }
                    else if ("cg" == truncatedCommandSwitch)
                    {
                        utilMutator.ComponentGroupName = this.GetArgumentParameter(args, i);

                        if (this.Core.Messaging.EncounteredError)
                        {
                            return;
                        }
                    }
                    else if ("dr" == truncatedCommandSwitch)
                    {
                        string dr = this.GetArgumentParameter(args, i);

                        if (this.Core.Messaging.EncounteredError)
                        {
                            return;
                        }

                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).RootedDirectoryRef = dr;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).RootedDirectoryRef = dr;
                        }
                    }
                    else if ("gg" == truncatedCommandSwitch)
                    {
                        utilMutator.GenerateGuids = true;
                    }
                    else if ("g1" == truncatedCommandSwitch)
                    {
                        utilMutator.GuidFormat = "D";
                    }
                    else if ("ke" == truncatedCommandSwitch)
                    {
                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).KeepEmptyDirectories = true;
                        }
                        else if (active)
                        {
                            // TODO: error message - not applicable to file harvester
                        }
                    }
                    else if ("scom" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.SuppressCOMElements = true;
                        }
                        else
                        {
                            // TODO: error message - not applicable
                        }
                    }
                    else if ("svb6" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.SuppressVB6COMElements = true;
                        }
                        else
                        {
                            // TODO: error message - not applicable
                        }
                    }
                    else if ("sfrag" == truncatedCommandSwitch)
                    {
                        utilMutator.CreateFragments = false;
                    }
                    else if ("srd" == truncatedCommandSwitch)
                    {
                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).SuppressRootDirectory = true;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).SuppressRootDirectory = true;
                        }
                    }
                    else if ("sreg" == truncatedCommandSwitch)
                    {
                        suppressHarvestingRegistryValues = true;
                    }
                    else if ("suid" == truncatedCommandSwitch)
                    {
                        utilMutator.SetUniqueIdentifiers = false;

                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).SetUniqueIdentifiers = false;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).SetUniqueIdentifiers = false;
                        }
                    }
                    else if (truncatedCommandSwitch.StartsWith("t:", StringComparison.Ordinal) || "t" == truncatedCommandSwitch)
                    {
                        string xslFile;
                        if (truncatedCommandSwitch.StartsWith("t:", StringComparison.Ordinal))
                        {
                            this.Core.Messaging.Write(WarningMessages.DeprecatedCommandLineSwitch("t:", "t"));
                            xslFile = truncatedCommandSwitch.Substring(2);
                        }
                        else
                        {
                            xslFile = this.GetArgumentParameter(args, i, true);
                        }

                        if (0 <= xslFile.IndexOf('\"'))
                        {
                            this.Core.Messaging.Write(ErrorMessages.PathCannotContainQuote(xslFile));
                            return;
                        }

                        try
                        {
                            xslFile = Path.GetFullPath(xslFile);
                        }
                        catch (Exception e)
                        {
                            this.Core.Messaging.Write(ErrorMessages.InvalidCommandLineFileName(xslFile, e.Message));
                            return;
                        }

                        transformMutators.Add(new UtilTransformMutator(xslFile, transformMutators.Count));
                    }
                    else if (truncatedCommandSwitch.StartsWith("template:", StringComparison.Ordinal) || "template" == truncatedCommandSwitch)
                    {
                        string template;
                        if(truncatedCommandSwitch.StartsWith("template:", StringComparison.Ordinal))
                        {
                            this.Core.Messaging.Write(WarningMessages.DeprecatedCommandLineSwitch("template:", "template"));
                            template = truncatedCommandSwitch.Substring(9);
                        }
                        else
                        {
                            template = this.GetArgumentParameter(args, i);
                        }

                        switch (template)
                        {
                            case "fragment":
                                utilMutator.TemplateType = TemplateType.Fragment;
                                break;
                            case "module":
                                utilMutator.TemplateType = TemplateType.Module;
                                break;
                            case "product":
                                utilMutator.TemplateType = TemplateType.Package ;
                                break;
                            default:
                                // TODO: error
                                break;
                        }
                    }
                    else if ("var" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.PreprocessorVariable = this.GetArgumentParameter(args, i);

                            if (this.Core.Messaging.EncounteredError)
                            {
                                return;
                            }
                        }
                    }
                    else if ("generate" == truncatedCommandSwitch)
                    {
                        if (harvesterExtension is DirectoryHarvester)
                        {
                            string genType = this.GetArgumentParameter(args, i).ToUpperInvariant();
                            switch (genType)
                            {
                                case "COMPONENTS":
                                    generateType = GenerateType.Components;
                                    break;
                                case "PAYLOADGROUP":
                                    generateType = GenerateType.PayloadGroup;
                                    break;
                                default:
                                    throw new WixException(HarvesterErrors.InvalidDirectoryOutputType(genType));
                            }
                        }
                        else
                        {
                            // TODO: error message - not applicable
                        }
                    }
                }
            }

            // set the appropriate harvester extension
            if (active)
            {
                this.Core.Harvester.Extension = harvesterExtension;

                if (!suppressHarvestingRegistryValues)
                {
                    this.Core.Mutator.AddExtension(new UtilHarvesterMutator());
                }

                this.Core.Mutator.AddExtension(utilFinalizeHarvesterMutator);

                if (harvesterExtension is DirectoryHarvester directoryHarvester)
                {
                    directoryHarvester.GenerateType = generateType;
                    this.Core.Harvester.Core.RootDirectory = this.Core.Harvester.Core.ExtensionArgument;
                }
                else if (harvesterExtension is FileHarvester)
                {
                    if (((FileHarvester)harvesterExtension).SuppressRootDirectory)
                    {
                        this.Core.Harvester.Core.RootDirectory = Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument));
                    }
                    else
                    {
                        this.Core.Harvester.Core.RootDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument)));

                        // GetDirectoryName() returns null for root paths such as "c:\", so make sure to support that as well
                        if (null == this.Core.Harvester.Core.RootDirectory)
                        {
                            this.Core.Harvester.Core.RootDirectory = Path.GetPathRoot(Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument)));
                        }
                    }
                }
            }

            // set the mutator
            this.Core.Mutator.AddExtension(utilMutator);

            // add the transforms
            foreach (UtilTransformMutator transformMutator in transformMutators)
            {
                this.Core.Mutator.AddExtension(transformMutator);
            }
        }

        private string GetArgumentParameter(string[] args, int index)
        {
            return this.GetArgumentParameter(args, index, false);
        }

        private string GetArgumentParameter(string[] args, int index, bool allowSpaces)
        {
            string truncatedCommandSwitch = args[index];
            string commandSwitchValue = args[index + 1];
            
            //increment the index to the switch value
            index++;

            if (IsValidArg(args, index) && !String.IsNullOrEmpty(commandSwitchValue.Trim()))
            {
                if (!allowSpaces && commandSwitchValue.Contains(" "))
                {
                    this.Core.Messaging.Write(HarvesterErrors.SpacesNotAllowedInArgumentValue(truncatedCommandSwitch, commandSwitchValue));
                }
                else
                {
                    return commandSwitchValue;
                }
            }
            else
            {
                this.Core.Messaging.Write(HarvesterErrors.ArgumentRequiresValue(truncatedCommandSwitch));
            }

            return null;
        }
    }
}
