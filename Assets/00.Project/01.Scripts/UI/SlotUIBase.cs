using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI 갱신을 위한 동물, 도구, 도감에 출력되는 슬롯 1 칸의 정보 베이스.

namespace UI
{
    public class SlotUIBase : MonoBehaviour
    {
        [Header("공통으로 표시되는 슬롯 UI")]
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TMP_Text displayNameText;

        protected string currentId;

        public string CurrentId => currentId;

        public virtual void SetBaseInfo(string id, string displayName, Sprite icon)
        {
            currentId = id;

            if (displayNameText != null)
                displayNameText.text = displayName;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }

        public virtual void Clear()
        {
            currentId = string.Empty;

            if (displayNameText != null)
                displayNameText.text = string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }
}