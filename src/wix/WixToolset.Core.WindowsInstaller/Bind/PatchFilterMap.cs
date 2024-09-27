// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data.WindowsInstaller;

    internal class PatchFilterMap
    {
        private readonly Dictionary<Row, PatchFilter> filterMap = new Dictionary<Row, PatchFilter>();

        public void AddTargetRowFilterIds(IEnumerable<KeyValuePair<Row, string>> rowFilterIds)
        {
            foreach (var kvp in rowFilterIds)
            {
                this.filterMap.Add(kvp.Key, new PatchFilter(kvp.Key, kvp.Value, null));
            }
        }

        public void AddUpdatedRowFilterIds(IEnumerable<KeyValuePair<Row, string>> rowFilterIds)
        {
            foreach (var kvp in rowFilterIds)
            {
                this.filterMap.Add(kvp.Key, new PatchFilter(kvp.Key, null, kvp.Value));
            }
        }

        public void AddTargetRowFilterToUpdatedRowFilter(Row targetRow, Row updatedRow)
        {
            if (this.filterMap.TryGetValue(targetRow, out var targetPatchFilter) && !String.IsNullOrEmpty(targetPatchFilter.TargetFilterId))
            {
                // If the updated row didn't have a patch filter, it gets one now because the target patch has
                // a target filter id to add.
                if (!this.filterMap.TryGetValue(updatedRow, out var updatedPatchFilter))
                {
                    updatedPatchFilter = new PatchFilter(updatedRow, null, null);
                    this.filterMap.Add(updatedRow, updatedPatchFilter);
                }

                updatedPatchFilter.SetTargetFilterId(targetPatchFilter);
            }
        }

        internal bool ContainsPatchFilterForRow(Row row)
        {
            return this.filterMap.ContainsKey(row);
        }

        internal bool TryGetPatchFiltersForRow(Row row, out string targetFilterId, out string updatedFilterId)
        {
            this.filterMap.TryGetValue(row, out var patchFilter);

            targetFilterId = patchFilter?.TargetFilterId;
            updatedFilterId = patchFilter?.UpdatedFilterId;

            return patchFilter != null;
        }

        private class PatchFilter
        {
            public PatchFilter(Row row, string targetFilterId, string updatedFilterId)
            {
                this.Row = row;
                this.TargetFilterId = targetFilterId;
                this.UpdatedFilterId = updatedFilterId;
            }

            public Row Row { get; }

            public string TargetFilterId { get; private set; }

            public string UpdatedFilterId { get; }

            public void SetTargetFilterId(PatchFilter targetPatchFilter)
            {
                this.TargetFilterId = targetPatchFilter.TargetFilterId;
            }
        }
    }
}
