// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using WixToolset.Data;
    
    public static class IIsErrors
    {
        public static Message MimeMapExtensionMissingPeriod(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            throw new NotImplementedException();
        }
        
        public static Message IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            throw new NotImplementedException();
        }
        
        public static Message IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            throw new NotImplementedException();
        }
        
        public static Message OneOfAttributesRequiredUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4)
        {
            throw new NotImplementedException();
        }
        
        public static Message WebSiteAttributeUnderWebSite(SourceLineNumber sourceLineNumbers, string elementName)
        {
            throw new NotImplementedException();
        }
        
        public static Message WebApplicationAlreadySpecified(SourceLineNumber sourceLineNumbers, string elementName)
        {
            throw new NotImplementedException();
        }
        
        public static Message IllegalCharacterInAttributeValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, char illegalCharacter)
        {
            throw new NotImplementedException();
        }
        
        public static Message DeprecatedBinaryChildElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            throw new NotImplementedException();
        }
        
        public static Message WebSiteNotFound(string webSiteDescription)
        {
            throw new NotImplementedException();
        }
        
        public static Message InsufficientPermissionHarvestWebSite()
        {
            throw new NotImplementedException();
        }
        
        public static Message CannotHarvestWebSite()
        {
            throw new NotImplementedException();
        }
        
        public static Message RequiredAttributeUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            throw new NotImplementedException();
        }
    }
}