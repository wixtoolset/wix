// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    public sealed class SqlErrors
    {
        
        private SqlErrors()
        {
        }
        
        public static MessageEventArgs IllegalAttributeWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return new SqlErrorEventArgs(sourceLineNumbers, 5100, "SqlErrors_IllegalAttributeWithoutComponent_1", elementName, attributeName);
        }
        
        public static MessageEventArgs IllegalElementWithoutComponent(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return new SqlErrorEventArgs(sourceLineNumbers, 5101, "SqlErrors_IllegalElementWithoutComponent_1", elementName);
        }
        
        public static MessageEventArgs OneOfAttributesRequiredUnderComponent(SourceLineNumber sourceLineNumbers, string elementName, string attributeName1, string attributeName2, string attributeName3, string attributeName4)
        {
            return new SqlErrorEventArgs(sourceLineNumbers, 5102, "SqlErrors_OneOfAttributesRequiredUnderComponent_1", elementName, attributeName1, attributeName2, attributeName3, attributeName4);
        }
        
        public static MessageEventArgs DeprecatedBinaryChildElement(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return new SqlErrorEventArgs(sourceLineNumbers, 5103, "SqlErrors_DeprecatedBinaryChildElement_1", elementName);
        }
    }
}