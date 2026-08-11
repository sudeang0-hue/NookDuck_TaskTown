//NB

using System.Collections.Generic;
using UnityEngine;

public class GlobalUIStackManager : MonoBehaviour
{
    private static GlobalUIStackManager _instance;

    // 앱 종료 및 씬 전환 파괴 상태를 추적하는 정적 플래그 (메모리 오버헤드 $\approx 0\text{ bytes}$)
    private static bool _isQuitting = false;

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

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
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
        if (ui == null || _activeUIList.Contains(ui)) return;

        _activeUIList.Add(ui);
    }

    public void UnregisterUI(UIStackMember ui)
    {
        if (ui == null) return;

        _activeUIList.Remove(ui);
    }

    private void PopAndCloseTopUI()
    {
        int lastIndex = _activeUIList.Count - 1;

        if (lastIndex < 0) return;

        UIStackMember topUI = _activeUIList[lastIndex];
        if (topUI != null)
        {
            topUI.CloseSelf();
        }
    }

    // 유니티 에디터 플레이 종료 또는 앱 종료 시 자동 호출되는 생명주기 함수
    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    // 싱글톤 오브젝트가 파괴될 때 플래그 동기화
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _isQuitting = true;
        }
    }
}