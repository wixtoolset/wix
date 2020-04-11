// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Set sequence numbers for all the actions and create tuples in the output object.
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
            var requiredActionTuples = new Dictionary<string, WixActionTuple>();

            // Get the standard actions required based on tuples in the section.
            var overridableActionTuples = this.GetRequiredStandardActions();

            // Index all the action tuples and look for collisions.
            foreach (var actionTuple in this.Section.Tuples.OfType<WixActionTuple>())
            {
                if (actionTuple.Overridable) // overridable action
                {
                    if (overridableActionTuples.TryGetValue(actionTuple.Id.Id, out var collidingActionTuple))
                    {
                        this.Messaging.Write(ErrorMessages.OverridableActionCollision(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action));
                        if (null != collidingActionTuple.SourceLineNumbers)
                        {
                            this.Messaging.Write(ErrorMessages.OverridableActionCollision2(collidingActionTuple.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        overridableActionTuples.Add(actionTuple.Id.Id, actionTuple);
                    }
                }
                else // unsequenced or sequenced action.
                {
                    // Unsequenced action (allowed for certain standard actions).
                    if (null == actionTuple.Before && null == actionTuple.After && !actionTuple.Sequence.HasValue)
                    {
                        if (WindowsInstallerStandard.TryGetStandardAction(actionTuple.Id.Id, out var standardAction))
                        {
                            // Populate the sequence from the standard action
                            actionTuple.Sequence = standardAction.Sequence;
                        }
                        else // not a supported unscheduled action.
                        {
                            throw new InvalidOperationException("Found an action with no Sequence, Before, or After column set.");
                        }
                    }

                    if (requiredActionTuples.TryGetValue(actionTuple.Id.Id, out var collidingActionTuple))
                    {
                        this.Messaging.Write(ErrorMessages.ActionCollision(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action));
                        if (null != collidingActionTuple.SourceLineNumbers)
                        {
                            this.Messaging.Write(ErrorMessages.ActionCollision2(collidingActionTuple.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        requiredActionTuples.Add(actionTuple.Id.Id, actionTuple);
                    }
                }
            }

            // Add the overridable action tuples that are not overridden to the required action tuples.
            foreach (var actionTuple in overridableActionTuples.Values)
            {
                if (!requiredActionTuples.ContainsKey(actionTuple.Id.Id))
                {
                    requiredActionTuples.Add(actionTuple.Id.Id, actionTuple);
                }
            }

            // Suppress the required actions that are overridable.
            foreach (var suppressActionTuple in this.Section.Tuples.OfType<WixSuppressActionTuple>())
            {
                var key = suppressActionTuple.Id.Id;

                // If there is an overridable tuple to suppress; suppress it. There is no warning if there
                // is no action to suppress because the action may be suppressed from a merge module in
                // the binder.
                if (requiredActionTuples.TryGetValue(key, out var requiredActionTuple))
                {
                    if (requiredActionTuple.Overridable)
                    {
                        this.Messaging.Write(WarningMessages.SuppressAction(suppressActionTuple.SourceLineNumbers, suppressActionTuple.Action, suppressActionTuple.SequenceTable.ToString()));
                        if (null != requiredActionTuple.SourceLineNumbers)
                        {
                            this.Messaging.Write(WarningMessages.SuppressAction2(requiredActionTuple.SourceLineNumbers));
                        }

                        requiredActionTuples.Remove(key);
                    }
                    else // suppressing a non-overridable action tuple
                    {
                        this.Messaging.Write(ErrorMessages.SuppressNonoverridableAction(suppressActionTuple.SourceLineNumbers, suppressActionTuple.SequenceTable.ToString(), suppressActionTuple.Action));
                        if (null != requiredActionTuple.SourceLineNumbers)
                        {
                            this.Messaging.Write(ErrorMessages.SuppressNonoverridableAction2(requiredActionTuple.SourceLineNumbers));
                        }
                    }
                }
            }

            // Build up dependency trees of the relatively scheduled actions.
            // Use ToList() to create a copy of the required action tuples so that new tuples can
            // be added while enumerating.
            foreach (var actionTuple in requiredActionTuples.Values.ToList())
            {
                if (!actionTuple.Sequence.HasValue)
                {
                    // check for standard actions that don't have a sequence number in a merge module
                    if (SectionType.Module == this.Section.Type && WindowsInstallerStandard.IsStandardAction(actionTuple.Action))
                    {
                        this.Messaging.Write(ErrorMessages.StandardActionRelativelyScheduledInModule(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action));
                    }

                    this.SequenceActionTuple(actionTuple, requiredActionTuples);
                }
                else if (SectionType.Module == this.Section.Type && 0 < actionTuple.Sequence && !WindowsInstallerStandard.IsStandardAction(actionTuple.Action)) // check for custom actions and dialogs that have a sequence number
                {
                    this.Messaging.Write(ErrorMessages.CustomActionSequencedInModule(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action));
                }
            }

            // Look for standard actions with sequence restrictions that aren't necessarily scheduled based
            // on the presence of a particular table.
            if (requiredActionTuples.ContainsKey("InstallExecuteSequence/DuplicateFiles") && !requiredActionTuples.ContainsKey("InstallExecuteSequence/InstallFiles"))
            {
                WindowsInstallerStandard.TryGetStandardAction("InstallExecuteSequence/InstallFiles", out var standardAction);
                requiredActionTuples.Add(standardAction.Id.Id, standardAction);
            }

            // Schedule actions.
            List<WixActionTuple> scheduledActionTuples;
            if (SectionType.Module == this.Section.Type)
            {
                scheduledActionTuples = requiredActionTuples.Values.ToList();
            }
            else
            {
                scheduledActionTuples = this.ScheduleActions(requiredActionTuples);
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

            foreach (var action in scheduledActionTuples)
            {
                this.Section.Tuples.Add(action);
            }
        }

        private Dictionary<string, WixActionTuple> GetRequiredStandardActions()
        {
            var overridableActionTuples = new Dictionary<string, WixActionTuple>();

            var requiredActionIds = this.GetRequiredActionIds();

            foreach (var actionId in requiredActionIds)
            {
                WindowsInstallerStandard.TryGetStandardAction(actionId, out var standardAction);
                overridableActionTuples.Add(standardAction.Id.Id, standardAction);
            }

            return overridableActionTuples;
        }

        private List<WixActionTuple> ScheduleActions(Dictionary<string, WixActionTuple> requiredActionTuples)
        {
            var scheduledActionTuples = new List<WixActionTuple>();

            // Process each sequence table individually.
            foreach (SequenceTable sequenceTable in Enum.GetValues(typeof(SequenceTable)))
            {
                // Create a collection of just the action tuples in this sequence
                var sequenceActionTuples = requiredActionTuples.Values.Where(a => a.SequenceTable == sequenceTable).ToList();

                // Schedule the absolutely scheduled actions (by sorting them by their sequence numbers).
                var absoluteActionTuples = new List<WixActionTuple>();
                foreach (var actionTuple in sequenceActionTuples)
                {
                    if (actionTuple.Sequence.HasValue)
                    {
                        // Look for sequence number collisions
                        foreach (var sequenceScheduledActionTuple in absoluteActionTuples)
                        {
                            if (sequenceScheduledActionTuple.Sequence == actionTuple.Sequence)
                            {
                                this.Messaging.Write(WarningMessages.ActionSequenceCollision(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action, sequenceScheduledActionTuple.Action, actionTuple.Sequence ?? 0));
                                if (null != sequenceScheduledActionTuple.SourceLineNumbers)
                                {
                                    this.Messaging.Write(WarningMessages.ActionSequenceCollision2(sequenceScheduledActionTuple.SourceLineNumbers));
                                }
                            }
                        }

                        absoluteActionTuples.Add(actionTuple);
                    }
                }

                absoluteActionTuples.Sort((x, y) => (x.Sequence ?? 0).CompareTo(y.Sequence ?? 0));

                // Schedule the relatively scheduled actions (by resolving the dependency trees).
                var previousUsedSequence = 0;
                var relativeActionTuples = new List<WixActionTuple>();
                for (int j = 0; j < absoluteActionTuples.Count; j++)
                {
                    var absoluteActionTuple = absoluteActionTuples[j];

                    // Get all the relatively scheduled action tuples occuring before and after this absolutely scheduled action tuple.
                    var relativeActions = this.GetAllRelativeActionsForSequenceType(sequenceTable, absoluteActionTuple);

                    // Check for relatively scheduled actions occuring before/after a special action
                    // (those actions with a negative sequence number).
                    if (absoluteActionTuple.Sequence < 0 && (relativeActions.PreviousActions.Any() || relativeActions.NextActions.Any()))
                    {
                        // Create errors for all the before actions.
                        foreach (var actionTuple in relativeActions.PreviousActions)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action, absoluteActionTuple.Action));
                        }

                        // Create errors for all the after actions.
                        foreach (var actionTuple in relativeActions.NextActions)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action, absoluteActionTuple.Action));
                        }

                        // If there is source line information for the absolutely scheduled action display it
                        if (absoluteActionTuple.SourceLineNumbers != null)
                        {
                            this.Messaging.Write(ErrorMessages.ActionScheduledRelativeToTerminationAction2(absoluteActionTuple.SourceLineNumbers));
                        }

                        continue;
                    }

                    // Schedule the action tuples before this one.
                    var unusedSequence = absoluteActionTuple.Sequence - 1;
                    for (var i = relativeActions.PreviousActions.Count - 1; i >= 0; i--)
                    {
                        var relativeActionTuple = relativeActions.PreviousActions[i];

                        // look for collisions
                        if (unusedSequence == previousUsedSequence)
                        {
                            this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber(relativeActionTuple.SourceLineNumbers, relativeActionTuple.SequenceTable.ToString(), relativeActionTuple.Action, absoluteActionTuple.Action));
                            if (absoluteActionTuple.SourceLineNumbers != null)
                            {
                                this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber2(absoluteActionTuple.SourceLineNumbers));
                            }

                            unusedSequence++;
                        }

                        relativeActionTuple.Sequence = unusedSequence;
                        relativeActionTuples.Add(relativeActionTuple);

                        unusedSequence--;
                    }

                    // Determine the next used action sequence number.
                    var nextUsedSequence = Int16.MaxValue + 1;
                    if (absoluteActionTuples.Count > j + 1)
                    {
                        nextUsedSequence = absoluteActionTuples[j + 1].Sequence ?? 0;
                    }

                    // Schedule the action tuples after this one.
                    unusedSequence = absoluteActionTuple.Sequence + 1;
                    for (var i = 0; i < relativeActions.NextActions.Count; i++)
                    {
                        var relativeActionTuple = relativeActions.NextActions[i];

                        if (unusedSequence == nextUsedSequence)
                        {
                            this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber(relativeActionTuple.SourceLineNumbers, relativeActionTuple.SequenceTable.ToString(), relativeActionTuple.Action, absoluteActionTuple.Action));
                            if (absoluteActionTuple.SourceLineNumbers != null)
                            {
                                this.Messaging.Write(ErrorMessages.NoUniqueActionSequenceNumber2(absoluteActionTuple.SourceLineNumbers));
                            }

                            unusedSequence--;
                        }

                        relativeActionTuple.Sequence = unusedSequence;
                        relativeActionTuples.Add(relativeActionTuple);

                        unusedSequence++;
                    }

                    // keep track of this sequence number as the previous used sequence number for the next iteration
                    previousUsedSequence = absoluteActionTuple.Sequence ?? 0;
                }

                // add the absolutely and relatively scheduled actions to the list of scheduled actions
                scheduledActionTuples.AddRange(absoluteActionTuples);
                scheduledActionTuples.AddRange(relativeActionTuples);
            }

            return scheduledActionTuples;
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

            // Gather the required actions for each tuple type.
            foreach (var tupleType in this.Section.Tuples.Select(t => t.Definition.Type).Distinct())
            {
                switch (tupleType)
                {
                    case TupleDefinitionType.AppSearch:
                        set.Add("InstallExecuteSequence/AppSearch");
                        set.Add("InstallUISequence/AppSearch");
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
                        set.Add("AdvertiseExecuteSequence/RegisterClassInfo");
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
                        set.Add("AdvertiseExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/RegisterExtensionInfo");
                        set.Add("InstallExecuteSequence/UnregisterExtensionInfo");
                        break;
                    case TupleDefinitionType.File:
                        set.Add("InstallExecuteSequence/InstallFiles");
                        set.Add("InstallExecuteSequence/RemoveFiles");

                        var foundFont = false;
                        var foundSelfReg = false;
                        var foundBindPath = false;
                        foreach (var file in this.Section.Tuples.OfType<FileTuple>())
                        {
                            if (!foundFont && !String.IsNullOrEmpty(file.FontTitle))
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
                    case TupleDefinitionType.IniFile:
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
                        set.Add("AdvertiseExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/RegisterMIMEInfo");
                        set.Add("InstallExecuteSequence/UnregisterMIMEInfo");
                        break;
                    case TupleDefinitionType.MoveFile:
                        set.Add("InstallExecuteSequence/MoveFiles");
                        break;
                    case TupleDefinitionType.Assembly:
                        set.Add("AdvertiseExecuteSequence/MsiPublishAssemblies");
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
                        set.Add("AdvertiseExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/RegisterProgIdInfo");
                        set.Add("InstallExecuteSequence/UnregisterProgIdInfo");
                        break;
                    case TupleDefinitionType.PublishComponent:
                        set.Add("AdvertiseExecuteSequence/PublishComponents");
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
                    case TupleDefinitionType.ServiceControl:
                        set.Add("InstallExecuteSequence/StartServices");
                        set.Add("InstallExecuteSequence/StopServices");
                        set.Add("InstallExecuteSequence/DeleteServices");
                        break;
                    case TupleDefinitionType.ServiceInstall:
                        set.Add("InstallExecuteSequence/InstallServices");
                        break;
                    case TupleDefinitionType.Shortcut:
                        set.Add("AdvertiseExecuteSequence/CreateShortcuts");
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
                        if (this.Section.Tuples.OfType<UpgradeTuple>().Any(t => t.MigrateFeatures))
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
        /// <param name="actionTuple">The action tuple to be sequenced.</param>
        /// <param name="requiredActionTuples">Collection of actions which must be included.</param>
        private void SequenceActionTuple(WixActionTuple actionTuple, Dictionary<string, WixActionTuple> requiredActionTuples)
        {
            var after = false;

            if (actionTuple.After != null)
            {
                after = true;
            }
            else if (actionTuple.Before == null)
            {
                throw new InvalidOperationException("Found an action with no Sequence, Before, or After column set.");
            }

            var parentActionName = (after ? actionTuple.After : actionTuple.Before);
            var parentActionKey = actionTuple.SequenceTable.ToString() + "/" + parentActionName;

            if (!requiredActionTuples.TryGetValue(parentActionKey, out var parentActionTuple))
            {
                // If the missing parent action is a standard action (with a suggested sequence number), add it.
                if (WindowsInstallerStandard.TryGetStandardAction(parentActionKey, out parentActionTuple))
                {
                    // Create a clone to avoid modifying the static copy of the object.
                    // TODO: consider this: parentActionTuple = parentActionTuple.Clone();

                    requiredActionTuples.Add(parentActionTuple.Id.Id, parentActionTuple);
                }
                else
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Found an action with a non-existent {0} action: {1}.", (after ? "After" : "Before"), parentActionName));
                }
            }
            else if (actionTuple == parentActionTuple || this.ContainsChildActionTuple(actionTuple, parentActionTuple)) // cycle detected
            {
                throw new WixException(ErrorMessages.ActionCircularDependency(actionTuple.SourceLineNumbers, actionTuple.SequenceTable.ToString(), actionTuple.Action, parentActionTuple.Action));
            }

            // Add this action to the appropriate list of dependent action tuples.
            var relativeActions = this.GetRelativeActions(parentActionTuple);
            var relatedTuples = (after ? relativeActions.NextActions : relativeActions.PreviousActions);
            relatedTuples.Add(actionTuple);
        }

        private bool ContainsChildActionTuple(WixActionTuple childTuple, WixActionTuple parentTuple)
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
