// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using WixToolset.Data;

    internal static class NativeWarnings
    {
        public static Message ValidationFailedDueToSystemPolicy()
        {
            return Message(null, Ids.ValidationFailedDueToSystemPolicy, "Validation could not run due to system policy. To eliminate this warning, run the process as admin or suppress ICE validation.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            ValidationFailedDueToSystemPolicy = 1105,
        }
    }
}
