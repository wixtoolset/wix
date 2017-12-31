// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolver for the WiX toolset.
    /// </summary>
    public sealed class Resolver
    {
        public Resolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        private IServiceProvider ServiceProvider { get; set; }

        public IEnumerable<BindPath> BindPaths { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public IEnumerable<Localization> Localizations { get; set; }

        public ResolveResult Execute()
        {
            var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

            var context = this.ServiceProvider.GetService<IResolveContext>();
            context.Messaging = this.ServiceProvider.GetService<IMessaging>();
            context.BindPaths = this.BindPaths;
            context.Extensions = extensionManager.Create<IResolverExtension>();
            context.ExtensionData = extensionManager.Create<IExtensionData>();
            context.IntermediateFolder = this.IntermediateFolder;
            context.IntermediateRepresentation = this.IntermediateRepresentation;
            context.Localizations = this.Localizations;
            context.VariableResolver = new WixVariableResolver(context.Messaging);

            foreach (IResolverExtension extension in context.Extensions)
            {
                extension.PreResolve(context);
            }

            ResolveResult resolveResult = null;
            try
            {
                PopulateVariableResolver(context);

                this.LocalizeUI(context);

                resolveResult = this.Resolve(context);
            }
            finally
            {
                foreach (IResolverExtension extension in context.Extensions)
                {
                    extension.PostResolve(resolveResult);
                }
            }

            return resolveResult;
        }

        private ResolveResult Resolve(IResolveContext context)
        {
            var buildingPatch = context.IntermediateRepresentation.Sections.Any(s => s.Type == SectionType.Patch);

            var filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IEnumerable<DelayedField> delayedFields;
            {
                var command = new ResolveFieldsCommand();
                command.Messaging = context.Messaging;
                command.BuildingPatch = buildingPatch;
                command.VariableResolver = context.VariableResolver;
                command.BindPaths = context.BindPaths;
                command.Extensions = context.Extensions;
                command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                command.IntermediateFolder = context.IntermediateFolder;
                command.Intermediate = context.IntermediateRepresentation;
                command.SupportDelayedResolution = true;
                command.Execute();

                delayedFields = command.DelayedFields;
            }

#if REVISIT_FOR_PATCHING
            if (context.IntermediateRepresentation.SubStorages != null)
            {
                foreach (SubStorage transform in context.IntermediateRepresentation.SubStorages)
                {
                    var command = new ResolveFieldsCommand();
                    command.BuildingPatch = buildingPatch;
                    command.BindVariableResolver = context.WixVariableResolver;
                    command.BindPaths = context.BindPaths;
                    command.Extensions = context.Extensions;
                    command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                    command.IntermediateFolder = context.IntermediateFolder;
                    command.Intermediate = context.IntermediateRepresentation;
                    command.SupportDelayedResolution = false;
                    command.Execute();
                }
            }
#endif

            var expectedEmbeddedFiles = filesWithEmbeddedFiles.GetExpectedEmbeddedFiles();

            return new ResolveResult
            {
                Codepage = context.VariableResolver.Codepage,
                ExpectedEmbeddedFiles = expectedEmbeddedFiles,
                DelayedFields = delayedFields,
                IntermediateRepresentation = context.IntermediateRepresentation
            };
        }

        /// <summary>
        /// Localize dialogs and controls.
        /// </summary>
        private void LocalizeUI(IResolveContext context)
        {
            foreach (var section in context.IntermediateRepresentation.Sections)
            {
                foreach (var row in section.Tuples.OfType<DialogTuple>())
                {
                    string dialog = row.Dialog;

                    if (context.VariableResolver.TryGetLocalizedControl(dialog, null, out LocalizedControl localizedControl))
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row.HCentering = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row.VCentering = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row.Width = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row.Height = localizedControl.Height;
                        }

                        row.Attributes = row.Attributes | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row.Title = localizedControl.Text;
                        }
                    }
                }

                foreach (var row in section.Tuples.OfType<ControlTuple>())
                {
                    string dialog = row.Dialog_;
                    string control = row.Control;

                    if (context.VariableResolver.TryGetLocalizedControl(dialog, control, out LocalizedControl localizedControl))
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row.X = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row.Y = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row.Width = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row.Height = localizedControl.Height;
                        }

                        row.Attributes = row.Attributes | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row.Text = localizedControl.Text;
                        }
                    }
                }
            }
        }

        private static void PopulateVariableResolver(IResolveContext context)
        {
            var creator = context.ServiceProvider.GetService<ITupleDefinitionCreator>();

            var localizations = context.Localizations.Concat(context.IntermediateRepresentation.Localizations).ToList();

            // Add localizations from the extensions with data.
            foreach (var data in context.ExtensionData)
            {
                var library = data.GetLibrary(creator);

                if (library?.Localizations != null)
                {
                    localizations.AddRange(library.Localizations);
                }
            }

            foreach (var localization in localizations)
            {
                context.VariableResolver.AddLocalization(localization);
            }

            // Gather all the wix variables.
            var wixVariableTuples = context.IntermediateRepresentation.Sections.SelectMany(s => s.Tuples).OfType<WixVariableTuple>();
            foreach (var tuple in wixVariableTuples)
            {
                context.VariableResolver.AddVariable(tuple.SourceLineNumbers, tuple.WixVariable, tuple.Value, tuple.Overridable);
            }
        }
    }
}
