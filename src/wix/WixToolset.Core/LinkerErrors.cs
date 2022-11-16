// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class LinkerErrors
    {
        public static Message DuplicateBindPathVariableOnCommandLine(string argument, string bindName, string bindValue, string collisionValue)
        {
            return Message(null, Ids.DuplicateBindPathVariableOnCommandLine, "", argument, bindName, bindValue, collisionValue);
        }

        public static Message OrphanedPayload(SourceLineNumber sourceLineNumbers, string payloadId)
        {
            return Message(sourceLineNumbers, Ids.OrphanedPayload, "Found orphaned Payload '{0}'. Make sure to reference it from a Package, the BootstrapperApplication, or the Bundle or move it into its own Fragment so it only gets linked in when actually used.", payloadId);
        }

        public static Message PackageInMultipleContainers(SourceLineNumber sourceLineNumbers, string packageId, string containerId1, string containerId2)
        {
            return Message(sourceLineNumbers, Ids.PackageInMultipleContainers, "The Package '{0}' is referenced from multiple containers - Container '{1}' and Container '{2}'. This is not currently supported.", packageId, containerId1, containerId2);
        }

        public static Message PayloadSharedWithBA(SourceLineNumber sourceLineNumbers, string payloadId)
        {
            return Message(sourceLineNumbers, Ids.PayloadSharedWithBA, "The Payload '{0}' is shared with the BootstrapperApplication. This is not currently supported.", payloadId);
        }

        public static Message UnscheduledChainPackage(SourceLineNumber sourceLineNumbers, string packageId)
        {
            return Message(sourceLineNumbers, Ids.UnscheduledChainPackage, "Found orphaned Package '{0}'. Make sure to reference it from the Chain or move it into its own Fragment so it only gets linked in when actually used.", packageId);
        }

        public static Message UnscheduledRollbackBoundary(SourceLineNumber sourceLineNumbers, string rollbackBoundaryId)
        {
            return Message(sourceLineNumbers, Ids.UnscheduledRollbackBoundary, "Found orphaned RollbackBoundary '{0}'. Make sure to reference it from the Chain or move it into its own Fragment so it only gets linked in when actually used.", rollbackBoundaryId);
        }

        public static Message BAContainerCannotContainRemotePayload(SourceLineNumber sourceLineNumbers, string payloadName)
        {
            return Message(sourceLineNumbers, Ids.BAContainerCannotContainRemotePayload, "Bootstrapper application and bundle extension payloads must be embedded in the bundle. The payload '{0}' is remote thus cannot be found for embedding. Provide a full path to the payload via the Payload/@SourceFile attribute.", payloadName);
        }

        public static Message UncompressedPayloadInContainer(SourceLineNumber sourceLineNumbers, string payloadId, string containerId)
        {
            return Message(sourceLineNumbers, Ids.UncompressedPayloadInContainer, "The payload '{0}' is uncompressed and cannot be added to container '{1}'. Remove its Compressed attribute and provide a @SourceFile value to allow it to be added to a container.", payloadId, containerId);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            OrphanedPayload = 7000,
            PackageInMultipleContainers = 7001,
            PayloadSharedWithBA = 7002,
            UnscheduledChainPackage = 7003,
            UnscheduledRollbackBoundary = 7004,
            UncompressedPayloadInContainer = 7005,
            BAContainerCannotContainRemotePayload = 7006,
            DuplicateBindPathVariableOnCommandLine = 7007,
        } // last available is 7099. 7100 is WindowsInstallerBackendWarnings.
    }
}
