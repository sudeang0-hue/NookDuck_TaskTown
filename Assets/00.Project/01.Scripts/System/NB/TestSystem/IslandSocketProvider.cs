using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandSocketProvider : MonoBehaviour
{
    [System.Serializable]
    public struct LevelDataSocket
    {
        [Tooltip("건물 종류")]
        public BuildingType buildingType;

        [Tooltip("해당 건물이 들어설 마을/해금 레벨 (예: 2렙 건물이면 2)")]
        public int targetLevel;

        [Tooltip("주인님이 씬 뷰에서 직접 배치한 소켓 Transform")]
        public Transform socketTransform;
    }

    [Header("★ 레벨별 건물 위치 직접 매핑 리스트 ★")]
    [SerializeField] private List<LevelDataSocket> levelSockets = new List<LevelDataSocket>();

    [Header("환경 프롭 소켓")]
    [SerializeField] private List<Transform> propSockets = new List<Transform>();

    /// <summary>
    /// 건물 종류와 레벨을 입력받아 주인님이 지정한 정확한 위치를 반환합니다.
    /// </summary>
    public Transform GetBuildingSocket(BuildingType type, int level)
    {
        // 주인님이 인스펙터에 지정한 (건물타입 + 레벨) 조합을 검색
        foreach (var socketData in levelSockets)
        {
            if (socketData.buildingType == type && socketData.targetLevel == level)
            {
                if (socketData.socketTransform != null)
                {
                    return socketData.socketTransform; // 주인님이 정한 위치 반환!
                }
            }
        }

        // 백업 예외 처리: 해당 레벨 전용 소켓이 없으면 타입만 맞는 소켓 검색
        foreach (var socketData in levelSockets)
        {
            if (socketData.buildingType == type && socketData.socketTransform != null)
            {
                Debug.LogWarning($"[SocketProvider] {level}레벨 전용 소켓을 찾지 못해 기존 {socketData.targetLevel}레벨 소켓 위치로 대체합니다.");
                return socketData.socketTransform;
            }
        }

        Debug.LogError($"[SocketProvider] '{type}' (Level {level}) 소켓이 인스펙터에 전혀 매핑되지 않았습니다!");
        return transform;
    }

    public Transform GetRandomPropSocket()
    {
        if (propSockets != null && propSockets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, propSockets.Count);
            if (propSockets[randomIndex] != null) return propSockets[randomIndex];
        }
        return transform;
    }

#if UNITY_EDITOR
    // 씬 뷰에서 주인님이 각 레벨별 건물 자리를 한눈에 볼 수 있는 Gizmos
    private void OnDrawGizmos()
    {
        if (levelSockets == null) return;

        foreach (var data in levelSockets)
        {
            if (data.socketTransform == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(data.socketTransform.position + Vector3.up * 0.5f, new Vector3(1.5f, 1f, 1.5f));

            UnityEditor.Handles.Label(
                data.socketTransform.position + Vector3.up * 1.8f,
                $"[Lv.{data.targetLevel} {data.buildingType}]",
                new GUIStyle { normal = { textColor = Color.yellow }, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }
            );
        }
    }
#endif
}