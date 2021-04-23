// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WixToolset.Core.Native.Msi;
    using WixToolset.Data;

    /// <summary>
    /// Windows installer validation implementation.
    /// </summary>
    public class WindowsInstallerValidator
    {
        private const string CubesFolder = "cubes";

        /// <summary>
        /// Creates a new Windows Installer validator.
        /// </summary>
        /// <param name="callback">Callback interface to handle messages.</param>
        /// <param name="databasePath">Database to validate.</param>
        /// <param name="cubeFiles">Set of CUBe files to merge.</param>
        /// <param name="ices">ICEs to execute.</param>
        /// <param name="suppressedIces">Suppressed ICEs.</param>
        public WindowsInstallerValidator(IWindowsInstallerValidatorCallback callback, string databasePath, IEnumerable<string> cubeFiles, IEnumerable<string> ices, IEnumerable<string> suppressedIces)
        {
            this.Callback = callback;
            this.DatabasePath = databasePath;
            this.CubeFiles = cubeFiles;
            this.Ices = new SortedSet<string>(ices);
            this.SuppressedIces = new SortedSet<string>(suppressedIces);
        }

        private IWindowsInstallerValidatorCallback Callback { get; }

        private string DatabasePath { get; }

        private IEnumerable<string> CubeFiles { get; }

        private SortedSet<string> Ices { get; }

        private SortedSet<string> SuppressedIces { get; }

        private bool ValidationSessionInProgress { get; set; }

        private string CurrentIce { get; set; }

        /// <summary>
        /// Execute the validations.
        /// </summary>
        public void Execute()
        {
            using (var mutex = new Mutex(false, "WixValidator"))
            {
                try
                {
                    if (!mutex.WaitOne(0))
                    {
                        this.Callback.ValidationBlocked();
                        mutex.WaitOne();
                    }
                }
                catch (AbandonedMutexException)
                {
                    // Another validation process was probably killed, we own the mutex now.
                }

                try
                {
                    this.RunValidations();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private void RunValidations()
        {
            var previousUILevel = (int)InstallUILevels.Basic;
            var previousHwnd = IntPtr.Zero;
            InstallUIHandler previousUIHandler = null;

            try
            {
                using (var database = new Database(this.DatabasePath, OpenDatabase.Direct))
                {
                    var propertyTableExists = database.TableExists("Property");
                    string productCode = null;

                    // Remove the product code from the database before opening a session to prevent opening an installed product.
                    if (propertyTableExists)
                    {
                        using (var view = database.OpenExecuteView("SELECT `Value` FROM `Property` WHERE Property = 'ProductCode'"))
                        {
                            using (var record = view.Fetch())
                            {
                                if (null != record)
                                {
                                    productCode = record.GetString(1);

                                    using (var dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                                    {
                                    }
                                }
                            }
                        }
                    }

                    // Merge in the cube databases.
                    foreach (var cubeFile in this.CubeFiles)
                    {
                        var findCubeFile = typeof(WindowsInstallerValidator).Assembly.FindFileRelativeToAssembly(Path.Combine(CubesFolder, cubeFile), searchNativeDllDirectories: false);

                        if (!findCubeFile.Found)
                        {
                            throw new WixException(ErrorMessages.CubeFileNotFound(findCubeFile.Path));
                        }

                        try
                        {
                            using (var cubeDatabase = new Database(findCubeFile.Path, OpenDatabase.ReadOnly))
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
                                throw new WixException(ErrorMessages.CubeFileNotFound(findCubeFile.Path));
                            }

                            throw;
                        }
                    }

                    // Commit the database before proceeding to ensure the streams don't get confused.
                    database.Commit();

                    // The property table may have been added to the database from a cub database without the proper validation rows.
                    if (!propertyTableExists)
                    {
                        using (var view = database.OpenExecuteView("DROP table `Property`"))
                        {
                        }
                    }

                    // Get all the action names for ICEs which have not been suppressed.
                    var actions = new List<string>();
                    using (var view = database.OpenExecuteView("SELECT `Action` FROM `_ICESequence` ORDER BY `Sequence`"))
                    {
                        foreach (var record in view.Records)
                        {
                            var action = record.GetString(1);

                            if (!this.SuppressedIces.Contains(action) && this.Ices.Contains(action))
                            {
                                actions.Add(action);
                            }
                        }
                    }

                    // Disable the internal UI handler and set an external UI handler.
                    previousUILevel = Installer.SetInternalUI((int)InstallUILevels.None, ref previousHwnd);
                    previousUIHandler = Installer.SetExternalUI(this.ValidationUIHandler, (int)InstallLogModes.Error | (int)InstallLogModes.Warning | (int)InstallLogModes.User, IntPtr.Zero);

                    // Create a session for running the ICEs.
                    this.ValidationSessionInProgress = true;

                    using (var session = new Session(database))
                    {
                        // Add the product code back into the database.
                        if (null != productCode)
                        {
                            // Some CUBs erroneously have a ProductCode property, so delete it if we just picked one up.
                            using (var dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                            {
                            }

                            using (var view = database.OpenExecuteView($"INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductCode', '{productCode}')"))
                            {
                            }
                        }

                        foreach (var action in actions)
                        {
                            this.CurrentIce = action;

                            try
                            {
                                session.DoAction(action);
                            }
                            catch (Win32Exception e)
                            {
                                if (!this.Callback.EncounteredError)
                                {
                                    throw e;
                                }
                            }

                            this.CurrentIce = null;
                        }

                        // Mark the validation session complete so we ignore any messages that MSI may fire
                        // during session clean-up.
                        this.ValidationSessionInProgress = false;
                    }
                }
            }
            catch (Win32Exception e)
            {
                // Avoid displaying errors twice since one may have already occurred in the UI handler.
                if (!this.Callback.EncounteredError)
                {
                    if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                    {
                        // The database path is not passed to this exception since inside wix.exe
                        // this would be the temporary copy and there would be no final output becasue
                        // this error occured; and during standalone validation they should know the path
                        // passed in.
                        throw new WixException(ErrorMessages.ValidationFailedToOpenDatabase());
                    }
                    else if (0x64D == e.NativeErrorCode)
                    {
                        throw new WixException(ErrorMessages.ValidationFailedDueToLowMsiEngine());
                    }
                    else if (0x654 == e.NativeErrorCode)
                    {
                        throw new WixException(ErrorMessages.ValidationFailedDueToInvalidPackage());
                    }
                    else if (0x658 == e.NativeErrorCode)
                    {
                        throw new WixException(ErrorMessages.ValidationFailedDueToMultilanguageMergeModule());
                    }
                    else if (0x659 == e.NativeErrorCode)
                    {
                        throw new WixException(WarningMessages.ValidationFailedDueToSystemPolicy());
                    }
                    else
                    {
                        var msg = String.IsNullOrEmpty(this.CurrentIce) ? e.Message : $"Action - '{this.CurrentIce}' {e.Message}";

                        throw new WixException(ErrorMessages.Win32Exception(e.NativeErrorCode, msg));
                    }
                }
            }
            finally
            {
                this.ValidationSessionInProgress = false;

                Installer.SetExternalUI(previousUIHandler, 0, IntPtr.Zero);
                Installer.SetInternalUI(previousUILevel, ref previousHwnd);
            }
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
            var continueValidation = true;

            // If we're getting messges during the validation session, log them.
            // Otherwise, ignore the messages.
            if (!this.ValidationSessionInProgress)
            {
                var parsedMessage = ParseValidationMessage(message, this.CurrentIce);

                continueValidation = this.Callback.ValidationMessage(parsedMessage);
            }

            return continueValidation ? 1 : 3;
        }

        /// <summary>
        /// Parses a message from the Validator.
        /// </summary>
        /// <param name="message">A <see cref="String"/> of tab-delmited tokens
        /// in the validation message.</param>
        /// <param name="currentIce">The name of the action to which the message
        /// belongs.</param>
        /// <exception cref="ArgumentNullException">The message cannot be null.
        /// </exception>
        /// <exception cref="WixException">The message does not contain four (4)
        /// or more tab-delimited tokens.</exception>
        /// <remarks>
        /// <para><paramref name="message"/> a tab-delimited set of tokens,
        /// formatted according to Windows Installer guidelines for ICE
        /// message. The following table lists what each token by index
        /// should mean.</para>
        /// <para><paramref name="currentIce"/> a name that represents the ICE
        /// action that was executed (e.g. 'ICE08').</para>
        /// <list type="table">
        /// <listheader>
        ///     <term>Index</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>0</term>
        ///     <description>Name of the ICE.</description>
        /// </item>
        /// <item>
        ///     <term>1</term>
        ///     <description>Message type. See the following list.</description>
        /// </item>
        /// <item>
        ///     <term>2</term>
        ///     <description>Detailed description.</description>
        /// </item>
        /// <item>
        ///     <term>3</term>
        ///     <description>Help URL or location.</description>
        /// </item>
        /// <item>
        ///     <term>4</term>
        ///     <description>Table name.</description>
        /// </item>
        /// <item>
        ///     <term>5</term>
        ///     <description>Column name.</description>
        /// </item>
        /// <item>
        ///     <term>6</term>
        ///     <description>This and remaining fields are primary keys
        ///     to identify a row.</description>
        /// </item>
        /// </list>
        /// <para>The message types are one of the following value.</para>
        /// <list type="table">
        /// <listheader>
        ///     <term>Value</term>
        ///     <description>Message Type</description>
        /// </listheader>
        /// <item>
        ///     <term>0</term>
        ///     <description>Failure message reporting the failure of the
        ///     ICE custom action.</description>
        /// </item>
        /// <item>
        ///     <term>1</term>
        ///     <description>Error message reporting database authoring that
        ///     case incorrect behavior.</description>
        /// </item>
        /// <item>
        ///     <term>2</term>
        ///     <description>Warning message reporting database authoring that
        ///     causes incorrect behavior in certain cases. Warnings can also
        ///     report unexpected side-effects of database authoring.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>3</term>
        ///     <description>Informational message.</description>
        /// </item>
        /// </list>
        /// </remarks>
        private static ValidationMessage ParseValidationMessage(string message, string currentIce)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageParts = message.Split('\t');
            if (messageParts.Length < 3)
            {
                if (null == currentIce)
                {
                    throw new WixException(ErrorMessages.UnexpectedExternalUIMessage(message));
                }
                else
                {
                    throw new WixException(ErrorMessages.UnexpectedExternalUIMessage(message, currentIce));
                }
            }

            var type = ParseValidationMessageType(messageParts[1]);

            return new ValidationMessage
            {
                IceName = messageParts[0],
                Type = type,
                Description = messageParts[2],
                HelpUrl = messageParts.Length > 3 ? messageParts[3] : null,
                Table = messageParts.Length > 4 ? messageParts[4] : null,
                Column = messageParts.Length > 5 ? messageParts[4] : null,
                PrimaryKeys = messageParts.Length > 6 ? messageParts.Skip(6).ToArray() : null
            };
        }

        private static ValidationMessageType ParseValidationMessageType(string type)
        {
            switch (type)
            {
                case "0":
                    return ValidationMessageType.InternalFailure;
                case "1":
                    return ValidationMessageType.Error;
                case "2":
                    return ValidationMessageType.Warning;
                case "3":
                    return ValidationMessageType.Info;
                default:
                    throw new WixException(ErrorMessages.InvalidValidatorMessageType(type));
            }
        }
    }
}
