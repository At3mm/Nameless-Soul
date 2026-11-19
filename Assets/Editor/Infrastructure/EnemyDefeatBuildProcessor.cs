using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

/// <summary>Bakes scene identities into the build copy without modifying the authored scene.</summary>
public sealed class EnemyDefeatBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene,BuildReport report)
    {
        foreach(var root in scene.GetRootGameObjects())
            foreach(var enemy in root.GetComponentsInChildren<EnemyClassification>(true))
                if(enemy.enemyType!=EnemyClassification.EnemyType.Normal && string.IsNullOrWhiteSpace(enemy.persistentId))
                    enemy.persistentId=EnemyDefeatState.GetIdentity(enemy);
    }
}
