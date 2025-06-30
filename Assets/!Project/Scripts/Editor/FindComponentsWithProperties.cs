using System.Linq;
using System.Text;
using MisterGames.Common.GameObjects;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor {
    
    public static class FindComponentsWithProperties {
        
        [MenuItem("GameObject/Find renderers with lightmap", false, 0)]
        private static void FindMeshRenderersWithReceiveGILightmap() {
            var transforms = Selection.transforms.ToList();
            
            if (transforms is not { Count: > 0 }) {
                Debug.Log($"No objects selected to find renderers with lightmap.");
                return;
            }

            int count = 0;
            int totalCount = 0;
            int logCount = 0;
            StringBuilder sb = null;
            
            for (int i = 0; i < transforms.Count; i++) {
                var t = transforms[i];
                
                var meshRenderers = t.GetComponentsInChildren<MeshRenderer>(includeInactive: true)
                    .Where(m => (GameObjectUtility.GetStaticEditorFlags(m.gameObject) & StaticEditorFlags.ContributeGI) != 0 
                                && m.receiveGI == ReceiveGI.Lightmaps)
                    .ToArray();

                if (meshRenderers.Length == 0) continue;

                sb ??= new StringBuilder();

                for (int j = 0; j < meshRenderers.Length; j++) {
                    if (count >= 100) {
                        Debug.Log($"Found <color=yellow>{count}</color> usages " +
                                  $"of renderer with lightmap (part {++logCount}):\n{sb}");
                        sb.Clear();
                        count = 0;
                    }
                    
                    sb.AppendLine($" + {meshRenderers[j].GetPathInScene()}");
                    count++;
                    totalCount++;
                }
            }

            if (count < 100 && sb != null) {
                Debug.Log($"Found <color=yellow>{count}</color> usages " +
                          $"of renderer with lightmap (part {++logCount}):\n{sb}");
            }
            
            Debug.Log($"Found total <color=yellow>{totalCount}</color> usages " +
                      $"of renderer with lightmap ({logCount} parts).");
        }
    }
    
}