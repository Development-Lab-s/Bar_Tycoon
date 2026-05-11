using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryExpressionEnumGeneratorWindow : EditorWindow
    {
        private const string DefaultOutputPath = "Assets/00. Work/CheolYee/02. Scripts/Story/RuntIme/Shared/Types/StoryExpressionType.cs";
        private const string EnumNamespace = "_00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types";
        private const string EnumName = "StoryExpressionType";

        [SerializeField] private string expressionNames = "Neutral\nHappy\nSad\nAngry\nSurprised\nEmbarrassed";
        [SerializeField] private string outputPath = DefaultOutputPath;

        [MenuItem("Tools/Story/Expression Enum Generator")]
        public static void Open()
        {
            var window = GetWindow<StoryExpressionEnumGeneratorWindow>("Expression Enum");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Story Expression Enum Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter one English expression identifier per line. The generator writes a runtime enum file as UTF-8.",
                MessageType.Info);

            EditorGUILayout.LabelField("Expression Names");
            expressionNames = EditorGUILayout.TextArea(expressionNames, GUILayout.MinHeight(150f));

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                outputPath = EditorGUILayout.TextField("Output", outputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(72f)))
                    BrowseOutputPath();
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Default Path"))
                    outputPath = DefaultOutputPath;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Generate", GUILayout.Width(120f)))
                    Generate();
            }
        }

        private void BrowseOutputPath()
        {
            string selected = EditorUtility.SaveFilePanelInProject(
                "Save Story Expression Enum",
                EnumName,
                "cs",
                "Choose a runtime C# file path for the expression enum.",
                Path.GetDirectoryName(outputPath)?.Replace('\\', '/'));

            if (!string.IsNullOrWhiteSpace(selected))
                outputPath = selected.Replace('\\', '/');
        }

        private void Generate()
        {
            string assetPath = NormalizeAssetPath(outputPath);
            if (!IsValidAssetPath(assetPath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Output path must be a .cs file inside Assets/.", "OK");
                return;
            }

            List<string> identifiers = ParseIdentifiers(expressionNames);
            if (identifiers.Count == 0)
            {
                EditorUtility.DisplayDialog("No Expressions", "Enter at least one valid expression name.", "OK");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                EditorUtility.DisplayDialog("Project Path Error", "Could not resolve the Unity project root.", "OK");
                return;
            }

            string fullPath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath)
                && !EditorUtility.DisplayDialog("Overwrite Enum?", $"{assetPath} already exists. Overwrite it?", "Overwrite", "Cancel"))
                return;

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, BuildEnumSource(identifiers), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.Refresh();
            Debug.Log($"Generated {EnumName} with {identifiers.Count} values at {assetPath}");
        }

        private static List<string> ParseIdentifiers(string raw)
        {
            var result = new List<string>();
            var used = new HashSet<string>();
            string[] lines = (raw ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string identifier = SanitizeIdentifier(lines[i], result.Count);
                if (string.IsNullOrWhiteSpace(identifier))
                    continue;

                string unique = identifier;
                int suffix = 2;
                while (!used.Add(unique))
                {
                    unique = $"{identifier}_{suffix}";
                    suffix++;
                }

                result.Add(unique);
            }

            return result;
        }

        private static string SanitizeIdentifier(string raw, int fallbackIndex)
        {
            string trimmed = (raw ?? string.Empty).Trim();
            var builder = new StringBuilder(trimmed.Length);

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool isAsciiLetter = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
                bool isDigit = c is >= '0' and <= '9';
                if (isAsciiLetter || isDigit || c == '_')
                    builder.Append(c);
            }

            if (builder.Length == 0)
                builder.Append("Expr").Append(fallbackIndex + 1);
            else if (builder[0] is >= '0' and <= '9')
                builder.Insert(0, "Expr");

            return builder.ToString();
        }

        private static string BuildEnumSource(IReadOnlyList<string> identifiers)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Generated by Tools > Story > Expression Enum Generator.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();
            builder.AppendLine($"namespace {EnumNamespace}");
            builder.AppendLine("{");
            builder.AppendLine($"    public enum {EnumName}");
            builder.AppendLine("    {");

            for (int i = 0; i < identifiers.Count; i++)
                builder.AppendLine($"        {identifiers[i]} = {i},");

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static bool IsValidAssetPath(string assetPath) =>
            assetPath.StartsWith("Assets/", System.StringComparison.Ordinal)
            && assetPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase);

        private static string NormalizeAssetPath(string path) =>
            (path ?? string.Empty).Trim().Replace('\\', '/');
    }
}
