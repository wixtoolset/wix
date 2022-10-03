// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Versioning
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// WixVersion comparer.
    /// </summary>
    public class WixVersionComparer : IEqualityComparer<WixVersion>, IComparer<WixVersion>
    {
        /// <summary>
        /// Default WixVersion comparer.
        /// </summary>
        public static readonly WixVersionComparer Default = new WixVersionComparer();

        /// <inheritdoc />
        public int Compare(WixVersion x, WixVersion y)
        {
            if ((object)x == y)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var result = x.Major.CompareTo(y.Major);
            if (result != 0)
            {
                return result;
            }

            result = x.Minor.CompareTo(y.Minor);
            if (result != 0)
            {
                return result;
            }

            result = x.Patch.CompareTo(y.Patch);
            if (result != 0)
            {
                return result;
            }

            result = x.Revision.CompareTo(y.Revision);
            if (result != 0)
            {
                return result;
            }

            var xLabelCount = x.Labels?.Length ?? 0;
            var yLabelCount = y.Labels?.Length ?? 0;
            var maxLabelCount = Math.Max(xLabelCount, yLabelCount);

            if (xLabelCount > 0)
            {
                if (yLabelCount == 0)
                {
                    return -1;
                }
            }
            else if (yLabelCount > 0)
            {
                return 1;
            }

            for (var i = 0; i < maxLabelCount; ++i)
            {
                var xLabel = i < xLabelCount ? x.Labels[i] : null;
                var yLabel = i < yLabelCount ? y.Labels[i] : null;

                result = CompareReleaseLabel(xLabel, yLabel);
                if (result != 0)
                {
                    return result;
                }
            }

            var compareMetadata = false;

            if (x.Invalid)
            {
                if (!y.Invalid)
                {
                    return -1;
                }
                else
                {
                    compareMetadata = true;
                }
            }
            else if (y.Invalid)
            {
                return 1;
            }

            if (compareMetadata)
            {
                result = String.Compare(x.Metadata, y.Metadata, StringComparison.OrdinalIgnoreCase);
            }

            return (result == 0) ? 0 : (result < 0) ? -1 : 1;
        }

        /// <inheritdoc />
        public bool Equals(WixVersion x, WixVersion y)
        {
            if ((object)x == y)
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.Major != y.Major)
            {
                return false;
            }

            if (x.Minor != y.Minor)
            {
                return false;
            }

            if (x.Patch != y.Patch)
            {
                return false;
            }

            if (x.Revision != y.Revision)
            {
                return false;
            }

            var labelCount = x.Labels?.Length ?? 0;
            if (labelCount != (y.Labels?.Length ?? 0))
            {
                return false;
            }

            for (var i = 0; i < labelCount; ++i)
            {
                var result = CompareReleaseLabel(x.Labels[i], y.Labels[i]);
                if (result != 0)
                {
                    return false;
                }
            }

            if (x.Invalid)
            {
                if (y.Invalid)
                {
                    return String.Equals(x.Metadata, y.Metadata, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return false;
                }
            }
            else if (y.Invalid)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public int GetHashCode(WixVersion version)
        {
            var hash = 23L;
            hash = hash * 37 + (version.Prefix ?? '\0');
            hash = hash * 37 + version.Major;
            hash = hash * 37 + version.Minor;
            hash = hash * 37 + version.Patch;
            hash = hash * 37 + version.Revision;
            hash = hash * 37 + (version.Invalid ? 1 : 0);
            hash = hash * 37 + (version.HasMajor ? 1 : 0);
            hash = hash * 37 + (version.HasMinor ? 1 : 0);
            hash = hash * 37 + (version.HasPatch ? 1 : 0);
            hash = hash * 37 + (version.HasRevision ? 1 : 0);

            if (version.Labels != null)
            {
                foreach (var label in version.Labels)
                {
                    hash = hash * 37 + label.Label.GetHashCode();
                }
            }

            hash = hash * 37 + version.Metadata?.GetHashCode() ?? 0;

            return unchecked((int)hash);
        }

        private static int CompareReleaseLabel(WixVersionLabel l1, WixVersionLabel l2)
        {
            if (l1 == l2)
            {
                return 0;
            }
            else if (l2 == null)
            {
                return 1;
            }
            else if (l1 == null)
            {
                return -1;
            }

            if (l1.Numeric.HasValue)
            {
                if (l2.Numeric.HasValue)
                {
                    return l1.Numeric.Value.CompareTo(l2.Numeric.Value);
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (l2.Numeric.HasValue)
                {
                    return 1;
                }
                else
                {
                    return String.Compare(l1.Label, l2.Label, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
