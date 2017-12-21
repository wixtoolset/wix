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
        public Resolver(IServiceProvider serviceProvider, IEnumerable<BindPath> bindPaths, Intermediate intermediateRepresentation, string intermediateFolder, IEnumerable<Localization> localizations)
        {
            this.ServiceProvider = serviceProvider;
            this.BindPaths = bindPaths;
            this.IntermediateRepresentation = intermediateRepresentation;
            this.IntermediateFolder = intermediateFolder;
            this.Localizations = localizations;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IEnumerable<BindPath> BindPaths { get; }

        private Intermediate IntermediateRepresentation { get; }

        private string IntermediateFolder { get; }

        private IEnumerable<Localization> Localizations { get; }

        private  IMessaging Messaging { get; }

        public ResolveResult Execute()
        {
            var localizer = new Localizer(this.Messaging, this.Localizations);

            var variableResolver = new WixVariableResolver(this.Messaging, localizer);

            var context = this.ServiceProvider.GetService<IResolveContext>();
            context.Messaging = this.Messaging;
            context.BindPaths = this.BindPaths;
            context.Extensions = this.ServiceProvider.GetService<IExtensionManager>().Create<IResolverExtension>();
            context.IntermediateFolder = this.IntermediateFolder;
            context.IntermediateRepresentation = this.IntermediateRepresentation;
            context.WixVariableResolver = this.PopulateVariableResolver(variableResolver);

            // Preresolve.
            //
            foreach (IResolverExtension extension in context.Extensions)
            {
                extension.PreResolve(context);
            }

            // Resolve.
            //
            this.LocalizeUI(context);

            var resolveResult = this.Resolve(localizer.Codepage, context);

            if (resolveResult != null)
            {
                // Postresolve.
                //
                foreach (IResolverExtension extension in context.Extensions)
                {
                    extension.PostResolve(resolveResult);
                }
            }

            return resolveResult;
        }

        private ResolveResult Resolve(int codepage, IResolveContext context)
        {
            var buildingPatch = context.IntermediateRepresentation.Sections.Any(s => s.Type == SectionType.Patch);

            var filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IEnumerable<DelayedField> delayedFields;
            {
                var command = new ResolveFieldsCommand();
                command.Messaging = context.Messaging;
                command.BuildingPatch = buildingPatch;
                command.BindVariableResolver = context.WixVariableResolver;
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
                Codepage = codepage,
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

                    if (context.WixVariableResolver.TryGetLocalizedControl(dialog, null, out LocalizedControl localizedControl))
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

                    if (context.WixVariableResolver.TryGetLocalizedControl(dialog, control, out LocalizedControl localizedControl))
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

        private WixVariableResolver PopulateVariableResolver(WixVariableResolver resolver)
        {
            // Gather all the wix variables.
            var wixVariableTuples = this.IntermediateRepresentation.Sections.SelectMany(s => s.Tuples).OfType<WixVariableTuple>();
            foreach (var tuple in wixVariableTuples)
            {
                try
                {
                    resolver.AddVariable(tuple.WixVariable, tuple.Value, tuple.Overridable);
                }
                catch (ArgumentException)
                {
                    this.Messaging.Write(ErrorMessages.WixVariableCollision(tuple.SourceLineNumbers, tuple.WixVariable));
                }
            }

            return resolver;
        }
    }
}
