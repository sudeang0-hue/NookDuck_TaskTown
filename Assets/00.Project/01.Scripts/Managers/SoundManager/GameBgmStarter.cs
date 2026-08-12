using UnityEngine;

/// <summary>
/// MainScene 진입 시 인게임 BGM 플레이리스트를 재생합니다.
/// 첫 곡은 무작위로 선택하고 이후에는 등록된 순서대로 재생합니다.
/// 마지막 곡 이후에는 첫 곡으로 돌아갑니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameBgmStarter : MonoBehaviour
{
    [SerializeField] private string gameBgmSoundId = "Game_BGM_Playlist";

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(gameBgmSoundId))
        {
            Debug.LogWarning("[GameBgmStarter] 인게임 BGM Sound ID가 비어 있습니다.", this);
            return;
        }

        SoundManager manager = SoundManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("[GameBgmStarter] SoundManager를 찾을 수 없습니다.", this);
            return;
        }

        if (!manager.PlayBGMPlaylistFromRandomTrack(gameBgmSoundId))
        {
            Debug.LogWarning($"[GameBgmStarter] 인게임 BGM 재생에 실패했습니다: {gameBgmSoundId}", this);
        }
    }
}
