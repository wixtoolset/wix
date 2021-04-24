// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class LinkerWarnings
    {
        public static Message LayoutPayloadInContainer(SourceLineNumber sourceLineNumbers, string payloadId, string containerId)
        {
            return Message(sourceLineNumbers, Ids.LayoutPayloadInContainer, "The layout-only Payload '{0}' is being added to Container '{1}'. It will not be extracted during layout.", payloadId, containerId);
        }

        public static Message PayloadInMultipleContainers(SourceLineNumber sourceLineNumbers, string payloadId, string containerId1, string containerId2)
        {
            return Message(sourceLineNumbers, Ids.PayloadInMultipleContainers, "The Payload '{0}' can't be added to Container '{1}' because it was already added to Container '{2}'.", payloadId, containerId1, containerId2);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            LayoutPayloadInContainer = 6900,
            PayloadInMultipleContainers = 6901,
        } // last available is 6999. 7000 is LinkerErrors.
    }
}
