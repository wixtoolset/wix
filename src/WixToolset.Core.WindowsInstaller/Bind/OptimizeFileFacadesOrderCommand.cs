// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Core.Bind;

    internal class OptimizeFileFacadesOrderCommand
    {
        public OptimizeFileFacadesOrderCommand(List<FileFacade> fileFacades)
        {
            this.FileFacades = fileFacades;
        }

        public List<FileFacade> FileFacades { get; private set; }

        public List<FileFacade> Execute()
        {
            this.FileFacades.Sort(FileFacadeOptimizer.Instance);

            return this.FileFacades;
        }

        private class FileFacadeOptimizer : IComparer<FileFacade>
        {
            public static readonly FileFacadeOptimizer Instance = new FileFacadeOptimizer();

            public int Compare(FileFacade x, FileFacade y)
            {
                // TODO: Sort these facades even smarter by directory path and component id 
                //       and maybe file size or file extension and other creative ideas to
                //       get optimal install speed out of MSI.
                return String.Compare(x.ComponentRef, y.ComponentRef, StringComparison.Ordinal);
            }
        }
    }
}
