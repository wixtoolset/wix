// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    internal static class StrUtil
    {
        [DllImport("mbanative.dll", ExactSpelling = true)]
        internal static extern void StrFree(
            IntPtr scz
            );

        internal sealed class StrHandle : SafeHandleZeroIsDefaultAndInvalid
        {
            protected override bool ReleaseHandle()
            {
                StrFree(this.handle);
                return true;
            }

            public string ToUniString()
            {
                return Marshal.PtrToStringUni(this.handle);
            }

            public SecureString ToSecureString()
            {
                if (this.handle == IntPtr.Zero)
                {
                    return null;
                }

                SecureString value = new SecureString();
                char c;
                for (int charIndex = 0; ; charIndex++)
                {
                    c = (char)Marshal.ReadInt16(this.handle, charIndex * UnicodeEncoding.CharSize);
                    if (c == '\0')
                    {
                        break;
                    }

                    value.AppendChar(c);
                }

                return value;
            }
        }
    }
}
