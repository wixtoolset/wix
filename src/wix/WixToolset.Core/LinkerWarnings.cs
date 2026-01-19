// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;

    internal static class LinkerWarnings
    {
        public static Message LayoutPayloadInContainer(SourceLineNumber sourceLineNumbers, string payloadId, string containerId)
        {
            return Message(sourceLineNumbers, Ids.LayoutPayloadInContainer, "The layout-only Payload '{0}' is being added to Container '{1}'. It will not be extracted during layout.", payloadId, containerId);
        }

        public static Message PayloadInMultipleContainers(SourceLineNumber sourceLineNumbers, string payloadId, string containerId1, string containerId2)
        {
            return Message(sourceLineNumbers, Ids.PayloadInMultipleContainers, "The Payload '{0}' can't be added to Container '{1}' because it was already added to Container '{2}'.", payloadId, containerId1, containerId2);
        }

        public static Message ImplicitComponentPrimaryFeature(string componentId)
        {
            return Message(null, Ids.ImplicitComponentPrimaryFeature, "The component '{0}' does not have an explicit primary feature parent specified. If the source files are linked in a different order, the primary parent feature may change. To prevent accidental changes, the primary feature parent should be set to 'yes' in one of the ComponentRef/@Primary, ComponentGroupRef/@Primary, or FeatureGroupRef/@Primary locations for this component.", componentId);
        }

        public static Message ImplicitMergeModulePrimaryFeature(string componentId)
        {
            return Message(null, Ids.ImplicitMergeModulePrimaryFeature, "The merge module '{0}' does not have an explicit primary feature parent specified. If the source files are linked in a different order, the primary parent feature may change. To prevent accidental changes, the primary feature parent should be set to 'yes' in one of the MergeRef/@Primary or FeatureGroupRef/@Primary locations for this component.", componentId);
        }

        public static Message UnexpectedEntrySection(SourceLineNumber sourceLineNumbers, string sectionType, string expectedType)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedEntrySection, "Found entry point <{0}> that does not match expected <{1}> output type. Verify that your source code is correct and matches the expected output type.", sectionType, expectedType);
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Warning, (int)id, format, args);
        }

        public enum Ids
        {
            ImplicitComponentPrimaryFeature = 1049,
            ImplicitMergeModulePrimaryFeature = 1084,
            UnexpectedEntrySection = 1109,
            LayoutPayloadInContainer = 6900,
            PayloadInMultipleContainers = 6901,
        } // last available is 6999. 7000 is LinkerErrors.
    }
}
