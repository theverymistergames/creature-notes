using _Project.Scripts.Runtime.Levels;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace _Project.Scripts.Editor {
    
    internal sealed class LevelsBuildHelper : IPreprocessBuildWithReport, IPostprocessBuildWithReport {

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            LevelLoadingOptions.AllowLevelLoadingInEditor = false;
        }
        
        public void OnPostprocessBuild(BuildReport report) {
            LevelLoadingOptions.AllowLevelLoadingInEditor = true;
        }
    }
    
}