// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System.Resources;
    using WixToolset.Data;

    public static class FirewallErrors
    {
        public static Message IllegalRemoteAddressWithScopeAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalRemoteAddressWithScopeAttribute, "The RemoteAddress element cannot be specified because its parent FirewallException already specified the Scope attribute. To use RemoteAddress elements, omit the Scope attribute.");
        }

        public static Message NoExceptionSpecified(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.NoExceptionSpecified, "The FirewallException element doesn't identify the target of the firewall exception. To create an application exception, nest the FirewallException element under a File element or provide a value for the File or Program attributes. To create a port exception, provide a value for the Port attribute.");
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
            IllegalRemoteAddressWithScopeAttribute = 6401,
            NoExceptionSpecified = 6403,
        }
    }
}
