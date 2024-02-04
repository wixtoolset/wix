// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    internal static class LinkerErrors
    {
        public static Message DuplicateBindPathVariableOnCommandLine(string argument, string bindName, string bindValue, string collisionValue)
        {
            return Message(null, Ids.DuplicateBindPathVariableOnCommandLine, "", argument, bindName, bindValue, collisionValue);
        }

        public static Message DuplicateSymbol(IntermediateSymbol symbol)
        {
            return Message(symbol.SourceLineNumbers, Ids.DuplicateSymbol, "Duplicate {0} with identifier '{1}' found. Access modifiers (global, library, file, section) cannot prevent these conflicts. Ensure all your identifiers of a given type (Directory, File, etc.) are unique.", symbol.Definition.Name, symbol.Id.Id);
        }

        public static Message DuplicateSymbol(IntermediateSymbol symbol, SourceLineNumber referencingSourceLineNumber)
        {
            if (referencingSourceLineNumber is null)
            {
                return DuplicateSymbol(symbol);
            }

            return Message(symbol.SourceLineNumbers, Ids.DuplicateSymbol, "Duplicate {0} with identifier '{1}' referenced by {2}. Ensure all your identifiers of a given type (Directory, File, etc.) are unique or use an access modifier to scope the identfier.", symbol.Definition.Name, symbol.Id.Id, referencingSourceLineNumber);
        }

        public static Message DuplicateVirtualSymbol(IntermediateSymbol symbol)
        {
            return Message(symbol.SourceLineNumbers, Ids.DuplicateSymbol, "The virtual {0} with identifier '{1}' is duplicated. Ensure identifiers of a given type (Directory, File, etc.) are unique or did you mean to make one an override for the virtual symbol?", symbol.Definition.Name, symbol.Id.Id);
        }

        public static Message DuplicateVirtualSymbol(IntermediateSymbol symbol, SourceLineNumber referencingSourceLineNumber)
        {
            if (referencingSourceLineNumber is null)
            {
                return DuplicateVirtualSymbol(symbol);
            }

            return Message(symbol.SourceLineNumbers, Ids.DuplicateSymbol, "The virtual {0} with identifier '{1}' is duplicated. Ensure identifiers of a given type (Directory, File, etc.) are unique or did you mean to make one an override for the virtual symbol? Referenced from {2}", symbol.Definition.Name, symbol.Id.Id, referencingSourceLineNumber);
        }

        public static Message DuplicateSymbol2(IntermediateSymbol symbol)
        {
            return Message(symbol.SourceLineNumbers, Ids.DuplicateSymbol2, "Location of symbol related to previous error.");
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

        public static Message VirtualSymbolNotFoundForOverride(IntermediateSymbol symbol)
        {
            return Message(symbol.SourceLineNumbers, Ids.VirtualSymbolNotFoundForOverride, "Could not find a virtual symbol to override with the {0} symbol '{1}'. Remove the override access modifier or include the code with the virtual symbol.", symbol.Definition.Name, symbol.Id.Id);
        }

        public static Message VirtualSymbolNotFoundForOverride(IntermediateSymbol symbol, SourceLineNumber referencingSourceLineNumber)
        {
            if (referencingSourceLineNumber is null)
            {
                return VirtualSymbolNotFoundForOverride(symbol);
            }

            return Message(symbol.SourceLineNumbers, Ids.VirtualSymbolNotFoundForOverride, "Could not find a virtual symbol to override with the {0} symbol '{1}'. Remove the override access modifier or include the code with the virtual symbol. Referenced from {2}", symbol.Definition.Name, symbol.Id.Id, referencingSourceLineNumber);
        }

        public static Message VirtualSymbolMustBeOverridden(IntermediateSymbol symbol)
        {
            return Message(symbol.SourceLineNumbers, Ids.VirtualSymbolMustBeOverridden, "The {0} symbol '{1}' conflicts with a virtual symbol. Use the 'override' access modifier to override the virtual symbol or use a different Id to avoid the conflict.", symbol.Definition.Name, symbol.Id.Id);
        }

        public static Message VirtualSymbolMustBeOverridden(WixActionSymbol actionSymbol)
        {
            return Message(actionSymbol.SourceLineNumbers, Ids.VirtualSymbolMustBeOverridden, "The action '{0}' conflicts with a virtual symbol with the same id. To override the virtual symbol (e.g., to reschedule a custom action), use the 'override' access modifier: 'override {0}'. If you didn't intend to override a virtual symbol, use a different id to avoid the conflict.", actionSymbol.Action);
        }

        public static Message VirtualSymbolMustBeOverridden(IntermediateSymbol symbol, SourceLineNumber referencingSourceLineNumber)
        {
            if (referencingSourceLineNumber is null)
            {
                return VirtualSymbolMustBeOverridden(symbol);
            }

            return Message(symbol.SourceLineNumbers, Ids.VirtualSymbolMustBeOverridden, "The {0} symbol '{1}' conflicts with a virtual symbol. Use the 'override' access modifier to override the virtual symbol or use a different Id to avoid the conflict. Referenced from {2}", symbol.Definition.Name, symbol.Id.Id, referencingSourceLineNumber);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            DuplicateSymbol = 91,
            DuplicateSymbol2 = 92,

            OrphanedPayload = 7000,
            PackageInMultipleContainers = 7001,
            PayloadSharedWithBA = 7002,
            UnscheduledChainPackage = 7003,
            UnscheduledRollbackBoundary = 7004,
            UncompressedPayloadInContainer = 7005,
            BAContainerCannotContainRemotePayload = 7006,
            DuplicateBindPathVariableOnCommandLine = 7007,
            VirtualSymbolNotFoundForOverride = 7008,
            VirtualSymbolMustBeOverridden = 7009,
        } // last available is 7099. 7100 is WindowsInstallerBackendWarnings.
    }
}
