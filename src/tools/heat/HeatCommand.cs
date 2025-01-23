// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Harvesters.Extensibility;

    internal class HeatCommand : BaseCommandLineCommand
    {
        private bool showLogo;

        public HeatCommand(string harvestType, IList<IHeatExtension> extensions, IServiceProvider serviceProvider)
        {
            this.Extensions = extensions;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.ServiceProvider = serviceProvider;

            this.ExtensionType = harvestType;
            this.ExtensionOptions.Add(harvestType);
        }

        public override bool ShowLogo => this.showLogo;

        private string ExtensionArgument { get; set; }

        private List<string> ExtensionOptions { get; } = new List<string>();

        private string ExtensionType { get; }

        private IList<IHeatExtension> Extensions { get; }

        private int Indent { get; set; } = 4;

        private IMessaging Messaging { get; }

        private string OutputFile { get; set; }

        private IServiceProvider ServiceProvider { get; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return null;
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            var exitCode = this.Harvest();
            return Task.FromResult(exitCode);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string arg)
        {
            if (this.ExtensionArgument == null)
            {
                this.ExtensionArgument = arg;
            }
            else if ('-' == arg[0] || '/' == arg[0])
            {
                var parameter = arg.Substring(1);
                if ("nologo" == parameter)
                {
                    this.showLogo = false;
                }
                else if ("o" == parameter || "out" == parameter)
                {
                    this.OutputFile = parser.GetNextArgumentAsFilePathOrError(arg, "output source file");

                    if (String.IsNullOrEmpty(this.OutputFile))
                    {
                        return false;
                    }
                }
                else if ("swall" == parameter)
                {
                    this.Messaging.Write(WarningMessages.DeprecatedCommandLineSwitch("swall", "sw"));
                    this.Messaging.SuppressAllWarnings = true;
                }
                else if (parameter.StartsWith("sw"))
                {
                    var paramArg = parameter.Substring(2);
                    try
                    {
                        if (0 == paramArg.Length)
                        {
                            this.Messaging.SuppressAllWarnings = true;
                        }
                        else
                        {
                            var suppressWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                            if (0 >= suppressWarning)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalSuppressWarningId(paramArg));
                            }

                            this.Messaging.SuppressWarningMessage(suppressWarning);
                        }
                    }
                    catch (FormatException)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalSuppressWarningId(paramArg));
                    }
                    catch (OverflowException)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalSuppressWarningId(paramArg));
                    }
                }
                else if ("wxall" == parameter)
                {
                    this.Messaging.Write(WarningMessages.DeprecatedCommandLineSwitch("wxall", "wx"));
                    this.Messaging.WarningsAsError = true;
                }
                else if (parameter.StartsWith("wx"))
                {
                    var paramArg = parameter.Substring(2);
                    try
                    {
                        if (0 == paramArg.Length)
                        {
                            this.Messaging.WarningsAsError = true;
                        }
                        else
                        {
                            var elevateWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                            if (0 >= elevateWarning)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalWarningIdAsError(paramArg));
                            }

                            this.Messaging.ElevateWarningMessage(elevateWarning);
                        }
                    }
                    catch (FormatException)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalWarningIdAsError(paramArg));
                    }
                    catch (OverflowException)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalWarningIdAsError(paramArg));
                    }
                }
                else if ("v" == parameter)
                {
                    this.Messaging.ShowVerboseMessages = true;
                }
                else if ("indent" == parameter)
                {
                    try
                    {
                        this.Indent = Int32.Parse(parser.GetNextArgumentOrError(arg), CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid numeric argument.", parameter);
                    }
                }
            }

            this.ExtensionOptions.Add(arg);
            return true;
        }

        private int Harvest()
        {
            try
            {
                if (String.IsNullOrEmpty(this.ExtensionArgument))
                {
                    this.Messaging.Write(ErrorMessages.HarvestSourceNotSpecified());
                }
                else if (String.IsNullOrEmpty(this.OutputFile))
                {
                    this.Messaging.Write(ErrorMessages.OutputTargetNotSpecified());
                }

                // exit if there was an error parsing the core command line
                if (this.Messaging.EncounteredError)
                {
                    return this.Messaging.LastErrorNumber;
                }

                if (this.ShowLogo)
                {
                    HelpCommand.DisplayToolHeader();
                }

                var heatCore = new HeatCore(this.ServiceProvider, this.ExtensionArgument);

                // parse the extension's command line arguments
                var extensionOptionsArray = this.ExtensionOptions.ToArray();
                foreach (var heatExtension in this.Extensions)
                {
                    heatExtension.Core = heatCore;
                    heatExtension.ParseOptions(this.ExtensionType, extensionOptionsArray);
                }

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.Messaging.EncounteredError)
                {
                    return this.Messaging.LastErrorNumber;
                }

                // harvest the output
                var wix = heatCore.Harvester.Harvest(this.ExtensionArgument);
                if (null == wix)
                {
                    return this.Messaging.LastErrorNumber;
                }

                // mutate the output
                if (!heatCore.Mutator.Mutate(wix))
                {
                    return this.Messaging.LastErrorNumber;
                }

                var xmlSettings = new XmlWriterSettings();
                xmlSettings.Indent = true;
                xmlSettings.IndentChars = new string(' ', this.Indent);
                xmlSettings.OmitXmlDeclaration = true;

                string wixString;
                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                    {
                        wix.OutputXml(xmlWriter);
                    }

                    wixString = stringWriter.ToString();
                }

                var mutatedWixString = heatCore.Mutator.Mutate(wixString);
                if (String.IsNullOrEmpty(mutatedWixString))
                {
                    return this.Messaging.LastErrorNumber;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(this.OutputFile));

                using (var streamWriter = new StreamWriter(this.OutputFile, false, System.Text.Encoding.UTF8))
                {
                    using (var xmlWriter = XmlWriter.Create(streamWriter, xmlSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.Flush();
                    }

                    streamWriter.Write(mutatedWixString);
                }
            }
            catch (WixException we)
            {
                this.Messaging.Write(we.Error);
            }
            catch (Exception e)
            {
                this.Messaging.Write(ErrorMessages.UnexpectedException(e));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return this.Messaging.LastErrorNumber;
        }
    }
}
