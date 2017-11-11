// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    internal class SequenceActionsCommand
    {
        public SequenceActionsCommand(IntermediateSection section)
        {
            this.Section = section;

            this.RelativeActionsForActions = new Dictionary<string, RelativeActions>();

            this.StandardActionsById = WindowsInstallerStandard.StandardActions().ToDictionary(a => a.Id.Id);
        }

        private IntermediateSection Section { get; }

        private Dictionary<string, RelativeActions> RelativeActionsForActions { get; }

        private Dictionary<string, WixActionTuple> StandardActionsById { get; }

        public Messaging Messaging { private get; set; }

        public void Execute()
        {
            var actions = this.Section.Tuples.OfType<WixActionTuple>().ToList();
            var suppressActions = this.Section.Tuples.OfType<WixSuppressActionTuple>().ToList();

            this.SequenceActions(actions, suppressActions);
        }

        /// <summary>
        /// Set sequence numbers for all the actions and create rows in the output object.
        /// </summary>
        /// <param name="actionRows">Collection of actions to schedule.</param>
        /// <param name="suppressActionRows">Collection of actions to suppress.</param>
        private void SequenceActions(List<WixActionTuple> actionRows, List<WixSuppressActionTuple> suppressActionRows)
        {
            var overridableActionRows = new Dictionary<string, WixActionTuple>();
            var requiredActionRows = new Dictionary<string, WixActionTuple>();

            // Get the standard actions required based on tuples in the section.
            var requiredActionIds = this.GetRequiredActionIds();

            foreach (var actionId in requiredActionIds)
            {
                var standardAction = this.StandardActionsById[actionId];

                overridableActionRows.Add(standardAction.Id.Id, standardAction);
            }

            // Index all the action rows and look for collisions.
            foreach (var actionRow in this.Section.Tuples.OfType<WixActionTuple>())
            {
                if (actionRow.Overridable) // overridable action
                {
                    if (overridableActionRows.TryGetValue(actionRow.Id.Id, out var collidingActionRow))
                    {
                        this.Messaging.OnMessage(WixErrors.OverridableActionCollision(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                        if (null != collidingActionRow.SourceLineNumbers)
                        {
                            this.Messaging.OnMessage(WixErrors.OverridableActionCollision2(collidingActionRow.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        overridableActionRows.Add(actionRow.Id.Id, actionRow);
                    }
                }
                else // unsequenced or sequenced action.
                {
                    // Unsequenced action (allowed for certain standard actions).
                    if (null == actionRow.Before && null == actionRow.After && 0 == actionRow.Sequence)
                    {
                        if (this.StandardActionsById.TryGetValue(actionRow.Id.Id, out var standardAction))
                        {
                            // Populate the sequence from the standard action
                            actionRow.Sequence = standardAction.Sequence;
                        }
                        else // not a supported unscheduled action.
                        {
                            throw new InvalidOperationException(WixStrings.EXP_FoundActionRowWithNoSequenceBeforeOrAfterColumnSet);
                        }
                    }

                    if (overridableActionRows.TryGetValue(actionRow.Id.Id, out var collidingActionRow))
                    {
                        this.Messaging.OnMessage(WixErrors.ActionCollision(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                        if (null != collidingActionRow.SourceLineNumbers)
                        {
                            this.Messaging.OnMessage(WixErrors.ActionCollision2(collidingActionRow.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        requiredActionRows.Add(actionRow.Id.Id, actionRow);
                    }
                }
            }

            // Add the overridable action rows that are not overridden to the required action rows.
            foreach (var actionRow in overridableActionRows.Values)
            {
                if (!requiredActionRows.ContainsKey(actionRow.Id.Id))
                {
                    requiredActionRows.Add(actionRow.Id.Id, actionRow);
                }
            }

            // Suppress the required actions that are overridable.
            foreach (var suppressActionRow in suppressActionRows)
            {
                var key = suppressActionRow.Id.Id;

                // If there is an overridable row to suppress; suppress it. There is no warning if there
                // is no action to suppress because the action may be suppressed from a merge module in
                // the binder.
                if (requiredActionRows.TryGetValue(key, out var requiredActionRow))
                {
                    if (requiredActionRow.Overridable)
                    {
                        this.Messaging.OnMessage(WixWarnings.SuppressAction(suppressActionRow.SourceLineNumbers, suppressActionRow.Action, suppressActionRow.SequenceTable.ToString()));
                        if (null != requiredActionRow.SourceLineNumbers)
                        {
                            this.Messaging.OnMessage(WixWarnings.SuppressAction2(requiredActionRow.SourceLineNumbers));
                        }

                        requiredActionRows.Remove(key);
                    }
                    else // suppressing a non-overridable action row
                    {
                        this.Messaging.OnMessage(WixErrors.SuppressNonoverridableAction(suppressActionRow.SourceLineNumbers, suppressActionRow.SequenceTable.ToString(), suppressActionRow.Action));
                        if (null != requiredActionRow.SourceLineNumbers)
                        {
                            this.Messaging.OnMessage(WixErrors.SuppressNonoverridableAction2(requiredActionRow.SourceLineNumbers));
                        }
                    }
                }
            }

            // Build up dependency trees of the relatively scheduled actions.
            // Use ToList() to create a copy of the required action rows so that new tuples can
            // be added while enumerating.
            foreach (var actionRow in requiredActionRows.Values.ToList())
            {
                if (0 == actionRow.Sequence)
                {
                    // check for standard actions that don't have a sequence number in a merge module
                    if (SectionType.Module == this.Section.Type && WindowsInstallerStandard.IsStandardAction(actionRow.Action))
                    {
                        this.Messaging.OnMessage(WixErrors.StandardActionRelativelyScheduledInModule(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                    }

                    this.SequenceActionRow(actionRow, requiredActionRows);
                }
                else if (SectionType.Module == this.Section.Type && 0 < actionRow.Sequence && !WindowsInstallerStandard.IsStandardAction(actionRow.Action)) // check for custom actions and dialogs that have a sequence number
                {
                    this.Messaging.OnMessage(WixErrors.CustomActionSequencedInModule(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action));
                }
            }

            // Look for standard actions with sequence restrictions that aren't necessarily scheduled based
            // on the presence of a particular table.
            if (requiredActionRows.ContainsKey("InstallExecuteSequence/DuplicateFiles") && !requiredActionRows.ContainsKey("InstallExecuteSequence/InstallFiles"))
            {
                var standardAction = this.StandardActionsById["InstallExecuteSequence/InstallFiles"];
                requiredActionRows.Add(standardAction.Id.Id, standardAction);
            }

            // Schedule actions.
            List<WixActionTuple> scheduledActionRows;
            if (SectionType.Module == this.Section.Type)
            {
                scheduledActionRows = requiredActionRows.Values.ToList();
            }
            else
            {
                scheduledActionRows = ScheduleActions(requiredActionRows);
            }

            // Remove all existing WixActionTuples from the section then add the
            // scheduled actions back to the section. Note: we add the indices in
            // reverse order to make it easy to remove them from the list later.
            var removeIndices = new List<int>();
            for (var i = this.Section.Tuples.Count - 1; i >= 0; --i)
            {
                var tuple = this.Section.Tuples[i];
                if (tuple.Definition.Type == TupleDefinitionType.WixAction)
                {
                    removeIndices.Add(i);
                }
            }

            foreach (var removeIndex in removeIndices)
            {
                this.Section.Tuples.RemoveAt(removeIndex);
            }

            foreach (var action in scheduledActionRows)
            {
                this.Section.Tuples.Add(action);
            }
        }

        private List<WixActionTuple> ScheduleActions(Dictionary<string, WixActionTuple> requiredActionRows)
        {
            var scheduledActionRows = new List<WixActionTuple>();

            // Process each sequence table individually.
            foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
            {
                // Create a collection of just the action rows in this sequence
                var sequenceActionRows = requiredActionRows.Values.Where(a => a.SequenceTable == sequenceTable).ToList();

                // Schedule the absolutely scheduled actions (by sorting them by their sequence numbers).
                var absoluteActionRows = new List<WixActionTuple>();
                foreach (var actionRow in sequenceActionRows)
                {
                    if (0 != actionRow.Sequence)
                    {
                        // Look for sequence number collisions
                        foreach (var sequenceScheduledActionRow in absoluteActionRows)
                        {
                            if (sequenceScheduledActionRow.Sequence == actionRow.Sequence)
                            {
                                this.Messaging.OnMessage(WixWarnings.ActionSequenceCollision(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action, sequenceScheduledActionRow.Action, actionRow.Sequence));
                                if (null != sequenceScheduledActionRow.SourceLineNumbers)
                                {
                                    this.Messaging.OnMessage(WixWarnings.ActionSequenceCollision2(sequenceScheduledActionRow.SourceLineNumbers));
                                }
                            }
                        }

                        absoluteActionRows.Add(actionRow);
                    }
                }

                absoluteActionRows.Sort((x, y) => x.Sequence.CompareTo(y.Sequence));

                // Schedule the relatively scheduled actions (by resolving the dependency trees).
                var previousUsedSequence = 0;
                var relativeActionRows = new List<WixActionTuple>();
                for (int j = 0; j < absoluteActionRows.Count; j++)
                {
                    var absoluteActionRow = absoluteActionRows[j];

                    // Get all the relatively scheduled action rows occuring before and after this absolutely scheduled action row.
                    var relativeActions = this.GetAllRelativeActionsForSequenceType(sequenceTable, absoluteActionRow);

                    // Check for relatively scheduled actions occuring before/after a special action
                    // (those actions with a negative sequence number).
                    if (absoluteActionRow.Sequence < 0 && (relativeActions.PreviousActions.Any() || relativeActions.NextActions.Any()))
                    {
                        // Create errors for all the before actions.
                        foreach (var actionRow in relativeActions.PreviousActions)
                        {
                            this.Messaging.OnMessage(WixErrors.ActionScheduledRelativeToTerminationAction(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action, absoluteActionRow.Action));
                        }

                        // Create errors for all the after actions.
                        foreach (var actionRow in relativeActions.NextActions)
                        {
                            this.Messaging.OnMessage(WixErrors.ActionScheduledRelativeToTerminationAction(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action, absoluteActionRow.Action));
                        }

                        // If there is source line information for the absolutely scheduled action display it
                        if (absoluteActionRow.SourceLineNumbers != null)
                        {
                            this.Messaging.OnMessage(WixErrors.ActionScheduledRelativeToTerminationAction2(absoluteActionRow.SourceLineNumbers));
                        }

                        continue;
                    }

                    // Schedule the action rows before this one.
                    var unusedSequence = absoluteActionRow.Sequence - 1;
                    for (var i = relativeActions.PreviousActions.Count - 1; i >= 0; i--)
                    {
                        var relativeActionRow = relativeActions.PreviousActions[i];

                        // look for collisions
                        if (unusedSequence == previousUsedSequence)
                        {
                            this.Messaging.OnMessage(WixErrors.NoUniqueActionSequenceNumber(relativeActionRow.SourceLineNumbers, relativeActionRow.SequenceTable.ToString(), relativeActionRow.Action, absoluteActionRow.Action));
                            if (absoluteActionRow.SourceLineNumbers != null)
                            {
                                this.Messaging.OnMessage(WixErrors.NoUniqueActionSequenceNumber2(absoluteActionRow.SourceLineNumbers));
                            }

                            unusedSequence++;
                        }

                        relativeActionRow.Sequence = unusedSequence;
                        relativeActionRows.Add(relativeActionRow);

                        unusedSequence--;
                    }

                    // Determine the next used action sequence number.
                    var nextUsedSequence = Int16.MaxValue + 1;
                    if (absoluteActionRows.Count > j + 1)
                    {
                        nextUsedSequence = absoluteActionRows[j + 1].Sequence;
                    }

                    // Schedule the action rows after this one.
                    unusedSequence = absoluteActionRow.Sequence + 1;
                    for (var i = 0; i < relativeActions.NextActions.Count; i++)
                    {
                        var relativeActionRow = relativeActions.NextActions[i];

                        if (unusedSequence == nextUsedSequence)
                        {
                            this.Messaging.OnMessage(WixErrors.NoUniqueActionSequenceNumber(relativeActionRow.SourceLineNumbers, relativeActionRow.SequenceTable.ToString(), relativeActionRow.Action, absoluteActionRow.Action));
                            if (absoluteActionRow.SourceLineNumbers != null)
                            {
                                this.Messaging.OnMessage(WixErrors.NoUniqueActionSequenceNumber2(absoluteActionRow.SourceLineNumbers));
                            }

                            unusedSequence--;
                        }

                        relativeActionRow.Sequence = unusedSequence;
                        relativeActionRows.Add(relativeActionRow);

                        unusedSequence++;
                    }

                    // keep track of this sequence number as the previous used sequence number for the next iteration
                    previousUsedSequence = absoluteActionRow.Sequence;
                }

                // add the absolutely and relatively scheduled actions to the list of scheduled actions
                scheduledActionRows.AddRange(absoluteActionRows);
                scheduledActionRows.AddRange(relativeActionRows);
            }

            return scheduledActionRows;
        }

        private IEnumerable<string> GetRequiredActionIds()
        {
            var set = new HashSet<string>();

            // gather the required actions for the output type
            if (SectionType.Product == this.Section.Type)
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
                set.Add("AdvtExecuteSequence/CostFinalize");
                set.Add("AdvtExecuteSequence/CostInitialize");
                set.Add("AdvtExecuteSequence/InstallFinalize");
                set.Add("AdvtExecuteSequence/InstallValidate");
                set.Add("AdvtExecuteSequence/PublishFeatures");
                set.Add("AdvtExecuteSequence/PublishProduct");

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

            // Gather the required actions for each tuple type.
            foreach (var tupleType in this.Section.Tuples.Select(t => t.Definition.Type).Distinct())
            {
                switch (tupleType)
                {
                    case TupleDefinitionType.AppSearch:
                        set.Add("InstallExecuteSequence/AppSearch");
                        set.Add("InstallUISequence/AppSearch");
                        break;
                    case TupleDefinitionType.BindImage:
                        set.Add("InstallExecuteSequence/BindImage");
                        break;
                    case TupleDefinitionType.CCPSearch:
                        set.Add("InstallExecuteSequence/AppSearch");
                        set.Add("InstallExecuteSequence/CCPSearch");
                        set.Add("InstallExecuteSequence/RMCCPSearch");
                        set.Add("InstallUISequence/AppSearch");
                        set.Add("InstallUISequence/CCPSearch");
                        set.Add("InstallUISequence/RMCCPSearch");
                        break;
                    case TupleDefinitionType.Class:
                        set.Add("AdvtExecuteSequence/RegisterClassInfo");
                        set.Add("InstallExecuteSequence/RegisterClassInfo");
                        set.Add("InstallExecuteSequence/UnregisterClassInfo");
                        break;
                    case TupleDefinitionType.Complus:
                        set.Add("InstallExecuteSequence/RegisterComPlus");
                        set.Add("InstallExecuteSequence/UnregisterComPlus");
                        break;
                    case TupleDefinitionType.CreateFolder:
                        set.Add("InstallExecuteSequence/CreateFolders");
                        set.Add("InstallExecuteSequence/RemoveFolders");
                        break;
                    case TupleDefinitionType.DuplicateFile:
                        set.Add("InstallExecuteSequence/DuplicateFiles");
                        set.Add("InstallExecuteSequence/RemoveDuplicateFiles");
                        break;
                    case TupleDefinitionType.Environment:
                        set.Add("InstallExecuteSequence/WriteEnvironmentStrings");
                        set.Add("InstallExecuteSequence/RemoveEnvironmentStrings");
                        break;
                    case TupleDefinitionType.Extension:
                        set.Add("AdvtExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/UnregisterExtensionInfo");
                        break;
                    case TupleDefinitionType.File:
                        set.Add("InstallExecuteSequence/InstallFiles");
                        set.Add("InstallExecuteSequence/RemoveFiles");
                        break;
                    case TupleDefinitionType.Font:
                        set.Add("InstallExecuteSequence/RegisterFonts");
                        set.Add("InstallExecuteSequence/UnregisterFonts");
                        break;
                    case TupleDefinitionType.IniFile:
                    case TupleDefinitionType.RemoveIniFile:
                        set.Add("InstallExecuteSequence/WriteIniValues");
                        set.Add("InstallExecuteSequence/RemoveIniValues");
                        break;
                    case TupleDefinitionType.IsolatedComponent:
                        set.Add("InstallExecuteSequence/IsolateComponents");
                        break;
                    case TupleDefinitionType.LaunchCondition:
                        set.Add("InstallExecuteSequence/LaunchConditions");
                        set.Add("InstallUISequence/LaunchConditions");
                        break;
                    case TupleDefinitionType.MIME:
                        set.Add("AdvtExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/UnregisterMIMEInfo");
                        break;
                    case TupleDefinitionType.MoveFile:
                        set.Add("InstallExecuteSequence/MoveFiles");
                        break;
                    case TupleDefinitionType.MsiAssembly:
                        set.Add("AdvtExecuteSequence/MsiPublishAssemblies");
                        set.Add("InstallExecuteSequence/MsiPublishAssemblies");
                        set.Add("InstallExecuteSequence/MsiUnpublishAssemblies");
                        break;
                    case TupleDefinitionType.MsiServiceConfig:
                    case TupleDefinitionType.MsiServiceConfigFailureActions:
                        set.Add("InstallExecuteSequence/MsiConfigureServices");
                        break;
                    case TupleDefinitionType.ODBCDataSource:
                    case TupleDefinitionType.ODBCTranslator:
                    case TupleDefinitionType.ODBCDriver:
                        set.Add("InstallExecuteSequence/SetODBCFolders");
                        set.Add("InstallExecuteSequence/InstallODBC");
                        set.Add("InstallExecuteSequence/RemoveODBC");
                        break;
                    case TupleDefinitionType.ProgId:
                        set.Add("AdvtExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/UnregisterProgIdInfo");
                        break;
                    case TupleDefinitionType.PublishComponent:
                        set.Add("AdvtExecuteSequence/PublishComponents");
                        set.Add("InstallExecuteSequence/PublishComponents");
                        set.Add("InstallExecuteSequence/UnpublishComponents");
                        break;
                    case TupleDefinitionType.Registry:
                    case TupleDefinitionType.RemoveRegistry:
                        set.Add("InstallExecuteSequence/WriteRegistryValues");
                        set.Add("InstallExecuteSequence/RemoveRegistryValues");
                        break;
                    case TupleDefinitionType.RemoveFile:
                        set.Add("InstallExecuteSequence/RemoveFiles");
                        break;
                    case TupleDefinitionType.SelfReg:
                        set.Add("InstallExecuteSequence/SelfRegModules");
                        set.Add("InstallExecuteSequence/SelfUnregModules");
                        break;
                    case TupleDefinitionType.ServiceControl:
                        set.Add("InstallExecuteSequence/StartServices");
                        set.Add("InstallExecuteSequence/StopServices");
                        set.Add("InstallExecuteSequence/DeleteServices");
                        break;
                    case TupleDefinitionType.ServiceInstall:
                        set.Add("InstallExecuteSequence/InstallServices");
                        break;
                    case TupleDefinitionType.Shortcut:
                        set.Add("AdvtExecuteSequence/CreateShortcuts");
                        set.Add("InstallExecuteSequence/CreateShortcuts");
                        set.Add("InstallExecuteSequence/RemoveShortcuts");
                        break;
                    case TupleDefinitionType.TypeLib:
                        set.Add("InstallExecuteSequence/RegisterTypeLibraries");
                        set.Add("InstallExecuteSequence/UnregisterTypeLibraries");
                        break;
                    case TupleDefinitionType.Upgrade:
                        set.Add("InstallExecuteSequence/FindRelatedProducts");
                        set.Add("InstallUISequence/FindRelatedProducts");

                        // Only add the MigrateFeatureStates action if MigrateFeature attribute is set on
                        // at least one UpgradeVersion element.
                        if (this.Section.Tuples.OfType<UpgradeTuple>().Any(t => (t.Attributes & MsiInterop.MsidbUpgradeAttributesMigrateFeatures) == MsiInterop.MsidbUpgradeAttributesMigrateFeatures))
                        {
                            set.Add("InstallExecuteSequence/MigrateFeatureStates");
                            set.Add("InstallUISequence/MigrateFeatureStates");
                        }
                        break;
                }
            }

            return set;
        }

        private IEnumerable<WixActionTuple> GetActions(SequenceTable sequence, string[] actionNames)
        {
            foreach (var action in WindowsInstallerStandard.StandardActions())
            {
                if (action.SequenceTable == sequence && actionNames.Contains(action.Action))
                {
                    yield return action;
                }
            }
        }

        /// <summary>
        /// Sequence an action before or after a standard action.
        /// </summary>
        /// <param name="actionRow">The action row to be sequenced.</param>
        /// <param name="requiredActionRows">Collection of actions which must be included.</param>
        private void SequenceActionRow(WixActionTuple actionRow, Dictionary<string, WixActionTuple> requiredActionRows)
        {
            var after = false;

            if (actionRow.After != null)
            {
                after = true;
            }
            else if (actionRow.Before == null)
            {
                throw new InvalidOperationException(WixStrings.EXP_FoundActionRowWithNoSequenceBeforeOrAfterColumnSet);
            }

            var parentActionName = (after ? actionRow.After : actionRow.Before);
            var parentActionKey = actionRow.SequenceTable.ToString() + "/" + parentActionName;

            if (!requiredActionRows.TryGetValue(parentActionKey, out var parentActionRow))
            {
                // If the missing parent action is a standard action (with a suggested sequence number), add it.
                if (this.StandardActionsById.TryGetValue(parentActionKey, out parentActionRow))
                {
                    // Create a clone to avoid modifying the static copy of the object.
                    // TODO: consider this: parentActionRow = parentActionRow.Clone();

                    requiredActionRows.Add(parentActionRow.Id.Id, parentActionRow);
                }
                else
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_FoundActionRowWinNonExistentAction, (after ? "After" : "Before"), parentActionName));
                }
            }
            else if (actionRow == parentActionRow || this.ContainsChildActionRow(actionRow, parentActionRow)) // cycle detected
            {
                throw new WixException(WixErrors.ActionCircularDependency(actionRow.SourceLineNumbers, actionRow.SequenceTable.ToString(), actionRow.Action, parentActionRow.Action));
            }

            // Add this action to the appropriate list of dependent action rows.
            var relativeActions = this.GetRelativeActions(parentActionRow);
            var relatedRows = (after ? relativeActions.NextActions : relativeActions.PreviousActions);
            relatedRows.Add(actionRow);
        }

        private bool ContainsChildActionRow(WixActionTuple childTuple, WixActionTuple parentTuple)
        {
            var result = false;

            if (this.RelativeActionsForActions.TryGetValue(childTuple.Id.Id, out var relativeActions))
            {
                result = relativeActions.NextActions.Any(a => a.SequenceTable == parentTuple.SequenceTable && a.Id.Id == parentTuple.Id.Id) ||
                     relativeActions.PreviousActions.Any(a => a.SequenceTable == parentTuple.SequenceTable && a.Id.Id == parentTuple.Id.Id);
            }

            return result;
        }

        private RelativeActions GetRelativeActions(WixActionTuple action)
        {
            if (!this.RelativeActionsForActions.TryGetValue(action.Id.Id, out var relativeActions))
            {
                relativeActions = new RelativeActions();
                this.RelativeActionsForActions.Add(action.Id.Id, relativeActions);
            }

            return relativeActions;
        }

        private RelativeActions GetAllRelativeActionsForSequenceType(SequenceTable sequenceType, WixActionTuple action)
        {
            var relativeActions = new RelativeActions();

            if (this.RelativeActionsForActions.TryGetValue(action.Id.Id, out var actionRelatives))
            {
                this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.PreviousActions, relativeActions.PreviousActions);

                this.RecurseRelativeActionsForSequenceType(sequenceType, actionRelatives.NextActions, relativeActions.NextActions);
            }

            return relativeActions;
        }

        private void RecurseRelativeActionsForSequenceType(SequenceTable sequenceType, List<WixActionTuple> actions, List<WixActionTuple> visitedActions)
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
            public List<WixActionTuple> PreviousActions { get; } = new List<WixActionTuple>();

            public List<WixActionTuple> NextActions { get; } = new List<WixActionTuple>();
        }
    }
}
