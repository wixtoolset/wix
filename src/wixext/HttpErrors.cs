// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using System;
    using System.Resources;
    using WixToolset.Data;

    public static class HttpErrors
    {
        public static Message NoSecuritySpecified(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.NoSecuritySpecified, "The UrlReservation element doesn't identify the security for the reservation. You must either specify the Sddl attribute, or provide child UrlAce elements.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            NoSecuritySpecified = 6701,
        }
    }
}
