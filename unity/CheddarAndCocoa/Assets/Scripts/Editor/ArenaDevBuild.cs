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

        [MenuItem("Cheddar And Cocoa/Build Arena Dev Mac")]
        public static void BuildDevMac()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.GetFullPath(Path.Combine(projectRoot, DefaultOutputRelativeToProject));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ArenaScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Arena dev build result: {summary.result}; output: {outputPath}; size: {summary.totalSize} bytes");

            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Arena dev build failed with result {summary.result}. See Unity log for details.");
        }
    }
}
#endif
