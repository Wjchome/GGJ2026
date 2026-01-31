using UnityEngine;
using UnityEngine.UI;

public class CardUIManager : MonoBehaviour
{
    public Button playerDrawCardBtn; // 玩家摸牌按钮
    public Button playerStandBtn; // 玩家不摸按钮
    public Button revealCardsBtn; // 开牌按钮
    public Button sortCardsBtn; // 排序按钮
    public Button showStrategyBtn; // 显示策略按钮
    public Text playerCardsText;
    public Text aiCardsText;

    public GameObject endPanel;
    public Button endBtn;
    public Text endText;
    public Text endBtnText;

    public GameObject strategyPanel; // 策略说明面板
    public Text strategyText; // 策略说明文本
    public Button closeStrategyBtn; // 关闭策略面板按钮
}