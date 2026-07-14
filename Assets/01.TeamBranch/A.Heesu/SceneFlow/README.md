# TaskTown Scene Flow

## 실행 흐름

```text
BootstrapScene에서 SceneFlowManager와 SoundManager 준비
→ TeamLogoScene에서 로고와 효과음 재생
→ TitleScene에서 로딩 BGM과 StartupLoadPipeline 실행
→ TestMainGameScene 사전 로드
→ 전체 진행률 100%
→ 로딩 BGM 정지 후 TestMainGameScene 자동 활성화
```

정상 흐름에는 Start 버튼이나 사용자 입력 대기가 없습니다.

## Bootstrap 역할

`BootstrapScene`은 화면 연출용 Scene이 아니라 전역 Manager를 먼저 준비하는 시작점입니다.

```text
AppRoot
├─ SceneFlowManager
├─ SoundManager
└─ SoundSettingsApplier
```

사운드 설정과 AudioSource Pool이 준비된 다음 `TeamLogoScene`으로 자동 전환합니다.

## Unity 메뉴

- `Tools/TaskTown/Scene Flow/Generate Startup Flow`
  - Scene, 설정 에셋, Build Settings를 생성하거나 누락 항목을 보완합니다.
  - 기존 동명 Scene과 설정 에셋은 덮어쓰지 않습니다.
- `Tools/TaskTown/Scene Flow/Validate Startup Flow`
  - 현재 편집 중인 Scene을 바꾸지 않고 전체 시작 흐름을 Play Mode에서 검증합니다.
- `Tools/TaskTown/Scene Flow/Select Startup Load Plan`
  - 로딩 Step 통합 목록을 선택합니다.

## 저장 시스템 연결 순서

| Phase | 용도 |
|---|---|
| `CoreValidation` | 필수 설정 검증 |
| `LocalSettings` | Bootstrap 이후 추가 설정이 필요한 경우 사용 |
| `SaveRead` | 저장 파일 읽기 |
| `SaveConvert` | 역직렬화 및 버전 마이그레이션 |
| `SaveValidation` | 누락값과 잘못된 값 검증 |
| `SessionBuild` | 검증 데이터로 `GameSession` 구성 |
| `RuntimeApply` | Runtime Manager에 데이터 적용 |
| `ScenePreload` | Pipeline에 지정된 대상 Scene 사전 로드 |
| `Finalize` | 최종 무결성 확인 |

## 새로운 로딩 Step 추가

1. `StartupLoadStepSO`를 상속한 클래스를 만듭니다.
2. `ExecuteStep`에서 하나의 책임만 처리합니다.
3. 진행률은 `context.ReportProgress(0f~1f)`로 보고합니다.
4. 임시 결과는 `context.Shared.Set(data)`로 다음 Step에 전달합니다.
5. 최종 Runtime Snapshot은 `context.Shared.GameSession.Set(data)`에 기록합니다.
6. Unity에서 Step 에셋을 생성합니다.
7. 통합 담당자가 `StartupLoadPlan.asset`에 Step을 추가하고 Phase와 Order를 확인합니다.

`StartupLoadPlan.asset`은 충돌 위험이 있는 중앙 통합 파일이므로 여러 팀원이 동시에 수정하지 않습니다.

## 게임 Scene Runtime 연결

저장 데이터가 준비되면 실제 게임 Scene 시스템은 `IMainSceneInitializer`를 구현할 수 있습니다.

```csharp
public sealed class CoinInitializer : MonoBehaviour, IMainSceneInitializer
{
    public int InitializationOrder => 610;

    public void Initialize(GameSession session)
    {
        // session.TryGet<CoinSnapshot>(out var snapshot)
        // CoinManager에 검증된 Runtime 값을 적용합니다.
    }
}
```

저장 시스템 연결 시 대상 Scene에 `MainSceneBootstrapper`를 추가하면 `InitializationOrder` 순서대로 실행한 뒤 `GameReady` 이벤트를 발생시킵니다.

## 다른 Scene 등록 및 불러오기

Scene 이름 문자열을 직접 호출하지 않고 `SceneId`와 `SceneCatalog.asset`을 통해 불러옵니다.

### Scene 등록

1. `SceneId` enum 마지막에 새 ID를 추가합니다. 기존 숫자는 직렬화 호환성을 위해 변경하지 않습니다.
2. `SceneCatalog.asset`에 같은 ID와 실제 Scene 이름을 등록합니다.
3. Unity Build Settings의 Scene 목록에 해당 `.unity` 파일을 활성화 상태로 추가합니다.
4. `SceneFlowManager.CanLoadScene`으로 Catalog와 Build Settings 연결을 확인합니다.

`Generate Startup Flow` 도구는 기존 Catalog 항목과 추가 Build Scene을 보존합니다. Catalog와 Build Settings는 중앙 통합 파일이므로 한 명이 담당해서 수정하는 것을 권장합니다.

### 즉시 Scene 전환

```csharp
SceneFlowManager manager = SceneFlowManager.EnsureInstance();
if (!manager.LoadScene(SceneId.TestMainGame))
{
    Debug.LogError(manager.LastError);
}
```

### 사전 로드 후 원하는 시점에 활성화

```csharp
SceneFlowManager manager = SceneFlowManager.EnsureInstance();
manager.PreloadScene(SceneId.TestMainGame);

// State가 ReadyToActivate가 되거나 SceneReady 이벤트를 받은 뒤 호출합니다.
manager.ActivatePreloadedScene();
```

`SceneReady`, `SceneLoadCompleted`, `LoadFailed` 이벤트를 구독했다면 `OnDisable` 또는 `OnDestroy`에서 반드시 구독을 해제합니다.

### Title 이후 시작 대상 변경

1. `TitleScene/StartupLoading`의 `StartupLoadPipeline > Target Scene`을 변경합니다.
2. 새로 생성되는 기본 Flow도 바꿀 경우 `SceneFlowSetupTool`의 `StartupTargetSceneId`와 `StartupTargetScenePath`를 함께 변경합니다.
3. `Validate Startup Flow`를 실행해 목표 Scene 도달 여부를 확인합니다.

## 현재 UI

- 현재 `TeamLogoScene`은 팀 로고 이미지와 `TeamLogo_DuckQuack` 효과음을 사용합니다.
- 실제 로고를 다시 교체할 때도 `LogoGroup`의 `CanvasGroup` 연결은 유지합니다.
- `TitleScene`은 `Title_LodingBGM`을 재생하면서 진행률 Slider와 현재 Step 이름을 표시합니다.
