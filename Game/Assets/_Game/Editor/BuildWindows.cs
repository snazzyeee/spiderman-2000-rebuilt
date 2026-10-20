using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpiderManRebuild.Editor
{
    public static class BuildWindows
    {
        [MenuItem("Rebuild/Build Windows Laboratory")]
        public static void Build()
        {
            LabSceneBuilder.Create();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/TraversalLab.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { LabSceneBuilder.ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + result.summary.result);
            Debug.Log("TRAVERSAL_BUILD_SUCCEEDED: " + output + " / " + result.summary.totalSize + " bytes");
        }
    }
}
