# TaskTown Scene Flow

## 실행 흐름

```text
TeamLogoScene
→ TitleScene에서 StartupLoadPipeline 실행
→ MainScene 사전 로드
→ 전체 진행률 100%
→ MainScene 자동 활성화
```

정상 흐름에는 Start 버튼이나 사용자 입력 대기가 없습니다.

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
| `LocalSettings` | 사운드, 그래픽, 언어 설정 |
| `SaveRead` | 저장 파일 읽기 |
| `SaveConvert` | 역직렬화 및 버전 마이그레이션 |
| `SaveValidation` | 누락값과 잘못된 값 검증 |
| `SessionBuild` | 검증 데이터로 `GameSession` 구성 |
| `RuntimeApply` | Runtime Manager에 데이터 적용 |
| `ScenePreload` | MainScene 사전 로드 |
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

## MainScene Runtime 연결

저장 데이터가 준비되면 MainScene 시스템은 `IMainSceneInitializer`를 구현할 수 있습니다.

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

저장 시스템 연결 시 MainScene에 `MainSceneBootstrapper`를 추가하면 `InitializationOrder` 순서대로 실행한 뒤 `GameReady` 이벤트를 발생시킵니다.

## 현재 UI

- `TeamLogoScene`은 실제 로고 이미지가 준비되기 전 사용할 텍스트 Placeholder를 포함합니다.
- 실제 로고 적용 시 `LogoGroup` 아래 UI만 교체하고 `CanvasGroup` 연결은 유지합니다.
- `TitleScene`은 진행률 Slider와 현재 Step 이름을 표시합니다.
