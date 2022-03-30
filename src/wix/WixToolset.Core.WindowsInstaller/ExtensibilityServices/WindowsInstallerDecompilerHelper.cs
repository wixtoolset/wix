// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Core.WindowsInstaller.Decompile;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class WindowsInstallerDecompilerHelper : IWindowsInstallerDecompilerHelper
    {
        public WindowsInstallerDecompilerHelper(IServiceProvider _)
        {
        }

        private Dictionary<string, XElement> IndexedElements { get; } = new Dictionary<string, XElement>();

        #region IWindowsInstallerDecompilerHelper interfaces

        public XElement RootElement { get; set; }

        public XElement AddElementToRoot(string name, params object[] content)
        {
            var element = new XElement(Names.WxsNamespace + name, content);

            this.RootElement.Add(element);

            return element;
        }

        public XElement AddElementToRoot(XName name, params object[] content)
        {
            var element = new XElement(name, content);

            this.RootElement.Add(element);

            return element;
        }

        public XElement AddElementToRoot(XElement element)
        {
            this.RootElement.Add(element);

            return element;
        }

        public XElement CreateElement(string name, params object[] content)
        {
            return new XElement(Names.WxsNamespace + name, content);
        }

        public XElement GetIndexedElement(Row row)
        {
            return this.GetIndexedElement(row.TableDefinition.Name, row.GetPrimaryKey());
        }

        public XElement GetIndexedElement(string table, string primaryKey)
        {
            return this.TryGetIndexedElement(table, primaryKey, out var element) ? element : null;
        }

        public XElement GetIndexedElement(string table, string primaryKey1, string primaryKey2)
        {
            return this.TryGetIndexedElement(table, primaryKey1, primaryKey2, out var element) ? element : null;
        }

        public XElement GetIndexedElement(string table, string primaryKey1, string primaryKey2, string primaryKey3)
        {
            return this.TryGetIndexedElement(table, primaryKey1, primaryKey2, primaryKey3, out var element) ? element : null;
        }

        public XElement GetIndexedElement(string table, string[] primaryKeys)
        {
            return this.TryGetIndexedElement(table, primaryKeys, out var element) ? element : null;
        }

        public void IndexElement(Row row, XElement element)
        {
            this.IndexElement(row.Table.Name, row.GetPrimaryKey(), element);
        }

        public void IndexElement(string table, string primaryKey, XElement element)
        {
            var key = String.Concat(table, ':', primaryKey);
            this.IndexedElements.Add(key, element);
        }

        public void IndexElement(string table, string primaryKey1, string primaryKey2, XElement element)
        {
            this.IndexElement(table, String.Join("/", primaryKey1, primaryKey2), element);
        }

        public void IndexElement(string table, string primaryKey1, string primaryKey2, string primaryKey3, XElement element)
        {
            this.IndexElement(table, String.Join("/", primaryKey1, primaryKey2, primaryKey3), element);
        }

        public void IndexElement(string table, string[] primaryKeys, XElement element)
        {
            this.IndexElement(table, String.Join("/", primaryKeys), element);
        }

        public bool TryGetIndexedElement(Row row, out XElement element)
        {
            return this.TryGetIndexedElement(row.TableDefinition.Name, row.GetPrimaryKey(), out element);
        }

        public bool TryGetIndexedElement(string table, string primaryKey, out XElement element)
        {
            var key = String.Concat(table, ':', primaryKey);
            return this.IndexedElements.TryGetValue(key, out element);
        }

        public bool TryGetIndexedElement(string table, string primaryKey1, string primaryKey2, out XElement element)
        {
            return this.TryGetIndexedElement(table, String.Join("/", primaryKey1, primaryKey2), out element);
        }

        public bool TryGetIndexedElement(string table, string primaryKey1, string primaryKey2, string primaryKey3, out XElement element)
        {
            return this.TryGetIndexedElement(table, String.Join("/", primaryKey1, primaryKey2, primaryKey3), out element);
        }

        public bool TryGetIndexedElement(string table, string[] primaryKeys, out XElement element)
        {
            return this.TryGetIndexedElement(table, String.Join("/", primaryKeys), out element);
        }

        #endregion
    }
}
