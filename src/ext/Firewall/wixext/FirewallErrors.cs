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

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, resourceManager, resourceName, args);
        }

        public static Message IllegalInterfaceWithInterfaceAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalInterfaceWithInterfaceAttribute, "The Interface element cannot be specified because its parent FirewallException already specified the Interface attribute. To use Interface elements, omit the Interface attribute.");
        }

        public static Message IllegalInterfaceTypeWithInterfaceTypeAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalInterfaceTypeWithInterfaceTypeAttribute, "The InterfaceType element cannot be specified because its parent FirewallException already specified the InterfaceType attribute. To use InterfaceType elements, omit the InterfaceType attribute.");
        }

        public static Message IllegalInterfaceTypeWithInterfaceTypeAll(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalInterfaceTypeWithInterfaceTypeAll, "The InterfaceType element cannot be specified because its parent FirewallException contains another InterfaceType element with value 'All'.");
        }
        public static Message IllegalLocalAddressWithLocalScopeAttribute(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalLocalAddressWithLocalScopeAttribute, "The LocalAddress element cannot be specified because its parent FirewallException already specified the LocalScope attribute. To use LocalAddress elements, omit the LocalScope attribute.");
        }

        public enum Ids
        {
            IllegalRemoteAddressWithScopeAttribute = 6401,
            IllegalInterfaceWithInterfaceAttribute = 6402,
            IllegalInterfaceTypeWithInterfaceTypeAttribute = 6404,
            IllegalInterfaceTypeWithInterfaceTypeAll = 6405,
            IllegalLocalAddressWithLocalScopeAttribute = 6406,
        }
    }
}
