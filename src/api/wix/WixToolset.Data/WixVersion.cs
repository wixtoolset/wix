// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// WiX Toolset's representation of a version designed to support
    /// a different version types including 4-part versions with very
    /// large numbers and semantic versions with 4-part versions with
    /// or without leading "v" indicators.
    /// </summary>
    [CLSCompliant(false)]
    public class WixVersion
    {
        /// <summary>
        /// Gets the prefix of the version if present when parsed. Usually, 'v' or 'V'.
        /// </summary>
        public char? Prefix { get; set; } 

        /// <summary>
        /// Gets or sets the major version.
        /// </summary>
        public uint Major { get; set; }

        /// <summary>
        /// Gets or sets the minor version.
        /// </summary>
        public uint Minor { get; set; }

        /// <summary>
        /// Gets or sets the patch version.
        /// </summary>
        public uint Patch { get; set; }

        /// <summary>
        /// Gets or sets the revision version.
        /// </summary>
        public uint Revision { get; set; }

        /// <summary>
        /// Gets or sets whether the major version was defined.
        /// </summary>
        public bool HasMajor { get; set; }

        /// <summary>
        /// Gets or sets the whether the minor version was defined.
        /// </summary>
        public bool HasMinor { get; set; }

        /// <summary>
        /// Gets or sets the whether the patch version was defined.
        /// </summary>
        public bool HasPatch { get; set; }

        /// <summary>
        /// Gets or sets the whether the revision version was defined.
        /// </summary>
        public bool HasRevision { get; set; }

        /// <summary>
        /// Gets or sets the labels in the version.
        /// </summary>
        public WixVersionLabel[] Labels { get; set; }

        /// <summary>
        /// Gets or sets the metadata in the version.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Tries to parse a string value into a valid <c>WixVersion</c>.
        /// </summary>
        /// <param name="parse">String value to parse into a version.</param>
        /// <param name="version">Parsed version.</param>
        /// <returns>True if the version was successfully parsed, or false otherwise.</returns>
        public static bool TryParse(string parse, out WixVersion version)
        {
            version = new WixVersion();

            var labels = new List<WixVersionLabel>();
            var start = 0;
            var end = parse.Length;

            if (end > 0 && (parse[0] == 'v' || parse[0] == 'V'))
            {
                version.Prefix = parse[0];

                ++start;
            }

            var partBegin = start;
            var partEnd = start;
            var lastPart = false;
            var trailingDot = false;
            var invalid = false;
            var currentPart = 0;
            var parsedVersionNumber = false;
            var expectedReleaseLabels = false;

            // Parse version number
            while (start < end)
            {
                trailingDot = false;

                // Find end of part.
                for (; ; )
                {
                    if (partEnd >= end)
                    {
                        lastPart = true;
                        break;
                    }

                    var ch = parse[partEnd];

                    switch (ch)
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ++partEnd;
                            continue;
                        case '.':
                            trailingDot = true;
                            break;
                        case '-':
                        case '+':
                            lastPart = true;
                            break;
                        default:
                            invalid = true;
                            break;
                    }

                    break;
                }

                var partLength = partEnd - partBegin;
                if (invalid || partLength <= 0)
                {
                    invalid = true;
                    break;
                }

                // Parse version part.
                var s = parse.Substring(partBegin, partLength);
                if (!UInt32.TryParse(s, out var part))
                {
                    invalid = true;
                    break;
                }

                switch (currentPart)
                {
                    case 0:
                        version.Major = part;
                        version.HasMajor = true;
                        break;
                    case 1:
                        version.Minor = part;
                        version.HasMinor = true;
                        break;
                    case 2:
                        version.Patch = part;
                        version.HasPatch = true;
                        break;
                    case 3:
                        version.Revision = part;
                        version.HasRevision = true;
                        break;
                }

                if (trailingDot)
                {
                    ++partEnd;
                }
                partBegin = partEnd;
                ++currentPart;

                if (4 <= currentPart || lastPart)
                {
                    parsedVersionNumber = true;
                    break;
                }
            }

            invalid |= !parsedVersionNumber || trailingDot;

            if (!invalid && partBegin < end && parse[partBegin] == '-')
            {
                partBegin = partEnd = partBegin + 1;
                expectedReleaseLabels = true;
                lastPart = false;
            }

            while (expectedReleaseLabels && partBegin < end)
            {
                trailingDot = false;

                // Find end of part.
                for (; ; )
                {
                    if (partEnd >= end)
                    {
                        lastPart = true;
                        break;
                    }

                    var ch = parse[partEnd];
                    if (ch >= '0' && ch <= '9' ||
                        ch >= 'A' && ch <= 'Z' ||
                        ch >= 'a' && ch <= 'z' ||
                        ch == '-')
                    {
                        ++partEnd;
                        continue;
                    }
                    else if (ch == '+')
                    {
                        lastPart = true;
                    }
                    else if (ch == '.')
                    {
                        trailingDot = true;
                    }
                    else
                    {
                        invalid = true;
                    }

                    break;
                }

                var partLength = partEnd - partBegin;
                if (invalid || partLength <= 0)
                {
                    invalid = true;
                    break;
                }

                WixVersionLabel label;
                var partString = parse.Substring(partBegin, partLength);
                if (UInt32.TryParse(partString, out var numericPart))
                {
                    label = new WixVersionLabel(partString, numericPart);
                }
                else
                {
                    label = new WixVersionLabel(partString);
                }

                labels.Add(label);

                if (trailingDot)
                {
                    ++partEnd;
                }
                partBegin = partEnd;

                if (lastPart)
                {
                    break;
                }
            }

            invalid |= expectedReleaseLabels && (labels.Count == 0 || trailingDot);

            if (!invalid && partBegin < end)
            {
                if (parse[partBegin] == '+')
                {
                    version.Metadata = parse.Substring(partBegin + 1);
                }
                else
                {
                    invalid = true;
                }
            }

            if (invalid)
            {
                version = null;
                return false;
            }

            version.Labels = labels.Count == 0 ? null : labels.ToArray();

            return true;
        }
    }
}
