using UnityEditor;
using UnityEngine;
using EnemySystem;

public class EnemyDataMigrator : MonoBehaviour
{
    // This creates a clickable button in the top toolbar of Unity
    [MenuItem("Tools/AI/Migrate Patrol Points")]
    public static void MigratePatrolPoints()
    {
        // Grab every GameObject currently highlighted in the Hierarchy/Scene
        GameObject[] selectedObjects = Selection.gameObjects;
        int successCount = 0;

        foreach (GameObject go in selectedObjects)
        {
            // Check if the highlighted object actually has both scripts
            if (go.TryGetComponent(out EnemyMovementOld oldMovement) && 
                go.TryGetComponent(out EnemyBrain enemyBrain))
            {
                // 1. MUST DO: Record the action so you can press Ctrl+Z if you make a mistake!
                Undo.RecordObject(enemyBrain, "Migrate Patrol Points");

                // 2. Transfer the data
                // (Change 'aiCore.patrolPoints' if your variable is named differently or lives in a sub-module)
                enemyBrain.patrolPoints = oldMovement.patrolPoints;

                // 3. MUST DO: Tell Unity this object was modified, otherwise the scene won't save the changes
                EditorUtility.SetDirty(enemyBrain);
                
                successCount++;
            }
        }

        // Show a nice popup confirming it worked
        if (successCount > 0)
        {
            EditorUtility.DisplayDialog("Migration Complete", 
                $"Successfully transferred patrol points for {successCount} enemies!", "Awesome");
        }
        else
        {
            EditorUtility.DisplayDialog("Migration Failed", 
                "No highlighted objects had both EnemyMovementOld and AICore attached.", "Okay");
        }
    }
}