// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public delegate void DisplayEventHandler(object sender, DisplayEventArgs e);

    public class DisplayEventArgs : EventArgs
    {
        public MessageLevel Level { get; set; }

        public string Message { get; set; }
    }
}
