//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-200)]
public class GlobalUIStackManager : MonoBehaviour
{
    private static GlobalUIStackManager _instance;

    // 최신 유니티 API 명세가 적용된 싱글톤 프로퍼티
    public static GlobalUIStackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 1. CS0618 경고 해결: 정렬 오버헤드가 없는 FindAnyObjectByType 사용
                _instance = Object.FindAnyObjectByType<GlobalUIStackManager>();

                // 2. 씬에 매니저 오브젝트가 없을 경우 자생(Auto-Creation) 로직
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

    [Header("일시정지 / 옵션창 연동 설정")]
    [Tooltip("스택이 비어있을 때 직접 열어줄 옵션/일시정지 UI의 UIStackMember")]
    [SerializeField] private UIStackMember _pauseMenuUI;

    [Tooltip("스택이 비어있을 때 추가로 실행할 이벤트 (필요 시 사용)")]
    [SerializeField] private UnityEvent _onEmptyStackEvent;

    public static bool TryGetExisting(out GlobalUIStackManager manager)
    {
        manager = _instance;
        return manager != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
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

        // MainScene 재진입 시 새 Scene의 UI 참조를 영구 인스턴스에 넘깁니다.
        _instance.RebindSceneReferences(_pauseMenuUI, _onEmptyStackEvent);

        // 같은 GameObject의 SaveManager 등 다른 컴포넌트까지 제거하지 않습니다.
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
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
        Debug.Log($"<color=green>[UIStackManager] UI 등록 성공:</color> {ui.gameObject.name} | 현재 스택 수: {_activeUIList.Count}");
    }

    public void UnregisterUI(UIStackMember ui)
    {
        if (ui == null)
        {
            RemoveInvalidStackEntries();
            return;
        }

        if (_activeUIList.Remove(ui))
        {
            Debug.Log($"<color=red>[UIStackManager] UI 해제 완료:</color> {ui.gameObject.name} | 남아있는 UI 수: {_activeUIList.Count}");
        }
    }

    private void PopAndCloseTopUI()
    {
        RemoveInvalidStackEntries();

        int lastIndex = _activeUIList.Count - 1;
        if (lastIndex < 0)
        {
            OnEmptyStack();
            return;
        }

        _activeUIList[lastIndex].CloseSelf();
    }

    private void RebindSceneReferences(UIStackMember pauseMenuUI, UnityEvent onEmptyStackEvent)
    {
        if (pauseMenuUI != null)
            _pauseMenuUI = pauseMenuUI;

        if (onEmptyStackEvent != null)
            _onEmptyStackEvent = onEmptyStackEvent;

        RemoveInvalidStackEntries();
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

    // 스택에 남아있는 UI가 없을 때 ESC를 누르면 실행되는 로직
    private void OnEmptyStack()
    {
        Debug.Log("[UIStackManager] 닫을 UI가 없습니다. 옵션/일시정지 메뉴를 호출합니다.");

        // 1. 직접 연결된 옵션 UI가 있다면 활성화
        if (_pauseMenuUI != null)
        {
            // GameObject를 켜면 옵션 UI의 UIStackMember.OnEnable()이 실행되면서
            // 스스로 스택에 등록됩니다!
            _pauseMenuUI.gameObject.SetActive(true);
        }

        // 2. 추가 유니티 이벤트 호출 (사운드 재생, 게임 일시정지 로직 등 연동용)
        _onEmptyStackEvent?.Invoke();
    }
}
