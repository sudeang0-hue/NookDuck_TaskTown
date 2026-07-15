# TaskTown Scene Flow

## 실행 흐름

```text
00.BootstrapScene에서 SceneFlowManager와 SoundManager 준비
→ 01.TeamLogoScene에서 로고와 효과음 재생
→ 02.TitleLoadingScene에서 로딩 BGM과 StartupLoadPipeline 실행
→ 03.MainScene 사전 로드
→ 전체 진행률 100%
→ 로딩 BGM 정지 후 03.MainScene 자동 활성화
```

정상 흐름에는 Start 버튼이나 사용자 입력 대기가 없습니다.

## Bootstrap 역할

`00.BootstrapScene`은 화면 연출용 Scene이 아니라 전역 Manager를 먼저 준비하는 시작점입니다.

```text
AppRoot
├─ SceneFlowManager
├─ SoundManager
└─ SoundSettingsApplier
```

사운드 설정과 AudioSource Pool이 준비된 다음 `01.TeamLogoScene`으로 자동 전환합니다.

## Scene Flow 설정 관리

취합 브랜치에서는 자동 생성 Editor Tool을 사용하지 않습니다.
Scene, 설정 에셋, Build Settings는 팀 충돌을 줄이기 위해 담당자가 직접 확인 후 수정합니다.

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

Catalog와 Build Settings는 중앙 통합 파일이므로 한 명이 담당해서 수정하는 것을 권장합니다.

### 즉시 Scene 전환

```csharp
SceneFlowManager manager = SceneFlowManager.EnsureInstance();
if (!manager.LoadScene(SceneId.Main))
{
    Debug.LogError(manager.LastError);
}
```

### 사전 로드 후 원하는 시점에 활성화

```csharp
SceneFlowManager manager = SceneFlowManager.EnsureInstance();
manager.PreloadScene(SceneId.Main);

// State가 ReadyToActivate가 되거나 SceneReady 이벤트를 받은 뒤 호출합니다.
manager.ActivatePreloadedScene();
```

`SceneReady`, `SceneLoadCompleted`, `LoadFailed` 이벤트를 구독했다면 `OnDisable` 또는 `OnDestroy`에서 반드시 구독을 해제합니다.

### Title 이후 시작 대상 변경

1. `02.TitleLoadingScene/StartupLoading`의 `StartupLoadPipeline > Target Scene`을 변경합니다.
2. `Assets/00.Project/03.ScriptableObjects/Resources/SceneFlow/SceneCatalog.asset`에서 해당 SceneId와 Scene 이름이 맞는지 확인합니다.
3. Play Mode 또는 빌드에서 `00.BootstrapScene > 01.TeamLogoScene > 02.TitleLoadingScene > Target Scene` 도달 여부를 확인합니다.

## 현재 UI

- 현재 `01.TeamLogoScene`은 팀 로고 이미지와 `TeamLogo_DuckQuack` 효과음을 사용합니다.
- 실제 로고를 다시 교체할 때도 `LogoGroup`의 `CanvasGroup` 연결은 유지합니다.
- `02.TitleLoadingScene`은 `Title_LodingBGM`을 재생하면서 진행률 Slider와 현재 Step 이름을 표시합니다.

## Windows 창 표시 모드

- `00.BootstrapScene`은 `700 x 700` 일반 Windowed 창을 사용하고, `01.TeamLogoScene`은 `900 x 900` 불투명 무테 창을 사용합니다. 두 Scene 모두 투명 창 컴포넌트는 사용하지 않습니다.
- `02.TitleLoadingScene/TitleWindowInitializer`에서 테스트 복사본인 `TransparentWindowFlowTest`를 실행합니다. 다음 Scene에 팀 원본 `TransparentWindow`가 있으면 테스트 복사본이 제거되어 제어권을 원본에 넘깁니다.
- 팀원이 작성한 원본 `TransparentWindow.cs`는 수정하지 않으며, 테스트 복사본과 동시에 실행되지 않도록 Flow Scene에서는 교체합니다.
- `TransparentWindowFlowTest`는 Scene 전환 시 Main Camera와 네이티브 창 스타일을 다시 연결하므로 해상도 변경 뒤에도 무테·투명 상태를 복구합니다.
- Title/Main의 투명 구간 Camera는 `Solid Color + Alpha 0`, HDR Off, MSAA Off를 사용합니다. 현재 PC URP의 32-bit HDR 버퍼에는 Alpha 채널이 없어, HDR을 켜면 DWM 적용이 성공해도 빈 영역이 검정색으로 출력되기 때문입니다.
- `SceneWindowResolutionController`는 Windows Player에서 Scene별 창 해상도와 위치를 연속 3프레임 안정화합니다. Scene-local인 Logo 구간에서만 이후에도 스타일·창 영역을 감시해 Unity가 테두리나 프레임 기준 크기를 복원하면 다시 교정합니다.
- `Screen.SetResolution` 중 HWND가 교체될 수 있으므로, 현재 프로세스의 표시 중인 `UnityWndClass`를 다시 검색해 실제 Player 창에만 적용합니다.
- Player 프로세스 소유와 표시 상태가 확인된 창 핸들만 사용하므로 클릭 관통으로 포커스를 잃어도 다른 Windows 창을 잘못 수정하지 않습니다.
- Logo 구간의 무테 처리는 DWM 투명화 없이 네이티브 테두리만 제거하므로 Title/Main의 투명 창 생명주기와 분리됩니다.
- `01.TeamLogoScene`은 `900 x 900` 창과 같은 Canvas 기준 해상도를 사용하며 Background가 창 전체를 채웁니다.
- Logo 창은 무테 적용 뒤 외곽과 Client 영역을 모두 `900 x 900`으로 맞추고, 현재 창이 위치한 모니터의 작업 영역을 기준으로 중앙에 배치됩니다.
- `02.TitleLoadingScene`부터는 현재 Player 창이 위치한 모니터의 네이티브 영역과 원점에 맞춘 뒤 Camera 및 전체 화면 Background의 Alpha를 0으로 사용합니다.
- 현재 창 정책은 `Bootstrap → Logo → Title → Main` 단방향 Startup Flow를 기준으로 합니다. 추후 테두리가 필요한 Scene으로 되돌아가는 Flow를 추가할 때는 네이티브 테두리 복원과 지속 중인 `TransparentWindowFlowTest` 해제를 별도 정책으로 추가해야 합니다.
- 이 동작은 `UNITY_EDITOR`에서 기본적으로 실행하지 않으므로 Windows Build에서 확인해야 합니다. 창 스타일뿐 아니라 다른 색상 창을 뒤에 둔 상태에서 빈 픽셀을 통해 그 색상이 실제로 보이는지도 확인해야 합니다.
