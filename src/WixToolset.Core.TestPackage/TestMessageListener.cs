using System.Collections.Generic;
using WixToolset.Data;
using WixToolset.Extensibility;
using WixToolset.Extensibility.Services;

namespace WixToolset.Core.TestPackage
{
    public sealed class TestMessageListener : IMessageListener
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

        public MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel) => defaultMessageLevel;
    }
}
