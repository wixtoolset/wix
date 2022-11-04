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
        public TransferFilesCommand(IMessaging messaging, IFileSystem fileSystem, IEnumerable<ILayoutExtension> extensions, IEnumerable<IFileTransfer> fileTransfers, bool resetAcls)
        {
            this.Extensions = extensions;
            this.Messaging = messaging;
            this.FileSystem = fileSystem;
            this.FileTransfers = fileTransfers;
            this.ResetAcls = resetAcls;
        }

        private IMessaging Messaging { get; }

        private IFileSystem FileSystem { get; }

        private IEnumerable<ILayoutExtension> Extensions { get; }

        private IEnumerable<IFileTransfer> FileTransfers { get; }

        private bool ResetAcls { get; }

        public void Execute()
        {
            var destinationFiles = new List<string>();

            foreach (var fileTransfer in this.FileTransfers)
            {
                // If the source and destination are identical, then there's nothing to do here
                if (0 == String.Compare(fileTransfer.Source, fileTransfer.Destination, StringComparison.OrdinalIgnoreCase))
                {
                    fileTransfer.Redundant = true;
                    continue;
                }

                var retry = false;
                do
                {
                    try
                    {
                        if (fileTransfer.Move)
                        {
                            this.Messaging.Write(VerboseMessages.MoveFile(fileTransfer.Source, fileTransfer.Destination));
                            this.MoveFile(fileTransfer.SourceLineNumbers, fileTransfer.Source, fileTransfer.Destination);
                        }
                        else
                        {
                            this.Messaging.Write(VerboseMessages.CopyFile(fileTransfer.Source, fileTransfer.Destination));
                            this.CopyFile(fileTransfer.SourceLineNumbers, fileTransfer.Source, fileTransfer.Destination);
                        }

                        retry = false;
                        destinationFiles.Add(fileTransfer.Destination);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new WixException(ErrorMessages.FileNotFound(fileTransfer.SourceLineNumbers, e.FileName));
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        var directory = Path.GetDirectoryName(fileTransfer.Destination);
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
                            var attributes = File.GetAttributes(fileTransfer.Destination);
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
                            var attributes = File.GetAttributes(fileTransfer.Destination);
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

            // Finally, if directed then reset remove ACLs that may may have been picked up
            // during the file transfer process.
            if (this.ResetAcls && 0 < destinationFiles.Count)
            {
                try
                {
                    this.AclReset(destinationFiles);
                }
                catch (Exception e)
                {
                    this.Messaging.Write(WarningMessages.UnableToResetAcls(e.Message));
                }
            }
        }

        private void CopyFile(SourceLineNumber sourceLineNumbers, string source, string destination)
        {
            foreach (var extension in this.Extensions)
            {
                if (extension.CopyFile(source, destination))
                {
                    return;
                }
            }

            this.FileSystem.CopyFile(sourceLineNumbers, source, destination, allowHardlink: true);
        }

        private void MoveFile(SourceLineNumber sourceLineNumbers, string source, string destination)
        {
            foreach (var extension in this.Extensions)
            {
                if (extension.MoveFile(source, destination))
                {
                    return;
                }
            }

            this.FileSystem.MoveFile(sourceLineNumbers, source, destination);
        }

        private void AclReset(IEnumerable<string> files)
        {
            var aclReset = new FileSecurity();
            aclReset.SetAccessRuleProtection(false, false);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                this.FileSystem.ExecuteWithRetries(() => fileInfo.SetAccessControl(aclReset));
            }
        }
    }
}
