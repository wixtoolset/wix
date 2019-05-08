// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using WixToolset.Core.WindowsInstaller.Msi;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Runs internal consistency evaluators (ICEs) from cub files against a database.
    /// </summary>
    public sealed class Validator
    {
        private string actionName;
        private StringCollection cubeFiles;
        private ValidatorExtension extension;
        private Output output;
        private InstallUIHandler validationUIHandler;
        private bool validationSessionComplete;
        private readonly IMessaging messaging;

        /// <summary>
        /// Instantiate a new Validator.
        /// </summary>
        public Validator(IMessaging messaging)
        {
            this.cubeFiles = new StringCollection();
            this.extension = new ValidatorExtension(messaging);
            this.validationUIHandler = new InstallUIHandler(this.ValidationUIHandler);
            this.messaging = messaging;
        }

        /// <summary>
        /// Gets or sets a <see cref="ValidatorExtension"/> that directs messages from the validator.
        /// </summary>
        /// <value>A <see cref="ValidatorExtension"/> that directs messages from the validator.</value>
        public ValidatorExtension Extension
        {
            get { return this.extension; }
            set { this.extension = value; }
        }

        /// <summary>
        /// Gets or sets the list of ICEs to run.
        /// </summary>
        /// <value>The list of ICEs.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ISet<string> ICEs { get; set; }

        /// <summary>
        /// Gets or sets the output used for finding source line information.
        /// </summary>
        /// <value>The output used for finding source line information.</value>
        public Output Output
        {
            // cache Output object until validation for changes in extension
            get { return this.output; }
            set { this.output = value; }
        }

        /// <summary>
        /// Gets or sets the suppressed ICEs.
        /// </summary>
        /// <value>The suppressed ICEs.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ISet<string> SuppressedICEs { get; set; }

        /// <summary>
        /// Sets the temporary path for the Binder.
        /// </summary>
        public string IntermediateFolder { private get; set; }

        /// <summary>
        /// Add a cube file to the validation run.
        /// </summary>
        /// <param name="cubeFile">A cube file.</param>
        public void AddCubeFile(string cubeFile)
        {
            this.cubeFiles.Add(cubeFile);
        }

        /// <summary>
        /// Validate a database.
        /// </summary>
        /// <param name="databaseFile">The database to validate.</param>
        /// <returns>true if validation succeeded; false otherwise.</returns>
        public void Validate(string databaseFile)
        {
            int previousUILevel = (int)InstallUILevels.Basic;
            IntPtr previousHwnd = IntPtr.Zero;
            InstallUIHandler previousUIHandler = null;

            if (null == databaseFile)
            {
                throw new ArgumentNullException("databaseFile");
            }

            // initialize the validator extension
            this.extension.DatabaseFile = databaseFile;
            this.extension.Output = this.output;
            this.extension.InitializeValidator();

            // Ensure the temporary files can be created.
            Directory.CreateDirectory(this.IntermediateFolder);

            // copy the database to a temporary location so it can be manipulated
            string tempDatabaseFile = Path.Combine(this.IntermediateFolder, Path.GetFileName(databaseFile));
            File.Copy(databaseFile, tempDatabaseFile);

            // remove the read-only property from the temporary database
            FileAttributes attributes = File.GetAttributes(tempDatabaseFile);
            File.SetAttributes(tempDatabaseFile, attributes & ~FileAttributes.ReadOnly);

            Mutex mutex = new Mutex(false, "WixValidator");
            try
            {
                if (!mutex.WaitOne(0, false))
                {
                    this.messaging.Write(VerboseMessages.ValidationSerialized());
                    mutex.WaitOne();
                }

                using (Database database = new Database(tempDatabaseFile, OpenDatabase.Direct))
                {
                    bool propertyTableExists = database.TableExists("Property");
                    string productCode = null;

                    // remove the product code from the database before opening a session to prevent opening an installed product
                    if (propertyTableExists)
                    {
                        using (View view = database.OpenExecuteView("SELECT `Value` FROM `Property` WHERE Property = 'ProductCode'"))
                        {
                            using (Record record = view.Fetch())
                            {
                                if (null != record)
                                {
                                    productCode = record.GetString(1);

                                    using (View dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                                    {
                                    }
                                }
                            }
                        }
                    }

                    // merge in the cube databases
                    foreach (string cubeFile in this.cubeFiles)
                    {
                        try
                        {
                            using (Database cubeDatabase = new Database(cubeFile, OpenDatabase.ReadOnly))
                            {
                                try
                                {
                                    database.Merge(cubeDatabase, "MergeConflicts");
                                }
                                catch
                                {
                                    // ignore merge errors since they are expected in the _Validation table
                                }
                            }
                        }
                        catch (Win32Exception e)
                        {
                            if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                            {
                                throw new WixException(ErrorMessages.CubeFileNotFound(cubeFile));
                            }

                            throw;
                        }
                    }

                    // commit the database before proceeding to ensure the streams don't get confused
                    database.Commit();

                    // the property table may have been added to the database
                    // from a cub database without the proper validation rows
                    if (!propertyTableExists)
                    {
                        using (View view = database.OpenExecuteView("DROP table `Property`"))
                        {
                        }
                    }

                    // get all the action names for ICEs which have not been suppressed
                    List<string> actions = new List<string>();
                    using (View view = database.OpenExecuteView("SELECT `Action` FROM `_ICESequence` ORDER BY `Sequence`"))
                    {
                        while (true)
                        {
                            using (Record record = view.Fetch())
                            {
                                if (null == record)
                                {
                                    break;
                                }

                                string action = record.GetString(1);

                                if ((this.SuppressedICEs == null || !this.SuppressedICEs.Contains(action)) && (this.ICEs == null || this.ICEs.Contains(action)))
                                {
                                    actions.Add(action);
                                }
                            }
                        }
                    }

                    // disable the internal UI handler and set an external UI handler
                    previousUILevel = Installer.SetInternalUI((int)InstallUILevels.None, ref previousHwnd);
                    previousUIHandler = Installer.SetExternalUI(this.validationUIHandler, (int)InstallLogModes.Error | (int)InstallLogModes.Warning | (int)InstallLogModes.User, IntPtr.Zero);

                    // create a session for running the ICEs
                    this.validationSessionComplete = false;
                    using (Session session = new Session(database))
                    {
                        // add the product code back into the database
                        if (null != productCode)
                        {
                            // some CUBs erroneously have a ProductCode property, so delete it if we just picked one up
                            using (View dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                            {
                            }

                            using (View view = database.OpenExecuteView(String.Format(CultureInfo.InvariantCulture, "INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductCode', '{0}')", productCode)))
                            {
                            }
                        }

                        foreach (string action in actions)
                        {
                            this.actionName = action;
                            try
                            {
                                session.DoAction(action);
                            }
                            catch (Win32Exception e)
                            {
                                if (!this.messaging.EncounteredError)
                                {
                                    throw e;
                                }
                                // TODO: Review why this was clearing the error state when an exception had happened but an error was already encountered. That's weird.
                                //else
                                //{
                                //    this.encounteredError = false;
                                //}
                            }
                            this.actionName = null;
                        }

                        // Mark the validation session complete so we ignore any messages that MSI may fire
                        // during session clean-up.
                        this.validationSessionComplete = true;
                    }
                }
            }
            catch (Win32Exception e)
            {
                // avoid displaying errors twice since one may have already occurred in the UI handler
                if (!this.messaging.EncounteredError)
                {
                    if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                    {
                        // databaseFile is not passed since during light
                        // this would be the temporary copy and there would be
                        // no final output since the error occured; during smoke
                        // they should know the path passed into smoke
                        this.messaging.Write(ErrorMessages.ValidationFailedToOpenDatabase());
                    }
                    else if (0x64D == e.NativeErrorCode)
                    {
                        this.messaging.Write(ErrorMessages.ValidationFailedDueToLowMsiEngine());
                    }
                    else if (0x654 == e.NativeErrorCode)
                    {
                        this.messaging.Write(ErrorMessages.ValidationFailedDueToInvalidPackage());
                    }
                    else if (0x658 == e.NativeErrorCode)
                    {
                        this.messaging.Write(ErrorMessages.ValidationFailedDueToMultilanguageMergeModule());
                    }
                    else if (0x659 == e.NativeErrorCode)
                    {
                        this.messaging.Write(WarningMessages.ValidationFailedDueToSystemPolicy());
                    }
                    else
                    {
                        string msgTemp = e.Message;

                        if (null != this.actionName)
                        {
                            msgTemp = String.Concat("Action - '", this.actionName, "' ", e.Message);
                        }

                        this.messaging.Write(ErrorMessages.Win32Exception(e.NativeErrorCode, msgTemp));
                    }
                }
            }
            finally
            {
                Installer.SetExternalUI(previousUIHandler, 0, IntPtr.Zero);
                Installer.SetInternalUI(previousUILevel, ref previousHwnd);

                this.validationSessionComplete = false; // no validation session at this point, so reset the completion flag.

                mutex.ReleaseMutex();
                this.cubeFiles.Clear();
                this.extension.FinalizeValidator();
            }
        }

        public static Validator CreateFromContext(IBindContext context, string cubeFilename)
        {
            Validator validator = null;
            var messaging = context.ServiceProvider.GetService<IMessaging>();

            // Tell the binder about the validator if validation isn't suppressed
            if (!context.SuppressValidation)
            {
                validator = new Validator(messaging);
                validator.IntermediateFolder = Path.Combine(context.IntermediateFolder, "validate");

                // set the default cube file
                string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                validator.AddCubeFile(Path.Combine(thisPath, cubeFilename));

                // Set the ICEs
                validator.ICEs = new SortedSet<string>(context.Ices);

                // Set the suppressed ICEs and disable ICEs that have equivalent-or-better checks in WiX.
                validator.SuppressedICEs = new SortedSet<string>(context.SuppressIces.Union(new[] { "ICE08", "ICE33", "ICE47", "ICE66" }));
            }

            return validator;
        }

        /// <summary>
        /// The validation external UI handler.
        /// </summary>
        /// <param name="context">Pointer to an application context.
        /// This parameter can be used for error checking.</param>
        /// <param name="messageType">Specifies a combination of one message box style,
        /// one message box icon type, one default button, and one installation message type.</param>
        /// <param name="message">Specifies the message text.</param>
        /// <returns>-1 for an error, 0 if no action was taken, 1 if OK, 3 to abort.</returns>
        private int ValidationUIHandler(IntPtr context, uint messageType, string message)
        {
            try
            {
                // If we're getting messges during the validation session, send them to
                // the extension. Otherwise, ignore the messages.
                if (!this.validationSessionComplete)
                {
                    this.extension.Log(message, this.actionName);
                }
            }
            catch (WixException ex)
            {
                this.messaging.Write(ex.Error);
            }

            return 1;
        }
    }
}
