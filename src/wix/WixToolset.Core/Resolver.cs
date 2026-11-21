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
                var cultureGroups = context.FilterCultureGroups;
                var hasAdditionalCultures = cultureGroups != null && cultureGroups.Count > 1;
                var originalIntermediate = hasAdditionalCultures ? context.IntermediateRepresentation.Clone() : null;

                var primaryFilterCultures = (cultureGroups != null && cultureGroups.Count > 0) ? cultureGroups.First() : context.FilterCultures;
                resolveResult = this.ResolveCultureGroup(context, context.IntermediateRepresentation, primaryFilterCultures);

                if (hasAdditionalCultures && !this.Messaging.EncounteredError)
                {
                    var embeddedResults = new List<IResolveResult>();

                    foreach (var cultureGroup in cultureGroups.Skip(1))
                    {
                        var clonedIntermediate = originalIntermediate.Clone();
                        var embeddedResult = this.ResolveCultureGroup(context, clonedIntermediate, cultureGroup);

                        if (this.Messaging.EncounteredError)
                        {
                            break;
                        }

                        embeddedResults.Add(embeddedResult);
                    }

                    resolveResult.EmbeddedLanguageResolveResults = embeddedResults;
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

        private ResolveResult ResolveCultureGroup(IResolveContext context, Intermediate intermediate, IReadOnlyCollection<string> filterCultures)
        {
            var filteredLocalizations = FilterLocalizations(context, intermediate, filterCultures);

            var variableResolver = this.CreateVariableResolver(context, intermediate, filteredLocalizations);

            this.LocalizeUI(variableResolver, intermediate);

            var resolveResult = this.ResolveFields(context, intermediate, variableResolver);

            var primaryLocalization = filteredLocalizations.FirstOrDefault(l => !String.IsNullOrEmpty(l.Culture)) ?? filteredLocalizations.FirstOrDefault();

            if (primaryLocalization != null)
            {
                this.TryGetCultureInfo(primaryLocalization.Culture, out var cultureInfo);

                resolveResult.Codepage = primaryLocalization.Codepage ?? cultureInfo?.TextInfo.ANSICodePage;

                resolveResult.SummaryInformationCodepage = primaryLocalization.SummaryInformationCodepage ?? primaryLocalization.Codepage ?? cultureInfo?.TextInfo.ANSICodePage;

                resolveResult.PackageLcid = cultureInfo?.LCID;
            }

            return resolveResult;
        }

        private ResolveResult ResolveFields(IResolveContext context, Intermediate intermediate, IVariableResolver variableResolver)
        {
            var filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IReadOnlyCollection<DelayedField> delayedFields;
            {
                var command = new ResolveFieldsCommand(this.Messaging, this.FileResolver, variableResolver, context.BindPaths, context.Extensions, filesWithEmbeddedFiles, context.IntermediateFolder, intermediate, context.AllowUnresolvedVariables);
                command.Execute();

                delayedFields = command.DelayedFields;
            }

            var expectedEmbeddedFiles = filesWithEmbeddedFiles.GetExpectedEmbeddedFiles();

            intermediate.UpdateLevel(IntermediateLevels.Resolved);

            return new ResolveResult
            {
                ExpectedEmbeddedFiles = expectedEmbeddedFiles,
                DelayedFields = delayedFields,
                IntermediateRepresentation = intermediate
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

        private IVariableResolver CreateVariableResolver(IResolveContext context, Intermediate intermediate, IEnumerable<Localization> filteredLocalizations)
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
            var wixVariableSymbols = intermediate.Sections.SelectMany(s => s.Symbols).OfType<WixVariableSymbol>();
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

        private static IEnumerable<Localization> FilterLocalizations(IResolveContext context, Intermediate intermediate, IReadOnlyCollection<string> filterCultures)
        {
            var result = new List<Localization>();
            var filter = CalculateCultureFilter(context, filterCultures);

            var localizations = context.Localizations.Concat(intermediate.Localizations).ToList();

            AddFilteredLocalizations(result, filter, localizations);

            return result;
        }

        private static IEnumerable<string> CalculateCultureFilter(IResolveContext context, IReadOnlyCollection<string> filterCultures)
        {
            var filter = (filterCultures ?? Array.Empty<string>())
                .SelectMany(c => c == null ? Array.Empty<string>() : c.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(c => c.Trim())
                .Select(c => c.Equals("neutral", StringComparison.OrdinalIgnoreCase) ? String.Empty : c)
                .ToList();

            // If no filter was specified, look for a language neutral localization file specified
            // from the command-line (not embedded in the intermediate). If found, filter on language
            // neutral.
            if (!filter.Any() && context.Localizations.Any(l => String.IsNullOrEmpty(l.Culture)))
            {
                filter.Add(String.Empty);
            }

            return filter;
        }

        private static void AddFilteredLocalizations(List<Localization> result, IEnumerable<string> filter, IEnumerable<Localization> localizations)
        {
            // If there is no filter, return all localizations provided by the user (either as a .wxl or from a .wixlib on the command-line)
            // **and only** the extension's default culture localizations.
            if (!filter.Any())
            {
                // The filter turns out to be really simple, skip localizations that came from an extension (LocalizationLocation.Extension)
                // but keep those that are marked as the extension's default culture (LocalizationLocation.ExtensionDefaultCulture).
                var filtered = localizations.Where(l => l.Location != LocalizationLocation.Extension);

                result.AddRange(filtered);
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
