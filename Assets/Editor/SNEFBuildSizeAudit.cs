#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SNEF.EditorTools
{
    /// <summary>
    /// Read-only build-size diagnostics. It never changes importers, scenes, assets,
    /// PlayerSettings, QualitySettings, GraphicsSettings, or build profiles.
    /// </summary>
    public sealed class SNEFBuildSizeAudit : IPostprocessBuildWithReport
    {
        private const string OutputDirectory = "Docs/SNEF_BuildReports";

        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            Export(report, "LatestBuildReport");
        }

        [MenuItem("Tools/SNEF/Build Size Audit/Export Latest BuildReport")]
        public static void ExportLatestBuildReport()
        {
            BuildReport report = BuildReport.GetLatestReport();
            if (report == null)
            {
                EditorUtility.DisplayDialog(
                    "SNEF Build Size Audit",
                    "Unity no tiene un BuildReport cargado. Haz un build y vuelve a ejecutar esta opción.",
                    "Aceptar");
                return;
            }

            Export(report, "LatestBuildReport");
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputDirectory));
        }

        private static void Export(BuildReport report, string baseName)
        {
            Directory.CreateDirectory(OutputDirectory);

            AuditData data = Collect(report);
            string jsonPath = Path.Combine(OutputDirectory, baseName + ".json");
            string csvPath = Path.Combine(OutputDirectory, baseName + "_Assets.csv");
            string markdownPath = Path.Combine(OutputDirectory, baseName + ".md");

            File.WriteAllText(jsonPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            File.WriteAllText(csvPath, BuildCsv(data.assets), new UTF8Encoding(false));
            File.WriteAllText(markdownPath, BuildMarkdown(data), new UTF8Encoding(false));

            Debug.Log($"SNEF Build Size Audit: reporte diagnóstico exportado a {Path.GetFullPath(markdownPath)}");
        }

        private static AuditData Collect(BuildReport report)
        {
            BuildSummary summary = report.summary;
            AuditData data = new AuditData
            {
                generatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                outputPath = summary.outputPath,
                platform = summary.platform.ToString(),
                result = summary.result.ToString(),
                buildOptions = summary.options.ToString(),
                totalBytes = ToLong(summary.totalSize),
                totalTimeSeconds = summary.totalTime.TotalSeconds,
                totalWarnings = summary.totalWarnings,
                totalErrors = summary.totalErrors,
                playerSettings = CollectPlayerSettings()
            };

            Dictionary<string, AssetRecord> assets = new Dictionary<string, AssetRecord>(StringComparer.OrdinalIgnoreCase);

            foreach (PackedAssets packedAsset in report.packedAssets)
            {
                foreach (PackedAssetInfo info in packedAsset.contents)
                {
                    string path = Normalize(info.sourceAssetPath);
                    if (string.IsNullOrEmpty(path))
                        path = "(sin ruta de asset)";

                    if (!assets.TryGetValue(path, out AssetRecord record))
                    {
                        record = new AssetRecord
                        {
                            path = path,
                            type = Classify(path),
                            sourceBytes = GetSourceBytes(path),
                            importer = DescribeImporter(path),
                            includedBecause = ExplainInclusion(path)
                        };
                        assets.Add(path, record);
                    }

                    record.packedBytes += ToLong(info.packedSize);
                    if (!record.packedFiles.Contains(packedAsset.shortPath))
                        record.packedFiles.Add(packedAsset.shortPath);
                }
            }

            data.assets = assets.Values
                .OrderByDescending(x => x.packedBytes)
                .ThenBy(x => x.path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (BuildFile file in report.GetFiles())
            {
                string path = file.path;
                data.files.Add(new BuildFileRecord
                {
                    path = path,
                    role = file.role,
                    bytes = ToLong(file.size)
                });
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(x => x.enabled))
            {
                string[] dependencies = AssetDatabase.GetDependencies(scene.path, true);
                data.scenes.Add(new SceneRecord
                {
                    path = scene.path,
                    dependencyCount = dependencies.Length,
                    dependencySourceBytes = dependencies.Sum(GetSourceBytes)
                });
            }

            return data;
        }

        private static PlayerSettingsRecord CollectPlayerSettings()
        {
            return new PlayerSettingsRecord
            {
                webGLCompressionFormat = PlayerSettings.WebGL.compressionFormat.ToString(),
                webGLDecompressionFallback = PlayerSettings.WebGL.decompressionFallback,
                webGLDataCaching = PlayerSettings.WebGL.dataCaching,
                webGLExceptionSupport = PlayerSettings.WebGL.exceptionSupport.ToString(),
                stripEngineCode = PlayerSettings.stripEngineCode,
                managedStrippingLevel = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL).ToString(),
                colorSpace = PlayerSettings.colorSpace.ToString(),
                activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString()
            };
        }

        private static string DescribeImporter(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer == null)
                return string.Empty;

            if (importer is TextureImporter texture)
            {
                TextureImporterPlatformSettings webGL = texture.GetPlatformTextureSettings("WebGL");
                return $"Texture {texture.textureType}; max={webGL.maxTextureSize}; format={webGL.format}; " +
                       $"compression={webGL.textureCompression}; quality={webGL.compressionQuality}; " +
                       $"override={webGL.overridden}; alpha={texture.DoesSourceTextureHaveAlpha()}; " +
                       $"readable={texture.isReadable}; mipmaps={texture.mipmapEnabled}";
            }

            if (importer is ModelImporter model)
            {
                return $"Model; meshCompression={model.meshCompression}; readable={model.isReadable}; " +
                       $"blendShapes={model.importBlendShapes}; animation={model.importAnimation}; " +
                       $"animationType={model.animationType}; optimizeGameObjects={model.optimizeGameObjects}";
            }

            if (importer is AudioImporter audio)
            {
                AudioImporterSampleSettings settings = audio.defaultSampleSettings;
                return $"Audio; loadType={settings.loadType}; format={settings.compressionFormat}; " +
                       $"quality={settings.quality:0.##}; sampleRate={settings.sampleRateSetting}; " +
                       $"forceMono={audio.forceToMono}; preload={settings.preloadAudioData}";
            }

            return importer.GetType().Name;
        }

        private static string ExplainInclusion(string path)
        {
            if (path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Resources: Unity lo incluye automáticamente";
            if (path.IndexOf("/StreamingAssets/", StringComparison.OrdinalIgnoreCase) >= 0)
                return "StreamingAssets: copia directa al build";
            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return "Dependencia de paquete o módulo usado por el Player";
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return "Código administrado alcanzado por el Player/IL2CPP";
            return "Dependencia serializada de una escena, prefab, material, ScriptableObject o asset incluido";
        }

        private static long GetSourceBytes(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            string fullPath = Path.GetFullPath(path);
            return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static long ToLong(ulong value)
        {
            return value > long.MaxValue ? long.MaxValue : (long)value;
        }

        private static string Classify(string path)
        {
            string lower = path.ToLowerInvariant();
            string extension = Path.GetExtension(lower);
            if (lower.Contains("lightmap") || lower.Contains("lightingdata")) return "Lightmaps";
            if (lower.Contains("reflectionprobe") || lower.Contains("cubemap")) return "Cubemaps";
            if (lower.Contains("streamingassets")) return "StreamingAssets";
            if (lower.StartsWith("packages/")) return "Packages";
            switch (extension)
            {
                case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd": case ".exr": return "Textures";
                case ".fbx": case ".obj": return "Meshes / Models";
                case ".mat": return "Materials";
                case ".shader": case ".shadergraph": case ".shadersubgraph": case ".compute": return "Shaders";
                case ".wav": case ".mp3": case ".ogg": case ".aiff": return "Audio";
                case ".mp4": case ".webm": case ".mov": case ".avi": return "Video";
                case ".anim": case ".controller": return "Animations";
                case ".ttf": case ".otf": return "Fonts";
                case ".prefab": return "Prefabs";
                case ".unity": return "Scenes";
                case ".rendertexture": return "RenderTextures";
                case ".dll": case ".a": case ".so": case ".jslib": return "Plugins / DLLs";
                case ".asset": return "ScriptableObjects / Assets";
                default: return "Otros";
            }
        }

        private static string BuildCsv(IEnumerable<AssetRecord> assets)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("packedBytes,sourceBytes,type,path,packedFiles,importer,includedBecause");
            foreach (AssetRecord asset in assets)
            {
                text.Append(asset.packedBytes).Append(',')
                    .Append(asset.sourceBytes).Append(',')
                    .Append(Csv(asset.type)).Append(',')
                    .Append(Csv(asset.path)).Append(',')
                    .Append(Csv(string.Join("; ", asset.packedFiles))).Append(',')
                    .Append(Csv(asset.importer)).Append(',')
                    .Append(Csv(asset.includedBecause)).AppendLine();
            }
            return text.ToString();
        }

        private static string Csv(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static string BuildMarkdown(AuditData data)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# SNEF BuildReport export");
            text.AppendLine();
            text.AppendLine($"- Generado UTC: {data.generatedUtc}");
            text.AppendLine($"- Unity: {data.unityVersion}");
            text.AppendLine($"- Plataforma: {data.platform}");
            text.AppendLine($"- Resultado: {data.result}");
            text.AppendLine($"- Salida: `{data.outputPath}`");
            text.AppendLine($"- Tamaño total: {FormatBytes(data.totalBytes)}");
            text.AppendLine();
            text.AppendLine("## Top 100 assets empaquetados");
            text.AppendLine();
            text.AppendLine("| # | Tamaño empaquetado | Fuente | Tipo | Asset | Importador | Motivo |");
            text.AppendLine("|---:|---:|---:|---|---|---|---|");
            int index = 0;
            foreach (AssetRecord asset in data.assets.Take(100))
            {
                index++;
                text.AppendLine($"| {index} | {FormatBytes(asset.packedBytes)} | {FormatBytes(asset.sourceBytes)} | " +
                                $"{Md(asset.type)} | `{Md(asset.path)}` | {Md(asset.importer)} | {Md(asset.includedBecause)} |");
            }
            return text.ToString();
        }

        private static string Md(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L) return (bytes / (1024d * 1024d * 1024d)).ToString("0.00", CultureInfo.InvariantCulture) + " GiB";
            if (bytes >= 1024L * 1024L) return (bytes / (1024d * 1024d)).ToString("0.00", CultureInfo.InvariantCulture) + " MiB";
            if (bytes >= 1024L) return (bytes / 1024d).ToString("0.00", CultureInfo.InvariantCulture) + " KiB";
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        [Serializable]
        private sealed class AuditData
        {
            public string generatedUtc;
            public string unityVersion;
            public string outputPath;
            public string platform;
            public string result;
            public string buildOptions;
            public long totalBytes;
            public double totalTimeSeconds;
            public int totalWarnings;
            public int totalErrors;
            public PlayerSettingsRecord playerSettings;
            public List<BuildFileRecord> files = new List<BuildFileRecord>();
            public List<SceneRecord> scenes = new List<SceneRecord>();
            public List<AssetRecord> assets = new List<AssetRecord>();
        }

        [Serializable]
        private sealed class PlayerSettingsRecord
        {
            public string webGLCompressionFormat;
            public bool webGLDecompressionFallback;
            public bool webGLDataCaching;
            public string webGLExceptionSupport;
            public bool stripEngineCode;
            public string managedStrippingLevel;
            public string colorSpace;
            public string activeBuildTarget;
        }

        [Serializable]
        private sealed class BuildFileRecord
        {
            public string path;
            public string role;
            public long bytes;
        }

        [Serializable]
        private sealed class SceneRecord
        {
            public string path;
            public int dependencyCount;
            public long dependencySourceBytes;
        }

        [Serializable]
        private sealed class AssetRecord
        {
            public string path;
            public string type;
            public long packedBytes;
            public long sourceBytes;
            public string importer;
            public string includedBecause;
            public List<string> packedFiles = new List<string>();
        }
    }
}
#endif
