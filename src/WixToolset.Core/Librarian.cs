// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Link;

    /// <summary>
    /// Core librarian tool.
    /// </summary>
    public sealed class Librarian
    {
        /// <summary>
        /// Instantiate a new Librarian class.
        /// </summary>
        public Librarian()
        {
            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerStandard.GetTableDefinitions());
        }

        /// <summary>
        /// Gets table definitions used by this librarian.
        /// </summary>
        /// <value>Table definitions.</value>
        public TableDefinitionCollection TableDefinitions { get; private set; }

        /// <summary>
        /// Adds an extension's data.
        /// </summary>
        /// <param name="extension">The extension data to add.</param>
        public void AddExtensionData(IExtensionData extension)
        {
            if (null != extension.TableDefinitions)
            {
                foreach (TableDefinition tableDefinition in extension.TableDefinitions)
                {
                    try
                    {
                        this.TableDefinitions.Add(tableDefinition);
                    }
                    catch (ArgumentException)
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicateExtensionTable(extension.GetType().ToString(), tableDefinition.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Create a library by combining several intermediates (objects).
        /// </summary>
        /// <param name="sections">The sections to combine into a library.</param>
        /// <returns>Returns the new library.</returns>
        public Library Combine(IEnumerable<Section> sections)
        {
            Library library = new Library(sections);

            this.Validate(library);

            return (Messaging.Instance.EncounteredError ? null : library);
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }

        /// <summary>
        /// Validate that a library contains one entry section and no duplicate symbols.
        /// </summary>
        /// <param name="library">Library to validate.</param>
        private void Validate(Library library)
        {
            FindEntrySectionAndLoadSymbolsCommand find = new FindEntrySectionAndLoadSymbolsCommand(library.Sections);
            find.Execute();

            // TODO: Consider bringing this sort of verification back.
            // foreach (Section section in library.Sections)
            // {
            //     ResolveReferencesCommand resolve = new ResolveReferencesCommand(find.EntrySection, find.Symbols);
            //     resolve.Execute();
            //
            //     ReportDuplicateResolvedSymbolErrorsCommand reportDupes = new ReportDuplicateResolvedSymbolErrorsCommand(find.SymbolsWithDuplicates, resolve.ResolvedSections);
            //     reportDupes.Execute();
            // }
        }
    }
}
