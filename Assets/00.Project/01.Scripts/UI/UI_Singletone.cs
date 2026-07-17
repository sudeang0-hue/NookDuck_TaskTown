using UI;
using UnityEngine;

public class UI_Singletone : MonoBehaviour
{
    public static UI_Singletone instance;

    [SerializeField] private UIController_Coin coin;
    [SerializeField] private UIController_Menu_Test menu;
    [SerializeField] private UIController_AnimalInv animalinv;
    [SerializeField] private UIController_ToolInv toolinv;
    [SerializeField] private UIController_Gacha gacha;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        coin = GetComponent<UIController_Coin>();
        menu = GetComponent<UIController_Menu_Test>();
        animalinv = GetComponent<UIController_AnimalInv>();
        toolinv = GetComponent<UIController_ToolInv>();
        gacha = GetComponent<UIController_Gacha>();
    }


}
