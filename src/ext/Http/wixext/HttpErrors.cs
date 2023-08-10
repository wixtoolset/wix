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

        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.IllegalElementWithoutComponent, "The {0} element cannot be specified unless the element has a Component as an ancestor. A {0} that does not have a Component ancestor is not installed.", elementName);
        }

        public enum Ids
        {
            NoSecuritySpecified = 6701,
            IllegalElementWithoutComponent = 6721,
        }
    }
}
