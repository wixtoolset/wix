// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class VersionCommand : BaseCommandLineCommand
    {
        public override CommandLineHelp GetCommandLineHelp()
        {
            return null;
        }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            // $(GitBaseVersionMajor).$(GitBaseVersionMinor).$(GitBaseVersionPatch)$(GitSemVerDashLabel)+$(Commit)
            Console.WriteLine("{0}.{1}.{2}{3}+{4}", SomeVerInfo.Major
                                                  , SomeVerInfo.Minor
                                                  , SomeVerInfo.Patch
                                                  , SomeVerInfo.Label
                                                  , SomeVerInfo.ShortSha);
            return Task.FromResult(0);
        }

        public override bool TryParseArgument(ICommandLineParser parseHelper, string argument)
        {
            return true; // eat any arguments
        }
    }
}
