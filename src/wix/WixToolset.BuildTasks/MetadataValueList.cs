// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;

    internal class MetadataValueList
    {
        private static readonly char[] MetadataListSplitter = new char[] { ',', ';' };

        public MetadataValueList(ITaskItem item, string name)
        {
            this.Item = item;
            this.Name = name;

            var value = item.GetMetadata(name);

            this.HadValue = !String.IsNullOrWhiteSpace(value);
            this.OriginalValue = value;

            this.Values = value.Split(MetadataListSplitter).Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
        }

        public ITaskItem Item { get; }

        public string Name { get; }

        public bool HadValue { get; }

        public string OriginalValue { get; }

        public bool Modified { get; private set; }

        public List<string> Values { get; }

        public void Clear()
        {
            if (this.Values.Count > 0)
            {
                this.Modified = true;
                this.Values.Clear();
            }
        }

        public void SetValue(string prefix, string value)
        {
            if (!String.IsNullOrEmpty(prefix))
            {
                value = String.IsNullOrWhiteSpace(value) ? null : prefix + value;

                for (var i = 0; i < this.Values.Count; ++i)
                {
                    if (this.Values[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (value == null)
                        {
                            this.Values.RemoveAt(i);
                        }
                        else
                        {
                            this.Values[i] = value;
                        }

                        this.Modified = true;
                        return;
                    }
                }
            }

            if (!String.IsNullOrWhiteSpace(value) && !this.Values.Contains(value))
            {
                this.Modified = true;
                this.Values.Add(value);
            }
        }

        public void AddRange(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                this.SetValue(null, value);
            }
        }

        public void Apply()
        {
            if (this.Values.Count == 0)
            {
                this.Item.RemoveMetadata(this.Name);
            }
            else
            {
                this.Item.SetMetadata(this.Name, String.Join(";", this.Values));
            }
        }
    }
}
