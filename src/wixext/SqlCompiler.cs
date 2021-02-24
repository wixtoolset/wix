// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Sql.Symbols;

    /// <summary>
    /// The compiler for the WiX Toolset SQL Server Extension.
    /// </summary>
    public sealed class SqlCompiler : BaseCompilerExtension
    {
        // sql database attributes definitions (from sca.h)
        internal const int DbCreateOnInstall = 0x00000001;
        internal const int DbDropOnUninstall = 0x00000002;
        internal const int DbContinueOnError = 0x00000004;
        internal const int DbDropOnInstall = 0x00000008;
        internal const int DbCreateOnUninstall = 0x00000010;
        internal const int DbConfirmOverwrite = 0x00000020;
        internal const int DbCreateOnReinstall = 0x00000040;
        internal const int DbDropOnReinstall = 0x00000080;

        // sql string/script attributes definitions (from sca.h)
        internal const int SqlExecuteOnInstall = 0x00000001;
        internal const int SqlExecuteOnUninstall = 0x00000002;
        internal const int SqlContinueOnError = 0x00000004;
        internal const int SqlRollback = 0x00000008;
        internal const int SqlExecuteOnReinstall = 0x00000010;

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/sql";

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="section"></param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    var componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "SqlDatabase":
                            this.ParseSqlDatabaseElement(intermediate, section, element, componentId);
                            break;
                        case "SqlScript":
                            this.ParseSqlScriptElement(intermediate, section, element, componentId, null);
                            break;
                        case "SqlString":
                            this.ParseSqlStringElement(intermediate, section, element, componentId, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Package":
                    switch (element.Name.LocalName)
                    {
                        case "SqlDatabase":
                            this.ParseSqlDatabaseElement(intermediate, section, element, null);
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

        /// <summary>
        /// Parses a sql database element
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="section"></param>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseSqlDatabaseElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            string database = null;
            Identifier fileSpec = null;
            string instance = null;
            Identifier logFileSpec = null;
            string server = null;
            string user = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ConfirmOverwrite":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbConfirmOverwrite;
                            }
                            break;
                        case "ContinueOnError":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbContinueOnError;
                            }
                            break;
                        case "CreateOnInstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnInstall;
                            }
                            break;
                        case "CreateOnReinstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnReinstall;
                            }
                            break;
                        case "CreateOnUninstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbCreateOnUninstall;
                            }
                            break;
                        case "Database":
                            database = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DropOnInstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnInstall;
                            }
                            break;
                        case "DropOnReinstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnReinstall;
                            }
                            break;

                        case "DropOnUninstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DbDropOnUninstall;
                            }
                            break;
                        case "Instance":
                            instance = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Server":
                            server = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!this.ParseHelper.ContainsProperty(user))
                            {
                                user = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("sdb", componentId, server, database);
            }

            if (null == database)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Database"));
            }
            else if (128 < database.Length)
            {
                this.Messaging.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, element.Name.LocalName, "Database", database, 128));
            }

            if (null == server)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Server"));
            }

            if (0 == attributes && null != componentId)
            {
                this.Messaging.Write(SqlErrors.OneOfAttributesRequiredUnderComponent(sourceLineNumbers, element.Name.LocalName, "CreateOnInstall", "CreateOnUninstall", "DropOnInstall", "DropOnUninstall"));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "SqlScript":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseSqlScriptElement(intermediate, section, child, componentId, id?.Id);
                            break;
                        case "SqlString":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseSqlStringElement(intermediate, section, child, componentId, id?.Id);
                            break;
                        case "SqlFileSpec":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }
                            else if (null != fileSpec)
                            {
                                this.Messaging.Write(ErrorMessages.TooManyElements(sourceLineNumbers, element.Name.LocalName, child.Name.LocalName, 1));
                            }

                            fileSpec = this.ParseSqlFileSpecElement(intermediate, section, child, id?.Id);
                            break;
                        case "SqlLogFileSpec":
                            if (null == componentId)
                            {
                                this.Messaging.Write(SqlErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }
                            else if (null != logFileSpec)
                            {
                                this.Messaging.Write(ErrorMessages.TooManyElements(sourceLineNumbers, element.Name.LocalName, child.Name.LocalName, 1));
                            }

                            logFileSpec = this.ParseSqlFileSpecElement(intermediate, section, child, id?.Id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (null != componentId)
            {
                // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
                this.AddReferenceToInstallSqlData(section, sourceLineNumbers);
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new SqlDatabaseSymbol(sourceLineNumbers, id)
                {
                    Server = server,
                    Instance = instance,
                    Database = database,
                    ComponentRef = componentId,
                    UserRef = user,
                    FileSpecRef = fileSpec?.Id,
                    LogFileSpecRef = logFileSpec?.Id,
                });

                if (0 != attributes)
                {
                    symbol.Attributes = attributes;
                }
            }
        }

        /// <summary>
        /// Parses a sql file specification element.
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="section"></param>
        /// <param name="element">Element to parse.</param>
        /// <returns>Identifier of sql file specification.</returns>
        private Identifier ParseSqlFileSpecElement(Intermediate intermediate, IntermediateSection section, XElement element, string parentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string fileName = null;
            string growthSize = null;
            string maxSize = null;
            string name = null;
            string size = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Filename":
                            fileName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Size":
                            size = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxSize":
                            maxSize = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "GrowthSize":
                            growthSize = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("sfs", parentId, name, fileName);
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == fileName)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Filename"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new SqlFileSpecSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    Filename = fileName,
                });

                if (null != size)
                {
                    symbol.Size = size;
                }

                if (null != maxSize)
                {
                    symbol.MaxSize = maxSize;
                }

                if (null != growthSize)
                {
                    symbol.GrowthSize = growthSize;
                }
            }

            return id;
        }

        /// <summary>
        /// Parses a sql script element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="sqlDb">Optional database to execute script against.</param>
        private void ParseSqlScriptElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string sqlDb)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            var rollbackAttribute = false;
            var nonRollbackAttribute = false;
            string binaryRef = null;
            var sequence = CompilerConstants.IntegerNotSet;
            string user = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinaryRef":
                            binaryRef = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Binary, binaryRef);
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "SqlDb":
                            if (null != sqlDb)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, element.Parent.Name.LocalName));
                            }
                            sqlDb = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SqlSymbolDefinitions.SqlDatabase, sqlDb);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
                            break;

                        // Flag-setting attributes
                        case "ContinueOnError":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlContinueOnError;
                            }
                            break;
                        case "ExecuteOnInstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnInstall;
                            }
                            break;
                        case "ExecuteOnReinstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnReinstall;
                            }
                            break;
                        case "ExecuteOnUninstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnUninstall;
                            }
                            break;
                        case "RollbackOnInstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnInstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        case "RollbackOnReinstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnReinstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        case "RollbackOnUninstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnUninstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ssc", componentId, binaryRef, sqlDb);
            }

            if (null == binaryRef)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "BinaryRef"));
            }

            if (null == sqlDb)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "SqlDb"));
            }

            if (0 == attributes)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall", "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
            this.AddReferenceToInstallSqlData(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new SqlScriptSymbol(sourceLineNumbers, id)
                {
                    SqlDbRef = sqlDb,
                    ComponentRef = componentId,
                    ScriptBinaryRef = binaryRef,
                    UserRef = user,
                    Attributes = attributes,
                });

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
            }
        }

        /// <summary>
        /// Parses a sql string element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="sqlDb">Optional database to execute string against.</param>
        private void ParseSqlStringElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string sqlDb)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            var rollbackAttribute = false;
            var nonRollbackAttribute = false;
            var sequence = CompilerConstants.IntegerNotSet;
            string sql = null;
            string user = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ContinueOnError":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlContinueOnError;
                            }
                            break;
                        case "ExecuteOnInstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnInstall;
                            }
                            break;
                        case "ExecuteOnReinstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnReinstall;
                            }
                            break;
                        case "ExecuteOnUninstall":
                            if (rollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
                            }
                            nonRollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnUninstall;
                            }
                            break;
                        case "RollbackOnInstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnInstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        case "RollbackOnReinstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnReinstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        case "RollbackOnUninstall":
                            if (nonRollbackAttribute)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall"));
                            }
                            rollbackAttribute = true;

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= SqlExecuteOnUninstall;
                                attributes |= SqlRollback;
                            }
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "SQL":
                            sql = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SqlDb":
                            if (null != sqlDb)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "SqlDb", "SqlDatabase"));
                            }

                            sqlDb = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SqlSymbolDefinitions.SqlDatabase, sqlDb);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("sst", componentId, sql, sqlDb);
            }

            if (null == sql)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "SQL"));
            }

            if (null == sqlDb)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "SqlDb"));
            }

            if (0 == attributes)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "ExecuteOnInstall", "ExecuteOnReinstall", "ExecuteOnUninstall", "RollbackOnInstall", "RollbackOnReinstall", "RollbackOnUninstall"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference InstallSqlData and UninstallSqlData since nothing will happen without it
            this.AddReferenceToInstallSqlData(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new SqlStringSymbol(sourceLineNumbers, id)
                {
                    SqlDbRef = sqlDb,
                    ComponentRef = componentId,
                    SQL = sql,
                    UserRef = user,
                    Attributes = attributes,
                });

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
            }
        }

        private void AddReferenceToInstallSqlData(IntermediateSection section, SourceLineNumber sourceLineNumbers)
        {
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4InstallSqlData", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4UninstallSqlData", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }
    }
}
