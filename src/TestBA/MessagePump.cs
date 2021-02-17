// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Test.BA
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class MessagePump
    {
        const uint PM_REMOVE = 1;

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessageW(ref Message pMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage(ref Message pMsg);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr DispatchMessageW(ref Message pMsg);

        public static void ProcessMessages(int maxMessages)
        {
            for (int i = 0; i < maxMessages; i++)
            {
                Message message = new Message();
                if (!PeekMessageW(ref message, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    break;
                }

                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }
        }
    }
}
