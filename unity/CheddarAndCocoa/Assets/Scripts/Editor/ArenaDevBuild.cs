#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CheddarAndCocoa.EditorTools
{
    public static class ArenaDevBuild
    {
        private const string ArenaScenePath = "Assets/Scenes/ArenaScene.unity";
        private const string DefaultOutputRelativeToProject = "../builds/dev/CheddarAndCocoa-Arena.app";
        private const string ReleaseOutputRelativeToProject = "../builds/release/CheddarAndCocoa-Demo.app";

        [MenuItem("Cheddar And Cocoa/Build Arena Dev Mac")]
        public static void BuildDevMac()
        {
            BuildMac(DefaultOutputRelativeToProject, BuildOptions.Development | BuildOptions.AllowDebugging, "Arena dev");
        }

        [MenuItem("Cheddar And Cocoa/Build Arena Release Mac")]
        public static void BuildReleaseMac()
        {
            BuildMac(ReleaseOutputRelativeToProject, BuildOptions.CompressWithLz4HC, "Arena release");
        }

        private static void BuildMac(string outputRelativeToProject, BuildOptions buildOptions, string label)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.GetFullPath(Path.Combine(projectRoot, outputRelativeToProject));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ArenaScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"{label} build result: {summary.result}; output: {outputPath}; size: {summary.totalSize} bytes");

            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"{label} build failed with result {summary.result}. See Unity log for details.");
        }
    }
}
#endif
