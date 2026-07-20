using System;
using TaskTown.KDH;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{

    public class AnimalInvPage_Manager : MonoBehaviour
    {
        [Header("해당 동물의 설정 상호작용 버튼")]
        [SerializeField] private Button levelupButton;         // 레벨업 버튼
        [SerializeField] private Button toolSetButton;     // 도구 배치 버튼 (추후 구현)
        [SerializeField] private Button villageSetButton; // 마을 배치 버튼 (추후 구현)


        private void Awake()
        {

            if (levelupButton != null)
            {
                levelupButton.onClick.AddListener(OnClickLevelUp);
            }

            if (toolSetButton != null)
            {
                toolSetButton.onClick.AddListener(OnClickToolSet);
            }

            if (villageSetButton != null)
            {
                villageSetButton.onClick.AddListener(OnClickVillageSet);
            }
        }


        private void OnClickLevelUp()
        {
            Debug.Log("[AnimalInvPage_Manager] 레벨업 시도");
        }

        private void OnClickToolSet()
        {
            Debug.Log("[AnimalInvPage_Manager] 도구 세팅하기");
        }

        private void OnClickVillageSet()
        {
            Debug.Log("[AnimalInvPage_Manager] 마을에 배치하기");
        }
    }
}
