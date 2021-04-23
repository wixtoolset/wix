// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class LinkerErrors
    {
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
        } // last available is 7099. 7100 is WindowsInstallerBackendWarnings.
    }
}
