// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    public interface IMessageListener
    {
        string ShortAppName { get; }

        string LongAppName { get; }

        void Write(Message message);

        void Write(string message);
    }
}
