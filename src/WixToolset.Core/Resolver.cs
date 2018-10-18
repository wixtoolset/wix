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
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolver for the WiX toolset.
    /// </summary>
    internal class Resolver : IResolver
    {
        internal Resolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public IEnumerable<BindPath> BindPaths { get; set; }

        public string IntermediateFolder { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }

        public IEnumerable<Localization> Localizations { get; set; }

        public IEnumerable<string> FilterCultures { get; set; }

        public ResolveResult Resolve(IResolveContext context)
        {

            foreach (var extension in context.Extensions)
            {
                extension.PreResolve(context);
            }

            ResolveResult resolveResult = null;
            try
            {
                PopulateVariableResolver(context);

                this.LocalizeUI(context);

                resolveResult = this.DoResolve(context);
            }
            finally
            {
                foreach (var extension in context.Extensions)
                {
                    extension.PostResolve(resolveResult);
                }
            }

            return resolveResult;
        }

        private ResolveResult DoResolve(IResolveContext context)
        {
            var buildingPatch = context.IntermediateRepresentation.Sections.Any(s => s.Type == SectionType.Patch);

            var filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IEnumerable<DelayedField> delayedFields;
            {
                var command = new ResolveFieldsCommand();
                command.Messaging = this.Messaging;
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

            var localizations = FilterLocalizations(context);

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

        private static IEnumerable<Localization> FilterLocalizations(IResolveContext context)
        {
            var result = new List<Localization>();
            var filter = CalculateCultureFilter(context);

            var localizations = context.Localizations.Concat(context.IntermediateRepresentation.Localizations).ToList();

            AddFilteredLocalizations(result, filter, localizations);

            // Filter localizations provided by extensions with data.
            var creator = context.ServiceProvider.GetService<ITupleDefinitionCreator>();

            foreach (var data in context.ExtensionData)
            {
                var library = data.GetLibrary(creator);

                if (library?.Localizations != null && library.Localizations.Any())
                {
                    var extensionFilter = (!filter.Any() && data.DefaultCulture != null) ? new[] { data.DefaultCulture } : filter;

                    AddFilteredLocalizations(result, extensionFilter, library.Localizations);
                }
            }

            return result;
        }

        private static IEnumerable<string> CalculateCultureFilter(IResolveContext context)
        {
            var filter = context.FilterCultures ?? Array.Empty<string>();

            // If no filter was specified, look for a language neutral localization file specified
            // from the command-line (not embedded in the intermediate). If found, filter on language
            // neutral.
            if (!filter.Any() && context.Localizations.Any(l => String.IsNullOrEmpty(l.Culture)))
            {
                filter = new[] { String.Empty };
            }

            return filter;
        }

        private static void AddFilteredLocalizations(List<Localization> result, IEnumerable<string> filter, IEnumerable<Localization> localizations)
        {
            // If there is no filter, return all localizations.
            if (!filter.Any())
            {
                result.AddRange(localizations);
            }
            else // filter localizations in order specified by the filter
            {
                foreach (var culture in filter)
                {
                    result.AddRange(localizations.Where(l => culture.Equals(l.Culture, StringComparison.OrdinalIgnoreCase) || String.IsNullOrEmpty(l.Culture)));
                }
            }
        }
    }
}
