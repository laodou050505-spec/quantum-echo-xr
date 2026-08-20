using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace QuantumEcho.Editor
{
    /// <summary>
    /// Keeps the local Game view at native 1x zoom.  Game view zoom is editor
    /// layout state rather than gameplay Transform scale, and a 2x layout makes
    /// the desktop preview look artificially soft.
    /// </summary>
    [InitializeOnLoad]
    public static class QuantumEchoGameViewScaleOneLock
    {
        static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        static double _nextRefresh;
        static bool _verified;

        static QuantumEchoGameViewScaleOneLock()
        {
            EditorApplication.delayCall += SetScaleToOne;
            EditorApplication.update += KeepScaleAtOne;
        }

        [MenuItem("Quantum Echo/Set Game View Scale to 1x")]
        public static void SetScaleToOne()
        {
            if (GameViewType == null) return;
            var zoomField = GameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            if (zoomField == null) return;
            var lowResolutionProperty = GameViewType.GetProperty(
                "lowResolutionForAspectRatios",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var gameView in Resources.FindObjectsOfTypeAll(GameViewType))
            {
                // On Retina displays Unity's Low Resolution Aspect Ratios mode
                // clamps the Game view minimum to the 2x backing scale. Disable
                // that mode before applying a real 1x zoom.
                if (lowResolutionProperty?.CanWrite == true)
                    lowResolutionProperty.SetValue(gameView, false);

                var defaultScaleField = GameViewType.GetField("m_defaultScale", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultScaleField != null && defaultScaleField.FieldType == typeof(float))
                    defaultScaleField.SetValue(gameView, 1f);

                var zoomArea = zoomField.GetValue(gameView);
                var scaleField = zoomArea?.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
                if (scaleField == null) continue;

                var scale = (Vector2)scaleField.GetValue(zoomArea);
                if ((scale - Vector2.one).sqrMagnitude >= 0.0001f)
                    scaleField.SetValue(zoomArea, Vector2.one);
                ((EditorWindow)gameView).Repaint();

                if (_verified) continue;
                var minScaleProperty = GameViewType.GetProperty(
                    "minScale",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var minScale = minScaleProperty?.GetValue(gameView) is float value ? value : -1f;
                var lowResolution = lowResolutionProperty?.GetValue(gameView) is bool enabled && enabled;
                var actualScale = (Vector2)scaleField.GetValue(zoomArea);
                Debug.Log(
                    $"Quantum Echo Game View verified: scale={actualScale.y:0.##}x, " +
                    $"minimum={minScale:0.##}x, lowResolution={lowResolution}, defaultScale=1x.");
                _verified = true;
            }
        }

        static void KeepScaleAtOne()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + 0.25d;
            SetScaleToOne();
        }
    }
}
