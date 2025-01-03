// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Util.Symbols;

    /// <summary>
    /// The decompiler for the WiX Toolset Utility Extension.
    /// </summary>
    internal sealed class UtilDecompiler : BaseWindowsInstallerDecompilerExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => UtilTableDefinitions.All;

        private static readonly Dictionary<string, XName> CustomActionMapping = new Dictionary<string, XName>()
        {
            {  "Wix4BroadcastEnvironmentChange_X86", UtilConstants.BroadcastEnvironmentChange },
            {  "Wix4BroadcastEnvironmentChange_X64", UtilConstants.BroadcastEnvironmentChange },
            {  "Wix4BroadcastEnvironmentChange_ARM64", UtilConstants.BroadcastEnvironmentChange },
            {  "Wix4BroadcastSettingChange_X86", UtilConstants.BroadcastSettingChange },
            {  "Wix4BroadcastSettingChange_X64", UtilConstants.BroadcastSettingChange },
            {  "Wix4BroadcastSettingChange_ARM64", UtilConstants.BroadcastSettingChange },
            {  "Wix4CheckRebootRequired_X86", UtilConstants.CheckRebootRequired },
            {  "Wix4CheckRebootRequired_X64", UtilConstants.CheckRebootRequired },
            {  "Wix4CheckRebootRequired_ARM64", UtilConstants.CheckRebootRequired },
            {  "Wix4QueryNativeMachine_X86", UtilConstants.QueryNativeMachine },
            {  "Wix4QueryNativeMachine_X64", UtilConstants.QueryNativeMachine },
            {  "Wix4QueryNativeMachine_ARM64", UtilConstants.QueryNativeMachine },
            {  "Wix4QueryOsDriverInfo_X86", UtilConstants.QueryWindowsDriverInfo },
            {  "Wix4QueryOsDriverInfo_X64", UtilConstants.QueryWindowsDriverInfo },
            {  "Wix4QueryOsDriverInfo_ARM64", UtilConstants.QueryWindowsDriverInfo },
            {  "Wix4QueryOsInfo_X86", UtilConstants.QueryWindowsSuiteInfo },
            {  "Wix4QueryOsInfo_X64", UtilConstants.QueryWindowsSuiteInfo },
            {  "Wix4QueryOsInfo_ARM64", UtilConstants.QueryWindowsSuiteInfo },
        };

        private IReadOnlyCollection<string> customActionNames;

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void PreDecompileTables(TableIndexedCollection tables)
        {
            this.RememberCustomActionNames(tables);
            this.CleanupSecureCustomProperties(tables);
            this.CleanupInternetShortcutRemoveFileTables(tables);
        }

        private void RememberCustomActionNames(TableIndexedCollection tables)
        {
            var customActionTable = tables["CustomAction"];
            this.customActionNames = customActionTable?.Rows.Select(r => r.GetPrimaryKey()).Distinct().ToList() ?? (IReadOnlyCollection<string>)Array.Empty<string>();
        }

        /// <summary>
        /// Decompile the SecureCustomProperties field to PropertyRefs for known extension properties.
        /// </summary>
        /// <remarks>
        /// If we've referenced any of the suite or directory properties, add
        /// a PropertyRef to refer to the Property (and associated custom action)
        /// from the extension's library. Then remove the property from
        /// SecureCustomExtensions property so later decompilation won't create
        /// new Property elements.
        /// </remarks>
        /// <param name="tables">The collection of all tables.</param>
        private void CleanupSecureCustomProperties(TableIndexedCollection tables)
        {
            var propertyTable = tables["Property"];

            if (null != propertyTable)
            {
                foreach (var row in propertyTable.Rows)
                {
                    if ("SecureCustomProperties" == row[0].ToString())
                    {
                        var remainingProperties = new StringBuilder();
                        var secureCustomProperties = row[1].ToString().Split(';');
                        foreach (var property in secureCustomProperties)
                        {
                            if (property.StartsWith("WIX_SUITE_", StringComparison.Ordinal) || property.StartsWith("WIX_DIR_", StringComparison.Ordinal)
                                || property.StartsWith("WIX_ACCOUNT_", StringComparison.Ordinal))
                            {
                                this.DecompilerHelper.AddElementToRoot("PropertyRef", new XAttribute("Id", property));
                            }
                            else
                            {
                                if (0 < remainingProperties.Length)
                                {
                                    remainingProperties.Append(";");
                                }
                                remainingProperties.Append(property);
                            }
                        }

                        row[1] = remainingProperties.ToString();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove RemoveFile rows that the InternetShortcut compiler extension adds for us.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void CleanupInternetShortcutRemoveFileTables(TableIndexedCollection tables)
        {
            // index the WixInternetShortcut table
            var wixInternetShortcutTable = tables["WixInternetShortcut"];
            var wixInternetShortcuts = new Hashtable();
            if (null != wixInternetShortcutTable)
            {
                foreach (var row in wixInternetShortcutTable.Rows)
                {
                    wixInternetShortcuts.Add(row.GetPrimaryKey(), row);
                }
            }

            // remove the RemoveFile rows with primary keys that match the WixInternetShortcut table's
            var removeFileTable = tables["RemoveFile"];
            if (null != removeFileTable)
            {
                for (var i = removeFileTable.Rows.Count - 1; 0 <= i; i--)
                {
                    if (null != wixInternetShortcuts[removeFileTable.Rows[i][0]])
                    {
                        removeFileTable.Rows.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override bool TryDecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixCloseApplication":
                case "Wix4CloseApplication":
                    this.DecompileWixCloseApplicationTable(table);
                    break;
                case "WixRemoveFolderEx":
                case "Wix4RemoveFolderEx":
                    this.DecompileWixRemoveFolderExTable(table);
                    break;
                case "WixRestartResource":
                case "Wix4RestartResource":
                    this.DecompileWixRestartResourceTable(table);
                    break;
                case "FileShare":
                case "Wix4FileShare":
                    this.DecompileFileShareTable(table);
                    break;
                case "FileSharePermissions":
                case "Wix4FileSharePermissions":
                    this.DecompileFileSharePermissionsTable(table);
                    break;
                case "WixInternetShortcut":
                case "Wix4InternetShortcut":
                    this.DecompileWixInternetShortcutTable(table);
                    break;
                case "Group":
                case "Wix4Group":
                    this.DecompileGroupTable(table);
                    break;
                case "Group6":
                case "Wix6Group":
                    this.DecompileGroup6Table(table);
                    break;
                case "GroupGroup":
                case "Wix6GroupGroup":
                    this.DecompileGroupGroup6Table(table);
                    break;
                case "Perfmon":
                case "Wix4Perfmon":
                    this.DecompilePerfmonTable(table);
                    break;
                case "PerfmonManifest":
                case "Wix4PerfmonManifest":
                    this.DecompilePerfmonManifestTable(table);
                    break;
                case "EventManifest":
                case "Wix4EventManifest":
                    this.DecompileEventManifestTable(table);
                    break;
                case "SecureObjects":
                case "Wix4SecureObjects":
                    this.DecompileSecureObjectsTable(table);
                    break;
                case "ServiceConfig":
                case "Wix4ServiceConfig":
                    this.DecompileServiceConfigTable(table);
                    break;
                case "User":
                case "Wix4User":
                    this.DecompileUserTable(table);
                    break;
                case "UserGroup":
                case "Wix4UserGroup":
                    this.DecompileUserGroupTable(table);
                    break;
                case "XmlConfig":
                case "Wix4XmlConfig":
                    this.DecompileXmlConfigTable(table);
                    break;
                case "XmlFile":
                case "Wix4XmlFile":
                    // XmlFile decompilation has been moved to FinalizeXmlFileTable function
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
            this.FinalizeCustomActions();
            this.FinalizePerfmonTable(tables);
            this.FinalizePerfmonManifestTable(tables);
            this.FinalizeSecureObjectsTable(tables);
            this.FinalizeServiceConfigTable(tables);
            this.FinalizeXmlConfigTable(tables);
            this.FinalizeXmlFileTable(tables);
            this.FinalizeEventManifestTable(tables);
        }

        /// <summary>
        /// Decompile the WixCloseApplication table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixCloseApplicationTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var attribute = row.FieldAsNullableInteger(4) ?? 0x2;

                this.DecompilerHelper.AddElementToRoot(UtilConstants.CloseApplicationName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Target", row.FieldAsString(1)),
                    AttributeIfNotNull("Description", row, 2),
                    AttributeIfNotNull("Content", row, 3),
                    AttributeIfNotNull("CloseMessage", 0x1 == (attribute & 0x1)),
                    AttributeIfNotNull("RebootPrompt", 0x2 == (attribute & 0x2)),
                    AttributeIfNotNull("ElevatedCloseMessage", 0x4 == (attribute & 0x4)),
                    NumericAttributeIfNotNull("Sequence", row, 5),
                    AttributeIfNotNull("Property", row, 6)
                    );
            }
        }

        /// <summary>
        /// Decompile the WixRemoveFolderEx table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixRemoveFolderExTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var on = String.Empty;
                var installMode = row.FieldAsInteger(3);
                switch (installMode)
                {
                    case (int)WixRemoveFolderExInstallMode.Install:
                        on = "install";
                        break;

                    case (int)WixRemoveFolderExInstallMode.Uninstall:
                        on = "uninstall";
                        break;

                    case (int)WixRemoveFolderExInstallMode.Both:
                        on = "both";
                        break;

                    default:
                        this.Messaging.Write(WarningMessages.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, "InstallMode", installMode));
                        break;
                }

                var removeFolder = new XElement(UtilConstants.RemoveFolderExName,
                    AttributeIfNotNull("Id", row, 0),
                    AttributeIfNotNull("Property", row, 2),
                    AttributeIfNotNull("On", on)
                    );

                // Add to the appropriate Component or section element.
                var componentId = row.FieldAsString(1);

                if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                {
                    component.Add(removeFolder);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                }
            }
        }

        /// <summary>
        /// Decompile the WixRestartResource table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixRestartResourceTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var restartResource = new XElement(UtilConstants.RestartResourceName,
                    new XAttribute("Id", row.FieldAsString(0)));

                // Determine the resource type and set accordingly.
                var resource = row.FieldAsString(2);
                var attributes = row.FieldAsInteger(3);
                var type = (WixRestartResourceAttributes)attributes;

                switch (type)
                {
                    case WixRestartResourceAttributes.Filename:
                        restartResource.Add(new XAttribute("Path", resource));
                        break;

                    case WixRestartResourceAttributes.ProcessName:
                        restartResource.Add(new XAttribute("ProcessName", resource));
                        break;

                    case WixRestartResourceAttributes.ServiceName:
                        restartResource.Add(new XAttribute("ServiceName", resource));
                        break;

                    default:
                        this.Messaging.Write(WarningMessages.UnrepresentableColumnValue(row.SourceLineNumbers, table.Name, "Attributes", attributes));
                        break;
                }

                // Add to the appropriate Component or section element.
                var componentId = row.FieldAsString(1);
                if (!String.IsNullOrEmpty(componentId))
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(restartResource);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }
                }
                else
                {
                    this.DecompilerHelper.AddElementToRoot(restartResource);
                }
            }
        }

        /// <summary>
        /// Decompile the FileShare table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileShareTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var fileShare = new XElement(UtilConstants.FileShareName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(1)),
                    AttributeIfNotNull("Description", row, 3)
                    );

                // the Directory_ column is set by the parent Component

                // the User_ and Permissions columns are deprecated

                if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(2), out var component))
                {
                    component.Add(fileShare);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Component_", (string)row[2], "Component"));
                }

                this.DecompilerHelper.IndexElement(row, fileShare);
            }
        }

        /// <summary>
        /// Decompile the FileSharePermissions table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileFileSharePermissionsTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var fileSharePermission = new XElement(UtilConstants.FileSharePermissionName,
                    new XAttribute("User", row.FieldAsString(1)));

                this.AddPermissionAttributes(fileSharePermission, row, 2, UtilConstants.FolderPermissions);

                if (this.DecompilerHelper.TryGetIndexedElement("Wix4FileShare", row.FieldAsString(0), out var fileShare) ||
                    this.DecompilerHelper.TryGetIndexedElement("FileShare", row.FieldAsString(0), out fileShare))
                {
                    fileShare.Add(fileSharePermission);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "FileShare_", (string)row[0], "Wix4FileShare"));
                }
            }
        }

        /// <summary>
        /// Decompile the Group table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileGroupTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                this.DecompilerHelper.AddElementToRoot(UtilConstants.GroupName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2)),
                    AttributeIfNotNull("Domain", row, 3)
                    );
            }
        }
        /// <summary>
        /// Decompile the Group6 table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileGroup6Table(Table table)
        {
            foreach (var row in table.Rows)
            {
                var groupId = row.FieldAsString(0);
                if (this.DecompilerHelper.TryGetIndexedElement("Group", groupId, out var group))
                {
                    var attributes = (Group6Symbol.SymbolAttributes)(row.FieldAsNullableInteger(2) ?? 0);
                    group.Add(AttributeIfNotNull("Comment", row, 1));
                    group.Add(AttributeIfTrue("FailIfExists", ((attributes & Group6Symbol.SymbolAttributes.FailIfExists) != 0)));
                    group.Add(AttributeIfTrue("UpdateIfExists", ((attributes & Group6Symbol.SymbolAttributes.UpdateIfExists) != 0)));
                    group.Add(AttributeIfTrue("DontRemoveOnUninstall", ((attributes & Group6Symbol.SymbolAttributes.DontRemoveOnUninstall) != 0)));
                    group.Add(AttributeIfTrue("DontCreateGroup", ((attributes & Group6Symbol.SymbolAttributes.DontCreateGroup) != 0)));
                    group.Add(AttributeIfTrue("NonVital", ((attributes & Group6Symbol.SymbolAttributes.NonVital) != 0)));
                    group.Add(AttributeIfTrue("RemoveComment", ((attributes & Group6Symbol.SymbolAttributes.RemoveComment) != 0)));
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Group_", groupId, "Group"));
                }
            }
        }


        /// <summary>
        /// Decompile the GroupGroup6 table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileGroupGroup6Table(Table table)
        {
            foreach (var row in table.Rows)
            {
                var parentId = row.FieldAsString(0);
                var parentExists = this.DecompilerHelper.TryGetIndexedElement("Group", parentId, out var parentGroup);

                var childId = row.FieldAsString(1);
                var childExists = this.DecompilerHelper.TryGetIndexedElement("Group", childId, out var childGroup);

                if (parentExists && childExists)
                {
                    childGroup.Add(new XElement(UtilConstants.GroupRefName, new XAttribute("Id", parentId)));
                }
                else
                {
                    if(!parentExists)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Parent_", parentId, "Group"));
                    }
                    if (!childExists)
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Child_", childId, "Group"));
                    }
                }
            }
        }

        /// <summary>
        /// Decompile the WixInternetShortcut table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixInternetShortcutTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var type = String.Empty;
                var shortcutType = (UtilCompiler.InternetShortcutType)row.FieldAsInteger(5);
                switch (shortcutType)
                {
                    case UtilCompiler.InternetShortcutType.Link:
                        type = "link";
                        break;
                    case UtilCompiler.InternetShortcutType.Url:
                        type = "url";
                        break;
                }

                var internetShortcut = new XElement(UtilConstants.InternetShortcutName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Directory", row.FieldAsString(2)),
                    new XAttribute("Name", Path.GetFileNameWithoutExtension(row.FieldAsString(3))), // remove .lnk/.url extension because compiler extension adds it back for us
                    new XAttribute("Type", type),
                    new XAttribute("Target", row.FieldAsString(4)),
                    new XAttribute("IconFile", row.FieldAsString(6)),
                    NumericAttributeIfNotNull("IconIndex", row, 7)
                    );

                var componentId = row.FieldAsString(1);
                if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                {
                    component.Add(internetShortcut);
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                }

                this.DecompilerHelper.IndexElement(row, internetShortcut);
            }
        }

        /// <summary>
        /// Decompile the Perfmon table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePerfmonTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                this.DecompilerHelper.IndexElement(row, new XElement(UtilConstants.PerfCounterName, new XAttribute("Name", row.FieldAsString(2))));
            }
        }

        /// <summary>
        /// Decompile the PerfmonManifest table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompilePerfmonManifestTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                this.DecompilerHelper.IndexElement(row, new XElement(UtilConstants.PerfCounterManifestName, new XAttribute("ResourceFileDirectory", row.FieldAsString(2))));
            }
        }

        /// <summary>
        /// Decompile the EventManifest table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileEventManifestTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                this.DecompilerHelper.IndexElement(row, new XElement(UtilConstants.EventManifestName));
            }
        }

        /// <summary>
        /// Decompile the SecureObjects table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSecureObjectsTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var permissionEx = new XElement(UtilConstants.PermissionExName,
                    AttributeIfNotNull("Domain", row, 2),
                    AttributeIfNotNull("User", row, 3)
                    );

                string[] specialPermissions;
                switch ((string)row[1])
                {
                    case "CreateFolder":
                        specialPermissions = UtilConstants.FolderPermissions;
                        break;
                    case "File":
                        specialPermissions = UtilConstants.FilePermissions;
                        break;
                    case "Registry":
                        specialPermissions = UtilConstants.RegistryPermissions;
                        break;
                    case "ServiceInstall":
                        specialPermissions = UtilConstants.ServicePermissions;
                        break;
                    default:
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, row.Table.Name, row.Fields[1].Column.Name, row[1]));
                        return;
                }

                this.AddPermissionAttributes(permissionEx, row, 4, specialPermissions);

                this.DecompilerHelper.IndexElement(row, permissionEx);
            }
        }

        /// <summary>
        /// Decompile the ServiceConfig table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileServiceConfigTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var serviceConfig = new XElement(UtilConstants.ServiceConfigName,
                    new XAttribute("ServiceName", row.FieldAsString(0)),
                    AttributeIfNotNull("FirstFailureActionType", row, 3),
                    AttributeIfNotNull("SecondFailureActionType", row, 4),
                    AttributeIfNotNull("ThirdFailureActionType", row, 5),
                    NumericAttributeIfNotNull("ResetPeriodInDays", row, 6),
                    NumericAttributeIfNotNull("RestartServiceDelayInSeconds", row, 7),
                    AttributeIfNotNull("ProgramCommandLine", row, 8),
                    AttributeIfNotNull("RebootMessage", row, 9)
                    );

                this.DecompilerHelper.IndexElement(row, serviceConfig);
            }
        }

        /// <summary>
        /// Decompile the User table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUserTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var attributes = row.FieldAsNullableInteger(6) ?? 0;

                var user = new XElement(UtilConstants.UserName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("Name", row.FieldAsString(2)),
                    AttributeIfNotNull("Domain", row, 3),
                    AttributeIfNotNull("Password", row, 4),
                    AttributeIfNotNull("Comment", row, 5),
                    AttributeIfTrue("PasswordNeverExpires", UtilCompiler.UserDontExpirePasswrd == (attributes & UtilCompiler.UserDontExpirePasswrd)),
                    AttributeIfTrue("CanNotChangePassword", UtilCompiler.UserPasswdCantChange == (attributes & UtilCompiler.UserPasswdCantChange)),
                    AttributeIfTrue("PasswordExpired", UtilCompiler.UserPasswdChangeReqdOnLogin == (attributes & UtilCompiler.UserPasswdChangeReqdOnLogin)),
                    AttributeIfTrue("Disabled", UtilCompiler.UserDisableAccount == (attributes & UtilCompiler.UserDisableAccount)),
                    AttributeIfTrue("FailIfExists", UtilCompiler.UserFailIfExists == (attributes & UtilCompiler.UserFailIfExists)),
                    AttributeIfTrue("UpdateIfExists", UtilCompiler.UserUpdateIfExists == (attributes & UtilCompiler.UserUpdateIfExists)),
                    AttributeIfTrue("LogonAsService", UtilCompiler.UserLogonAsService == (attributes & UtilCompiler.UserLogonAsService)),
                    AttributeIfTrue("LogonAsBatchJob", UtilCompiler.UserLogonAsBatchJob == (attributes & UtilCompiler.UserLogonAsBatchJob)),
                    AttributeIfTrue("RemoveComment", UtilCompiler.UserRemoveComment == (attributes & UtilCompiler.UserRemoveComment))
                    );

                if (UtilCompiler.UserDontRemoveOnUninstall == (attributes & UtilCompiler.UserDontRemoveOnUninstall))
                {
                    user.Add(new XAttribute("RemoveOnUninstall", "no"));
                }

                if (UtilCompiler.UserDontCreateUser == (attributes & UtilCompiler.UserDontCreateUser))
                {
                    user.Add(new XAttribute("CreateUser", "no"));
                }

                if (UtilCompiler.UserNonVital == (attributes & UtilCompiler.UserNonVital))
                {
                    user.Add(new XAttribute("Vital", "no"));
                }

                var componentId = row.FieldAsString(1);
                if (!String.IsNullOrEmpty(componentId))
                {
                    if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                    {
                        component.Add(user);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                    }
                }
                else
                {
                    this.DecompilerHelper.AddElementToRoot(user);
                }

                this.DecompilerHelper.IndexElement(row, user);
            }
        }

        /// <summary>
        /// Decompile the UserGroup table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileUserGroupTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var userId = row.FieldAsString(0);
                if (this.DecompilerHelper.TryGetIndexedElement("User", userId, out var user))
                {
                    user.Add(new XElement(UtilConstants.GroupRefName, new XAttribute("Id", row.FieldAsString(1))));
                }
                else
                {
                    this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(), "Group_", userId, "Group"));
                }
            }
        }

        /// <summary>
        /// Decompile the XmlConfig table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileXmlConfigTable(Table table)
        {
            foreach (var row in table.Rows)
            {
                var flags = row.FieldAsNullableInteger(7) ?? 0;
                string node = null;
                string action = null;
                string on = null;

                if (0x1 == (flags & 0x1))
                {
                    node = "element";
                }
                else if (0x2 == (flags & 0x2))
                {
                    node = "value";
                }
                else if (0x4 == (flags & 0x4))
                {
                    node = "document";
                }

                if (0x10 == (flags & 0x10))
                {
                    action = "create";
                }
                else if (0x20 == (flags & 0x20))
                {
                    action = "delete";
                }

                if (0x100 == (flags & 0x100))
                {
                    on = "install";
                }
                else if (0x200 == (flags & 0x200))
                {
                    on = "uninstall";
                }

                var xmlConfig = new XElement(UtilConstants.XmlConfigName,
                    new XAttribute("Id", row.FieldAsString(0)),
                    new XAttribute("File", row.FieldAsString(1)),
                    AttributeIfNotNull("ElementId", row, 2),
                    AttributeIfNotNull("ElementPath", row, 3),
                    AttributeIfNotNull("VerifyPath", row, 4),
                    AttributeIfNotNull("Name", row, 5),
                    AttributeIfNotNull("Value", row, 6),
                    AttributeIfNotNull("Node", node),
                    AttributeIfNotNull("Action", action),
                    AttributeIfNotNull("On", on),
                    AttributeIfTrue("PreserveModifiedDate", 0x00001000 == (flags & 0x00001000)),
                    NumericAttributeIfNotNull("Sequence", row, 9)
                    );

                this.DecompilerHelper.IndexElement(row, xmlConfig);
            }
        }

        private void FinalizeCustomActions()
        {
            foreach (var customActionName in this.customActionNames)
            {
                if (CustomActionMapping.TryGetValue(customActionName, out var elementName))
                {
                    this.DecompilerHelper.AddElementToRoot(elementName);
                }
            }
        }

        /// <summary>
        /// Finalize the Perfmon table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since the PerfCounter element nests under a File element, but
        /// the Perfmon table does not have a foreign key relationship with
        /// the File table (instead it has a formatted string that usually
        /// refers to a file row - but doesn't have to), the nesting must
        /// be inferred during finalization.
        /// </remarks>
        private void FinalizePerfmonTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("Perfmon", out var perfmonTable))
            {
                foreach (var row in perfmonTable.Rows)
                {
                    var formattedFile = row.FieldAsString(1);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        var fileId = formattedFile.Substring(2, formattedFile.Length - 3);
                        if (this.DecompilerHelper.TryGetIndexedElement("File", fileId, out var file))
                        {
                            var perfCounter = this.DecompilerHelper.GetIndexedElement(row);
                            file.Add(perfCounter);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, perfmonTable.Name, row.GetPrimaryKey(), "File", formattedFile, "File"));
                        }
                    }
                    else
                    {
                        this.Messaging.Write(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "Perfmon"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the PerfmonManifest table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizePerfmonManifestTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("PerfmonManifest", out var perfmonManifestTable))
            {
                foreach (var row in perfmonManifestTable.Rows)
                {
                    var formattedFile = row.FieldAsString(1);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        var perfCounterManifest = this.DecompilerHelper.GetIndexedElement(row);
                        var fileId = formattedFile.Substring(2, formattedFile.Length - 3);

                        if (this.DecompilerHelper.TryGetIndexedElement("File", fileId, out var file))
                        {
                            file.Add(perfCounterManifest);
                        }
                        else
                        {
                            var resourceFileDirectory = perfCounterManifest.Attribute("ResourceFileDirectory")?.Value;

                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, resourceFileDirectory, row.GetPrimaryKey(), "File", formattedFile, "File"));
                        }
                    }
                    else
                    {
                        this.Messaging.Write(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "PerfmonManifest"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the SecureObjects table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Nests the PermissionEx elements below their parent elements.  There are no declared foreign
        /// keys for the parents of the SecureObjects table.
        /// </remarks>
        private void FinalizeSecureObjectsTable(TableIndexedCollection tables)
        {
            var createFolderElementsByDirectoryId = new Dictionary<string, List<XElement>>();

            // index the CreateFolder table because the foreign key to this table from the
            // LockPermissions table is only part of the primary key of this table
            if (tables.TryGetTable("CreateFolder", out var createFolderTable))
            {
                foreach (var row in createFolderTable.Rows)
                {
                    var directoryId = row.FieldAsString(0);

                    if (!createFolderElementsByDirectoryId.TryGetValue(directoryId, out var createFolderElements))
                    {
                        createFolderElements = new List<XElement>();
                        createFolderElementsByDirectoryId.Add(directoryId, createFolderElements);
                    }

                    var createFolder = this.DecompilerHelper.GetIndexedElement(row);
                    createFolderElements.Add(createFolder);
                }
            }

            if (tables.TryGetTable("SecureObjects", out var secureObjectsTable))
            {
                foreach (var row in secureObjectsTable.Rows)
                {
                    var id = row.FieldAsString(0);
                    var table = row.FieldAsString(1);

                    var permissionEx = this.DecompilerHelper.GetIndexedElement(row);

                    if (table == "CreateFolder")
                    {
                        if (createFolderElementsByDirectoryId.TryGetValue(id, out var createFolderElements))
                        {
                            foreach (var createFolder in createFolderElements)
                            {
                                createFolder.Add(permissionEx);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "SecureObjects", row.GetPrimaryKey(), "LockObject", id, table));
                        }
                    }
                    else
                    {
                        var parentElement = this.DecompilerHelper.GetIndexedElement(table, id);

                        if (parentElement != null)
                        {
                            parentElement.Add(permissionEx);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, "SecureObjects", row.GetPrimaryKey(), "LockObject", id, table));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the ServiceConfig table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since there is no foreign key from the ServiceName column to the
        /// ServiceInstall table, this relationship must be handled late.
        /// </remarks>
        private void FinalizeServiceConfigTable(TableIndexedCollection tables)
        {
            //var serviceInstalls = new Hashtable();
            var serviceInstallElementsByName = new Dictionary<string, List<XElement>>();

            // index the ServiceInstall table because the foreign key used by the ServiceConfig
            // table is actually the ServiceInstall.Name, not the ServiceInstall.ServiceInstall
            // this is unfortunate because the service Name is not guaranteed to be unique, so
            // decompiler must assume there could be multiple matches and add the ServiceConfig to each
            // TODO: the Component column information should be taken into acount to accurately identify
            // the correct column to use
            if (tables.TryGetTable("ServiceInstall", out var serviceInstallTable))
            {
                foreach (var row in serviceInstallTable.Rows)
                {
                    var name = row.FieldAsString(1);

                    if (!serviceInstallElementsByName.TryGetValue(name, out var serviceInstallElements))
                    {
                        serviceInstallElements = new List<XElement>();
                        serviceInstallElementsByName.Add(name, serviceInstallElements);
                    }

                    var serviceInstall = this.DecompilerHelper.GetIndexedElement(row);
                    serviceInstallElements.Add(serviceInstall);
                }
            }

            if (tables.TryGetTable("ServiceConfig", out var serviceConfigTable))
            {
                foreach (var row in serviceConfigTable.Rows)
                {
                    var serviceConfig = this.DecompilerHelper.GetIndexedElement(row);

                    if (row.FieldAsInteger(2) == 0)
                    {
                        var componentId = row.FieldAsString(1);
                        if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                        {
                            component.Add(serviceConfig);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, serviceConfigTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                        }
                    }
                    else
                    {
                        var name = row.FieldAsString(0);
                        if (serviceInstallElementsByName.TryGetValue(name, out var serviceInstallElements))
                        {
                            foreach (var serviceInstall in serviceInstallElements)
                            {
                                serviceInstall.Add(serviceConfig);
                            }
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, serviceConfigTable.Name, row.GetPrimaryKey(), "ServiceName", name, "ServiceInstall"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the XmlConfig table.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void FinalizeXmlConfigTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("Wix4XmlConfig", out var xmlConfigTable))
            {
                foreach (var row in xmlConfigTable.Rows)
                {
                    var xmlConfig = this.DecompilerHelper.GetIndexedElement(row);

                    if (null != row[2])
                    {
                        var id = row.FieldAsString(2);
                        if (this.DecompilerHelper.TryGetIndexedElement("Wix4XmlConfig", id, out var parentXmlConfig))
                        {
                            parentXmlConfig.Add(xmlConfig);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, xmlConfigTable.Name, row.GetPrimaryKey(), "ElementPath", (string)row[2], "XmlConfig"));
                        }
                    }
                    else
                    {
                        var componentId = row.FieldAsString(8);
                        if (this.DecompilerHelper.TryGetIndexedElement("Component", componentId, out var component))
                        {
                            component.Add(xmlConfig);
                        }
                        else
                        {
                            this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, xmlConfigTable.Name, row.GetPrimaryKey(), "Component_", componentId, "Component"));
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Finalize the XmlFile table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Some of the XmlFile table rows are compiler generated from util:EventManifest node
        /// These rows should not be appended to component.
        /// </remarks>
        private void FinalizeXmlFileTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("XmlFile", out var xmlFileTable))
            {
                var eventManifestTable = tables["EventManifest"];

                foreach (var row in xmlFileTable.Rows)
                {
                    var manifestGenerated = false;
                    var xmlFileConfigId = (string)row[0];
                    if (null != eventManifestTable)
                    {
                        foreach (var emrow in eventManifestTable.Rows)
                        {
                            var formattedFile = (string)emrow[1];
                            if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                                && formattedFile.EndsWith("]", StringComparison.Ordinal))
                            {
                                var fileId = formattedFile.Substring(2, formattedFile.Length - 3);
                                if (String.Equals(String.Concat("Config_", fileId, "ResourceFile"), xmlFileConfigId))
                                {
                                    if (this.DecompilerHelper.TryGetIndexedElement(emrow, out var eventManifest))
                                    {
                                        eventManifest.Add(new XAttribute("ResourceFile", row.FieldAsString(4)));
                                    }
                                    manifestGenerated = true;
                                }

                                else if (String.Equals(String.Concat("Config_", fileId, "MessageFile"), xmlFileConfigId))
                                {
                                    if (this.DecompilerHelper.TryGetIndexedElement(emrow, out var eventManifest))
                                    {
                                        eventManifest.Add(new XAttribute("MessageFile", row.FieldAsString(4)));
                                    }
                                    manifestGenerated = true;
                                }
                            }
                        }
                    }

                    if (manifestGenerated)
                    {
                        continue;
                    }

                    var action = "setValue";
                    var flags = row.FieldAsInteger(5);
                    if (0x1 == (flags & 0x1) && 0x2 == (flags & 0x2))
                    {
                        this.Messaging.Write(WarningMessages.IllegalColumnValue(row.SourceLineNumbers, xmlFileTable.Name, row.Fields[5].Column.Name, row[5]));
                    }
                    else if (0x1 == (flags & 0x1))
                    {
                        action = "createElement";
                    }
                    else if (0x2 == (flags & 0x2))
                    {
                        action = "deleteValue";
                    }

                    var selectionLanguage = (0x100 == (flags & 0x100)) ? "XPath" : null;
                    var preserveModifiedDate = 0x00001000 == (flags & 0x00001000);
                    var permanent = 0x00010000 == (flags & 0x00010000);

                    if (this.DecompilerHelper.TryGetIndexedElement("Component", row.FieldAsString(6), out var component))
                    {
                        var xmlFile = new XElement(UtilConstants.XmlFileName,
                            AttributeIfNotNull("Id", row, 0),
                            AttributeIfNotNull("File", row, 1),
                            AttributeIfNotNull("ElementPath", row, 2),
                            AttributeIfNotNull("Name", row, 3),
                            AttributeIfNotNull("Value", row, 4),
                            AttributeIfNotNull("Action", action),
                            AttributeIfNotNull("SelectionLanguage", selectionLanguage),
                            AttributeIfTrue("PreserveModifiedDate", preserveModifiedDate),
                            AttributeIfTrue("Permanent", permanent),
                            NumericAttributeIfNotNull("Sequence", row, 7)
                            );

                        component.Add(xmlFile);
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.ExpectedForeignRow(row.SourceLineNumbers, xmlFileTable.Name, row.GetPrimaryKey(), "Component_", (string)row[6], "Component"));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the eventManifest table.
        /// This function must be called after FinalizeXmlFileTable
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        private void FinalizeEventManifestTable(TableIndexedCollection tables)
        {
            if (tables.TryGetTable("EventManifest", out var eventManifestTable))
            {
                foreach (var row in eventManifestTable.Rows)
                {
                    var eventManifest = this.DecompilerHelper.GetIndexedElement(row);
                    var formattedFile = row.FieldAsString(1);

                    // try to "de-format" the File column's value to determine the proper parent File element
                    if ((formattedFile.StartsWith("[#", StringComparison.Ordinal) || formattedFile.StartsWith("[!", StringComparison.Ordinal))
                        && formattedFile.EndsWith("]", StringComparison.Ordinal))
                    {
                        var fileId = formattedFile.Substring(2, formattedFile.Length - 3);

                        if (this.DecompilerHelper.TryGetIndexedElement("File", fileId, out var file))
                        {
                            file.Add(eventManifest);
                        }
                    }
                    else
                    {
                        this.Messaging.Write(UtilErrors.IllegalFileValueInPerfmonOrManifest(formattedFile, "EventManifest"));
                    }
                }
            }
        }

        private void AddPermissionAttributes(XElement element, Row row, int column, string[] specialPermissions)
        {
            var permissions = row.FieldAsInteger(column);
            for (var i = 0; i < 32; i++)
            {
                if (0 != ((permissions >> i) & 1))
                {
                    string name = null;

                    if (16 > i && specialPermissions.Length > i)
                    {
                        name = specialPermissions[i];
                    }
                    else if (28 > i && UtilConstants.StandardPermissions.Length > (i - 16))
                    {
                        name = UtilConstants.StandardPermissions[i - 16];
                    }
                    else if (0 <= (i - 28) && UtilConstants.GenericPermissions.Length > (i - 28))
                    {
                        name = UtilConstants.GenericPermissions[i - 28];
                    }

                    if (!String.IsNullOrEmpty(name))
                    {
                        element.Add(new XAttribute(name, "yes"));
                    }
                    else
                    {
                        this.Messaging.Write(WarningMessages.UnknownPermission(row.SourceLineNumbers, row.Table.Name, row.GetPrimaryKey(), i));
                    }
                }
            }
        }

        private static XAttribute AttributeIfNotNull(string name, string value)
        {
            return value == null ? null : new XAttribute(name, value);
        }

        private static XAttribute AttributeIfNotNull(string name, bool value)
        {
            return new XAttribute(name, value ? "yes" : "no");
        }

        private static XAttribute AttributeIfNotNull(string name, Row row, int field)
        {
            if (row[field] != null)
            {
                return new XAttribute(name, row.FieldAsString(field));
            }

            return null;
        }

        private static XAttribute NumericAttributeIfNotNull(string name, Row row, int field)
        {
            if (row[field] != null)
            {
                return new XAttribute(name, row.FieldAsInteger(field));
            }

            return null;
        }

        private static XAttribute AttributeIfTrue(string name, bool value)
        {
            return value ? new XAttribute(name, "yes") : null;
        }
    }

    internal static class XElementExtensions
    {
        public static XElement AttributeIfNotNull(this XElement element, string name, Row row, int field)
        {
            if (row[field] != null)
            {
                element.Add(new XAttribute(name, row.FieldAsString(field)));
            }

            return element;
        }

        public static XElement NumericAttributeIfNotNull(this XElement element, string name, Row row, int field)
        {
            if (row[field] != null)
            {
                element.Add(new XAttribute(name, row.FieldAsInteger(field)));
            }

            return element;
        }
    }
}
