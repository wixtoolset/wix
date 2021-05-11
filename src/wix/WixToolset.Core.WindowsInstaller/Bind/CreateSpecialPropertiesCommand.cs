// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal class CreateSpecialPropertiesCommand
    {
        public CreateSpecialPropertiesCommand(IntermediateSection section)
        {
            this.Section = section;
        }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            // Create lists of the properties that contribute to the special lists of properties.
            var adminProperties = new SortedSet<string>();
            var secureProperties = new SortedSet<string>();
            var hiddenProperties = new SortedSet<string>();

            foreach (var wixPropertyRow in this.Section.Symbols.OfType<WixPropertySymbol>())
            {
                if (wixPropertyRow.Admin)
                {
                    adminProperties.Add(wixPropertyRow.PropertyRef);
                }

                if (wixPropertyRow.Hidden)
                {
                    hiddenProperties.Add(wixPropertyRow.PropertyRef);
                }

                if (wixPropertyRow.Secure)
                {
                    secureProperties.Add(wixPropertyRow.PropertyRef);
                }
            }

            // Hide properties for in-script custom actions that have HideTarget set.
            var hideTargetCustomActions = this.Section.Symbols.OfType<CustomActionSymbol>().Where(
                ca => ca.Hidden
                && (ca.ExecutionType == CustomActionExecutionType.Deferred
                || ca.ExecutionType == CustomActionExecutionType.Commit
                || ca.ExecutionType == CustomActionExecutionType.Rollback))
                .Select(ca => ca.Id.Id);
            hiddenProperties.UnionWith(hideTargetCustomActions);

            // Ensure upgrade action properties are secure.
            var actionProperties = this.Section.Symbols.OfType<UpgradeSymbol>().Select(u => u.ActionProperty);
            secureProperties.UnionWith(actionProperties);

            if (0 < adminProperties.Count)
            {
                this.Section.AddSymbol(new PropertySymbol(null, new Identifier(AccessModifier.Section, "AdminProperties"))
                {
                    Value = String.Join(";", adminProperties),
                });
            }

            if (0 < secureProperties.Count)
            {
                this.Section.AddSymbol(new PropertySymbol(null, new Identifier(AccessModifier.Section, "SecureCustomProperties"))
                {
                    Value = String.Join(";", secureProperties),
                });
            }

            if (0 < hiddenProperties.Count)
            {
                this.Section.AddSymbol(new PropertySymbol(null, new Identifier(AccessModifier.Section, "MsiHiddenProperties"))
                {
                    Value = String.Join(";", hiddenProperties)
                });
            }
        }
    }
}
