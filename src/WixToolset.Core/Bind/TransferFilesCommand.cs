// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.AccessControl;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class TransferFilesCommand
    {
        public TransferFilesCommand(IMessaging messaging, IEnumerable<ILayoutExtension> extensions, IEnumerable<FileTransfer> fileTransfers, bool suppressAclReset)
        {
            this.FileSystem = new FileSystem(extensions);
            this.Messaging = messaging;
            this.FileTransfers = fileTransfers;
            this.SuppressAclReset = suppressAclReset;
        }

        private FileSystem FileSystem { get; }

        private IMessaging Messaging { get; }

        private IEnumerable<FileTransfer> FileTransfers { get; }

        private bool SuppressAclReset { get; }

        public void Execute()
        {
            List<string> destinationFiles = new List<string>();

            foreach (var fileTransfer in this.FileTransfers)
            {
                // If the source and destination are identical, then there's nothing to do here
                if (0 == String.Compare(fileTransfer.Source, fileTransfer.Destination, StringComparison.OrdinalIgnoreCase))
                {
                    fileTransfer.Redundant = true;
                    continue;
                }

                bool retry = false;
                do
                {
                    try
                    {
                        if (fileTransfer.Move)
                        {
                            this.Messaging.Write(VerboseMessages.MoveFile(fileTransfer.Source, fileTransfer.Destination));
                            this.TransferFile(true, fileTransfer.Source, fileTransfer.Destination);
                        }
                        else
                        {
                            this.Messaging.Write(VerboseMessages.CopyFile(fileTransfer.Source, fileTransfer.Destination));
                            this.TransferFile(false, fileTransfer.Source, fileTransfer.Destination);
                        }

                        retry = false;
                        destinationFiles.Add(fileTransfer.Destination);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new WixFileNotFoundException(fileTransfer.SourceLineNumbers, e.FileName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        string directory = Path.GetDirectoryName(fileTransfer.Destination);
                        this.Messaging.Write(VerboseMessages.CreateDirectory(directory));
                        Directory.CreateDirectory(directory);
                        retry = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.Messaging.Write(VerboseMessages.RemoveDestinationFile(fileTransfer.Destination));

                            // try to ensure the file is not read-only
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            try
                            {
                                File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            }
                            catch (ArgumentException) // thrown for unauthorized access errors
                            {
                                throw new WixException(ErrorMessages.UnauthorizedAccess(fileTransfer.Destination));
                            }

                            // try to delete the file
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(ErrorMessages.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                    catch (IOException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.Messaging.Write(VerboseMessages.RemoveDestinationFile(fileTransfer.Destination));

                            // ensure the file is not read-only, then delete it
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(ErrorMessages.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                } while (retry);
            }

            // Finally, if there were any files remove the ACL that may have been added to
            // during the file transfer process.
            if (0 < destinationFiles.Count && !this.SuppressAclReset)
            {
                var aclReset = new FileSecurity();
                aclReset.SetAccessRuleProtection(false, false);

                try
                {
                    //WixToolset.Core.Native.NativeMethods.ResetAcls(destinationFiles.ToArray(), (uint)destinationFiles.Count);

                    foreach (var file in destinationFiles)
                    {
                        new FileInfo(file).SetAccessControl(aclReset);
                    }
                }
                catch
                {
                    this.Messaging.Write(WarningMessages.UnableToResetAcls());
                }
            }
        }

        private void TransferFile(bool move, string source, string destination)
        {
            bool complete = false;

            if (move)
            {
                complete = this.FileSystem.MoveFile(source, destination);
            }
            else
            {
                complete = this.FileSystem.CopyFile(source, destination);
            }

            if (!complete)
            {
                throw new InvalidOperationException(); // TODO: something needs to be said here that none of the binder file managers returned a result.
            }
        }
    }
}
