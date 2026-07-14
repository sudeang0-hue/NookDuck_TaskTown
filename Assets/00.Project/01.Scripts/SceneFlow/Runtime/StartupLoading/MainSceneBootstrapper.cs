using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    /// <summary>
    /// Main Scene에 연결하면 IMainSceneInitializer 구현체를 순서대로 초기화합니다.
    /// 기존 MainScene은 이번 작업에서 수정하지 않으므로 저장 시스템 연결 시 추가합니다.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class MainSceneBootstrapper : MonoBehaviour
    {
        public static event Action<GameSession> GameReady;

        public bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            GameReady = null;
        }

        private void Start()
        {
            InitializeScene();
        }

        public void InitializeScene()
        {
            if (IsInitialized)
            {
                return;
            }

            GameSession session = GameSessionStore.Current;
            if (session == null || !session.IsReady)
            {
                Debug.LogWarning(
                    "[MainSceneBootstrapper] 준비된 GameSession이 없습니다. " +
                    "MainScene을 직접 실행한 경우일 수 있습니다.",
                    this);
                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            List<IMainSceneInitializer> initializers = new();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                if (behaviour is IMainSceneInitializer initializer)
                {
                    initializers.Add(initializer);
                }
            }

            initializers.Sort((left, right) =>
                left.InitializationOrder.CompareTo(right.InitializationOrder));

            for (int i = 0; i < initializers.Count; i++)
            {
                initializers[i].Initialize(session);
            }

            IsInitialized = true;
            GameReady?.Invoke(session);
        }
    }
}
