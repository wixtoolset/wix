// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Resources;

    public static class VerboseMessages
    {
        public static Message BinderTempDirLocatedAt(string directory)
        {
            return Message(null, Ids.BinderTempDirLocatedAt, "Binder temporary directory located at '{0}'.", directory);
        }

        public static Message BundleGuid(string bundleGuid)
        {
            return Message(null, Ids.BundleGuid, "Assigning bundle GUID '{0}'.", bundleGuid);
        }

        public static Message CabFile(string fileId, string filePath)
        {
            return Message(null, Ids.CabFile, "Cabbing file {0} from '{1}'.", fileId, filePath);
        }

        public static Message CabinetsSplitInParallel()
        {
            return Message(null, Ids.CabinetsSplitInParallel, "Multiple Cabinets with Large Files are splitting simultaneously. This current cabinet is waiting on a shared resource and splitting will resume when the other splitting has completed.");
        }

        public static Message ConnectingMergeModule(string modulePath, string feature)
        {
            return Message(null, Ids.ConnectingMergeModule, "Connecting merge module '{0}' to feature '{1}'.", modulePath, feature);
        }

        public static Message CopyFile(string sourceFile, string destinationFile)
        {
            return Message(null, Ids.CopyFile, "Copying file '{0}' to '{1}'.", sourceFile, destinationFile);
        }

        public static Message CopyingExternalPayload(string payload, string outputDirectory)
        {
            return Message(null, Ids.CopyingExternalPayload, "Copying external payload from '{0}' to '{1}'.", payload, outputDirectory);
        }

        public static Message CreateCabinet(string cabinet)
        {
            return Message(null, Ids.CreateCabinet, "Creating cabinet '{0}'.", cabinet);
        }

        public static Message CreateDirectory(string directory)
        {
            return Message(null, Ids.CreateDirectory, "The directory '{0}' does not exist, creating it now.", directory);
        }

        public static Message CreatingCabinetFiles()
        {
            return Message(null, Ids.CreatingCabinetFiles, "Creating cabinet files.");
        }

        public static Message DecompilingTable(string tableName)
        {
            return Message(null, Ids.DecompilingTable, "Decompiling the {0} table.", tableName);
        }

        public static Message EmbeddingContainer(string container, long size, string compression)
        {
            return Message(null, Ids.EmbeddingContainer, "Embedding container '{0}' ({1} bytes) with '{2}' compression.", container, size, compression);
        }

        public static Message GeneratingBundle(string bundleFile, string stubFile)
        {
            return Message(null, Ids.GeneratingBundle, "Generating Burn bundle '{0}' from stub '{1}'.", bundleFile, stubFile);
        }

        public static Message GeneratingDatabase()
        {
            return Message(null, Ids.GeneratingDatabase, "Generating database.");
        }

        public static Message ImportBinaryStream(string streamSource)
        {
            return Message(null, Ids.ImportBinaryStream, "Importing binary stream from '{0}'.", streamSource);
        }

        public static Message ImportIconStream(string streamSource)
        {
            return Message(null, Ids.ImportIconStream, "Importing icon stream from '{0}'.", streamSource);
        }

        public static Message ImportingStreams()
        {
            return Message(null, Ids.ImportingStreams, "Importing streams.");
        }

        public static Message LayingOutMedia()
        {
            return Message(null, Ids.LayingOutMedia, "Laying out media.");
        }

        public static Message LoadingPayload(string payload)
        {
            return Message(null, Ids.LoadingPayload, "Loading payload '{0}' into container.", payload);
        }

        public static Message MergingMergeModule(string modulePath)
        {
            return Message(null, Ids.MergingMergeModule, "Merging merge module '{0}'.", modulePath);
        }

        public static Message MergingModules()
        {
            return Message(null, Ids.MergingModules, "Merging modules.");
        }

        public static Message MoveFile(string sourceFile, string destinationFile)
        {
            return Message(null, Ids.MoveFile, "Moving file '{0}' to '{1}'.", sourceFile, destinationFile);
        }

        public static Message OpeningMergeModule(string modulePath, Int16 language)
        {
            return Message(null, Ids.OpeningMergeModule, "Opening merge module '{0}' with language '{1}'.", modulePath, language);
        }

        public static Message RemoveDestinationFile(string destinationFile)
        {
            return Message(null, Ids.RemoveDestinationFile, "The destination file '{0}' already exists, attempting to remove it.", destinationFile);
        }

        public static Message ResequencingMergeModuleFiles()
        {
            return Message(null, Ids.ResequencingMergeModuleFiles, "Resequencing files from all merge modules.");
        }

        public static Message ResolvingManifest(string manifestFile)
        {
            return Message(null, Ids.ResolvingManifest, "Generating resolved manifest '{0}'.", manifestFile);
        }

        public static Message ReusingCabCache(SourceLineNumber sourceLineNumbers, string cabinetName, string source)
        {
            return Message(sourceLineNumbers, Ids.ReusingCabCache, "Reusing cabinet '{0}' from cabinet cache path: '{1}'.", cabinetName, source);
        }

        public static Message SetCabbingThreadCount(string threads)
        {
            return Message(null, Ids.SetCabbingThreadCount, "There will be '{0}' threads used to produce CAB files.", threads);
        }

        public static Message SwitchingToPerUserPackage(SourceLineNumber sourceLineNumbers, string path)
        {
            return Message(sourceLineNumbers, Ids.SwitchingToPerUserPackage, "Bundle switching from per-machine to per-user due to addition of per-user package '{0}'.", path);
        }

        public static Message UpdatingFileInformation()
        {
            return Message(null, Ids.UpdatingFileInformation, "Updating file information.");
        }

        public static Message ValidatedDatabase(long size)
        {
            return Message(null, Ids.ValidatedDatabase, "Validation complete: {0:N0}ms elapsed.", size);
        }

        public static Message ValidatingDatabase()
        {
            return Message(null, Ids.ValidatingDatabase, "Validating database.");
        }

        public static Message ValidationInfo(string ice, string message)
        {
            return Message(null, Ids.ValidationInfo, "{0}: {1}", ice, message);
        }

        public static Message ValidationSerialized()
        {
            return Message(null, Ids.ValidationSerialized, "Multiple packages cannot reliably be validated simultaneously. This validation will resume when the other package being validated has completed.");
        }

        public static Message ValidatorTempDirLocatedAt(string directory)
        {
            return Message(null, Ids.ValidatorTempDirLocatedAt, "Validator temporary directory located at '{0}'.", directory);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Verbose, (int)id, format, args);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Verbose, (int)id, resourceManager, resourceName, args);
        }

        public enum Ids
        {
            ImportBinaryStream = 9000,
            ImportIconStream = 9001,
            CopyFile = 9002,
            MoveFile = 9003,
            CreateDirectory = 9004,
            RemoveDestinationFile = 9005,
            CabFile = 9006,
            UpdatingFileInformation = 9007,
            GeneratingDatabase = 9008,
            MergingModules = 9009,
            CreatingCabinetFiles = 9010,
            ImportingStreams = 9011,
            LayingOutMedia = 9012,
            DecompilingTable = 9013,
            ValidationInfo = 9014,
            CreateCabinet = 9015,
            ValidatingDatabase = 9016,
            OpeningMergeModule = 9017,
            MergingMergeModule = 9018,
            ConnectingMergeModule = 9019,
            ResequencingMergeModuleFiles = 9020,
            BinderTempDirLocatedAt = 9021,
            ValidatorTempDirLocatedAt = 9022,
            GeneratingBundle = 9023,
            ResolvingManifest = 9024,
            LoadingPayload = 9025,
            BundleGuid = 9026,
            CopyingExternalPayload = 9027,
            EmbeddingContainer = 9028,
            SwitchingToPerUserPackage = 9029,
            SetCabbingThreadCount = 9030,
            ValidationSerialized = 9031,
            ReusingCabCache = 9032,
            CabinetsSplitInParallel = 9033,
            ValidatedDatabase = 9034,
        }
    }
}
