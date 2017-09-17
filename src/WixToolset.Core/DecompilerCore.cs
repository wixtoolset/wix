// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The base of the decompiler. Holds some variables used by the decompiler and extensions,
    /// as well as some utility methods.
    /// </summary>
    internal class DecompilerCore : IDecompilerCore
    {
        private Hashtable elements;
        private Wix.IParentElement rootElement;
        private bool showPedanticMessages;
        private Wix.UI uiElement;

        /// <summary>
        /// Instantiate a new decompiler core.
        /// </summary>
        /// <param name="rootElement">The root element of the decompiled database.</param>
        /// <param name="messageHandler">The message handler.</param>
        internal DecompilerCore(Wix.IParentElement rootElement)
        {
            this.elements = new Hashtable();
            this.rootElement = rootElement;
        }

        /// <summary>
        /// Gets whether the decompiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Gets the root element of the decompiled output.
        /// </summary>
        /// <value>The root element of the decompiled output.</value>
        public Wix.IParentElement RootElement
        {
            get { return this.rootElement; }
        }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages
        {
            get { return this.showPedanticMessages; }
            set { this.showPedanticMessages = value; }
        }

        /// <summary>
        /// Gets the UI element.
        /// </summary>
        /// <value>The UI element.</value>
        public Wix.UI UIElement
        {
            get
            {
                if (null == this.uiElement)
                {
                    this.uiElement = new Wix.UI();
                    this.rootElement.AddChild(this.uiElement);
                }

                return this.uiElement;
            }
        }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        public virtual bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return false;
        }

        /// <summary>
        /// Convert an Int32 into a DateTime.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        /// <returns>The DateTime.</returns>
        public DateTime ConvertIntegerToDateTime(int value)
        {
            int date = value / 65536;
            int time = value % 65536;

            return new DateTime(1980 + (date / 512), (date % 512) / 32, date % 32, time / 2048, (time % 2048) / 32, (time % 32) * 2);
        }

        /// <summary>
        /// Gets the element corresponding to the row it came from.
        /// </summary>
        /// <param name="row">The row corresponding to the element.</param>
        /// <returns>The indexed element.</returns>
        public Wix.ISchemaElement GetIndexedElement(Row row)
        {
            return this.GetIndexedElement(row.TableDefinition.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter));
        }

        /// <summary>
        /// Gets the element corresponding to the primary key of the given table.
        /// </summary>
        /// <param name="table">The table corresponding to the element.</param>
        /// <param name="primaryKey">The primary key corresponding to the element.</param>
        /// <returns>The indexed element.</returns>
        public Wix.ISchemaElement GetIndexedElement(string table, params string[] primaryKey)
        {
            return (Wix.ISchemaElement)this.elements[String.Concat(table, ':', String.Join(DecompilerConstants.PrimaryKeyDelimiterString, primaryKey))];
        }

        /// <summary>
        /// Index an element by its corresponding row.
        /// </summary>
        /// <param name="row">The row corresponding to the element.</param>
        /// <param name="element">The element to index.</param>
        public void IndexElement(Row row, Wix.ISchemaElement element)
        {
            this.elements.Add(String.Concat(row.TableDefinition.Name, ':', row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter)), element);
        }

        /// <summary>
        /// Indicates the decompiler encountered and unexpected table to decompile.
        /// </summary>
        /// <param name="table">Unknown decompiled table.</param>
        public void UnexpectedTable(Table table)
        {
            this.OnMessage(WixErrors.TableDecompilationUnimplemented(table.Name));
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
