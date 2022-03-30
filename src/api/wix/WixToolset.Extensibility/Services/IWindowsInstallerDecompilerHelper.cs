// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Xml.Linq;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Interface provided to help Windows Installer decompiler extensions.
    /// </summary>
    public interface IWindowsInstallerDecompilerHelper
    {
        /// <summary>
        /// Gets or sets the root element of the decompiled output.
        /// </summary>
        XElement RootElement { get; set; }

        /// <summary>
        /// Creates an element from the standard WiX Toolset namespace and adds it to the root document.
        /// </summary>
        /// <param name="name">Name of the element to create and add.</param>
        /// <param name="content">Optional content to add to the new element.</param>
        /// <returns>Element in the standard namespace.</returns>
        XElement AddElementToRoot(string name, params object[] content);

        /// <summary>
        /// Creates an element with the specified name and adds it to the root document.
        /// </summary>
        /// <param name="name">Name of the element to create and add.</param>
        /// <param name="content">Optional content to add to the new element.</param>
        /// <returns>Element in the standard namespace.</returns>
        XElement AddElementToRoot(XName name, params object[] content);

        /// <summary>
        /// Adds an existing element to the root document.
        /// </summary>
        /// <param name="element">Element to add.</param>
        /// <returns>Same element provided.</returns>
        XElement AddElementToRoot(XElement element);

        /// <summary>
        /// Creates an element from the standard WiX Toolset namespace.
        /// </summary>
        /// <param name="name">Name of the element to create.</param>
        /// <param name="content">Optional content to add to the new element.</param>
        /// <returns>Element in the standard namespace.</returns>
        XElement CreateElement(string name, params object[] content);

        /// <summary>
        /// Get an element index by a row's table and primary keys.
        /// </summary>
        /// <param name="row">Row to get element.</param>
        /// <returns>Element indexed for the row or null if not found.</returns>
        XElement GetIndexedElement(Row row);

        /// <summary>
        /// Get an element index by table and primary key.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey">Primary key for indexed element.</param>
        /// <returns>Element indexed for the table and primary key or null if not found.</returns>
        XElement GetIndexedElement(string table, string primaryKey);

        /// <summary>
        /// Get an element index by table and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey1">Primary key for first column indexed element.</param>
        /// <param name="primaryKey2">Primary key for second column indexed element.</param>
        /// <returns>Element indexed for the table and primary keys or null if not found.</returns>
        XElement GetIndexedElement(string table, string primaryKey1, string primaryKey2);

        /// <summary>
        /// Get an element index by table and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey1">Primary key for first column indexed element.</param>
        /// <param name="primaryKey2">Primary key for second column indexed element.</param>
        /// <param name="primaryKey3">Primary key for third column indexed element.</param>
        /// <returns>Element indexed for the table and primary keys or null if not found.</returns>
        XElement GetIndexedElement(string table, string primaryKey1, string primaryKey2, string primaryKey3);

        /// <summary>
        /// Get an element index by table and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKeys">Primary keys for indexed element.</param>
        /// <returns>Element indexed for the table and primary keys or null if not found.</returns>
        XElement GetIndexedElement(string table, string[] primaryKeys);

        /// <summary>
        /// Try to get an element index by a row's table and primary keys.
        /// </summary>
        /// <param name="row">Row to get element.</param>
        /// <param name="element">Element indexed for the row.</param>
        /// <returns>True if the element was index otherwise false.</returns>
        bool TryGetIndexedElement(Row row, out XElement element);

        /// <summary>
        /// Try to get an element index by table name and primary key.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey">Primary key for indexed element.</param>
        /// <param name="element">Element indexed for the table and primary key.</param>
        /// <returns>True if the element was index otherwise false.</returns>
        bool TryGetIndexedElement(string table, string primaryKey, out XElement element);

        /// <summary>
        /// Try to get an element index by table name and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey1">First column's primary key for indexed element.</param>
        /// <param name="primaryKey2">Second column's primary key for indexed element.</param>
        /// <param name="element">Element indexed for the table and primary key.</param>
        /// <returns>True if the element was index otherwise false.</returns>
        bool TryGetIndexedElement(string table, string primaryKey1, string primaryKey2, out XElement element);

        /// <summary>
        /// Try to get an element index by table name and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKey1">First column's primary key for indexed element.</param>
        /// <param name="primaryKey2">Second column's primary key for indexed element.</param>
        /// <param name="primaryKey3">Third column's primary key for indexed element.</param>
        /// <param name="element">Element indexed for the table and primary key.</param>
        /// <returns>True if the element was index otherwise false.</returns>
        bool TryGetIndexedElement(string table, string primaryKey1, string primaryKey2, string primaryKey3, out XElement element);

        /// <summary>
        /// Try to get an element index by table name and primary keys.
        /// </summary>
        /// <param name="table">Table name for indexed element.</param>
        /// <param name="primaryKeys">Primary keys for indexed element.</param>
        /// <param name="element">Element indexed for the table and primary key.</param>
        /// <returns>True if the element was index otherwise false.</returns>
        bool TryGetIndexedElement(string table, string[] primaryKeys, out XElement element);

        /// <summary>
        /// Index an element by a row's table and primary keys.
        /// </summary>
        /// <param name="row">Row to index element.</param>
        /// <param name="element">Element to index.</param>
        void IndexElement(Row row, XElement element);

        /// <summary>
        /// Index an element by table and primary key.
        /// </summary>
        /// <param name="table">Table name to index element.</param>
        /// <param name="primaryKey">Primary key to index element.</param>
        /// <param name="element">Element to index.</param>
        void IndexElement(string table, string primaryKey, XElement element);

        /// <summary>
        /// Index an element by table and primary keys.
        /// </summary>
        /// <param name="table">Table name to index element.</param>
        /// <param name="primaryKey1">First column's primary key to index element.</param>
        /// <param name="primaryKey2">Second column's primary key to index element.</param>
        /// <param name="element">Element to index.</param>
        void IndexElement(string table, string primaryKey1, string primaryKey2, XElement element);

        /// <summary>
        /// Index an element by table and primary keys.
        /// </summary>
        /// <param name="table">Table name to index element.</param>
        /// <param name="primaryKey1">First column's primary key to index element.</param>
        /// <param name="primaryKey2">Second column's primary key to index element.</param>
        /// <param name="primaryKey3">Third column's primary key to index element.</param>
        /// <param name="element">Element to index.</param>
        void IndexElement(string table, string primaryKey1, string primaryKey2, string primaryKey3, XElement element);

        /// <summary>
        /// Index an element by table and primary keys.
        /// </summary>
        /// <param name="table">Table name to index element.</param>
        /// <param name="primaryKeys">Column's primary key to index element.</param>
        /// <param name="element">Element to index.</param>
        void IndexElement(string table, string[] primaryKeys, XElement element);
    }
}
