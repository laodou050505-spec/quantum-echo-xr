using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumEcho
{
    public sealed class QuantumEchoGameManager : MonoBehaviour
    {
        const int MaxEchoes = 4;
        const float SampleInterval = 1f / 30f;
        const float HoldDuration = 0.9f;
        const float TrainForwardYaw = 180f;

        [Header("Scene References")]
        public QuantumLevelScene[] levels;
        public QuantumPlayerRig player;
        public QuantumHUD hud;
        public GameObject echoTemplate;
        public Material echoTrailMaterial;

        [Header("Audio From ange-embed")]
        public AudioSource ambientSource;
        public AudioSource effectsSource;
        public AudioClip confirmClip;
        public AudioClip errorClip;
        public AudioClip unlockClip;
        public AudioClip interactClip;

        [Header("Startup")]
        [Range(0, 3)] public int startingLevel;

        readonly List<EchoRecording> _echoRecordings = new List<EchoRecording>();
        readonly List<EchoRuntime> _echoes = new List<EchoRuntime>();
        readonly List<EchoSample> _currentRecording = new List<EchoSample>();

        QuantumLevelScene _level;
        int _currentLevelIndex;
        float _loopElapsed;
        float _totalElapsed;
        float _lastSampleTime;
        float _gateOpenAmount;
        float _targetGateOpen;
        float _coreCaptureProgress;
        float _trainProgress;
        float _trainDockProgress;
        float _lureCatchCooldown;
        readonly RaycastHit[] _sentinelSweepHits = new RaycastHit[32];
        bool _interactionLatched;
        bool _padAActive;
        bool _padBActive;
        bool _previousPadA;
        bool _previousPadB;
        bool _trainAtJunction;
        bool _trainSwitchSeen;
        bool _trainFaulted;
        bool _trainDockAnnounced;
        bool _won;
        bool _advancingLevel;
        float _advanceLevelAt;
        int _luredSentinels;

        const float LevelAdvanceFeedbackSeconds = 1.8f;

        [Serializable]
        sealed class EchoRecording
        {
            public readonly List<EchoSample> samples = new List<EchoSample>();
        }

        struct EchoSample
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
            public bool interact;
        }

        sealed class EchoRuntime
        {
            public GameObject visual;
            public LineRenderer trail;
            public bool interact;
        }

        void Start()
        {
            SetLevel(startingLevel);
            if (ambientSource != null && ambientSource.clip != null && !ambientSource.isPlaying) ambientSource.Play();
        }

        void Update()
        {
            if (player.LevelSelectPressed >= 0)
            {
                SetLevel(player.LevelSelectPressed);
                return;
            }

            if (_won)
            {
                if (player.CommitPressed || player.UndoPressed)
                    hud.ShowToast(_currentLevelIndex < levels.Length - 1 ? "本关已完成，正在进入下一关…" : "回声任务已全部完成");
                // Earlier levels advance on their own after a short, readable
                // success beat.  No extra controller button is required, avoiding
                // a confusing pause between rooms in PICO.
                if (_advancingLevel && Time.unscaledTime >= _advanceLevelAt)
                {
                    SetLevel(_currentLevelIndex + 1);
                    return;
                }
                if (player.ResetPressed) SetLevel(_currentLevelIndex);
                return;
            }

            if (player.ResetPressed)
            {
                ResetExperiment();
                return;
            }
            if (player.UndoPressed)
            {
                UndoEcho();
                return;
            }
            if (player.CommitPressed)
            {
                CommitEcho();
                return;
            }

            _loopElapsed += Time.deltaTime;
            _totalElapsed += Time.deltaTime;
            if (_level.mode != QuantumLevelMode.Lure && player.InteractPressed) _interactionLatched = !_interactionLatched;
            if (player.IsMoving && !player.InteractHeld) _interactionLatched = false;

            UpdateEchoes(_loopElapsed);
            RecordSample();
            UpdatePuzzle(Time.deltaTime);
            UpdateHud();

            if (_loopElapsed >= _level.loopDuration)
            {
                Play(errorClip);
                hud.ShowToast("回路失稳：当前动作未封存");
                ResetCycle();
            }
        }

        public void SetLevel(int levelIndex)
        {
            if (levels == null || levels.Length == 0) return;
            _currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
            for (var i = 0; i < levels.Length; i++) levels[i].gameObject.SetActive(i == _currentLevelIndex);
            _level = levels[_currentLevelIndex];
            player.SetDesignStart(_level.designPlayerStart);
            hud.SetWorldAnchor(_level.worldTaskTerminalAnchor);
            ClearEchoes();
            _totalElapsed = 0f;
            _won = false;
            _advancingLevel = false;
            if (_level.core != null) _level.core.gameObject.SetActive(true);
            hud.HideResult();
            ResetCycle();
            hud.ShowToast("回路已启动：按左上角面板中的手柄键位操作", 3.2f);
        }

        void ResetExperiment()
        {
            ClearEchoes();
            _totalElapsed = 0f;
            _won = false;
            _advancingLevel = false;
            if (_level.core != null) _level.core.gameObject.SetActive(true);
            hud.HideResult();
            ResetCycle();
            Play(interactClip);
            hud.ShowToast("实验已重置");
        }

        void ResetCycle()
        {
            _loopElapsed = 0f;
            _lastSampleTime = float.NegativeInfinity;
            _currentRecording.Clear();
            _interactionLatched = false;
            _gateOpenAmount = 0f;
            _targetGateOpen = 0f;
            _coreCaptureProgress = 0f;
            _padAActive = false;
            _padBActive = false;
            _previousPadA = false;
            _previousPadB = false;
            ResetTrain();
            ResetLure();
            player.MoveToDesignStart();
            foreach (var echo in _echoes) echo.visual.SetActive(true);
            RecordSample(true);
            hud.SetProgress(false);
            UpdateHud();
        }

        void ResetTrain()
        {
            _trainProgress = 0f;
            _trainDockProgress = 0f;
            _trainAtJunction = false;
            _trainSwitchSeen = false;
            _trainFaulted = false;
            _trainDockAnnounced = false;
            if (_level.train == null) return;
            _level.train.gameObject.SetActive(true);
            foreach (var renderer in _level.train.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                if (_level.trainRenderer == null) _level.trainRenderer = renderer;
            }
            _level.train.localPosition = _level.trainStartLocal;
            _level.train.localRotation = Quaternion.Euler(0f, TrainForwardYaw, 0f);
            SetEmission(_level.trainRenderer, new Color(0.26f, 0.9f, 0.82f), 1.4f);
        }

        void ResetLure()
        {
            _luredSentinels = 0;
            _lureCatchCooldown = 3.2f;
            if (_level.sentinels != null)
            {
                foreach (var sentinel in _level.sentinels)
                {
                    if (sentinel == null) continue;
                    sentinel.transform.localPosition = sentinel.homeLocalPosition;
                    sentinel.collected = false;
                    SetEmission(sentinel.mainRenderer, new Color(1f, 0.2f, 0.25f), 1.5f);
                    // Track the controlled avatar's head, never the HMD. In the
                    // PICO comfort camera the viewer can look around freely from a
                    // stable observation point without making an enemy jerk around.
                    FaceSentinelToward(sentinel.transform, player.AvatarHeadPosition, 1f, true);
                }
            }
            if (_level.fragments == null) return;
            foreach (var fragment in _level.fragments)
            {
                if (fragment == null) continue;
                fragment.collected = false;
                fragment.captureProgress = 0f;
                fragment.gameObject.SetActive(true);
                if (fragment.halo != null) fragment.halo.localScale = fragment.haloBaseScale;
                SetEmission(fragment.mainRenderer, new Color(0.26f, 0.9f, 0.82f), 1.9f);
            }
        }

        void RecordSample(bool force = false)
        {
            if (!force && _loopElapsed - _lastSampleTime < SampleInterval) return;
            _currentRecording.Add(new EchoSample
            {
                time = _loopElapsed,
                position = player.GroundPosition,
                rotation = player.FacingRotation,
                interact = _interactionLatched || player.InteractHeld
            });
            _lastSampleTime = _loopElapsed;
        }

        void CommitEcho()
        {
            RecordSample(true);
            if (_currentRecording.Count < 5 || _loopElapsed < 0.35f)
            {
                Play(errorClip);
                hud.ShowToast("当前回路太短，尚未形成稳定回声");
                return;
            }
            if (_echoRecordings.Count >= MaxEchoes)
            {
                Play(errorClip);
                hud.ShowToast("回声槽已满，请撤销一个回声");
                return;
            }

            var recording = new EchoRecording();
            recording.samples.AddRange(_currentRecording);
            _echoRecordings.Add(recording);
            CreateEchoVisual(recording, _echoRecordings.Count - 1);
            Play(unlockClip);
            player.PulseHaptics(0.55f, 0.10f);
            hud.ShowToast($"回声 {_echoRecordings.Count} 已封存");
            ResetCycle();
        }

        void CreateEchoVisual(EchoRecording recording, int index)
        {
            var visual = Instantiate(echoTemplate);
            visual.name = $"Runtime Echo {index + 1:00}";
            visual.SetActive(true);
            var trailObject = new GameObject($"Echo Trail {index + 1:00}");
            var trail = trailObject.AddComponent<LineRenderer>();
            trail.material = echoTrailMaterial;
            trail.widthMultiplier = 0.035f;
            trail.positionCount = Mathf.Max(2, Mathf.CeilToInt(recording.samples.Count / 4f));
            for (var point = 0; point < trail.positionCount; point++)
            {
                var sampleIndex = Mathf.Min(recording.samples.Count - 1, point * 4);
                var position = recording.samples[sampleIndex].position + Vector3.up * (0.035f + index * 0.012f);
                trail.SetPosition(point, position);
            }
            _echoes.Add(new EchoRuntime { visual = visual, trail = trail });
        }

        void UndoEcho()
        {
            if (_echoRecordings.Count == 0)
            {
                Play(errorClip);
                hud.ShowToast("没有可以撤销的回声");
                return;
            }
            var last = _echoes.Count - 1;
            Destroy(_echoes[last].visual);
            Destroy(_echoes[last].trail.gameObject);
            _echoes.RemoveAt(last);
            _echoRecordings.RemoveAt(_echoRecordings.Count - 1);
            Play(interactClip);
            player.PulseHaptics(0.32f, 0.07f);
            hud.ShowToast("最后一个回声已撤销");
            ResetCycle();
        }

        void ClearEchoes()
        {
            foreach (var echo in _echoes)
            {
                if (echo.visual != null) Destroy(echo.visual);
                if (echo.trail != null) Destroy(echo.trail.gameObject);
            }
            _echoes.Clear();
            _echoRecordings.Clear();
        }

        void UpdateEchoes(float time)
        {
            for (var index = 0; index < _echoes.Count; index++)
            {
                var sample = SampleRecording(_echoRecordings[index], time);
                _echoes[index].visual.transform.SetPositionAndRotation(sample.position, sample.rotation);
                _echoes[index].interact = sample.interact;
            }
        }

        static EchoSample SampleRecording(EchoRecording recording, float time)
        {
            var samples = recording.samples;
            if (time <= samples[0].time) return samples[0];
            if (time >= samples[samples.Count - 1].time) return samples[samples.Count - 1];
            var nextIndex = 1;
            while (nextIndex < samples.Count && samples[nextIndex].time < time) nextIndex++;
            var previous = samples[nextIndex - 1];
            var next = samples[nextIndex];
            var alpha = Mathf.InverseLerp(previous.time, next.time, time);
            return new EchoSample
            {
                time = time,
                position = Vector3.Lerp(previous.position, next.position, alpha),
                rotation = Quaternion.Slerp(previous.rotation, next.rotation, alpha),
                interact = alpha < 0.5f ? previous.interact : next.interact
            };
        }

        void UpdatePuzzle(float delta)
        {
            hud.SetProgress(false);
            _padAActive = _level.mode == QuantumLevelMode.Tutorial
                ? AnyAvatarOnPad(_level.padA, false)
                : AnyAvatarOnPad(_level.padA, true);
            _padBActive = AnyAvatarOnPad(_level.padB, true);
            UpdatePadVisual(_level.padARenderer, _padAActive);
            UpdatePadVisual(_level.padBRenderer, _padBActive);

            if (_padAActive && !_previousPadA) PadActivated();
            if (_padBActive && !_previousPadB) PadActivated();
            _previousPadA = _padAActive;
            _previousPadB = _padBActive;

            switch (_level.mode)
            {
                case QuantumLevelMode.Tutorial:
                    _targetGateOpen = _padAActive ? 1f : 0f;
                    break;
                case QuantumLevelMode.Paired:
                    _targetGateOpen = _padAActive && _padBActive ? 1f : 0f;
                    break;
                case QuantumLevelMode.Train:
                    UpdateTrain(delta);
                    break;
                case QuantumLevelMode.Lure:
                    if (!UpdateLure(delta)) return;
                    break;
            }

            UpdateGate(delta);
            UpdateCoreCapture(delta);
        }

        bool AnyAvatarOnPad(Transform pad, bool requireInteraction)
        {
            if (pad == null) return false;
            var playerOnPad = FlatDistance(player.GroundPosition, pad.position) < 0.92f;
            if (playerOnPad && (!requireInteraction || _interactionLatched || player.InteractHeld)) return true;
            foreach (var echo in _echoes)
            {
                if (FlatDistance(echo.visual.transform.position, pad.position) < 0.92f && (!requireInteraction || echo.interact)) return true;
            }
            return false;
        }

        bool AnyEchoOnPad(Transform pad)
        {
            if (pad == null) return false;
            foreach (var echo in _echoes)
            {
                if (echo.interact && FlatDistance(echo.visual.transform.position, pad.position) < 0.92f) return true;
            }
            return false;
        }

        void UpdateTrain(float delta)
        {
            if (_level.train == null) return;
            var firstEchoReady = _echoRecordings.Count >= 1 && AnyEchoOnPad(_level.padA);
            var secondEchoReady = _echoRecordings.Count >= 2 && AnyEchoOnPad(_level.padB);
            if (firstEchoReady && !_trainAtJunction && !_trainFaulted)
            {
                _trainProgress = Mathf.Min(1f, _trainProgress + delta * 0.22f);
                _level.train.localPosition = Vector3.Lerp(_level.trainStartLocal, _level.trainJunctionLocal, _trainProgress);
            }
            if (secondEchoReady && !_trainSwitchSeen)
            {
                _trainSwitchSeen = true;
                if (_trainProgress < 0.78f)
                {
                    _trainFaulted = true;
                    SetEmission(_level.trainRenderer, new Color(1f, 0.2f, 0.25f), 2f);
                    Play(errorClip);
                    hud.ShowToast("换轨过早：让第二段路线更晚抵达，撤销后重录");
                }
                else
                {
                    _trainAtJunction = true;
                    Play(unlockClip);
                    hud.ShowToast("换轨节拍命中：列车驶向核心泊位");
                }
            }
            if (_trainAtJunction && !_trainFaulted)
            {
                _trainDockProgress = Mathf.Min(1f, _trainDockProgress + delta * 0.34f);
                _level.train.localPosition = Vector3.Lerp(_level.trainJunctionLocal, _level.trainDockLocal, _trainDockProgress);
                _level.train.localRotation = Quaternion.Euler(0f, TrainForwardYaw - 90f * _trainDockProgress, 0f);
            }
            var docked = _trainAtJunction && _trainDockProgress > 0.96f;
            _targetGateOpen = docked ? 1f : 0f;
            if (docked && !_trainDockAnnounced)
            {
                _trainDockAnnounced = true;
                Play(confirmClip);
                hud.ShowToast("列车已停靠核心泊位");
            }
        }

        bool UpdateLure(float delta)
        {
            if (_level == null || player == null) return false;
            _lureCatchCooldown = Mathf.Max(0f, _lureCatchCooldown - delta);
            if (_level.sentinels == null) return false;
            for (var index = 0; index < _level.sentinels.Length; index++)
            {
                var sentinel = _level.sentinels[index];
                if (sentinel == null) continue;
                var sentinelTransform = sentinel.transform;
                if (sentinelTransform == null) continue;
                var lureZone = _level.lureZones != null && index < _level.lureZones.Length ? _level.lureZones[index] : null;
                var canLure = lureZone != null && index < _echoRecordings.Count && RecordingVisitsZone(_echoRecordings[index], lureZone);
                if (canLure && !sentinel.collected)
                {
                    sentinel.collected = true;
                    _luredSentinels++;
                    SetEmission(sentinel.mainRenderer, new Color(0.88f, 0.32f, 0.74f), 1.7f);
                    Play(unlockClip);
                    hud.ShowToast($"追猎体 {index + 1} 已锁定回声诱饵");
                }
                var target = sentinel.collected && lureZone != null
                    ? lureZone.position + Vector3.up * 0.8f
                    : player.AvatarHeadPosition;
                var sentinelPosition = sentinelTransform.position;
                var movementTarget = new Vector3(target.x, sentinelPosition.y, target.z);
                var next = Vector3.MoveTowards(sentinelPosition, movementTarget, delta * (sentinel.collected ? 1.5f : 1.45f));
                next.y = sentinelPosition.y;
                MoveSentinelWithCollision(sentinelTransform, next);
                FaceSentinelToward(sentinelTransform, target, delta);
            }

            if (_level.fragments == null) return true;
            foreach (var fragment in _level.fragments)
            {
                if (fragment == null) continue;
                if (fragment.collected) continue;
                var inRange = IsTargetedOrNear(fragment.transform, 0.82f, 3.2f);
                var extracting = inRange && player.InteractHeld;
                fragment.captureProgress = Mathf.Clamp01(fragment.captureProgress + (extracting ? delta / HoldDuration : -delta * 2.2f));
                if (fragment.halo != null) fragment.halo.localScale = fragment.haloBaseScale * (1f + fragment.captureProgress * 0.75f);
                SetEmission(fragment.mainRenderer, new Color(0.26f, 0.9f, 0.82f), 1.9f + fragment.captureProgress * 2.3f);
                if (inRange) hud.SetProgress(true, "【右扳机·持续按住】提取失落数据", fragment.captureProgress);
                if (fragment.captureProgress >= 1f)
                {
                    fragment.collected = true;
                    fragment.gameObject.SetActive(false);
                    Play(confirmClip);
                    player.PulseHaptics(0.55f, 0.1f);
                    hud.ShowToast("失落数据已回收");
                }
            }

            // The door remains the reward for diverting both hunters. The two blue
            // fragments stay visible on the player side so the next objective is clear.
            _targetGateOpen = _luredSentinels >= 2 ? 1f : 0f;
            foreach (var sentinel in _level.sentinels)
            {
                if (sentinel == null) continue;
                if (!sentinel.collected && _lureCatchCooldown <= 0f && FlatDistance(player.GroundPosition, sentinel.transform.position) < 1.50f)
                {
                    Play(errorClip);
                    hud.ShowToast("本体被追猎体锁定：先录制诱饵路线");
                    ResetCycle();
                    return false;
                }
            }
            return true;
        }

        void MoveSentinelWithCollision(Transform sentinel, Vector3 destination)
        {
            var hull = sentinel.GetComponentInChildren<BoxCollider>();
            var displacement = destination - sentinel.position;
            if (hull == null || !hull.enabled || hull.isTrigger || displacement.sqrMagnitude < 0.000001f)
            {
                sentinel.position = destination;
                return;
            }
            var distance = displacement.magnitude;
            var direction = displacement / distance;
            var bounds = hull.bounds;
            var travel = distance;
            var count = Physics.BoxCastNonAlloc(bounds.center, bounds.extents, direction,
                _sentinelSweepHits, Quaternion.identity, distance + 0.02f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i=0; i<count; i++)
            {
                var hit = _sentinelSweepHits[i];
                if (hit.collider.transform.IsChildOf(sentinel)) continue;
                // Starting contact with the deck must not stop a hovering agent.
                if (Vector3.Dot(hit.normal, direction) >= -0.01f) continue;
                travel = Mathf.Min(travel, Mathf.Max(0f, hit.distance - 0.02f));
            }
            sentinel.position += direction * travel;
        }

        static void FaceSentinelToward(Transform sentinel, Vector3 target, float delta, bool immediate = false)
        {
            if (sentinel == null) return;
            var direction = target - sentinel.position;
            if (direction.sqrMagnitude < 0.001f) return;
            var desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            sentinel.rotation = immediate
                ? desired
                : Quaternion.Slerp(sentinel.rotation, desired, 1f - Mathf.Exp(-9f * delta));
        }

        static bool RecordingVisitsZone(EchoRecording recording, Transform zone)
        {
            foreach (var sample in recording.samples)
            {
                if (FlatDistance(sample.position, zone.position) < 0.9f) return true;
            }
            return false;
        }

        void UpdateGate(float delta)
        {
            _gateOpenAmount = Mathf.Lerp(_gateOpenAmount, _targetGateOpen, 1f - Mathf.Exp(-5.5f * delta));
            _level.gateLeft.localPosition = _level.gateLeftClosedLocal + Vector3.left * (_gateOpenAmount * 1.56f);
            _level.gateRight.localPosition = _level.gateRightClosedLocal + Vector3.right * (_gateOpenAmount * 1.56f);
            SetEmission(_level.gateLeftRenderer, new Color(0.26f, 0.9f, 0.82f), 0.22f + _gateOpenAmount * 1.15f);
            SetEmission(_level.gateRightRenderer, new Color(0.26f, 0.9f, 0.82f), 0.22f + _gateOpenAmount * 1.15f);
        }

        void UpdateCoreCapture(float delta)
        {
            var inRange = IsCoreReady() && IsTargetedOrNear(_level.core, 1.8f, 3.5f);
            var capturing = inRange && player.InteractHeld;
            _coreCaptureProgress = Mathf.Clamp01(_coreCaptureProgress + (capturing ? delta / HoldDuration : -delta * 2.2f));
            if (inRange)
            {
                hud.SetProgress(true, capturing
                    ? "【右扳机·持续按住】正在回收量子核心"
                    : "【右扳机·持续按住】回收量子核心", _coreCaptureProgress);
            }
            if (_coreCaptureProgress >= 1f) WinLevel();
        }

        bool IsCoreReady()
        {
            if (_level.mode == QuantumLevelMode.Train) return _trainAtJunction && _trainDockProgress > 0.96f;
            if (_level.mode == QuantumLevelMode.Lure) return AllFragmentsCollected() && _gateOpenAmount > 0.68f;
            return _gateOpenAmount > 0.68f;
        }

        bool IsTargetedOrNear(Transform target, float nearDistance, float rayDistance)
        {
            return QuantumInteractionReach.CanReach(player, target, nearDistance, rayDistance);
        }

        bool AllFragmentsCollected()
        {
            if (_level.fragments == null || _level.fragments.Length == 0) return false;
            var validFragmentCount = 0;
            foreach (var fragment in _level.fragments)
            {
                if (fragment == null) continue;
                validFragmentCount++;
                if (!fragment.collected) return false;
            }
            if (validFragmentCount == 0) return false;
            return true;
        }

        void UpdateHud()
        {
            var timerLabel = _level.mode switch
            {
                QuantumLevelMode.Tutorial => "教学回路长度",
                QuantumLevelMode.Train => "本段节拍长度",
                QuantumLevelMode.Lure => "诱饵回路长度",
                _ => "回路稳定度"
            };
            hud.SetLevel(_level.levelCode, _echoRecordings.Count + 1, timerLabel);
            hud.SetTimer(_level.loopDuration - _loopElapsed, _level.loopDuration);
            hud.SetEchoCount(_echoRecordings.Count, MaxEchoes);
            UpdateObjective();
            UpdatePrompt();
        }

        void UpdateObjective()
        {
            var step = Mathf.Min(_echoRecordings.Count, 2);
            if (_level.mode == QuantumLevelMode.Tutorial && _echoRecordings.Count >= 1) step = _gateOpenAmount > 0.65f ? 2 : 1;
            if (_level.mode == QuantumLevelMode.Train && _trainAtJunction && _trainDockProgress > 0.94f) step = 2;
            if (_level.mode == QuantumLevelMode.Lure && _luredSentinels >= 2) step = 2;
            var objective = GetObjective(_currentLevelIndex, step);
            hud.SetObjective(step, objective.title, objective.copy);
        }

        void UpdatePrompt()
        {
            const string interactKey = "右扳机";
            const string commitKey = "握把 / A / X";
            if (IsCoreReady() && IsTargetedOrNear(_level.core, 1.8f, 3.5f))
            {
                hud.SetPrompt(interactKey, player.InteractHeld ? $"持续按住，核心回收 {Mathf.RoundToInt(_coreCaptureProgress * 100f)}%" : "回收量子核心");
                return;
            }
            if (_level.mode == QuantumLevelMode.Lure)
            {
                if (_level.fragments != null)
                {
                    foreach (var fragment in _level.fragments)
                    {
                        if (fragment == null) continue;
                        if (!fragment.collected && IsTargetedOrNear(fragment.transform, 0.82f, 3.2f))
                        {
                            hud.SetPrompt(interactKey, $"提取失落数据 {Mathf.RoundToInt(fragment.captureProgress * 100f)}%");
                            return;
                        }
                    }
                }
                if (_echoRecordings.Count < 2) hud.SetPrompt(commitKey, "跑入左右紫色残响区，分别封存两条诱饵路线");
                else if (_luredSentinels < 2) hud.SetPrompt("左摇杆", "保持距离，等待回声引走追猎体");
                else if (!AllFragmentsCollected()) hud.SetPrompt("右扳机", "靠近门前碎片并持续吸取");
                else hud.SetPrompt(interactKey, "穿过已开启的收容门并同步核心");
                return;
            }

            var nearA = _level.padA != null && FlatDistance(player.GroundPosition, _level.padA.position) < 1.25f;
            var nearB = _level.padB != null && FlatDistance(player.GroundPosition, _level.padB.position) < 1.25f;
            if (nearA || nearB)
            {
                if (_level.mode == QuantumLevelMode.Tutorial) hud.SetPrompt(commitKey, "保持器已踩住：封存这段站位");
                else hud.SetPrompt("右扳机", $"{(nearA ? "A" : "B")} 区同步{(_interactionLatched || player.InteractHeld ? "中" : "")}");
                return;
            }
            if (_level.mode == QuantumLevelMode.Train && _echoRecordings.Count >= 2 && !_trainDockAnnounced)
            {
                hud.SetPrompt("左摇杆", "移至安全观察位，等待列车靠站");
                return;
            }
            if (_echoRecordings.Count == 0)
                hud.SetPrompt("左摇杆", _level.mode == QuantumLevelMode.Tutorial ? "走上蓝色保持器" : "前往 A 区稳定器");
            else if (_gateOpenAmount > 0.65f)
                hud.SetPrompt("左摇杆", "穿过门，靠近量子核心");
            else
                hud.SetPrompt(commitKey, "封存当前动作，开始下一回路");
        }

        static (string title, string copy) GetObjective(int level, int step)
        {
            var objectives = new[]
            {
                new[]
                {
                    ("踩住保持器", "走进蓝色圆盘，门会保持开启；站上去后封存为回声。"),
                    ("让回声接手", "新回路开始后，回声会走回保持器。等门打开后，自己穿过去。"),
                    ("回收量子核心", "穿过已开启的教学力场，靠近并持续吸取核心。")
                },
                new[]
                {
                    ("记录左侧稳定器", "进入 A 区同步，然后封存当前回路。"),
                    ("记录右侧稳定器", "让回声同步 A 区；你在 B 区同步，再封存一次。"),
                    ("取回量子核心", "等待两个回声解除力场，靠近并持续吸取核心。")
                },
                new[]
                {
                    ("录制发车节拍", "到 A 枢纽同步后封存，它会让列车从始发站出发。"),
                    ("录制换轨节拍", "到 B 枢纽同步后再封存；这条路径要比第一条更晚抵达。"),
                    ("等待列车靠站", "回声会让列车先发车、再换轨。停靠后靠近并持续吸取核心。")
                },
                new[]
                {
                    ("录制左侧诱饵", "跑向左上方紫色残响区并封存，第一台追猎体会认错目标。"),
                    ("录制右侧诱饵", "下一回路跑向右上方残响区并封存，两台追猎体将分别被带走。"),
                    ("回收门前数据", "两台追猎体被引走后，持续吸取门前两枚蓝色碎片。")
                }
            };
            return objectives[Mathf.Clamp(level, 0, objectives.Length - 1)][Mathf.Clamp(step, 0, 2)];
        }

        void WinLevel()
        {
            if (_won) return;
            _won = true;
            _level.core.gameObject.SetActive(false);
            hud.SetProgress(false);
            Play(unlockClip);
            player.PulseHaptics(0.8f, 0.18f);
            var result = _currentLevelIndex switch
            {
                0 => "回声接力完成",
                1 => "核心回收完成",
                2 => "相位列车调度完成",
                _ => "失落数据已回收"
            };
            hud.ShowResult(result, _echoRecordings.Count, _totalElapsed, _currentLevelIndex == levels.Length - 1);
            if (_currentLevelIndex < levels.Length - 1)
            {
                _advancingLevel = true;
                _advanceLevelAt = Time.unscaledTime + LevelAdvanceFeedbackSeconds;
                hud.ShowLevelTransition(result, _currentLevelIndex + 2, LevelAdvanceFeedbackSeconds);
            }
        }

        void PadActivated()
        {
            Play(confirmClip);
            player.PulseHaptics(0.25f, 0.04f);
        }

        void Play(AudioClip clip)
        {
            if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip);
        }

        static void UpdatePadVisual(Renderer renderer, bool active)
        {
            if (renderer == null) return;
            SetEmission(renderer, active ? new Color(0.26f, 0.9f, 0.82f) : new Color(1f, 0.64f, 0.2f), active ? 2.5f : 0.2f);
        }

        static void SetEmission(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null) return;
            var material = renderer.material;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }
    }
}
