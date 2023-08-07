// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Set sequence numbers for all the actions and create symbols in the output object.
    /// </summary>
    internal class SequenceActionsCommand
    {
        public SequenceActionsCommand(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;

            this.RelativeActionsForActions = new Dictionary<string, RelativeActions>();
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        private Dictionary<string, RelativeActions> RelativeActionsForActions { get; }

        public void Execute()
        {
            var requiredActionSymbols = new Dictionary<string, WixActionSymbol>();

            // Index all the action symbols and look for collisions.
            foreach (var actionSymbol in this.Section.Symbols.OfType<WixActionSymbol>())
            {
                if (actionSymbol.Overridable) // overridable action
                {
                    if (requiredActionSymbols.TryGetValue(actionSymbol.Id.Id, out var collidingActionSymbol))
                    {
                        if (collidingActionSymbol.Overridable)
                        {
                            this.Messaging.Write(ErrorMessages.OverridableActionCollision(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                            if (null != collidingActionSymbol.SourceLineNumbers)
                            {
                                this.Messaging.Write(ErrorMessages.OverridableActionCollision2(collidingActionSymbol.SourceLineNumbers));
                            }
                        }
                    }
                    else
                    {
                        requiredActionSymbols.Add(actionSymbol.Id.Id, actionSymbol);
                    }
                }
                else // unsequenced or sequenced action.
                {
                    // Unsequenced action (allowed for certain standard actions).
                    if (null == actionSymbol.Before && null == actionSymbol.After && !actionSymbol.Sequence.HasValue)
                    {
                        if (WindowsInstallerStandard.TryGetStandardAction(actionSymbol.Id.Id, out var standardAction))
                        {
                            // Populate the sequence from the standard action
                            actionSymbol.Sequence = standardAction.Sequence;
                        }
                        else // not a supported unscheduled action.
                        {
                            throw new WixException($"Found action '{actionSymbol.Id.Id}' at {actionSymbol.SourceLineNumbers}' with no Sequence, Before, or After column set. The compiler should have prevented this.");
                        }
                    }

                    if (requiredActionSymbols.TryGetValue(actionSymbol.Id.Id, out var collidingActionSymbol) && !collidingActionSymbol.Overridable)
                    {
                        this.Messaging.Write(ErrorMessages.ActionCollision(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                        if (null != collidingActionSymbol.SourceLineNumbers)
                        {
                            this.Messaging.Write(ErrorMessages.ActionCollision2(collidingActionSymbol.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        requiredActionSymbols[actionSymbol.Id.Id] = actionSymbol;
                    }
                }
            }

            // Get the standard actions required based on symbols in the section.
            var requiredStandardActions = this.GetRequiredStandardActions();

            // Add the overridable action symbols that are not overridden to the required action symbols.
            foreach (var actionSymbol in requiredStandardActions.Values)
            {
                if (!requiredActionSymbols.ContainsKey(actionSymbol.Id.Id))
                {
                    requiredActionSymbols.Add(actionSymbol.Id.Id, actionSymbol);
                }
            }

            // Suppress the required actions that are overridable.
            foreach (var suppressActionSymbol in this.Section.Symbols.OfType<WixSuppressActionSymbol>())
            {
                var key = suppressActionSymbol.Id.Id;

                // If there is an overridable symbol to suppress; suppress it. There is no warning if there
                // is no action to suppress because the action may be suppressed from a merge module in
                // the binder.
                if (requiredActionSymbols.TryGetValue(key, out var requiredActionSymbol))
                {
                    if (requiredActionSymbol.Overridable)
                    {
                        this.Messaging.Write(WarningMessages.SuppressAction(suppressActionSymbol.SourceLineNumbers, suppressActionSymbol.Action, suppressActionSymbol.SequenceTable.ToString()));
                        if (null != requiredActionSymbol.SourceLineNumbers)
                        {
                            this.Messaging.Write(WarningMessages.SuppressAction2(requiredActionSymbol.SourceLineNumbers));
                        }

                        requiredActionSymbols.Remove(key);
                    }
                    else // suppressing a non-overridable action symbol
                    {
                        this.Messaging.Write(ErrorMessages.SuppressNonoverridableAction(suppressActionSymbol.SourceLineNumbers, suppressActionSymbol.SequenceTable.ToString(), suppressActionSymbol.Action));
                        if (null != requiredActionSymbol.SourceLineNumbers)
                        {
                            this.Messaging.Write(ErrorMessages.SuppressNonoverridableAction2(requiredActionSymbol.SourceLineNumbers));
                        }
                    }
                }
            }

            // A dictionary used for detecting cyclic references among action symbols.
            var firstReference = new Dictionary<WixActionSymbol, WixActionSymbol>();
            
            // Build up dependency trees of the relatively scheduled actions.
            // Use ToList() to create a copy of the required action symbols so that new symbols can
            // be added while enumerating.
            foreach (var actionSymbol in requiredActionSymbols.Values.ToList())
            {
                if (!actionSymbol.Sequence.HasValue)
                {
                    // check for standard actions that don't have a sequence number in a merge module
                    if (SectionType.Module == this.Section.Type && WindowsInstallerStandard.IsStandardAction(actionSymbol.Action))
                    {
                        this.Messaging.Write(ErrorMessages.StandardActionRelativelyScheduledInModule(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                    }

                    this.SequenceActionSymbol(actionSymbol, requiredActionSymbols, firstReference);
                }
                else if (SectionType.Module == this.Section.Type && 0 < actionSymbol.Sequence && !WindowsInstallerStandard.IsStandardAction(actionSymbol.Action)) // check for custom actions and dialogs that have a sequence number
                {
                    this.Messaging.Write(ErrorMessages.CustomActionSequencedInModule(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action));
                }
            }

            // Look for standard actions with sequence restrictions that aren't necessarily scheduled based
            // on the presence of a particular table.
            if (requiredActionSymbols.ContainsKey("InstallExecuteSequence/DuplicateFiles") && !requiredActionSymbols.ContainsKey("InstallExecuteSequence/InstallFiles"))
            {
                WindowsInstallerStandard.TryGetStandardAction("InstallExecuteSequence/InstallFiles", out var standardAction);
                requiredActionSymbols.Add(standardAction.Id.Id, standardAction);
            }

            // Schedule actions.
            List<WixActionSymbol> scheduledActionSymbols;
            if (SectionType.Module == this.Section.Type)
            {
                scheduledActionSymbols = requiredActionSymbols.Values.ToList();
            }
            else
            {
                scheduledActionSymbols = this.ScheduleActions(requiredActionSymbols);
            }

            // Remove all existing WixActionSymbols from the section then add the
            // scheduled actions back to the section.
            var removeActionSymbols = this.Section.Symbols.Where(s => s.Definition.Type == SymbolDefinitionType.WixAction).ToList();

            foreach (var removeSymbol in removeActionSymbols)
            {
                this.Section.RemoveSymbol(removeSymbol);
            }

            foreach (var action in scheduledActionSymbols)
            {
                this.Section.AddSymbol(action);
            }
        }

        private Dictionary<string, WixActionSymbol> GetRequiredStandardActions()
        {
            var overridableActionSymbols = new Dictionary<string, WixActionSymbol>();

            var requiredActionIds = this.GetRequiredActionIds();

            foreach (var actionId in requiredActionIds)
            {
                WindowsInstallerStandard.TryGetStandardAction(actionId, out var standardAction);
                overridableActionSymbols.Add(standardAction.Id.Id, standardAction);
            }

            return overridableActionSymbols;
        }

        private List<WixActionSymbol> ScheduleActions(Dictionary<string, WixActionSymbol> requiredActionSymbols)
        {
            var scheduledActionSymbols = new List<WixActionSymbol>();

            // Process each sequence table individually.
            foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
            {
                // Create a collection of just the action symbols in this sequence
                var sequenceActionSymbols = requiredActionSymbols.Values.Where(a => a.SequenceTable == sequenceTable).ToList();

                // Schedule the absolutely scheduled actions (by sorting them by their sequence numbers).
                var absoluteActionSymbols = new List<WixActionSymbol>();
                foreach (var actionSymbol in sequenceActionSymbols)
                {
                    if (actionSymbol.Sequence.HasValue)
                    {
                        // Look for sequence number collisions
                        foreach (var sequenceScheduledActionSymbol in absoluteActionSymbols)
                        {
                            if (sequenceScheduledActionSymbol.Sequence == actionSymbol.Sequence)
                            {
                                this.Messaging.Write(WarningMessages.ActionSequenceCollision(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action, sequenceScheduledActionSymbol.Action, actionSymbol.Sequence ?? 0));
                                if (null != sequenceScheduledActionSymbol.SourceLineNumbers)
                                {
                                    this.Messaging.Write(WarningMessages.ActionSequenceCollision2(sequenceScheduledActionSymbol.SourceLineNumbers));
                                }
                            }
                        }

                        absoluteActionSymbols.Add(actionSymbol);
                    }
                }

                absoluteActionSymbols.Sort((x, y) => (x.Sequence ?? 0).CompareTo(y.Sequence ?? 0));

                // Schedule the relatively scheduled actions (by resolving the dependency trees).
                var previousUsedSequence = 0;
                var relativeActionSymbols = new List<WixActionSymbol>();
                for (int j = 0; j < absoluteActionSymbols.Count; j++)
                {
                    var absoluteActionSymbol = absoluteActionSymbols[j];

                    // Get all the relatively scheduled action symbols occuring before and after this absolutely scheduled action symbol.
                    var relativeActions = this.GetAllRelativeActionsForSequenceType(sequenceTable, absoluteActionSymbol);

                    // Check for relatively scheduled actions occuring before/after a special action
                    // (those actions with a negative sequence number).
                    if (absoluteActionSymbol.Sequence < 0 && (relativeActions.PreviousActions.Any() || relativeActions.NextActions.Any()))
                    {
                        // Create errors for all the before actions.
                        foreach (var actionSymbol in relativeActions.PreviousActions)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action, absoluteActionSymbol.Action));
                        }

                        // Create errors for all the after actions.
                        foreach (var actionSymbol in relativeActions.NextActions)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction(actionSymbol.SourceLineNumbers, actionSymbol.SequenceTable.ToString(), actionSymbol.Action, absoluteActionSymbol.Action));
                        }

                        // If there is source line information for the absolutely scheduled action display it
                        if (absoluteActionSymbol.SourceLineNumbers != null)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction2(absoluteActionSymbol.SourceLineNumbers));
                        }

                        continue;
                    }

                    // Schedule the action symbols before this one.
                    var unusedSequence = absoluteActionSymbol.Sequence - 1;
                    for (var i = relativeActions.PreviousActions.Count - 1; i >= 0; i--)
                    {
                        var relativeActionSymbol = relativeActions.PreviousActions[i];

                        // look for collisions
                        if (unusedSequence == previousUsedSequence)
                        {
                            this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber(relativeActionSymbol.SourceLineNumbers, relativeActionSymbol.SequenceTable.ToString(), relativeActionSymbol.Action, absoluteActionSymbol.Action));
                            if (absoluteActionSymbol.SourceLineNumbers != null)
                            {
                                this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber2(absoluteActionSymbol.SourceLineNumbers));
                            }

                            unusedSequence++;
                        }

                        relativeActionSymbol.Sequence = unusedSequence;
                        relativeActionSymbols.Add(relativeActionSymbol);

                        unusedSequence--;
                    }

                    // Determine the next used action sequence number.
                    var nextUsedSequence = Int16.MaxValue + 1;
                    if (absoluteActionSymbols.Count > j + 1)
                    {
                        nextUsedSequence = absoluteActionSymbols[j + 1].Sequence ?? 0;
                    }

                    // Schedule the action symbols after this one.
                    unusedSequence = absoluteActionSymbol.Sequence + 1;
                    for (var i = 0; i < relativeActions.NextActions.Count; i++)
                    {
                        var relativeActionSymbol = relativeActions.NextActions[i];

                        if (unusedSequence == nextUsedSequence)
                        {
                            this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber(relativeActionSymbol.SourceLineNumbers, relativeActionSymbol.SequenceTable.ToString(), relativeActionSymbol.Action, absoluteActionSymbol.Action));
                            if (absoluteActionSymbol.SourceLineNumbers != null)
                            {
                                this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber2(absoluteActionSymbol.SourceLineNumbers));
                            }

                            unusedSequence--;
                        }

                        relativeActionSymbol.Sequence = unusedSequence;
                        relativeActionSymbols.Add(relativeActionSymbol);

                        unusedSequence++;
                    }

                    // keep track of this sequence number as the previous used sequence number for the next iteration
                    previousUsedSequence = absoluteActionSymbol.Sequence ?? 0;
                }

                // add the absolutely and relatively scheduled actions to the list of scheduled actions
                scheduledActionSymbols.AddRange(absoluteActionSymbols);
                scheduledActionSymbols.AddRange(relativeActionSymbols);
            }

            return scheduledActionSymbols;
        }

        private IEnumerable<string> GetRequiredActionIds()
        {
            var set = new HashSet<string>();

            // gather the required actions for the output type
            if (SectionType.Package == this.Section.Type)
            {
                // AdminExecuteSequence table
                set.Add("AdminExecuteSequence/CostFinalize");
                set.Add("AdminExecuteSequence/CostInitialize");
                set.Add("AdminExecuteSequence/FileCost");
                set.Add("AdminExecuteSequence/InstallAdminPackage");
                set.Add("AdminExecuteSequence/InstallFiles");
                set.Add("AdminExecuteSequence/InstallFinalize");
                set.Add("AdminExecuteSequence/InstallInitialize");
                set.Add("AdminExecuteSequence/InstallValidate");

                // AdminUISequence table
                set.Add("AdminUISequence/CostFinalize");
                set.Add("AdminUISequence/CostInitialize");
                set.Add("AdminUISequence/ExecuteAction");
                set.Add("AdminUISequence/FileCost");

                // AdvtExecuteSequence table
                set.Add("AdvertiseExecuteSequence/CostFinalize");
                set.Add("AdvertiseExecuteSequence/CostInitialize");
                set.Add("AdvertiseExecuteSequence/InstallInitialize");
                set.Add("AdvertiseExecuteSequence/InstallFinalize");
                set.Add("AdvertiseExecuteSequence/InstallValidate");
                set.Add("AdvertiseExecuteSequence/PublishFeatures");
                set.Add("AdvertiseExecuteSequence/PublishProduct");

                // InstallExecuteSequence table
                set.Add("InstallExecuteSequence/CostFinalize");
                set.Add("InstallExecuteSequence/CostInitialize");
                set.Add("InstallExecuteSequence/FileCost");
                set.Add("InstallExecuteSequence/InstallFinalize");
                set.Add("InstallExecuteSequence/InstallInitialize");
                set.Add("InstallExecuteSequence/InstallValidate");
                set.Add("InstallExecuteSequence/ProcessComponents");
                set.Add("InstallExecuteSequence/PublishFeatures");
                set.Add("InstallExecuteSequence/PublishProduct");
                set.Add("InstallExecuteSequence/RegisterProduct");
                set.Add("InstallExecuteSequence/RegisterUser");
                set.Add("InstallExecuteSequence/UnpublishFeatures");
                set.Add("InstallExecuteSequence/ValidateProductID");

                // InstallUISequence table
                set.Add("InstallUISequence/CostFinalize");
                set.Add("InstallUISequence/CostInitialize");
                set.Add("InstallUISequence/ExecuteAction");
                set.Add("InstallUISequence/FileCost");
                set.Add("InstallUISequence/ValidateProductID");
            }

            // Gather the required actions for each symbol type.
            foreach (var symbolType in this.Section.Symbols.Select(t => t.Definition.Type).Distinct())
            {
                switch (symbolType)
                {
                    case SymbolDefinitionType.AppSearch:
                        set.Add("InstallExecuteSequence/AppSearch");
                        set.Add("InstallUISequence/AppSearch");
                        break;
                    case SymbolDefinitionType.CCPSearch:
                        set.Add("InstallExecuteSequence/AppSearch");
                        set.Add("InstallExecuteSequence/CCPSearch");
                        set.Add("InstallExecuteSequence/RMCCPSearch");
                        set.Add("InstallUISequence/AppSearch");
                        set.Add("InstallUISequence/CCPSearch");
                        set.Add("InstallUISequence/RMCCPSearch");
                        break;
                    case SymbolDefinitionType.Class:
                        set.Add("AdvertiseExecuteSequence/RegisterClassInfo");
                        set.Add("InstallExecuteSequence/RegisterClassInfo");
                        set.Add("InstallExecuteSequence/UnregisterClassInfo");
                        break;
                    case SymbolDefinitionType.Complus:
                        set.Add("InstallExecuteSequence/RegisterComPlus");
                        set.Add("InstallExecuteSequence/UnregisterComPlus");
                        break;
                    case SymbolDefinitionType.Component:
                    case SymbolDefinitionType.CreateFolder:
                        set.Add("InstallExecuteSequence/CreateFolders");
                        set.Add("InstallExecuteSequence/RemoveFolders");
                        break;
                    case SymbolDefinitionType.DuplicateFile:
                        set.Add("InstallExecuteSequence/DuplicateFiles");
                        set.Add("InstallExecuteSequence/RemoveDuplicateFiles");
                        break;
                    case SymbolDefinitionType.Environment:
                        set.Add("InstallExecuteSequence/WriteEnvironmentStrings");
                        set.Add("InstallExecuteSequence/RemoveEnvironmentStrings");
                        break;
                    case SymbolDefinitionType.Extension:
                        set.Add("AdvertiseExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/UnregisterExtensionInfo");
                        break;
                    case SymbolDefinitionType.File:
                        set.Add("InstallExecuteSequence/InstallFiles");
                        set.Add("InstallExecuteSequence/RemoveFiles");

                        var foundFont = false;
                        var foundSelfReg = false;
                        var foundBindPath = false;
                        foreach (var file in this.Section.Symbols.OfType<FileSymbol>())
                        {
                            // Note that TrueType fonts are denoted by the empty string in the FontTitle
                            // field. So, non-null means a font is present.
                            if (!foundFont && file.FontTitle != null)
                            {
                                set.Add("InstallExecuteSequence/RegisterFonts");
                                set.Add("InstallExecuteSequence/UnregisterFonts");
                                foundFont = true;
                            }

                            if (!foundSelfReg && file.SelfRegCost.HasValue)
                            {
                                set.Add("InstallExecuteSequence/SelfRegModules");
                                set.Add("InstallExecuteSequence/SelfUnregModules");
                                foundSelfReg = true;
                            }

                            if (!foundBindPath && !String.IsNullOrEmpty(file.BindPath))
                            {
                                set.Add("InstallExecuteSequence/BindImage");
                                foundBindPath = true;
                            }
                        }
                        break;
                    case SymbolDefinitionType.IniFile:
                        set.Add("InstallExecuteSequence/WriteIniValues");
                        set.Add("InstallExecuteSequence/RemoveIniValues");
                        break;
                    case SymbolDefinitionType.IsolatedComponent:
                        set.Add("InstallExecuteSequence/IsolateComponents");
                        break;
                    case SymbolDefinitionType.LaunchCondition:
                        set.Add("InstallExecuteSequence/LaunchConditions");
                        set.Add("InstallUISequence/LaunchConditions");
                        break;
                    case SymbolDefinitionType.MIME:
                        set.Add("AdvertiseExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/UnregisterMIMEInfo");
                        break;
                    case SymbolDefinitionType.MoveFile:
                        set.Add("InstallExecuteSequence/MoveFiles");
                        break;
                    case SymbolDefinitionType.Assembly:
                        set.Add("AdvertiseExecuteSequence/MsiPublishAssemblies");
                        set.Add("InstallExecuteSequence/MsiPublishAssemblies");
                        set.Add("InstallExecuteSequence/MsiUnpublishAssemblies");
                        break;
                    case SymbolDefinitionType.MsiServiceConfig:
                    case SymbolDefinitionType.MsiServiceConfigFailureActions:
                        set.Add("InstallExecuteSequence/MsiConfigureServices");
                        break;
                    case SymbolDefinitionType.ODBCDataSource:
                    case SymbolDefinitionType.ODBCTranslator:
                    case SymbolDefinitionType.ODBCDriver:
                        set.Add("InstallExecuteSequence/SetODBCFolders");
                        set.Add("InstallExecuteSequence/InstallODBC");
                        set.Add("InstallExecuteSequence/RemoveODBC");
                        break;
                    case SymbolDefinitionType.ProgId:
                        set.Add("AdvertiseExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/UnregisterProgIdInfo");
                        break;
                    case SymbolDefinitionType.PublishComponent:
                        set.Add("AdvertiseExecuteSequence/PublishComponents");
                        set.Add("InstallExecuteSequence/PublishComponents");
                        set.Add("InstallExecuteSequence/UnpublishComponents");
                        break;
                    case SymbolDefinitionType.Registry:
                    case SymbolDefinitionType.RemoveRegistry:
                        set.Add("InstallExecuteSequence/WriteRegistryValues");
                        set.Add("InstallExecuteSequence/RemoveRegistryValues");
                        break;
                    case SymbolDefinitionType.RemoveFile:
                        set.Add("InstallExecuteSequence/RemoveFiles");
                        break;
                    case SymbolDefinitionType.ServiceControl:
                        set.Add("InstallExecuteSequence/StartServices");
                        set.Add("InstallExecuteSequence/StopServices");
                        set.Add("InstallExecuteSequence/DeleteServices");
                        break;
                    case SymbolDefinitionType.ServiceInstall:
                        set.Add("InstallExecuteSequence/InstallServices");
                        break;
                    case SymbolDefinitionType.Shortcut:
                        set.Add("AdvertiseExecuteSequence/CreateShortcuts");
                        set.Add("InstallExecuteSequence/CreateShortcuts");
                        set.Add("InstallExecuteSequence/RemoveShortcuts");
                        break;
                    case SymbolDefinitionType.TypeLib:
                        set.Add("InstallExecuteSequence/RegisterTypeLibraries");
                        set.Add("InstallExecuteSequence/UnregisterTypeLibraries");
                        break;
                    case SymbolDefinitionType.Upgrade:
                        set.Add("InstallExecuteSequence/FindRelatedProducts");
                        set.Add("InstallUISequence/FindRelatedProducts");

                        // Only add the MigrateFeatureStates action if MigrateFeature attribute is set on
                        // at least one UpgradeVersion element.
                        if (this.Section.Symbols.OfType<UpgradeSymbol>().Any(t => t.MigrateFeatures))
                        {
                            set.Add("InstallExecuteSequence/MigrateFeatureStates");
                            set.Add("InstallUISequence/MigrateFeatureStates");
                        }
                        break;
                }
            }

            return set;
        }

        /// <summary>
        /// Sequence an action before or after a standard action.
        /// </summary>
        /// <param name="actionSymbol">The action symbol to be sequenced.</param>
        /// <param name="requiredActionSymbols">Collection of actions which must be included.</param>
        /// <param name="firstReference">A dictionary used for detecting cyclic references among action symbols.</param>
        private void SequenceActionSymbol(WixActionSymbol actionSymbol, Dictionary<string, WixActionSymbol> requiredActionSymbols, Dictionary<WixActionSymbol, WixActionSymbol> firstReference)
        {
            var after = false;

            if (actionSymbol.After != null)
            {
                after = true;
            }
            else if (actionSymbol.Before == null)
            {
                throw new WixException($"Found action '{actionSymbol.Id.Id}' at {actionSymbol.SourceLineNumbers}' with no Sequence, Before, or After column set. The compiler should have prevented this.");
            }

            var parentActionName = (after ? actionSymbol.After : actionSymbol.Before);
            var parentActionKey = actionSymbol.SequenceTable.ToString() + "/" + parentActionName;

            if (!requiredActionSymbols.TryGetValue(parentActionKey, out var parentActionSymbol))
            {
                // If the missing parent action is a standard action (with a suggested sequence number), add it.
                if (WindowsInstallerStandard.TryGetStandardAction(parentActionKey, out parentActionSymbol))
                {
                    // Create a clone to avoid modifying the static copy of the object.
                    // TODO: consider this: parentActionSymbol = parentActionSymbol.Clone();

                    requiredActionSymbols.Add(parentActionSymbol.Id.Id, parentActionSymbol);
                }
                else
                {
                    throw new WixException($"Found action {actionSymbol.Id.Id} with a non-existent {(after ? "After" : "Before")} action '{parentActionName}'. The linker should have prevented this.");
                }
            }

            this.CheckForCircularActionReference(actionSymbol, requiredActionSymbols, firstReference);

            // Add this action to the appropriate list of dependent action symbols.
            var relativeActions = this.GetRelativeActions(parentActionSymbol);
            var relatedSymbols = (after ? relativeActions.NextActions : relativeActions.PreviousActions);
            relatedSymbols.Add(actionSymbol);
        }

        /// <summary>
        /// Check the specified action symbol to see if it leads to a cycle.
        /// </summary>
        /// <para> Use the provided dictionary to note the initial action symbol that first led to each action
        /// symbol. Any action symbol encountered that has already been encountered starting from a different
        /// initial action symbol inherits the loop characteristics of that initial action symbol, and thus is
        /// also not part of a cycle. However, any action symbol encountered that has already been encountered
        /// starting from the same initial action symbol is an indication that the current action symbol is
        /// part of a cycle.
        /// </para>
        /// <param name="actionSymbol">The action symbol to be checked.</param>
        /// <param name="requiredActionSymbols">Collection of actions which must be included.</param>
        /// <param name="firstReference">The first encountered action symbol that led to each action symbol.</param>
        private void CheckForCircularActionReference(WixActionSymbol actionSymbol, Dictionary<string, WixActionSymbol> requiredActionSymbols, Dictionary<WixActionSymbol, WixActionSymbol> firstReference)
        {
            WixActionSymbol currentActionSymbol = null;
            var parentActionSymbol = actionSymbol;

            do
            {
                var previousActionSymbol = currentActionSymbol ?? parentActionSymbol;
                currentActionSymbol = parentActionSymbol;

                if (!firstReference.TryGetValue(currentActionSymbol, out var existingInitialActionSymbol))
                {
                    firstReference[currentActionSymbol] = actionSymbol;
                }
                else if (existingInitialActionSymbol == actionSymbol)
                {
                    this.Messaging.Write(ErrorMessages.ActionCircularDependency(currentActionSymbol.SourceLineNumbers, currentActionSymbol.SequenceTable.ToString(), currentActionSymbol.Action, previousActionSymbol.Action));
                }

                parentActionSymbol = this.GetParentActionSymbol(currentActionSymbol, requiredActionSymbols);
            } while (null != parentActionSymbol && !this.Messaging.EncounteredError);
        }

        /// <summary>
        /// Get the action symbol that is the parent of the given action symbol.
        /// </summary>
        /// <param name="actionSymbol">The given action symbol.</param>
        /// <param name="requiredActionSymbols">Collection of actions which must be included.</param>
        /// <returns>Null if there is no parent. Used for loop termination.</returns>
        private WixActionSymbol GetParentActionSymbol(WixActionSymbol actionSymbol, Dictionary<string, WixActionSymbol> requiredActionSymbols)
        {
            if (null == actionSymbol.Before && null == actionSymbol.After)
            {
                return null;
            }

            var parentActionKey = actionSymbol.SequenceTable.ToString() + "/" + (actionSymbol.After ?? actionSymbol.Before);

            if (!requiredActionSymbols.TryGetValue(parentActionKey, out var parentActionSymbol))
            {
                WindowsInstallerStandard.TryGetStandardAction(parentActionKey, out parentActionSymbol);
            }

            return parentActionSymbol;
        }
    
        
        private RelativeActions GetRelativeActions(WixActionSymbol action)
        {
            if (!this.RelativeActionsForActions.TryGetValue(action.Id.Id, out var relativeActions))
            {
                relativeActions = new RelativeActions();
                this.RelativeActionsForActions.Add(action.Id.Id, relativeActions);
            }

            return relativeActions;
        }

        private RelativeActions GetAllRelativeActionsForSequenceType(SequenceTable sequenceType, WixActionSymbol action)
        {
            var relativeActions = new RelativeActions();

            if (this.RelativeActionsForActions.TryGetValue(action.Id.Id, out var actionRelatives))
            {
                this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.PreviousActions, relativeActions.PreviousActions);

                this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.NextActions, relativeActions.NextActions);
            }

            return relativeActions;
        }

        private void RecurseRelativeActionsForSequenceType(SequenceTable sequenceType, List<WixActionSymbol> actions, List<WixActionSymbol> visitedActions)
        {
            foreach (var action in actions.Where(a => a.SequenceTable == sequenceType))
            {
                if (this.RelativeActionsForActions.TryGetValue(action.Id.Id, out var actionRelatives))
                {
                    this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.PreviousActions, visitedActions);
                }

                visitedActions.Add(action);

                if (actionRelatives != null)
                {
                    this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.NextActions, visitedActions);
                }
            }
        }

        private class RelativeActions
        {
            public List<WixActionSymbol> PreviousActions { get; } = new List<WixActionSymbol>();

            public List<WixActionSymbol> NextActions { get; } = new List<WixActionSymbol>();
        }
    }
}
