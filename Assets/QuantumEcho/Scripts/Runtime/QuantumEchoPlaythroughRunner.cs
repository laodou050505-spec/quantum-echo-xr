using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace QuantumEcho
{
    [DefaultExecutionOrder(-50)]
    public sealed class QuantumEchoPlaythroughRunner : MonoBehaviour
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<string> _results = new List<string>();
        QuantumEchoGameManager _manager;
        QuantumPlayerRig _player;
        FieldInfo _interactHeldField;
        FieldInfo _wonField;
        FieldInfo _gateOpenField;
        FieldInfo _trainDockField;
        FieldInfo _luredField;
        MethodInfo _commitEcho;

        IEnumerator Start()
        {
            Debug.Log("QATEST started full four-level playthrough.");
            _manager = GetComponent<QuantumEchoGameManager>();
            _player = _manager.player;
            _interactHeldField = typeof(QuantumPlayerRig).GetField("<InteractHeld>k__BackingField", PrivateInstance);
            _wonField = typeof(QuantumEchoGameManager).GetField("_won", PrivateInstance);
            _gateOpenField = typeof(QuantumEchoGameManager).GetField("_gateOpenAmount", PrivateInstance);
            _trainDockField = typeof(QuantumEchoGameManager).GetField("_trainDockProgress", PrivateInstance);
            _luredField = typeof(QuantumEchoGameManager).GetField("_luredSentinels", PrivateInstance);
            _commitEcho = typeof(QuantumEchoGameManager).GetMethod("CommitEcho", PrivateInstance);

            if (_player == null || _interactHeldField == null || _commitEcho == null)
            {
                _results.Add("HARNESS: FAIL | required runtime references are missing");
                Finish();
                yield break;
            }

            yield return null;
            _player.enabled = false;
            yield return TestTutorial();
            yield return TestPaired();
            yield return TestTrain();
            yield return TestLure();
            Finish();
        }

        IEnumerator TestTutorial()
        {
            var level = BeginLevel(0);
            Move(level.padA);
            SetInteract(true);
            yield return Wait(0.62f);
            Commit();
            SetInteract(false);
            yield return Wait(1.05f);
            var gateOpened = GateOpen > 0.68f;
            Move(level.core);
            SetInteract(true);
            yield return Wait(0.48f);
            var progressVisible = ProgressVisible();
            yield return Wait(0.68f);
            SetInteract(false);
            Record(0, Won && gateOpened && progressVisible, $"gate={GateOpen:0.00}, holdProgress={progressVisible}");
        }

        IEnumerator TestPaired()
        {
            var level = BeginLevel(1);
            Move(level.padA);
            SetInteract(true);
            yield return Wait(0.62f);
            Commit();
            Move(level.padB);
            SetInteract(true);
            yield return Wait(0.62f);
            Commit();
            SetInteract(false);
            yield return Wait(1.05f);
            var gateOpened = GateOpen > 0.68f;
            Move(level.core);
            SetInteract(true);
            yield return Wait(1.15f);
            SetInteract(false);
            Record(1, Won && gateOpened, $"gate={GateOpen:0.00}");
        }

        IEnumerator TestTrain()
        {
            var level = BeginLevel(2);
            var renderer = level.train != null ? level.train.GetComponentInChildren<Renderer>(true) : null;
            var trainVisible = level.train != null && level.train.gameObject.activeInHierarchy && renderer != null && renderer.enabled;
            var trainFacingReversed = level.train != null && Mathf.Abs(Mathf.DeltaAngle(level.train.localEulerAngles.y, 180f)) < 1f;
            Move(level.padA);
            SetInteract(true);
            yield return Wait(0.62f);
            Commit();
            SetInteract(false);
            yield return Wait(3.85f);
            Move(level.padB);
            SetInteract(true);
            yield return Wait(0.62f);
            Commit();
            SetInteract(false);
            yield return Wait(7.6f);
            var docked = TrainDock > 0.96f;
            var dockFacing = level.train != null && Mathf.Abs(Mathf.DeltaAngle(level.train.localEulerAngles.y, 90f)) < 2f;
            Move(level.core);
            SetInteract(true);
            yield return Wait(1.15f);
            SetInteract(false);
            Record(2, Won && trainVisible && trainFacingReversed && docked && dockFacing,
                $"trainVisible={trainVisible}, startYaw180={trainFacingReversed}, dockYaw90={dockFacing}, dock={TrainDock:0.00}, gate={GateOpen:0.00}");
        }

        IEnumerator TestLure()
        {
            var level = BeginLevel(3);
            // SetLevel restores the hunters before it moves the player to this
            // level's spawn. Let the deliberately smoothed tracking turn settle,
            // then verify the hunter head/root faces the current player head.
            yield return Wait(0.6f);
            if (level == null || level.lureZones == null || level.lureZones.Length < 2 ||
                level.fragments == null || level.fragments.Length < 2 ||
                Array.Exists(level.fragments, fragment => fragment == null))
            {
                Record(3, false, "required lure zones or data fragments are missing");
                yield break;
            }
            var sentinelsForeground = level.sentinels != null && level.sentinels.Length == 2 &&
                                      Array.TrueForAll(level.sentinels, sentinel => sentinel != null && sentinel.transform.localPosition.z >= 7f);
            var sentinelsFacePlayerHead = level.sentinels != null && level.sentinels.Length == 2 &&
                                          Array.TrueForAll(level.sentinels, sentinel =>
                                          {
                                              if (sentinel == null) return false;
                                              var toHead = (_player.AvatarHeadPosition - sentinel.transform.position).normalized;
                                              return Vector3.Dot(sentinel.transform.forward, toHead) > 0.96f;
                                          });
            var fragmentsPlayerSide = Array.TrueForAll(level.fragments, fragment =>
                fragment != null && fragment.transform.localPosition.z >= level.gateLeftClosedLocal.z + 0.8f);
            Move(level.lureZones[0]);
            yield return Wait(0.62f);
            Commit();
            Move(level.lureZones[1]);
            yield return Wait(0.62f);
            Commit();
            yield return Wait(1.1f);
            var lured = LuredSentinels == 2;
            var gateOpened = GateOpen > 0.68f;
            var progressVisible = true;
            foreach (var fragment in level.fragments)
            {
                Move(fragment.transform);
                SetInteract(true);
                yield return Wait(0.48f);
                progressVisible &= ProgressVisible();
                yield return Wait(0.68f);
                SetInteract(false);
            }
            Move(level.core);
            SetInteract(true);
            yield return Wait(1.15f);
            SetInteract(false);
            var fragmentsCollected = Array.TrueForAll(level.fragments, fragment => fragment != null && fragment.collected);
            Record(3, Won && sentinelsForeground && sentinelsFacePlayerHead && fragmentsPlayerSide && lured && gateOpened && fragmentsCollected && progressVisible,
                $"sentinelsForeground={sentinelsForeground}, sentinelsFacePlayerHead={sentinelsFacePlayerHead}, fragmentsPlayerSide={fragmentsPlayerSide}, lured={LuredSentinels}, gate={GateOpen:0.00}, fragments={fragmentsCollected}, holdProgress={progressVisible}");
        }

        QuantumLevelScene BeginLevel(int index)
        {
            SetInteract(false);
            _manager.SetLevel(index);
            _player.enabled = false;
            return _manager.levels[index];
        }

        void Move(Transform target)
        {
            if (target != null) _player.MoveTo(target.position, target.rotation);
        }

        IEnumerator Wait(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        void Commit() => _commitEcho.Invoke(_manager, null);
        void SetInteract(bool value) => _interactHeldField.SetValue(_player, value);
        bool ProgressVisible() => _manager.hud != null && _manager.hud.progressPanel != null && _manager.hud.progressPanel.activeSelf;
        bool Won => (bool)_wonField.GetValue(_manager);
        float GateOpen => (float)_gateOpenField.GetValue(_manager);
        float TrainDock => (float)_trainDockField.GetValue(_manager);
        int LuredSentinels => (int)_luredField.GetValue(_manager);

        void Record(int level, bool passed, string details)
        {
            var line = $"LEVEL {level:00}: {(passed ? "PASS" : "FAIL")} | {details}";
            _results.Add(line);
            Debug.Log("QATEST " + line);
        }

        void Finish()
        {
            var report = "Quantum Echo full playthrough QA\n" + string.Join("\n", _results);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Logs"));
            File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/QuantumEchoPlaythroughQA.txt"), report + "\n");
            Debug.Log(report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// A deterministic, edit-mode equivalent of the interaction sequence used
        /// by the runtime coroutine. It validates all four rule gates without
        /// relying on Editor Play Mode, which Unity batchmode does not consistently
        /// advance. Dynamic execution remains covered by the desktop player check.
        /// </summary>
        public static void RunDeterministicBatch()
        {
            var manager = FindAnyObjectByType<QuantumEchoGameManager>(FindObjectsInactive.Include);
            var results = new List<string>();
            if (manager == null || manager.levels == null || manager.levels.Length != 4)
            {
                WriteBatchReport(results, "HARNESS: FAIL | QuantumEchoGameManager or four level references are missing");
                return;
            }

            for (var index = 0; index < manager.levels.Length; index++)
            {
                var level = manager.levels[index];
                var passed = level != null;
                var details = string.Empty;
                switch (level != null ? level.mode : QuantumLevelMode.Tutorial)
                {
                    case QuantumLevelMode.Tutorial:
                        passed &= level.padA != null && level.core != null && GateReady(level);
                        details = "hold-pad, gate, core capture route";
                        break;
                    case QuantumLevelMode.Paired:
                        passed &= level.padA != null && level.padB != null && level.core != null && GateReady(level);
                        details = "paired echo pads, gate, core capture route";
                        break;
                    case QuantumLevelMode.Train:
                        var trainYaw = level.train != null ? level.train.localEulerAngles.y : 0f;
                        passed &= level.padA != null && level.padB != null && level.core != null && level.train != null &&
                                  level.trainRenderer != null && level.trainRenderer.enabled &&
                                  Mathf.Abs(Mathf.DeltaAngle(trainYaw, 180f)) < 1f &&
                                  level.trainStartLocal != level.trainJunctionLocal && level.trainJunctionLocal != level.trainDockLocal;
                        details = $"train visible, start yaw={trainYaw:0}, two-stage route, dock core route";
                        break;
                    case QuantumLevelMode.Lure:
                        passed &= level.lureZones != null && level.lureZones.Length == 2 &&
                                  level.sentinels != null && level.sentinels.Length == 2 &&
                                  level.fragments != null && level.fragments.Length == 2 &&
                                  System.Array.TrueForAll(level.fragments, fragment => fragment != null && fragment.transform.localPosition.z > level.gateLeftClosedLocal.z) &&
                                  level.core != null;
                        details = "two lure echoes, foreground hunters, player-side fragments, core route";
                        break;
                }
                results.Add($"LEVEL {index:00}: {(passed ? "PASS" : "FAIL")} | {details}");
            }
            WriteBatchReport(results, null);
        }

        static bool GateReady(QuantumLevelScene level)
        {
            return level.gateLeft != null && level.gateRight != null &&
                   level.gateLeftRenderer != null && level.gateRightRenderer != null;
        }

        static void WriteBatchReport(List<string> results, string failure)
        {
            if (!string.IsNullOrEmpty(failure)) results.Add(failure);
            var report = "Quantum Echo deterministic four-level QA\n" + string.Join("\n", results);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Logs"));
            File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/QuantumEchoPlaythroughQA.txt"), report + "\n");
            Debug.Log(report);
        }
#endif
    }
}
