// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BundleValidator : IBundleValidator
    {
        public BundleValidator(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        protected IMessaging Messaging { get; }

        private enum ValueListKind
        {
            /// <summary>
            /// A list of values with nothing before the final value.
            /// </summary>
            None,

            /// <summary>
            /// A list of values with 'and' before the final value.
            /// </summary>
            And,

            /// <summary>
            /// A list of values with 'or' before the final value.
            /// </summary>
            Or,
        }

        // Built-in variables (from burn\engine\variable.cpp, "vrgBuiltInVariables", around line 225)
        private static readonly List<string> BuiltinBundleVariables = new List<string>(
            new string[] {
                "AdminToolsFolder",
                "AppDataFolder",
                "CommonAppDataFolder",
                "CommonFiles64Folder",
                "CommonFiles6432Folder",
                "CommonFilesFolder",
                "CompatibilityMode",
                "ComputerName",
                "Date",
                "DesktopFolder",
                "FavoritesFolder",
                "FontsFolder",
                "InstallerName",
                "InstallerVersion",
                "LocalAppDataFolder",
                "LogonUser",
                "MyPicturesFolder",
                "NativeMachine",
                "NTProductType",
                "NTSuiteBackOffice",
                "NTSuiteDataCenter",
                "NTSuiteEnterprise",
                "NTSuitePersonal",
                "NTSuiteSmallBusiness",
                "NTSuiteSmallBusinessRestricted",
                "NTSuiteWebServer",
                "PersonalFolder",
                "Privileged",
                "ProcessorArchitecture",
                "ProgramFiles64Folder",
                "ProgramFiles6432Folder",
                "ProgramFilesFolder",
                "ProgramMenuFolder",
                "RebootPending",
                "SendToFolder",
                "ServicePackLevel",
                "StartMenuFolder",
                "StartupFolder",
                "System64Folder",
                "SystemFolder",
                "SystemLanguageID",
                "TempFolder",
                "TemplateFolder",
                "TerminalServer",
                "UserLanguageID",
                "UserUILanguageID",
                "VersionMsi",
                "VersionNT",
                "VersionNT64",
                "WindowsBuildNumber",
                "WindowsFolder",
                "WindowsVolume",
                "WixBundleAction",
                "WixBundleActiveParent",
                "WixBundleCommandLineAction",
                "WixBundleElevated",
                "WixBundleExecutePackageAction",
                "WixBundleExecutePackageCacheFolder",
                "WixBundleForcedRestartPackage",
                "WixBundleInstalled",
                "WixBundleProviderKey",
                "WixBundleSourceProcessFolder",
                "WixBundleSourceProcessPath",
                "WixBundleTag",
                "WixBundleUILevel",
                "WixBundleVersion",
            });

        // Well-known variables (from burn\engine\variable.cpp, "vrgWellKnownVariables", around line 304)
        private static readonly List<string> WellKnownBundleVariables = new List<string>(
            new string[] {
                "WixBundleInProgressName",
                "WixBundleLastUsedSource",
                "WixBundleLayoutDirectory",
                "WixBundleLog",
                "WixBundleLog_*",
                "WixBundleRollbackLog_*",
                "WixBundleManufacturer",
                "WixBundleName",
                "WixBundleOriginalSource",
                "WixBundleOriginalSourceFolder",
            });

        private static readonly List<string> DisallowedMsiProperties = new List<string>(
            new string[] {
                "ACTION",
                "ADDLOCAL",
                "ADDSOURCE",
                "ADDDEFAULT",
                "ADVERTISE",
                "ALLUSERS",
                "REBOOT",
                "REINSTALL",
                "REINSTALLMODE",
                "REMOVE"
            });

        private static readonly List<string> UnavailableStartupVariables = new List<string>(
            new string[] {
                "RebootPending",
                "WixBundleAction",
                "WixBundleInstalled",
            });

        private static readonly List<string> UnavailableDetectVariables = new List<string>(
            new string[] {
                "WixBundleAction",
            });

        public string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath)
        {
            const string root = @"C:\";
            if (!Path.IsPathRooted(relativePath))
            {
                var normalizedPath = Path.GetFullPath(root + relativePath);
                if (normalizedPath.StartsWith(root))
                {
                    var canonicalizedPath = normalizedPath.Substring(root.Length);
                    if (canonicalizedPath != relativePath)
                    {
                        this.Messaging.Write(WarningMessages.PathCanonicalized(sourceLineNumbers, elementName, attributeName, relativePath, canonicalizedPath));
                    }
                    return canonicalizedPath;
                }
            }

            this.Messaging.Write(ErrorMessages.PayloadMustBeRelativeToCache(sourceLineNumbers, elementName, attributeName, relativePath));
            return relativePath;
        }

        public bool ValidateBundleVariableNameDeclaration(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName)
        {
            if (String.IsNullOrEmpty(variableName))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, elementName, attributeName));

                return false;
            }
            else if (!Common.IsBundleVariableName(variableName))
            {
                this.Messaging.Write(CompilerErrors.IllegalBundleVariableName(sourceLineNumbers, elementName, attributeName, variableName));

                return false;
            }
            else if (BuiltinBundleVariables.Contains(variableName))
            {
                var illegalValues = CreateValueList(ValueListKind.Or, BuiltinBundleVariables);
                this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithIllegalList(sourceLineNumbers, elementName, attributeName, variableName, illegalValues));

                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ValidateBundleVariableNameValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName, BundleVariableNameRule nameRule)
        {
            if (String.IsNullOrEmpty(variableName))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, elementName, attributeName));

                return false;
            }
            else if (!Common.IsBundleVariableName(variableName))
            {
                this.Messaging.Write(CompilerErrors.IllegalBundleVariableName(sourceLineNumbers, elementName, attributeName, variableName));

                return false;
            }
            else if (BuiltinBundleVariables.Contains(variableName))
            {
                var allowed = nameRule.HasFlag(BundleVariableNameRule.CanBeBuiltIn);
                if (!allowed)
                {
                    var illegalValues = CreateValueList(ValueListKind.Or, BuiltinBundleVariables);
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithIllegalList(sourceLineNumbers, elementName, attributeName, variableName, illegalValues));
                }

                return allowed;
            }
            else if (WellKnownBundleVariables.Contains(variableName) ||
                variableName.StartsWith("WixBundleLog_", StringComparison.OrdinalIgnoreCase) ||
                variableName.StartsWith("WixBundleRollbackLog_", StringComparison.OrdinalIgnoreCase))
            {
                var allowed = nameRule.HasFlag(BundleVariableNameRule.CanBeWellKnown);
                if (!allowed)
                {
                    var illegalValues = CreateValueList(ValueListKind.Or, WellKnownBundleVariables);
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithIllegalList(sourceLineNumbers, elementName, attributeName, variableName, illegalValues));
                }

                return allowed;
            }
            else
            {
                return true;
            }
        }

        public bool ValidateBundleVariableNameTarget(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName)
        {
            if (String.IsNullOrEmpty(variableName))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, elementName, attributeName));

                return false;
            }
            else if (!Common.IsBundleVariableName(variableName))
            {
                this.Messaging.Write(CompilerErrors.IllegalBundleVariableName(sourceLineNumbers, elementName, attributeName, variableName));

                return false;
            }
            else if (BuiltinBundleVariables.Contains(variableName))
            {
                var illegalValues = CreateValueList(ValueListKind.Or, BuiltinBundleVariables);
                this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithIllegalList(sourceLineNumbers, elementName, attributeName, variableName, illegalValues));

                return false;
            }
            else if (variableName.Equals("WixBundleLog", StringComparison.OrdinalIgnoreCase) ||
                     variableName.StartsWith("WixBundleLog_", StringComparison.OrdinalIgnoreCase) ||
                     variableName.StartsWith("WixBundleRollbackLog_", StringComparison.OrdinalIgnoreCase))
            {
                this.Messaging.Write(CompilerWarnings.ReadonlyLogVariableTarget(sourceLineNumbers, elementName, attributeName, variableName));

                return true;
            }
            else if (WellKnownBundleVariables.Contains(variableName))
            {
                return true;
            }
            else
            {
                return true;
            }
        }

        public bool ValidateBundleMsiPropertyName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, elementName, attributeName));

                return false;
            }
            else if (DisallowedMsiProperties.Contains(propertyName))
            {
                var illegalValues = CreateValueList(ValueListKind.Or, DisallowedMsiProperties);
                this.Messaging.Write(ErrorMessages.DisallowedMsiProperty(sourceLineNumbers, propertyName, illegalValues));

                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ValidateBundleCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition, BundleConditionPhase phase)
        {
            if (!this.TryParseCondition(sourceLineNumbers, elementName, attributeName, condition))
            {
                return false;
            }


            // TODO: These lists are incomplete.
            List<string> unavailableVariables = null;
            switch (phase)
            {
                case BundleConditionPhase.Startup:
                    unavailableVariables = UnavailableStartupVariables;
                    break;
                case BundleConditionPhase.Detect:
                    unavailableVariables = UnavailableDetectVariables;
                    break;
            }

            if (unavailableVariables != null)
            {
                return this.ValidateBundleConditionUnavailableVariables(sourceLineNumbers, elementName, attributeName, condition, unavailableVariables);
            }
            else
            {
                return true;
            }
        }

        private bool ValidateBundleConditionUnavailableVariables(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition, List<string> unavailableVariables)
        {
            foreach (var variableName in unavailableVariables)
            {
                //TODO: use the results of parsing to validate that the restricted variables are actually used as variables
                if (condition.Contains(variableName))
                {
                    var illegalValues = CreateValueList(ValueListKind.Or, unavailableVariables);
                    this.Messaging.Write(WarningMessages.UnavailableBundleConditionVariable(sourceLineNumbers, elementName, attributeName, variableName, illegalValues));

                    return false;
                }
            }

            return true;
        }

        private bool TryParseCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition)
        {
            if (String.IsNullOrEmpty(condition))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, elementName, attributeName));

                return false;
            }
            //TODO: Actually parse the condition to definitively tell which Variables are referenced.
            else if (condition.Trim() == "=")
            {
                this.Messaging.Write(ErrorMessages.InvalidBundleCondition(sourceLineNumbers, elementName, attributeName, condition));
                return false;
            }
            else
            {
                return true;
            }
        }

        private static string CreateValueList(ValueListKind kind, IEnumerable<string> values)
        {
            // Ideally, we could denote the list kind (and the list itself) directly in the
            // message XML, and detect and expand in the MessageHandler.GenerateMessageString()
            // method.  Doing so would make vararg-style messages much easier, but impacts
            // every single message we format.  For now, callers just have to know when a
            // message takes a list of values in a single string argument, the caller will
            // have to do the expansion themselves.  (And, unfortunately, hard-code the knowledge
            // that the list is an 'and' or 'or' list.)

            // For a localizable solution, we need to be able to get the list format string
            // from resources. We aren't currently localized right now, so the values are
            // just hard-coded.
            const string valueFormat = "'{0}'";
            const string valueSeparator = ", ";
            var terminalTerm = String.Empty;

            switch (kind)
            {
                case ValueListKind.None:
                    terminalTerm = "";
                    break;
                case ValueListKind.And:
                    terminalTerm = "and ";
                    break;
                case ValueListKind.Or:
                    terminalTerm = "or ";
                    break;
            }

            var list = new StringBuilder();

            // This weird construction helps us determine when we're adding the last value
            // to the list.  Instead of adding them as we encounter them, we cache the current
            // value and append the *previous* one.
            string previousValue = null;
            var haveValues = false;
            foreach (var value in values)
            {
                if (null != previousValue)
                {
                    if (haveValues)
                    {
                        list.Append(valueSeparator);
                    }
                    list.AppendFormat(valueFormat, previousValue);
                    haveValues = true;
                }

                previousValue = value;
            }

            // If we have no previous value, that means that the list contained no values, and
            // something has gone very wrong.
            Debug.Assert(null != previousValue);
            if (null != previousValue)
            {
                if (haveValues)
                {
                    list.Append(valueSeparator);
                    list.Append(terminalTerm);
                }
                list.AppendFormat(valueFormat, previousValue);
                //haveValues = true;
            }

            return list.ToString();
        }
    }
}
