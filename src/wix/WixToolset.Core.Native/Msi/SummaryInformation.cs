// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using System.Globalization;
    using System.Text;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    /// <summary>
    /// Summary information for the MSI files.
    /// </summary>
    public sealed class SummaryInformation : MsiHandle
    {
        /// <summary>
        /// Summary information properties for products.
        /// </summary>
        public enum Package
        {
            /// <summary>PID_CODEPAGE = code page of the summary information stream</summary>
            CodePage = 1,

            /// <summary>PID_TITLE = a brief description of the package type</summary>
            Title = 2,

            /// <summary>PID_SUBJECT = package name</summary>
            PackageName = 3,

            /// <summary>PID_AUTHOR = manufacturer of the patch package</summary>
            Manufacturer = 4,

            /// <summary>PID_KEYWORDS = list of keywords used by file browser</summary>
            Keywords = 5,

            /// <summary>PID_COMMENTS = general purpose of the package</summary>
            Comments = 6,

            /// <summary>PID_TEMPLATE = supported platforms and languages</summary>
            PlatformsAndLanguages = 7,

            /// <summary>PID_LASTAUTHOR should be null for packages</summary>
            Reserved8 = 8,

            /// <summary>PID_REVNUMBER = GUID package code</summary>
            PackageCode = 9,

            /// <summary>PID_LASTPRINTED should be null for packages</summary>
            Reserved11 = 11,

            /// <summary>PID_CREATED datetime when package was created</summary>
            Created = 12,

            /// <summary>PID_SAVED datetime when package was last modified</summary>
            LastModified = 13,

            /// <summary>PID_PAGECOUNT minimum required Windows Installer</summary>
            InstallerRequirement = 14,

            /// <summary>PID_WORDCOUNT elevation privileges of package</summary>
            FileAndElevatedFlags = 15,

            /// <summary>PID_CHARCOUNT should be null for patches</summary>
            Reserved16 = 16,

            /// <summary>PID_APPLICATION tool used to create package</summary>
            BuildTool = 18,

            /// <summary>PID_SECURITY = read-only attribute of the package</summary>
            Security = 19,
        }

        /// <summary>
        /// Summary information properties for transforms.
        /// </summary>
        public enum Transform
        {
            /// <summary>PID_CODEPAGE = code page for the summary information stream</summary>
            CodePage = 1,

            /// <summary>PID_TITLE = typically just "Transform"</summary>
            Title = 2,

            /// <summary>PID_SUBJECT = original subject of target</summary>
            TargetSubject = 3,

            /// <summary>PID_AUTHOR = original manufacturer of target</summary>
            TargetManufacturer = 4,

            /// <summary>PID_KEYWORDS = keywords for the transform, typically including at least "Installer"</summary>
            Keywords = 5,

            /// <summary>PID_COMMENTS = describes what this package does</summary>
            Comments = 6,

            /// <summary>PID_TEMPLATE = target platform;language</summary>
            TargetPlatformAndLanguage = 7,

            /// <summary>PID_LASTAUTHOR = updated platform;language</summary>
            UpdatedPlatformAndLanguage = 8,

            /// <summary>PID_REVNUMBER = {productcode}version;{newproductcode}newversion;upgradecode</summary>
            ProductCodes = 9,

            /// <summary>PID_LASTPRINTED should be null for transforms</summary>
            Reserved11 = 11,

            ///.<summary>PID_CREATE_DTM = the timestamp when the transform was created</summary>
            CreationTime = 12,

            /// <summary>PID_PAGECOUNT = minimum installer version</summary>
            InstallerRequirement = 14,

            /// <summary>PID_CHARCOUNT = validation and error flags</summary>
            ValidationFlags = 16,

            /// <summary>PID_APPNAME = the application that created the transform</summary>
            CreatingApplication = 18,

            /// <summary>PID_SECURITY = whether read-only is enforced; should always be 4 for transforms</summary>
            Security = 19,
        }

        /// <summary>
        /// Summary information properties for patches.
        /// </summary>
        public enum Patch
        {
            /// <summary>PID_CODEPAGE = code page of the summary information stream</summary>
            CodePage = 1,

            /// <summary>PID_TITLE = a brief description of the package type</summary>
            Title = 2,

            /// <summary>PID_SUBJECT = package name</summary>
            PackageName = 3,

            /// <summary>PID_AUTHOR = manufacturer of the patch package</summary>
            Manufacturer = 4,

            /// <summary>PID_KEYWORDS = alternate sources for the patch package</summary>
            Sources = 5,

            /// <summary>PID_COMMENTS = general purpose of the patch package</summary>
            Comments = 6,

            /// <summary>PID_TEMPLATE = semicolon delimited list of ProductCodes</summary>
            ProductCodes = 7,

            /// <summary>PID_LASTAUTHOR = semicolon delimited list of transform names</summary>
            TransformNames = 8,

            /// <summary>PID_REVNUMBER = GUID patch code</summary>
            PatchCode = 9,

            /// <summary>PID_LASTPRINTED should be null for patches</summary>
            Reserved11 = 11,

            /// <summary>PID_PAGECOUNT should be null for patches</summary>
            Reserved14 = 14,

            /// <summary>PID_WORDCOUNT = minimum installer version</summary>
            InstallerRequirement = 15,

            /// <summary>PID_CHARCOUNT should be null for patches</summary>
            Reserved16 = 16,

            /// <summary>PID_SECURITY = read-only attribute of the patch package</summary>
            Security = 19,
        }

        /// <summary>
        /// Summary information values for the InstallerRequirement property.
        /// </summary>
        public enum InstallerRequirement
        {
            /// <summary>Any version of the installer will do</summary>
            Version10 = 1,

            /// <summary>At least 1.2</summary>
            Version12 = 2,

            /// <summary>At least 2.0</summary>
            Version20 = 3,

            /// <summary>At least 3.0</summary>
            Version30 = 4,

            /// <summary>At least 3.1</summary>
            Version31 = 5,
        }

        /// <summary>
        /// Instantiate a new SummaryInformation class from an open database.
        /// </summary>
        /// <param name="db">Database to retrieve summary information from.</param>
        public SummaryInformation(Database db)
        {
            if (null == db)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var handle = IntPtr.Zero;
            var error = MsiInterop.MsiGetSummaryInformation(db.Handle, null, 0, ref handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
            this.Handle = handle;
        }

        /// <summary>
        /// Instantiate a new SummaryInformation class from a database file.
        /// </summary>
        /// <param name="databaseFile">The database file.</param>
        public SummaryInformation(string databaseFile)
        {
            if (null == databaseFile)
            {
                throw new ArgumentNullException(nameof(databaseFile));
            }

            var handle = IntPtr.Zero;
            var error = MsiInterop.MsiGetSummaryInformation(IntPtr.Zero, databaseFile, 0, ref handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
            this.Handle = handle;
        }

        /// <summary>
        /// Gets a summary information package property.
        /// </summary>
        /// <param name="property">The summary information package property.</param>
        /// <returns>The summary information property.</returns>
        public string GetProperty(Package property)
        {
            return this.GetProperty((int)property);
        }

        /// <summary>
        /// Gets a summary information package property as a number.
        /// </summary>
        /// <param name="property">The summary information package property.</param>
        /// <returns>The summary information property.</returns>
        public long GetNumericProperty(Package property)
        {
            return this.GetNumericProperty((int)property);
        }

        /// <summary>
        /// Gets a summary information patch property.
        /// </summary>
        /// <param name="property">The summary information patch property.</param>
        /// <returns>The summary information property.</returns>
        public string GetProperty(Patch property)
        {
            return this.GetProperty((int)property);
        }

        /// <summary>
        /// Gets a summary information transform property.
        /// </summary>
        /// <param name="property">The summary information transform property.</param>
        /// <returns>The summary information property.</returns>
        public long GetNumericProperty(Transform property)
        {
            return this.GetNumericProperty((int)property);
        }

        /// <summary>
        /// Gets a summary information property.
        /// </summary>
        /// <param name="index">Index of the summary information property.</param>
        /// <returns>The summary information property.</returns>
        public string GetProperty(int index)
        {
            this.GetSummaryInformationValue(index, out var dataType, out var intValue, out var stringValue, out var timeValue);

            switch ((VT)dataType)
            {
                case VT.EMPTY:
                    return String.Empty;

                case VT.LPSTR:
                    return stringValue.ToString();

                case VT.I2:
                case VT.I4:
                    return Convert.ToString(intValue, CultureInfo.InvariantCulture);

                case VT.FILETIME:
                    var longFileTime = (((long)timeValue.dwHighDateTime) << 32) | unchecked((uint)timeValue.dwLowDateTime);
                    var dateTime = DateTime.FromFileTime(longFileTime);
                    return dateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets a summary information property as a number.
        /// </summary>
        /// <param name="index">Index of the summary information property.</param>
        /// <returns>The summary information property.</returns>
        public long GetNumericProperty(int index)
        {
            this.GetSummaryInformationValue(index, out var dataType, out var intValue, out var stringValue, out var timeValue);

            switch ((VT)dataType)
            {
                case VT.EMPTY:
                    return 0;

                case VT.LPSTR:
                    return Int64.Parse(stringValue.ToString(), CultureInfo.InvariantCulture);

                case VT.I2:
                case VT.I4:
                    return intValue;

                case VT.FILETIME:
                    return (((long)timeValue.dwHighDateTime) << 32) | unchecked((uint)timeValue.dwLowDateTime);

                default:
                    throw new InvalidOperationException();
            }
        }

        private void GetSummaryInformationValue(int index, out uint dataType, out int intValue, out StringBuilder stringValue, out FILETIME timeValue)
        {
            var bufSize = 64;
            stringValue = new StringBuilder(bufSize);
            timeValue.dwHighDateTime = 0;
            timeValue.dwLowDateTime = 0;

            var error = MsiInterop.MsiSummaryInfoGetProperty(this.Handle, index, out dataType, out intValue, ref timeValue, stringValue, ref bufSize);
            if (234 == error)
            {
                stringValue.EnsureCapacity(++bufSize);
                error = MsiInterop.MsiSummaryInfoGetProperty(this.Handle, index, out dataType, out intValue, ref timeValue, stringValue, ref bufSize);
            }

            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Variant types in the summary information table.
        /// </summary>
        private enum VT : uint
        {
            /// <summary>Variant has not been assigned.</summary>
            EMPTY = 0,

            /// <summary>Null variant type.</summary>
            NULL = 1,

            /// <summary>16-bit integer variant type.</summary>
            I2 = 2,

            /// <summary>32-bit integer variant type.</summary>
            I4 = 3,

            /// <summary>String variant type.</summary>
            LPSTR = 30,

            /// <summary>Date time (FILETIME, converted to Variant time) variant type.</summary>
            FILETIME = 64,
        }
    }
}
