using UnityEngine;
using UnityEngine.UI;

public class CardUIManager : MonoBehaviour
{
    public Button playerDrawCardBtn; // 玩家摸牌按钮
    public Button playerStandBtn; // 玩家不摸按钮
    public Button revealCardsBtn; // 开牌按钮
    public Text playerCardsText;
    public Text aiCardsText;

    public GameObject endPanel;
    public Button endBtn;
    public Text endText;
    public Text endBtnText;
}