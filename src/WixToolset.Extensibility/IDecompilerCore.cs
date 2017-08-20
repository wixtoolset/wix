// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;
    using Wix = WixToolset.Data.Serialize;

    public interface IDecompilerCore : IMessageHandler
    {

        /// <summary>
        /// Gets whether the decompiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets the root element of the decompiled output.
        /// </summary>
        /// <value>The root element of the decompiled output.</value>
        Wix.IParentElement RootElement { get; }

        /// <summary>
        /// Gets the UI element.
        /// </summary>
        /// <value>The UI element.</value>
        Wix.UI UIElement { get; }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        bool IsValidShortFilename(string filename, bool allowWildcards);

        /// <summary>
        /// Convert an Int32 into a DateTime.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        /// <returns>The DateTime.</returns>
        DateTime ConvertIntegerToDateTime(int value);

        /// <summary>
        /// Gets the element corresponding to the row it came from.
        /// </summary>
        /// <param name="row">The row corresponding to the element.</param>
        /// <returns>The indexed element.</returns>
        Wix.ISchemaElement GetIndexedElement(Row row);

        /// <summary>
        /// Gets the element corresponding to the primary key of the given table.
        /// </summary>
        /// <param name="table">The table corresponding to the element.</param>
        /// <param name="primaryKey">The primary key corresponding to the element.</param>
        /// <returns>The indexed element.</returns>
        Wix.ISchemaElement GetIndexedElement(string table, params string[] primaryKey);

        /// <summary>
        /// Index an element by its corresponding row.
        /// </summary>
        /// <param name="row">The row corresponding to the element.</param>
        /// <param name="element">The element to index.</param>
        void IndexElement(Row row, Wix.ISchemaElement element);

            /// <summary>
        /// Indicates the decompiler encountered and unexpected table to decompile.
        /// </summary>
        /// <param name="table">Unknown decompiled table.</param>
        void UnexpectedTable(Table table);
}
}
