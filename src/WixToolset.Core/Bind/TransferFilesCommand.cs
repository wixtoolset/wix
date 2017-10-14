// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.AccessControl;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;

    internal class TransferFilesCommand
    {
        public TransferFilesCommand(IEnumerable<BindPath> bindPaths, IEnumerable<IBinderExtension> extensions, IEnumerable<FileTransfer> fileTransfers, bool suppressAclReset)
        {
            this.FileResolver = new FileResolver(bindPaths, extensions);
            this.FileTransfers = fileTransfers;
            this.SuppressAclReset = suppressAclReset;
        }

        private FileResolver FileResolver { get; }

        private IEnumerable<FileTransfer> FileTransfers { get; }

        private bool SuppressAclReset { get; }

        public void Execute()
        {
            List<string> destinationFiles = new List<string>();

            foreach (var fileTransfer in this.FileTransfers)
            {
                string fileSource = this.FileResolver.ResolveFile(fileTransfer.Source, fileTransfer.Type, fileTransfer.SourceLineNumbers, BindStage.Normal);

                // If the source and destination are identical, then there's nothing to do here
                if (0 == String.Compare(fileSource, fileTransfer.Destination, StringComparison.OrdinalIgnoreCase))
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
                            Messaging.Instance.OnMessage(WixVerboses.MoveFile(fileSource, fileTransfer.Destination));
                            this.TransferFile(true, fileSource, fileTransfer.Destination);
                        }
                        else
                        {
                            Messaging.Instance.OnMessage(WixVerboses.CopyFile(fileSource, fileTransfer.Destination));
                            this.TransferFile(false, fileSource, fileTransfer.Destination);
                        }

                        retry = false;
                        destinationFiles.Add(fileTransfer.Destination);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new WixFileNotFoundException(e.FileName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        string directory = Path.GetDirectoryName(fileTransfer.Destination);
                        Messaging.Instance.OnMessage(WixVerboses.CreateDirectory(directory));
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
                            Messaging.Instance.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // try to ensure the file is not read-only
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            try
                            {
                                File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            }
                            catch (ArgumentException) // thrown for unauthorized access errors
                            {
                                throw new WixException(WixErrors.UnauthorizedAccess(fileTransfer.Destination));
                            }

                            // try to delete the file
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
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
                            Messaging.Instance.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // ensure the file is not read-only, then delete it
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
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
                    Messaging.Instance.OnMessage(WixWarnings.UnableToResetAcls());
                }
            }
        }

        private void TransferFile(bool move, string source, string destination)
        {
            bool complete = false;

            if (move)
            {
                complete = this.FileResolver.MoveFile(source, destination, true);
            }
            else
            {
                complete = this.FileResolver.CopyFile(source, destination, true);
            }

            if (!complete)
            {
                throw new InvalidOperationException(); // TODO: something needs to be said here that none of the binder file managers returned a result.
            }
        }
    }
}
