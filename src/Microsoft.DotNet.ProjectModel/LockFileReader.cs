using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel
{
    public static class LockFileReader
    {
        private static readonly HashSet<string> NonAssetProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "type",
            "dependencies"
        };

        public const string LockFileName = "project.lock.json";

        public static LockFile Read(Stream stream)
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
                    // Ran into parsing errors, return an empty, invalid, lock file
                    return LockFile.Invalid;
                }
            }
        }

        private static LockFile ReadLockFile(JObject cursor)
        {
            var version = ReadInt(cursor, "version", defaultValue: int.MinValue);
            var libraries = ReadObject(cursor["libraries"] as JObject, ReadLibrary);
            var targets = ReadObject(cursor["targets"] as JObject, ReadTarget(libraries));
            var projectDependencyGroups = ReadObject(cursor["projectFileDependencyGroups"] as JObject, ReadProjectFileDependencyGroup);
            return new LockFile(version, targets, libraries, projectDependencyGroups);
        }

        private static LockFileLibrary ReadLibrary(string property, JToken json)
        {
            var parts = property.Split(new[] { '/' }, 2);
            var name = parts[0];
            var version = parts.Length == 2 ? NuGetVersion.Parse(parts[1]) : null;

            var type = json["type"]?.Value<string>();

            return new LockFileLibrary(
                name,
                type,
                version,
                ReadBool(json, "serviceable", defaultValue: false),
                ReadString(json["sha512"]),
                ReadPathArray(json["files"] as JArray, ReadString));
        }

        private static Func<string, JToken, LockFileTarget> ReadTarget(IEnumerable<LockFileLibrary> libraries)
        {
            return (property, json) =>
            {
                var parts = property.Split(new[] { '/' }, 2);
                var framework = new NuGetFramework(parts[0]);
                var runtimeId = parts.Length == 2 ? parts[1] : null;
                var targetLibraries = ReadObject(json as JObject, ReadTargetLibrary(libraries));

                return new LockFileTarget(framework, runtimeId, targetLibraries);
            };
        }

        private static Func<string, JToken, LockFileTargetLibrary> ReadTargetLibrary(IEnumerable<LockFileLibrary> libraries)
        {
            return (property, json) =>
            {
                var parts = property.Split(new[] { '/' }, 2);
                var name = parts[0];
                var version = parts.Length == 2 ? NuGetVersion.Parse(parts[1]) : null;

                var library = libraries.FirstOrDefault(l =>
                    string.Equals(l.Name, name, StringComparison.Ordinal) &&
                    Equals(l.Version, version));

                var framework = json["framework"];
                NuGetFramework fx = null;
                if (framework != null)
                {
                    fx = new NuGetFramework(ReadString(framework));
                }

                return new LockFileTargetLibrary(
                    library,
                    framework,
                    ReadObject(json["dependencies"] as JObject, ReadPackageDependency),
                    ReadAssetGroups(json as JObject));
            };
        }

        private static LockFileDependency ReadPackageDependency(string property, JToken json)
        {
            return new LockFileDependency(
                property,
                NuGetVersion.Parse(ReadString(json)));
        }

        private static ProjectDependencyGroup ReadProjectFileDependencyGroup(string property, JToken json)
        {
            return new ProjectDependencyGroup(
                string.IsNullOrEmpty(property) ? null : new NuGetFramework(property),
                ReadArray(json as JArray, ReadString));
        }

        private static IList<LockFileAssetGroup> ReadAssetGroups(JObject json)
        {
            var groups = new List<LockFileAssetGroup>();
            if(json == null)
            {
                return groups;
            }

            foreach(var property in json.Properties().Where(p => !NonAssetProperties.Contains(p.Name)))
            {

            }
        }

        private static IList<TItem> ReadArray<TItem>(JArray json, Func<JToken, TItem> readItem)
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

        private static IList<string> ReadPathArray(JArray json, Func<JToken, string> readItem)
        {
            return ReadArray(json, readItem).Select(f => f.Replace('/', Path.DirectorySeparatorChar)).ToList();
        }

        private static IList<TItem> ReadObject<TItem>(JObject json, Func<string, JToken, TItem> readItem)
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

        private static bool ReadBool(JToken cursor, string property, bool defaultValue)
        {
            var valueToken = cursor[property];
            if (valueToken == null)
            {
                return defaultValue;
            }
            return valueToken.Value<bool>();
        }

        private static int ReadInt(JToken cursor, string property, int defaultValue)
        {
            var valueToken = cursor[property];
            if (valueToken == null)
            {
                return defaultValue;
            }
            return valueToken.Value<int>();
        }

        private static string ReadString(JToken json)
        {
            return json.Value<string>();
        }
    }
}
