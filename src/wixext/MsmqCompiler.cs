// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class MsmqCompiler : CompilerExtension
    {
        /// <summary>
        /// Instantiate a new MsmqCompiler.
        /// </summary>
        public MsmqCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/msmq";
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueueAttributes
        {
            Authenticate = (1 << 0),
            Journal = (1 << 1),
            Transactional = (1 << 2)
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueuePrivacyLevel
        {
            None = 0,
            Optional = 1,
            Body = 2
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum MqiMessageQueuePermission
        {
            DeleteMessage = (1 << 0),
            PeekMessage = (1 << 1),
            WriteMessage = (1 << 2),
            DeleteJournalMessage = (1 << 3),
            SetQueueProperties = (1 << 4),
            GetQueueProperties = (1 << 5),
            DeleteQueue = (1 << 6),
            GetQueuePermissions = (1 << 7),
            ChangeQueuePermissions = (1 << 8),
            TakeQueueOwnership = (1 << 9),
            ReceiveMessage = (1 << 10),
            ReceiveJournalMessage = (1 << 11),
            QueueGenericRead = (1 << 12),
            QueueGenericWrite = (1 << 13),
            QueueGenericExecute = (1 << 14),
            QueueGenericAll = (1 << 15)
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    string componentId = context["ComponentId"];
                    string directoryId = context["DirectoryId"];

                    switch (element.Name.LocalName)
                    {
                        case "MessageQueue":
                            this.ParseMessageQueueElement(element, componentId);
                            break;
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(element, componentId, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        ///	<summary>
        ///	Parses an MSMQ message queue element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        private void ParseMessageQueueElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string id = null;
            int basePriority = CompilerConstants.IntegerNotSet;
            int journalQuota = CompilerConstants.IntegerNotSet;
            string label = null;
            string multicastAddress = null;
            string pathName = null;
            int privLevel = CompilerConstants.IntegerNotSet;
            int quota = CompilerConstants.IntegerNotSet;
            string serviceTypeGuid = null;
            int attributes = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Authenticate":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Authenticate;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Authenticate;
                            }
                            break;
                        case "BasePriority":
                            basePriority = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Journal":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Journal;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Journal;
                            }
                            break;
                        case "JournalQuota":
                            journalQuota = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Label":
                            label = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MulticastAddress":
                            multicastAddress = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PathName":
                            pathName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PrivLevel":
                            string privLevelAttr = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (privLevelAttr)
                            {
                                case "none":
                                    privLevel = (int)MqiMessageQueuePrivacyLevel.None;
                                    break;
                                case "optional":
                                    privLevel = (int)MqiMessageQueuePrivacyLevel.Optional;
                                    break;
                                case "body":
                                    privLevel = (int)MqiMessageQueuePrivacyLevel.Body;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "MessageQueue", "PrivLevel", privLevelAttr, "none", "body", "optional"));
                                    break;
                            }
                            break;
                        case "Quota":
                            quota = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Transactional":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Transactional;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Transactional;
                            }
                            break;
                        case "ServiceTypeGuid":
                            serviceTypeGuid = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(child, componentId, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueue");
            row[0] = id;
            row[1] = componentId;
            if (CompilerConstants.IntegerNotSet != basePriority)
            {
                row[2] = basePriority;
            }
            if (CompilerConstants.IntegerNotSet != journalQuota)
            {
                row[3] = journalQuota;
            }
            row[4] = label;
            row[5] = multicastAddress;
            row[6] = pathName;
            if (CompilerConstants.IntegerNotSet != privLevel)
            {
                row[7] = privLevel;
            }
            if (CompilerConstants.IntegerNotSet != quota)
            {
                row[8] = quota;
            }
            row[9] = serviceTypeGuid;
            row[10] = attributes;

            this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "MessageQueuingInstall");
            this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "MessageQueuingUninstall");
        }

        ///	<summary>
        ///	Parses an MSMQ message queue permission element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent message queue.</param>
        private void ParseMessageQueuePermissionElement(XElement node, string componentId, string messageQueueId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string id = null;
            string user = null;
            string group = null;
            int permissions = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "MessageQueue":
                            if (null != messageQueueId)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            messageQueueId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "MessageQueue", messageQueueId);
                            break;
                        case "User":
                            if (null != group)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "User", "Group"));
                            }
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "User", user);
                            break;
                        case "Group":
                            if (null != user)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Group", "User"));
                            }
                            group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Group", group);
                            break;
                        case "DeleteMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteMessage;
                            }
                            break;
                        case "PeekMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.PeekMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.PeekMessage;
                            }
                            break;
                        case "WriteMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.WriteMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.WriteMessage;
                            }
                            break;
                        case "DeleteJournalMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteJournalMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteJournalMessage;
                            }
                            break;
                        case "SetQueueProperties":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.SetQueueProperties;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.SetQueueProperties;
                            }
                            break;
                        case "GetQueueProperties":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.GetQueueProperties;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.GetQueueProperties;
                            }
                            break;
                        case "DeleteQueue":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteQueue;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteQueue;
                            }
                            break;
                        case "GetQueuePermissions":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.GetQueuePermissions;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.GetQueuePermissions;
                            }
                            break;
                        case "ChangeQueuePermissions":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ChangeQueuePermissions;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ChangeQueuePermissions;
                            }
                            break;
                        case "TakeQueueOwnership":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.TakeQueueOwnership;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.TakeQueueOwnership;
                            }
                            break;
                        case "ReceiveMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ReceiveMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ReceiveMessage;
                            }
                            break;
                        case "ReceiveJournalMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ReceiveJournalMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ReceiveJournalMessage;
                            }
                            break;
                        case "QueueGenericRead":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericRead;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericRead;
                            }
                            break;
                        case "QueueGenericWrite":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericWrite;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericWrite;
                            }
                            break;
                        case "QueueGenericExecute":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericExecute;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericExecute;
                            }
                            break;
                        case "QueueGenericAll":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericAll;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericAll;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == messageQueueId)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "MessageQueue"));
            }
            if (null == user && null == group)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "User", "Group"));
            }

            this.Core.ParseForExtensionElements(node);

            if (null != user)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueueUserPermission");
                row[0] = id;
                row[1] = componentId;
                row[2] = messageQueueId;
                row[3] = user;
                row[4] = permissions;
            }
            if (null != group)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MessageQueueGroupPermission");
                row[0] = id;
                row[1] = componentId;
                row[2] = messageQueueId;
                row[3] = group;
                row[4] = permissions;
            }
        }

        /// <summary>
        /// Attempts to parse the input value as a GUID, and in case the value is a valid
        /// GUID returnes it in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}".
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string TryFormatGuidValue(string val)
        {
            try
            {
                Guid guid = new Guid(val);
                return guid.ToString("B").ToUpper();
            }
            catch (FormatException)
            {
                return val;
            }
            catch (OverflowException)
            {
                return val;
            }
        }
    }
}
