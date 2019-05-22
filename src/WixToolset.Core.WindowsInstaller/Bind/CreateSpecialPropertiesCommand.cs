// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

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
            SortedSet<string> adminProperties = new SortedSet<string>();
            SortedSet<string> secureProperties = new SortedSet<string>();
            SortedSet<string> hiddenProperties = new SortedSet<string>();

            foreach (var wixPropertyRow in this.Section.Tuples.OfType<WixPropertyTuple>())
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

            if (0 < adminProperties.Count)
            {
                var tuple = new PropertyTuple(null, new Identifier(AccessModifier.Private, "AdminProperties"));
                tuple.Value = String.Join(";", adminProperties);

                this.Section.Tuples.Add(tuple);
            }

            if (0 < secureProperties.Count)
            {
                var tuple = new PropertyTuple(null, new Identifier(AccessModifier.Private, "SecureCustomProperties"));
                tuple.Value = String.Join(";", secureProperties);

                this.Section.Tuples.Add(tuple);
            }

            if (0 < hiddenProperties.Count)
            {
                var tuple = new PropertyTuple(null, new Identifier(AccessModifier.Private, "MsiHiddenProperties"));
                tuple.Value = String.Join(";", hiddenProperties);

                this.Section.Tuples.Add(tuple);
            }
        }
    }
}
