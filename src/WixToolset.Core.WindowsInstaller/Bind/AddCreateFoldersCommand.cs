// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    /// <summary>
    /// Add CreateFolder tuples, if not already present, for null-keypath components.
    /// </summary>
    internal class AddCreateFoldersCommand
    {
        internal AddCreateFoldersCommand(IntermediateSection section)
        {
            this.Section = section;
        }

        private IntermediateSection Section { get; }

        public void Execute()
        {
            var createFolderTuplesByComponentRef = new HashSet<string>(this.Section.Tuples.OfType<CreateFolderTuple>().Select(t => t.ComponentRef));
            foreach (var componentTuple in this.Section.Tuples.OfType<ComponentTuple>().Where(t => t.KeyPathType == ComponentKeyPathType.Directory).ToList())
            {
                if (!createFolderTuplesByComponentRef.Contains(componentTuple.Id.Id))
                {
                    this.Section.AddTuple(new CreateFolderTuple(componentTuple.SourceLineNumbers)
                    {
                        DirectoryRef = componentTuple.DirectoryRef,
                        ComponentRef = componentTuple.Id.Id,
                    });
                }
            }
        }
    }
}