// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using SimpleJson;

    /// <summary>
    /// Container class for an intermediate object.
    /// </summary>
    public sealed class Intermediate
    {
        private static readonly Version CurrentVersion = new Version("4.0.0.0");
        private const string WixOutputStreamName = "wix-ir.json";

        private readonly Dictionary<string, Localization> localizationsByCulture;

        /// <summary>
        /// Instantiate a new Intermediate.
        /// </summary>
        public Intermediate()
        {
            this.Id = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            this.localizationsByCulture = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);
            this.Sections = new List<IntermediateSection>();
        }

        public Intermediate(string id, IEnumerable<IntermediateSection> sections, IDictionary<string, Localization> localizationsByCulture)
        {
            this.Id = id;
            this.localizationsByCulture = (localizationsByCulture != null) ? new Dictionary<string, Localization>(localizationsByCulture, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);
            this.Sections = (sections != null) ? new List<IntermediateSection>(sections) : new List<IntermediateSection>();
        }

        /// <summary>
        /// Get the id for the intermediate.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Get the localizations contained in this intermediate.
        /// </summary>
        public IEnumerable<Localization> Localizations => this.localizationsByCulture.Values;

        /// <summary>
        /// Get the sections contained in this intermediate.
        /// </summary>
        public IList<IntermediateSection> Sections { get; }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, bool suppressVersionCheck = false)
        {
            var creator = new SimpleTupleDefinitionCreator();
            return Intermediate.Load(path, creator, suppressVersionCheck);
        }

        /// <summary>
        /// Loads an intermediate from a stream.
        /// </summary>
        /// <param name="assembly">Assembly with intermediate embedded in resource stream.</param>
        /// <param name="resourceName">Name of resource stream.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(Assembly assembly, string resourceName, bool suppressVersionCheck = false)
        {
            var creator = new SimpleTupleDefinitionCreator();
            return Intermediate.Load(assembly, resourceName, creator, suppressVersionCheck);
        }

        /// <summary>
        /// Loads an intermediate from a stream.
        /// </summary>
        /// <param name="assembly">Assembly with intermediate embedded in resource stream.</param>
        /// <param name="resourceName">Name of resource stream.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(Assembly assembly, string resourceName, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            using (var wixout = WixOutput.Read(assembly, resourceName))
            {
                return Intermediate.LoadIntermediate(wixout, creator, suppressVersionCheck);
            }
        }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            using (var wixout = WixOutput.Read(path))
            {
                return Intermediate.LoadIntermediate(wixout, creator, suppressVersionCheck);
            }
        }

        /// <summary>
        /// Loads an intermediate from a WixOutput object.
        /// </summary>
        /// <param name="wixOutput">WixOutput object.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(WixOutput wixOutput, bool suppressVersionCheck = false)
        {
            var creator = new SimpleTupleDefinitionCreator();
            return Intermediate.LoadIntermediate(wixOutput, creator, suppressVersionCheck);
        }

        /// <summary>
        /// Loads an intermediate from a WixOutput object.
        /// </summary>
        /// <param name="wixOutput">WixOutput object.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(WixOutput wixOutput, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            return Intermediate.LoadIntermediate(wixOutput, creator, suppressVersionCheck);
        }

        /// <summary>
        /// Loads several intermediates from paths on disk using the same definitions.
        /// </summary>
        /// <param name="intermediateFiles">Paths to intermediate files saved on disk.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediates</returns>
        public static IEnumerable<Intermediate> Load(IEnumerable<string> intermediateFiles)
        {
            var creator = new SimpleTupleDefinitionCreator();
            return Intermediate.Load(intermediateFiles, creator);
        }

        /// <summary>
        /// Loads several intermediates from paths on disk using the same definitions.
        /// </summary>
        /// <param name="intermediateFiles">Paths to intermediate files saved on disk.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediates</returns>
        public static IEnumerable<Intermediate> Load(IEnumerable<string> intermediateFiles, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            var jsons = new Queue<JsonWithPath>();
            var intermediates = new List<Intermediate>();

            foreach (var path in intermediateFiles)
            {
                using (var wixout = WixOutput.Read(path))
                {
                    var data = wixout.GetData(WixOutputStreamName);
                    var json = Intermediate.LoadJson(data, wixout.Uri, suppressVersionCheck);

                    Intermediate.LoadDefinitions(json, creator);

                    jsons.Enqueue(new JsonWithPath { Json = json, Path = wixout.Uri });
                }
            }

            while (jsons.Count > 0)
            {
                var jsonWithPath = jsons.Dequeue();

                var intermediate = Intermediate.FinalizeLoad(jsonWithPath.Json, jsonWithPath.Path, creator);

                intermediates.Add(intermediate);
            }

            return intermediates;
        }

        /// <summary>
        /// Saves an intermediate to a path on disk.
        /// </summary>
        /// <param name="path">Path to save intermediate file to disk.</param>
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using (var wixout = WixOutput.Create(path))
            {
                this.Save(wixout);
            }
        }

        /// <summary>
        /// Saves an intermediate to a path on disk.
        /// </summary>
        /// <param name="path">Path to save intermediate file to disk.</param>
        public void Save(WixOutput wixout)
        {
            this.SaveEmbedFiles(wixout);

            this.SaveIR(wixout);
        }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="stream">Stream to intermediate file.</param>
        /// <param name="baseUri">Path name of intermediate file.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        private static Intermediate LoadIntermediate(WixOutput wixout, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            var data = wixout.GetData(WixOutputStreamName);
            var json = Intermediate.LoadJson(data, wixout.Uri, suppressVersionCheck);

            Intermediate.LoadDefinitions(json, creator);

            return Intermediate.FinalizeLoad(json, wixout.Uri, creator);
        }

        /// <summary>
        /// Loads json form of intermedaite from stream.
        /// </summary>
        /// <param name="stream">Stream to intermediate file.</param>
        /// <param name="baseUri">Path name of intermediate file.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded json.</returns>
        private static JsonObject LoadJson(string json, Uri baseUri, bool suppressVersionCheck)
        {
            var jsonObject = SimpleJson.DeserializeObject(json) as JsonObject;

            if (!suppressVersionCheck)
            {
                var versionJson = jsonObject.GetValueOrDefault<string>("version");

                if (!Version.TryParse(versionJson, out var version) || !Intermediate.CurrentVersion.Equals(version))
                {
                    throw new WixException(ErrorMessages.VersionMismatch(SourceLineNumber.CreateFromUri(baseUri.AbsoluteUri), "intermediate", versionJson, Intermediate.CurrentVersion.ToString()));
                }
            }

            return jsonObject;
        }

        /// <summary>
        /// Loads custom definitions in intermediate json into the creator.
        /// </summary>
        /// <param name="json">Json version of intermediate.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        private static void LoadDefinitions(JsonObject json, ITupleDefinitionCreator creator)
        {
            var definitionsJson = json.GetValueOrDefault<JsonArray>("definitions");

            if (definitionsJson != null)
            {
                foreach (JsonObject definitionJson in definitionsJson)
                {
                    var definition = IntermediateTupleDefinition.Deserialize(definitionJson);
                    creator.AddCustomTupleDefinition(definition);
                }
            }
        }

        /// <summary>
        /// Loads the sections and localization for the intermediate.
        /// </summary>
        /// <param name="json">Json version of intermediate.</param>
        /// <param name="baseUri">Path to the intermediate.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <returns>The finalized intermediate.</returns>
        private static Intermediate FinalizeLoad(JsonObject json, Uri baseUri, ITupleDefinitionCreator creator)
        {
            var id = json.GetValueOrDefault<string>("id");

            var sections = new List<IntermediateSection>();

            var sectionsJson = json.GetValueOrDefault<JsonArray>("sections");
            foreach (JsonObject sectionJson in sectionsJson)
            {
                var section = IntermediateSection.Deserialize(creator, baseUri, sectionJson);
                sections.Add(section);
            }

            var localizations = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);

            var localizationsJson = json.GetValueOrDefault<JsonArray>("localizations") ?? new JsonArray();
            foreach (JsonObject localizationJson in localizationsJson)
            {
                var localization = Localization.Deserialize(localizationJson);
                localizations.Add(localization.Culture, localization);
            }

            return new Intermediate(id, sections, localizations);
        }

        private void SaveEmbedFiles(WixOutput wixout)
        {
            var embeddedFields = this.Sections.SelectMany(s => s.Tuples)
                .SelectMany(t => t.Fields)
                .Where(f => f?.Type == IntermediateFieldType.Path)
                .Select(f => f.AsPath())
                .Where(f => f.Embed)
                .ToList();

            var savedEmbedFields = new Dictionary<string, IntermediateFieldPathValue>(StringComparer.OrdinalIgnoreCase);
            var uniqueEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var embeddedField in embeddedFields)
            {
                var key = String.Concat(embeddedField.BaseUri?.AbsoluteUri, "?", embeddedField.Path);

                if (savedEmbedFields.TryGetValue(key, out var existing))
                {
                    embeddedField.Path = existing.Path;
                }
                else
                {
                    var entryName = CalculateUniqueEntryName(uniqueEntryNames, embeddedField.Path);

                    if (embeddedField.BaseUri == null)
                    {
                        wixout.ImportDataStream(entryName, embeddedField.Path);
                    }
                    else // open the container specified in baseUri and copy the correct stream out of it.
                    {
                        using (var otherWixout = WixOutput.Read(embeddedField.BaseUri))
                        using (var stream = otherWixout.GetDataStream(embeddedField.Path))
                        using (var target = wixout.CreateDataStream(entryName))
                        {
                            stream.CopyTo(target);
                        }
                    }

                    embeddedField.Path = entryName;

                    savedEmbedFields.Add(key, embeddedField);
                }
            }
        }

        private void SaveIR(WixOutput wixout)
        {
            using (var writer = new StreamWriter(wixout.CreateDataStream(WixOutputStreamName)))
            {
                var jsonObject = new JsonObject
                {
                    { "id", this.Id },
                    { "version", Intermediate.CurrentVersion.ToString() }
                };

                var sectionsJson = new JsonArray(this.Sections.Count);
                foreach (var section in this.Sections)
                {
                    var sectionJson = section.Serialize();
                    sectionsJson.Add(sectionJson);
                }

                jsonObject.Add("sections", sectionsJson);

                var customDefinitions = this.GetCustomDefinitionsInSections();

                if (customDefinitions.Count > 0)
                {
                    var customDefinitionsJson = new JsonArray(customDefinitions.Count);

                    foreach (var kvp in customDefinitions.OrderBy(d => d.Key))
                    {
                        var customDefinitionJson = kvp.Value.Serialize();
                        customDefinitionsJson.Add(customDefinitionJson);
                    }

                    jsonObject.Add("definitions", customDefinitionsJson);
                }

                if (this.Localizations.Any())
                {
                    var localizationsJson = new JsonArray();
                    foreach (var localization in this.Localizations)
                    {
                        var localizationJson = localization.Serialize();
                        localizationsJson.Add(localizationJson);
                    }

                    jsonObject.Add("localizations", localizationsJson);
                }

                var json = SimpleJson.SerializeObject(jsonObject);
                writer.Write(json);
            }
        }

        private static string CalculateUniqueEntryName(ISet<string> entryNames, string path)
        {
            var filename = Path.GetFileName(path);
            var entryName = "wix-ir/" + filename;
            var i = 0;

            while (!entryNames.Add(entryName))
            {
                entryName = $"wix-ir/{filename}-{++i}";
            }

            return entryName;
        }

        private Dictionary<string, IntermediateTupleDefinition> GetCustomDefinitionsInSections()
        {
            var customDefinitions = new Dictionary<string, IntermediateTupleDefinition>();

            foreach (var tuple in this.Sections.SelectMany(s => s.Tuples).Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension))
            {
                if (!customDefinitions.ContainsKey(tuple.Definition.Name))
                {
                    customDefinitions.Add(tuple.Definition.Name, tuple.Definition);
                }
            }

            return customDefinitions;
        }

        private struct JsonWithPath
        {
            public JsonObject Json { get; set; }

            public Uri Path { get; set; }
        }
    }
}
