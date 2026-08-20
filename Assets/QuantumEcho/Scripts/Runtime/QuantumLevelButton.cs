using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    [RequireComponent(typeof(Button))]
    public sealed class QuantumLevelButton : MonoBehaviour
    {
        public int levelIndex;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
                FindAnyObjectByType<QuantumEchoGameManager>()?.SetLevel(levelIndex));
        }
    }
}
