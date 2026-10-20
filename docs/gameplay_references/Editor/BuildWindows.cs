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
            Phase1SceneBuilder.Create();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/SpiderManPhase1.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Phase1SceneBuilder.ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + result.summary.result);
            Debug.Log("PHASE1_BUILD_SUCCEEDED: " + output + " / " + result.summary.totalSize + " bytes");
        }
    }
}
