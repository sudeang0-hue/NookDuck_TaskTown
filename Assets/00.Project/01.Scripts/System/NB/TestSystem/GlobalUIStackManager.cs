//NB

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GlobalUIStackManager : MonoBehaviour
{
    private static GlobalUIStackManager _instance;

    // 앱 종료 중 UI 비활성화 콜백에서 싱글톤이 다시 생성되지 않도록 막습니다.
    private static bool _isQuitting;

    public static GlobalUIStackManager Instance
    {
        get
        {
            // 앱이 종료되는 중이라면 절대로 새 GameObject를 생성하지 않고 null을 반환합니다.
            if (_isQuitting)
            {
                Debug.LogWarning("[UIStackManager] 앱이 종료 중이므로 인스턴스 생성을 차단합니다.");
                return null;
            }

            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<GlobalUIStackManager>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("@GlobalUIStackManager");
                    _instance = singletonObject.AddComponent<GlobalUIStackManager>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    private readonly List<UIStackMember> _activeUIList = new List<UIStackMember>();

    public static bool TryGetExisting(out GlobalUIStackManager manager)
    {
        manager = _instance;
        return manager != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isQuitting = false;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (_instance == this)
            return;

        // MainScene 재진입 시 같은 GameObject의 다른 매니저까지 제거하지 않습니다.
        Destroy(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PopAndCloseTopUI();
        }
    }

    public void RegisterUI(UIStackMember ui)
    {
        RemoveInvalidStackEntries();

        if (ui == null || _activeUIList.Contains(ui))
            return;

        _activeUIList.Add(ui);
    }

    public void UnregisterUI(UIStackMember ui)
    {
        if (ui == null)
        {
            RemoveInvalidStackEntries();
            return;
        }

        _activeUIList.Remove(ui);
    }

    private void PopAndCloseTopUI()
    {
        RemoveInvalidStackEntries();

        int lastIndex = _activeUIList.Count - 1;

        if (lastIndex < 0)
            return;

        _activeUIList[lastIndex].CloseSelf();
    }

    private void RemoveInvalidStackEntries()
    {
        for (int i = _activeUIList.Count - 1; i >= 0; i--)
        {
            UIStackMember ui = _activeUIList[i];
            if (ui == null || !ui.isActiveAndEnabled)
                _activeUIList.RemoveAt(i);
        }
    }

    // 유니티 에디터 플레이 종료 또는 앱 종료 시 자동 호출되는 생명주기 함수
    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
