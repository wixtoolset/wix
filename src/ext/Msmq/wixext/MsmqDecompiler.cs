// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The decompiler for the WiX Toolset MSMQ Extension.
    /// </summary>
    public sealed class MsmqDecompiler : BaseWindowsInstallerDecompilerExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => MsmqTableDefinitions.All;

        private IParseHelper ParseHelper { get; set; }

        internal static XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/msmq";
        internal static XName MessageQueueName => Namespace + "MessageQueue";
        internal static XName MessageQueuePermissionName => Namespace + "MessageQueuePermission";

        public override void PreDecompile(IWindowsInstallerDecompileContext context, IWindowsInstallerDecompilerHelper helper)
        {
            base.PreDecompile(context, helper);
            this.ParseHelper = context.ServiceProvider.GetService<IParseHelper>();
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PreDecompileTables(TableIndexedCollection tables)
        {
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override bool TryDecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "MessageQueue":
                case "Wix4MessageQueue":
                    this.DecompileMessageQueueTable(table);
                    break;
                case "MessageQueueUserPermission":
                case "Wix4MessageQueueUserPermission":
                    this.DecompileMessageQueueUserPermissionTable(table);
                    break;
                case "MessageQueueGroupPermission":
                case "Wix4MessageQueueGroupPermission":
                    this.DecompileMessageQueueGroupPermissionTable(table);
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PostDecompileTables(TableIndexedCollection tables)
        {
            this.FinalizeMessageQueueTable(tables);
            this.FinalizeMessageQueueUserPermissionTable(tables);
            this.FinalizeMessageQueueGroupPermissionTable(tables);
        }

        /// <summary>
        /// Decompile the MessageQueue table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var messageQueue = new XElement(MessageQueueName,
                    new XAttribute("Id", row.FieldAsString(0))
                    );

                // Column(1) Component_ resolved in FinalizeMessageQueueTable

                if (!row.IsColumnEmpty(2))
                {
                    messageQueue.Add(new XAttribute("BasePriority", row.FieldAsString(2)));
                }

                if (!row.IsColumnEmpty(3))
                {
                    messageQueue.Add(new XAttribute("JournalQuota", row.FieldAsString(3)));
                }

                messageQueue.Add(new XAttribute("Label", row.FieldAsString(4)));

                if (!row.IsColumnEmpty(5))
                {
                    messageQueue.Add(new XAttribute("MulticastAddress", row.FieldAsString(5)));
                }

                messageQueue.Add(new XAttribute("PathName", row.FieldAsString(6)));

                
                if (!row.IsColumnEmpty(7))
                {
                    int privLevel = row.FieldAsInteger(7);
                    switch ((MsmqCompiler.MqiMessageQueuePrivacyLevel)privLevel)
                    {
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.None:
                            messageQueue.Add(new XAttribute("PrivLevel", "none"));
                            break;
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.Optional:
                            messageQueue.Add(new XAttribute("PrivLevel", "optional"));
                            break;
                        case MsmqCompiler.MqiMessageQueuePrivacyLevel.Body:
                            messageQueue.Add(new XAttribute("PrivLevel", "body"));
                            break;
                        default:
                            break;
                    }
                }

                if (!row.IsColumnEmpty(8))
                {
                    messageQueue.Add(new XAttribute("Quota", row.FieldAsString(8)));
                }

                if (!row.IsColumnEmpty(9))
                {
                    messageQueue.Add(new XAttribute("ServiceTypeGuid", row.FieldAsString(9)));
                }

                int attributes = row.FieldAsInteger(10);

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Authenticate))
                {
                    messageQueue.Add(new XAttribute("Authenticate", "yes"));
                }

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Journal))
                {
                    messageQueue.Add(new XAttribute("Journal", "yes"));
                }

                if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueueAttributes.Transactional))
                {
                    messageQueue.Add(new XAttribute("Transactional", "yes"));
                }

                this.DecompilerHelper.IndexElement(row, messageQueue);
            }

        }

        /// <summary>
        /// Finalize the MessageQueue table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeMessageQueueTable(TableIndexedCollection tables)
        {
            Table messageQueueTable;
            if (tables.TryGetTable("MessageQueue", out messageQueueTable)
                || tables.TryGetTable("Wix4MessageQueue", out messageQueueTable))
            {
                foreach (var row in messageQueueTable.Rows)
                {
                    var xmlConfig = this.DecompilerHelper.GetIndexedElement(row);

                    var componentId = row.FieldAsString(1);
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(xmlConfig);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the MessageQueueUserPermission table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueUserPermissionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var queuePermission = new XElement(MessageQueuePermissionName,
                    new XAttribute("Id", row.FieldAsString(0))
                    );

                DecompileMessageQueuePermissionAttributes(row, queuePermission);
                this.DecompilerHelper.IndexElement(row, queuePermission);
            }

        }

        /// <summary>
        /// Finalize the MessageQueueUserPermissionTable table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeMessageQueueUserPermissionTable(TableIndexedCollection tables)
        {
            Table messageQueueUserPermissionTable;
            if (tables.TryGetTable("MessageQueueUserPermission", out messageQueueUserPermissionTable)
                || tables.TryGetTable("Wix4MessageQueueUserPermission", out messageQueueUserPermissionTable))
            {
                foreach (var row in messageQueueUserPermissionTable.Rows)
                {
                    var xmlConfig = this.DecompilerHelper.GetIndexedElement(row);

                    var componentId = row.FieldAsString(1);
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(xmlConfig);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueUserPermissionTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }

                    var messageQueueId = row.FieldAsString(2);
                    XElement messageQueue;
                    if (this.DecompilerHelper.TryGetIndexedElement("MessageQueue", messageQueueId, out messageQueue)
                        || this.DecompilerHelper.TryGetIndexedElement("Wix4MessageQueue", messageQueueId, out messageQueue))
                    {
                        xmlConfig.Add(new XAttribute("MessageQueue", messageQueueId));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueUserPermissionTable.Name, row.GetPrimaryKey(), "MessageQueue_", messageQueueId, "Wix4MessageQueue"));
                    }

                    var userId = row.FieldAsString(3);
                    XElement user;
                    if (this.DecompilerHelper.TryGetIndexedElement("User", userId, out user)
                        || this.DecompilerHelper.TryGetIndexedElement("Wix4User", userId, out user))
                    {
                        xmlConfig.Add(new XAttribute("User", userId));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueUserPermissionTable.Name, row.GetPrimaryKey(), "User_", userId, "Wix4User"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the MessageQueueGroupPermission table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMessageQueueGroupPermissionTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                var queuePermission = new XElement(MessageQueuePermissionName,
                    new XAttribute("Id", row.FieldAsString(0))
                    );

                DecompileMessageQueuePermissionAttributes(row, queuePermission);
                this.DecompilerHelper.IndexElement(row, queuePermission);
            }
        }

        /// <summary>
        /// Finalize the MessageQueueGroupPermissionTable table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeMessageQueueGroupPermissionTable(TableIndexedCollection tables)
        {
            Table messageQueueGroupPermissionTable;
            if (tables.TryGetTable("MessageQueueGroupPermission", out messageQueueGroupPermissionTable)
                || tables.TryGetTable("Wix4MessageQueueGroupPermission", out messageQueueGroupPermissionTable))
            {
                foreach (var row in messageQueueGroupPermissionTable.Rows)
                {
                    var xmlConfig = this.DecompilerHelper.GetIndexedElement(row);

                    var componentId = row.FieldAsString(1);
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(xmlConfig);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueGroupPermissionTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }

                    var messageQueueId = row.FieldAsString(2);
                    XElement messageQueue;
                    if (this.DecompilerHelper.TryGetIndexedElement("MessageQueue", messageQueueId, out messageQueue)
                        || this.DecompilerHelper.TryGetIndexedElement("Wix4MessageQueue", messageQueueId, out messageQueue))
                    {
                        xmlConfig.Add(new XAttribute("MessageQueue", messageQueueId));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueGroupPermissionTable.Name, row.GetPrimaryKey(), "MessageQueue_", messageQueueId, "Wix4MessageQueue"));
                    }

                    var groupId = row.FieldAsString(3);
                    XElement group;
                    if (this.DecompilerHelper.TryGetIndexedElement("Group", groupId, out group)
                        || this.DecompilerHelper.TryGetIndexedElement("Wix4Group", groupId, out group))
                    {
                        xmlConfig.Add(new XAttribute("Group", groupId));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, messageQueueGroupPermissionTable.Name, row.GetPrimaryKey(), "Group_", groupId, "Wix4Group"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile row attributes for the MessageQueueUserPermission and MessageQueueGroupPermission tables.
        /// </summary>
        /// <param name="row">The row to decompile.</param>
        /// <param name="table">Target element.</param>
        private void DecompileMessageQueuePermissionAttributes(Row row, XElement element)
        {
            int attributes = row.FieldAsInteger(4);

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteMessage))
            {
                element.Add(new XAttribute("DeleteMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.PeekMessage))
            {
                element.Add(new XAttribute("PeekMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.WriteMessage))
            {
                element.Add(new XAttribute("WriteMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteJournalMessage))
            {
                element.Add(new XAttribute("DeleteJournalMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.SetQueueProperties))
            {
                element.Add(new XAttribute("SetQueueProperties", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.GetQueueProperties))
            {
                element.Add(new XAttribute("GetQueueProperties", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.DeleteQueue))
            {
                element.Add(new XAttribute("DeleteQueue", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.GetQueuePermissions))
            {
                element.Add(new XAttribute("GetQueuePermissions", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ChangeQueuePermissions))
            {
                element.Add(new XAttribute("ChangeQueuePermissions", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.TakeQueueOwnership))
            {
                element.Add(new XAttribute("TakeQueueOwnership", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ReceiveMessage))
            {
                element.Add(new XAttribute("ReceiveMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.ReceiveJournalMessage))
            {
                element.Add(new XAttribute("ReceiveJournalMessage", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericRead))
            {
                element.Add(new XAttribute("QueueGenericRead", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericWrite))
            {
                element.Add(new XAttribute("QueueGenericWrite", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericExecute))
            {
                element.Add(new XAttribute("QueueGenericExecute", "yes"));
            }

            if (0 != (attributes & (int)MsmqCompiler.MqiMessageQueuePermission.QueueGenericAll))
            {
                element.Add(new XAttribute("QueueGenericAll", "yes"));
            }
        }
    }
}
