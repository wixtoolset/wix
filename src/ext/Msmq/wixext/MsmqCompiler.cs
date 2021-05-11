// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Msmq.Symbols;

    /// <summary>
    /// The compiler for the WiX Toolset MSMQ Extension.
    /// </summary>
    public sealed class MsmqCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/msmq";

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
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    var componentId = context["ComponentId"];
                    var directoryId = context["DirectoryId"];

                    switch (element.Name.LocalName)
                    {
                        case "MessageQueue":
                            this.ParseMessageQueueElement(intermediate, section, element, componentId);
                            break;
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(intermediate, section, element, componentId, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        ///	<summary>
        ///	Parses an MSMQ message queue element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        private void ParseMessageQueueElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier id = null;
            var basePriority = CompilerConstants.IntegerNotSet;
            var journalQuota = CompilerConstants.IntegerNotSet;
            string label = null;
            string multicastAddress = null;
            string pathName = null;
            var privLevel = CompilerConstants.IntegerNotSet;
            var quota = CompilerConstants.IntegerNotSet;
            string serviceTypeGuid = null;
            int attributes = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Authenticate":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Authenticate;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Authenticate;
                            }
                            break;
                        case "BasePriority":
                            basePriority = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Journal":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Journal;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Journal;
                            }
                            break;
                        case "JournalQuota":
                            journalQuota = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Label":
                            label = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MulticastAddress":
                            multicastAddress = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PathName":
                            pathName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PrivLevel":
                            var privLevelAttr = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "MessageQueue", "PrivLevel", privLevelAttr, "none", "body", "optional"));
                                    break;
                            }
                            break;
                        case "Quota":
                            quota = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Transactional":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)MqiMessageQueueAttributes.Transactional;
                            }
                            else
                            {
                                attributes &= ~(int)MqiMessageQueueAttributes.Transactional;
                            }
                            break;
                        case "ServiceTypeGuid":
                            serviceTypeGuid = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MessageQueuePermission":
                            this.ParseMessageQueuePermissionElement(intermediate, section, child, componentId, id?.Id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, node, child);
                }
            }

            var symbol = section.AddSymbol(new MessageQueueSymbol(sourceLineNumbers, id)
            {
                ComponentRef = componentId,
                Label = label,
                MulticastAddress = multicastAddress,
                PathName = pathName,
                ServiceTypeGuid = serviceTypeGuid,
                Attributes = attributes,
            });

            if (CompilerConstants.IntegerNotSet != basePriority)
            {
                symbol.BasePriority = basePriority;
            }
            if (CompilerConstants.IntegerNotSet != journalQuota)
            {
                symbol.JournalQuota = journalQuota;
            }

            if (CompilerConstants.IntegerNotSet != privLevel)
            {
                symbol.PrivLevel = privLevel;
            }
            if (CompilerConstants.IntegerNotSet != quota)
            {
                symbol.Quota = quota;
            }

            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, "MessageQueuingInstall");
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, "MessageQueuingUninstall");
        }

        ///	<summary>
        ///	Parses an MSMQ message queue permission element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent message queue.</param>
        private void ParseMessageQueuePermissionElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId, string messageQueueId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier id = null;
            string user = null;
            string group = null;
            int permissions = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "MessageQueue":
                            if (null != messageQueueId)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            messageQueueId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, MsmqSymbolDefinitions.MessageQueue, messageQueueId);
                            break;
                        case "User":
                            if (null != group)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "User", "Group"));
                            }
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
                            break;
                        case "Group":
                            if (null != user)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Group", "User"));
                            }
                            group = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Group", group);
                            break;
                        case "DeleteMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteMessage;
                            }
                            break;
                        case "PeekMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.PeekMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.PeekMessage;
                            }
                            break;
                        case "WriteMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.WriteMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.WriteMessage;
                            }
                            break;
                        case "DeleteJournalMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteJournalMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteJournalMessage;
                            }
                            break;
                        case "SetQueueProperties":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.SetQueueProperties;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.SetQueueProperties;
                            }
                            break;
                        case "GetQueueProperties":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.GetQueueProperties;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.GetQueueProperties;
                            }
                            break;
                        case "DeleteQueue":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.DeleteQueue;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.DeleteQueue;
                            }
                            break;
                        case "GetQueuePermissions":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.GetQueuePermissions;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.GetQueuePermissions;
                            }
                            break;
                        case "ChangeQueuePermissions":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ChangeQueuePermissions;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ChangeQueuePermissions;
                            }
                            break;
                        case "TakeQueueOwnership":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.TakeQueueOwnership;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.TakeQueueOwnership;
                            }
                            break;
                        case "ReceiveMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ReceiveMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ReceiveMessage;
                            }
                            break;
                        case "ReceiveJournalMessage":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.ReceiveJournalMessage;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.ReceiveJournalMessage;
                            }
                            break;
                        case "QueueGenericRead":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericRead;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericRead;
                            }
                            break;
                        case "QueueGenericWrite":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericWrite;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericWrite;
                            }
                            break;
                        case "QueueGenericExecute":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericExecute;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericExecute;
                            }
                            break;
                        case "QueueGenericAll":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                permissions |= (int)MqiMessageQueuePermission.QueueGenericAll;
                            }
                            else
                            {
                                permissions &= ~(int)MqiMessageQueuePermission.QueueGenericAll;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("mqp", componentId, messageQueueId, user, group);
            }

            if (null == messageQueueId)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "MessageQueue"));
            }
            if (null == user && null == group)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "User", "Group"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (null != user)
            {
                section.AddSymbol(new MessageQueueUserPermissionSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    MessageQueueRef = messageQueueId,
                    UserRef = user,
                    Permissions = permissions,
                });
            }
            if (null != group)
            {
                section.AddSymbol(new MessageQueueGroupPermissionSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    MessageQueueRef = messageQueueId,
                    GroupRef = group,
                    Permissions = permissions,
                });
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
            if (!Guid.TryParse(val, out var guid))
            {
                return val;
            }
            return guid.ToString("B").ToUpper();
        }
    }
}
