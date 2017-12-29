// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public static class WixRunner
    {
        public static int Execute(string[] args, out List<Message> messages)
        {
            var listener = new TestListener();

            var program = new Program();
            var result = program.Run(new WixToolsetServiceProvider(), listener, args);

            messages = listener.Messages;

            return result;
        }

        private class TestListener : IMessageListener
        {
            public List<Message> Messages { get; } = new List<Message>();

            public string ShortAppName => "TEST";

            public string LongAppName => "Test";

            public void Write(Message message)
            {
                this.Messages.Add(message);
            }

            public void Write(string message)
            {
                this.Messages.Add(new Message(null, MessageLevel.Information, 0, message));
            }
        }
    }
}
