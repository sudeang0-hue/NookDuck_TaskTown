using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// GameInputGate가 닫힌 짧은 시간 동안 기존 입력 컴포넌트를 비활성화하고,
    /// Gate가 열리면 원래 활성 상태로 복원하는 런타임 전용 드라이버입니다.
    /// </summary>
    [DefaultExecutionOrder(-30000)]
    internal sealed class GameInputGateDriver : MonoBehaviour
    {
        private static GameInputGateDriver instance;

        private readonly List<Behaviour> disabledBehaviours = new();

        internal static void EnsureInstance()
        {
            if (!Application.isPlaying || instance != null)
                return;

            GameObject driverObject = new("[GameInputGate]");
            instance = driverObject.AddComponent<GameInputGateDriver>();
            DontDestroyOnLoad(driverObject);
        }

        internal static void ReleaseInstance()
        {
            if (instance == null)
                return;

            instance.RestoreDisabledBehaviours();
            Destroy(instance.gameObject);
            instance = null;
        }

        internal static void ResetStatics()
        {
            instance = null;
        }

        private void Update()
        {
            if (GameInputGate.IsInputAllowed)
            {
                ReleaseInstance();
                return;
            }

            DisableGameplayInputBehaviours();
        }

        private void OnDestroy()
        {
            RestoreDisabledBehaviours();

            if (instance == this)
                instance = null;
        }

        private void DisableGameplayInputBehaviours()
        {
            DisableEnabledBehaviours(FindObjectsByType<GlobalInputManager>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<TypingInputFilter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<TargetSelector>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<ObjectDragger>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<VillageInfoUI_Manager>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<GachaDirector>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<GameMasterManager>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
            DisableEnabledBehaviours(FindObjectsByType<EventSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None));
        }

        private void DisableEnabledBehaviours<T>(T[] behaviours) where T : Behaviour
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                T behaviour = behaviours[i];
                if (behaviour == null || behaviour == this || !behaviour.enabled)
                    continue;

                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);
            }
        }

        private void RestoreDisabledBehaviours()
        {
            for (int i = 0; i < disabledBehaviours.Count; i++)
            {
                Behaviour behaviour = disabledBehaviours[i];
                if (behaviour != null)
                    behaviour.enabled = true;
            }

            disabledBehaviours.Clear();
        }
    }
}
