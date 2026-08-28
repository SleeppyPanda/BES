#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;


namespace BES.EditorTools
{
    /// <summary>
    /// Professional Diagnostic & Auto-Fix Tool for Ground Z-Fighting and Duplicate Geometry.
    /// Scans the active scene for ground meshes that overlap vertically or are 100% duplicates,
    /// and fixes them automatically by deleting duplicates and applying a micro-offset (1mm).
    /// </summary>
    public static class FindOverlappingGround
    {
        [MenuItem("BES/Diagnostics/Find & Fix Ground Flickering")]
        public static void DetectAndFixOverlaps()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var groundRenderers = new List<MeshRenderer>();

            // Filter for ground/floor objects
            foreach (var r in renderers)
            {
                if (r == null || r.gameObject == null) continue;
                
                string name = r.gameObject.name.ToLower();
                if (name.Contains("floor") || name.Contains("ground") || name.Contains("nen") || 
                    name.Contains("plane") || name.Contains("pavement") || name.Contains("tile"))
                {
                    groundRenderers.Add(r);
                }
            }

            Debug.Log($"[Ground Fixer] Scanning {groundRenderers.Count} ground mesh renderers for Z-fighting...");

            int duplicatesDeleted = 0;
            int offsetApplied = 0;
            float verticalThreshold = 0.02f; // Overlapping within 2cm vertically
            float microOffset = 0.0015f;     // 1.5mm vertical offset to resolve Z-fighting

            // Keep track of objects we've already processed or deleted
            var destroyedObjects = new HashSet<GameObject>();

            for (int i = 0; i < groundRenderers.Count; i++)
            {
                var r1 = groundRenderers[i];
                if (r1 == null || destroyedObjects.Contains(r1.gameObject)) continue;
                
                var b1 = r1.bounds;
                var t1 = r1.transform;
                var meshFilter1 = r1.GetComponent<MeshFilter>();
                var mesh1 = meshFilter1 != null ? meshFilter1.sharedMesh : null;

                for (int j = i + 1; j < groundRenderers.Count; j++)
                {
                    var r2 = groundRenderers[j];
                    if (r2 == null || destroyedObjects.Contains(r2.gameObject)) continue;
                    
                    var b2 = r2.bounds;
                    var t2 = r2.transform;
                    var meshFilter2 = r2.GetComponent<MeshFilter>();
                    var mesh2 = meshFilter2 != null ? meshFilter2.sharedMesh : null;

                    // 1. Check for 100% duplicate overlapping objects
                    float posDist = Vector3.Distance(t1.position, t2.position);
                    float rotAngle = Quaternion.Angle(t1.rotation, t2.rotation);
                    float scaleDist = Vector3.Distance(t1.localScale, t2.localScale);

                    if (posDist < 0.01f && rotAngle < 0.1f && scaleDist < 0.01f && mesh1 == mesh2 && mesh1 != null)
                    {
                        // Found exact duplicate geometry! Delete the second one
                        Debug.LogWarning($"[Ground Fixer] Deleted 100% duplicate ground object: '{r2.gameObject.name}' at {t2.position}");
                        destroyedObjects.Add(r2.gameObject);
                        Undo.DestroyObjectImmediate(r2.gameObject);
                        duplicatesDeleted++;
                        continue;
                    }

                    // 2. Check for coplanar overlapping ground panels (causes Z-fighting)
                    if (Mathf.Abs(b1.center.y - b2.center.y) <= verticalThreshold)
                    {
                        // Check if bounds intersect in X and Z
                        Vector3 minIntersect = Vector3.Max(b1.min, b2.min);
                        Vector3 maxIntersect = Vector3.Min(b1.max, b2.max);

                        if (minIntersect.x < maxIntersect.x && minIntersect.z < maxIntersect.z)
                        {
                            // Calculate overlap area in XZ plane
                            float overlapArea = (maxIntersect.x - minIntersect.x) * (maxIntersect.z - minIntersect.z);
                            if (overlapArea > 0.08f) // Significant horizontal overlap
                            {
                                // Apply a micro vertical offset to r2 to resolve Z-fighting
                                Undo.RecordObject(t2, "Resolve Z-Fighting");
                                t2.position = new Vector3(t2.position.x, t2.position.y + microOffset, t2.position.z);
                                offsetApplied++;
                                
                                Debug.Log($"[Ground Fixer] Resolved Z-fighting: Offset '{r2.gameObject.name}' by +1.5mm vertically (overlapping '{r1.gameObject.name}', area: {overlapArea:F2} sqm)");
                                
                                // Update bounds after offset for subsequent checks
                                b2 = r2.bounds;
                            }
                        }
                    }
                }
            }

            Debug.Log($"[Ground Fixer] Scan and Auto-Fix complete! " +
                      $"Duplicates deleted: {duplicatesDeleted}, Micro-offsets applied: {offsetApplied}.");
            
            if (duplicatesDeleted > 0 || offsetApplied > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
#endif
