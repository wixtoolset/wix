// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Collections.Generic;

    public interface IParseCommandLine
    {
        bool IsSwitch(string arg);

        bool IsSwitchAt(IEnumerable<string> args, int index);

        void GetNextArgumentOrError(ref string arg);

        void GetNextArgumentOrError(IList<string> args);

        void GetNextArgumentAsFilePathOrError(IList<string> args, string fileType);

        bool TryGetNextArgumentOrError(out string arg);
    }
}
