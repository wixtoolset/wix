// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.TestSupport
{
    using System;
    using Xunit.Sdk;

    public class SpecificReturnCodeException : XunitException
    {
        public SpecificReturnCodeException(int hrExpected, int hr, string userMessage)
            : base(String.Format("WixAssert.SpecificReturnCode() Failure\r\n" +
                                 "Expected HRESULT:  0x{0:X8}\r\n" +
                                 "Actual   HRESULT:  0x{1:X8}\r\n" +
                                 "Message:  {2}",
                                 hrExpected, hr, userMessage))
        {
            this.HResult = hr;
        }
    }
}
