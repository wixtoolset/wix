// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Core.Bind;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
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

            this.FileResolver = serviceProvider.GetService<IFileResolver>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IFileResolver FileResolver { get; }

        public IResolveResult Resolve(IResolveContext context)
        {
            foreach (var extension in context.Extensions)
            {
                extension.PreResolve(context);
            }

            ResolveResult resolveResult = null;
            try
            {
                var filteredLocalizations = FilterLocalizations(context);

                var variableResolver = this.CreateVariableResolver(context, filteredLocalizations);

                this.LocalizeUI(variableResolver, context.IntermediateRepresentation);

                resolveResult = this.ResolveFields(context, variableResolver);

                var primaryLocalization = filteredLocalizations.FirstOrDefault();

                if (primaryLocalization != null)
                {
                    this.TryGetCultureInfo(primaryLocalization.Culture, out var cultureInfo);

                    resolveResult.Codepage = primaryLocalization.Codepage ?? cultureInfo?.TextInfo.ANSICodePage;

                    resolveResult.SummaryInformationCodepage = primaryLocalization.SummaryInformationCodepage ?? primaryLocalization.Codepage ?? cultureInfo?.TextInfo.ANSICodePage;

                    resolveResult.PackageLcid = cultureInfo?.LCID;
                }
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

        private ResolveResult ResolveFields(IResolveContext context, IVariableResolver variableResolver)
        {
            var filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IReadOnlyCollection<DelayedField> delayedFields;
            {
                var command = new ResolveFieldsCommand(this.Messaging, this.FileResolver, variableResolver, context.BindPaths, context.Extensions, filesWithEmbeddedFiles, context.IntermediateFolder, context.IntermediateRepresentation, context.AllowUnresolvedVariables);
                command.Execute();

                delayedFields = command.DelayedFields;
            }

            var expectedEmbeddedFiles = filesWithEmbeddedFiles.GetExpectedEmbeddedFiles();

            context.IntermediateRepresentation.UpdateLevel(IntermediateLevels.Resolved);

            return new ResolveResult
            {
                ExpectedEmbeddedFiles = expectedEmbeddedFiles,
                DelayedFields = delayedFields,
                IntermediateRepresentation = context.IntermediateRepresentation
            };
        }

        /// <summary>
        /// Localize dialogs and controls.
        /// </summary>
        private void LocalizeUI(IVariableResolver variableResolver, Intermediate intermediate)
        {
            foreach (var section in intermediate.Sections)
            {
                foreach (var symbol in section.Symbols.OfType<DialogSymbol>())
                {
                    if (variableResolver.TryGetLocalizedControl(symbol.Id.Id, null, out var localizedControl))
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            symbol.HCentering = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            symbol.VCentering = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            symbol.Width = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            symbol.Height = localizedControl.Height;
                        }

                        symbol.RightAligned |= localizedControl.RightAligned;
                        symbol.RightToLeft |= localizedControl.RightToLeft;
                        symbol.LeftScroll |= localizedControl.LeftScroll;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            symbol.Title = localizedControl.Text;
                        }
                    }
                }

                foreach (var symbol in section.Symbols.OfType<ControlSymbol>())
                {
                    if (variableResolver.TryGetLocalizedControl(symbol.DialogRef, symbol.Control, out var localizedControl))
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            symbol.X = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            symbol.Y = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            symbol.Width = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            symbol.Height = localizedControl.Height;
                        }

                        symbol.RightAligned |= localizedControl.RightAligned;
                        symbol.RightToLeft |= localizedControl.RightToLeft;
                        symbol.LeftScroll |= localizedControl.LeftScroll;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            symbol.Text = localizedControl.Text;
                        }
                    }
                }
            }
        }

        private IVariableResolver CreateVariableResolver(IResolveContext context, IEnumerable<Localization> filteredLocalizations)
        {
            var variableResolver = this.ServiceProvider.GetService<IVariableResolver>();

            foreach (var localization in filteredLocalizations)
            {
                variableResolver.AddLocalization(localization);
            }

            foreach (var bindVariable in context.BindVariables)
            {
                variableResolver.AddVariable(null, bindVariable.Key, bindVariable.Value, false);
            }

            // Gather all the wix variables.
            var wixVariableSymbols = context.IntermediateRepresentation.Sections.SelectMany(s => s.Symbols).OfType<WixVariableSymbol>();
            foreach (var symbol in wixVariableSymbols)
            {
                variableResolver.AddVariable(symbol.SourceLineNumbers, symbol.Id.Id, symbol.Value, symbol.Overridable);
            }

            return variableResolver;
        }

        private bool TryGetCultureInfo(string culture, out CultureInfo cultureInfo)
        {
            cultureInfo = null;

            if (!String.IsNullOrEmpty(culture))
            {
                try
                {
                    cultureInfo = new CultureInfo(culture, useUserOverride: false);
                }
                catch
                {
                    this.Messaging.Write("");
                }
            }

            return cultureInfo != null;
        }

        private static IEnumerable<Localization> FilterLocalizations(IResolveContext context)
        {
            var result = new List<Localization>();
            var filter = CalculateCultureFilter(context);

            var localizations = context.Localizations.Concat(context.IntermediateRepresentation.Localizations).ToList();

            AddFilteredLocalizations(result, filter, localizations);

            // Filter localizations provided by extensions with data.
            var creator = context.ServiceProvider.GetService<ISymbolDefinitionCreator>();

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
