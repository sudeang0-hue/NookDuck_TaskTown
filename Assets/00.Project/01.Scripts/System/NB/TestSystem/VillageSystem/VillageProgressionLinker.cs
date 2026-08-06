//NB

using UnityEngine;
using Manager;

public class VillageProgressionLinker : MonoBehaviour
{
    // 사용자가 '타이핑 트랙' 과제를 완료하거나 구매했을 때 호출하는 메서드
    public void OnTypingTrackCompleted()
    {
        if (VillageSystemManager.Instance == null) return;

        // 1. 이번 사이클에서 해당 트랙을 켤 수 있는지 검증 후 처리
        bool success = VillageSystemManager.Instance.TryMarkCycleTrackDone(VillageElementTrack.Typing);

        if (success)
        {
            Debug.Log("<color=cyan>타이핑 트랙 사이클 완료 기록 성공!</color>");

            // 2. 만약 3종 트랙이 모두 끝났다면 자동으로 레벨업 체크 혹은 UI 알림 유도 가능
            CheckAndTryAutoLevelUp();
        }
        else
        {
            Debug.LogWarning("이미 이번 사이클에서 완료된 트랙이거나 엔드리스 상태입니다.");
        }
    }

    private void CheckAndTryAutoLevelUp()
    {
        var villageMgr = VillageSystemManager.Instance;

        // 필수 3종 조건(Click, Typing, Tool)이 모두 채워졌는지 확인
        if (villageMgr.IsReadyForVillageLevelUp)
        {
            Debug.Log("<color=yellow>필수 3종 사이클 게이트가 모두 열렸습니다! 레벨업 가능 상태.</color>");

            // 기획에 따라 여기서 자동으로 레벨업을 시도할 수도 있고, 버튼 활성화만 시킬 수도 있습니다.
            // bool levelUpResult = villageMgr.TryVillageLevelUp();
        }
    }
}