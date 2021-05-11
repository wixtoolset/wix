using System.Collections.Generic;
using WixToolset.Data;
using WixToolset.Extensibility;
using WixToolset.Extensibility.Services;

namespace WixToolset.Core.TestPackage
{
    /// <summary>
    /// An <see cref="IMessageListener"/> that simply stores all the messages.
    /// </summary>
    public sealed class TestMessageListener : IMessageListener
    {
        /// <summary>
        /// All messages that have been received.
        /// </summary>
        public List<Message> Messages { get; } = new List<Message>();

        /// <summary>
        /// 
        /// </summary>
        public string ShortAppName => "TEST";

        /// <summary>
        /// 
        /// </summary>
        public string LongAppName => "Test";

        /// <summary>
        /// Stores the message in <see cref="Messages"/>.
        /// </summary>
        /// <param name="message"></param>
        public void Write(Message message)
        {
            this.Messages.Add(message);
        }

        /// <summary>
        /// Stores the message in <see cref="Messages"/>.
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message)
        {
            this.Messages.Add(new Message(null, MessageLevel.Information, 0, message));
        }

        /// <summary>
        /// Always returns defaultMessageLevel.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="message"></param>
        /// <param name="defaultMessageLevel"></param>
        /// <returns></returns>
        public MessageLevel CalculateMessageLevel(IMessaging messaging, Message message, MessageLevel defaultMessageLevel) => defaultMessageLevel;
    }
}
