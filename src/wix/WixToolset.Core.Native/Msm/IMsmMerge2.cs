// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMsmMerge2 interface.
    /// </summary>
    [ComImport, Guid("351A72AB-21CB-47ab-B7AA-C4D7B02EA305")]
    public interface IMsmMerge2
    {
        /// <summary>
        /// The OpenDatabase method of the Merge object opens a Windows Installer installation
        /// database, located at a specified path, that is to be merged with a module.
        /// </summary>
        /// <param name="path">Path to the database being opened.</param>
        void OpenDatabase(string path);

        /// <summary>
        /// The OpenModule method of the Merge object opens a Windows Installer merge module
        /// in read-only mode. A module must be opened before it can be merged with an installation database.
        /// </summary>
        /// <param name="fileName">Fully qualified file name pointing to a merge module.</param>
        /// <param name="language">A valid language identifier (LANGID).</param>
        void OpenModule(string fileName, short language);

        /// <summary>
        /// The CloseDatabase method of the Merge object closes the currently open Windows Installer database.
        /// </summary>
        /// <param name="commit">true if changes should be saved, false otherwise.</param>
        void CloseDatabase(bool commit);

        /// <summary>
        /// The CloseModule method of the Merge object closes the currently open Windows Installer merge module.
        /// </summary>
        void CloseModule();

        /// <summary>
        /// The OpenLog method of the Merge object opens a log file that receives progress and error messages.
        /// If the log file already exists, the installer appends new messages. If the log file does not exist,
        /// the installer creates a log file.
        /// </summary>
        /// <param name="fileName">Fully qualified filename pointing to a file to open or create.</param>
        void OpenLog(string fileName);

        /// <summary>
        /// The CloseLog method of the Merge object closes the current log file.
        /// </summary>
        void CloseLog();

        /// <summary>
        /// The Log method of the Merge object writes a text string to the currently open log file.
        /// </summary>
        /// <param name="message">The text string to display.</param>
        void Log(string message);

        /// <summary>
        /// Gets the errors from the last merge operation.
        /// </summary>
        /// <value>The errors from the last merge operation.</value>
        IMsmErrors Errors
        {
            get;
        }

        /// <summary>
        /// Gets a collection of Dependency objects that enumerates a set of unsatisfied dependencies for the current database.
        /// </summary>
        /// <value>A  collection of Dependency objects that enumerates a set of unsatisfied dependencies for the current database.</value>
        object Dependencies
        {
            get;
        }

        /// <summary>
        /// The Merge method of the Merge object executes a merge of the current database and current
        /// module. The merge attaches the components in the module to the feature identified by Feature.
        /// The root of the module's directory tree is redirected to the location given by RedirectDir.
        /// </summary>
        /// <param name="feature">The name of a feature in the database.</param>
        /// <param name="redirectDir">The key of an entry in the Directory table of the database.
        /// This parameter may be NULL or an empty string.</param>
        void Merge(string feature, string redirectDir);

        /// <summary>
        /// The Connect method of the Merge object connects a module to an additional feature.
        /// The module must have already been merged into the database or will be merged into the database.
        /// The feature must exist before calling this function.
        /// </summary>
        /// <param name="feature">The name of a feature already existing in the database.</param>
        void Connect(string feature);

        /// <summary>
        /// The ExtractCAB method of the Merge object extracts the embedded .cab file from a module and
        /// saves it as the specified file. The installer creates this file if it does not already exist
        /// and overwritten if it does exist.
        /// </summary>
        /// <param name="fileName">The fully qualified destination file.</param>
        void ExtractCAB(string fileName);

        /// <summary>
        /// The ExtractFiles method of the Merge object extracts the embedded .cab file from a module
        /// and then writes those files to the destination directory.
        /// </summary>
        /// <param name="path">The fully qualified destination directory.</param>
        void ExtractFiles(string path);

        /// <summary>
        /// The MergeEx method of the Merge object is equivalent to the Merge function, except that it
        /// takes an extra argument.  The Merge method executes a merge of the current database and
        /// current module. The merge attaches the components in the module to the feature identified
        /// by Feature. The root of the module's directory tree is redirected to the location given by RedirectDir.
        /// </summary>
        /// <param name="feature">The name of a feature in the database.</param>
        /// <param name="redirectDir">The key of an entry in the Directory table of the database. This parameter may
        /// be NULL or an empty string.</param>
        /// <param name="configuration">The pConfiguration argument is an interface implemented by the client. The argument may
        /// be NULL. The presence of this argument indicates that the client is capable of supporting the configuration
        /// functionality, but does not obligate the client to provide configuration data for any specific configurable item.</param>
        void MergeEx(string feature, string redirectDir, IMsmConfigureModule configuration);

        /// <summary>
        /// The ExtractFilesEx method of the Merge object extracts the embedded .cab file from a module and
        /// then writes those files to the destination directory.
        /// </summary>
        /// <param name="path">The fully qualified destination directory.</param>
        /// <param name="longFileNames">Set to specify using long file names for path segments and final file names.</param>
        /// <param name="filePaths">This is a list of fully-qualified paths for the files that were successfully extracted.
        /// The list is empty if no files can be extracted.  This argument may be null.  No list is provided if pFilePaths is null.</param>
        void ExtractFilesEx(string path, bool longFileNames, ref IntPtr filePaths);

        /// <summary>
        /// Gets a collection ConfigurableItem objects, each of which represents a single row from the ModuleConfiguration table.
        /// </summary>
        /// <value>A collection ConfigurableItem objects, each of which represents a single row from the ModuleConfiguration table.</value>
        /// <remarks>Semantically, each interface in the enumerator represents an item that can be configured by the module consumer.
        /// The collection is a read-only collection and implements the standard read-only collection interfaces of Item(), Count() and _NewEnum().
        /// The IEnumMsmConfigItems enumerator implements Next(), Skip(), Reset(), and Clone() with the standard semantics.</remarks>
        object ConfigurableItems
        {
            get;
        }

        /// <summary>
        /// The CreateSourceImage method of the Merge object allows the client to extract the files from a module to
        /// a source image on disk after a merge, taking into account changes to the module that might have been made
        /// during module configuration. The list of files to be extracted is taken from the file table of the module
        /// during the merge process. The list of files consists of every file successfully copied from the file table
        /// of the module to the target database. File table entries that were not copied due to primary key conflicts
        /// with existing rows in the database are not a part of this list. At image creation time, the directory for
        /// each of these files comes from the open (post-merge) database. The path specified in the Path parameter is
        /// the root of the source image for the install. fLongFileNames determines whether or not long file names are
        /// used for both path segments and final file names. The function fails if no database is open, no module is
        /// open, or no merge has been performed.
        /// </summary>
        /// <param name="path">The path of the root of the source image for the install.</param>
        /// <param name="longFileNames">Determines whether or not long file names are used for both path segments and final file names. </param>
        /// <param name="filePaths">This is a list of fully-qualified paths for the files that were successfully extracted.
        /// The list is empty if no files can be extracted.  This argument may be null.  No list is provided if pFilePaths is null.</param>
        void CreateSourceImage(string path, bool longFileNames, ref IntPtr filePaths);

        /// <summary>
        /// The get_ModuleFiles function implements the ModuleFiles property of the GetFiles object. This function
        /// returns the primary keys in the File table of the currently open module. The primary keys are returned
        /// as a collection of strings. The module must be opened by a call to the OpenModule function before calling get_ModuleFiles.
        /// </summary>
        IMsmStrings ModuleFiles
        {
            get;
        }
    }
}
