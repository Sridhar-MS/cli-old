// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Impl
{
    internal class LockFileFormat
    {
        public LockFile Read(Stream stream)
        {
            using (var textReader = new StreamReader(stream))
            {
                try
                {
                    using (var jsonReader = new JsonTextReader(textReader))
                    {
                        while (jsonReader.TokenType != JsonToken.StartObject)
                        {
                            if (!jsonReader.Read())
                            {
                                throw new InvalidDataException();
                            }
                        }
                        var token = JToken.Load(jsonReader);
                        return ReadLockFile(token as JObject);
                    }
                }
                catch
                {
                    // Ran into parsing errors, mark it as unlocked and out-of-date
                    return new LockFile
                    {
                        Version = int.MinValue
                    };
                }
            }
        }

        public void Write(Stream stream, LockFile lockFile)
        {
            using (var textWriter = new StreamWriter(stream))
            {
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;

                    var json = WriteLockFile(lockFile);
                    json.WriteTo(jsonWriter);
                }
            }
        }

        private LockFile ReadLockFile(JObject cursor)
        {
            var lockFile = new LockFile();
            lockFile.Version = ReadInt(cursor, "version", defaultValue: int.MinValue);
            lockFile.Targets = ReadObject(cursor["targets"] as JObject, ReadTarget);
            lockFile.ProjectFileDependencyGroups = ReadObject(cursor["projectFileDependencyGroups"] as JObject, ReadProjectFileDependencyGroup);
            ReadLibrary(cursor["libraries"] as JObject, lockFile);
            return lockFile;
        }

        private JObject WriteLockFile(LockFile lockFile)
        {
            var json = new JObject();
            json["locked"] = new JValue(false);
            json["version"] = new JValue(LockFile.CurrentVersion);
            json["targets"] = WriteObject(lockFile.Targets, WriteTarget);
            json["libraries"] = WriteLibraries(lockFile);
            json["projectFileDependencyGroups"] = WriteObject(lockFile.ProjectFileDependencyGroups, WriteProjectFileDependencyGroup);
            return json;
        }

        private void ReadLibrary(JObject json, LockFile lockFile)
        {
            if (json == null)
            {
                return;
            }

            foreach (var property in json)
            {
                var value = property.Value as JObject;
                if (value == null)
                {
                    continue;
                }

                var parts = property.Key.Split(new[] { '/' }, 2);
                var name = parts[0];
                var version = parts.Length == 2 ? NuGetVersion.Parse(parts[1]) : null;

                var type = value["type"]?.Value<string>();

                if (type == null || type == "package")
                {
                    lockFile.PackageLibraries.Add(new LockFilePackageLibrary
                    {
                        Name = name,
                        Version = version,
                        IsServiceable = ReadBool(value, "serviceable", defaultValue: false),
                        Sha512 = ReadString(value["sha512"]),
                        Files = ReadPathArray(value["files"] as JArray, ReadString)
                    });
                }
                else if (type == "project")
                {
                    lockFile.ProjectLibraries.Add(new LockFileProjectLibrary
                    {
                        Name = name,
                        Version = version,
                        Path = ReadString(value["path"])
                    });
                }
            }
        }

        private JObject WriteLibraries(LockFile lockFile)
        {
            var result = new JObject();

            foreach (var library in lockFile.ProjectLibraries)
            {
                var value = new JObject();
                value["type"] = WriteString("project");
                value["path"] = WriteString(library.Path);

                result[$"{library.Name}/{library.Version.ToString()}"] = value;
            }

            foreach (var library in lockFile.PackageLibraries)
            {
                var value = new JObject();
                value["type"] = WriteString("package");

                if (library.IsServiceable)
                {
                    WriteBool(value, "serviceable", library.IsServiceable);
                }

                value["sha512"] = WriteString(library.Sha512);
                WritePathArray(value, "files", library.Files.OrderBy(f => f), WriteString);

                result[$"{library.Name}/{library.Version.ToString()}"] = value;
            }

            return result;
        }

        private JProperty WriteTarget(LockFileTarget target)
        {
            var json = WriteObject(target.Libraries, WriteTargetLibrary);

            var key = target.TargetFramework + (target.RuntimeIdentifier == null ? "" : "/" + target.RuntimeIdentifier);

            return new JProperty(key, json);
        }

        private LockFileTarget ReadTarget(string property, JToken json)
        {
            var target = new LockFileTarget();
            var parts = property.Split(new[] { '/' }, 2);
            target.TargetFramework = NuGetFramework.Parse(parts[0]);
            if (parts.Length == 2)
            {
                target.RuntimeIdentifier = parts[1];
            }

            target.Libraries = ReadObject(json as JObject, ReadTargetLibrary);

            return target;
        }

        private LockFileTargetLibrary ReadTargetLibrary(string property, JToken json)
        {
            var library = new LockFileTargetLibrary();

            var parts = property.Split(new[] { '/' }, 2);
            library.Name = parts[0];
            if (parts.Length == 2)
            {
                library.Version = NuGetVersion.Parse(parts[1]);
            }

            var type = json["type"];
            if (type != null)
            {
                library.Type = ReadString(type);
            }

            var framework = json["framework"];
            if (framework != null)
            {
                library.TargetFramework = NuGetFramework.Parse(ReadString(framework));
            }

            library.Dependencies = ReadObject(json["dependencies"] as JObject, ReadPackageDependency);
            library.FrameworkAssemblies = new HashSet<string>(ReadArray(json["frameworkAssemblies"] as JArray, ReadFrameworkAssemblyReference), StringComparer.OrdinalIgnoreCase);
            library.RuntimeAssemblies = ReadObject(json["runtime"] as JObject, ReadFileItem);
            library.CompileTimeAssemblies = ReadObject(json["compile"] as JObject, ReadFileItem);
            library.ResourceAssemblies = ReadObject(json["resource"] as JObject, ReadFileItem);
            library.NativeLibraries = ReadObject(json["native"] as JObject, ReadFileItem);

            return library;
        }

        private JProperty WriteTargetLibrary(LockFileTargetLibrary library)
        {
            var json = new JObject();

            json["type"] = WriteString(library.Type);

            if (library.TargetFramework != null)
            {
                json["framework"] = WriteString(library.TargetFramework.ToString());
            }

            if (library.Dependencies.Count > 0)
            {
                json["dependencies"] = WriteObject(library.Dependencies.OrderBy(p => p.Id), WritePackageDependency);
            }

            if (library.FrameworkAssemblies.Count > 0)
            {
                json["frameworkAssemblies"] = WriteArray(library.FrameworkAssemblies.OrderBy(f => f), WriteFrameworkAssemblyReference);
            }

            if (library.CompileTimeAssemblies.Count > 0)
            {
                json["compile"] = WriteObject(library.CompileTimeAssemblies, WriteFileItem);
            }

            if (library.RuntimeAssemblies.Count > 0)
            {
                json["runtime"] = WriteObject(library.RuntimeAssemblies, WriteFileItem);
            }

            if (library.ResourceAssemblies.Count > 0)
            {
                json["resource"] = WriteObject(library.ResourceAssemblies, WriteFileItem);
            }

            if (library.NativeLibraries.Count > 0)
            {
                json["native"] = WriteObject(library.NativeLibraries, WriteFileItem);
            }

            return new JProperty(library.Name + "/" + library.Version, json);
        }

        private ProjectFileDependencyGroup ReadProjectFileDependencyGroup(string property, JToken json)
        {
            return new ProjectFileDependencyGroup(
                NuGetFramework.Parse(property),
                ReadArray(json as JArray, ReadString));
        }

        private JProperty WriteProjectFileDependencyGroup(ProjectFileDependencyGroup frameworkInfo)
        {
            return new JProperty(
                frameworkInfo.FrameworkName?.DotNetFrameworkName ?? string.Empty,
                WriteArray(frameworkInfo.Dependencies, WriteString));
        }

        private PackageDependencyGroup ReadPackageDependencySet(string property, JToken json)
        {
            var targetFramework = string.Equals(property, "*") ? null : NuGetFramework.Parse(property);
            return new PackageDependencyGroup(
                targetFramework,
                ReadObject(json as JObject, ReadPackageDependency));
        }

        private JProperty WritePackageDependencySet(PackageDependencyGroup item)
        {
            return new JProperty(
                item.TargetFramework?.ToString() ?? "*",
                WriteObject(item.Packages, WritePackageDependency));
        }


        private PackageDependency ReadPackageDependency(string property, JToken json)
        {
            var versionStr = json.Value<string>();
            return new PackageDependency(
                property,
                versionStr == null ? null : VersionRange.Parse(versionStr));
        }

        private JProperty WritePackageDependency(PackageDependency item)
        {
            return new JProperty(
                item.Id,
                WriteString(item.VersionRange?.ToString()));
        }

        private LockFileItem ReadFileItem(string property, JToken json)
        {
            var item = new LockFileItem { Path = PathUtility.GetPathWithDirectorySeparator(property) };
            foreach (var subProperty in json.OfType<JProperty>())
            {
                item.Properties[subProperty.Name] = subProperty.Value.Value<string>();
            }
            return item;
        }

        private JProperty WriteFileItem(LockFileItem item)
        {
            return new JProperty(
                item.Path,
                new JObject(item.Properties.Select(x => new JProperty(x.Key, x.Value))));
        }

        private string ReadFrameworkAssemblyReference(JToken json)
        {
            return json.Value<string>();
        }

        private JToken WriteFrameworkAssemblyReference(string item)
        {
            return new JValue(item);
        }

        private FrameworkSpecificGroup ReadPackageReferenceSet(JToken json)
        {
            var frameworkName = json["targetFramework"]?.ToString();
            return new FrameworkSpecificGroup(
                string.IsNullOrEmpty(frameworkName) ? null : NuGetFramework.Parse(frameworkName),
                ReadArray(json["references"] as JArray, ReadString));
        }

        private JToken WritePackageReferenceSet(FrameworkSpecificGroup item)
        {
            var json = new JObject();
            json["targetFramework"] = item.TargetFramework?.ToString();
            json["references"] = WriteArray(item.Items, WriteString);
            return json;
        }

        private IList<TItem> ReadArray<TItem>(JArray json, Func<JToken, TItem> readItem)
        {
            if (json == null)
            {
                return new List<TItem>();
            }
            var items = new List<TItem>();
            foreach (var child in json)
            {
                items.Add(readItem(child));
            }
            return items;
        }

        private IList<string> ReadPathArray(JArray json, Func<JToken, string> readItem)
        {
            return ReadArray(json, readItem).Select(f => PathUtility.GetPathWithDirectorySeparator(f)).ToList();
        }

        private void WriteArray<TItem>(JToken json, string property, IEnumerable<TItem> items, Func<TItem, JToken> writeItem)
        {
            if (items.Any())
            {
                json[property] = WriteArray(items, writeItem);
            }
        }

        private void WritePathArray(JToken json, string property, IEnumerable<string> items, Func<string, JToken> writeItem)
        {
            WriteArray(json, property, items.Select(f => PathUtility.GetPathWithForwardSlashes(f)), writeItem);
        }

        private JArray WriteArray<TItem>(IEnumerable<TItem> items, Func<TItem, JToken> writeItem)
        {
            var array = new JArray();
            foreach (var item in items)
            {
                array.Add(writeItem(item));
            }
            return array;
        }

        private JArray WritePathArray(IEnumerable<string> items, Func<string, JToken> writeItem)
        {
            return WriteArray(items.Select(f => PathUtility.GetPathWithForwardSlashes(f)), writeItem);
        }

        private IList<TItem> ReadObject<TItem>(JObject json, Func<string, JToken, TItem> readItem)
        {
            if (json == null)
            {
                return new List<TItem>();
            }
            var items = new List<TItem>();
            foreach (var child in json)
            {
                items.Add(readItem(child.Key, child.Value));
            }
            return items;
        }

        private void WriteObject<TItem>(JToken json, string property, IEnumerable<TItem> items, Func<TItem, JProperty> writeItem)
        {
            if (items.Any())
            {
                json[property] = WriteObject(items, writeItem);
            }
        }

        private JObject WriteObject<TItem>(IEnumerable<TItem> items, Func<TItem, JProperty> writeItem)
        {
            var array = new JObject();
            foreach (var item in items)
            {
                array.Add(writeItem(item));
            }
            return array;
        }

        private bool ReadBool(JToken cursor, string property, bool defaultValue)
        {
            var valueToken = cursor[property];
            if (valueToken == null)
            {
                return defaultValue;
            }
            return valueToken.Value<bool>();
        }

        private int ReadInt(JToken cursor, string property, int defaultValue)
        {
            var valueToken = cursor[property];
            if (valueToken == null)
            {
                return defaultValue;
            }
            return valueToken.Value<int>();
        }

        private string ReadString(JToken json)
        {
            return json.Value<string>();
        }

        private SemanticVersion ReadSemanticVersion(JToken json, string property)
        {
            var valueToken = json[property];
            if (valueToken == null)
            {
                throw new ArgumentException($"lock file missing required property '{property}'", nameof(property));
            }
            return SemanticVersion.Parse(valueToken.Value<string>());
        }

        private void WriteBool(JToken token, string property, bool value)
        {
            token[property] = new JValue(value);
        }

        private JToken WriteString(string item)
        {
            return item != null ? new JValue(item) : JValue.CreateNull();
        }

        private FrameworkName ReadFrameworkName(JToken json)
        {
            return json == null ? null : new FrameworkName(json.Value<string>());
        }
        private JToken WriteFrameworkName(FrameworkName item)
        {
            return item != null ? new JValue(item.ToString()) : JValue.CreateNull();
        }
    }
}