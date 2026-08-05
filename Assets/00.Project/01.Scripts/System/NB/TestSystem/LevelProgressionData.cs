using System;
using System.Collections.Generic;
using UnityEngine;

// 건물 종류 열거형
public enum BuildingType
{
    TownHall,   // 1L: 마을회관
    Warehouse,  // 2L: 창고
    ClockTower, // 3L: 시계탑
    Windmill,   // 4L: 풍차
    Fishery,    // 5L: 낚시터 (2단계 섬)
    Fountain,   // 6L: 분수대
    Salon,      // 7L: 미용실
    Cafe,       // 8L: 카페
    Bakery,     // 8L: 빵집
    Police,     // 9L: 파출소
    HotSpring   // 10L: 온천 (3단계 섬)
}

[Serializable]
public struct BuildingDropInfo
{
    public BuildingType buildingType;
    public GameObject buildingPrefab;       // 건물 프리팹
    public List<GameObject> propPrefabs;     // 함께 떨어질 환경 프롭 3~4개
}

[CreateAssetMenu(fileName = "LevelProgressionData", menuName = "Town/Level Progression Data")]
public class LevelProgressionData : ScriptableObject
{
    [Header("레벨 기본 정보")]
    public int level;

    [Header("★ 섬 교체 설정 ★")]
    [Tooltip("이 레벨에 도달 시 교체할 섬 프리팹. 교체하지 않는 레벨이면 비워두세요.")]
    // [핵심 해결] TownLevelManager가 호출하는 변수명과 일치시켰습니다!
    public GameObject targetIslandPrefab;

    [Header("해금 건물 및 프롭 데이터")]
    // TownLevelManager의 foreach문과 완벽하게 호환되는 리스트입니다.
    public List<BuildingDropInfo> unlockBuildings;
}