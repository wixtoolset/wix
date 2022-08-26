// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using WixToolset.Data;

    internal static class WindowsInstallerBackendWarnings
    {
        internal static Message LongPatchBaselineIdTrimmed(SourceLineNumber sourceLineNumbers, string baseTransformName, string trimmedTransformName)
        {
            return Message(sourceLineNumbers, Ids.LongPatchBaselineIdTrimmed, "The PatchBaseline/@Id='{0}' is too long. It is recommended to use short identifiers like 'RTM' and 'SP1'. The identifier has been trimmed to '{1}' so the patch can be created.", baseTransformName, trimmedTransformName);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            LongPatchBaselineIdTrimmed = 7100,
        } // last available is 7499. 7500 is WindowsInstallerBackendErrors.
    }
}
