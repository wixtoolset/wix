// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using Microsoft.Build.Framework;

    internal class MetadataValue
    {
        public MetadataValue(ITaskItem item, string name, string valuePrefix = null, string defaultValue = "")
        {
            this.Item = item;
            this.Name = name;

            var value = item.GetMetadata(name);

            this.HadValue = !String.IsNullOrWhiteSpace(value);
            this.OriginalValue = value;

            if (!this.HadValue)
            {
                this.Value = defaultValue;
                this.ValidValue = true;
            }
            else if (String.IsNullOrWhiteSpace(valuePrefix))
            {
                this.Value = value;
                this.ValidValue = true;
            }
            else if (value.StartsWith(valuePrefix) && value.Length > valuePrefix.Length)
            {
                this.Value = value.Substring(valuePrefix.Length);
                this.ValidValue = true;
            }
        }

        public ITaskItem Item { get; }

        public string Name { get; }

        public bool HadValue { get; }

        public bool Modified => !String.Equals(this.OriginalValue, this.Value, StringComparison.Ordinal);

        public string OriginalValue { get; }

        public string Value { get; private set; }

        public bool ValidValue { get; }

        public void SetValue(string value)
        {
            this.Value = value;
        }

        public void Apply()
        {
            if (String.IsNullOrWhiteSpace(this.Value))
            {
                this.Item.RemoveMetadata(this.Name);
            }
            else
            {
                this.Item.SetMetadata(this.Name, this.Value);
            }
        }
    }
}
