// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport.XunitExtensions
{
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class SkippableFactMessageBus : IMessageBus
    {
        private IMessageBus InnerBus { get; }

        public SkippableFactMessageBus(IMessageBus innerBus)
        {
            this.InnerBus = innerBus;
        }

        public int DynamicallySkippedTestCount { get; private set; }

        public void Dispose()
        {
        }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            if (message is ITestFailed testFailed)
            {
                var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
                if (exceptionType == typeof(SkipTestException).FullName)
                {
                    ++this.DynamicallySkippedTestCount;
                    return this.InnerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
                }
            }

            // Nothing we care about, send it on its way
            return this.InnerBus.QueueMessage(message);
        }
    }
}
