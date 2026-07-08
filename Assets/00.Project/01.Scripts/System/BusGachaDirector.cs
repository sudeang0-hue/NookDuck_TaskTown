using UnityEngine;
using DG.Tweening;

public class BusGachaDirector : MonoBehaviour
{
    [Header("오브젝트 연결")]
    public Transform busObject;         // 버스 3D 모델
    public Transform dropPoint;         // 동물이 내릴 정차 위치
    public GameObject animalPrefab;     // 소환될 동물 주민 프리팹

    [Header("버스 이동 경로 (징검다리)")]
    [Tooltip("씬에 빈 오브젝트를 배치해 경로를 순서대로 넣어주세요.")]
    public Transform[] pathPoints;

    // 테스트용 단축키
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartBusSummon();
        }
    }

    public void StartBusSummon()
    {
        // 1. 경로 좌표 배열 만들기 (Transform -> Vector3 추출)
        Vector3[] waypoints = new Vector3[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            waypoints[i] = pathPoints[i].position;
        }

        // 2. 버스 초기 위치 세팅 (첫 번째 시작점)
        busObject.position = waypoints[0];
        busObject.gameObject.SetActive(true);

        //DOTween 버스 주행 시퀀스 시작
        Sequence busSeq = DOTween.Sequence();

        // 1단계: 버스가 곡선 경로를 따라 이동 (3초 동안)
        // SetLookAt(0.01f): 버스가 이동하는 방향(앞)을 자연스럽게 바라보며 코너링
        busSeq.Append(busObject.DOPath(waypoints, 3f, PathType.CatmullRom)
                               .SetLookAt(0.01f)
                               .SetEase(Ease.InOutQuad));

        // 2단계: 도착 후 덜컹! (브레이크 밟는 느낌의 반동)
        busSeq.Append(busObject.DOPunchPosition(busObject.forward * 0.5f, 0.4f, 10, 1f));

        // 대기 시간 (문 열리는 타이밍)
        busSeq.AppendInterval(0.3f);

        // 3단계: 동물 주민 뿅! 등장
        busSeq.AppendCallback(() => {
            // 동물 소환
            GameObject newAnimal = Instantiate(animalPrefab, dropPoint.position, Quaternion.identity);
            newAnimal.transform.localScale = Vector3.zero;

            // 동물 연출: 크기가 커지면서 앞으로 뿅! 하고 점프해서 착지
            newAnimal.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            newAnimal.transform.DOJump(dropPoint.position + new Vector3(0, 0, -2f), 1.5f, 1, 0.5f);

            // 파티클이나 효과음은 여기서 실행
            // Debug.Log("새로운 주민이 이사 왔습니다!");
        });

        // 4단계: 동물이 내린 후, 1.5초 뒤에 버스가 다시 화면 밖으로 퇴장 (선택)
        busSeq.AppendInterval(1.5f);
        // 마지막 웨이포인트(화면 밖)로 직진해서 사라짐
        busSeq.Append(busObject.DOMove(waypoints[waypoints.Length - 1] + busObject.forward * 20f, 1.5f).SetEase(Ease.InQuad));
    }
}