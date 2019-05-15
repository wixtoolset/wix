// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters.Tupleizer
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Wix3 = Microsoft.Tools.WindowsInstallerXml;
    using WixToolset.Converters.Tupleizer;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class ConvertTuplesFixture
    {
        [Fact]
        public void CanLoadWixoutAndConvertToIntermediate()
        {
            var rootFolder = TestData.Get();
            var dataFolder = TestData.Get(@"TestData\Integration");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var path = Path.Combine(dataFolder, "test.wixout");
                var output = Wix3.Output.Load(path, suppressVersionCheck: true, suppressSchema: true);

                var command = new ConvertTuplesCommand();
                var intermediate = command.Execute(output);

                Assert.NotNull(intermediate);
                Assert.Single(intermediate.Sections);
                Assert.Equal(String.Empty, intermediate.Id);

                // Save and load to guarantee round-tripping support.
                //
                var wixiplFile = Path.Combine(intermediateFolder, "test.wixipl");
                intermediate.Save(wixiplFile);

                intermediate = Intermediate.Load(wixiplFile);

                // Dump to text for easy diffing, with some massaging to keep v3 and v4 diffable.
                //
                var tables = output.Tables.Cast<Wix3.Table>();
                var wix3Dump = tables
                    .SelectMany(table => table.Rows.Cast<Wix3.Row>()
                    .Select(row => RowToString(row)))
                    .ToArray();

                var tuples = intermediate.Sections.SelectMany(s => s.Tuples);
                var wix4Dump = tuples.Select(tuple => TupleToString(tuple)).ToArray();

                Assert.Equal(wix3Dump, wix4Dump);

                // Useful when you want to diff the outputs with another diff tool...
                // 
                //var wix3TextDump = String.Join(Environment.NewLine, wix3Dump.OrderBy(val => val));
                //var wix4TextDump = String.Join(Environment.NewLine, wix4Dump.OrderBy(val => val));
                //Assert.Equal(wix3TextDump, wix4TextDump);
            }
        }

        private static string RowToString(Wix3.Row row)
        {
            var fields = String.Join(",", row.Fields.Select(field => field.Data?.ToString()));

            // Massage output to match WiX v3 rows and v4 tuples.
            //
            switch (row.Table.Name)
            {
                case "File":
                    var fieldValues = row.Fields.Take(7).Select(field => field.Data?.ToString()).ToArray();
                    if (fieldValues[3] == null)
                    {
                        // "Somebody" sometimes writes out a null field even when the column definition says
                        // it's non-nullable. Not naming names or anything. (SWID tags.)
                        fieldValues[3] = "0";
                    }
                    fields = String.Join(",", fieldValues);
                    break;
                case "WixFile":
                    fields = String.Join(",", row.Fields.Take(8).Select(field => field.Data?.ToString()));
                    break;
            }

            return $"{row.Table.Name},{fields}";
        }

        private static string TupleToString(WixToolset.Data.IntermediateTuple tuple)
        {
            var fields = String.Join(",", tuple.Fields.Select(field => field?.AsString()));

            switch (tuple.Definition.Name)
            {
                // Massage output to match WiX v3 rows and v4 tuples.
                //
                case "Component":
                {
                    var componentTuple = (ComponentTuple)tuple;
                    var attributes = ComponentLocation.Either == componentTuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesOptional : 0;
                    attributes |= ComponentLocation.SourceOnly == componentTuple.Location ? WindowsInstallerConstants.MsidbComponentAttributesSourceOnly : 0;
                    attributes |= ComponentKeyPathType.Registry == componentTuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesRegistryKeyPath : 0;
                    attributes |= ComponentKeyPathType.OdbcDataSource == componentTuple.KeyPathType ? WindowsInstallerConstants.MsidbComponentAttributesODBCDataSource : 0;
                    attributes |= componentTuple.DisableRegistryReflection ? WindowsInstallerConstants.MsidbComponentAttributesDisableRegistryReflection : 0;
                    attributes |= componentTuple.NeverOverwrite ? WindowsInstallerConstants.MsidbComponentAttributesNeverOverwrite : 0;
                    attributes |= componentTuple.Permanent ? WindowsInstallerConstants.MsidbComponentAttributesPermanent : 0;
                    attributes |= componentTuple.SharedDllRefCount ? WindowsInstallerConstants.MsidbComponentAttributesSharedDllRefCount : 0;
                    attributes |= componentTuple.Shared ? WindowsInstallerConstants.MsidbComponentAttributesShared : 0;
                    attributes |= componentTuple.Transitive ? WindowsInstallerConstants.MsidbComponentAttributesTransitive : 0;
                    attributes |= componentTuple.UninstallWhenSuperseded ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;
                    attributes |= componentTuple.Win64 ? WindowsInstallerConstants.MsidbComponentAttributes64bit : 0;

                    fields = String.Join(",",
                        componentTuple.ComponentId,
                        componentTuple.Directory_,
                        attributes.ToString(),
                        componentTuple.Condition,
                        componentTuple.KeyPath
                        );
                    break;
                }
                case "CustomAction":
                {
                    var customActionTuple = (CustomActionTuple)tuple;
                    var type = customActionTuple.Win64 ? WindowsInstallerConstants.MsidbCustomActionType64BitScript : 0;
                    type |= customActionTuple.TSAware ? WindowsInstallerConstants.MsidbCustomActionTypeTSAware : 0;
                    type |= customActionTuple.Impersonate ? 0 : WindowsInstallerConstants.MsidbCustomActionTypeNoImpersonate;
                    type |= customActionTuple.IgnoreResult ? WindowsInstallerConstants.MsidbCustomActionTypeContinue : 0;
                    type |= customActionTuple.Hidden ? WindowsInstallerConstants.MsidbCustomActionTypeHideTarget : 0;
                    type |= customActionTuple.Async ? WindowsInstallerConstants.MsidbCustomActionTypeAsync : 0;
                    type |= CustomActionExecutionType.FirstSequence == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeFirstSequence : 0;
                    type |= CustomActionExecutionType.OncePerProcess == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeOncePerProcess : 0;
                    type |= CustomActionExecutionType.ClientRepeat == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeClientRepeat : 0;
                    type |= CustomActionExecutionType.Deferred == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript : 0;
                    type |= CustomActionExecutionType.Rollback == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeRollback : 0;
                    type |= CustomActionExecutionType.Commit == customActionTuple.ExecutionType ? WindowsInstallerConstants.MsidbCustomActionTypeInScript | WindowsInstallerConstants.MsidbCustomActionTypeCommit : 0;
                    type |= CustomActionSourceType.File == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeSourceFile : 0;
                    type |= CustomActionSourceType.Directory == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeDirectory : 0;
                    type |= CustomActionSourceType.Property == customActionTuple.SourceType ? WindowsInstallerConstants.MsidbCustomActionTypeProperty : 0;
                    type |= CustomActionTargetType.Dll == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeDll : 0;
                    type |= CustomActionTargetType.Exe == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeExe : 0;
                    type |= CustomActionTargetType.TextData == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeTextData : 0;
                    type |= CustomActionTargetType.JScript == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeJScript : 0;
                    type |= CustomActionTargetType.VBScript == customActionTuple.TargetType ? WindowsInstallerConstants.MsidbCustomActionTypeVBScript : 0;

                    fields = String.Join(",",
                        type.ToString(),
                        customActionTuple.Source,
                        customActionTuple.Target,
                        customActionTuple.PatchUninstall ? WindowsInstallerConstants.MsidbCustomActionTypePatchUninstall.ToString() : null
                        );
                    break;
                }
                case "Feature":
                {
                    var featureTuple = (FeatureTuple)tuple;
                    var attributes = featureTuple.DisallowAbsent ? WindowsInstallerConstants.MsidbFeatureAttributesUIDisallowAbsent : 0;
                    attributes |= featureTuple.DisallowAdvertise ? WindowsInstallerConstants.MsidbFeatureAttributesDisallowAdvertise : 0;
                    attributes |= FeatureInstallDefault.FollowParent == featureTuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFollowParent : 0;
                    attributes |= FeatureInstallDefault.Source == featureTuple.InstallDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorSource : 0;
                    attributes |= FeatureTypicalDefault.Advertise == featureTuple.TypicalDefault ? WindowsInstallerConstants.MsidbFeatureAttributesFavorAdvertise : 0;

                    fields = String.Join(",",
                        featureTuple.Feature_Parent,
                        featureTuple.Title,
                        featureTuple.Description,
                        featureTuple.Display.ToString(),
                        featureTuple.Level.ToString(),
                        featureTuple.Directory_,
                        attributes.ToString());
                    break;
                }
                case "File":
                {
                    var fileTuple = (FileTuple)tuple;
                    fields = String.Join(",",
                    fileTuple.Component_,
                    fileTuple.LongFileName,
                    fileTuple.FileSize.ToString(),
                    fileTuple.Version,
                    fileTuple.Language,
                    ((fileTuple.ReadOnly ? WindowsInstallerConstants.MsidbFileAttributesReadOnly : 0)
                        | (fileTuple.Hidden ? WindowsInstallerConstants.MsidbFileAttributesHidden : 0)
                        | (fileTuple.System ? WindowsInstallerConstants.MsidbFileAttributesSystem : 0)
                        | (fileTuple.Vital ? WindowsInstallerConstants.MsidbFileAttributesVital : 0)
                        | (fileTuple.Checksum ? WindowsInstallerConstants.MsidbFileAttributesChecksum : 0)
                        | ((fileTuple.Compressed.HasValue && fileTuple.Compressed.Value) ? WindowsInstallerConstants.MsidbFileAttributesCompressed : 0)
                        | ((fileTuple.Compressed.HasValue && !fileTuple.Compressed.Value) ? WindowsInstallerConstants.MsidbFileAttributesNoncompressed : 0))
                        .ToString());
                    break;
                }

                case "Registry":
                {
                    var registryTuple = (RegistryTuple)tuple;
                    var value = registryTuple.Value;

                    switch (registryTuple.ValueType)
                    {
                        case RegistryValueType.Binary:
                            value = String.Concat("#x", value);
                            break;
                        case RegistryValueType.Expandable:
                            value = String.Concat("#%", value);
                            break;
                        case RegistryValueType.Integer:
                            value = String.Concat("#", value);
                            break;
                        case RegistryValueType.MultiString:
                            switch (registryTuple.ValueAction)
                            {
                                case RegistryValueActionType.Append:
                                    value = String.Concat("[~]", value);
                                    break;
                                case RegistryValueActionType.Prepend:
                                    value = String.Concat(value, "[~]");
                                    break;
                                case RegistryValueActionType.Write:
                                default:
                                    if (null != value && -1 == value.IndexOf("[~]", StringComparison.Ordinal))
                                    {
                                        value = String.Concat("[~]", value, "[~]");
                                    }
                                    break;
                            }
                            break;
                        case RegistryValueType.String:
                            // escape the leading '#' character for string registry keys
                            if (null != value && value.StartsWith("#", StringComparison.Ordinal))
                            {
                                value = String.Concat("#", value);
                            }
                            break;
                    }

                    fields = String.Join(",",
                        ((int)registryTuple.Root).ToString(),
                        registryTuple.Key,
                        registryTuple.Name,
                        value,
                        registryTuple.Component_
                        );
                    break;
                }

                case "RemoveRegistry":
                {
                    var removeRegistryTuple = (RemoveRegistryTuple)tuple;
                    fields = String.Join(",",
                        ((int)removeRegistryTuple.Root).ToString(),
                        removeRegistryTuple.Key,
                        removeRegistryTuple.Name,
                        removeRegistryTuple.Component_
                        );
                    break;
                }

                case "ServiceControl":
                {
                    var serviceControlTuple = (ServiceControlTuple)tuple;

                    var events = serviceControlTuple.InstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventDelete : 0;
                    events |= serviceControlTuple.UninstallRemove ? WindowsInstallerConstants.MsidbServiceControlEventUninstallDelete : 0;
                    events |= serviceControlTuple.InstallStart ? WindowsInstallerConstants.MsidbServiceControlEventStart : 0;
                    events |= serviceControlTuple.UninstallStart ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStart : 0;
                    events |= serviceControlTuple.InstallStop ? WindowsInstallerConstants.MsidbServiceControlEventStop : 0;
                    events |= serviceControlTuple.UninstallStop ? WindowsInstallerConstants.MsidbServiceControlEventUninstallStop : 0;

                    fields = String.Join(",",
                        serviceControlTuple.Name,
                        events.ToString(),
                        serviceControlTuple.Arguments,
                        serviceControlTuple.Wait == true ? "1" : "0",
                        serviceControlTuple.Component_
                        );
                    break;
                }

                case "ServiceInstall":
                {
                    var serviceInstallTuple = (ServiceInstallTuple)tuple;

                    var errorControl = (int)serviceInstallTuple.ErrorControl;
                    errorControl |= serviceInstallTuple.Vital ? WindowsInstallerConstants.MsidbServiceInstallErrorControlVital : 0;

                    var serviceType = (int)serviceInstallTuple.ServiceType;
                    serviceType |= serviceInstallTuple.Interactive ? WindowsInstallerConstants.MsidbServiceInstallInteractive : 0;

                    fields = String.Join(",",
                        serviceInstallTuple.Name,
                        serviceInstallTuple.DisplayName,
                        serviceType.ToString(),
                        ((int)serviceInstallTuple.StartType).ToString(),
                        errorControl.ToString(),
                        serviceInstallTuple.LoadOrderGroup,
                        serviceInstallTuple.Dependencies,
                        serviceInstallTuple.StartName,
                        serviceInstallTuple.Password,
                        serviceInstallTuple.Arguments,
                        serviceInstallTuple.Component_,
                        serviceInstallTuple.Description
                        );
                    break;
                }

                case "Upgrade":
                {
                    var upgradeTuple = (UpgradeTuple)tuple;

                    var attributes = upgradeTuple.MigrateFeatures ? WindowsInstallerConstants.MsidbUpgradeAttributesMigrateFeatures : 0;
                    attributes |= upgradeTuple.OnlyDetect ? WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect : 0;
                    attributes |= upgradeTuple.IgnoreRemoveFailures ? WindowsInstallerConstants.MsidbUpgradeAttributesIgnoreRemoveFailure : 0;
                    attributes |= upgradeTuple.VersionMinInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive : 0;
                    attributes |= upgradeTuple.VersionMaxInclusive ? WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive : 0;
                    attributes |= upgradeTuple.ExcludeLanguages ? WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive : 0;

                    fields = String.Join(",",
                        upgradeTuple.VersionMin,
                        upgradeTuple.VersionMax,
                        upgradeTuple.Language,
                        attributes.ToString(),
                        upgradeTuple.Remove,
                        upgradeTuple.ActionProperty
                        );
                    break;
                }

                case "WixAction":
                {
                    var wixActionTuple = (WixActionTuple)tuple;
                    fields = String.Join(",",
                        wixActionTuple.SequenceTable,
                        wixActionTuple.Action,
                        wixActionTuple.Condition,
                        // BUGBUGBUG: AB#2626
                        wixActionTuple.Sequence == 0 ? String.Empty : wixActionTuple.Sequence.ToString(),
                        wixActionTuple.Before,
                        wixActionTuple.After,
                        wixActionTuple.Overridable == true ? "1" : "0"
                        );
                    break;
                }

                case "WixComplexReference":
                {
                    var wixComplexReferenceTuple = (WixComplexReferenceTuple)tuple;
                    fields = String.Join(",",
                        wixComplexReferenceTuple.Parent,
                        ((int)wixComplexReferenceTuple.ParentType).ToString(),
                        wixComplexReferenceTuple.ParentLanguage,
                        wixComplexReferenceTuple.Child,
                        ((int)wixComplexReferenceTuple.ChildType).ToString(),
                        wixComplexReferenceTuple.IsPrimary ? "1" : "0"
                        );
                    break;
                }

                case "WixFile":
                {
                    var wixFileTuple = (WixFileTuple)tuple;
                    fields = String.Concat(
                        wixFileTuple.AssemblyType == FileAssemblyType.DotNetAssembly ? "0" : wixFileTuple.AssemblyType == FileAssemblyType.Win32Assembly ? "1" : String.Empty, ",",
                        String.Join(",", tuple.Fields.Skip(2).Take(6).Select(field => (string)field).ToArray()));
                    break;
                }

                case "WixProperty":
                {
                    var wixPropertyTuple = (WixPropertyTuple)tuple;
                    var attributes = 0;
                    attributes |= wixPropertyTuple.Admin ? 0x1 : 0;
                    attributes |= wixPropertyTuple.Hidden ? 0x2 : 0;
                    attributes |= wixPropertyTuple.Secure ? 0x4 : 0;

                    fields = String.Join(",",
                        wixPropertyTuple.Property_,
                        attributes.ToString()
                        );
                    break;
                }

            }

            var id = tuple.Id == null ? String.Empty : String.Concat(",", tuple.Id.Id);
            return $"{tuple.Definition.Name}{id},{fields}";
        }
    }
}
