// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters.Mocks
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public class MockMessaging : IMessaging
    {
        public List<Message> Messages { get; } = new List<Message>();

        public bool EncounteredError { get; private set; }

        public int LastErrorNumber { get; }

        public bool ShowVerboseMessages { get; set; }

        public bool SuppressAllWarnings { get; set; }

        public bool WarningsAsError { get; set; }

        public void ElevateWarningMessage(int warningNumber) => throw new NotImplementedException();

        public void SetListener(IMessageListener listener) => throw new NotImplementedException();

        public void SuppressWarningMessage(int warningNumber) => throw new NotImplementedException();

        public void Write(Message message)
        {
            this.Messages.Add(message);
            this.EncounteredError |= message.Level == MessageLevel.Error;
        }

        public void Write(string message, bool verbose = false) => throw new NotImplementedException();
    }
}
