// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Validate that there are no duplicate GUIDs in the output.
    /// </summary>
    /// <remarks>
    /// Duplicate GUIDs without conditions are an error condition; with conditions, it's a
    /// warning, as the conditions might be mutually exclusive.
    /// </remarks>
    internal class ValidateComponentGuidsCommand
    {
        internal ValidateComponentGuidsCommand(IMessaging messaging, IntermediateSection section)
        {
            this.Messaging = messaging;
            this.Section = section;
        }

        private IMessaging Messaging { get; }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var componentGuidConditions = new Dictionary<string, bool>();

            foreach (var componentTuple in this.Section.Tuples.OfType<ComponentTuple>())
            {
                // We don't care about unmanaged components and if there's a * GUID remaining,
                // there's already an error that prevented it from being replaced with a real GUID.
                if (!String.IsNullOrEmpty(componentTuple.ComponentId) && "*" != componentTuple.ComponentId)
                {
                    var thisComponentHasCondition = !String.IsNullOrEmpty(componentTuple.Condition);
                    var allComponentsHaveConditions = thisComponentHasCondition;

                    if (componentGuidConditions.TryGetValue(componentTuple.ComponentId, out var alreadyCheckedCondition))
                    {
                        allComponentsHaveConditions = thisComponentHasCondition && alreadyCheckedCondition;

                        if (allComponentsHaveConditions)
                        {
                            this.Messaging.Write(WarningMessages.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(componentTuple.SourceLineNumbers, componentTuple.Id.Id, componentTuple.ComponentId));
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateComponentGuids(componentTuple.SourceLineNumbers, componentTuple.Id.Id, componentTuple.ComponentId));
                        }
                    }

                    componentGuidConditions[componentTuple.ComponentId] = allComponentsHaveConditions;
                }
            }
        }
    }
}
