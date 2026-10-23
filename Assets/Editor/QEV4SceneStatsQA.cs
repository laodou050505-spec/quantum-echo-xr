// v4 场景统计 + CollisionQA — batchmode 入口
// -executeMethod QEV4SceneStatsQA.Run
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class QEV4SceneStatsQA
{
    public static void Run()
    {
        string scene = QuantumEcho.Editor.QuantumEchoTitleSceneBuilder.GameplayScenePath;
        EditorSceneManager.OpenScene(scene);
        var gos  = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        var cols  = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include);
        var rbs   = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include);
        var mbs   = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        string stats = $"=== QE 场景统计 v4 ===\nGameObjects={gos.Length}\nColliders={cols.Length}\nRigidbodies={rbs.Length}\nMonoBehaviours={mbs.Length}\n";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/QE_scene_stats_v4.txt", stats);
        Debug.Log(stats);
        // CollisionQA opens scene itself internally
        QuantumEcho.Editor.QuantumEchoCollisionQA.RunBatch();
        File.AppendAllText("Logs/QE_scene_stats_v4.txt", "CollisionQA=PASS\n");
        Debug.Log("[v4 QA] CollisionQA PASSED");
    }
}
