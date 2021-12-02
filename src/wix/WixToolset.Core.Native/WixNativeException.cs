// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [Serializable]
    internal class WixNativeException : Exception
    {
        private static readonly string LineSeparator = Environment.NewLine + "  ";

        public WixNativeException()
        {
        }

        public WixNativeException(string message) : base(message)
        {
        }

        public WixNativeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WixNativeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static WixNativeException FromOutputLines(int errorCode, IReadOnlyCollection<string> lines)
        {
            var exception = new Win32Exception(errorCode);
            var output = String.Join(LineSeparator, lines);
            return new WixNativeException($"wixnative.exe failed with error code: {exception.ErrorCode} - {exception.Message} Output:{LineSeparator}{output}", exception);
        }
    }
}
