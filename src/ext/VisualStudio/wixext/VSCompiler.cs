// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// The compiler for the WiX Toolset Visual Studio Extension.
    /// </summary>
    public sealed class VSCompiler : BaseCompilerExtension
    {
        internal const int MsidbCustomActionTypeExe = 0x00000002;  // Target = command line args
        internal const int MsidbCustomActionTypeProperty = 0x00000030;  // Source = full path to executable
        internal const int MsidbCustomActionTypeContinue = 0x00000040;  // ignore action return status; continue running
        internal const int MsidbCustomActionTypeRollback = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        internal const int MsidbCustomActionTypeInScript = 0x00000400;  // queue for execution within script
        internal const int MsidbCustomActionTypeNoImpersonate = 0x00000800;  // queue for not impersonating

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/vs";

        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    switch (element.Name.LocalName)
                    {
                        case "VsixPackage":
                            this.ParseVsixPackageElement(intermediate, section, element, context["ComponentId"], null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "File":
                    switch (element.Name.LocalName)
                    {
                        case "VsixPackage":
                            this.ParseVsixPackageElement(intermediate, section, element, context["ComponentId"], context["FileId"]);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Package":
                    switch (element.Name.LocalName)
                    {
                        case "FindVisualStudio":
                            this.ParseFindVisualStudioElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        private void ParseFindVisualStudioElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4VSFindInstances", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64);
        }

        private void ParseVsixPackageElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            var propertyId = "VS_VSIX_INSTALLER_PATH";
            string packageId = null;
            var permanent = YesNoType.NotSet;
            string target = null;
            string targetVersion = null;
            var vital = YesNoType.NotSet;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            if (String.IsNullOrEmpty(fileId))
                            {
                                fileId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "File", "File"));
                            }
                            break;
                        case "PackageId":
                            packageId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            permanent = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (target.ToLowerInvariant())
                            {
                                case "integrated":
                                case "integratedshell":
                                    target = "IntegratedShell";
                                    break;
                                case "professional":
                                    target = "Pro";
                                    break;
                                case "premium":
                                    target = "Premium";
                                    break;
                                case "ultimate":
                                    target = "Ultimate";
                                    break;
                                case "vbexpress":
                                    target = "VBExpress";
                                    break;
                                case "vcexpress":
                                    target = "VCExpress";
                                    break;
                                case "vcsexpress":
                                    target = "VCSExpress";
                                    break;
                                case "vwdexpress":
                                    target = "VWDExpress";
                                    break;
                            }
                            break;
                        case "TargetVersion":
                            targetVersion = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Vital":
                            vital = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VsixInstallerPathProperty":
                            propertyId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (String.IsNullOrEmpty(fileId))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            if (String.IsNullOrEmpty(packageId))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "PackageId"));
            }

            if (!String.IsNullOrEmpty(target) && String.IsNullOrEmpty(targetVersion))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "TargetVersion", "Target"));
            }
            else if (String.IsNullOrEmpty(target) && !String.IsNullOrEmpty(targetVersion))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Target", "TargetVersion"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                // Ensure there is a reference to the AppSearch Property that will find the VsixInstaller.exe.
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Property, propertyId);

                // Ensure there is a reference to the package file (even if we are a child under it).
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.File, fileId);

                var cmdlinePrefix = "/q ";

                if (!String.IsNullOrEmpty(target))
                {
                    cmdlinePrefix = String.Format("{0} /skuName:{1} /skuVersion:{2}", cmdlinePrefix, target, targetVersion);
                }

                var installAfter = "WriteRegistryValues"; // by default, come after the registry key registration.

                var installNamePerUser = this.ParseHelper.CreateIdentifier("viu", componentId, fileId, "per-user", target, targetVersion);
                var installNamePerMachine = this.ParseHelper.CreateIdentifier("vim", componentId, fileId, "per-machine", target, targetVersion);
                var installCmdLinePerUser = String.Format("{0} \"[#{1}]\"", cmdlinePrefix, fileId);
                var installCmdLinePerMachine = String.Concat(installCmdLinePerUser, " /admin");
                var installConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.
                var installConditionPerMachine = String.Format("ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.
                var installPerUserCA = new CustomActionSymbol(sourceLineNumbers, installNamePerUser)
                {
                    ExecutionType = CustomActionExecutionType.Deferred,
                    Impersonate = true,
                };
                var installPerMachineCA = new CustomActionSymbol(sourceLineNumbers, installNamePerMachine)
                {
                    ExecutionType = CustomActionExecutionType.Deferred,
                    Impersonate = false,
                };

                // If the package is not vital, mark the install action as continue.
                if (vital == YesNoType.No)
                {
                    installPerUserCA.IgnoreResult = true;
                    installPerMachineCA.IgnoreResult = true;
                }
                else // the package is vital so ensure there is a rollback action scheduled.
                {
                    var rollbackNamePerUser = this.ParseHelper.CreateIdentifier("vru", componentId, fileId, "per-user", target, targetVersion);
                    var rollbackNamePerMachine = this.ParseHelper.CreateIdentifier("vrm", componentId, fileId, "per-machine", target, targetVersion);
                    var rollbackCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    var rollbackCmdLinePerMachine = String.Concat(rollbackCmdLinePerUser, " /admin");
                    var rollbackConditionPerUser = String.Format("NOT ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.
                    var rollbackConditionPerMachine = String.Format("ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.
                    var rollbackPerUserCA = new CustomActionSymbol(sourceLineNumbers, rollbackNamePerUser)
                    {
                        ExecutionType = CustomActionExecutionType.Rollback,
                        IgnoreResult = true,
                        Impersonate = true,
                    };
                    var rollbackPerMachineCA = new CustomActionSymbol(sourceLineNumbers, rollbackNamePerMachine)
                    {
                        ExecutionType = CustomActionExecutionType.Rollback,
                        IgnoreResult = true,
                        Impersonate = false,
                    };

                    this.SchedulePropertyExeAction(section, sourceLineNumbers, rollbackNamePerUser, propertyId, rollbackCmdLinePerUser, rollbackPerUserCA, rollbackConditionPerUser, null, installAfter);
                    this.SchedulePropertyExeAction(section, sourceLineNumbers, rollbackNamePerMachine, propertyId, rollbackCmdLinePerMachine, rollbackPerMachineCA, rollbackConditionPerMachine, null, rollbackNamePerUser.Id);

                    installAfter = rollbackNamePerMachine.Id;
                }

                this.SchedulePropertyExeAction(section, sourceLineNumbers, installNamePerUser, propertyId, installCmdLinePerUser, installPerUserCA, installConditionPerUser, null, installAfter);
                this.SchedulePropertyExeAction(section, sourceLineNumbers, installNamePerMachine, propertyId, installCmdLinePerMachine, installPerMachineCA, installConditionPerMachine, null, installNamePerUser.Id);

                // If not permanent, schedule the uninstall custom action.
                if (permanent != YesNoType.Yes)
                {
                    var uninstallNamePerUser = this.ParseHelper.CreateIdentifier("vuu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    var uninstallNamePerMachine = this.ParseHelper.CreateIdentifier("vum", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    var uninstallCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    var uninstallCmdLinePerMachine = String.Concat(uninstallCmdLinePerUser, " /admin");
                    var uninstallConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.
                    var uninstallConditionPerMachine = String.Format("ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.
                    var uninstallPerUserCA = new CustomActionSymbol(sourceLineNumbers, uninstallNamePerUser)
                    {
                        ExecutionType = CustomActionExecutionType.Deferred,
                        IgnoreResult = true,
                        Impersonate = true,
                    };
                    var uninstallPerMachineCA = new CustomActionSymbol(sourceLineNumbers, uninstallNamePerMachine)
                    {
                        ExecutionType = CustomActionExecutionType.Deferred,
                        IgnoreResult = true,
                        Impersonate = false,
                    };

                    this.SchedulePropertyExeAction(section, sourceLineNumbers, uninstallNamePerUser, propertyId, uninstallCmdLinePerUser, uninstallPerUserCA, uninstallConditionPerUser, "InstallFinalize", null);
                    this.SchedulePropertyExeAction(section, sourceLineNumbers, uninstallNamePerMachine, propertyId, uninstallCmdLinePerMachine, uninstallPerMachineCA, uninstallConditionPerMachine, "InstallFinalize", null);
                }
            }
        }

        private void SchedulePropertyExeAction(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier name, string source, string cmdline, CustomActionSymbol caTemplate, string condition, string beforeAction, string afterAction)
        {
            const SequenceTable sequence = SequenceTable.InstallExecuteSequence;

            caTemplate.SourceType = CustomActionSourceType.Property;
            caTemplate.Source = source;
            caTemplate.TargetType = CustomActionTargetType.Exe;
            caTemplate.Target = cmdline;
            section.AddSymbol(caTemplate);

            section.AddSymbol(new WixActionSymbol(sourceLineNumbers, new Identifier(name.Access, sequence, name.Id))
            {
                SequenceTable = SequenceTable.InstallExecuteSequence,
                Action = name.Id,
                Condition = condition,
                // no explicit sequence
                Before = beforeAction,
                After = afterAction,
                Overridable = false,
            });

            if (null != beforeAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixAction, sequence.ToString(), beforeAction);
                }
                else
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, beforeAction);
                }
            }

            if (null != afterAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(afterAction))
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixAction, sequence.ToString(), afterAction);
                }
                else
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, afterAction);
                }
            }
        }
    }
}
