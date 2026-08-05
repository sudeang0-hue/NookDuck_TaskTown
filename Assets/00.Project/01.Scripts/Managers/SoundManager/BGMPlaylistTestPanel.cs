/*
 * 역할:
 * - BGM 플레이리스트 기능을 통합 테스트 씬에서 검증하기 위한 임시 UI 컨트롤러입니다.
 *
 * 주요 기능:
 * - 현재 곡 이름, 곡 번호, 재생 시간과 전체 시간을 표시합니다.
 * - 이전/다음, 재생/일시정지, 순차 반복/현재 곡 반복 조작을 SoundManager에 전달합니다.
 *
 * 주의:
 * - 실제 옵션 UIController가 아닌 테스트 전용 컴포넌트입니다.
 */
/// <summary>
/// BGM Playlist 통합 테스트 Scene에서 공용 패널 로직을 검증하기 위한 래퍼입니다.
/// </summary>
public sealed class BGMPlaylistTestPanel : BGMPlaylistPanel
{
}
