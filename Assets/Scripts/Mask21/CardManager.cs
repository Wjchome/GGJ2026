using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    WaitingForAITurn, // 等待AI回合 
    WaitingForAISolo, //仅ai
    WaitingForPlayerTurn, // 等待玩家回合
    BothStand, // 双方都不摸牌，可以开牌
    GameOver // 游戏结束
}

public class CardManager : MonoBehaviour
{
    public Sprite[] cardSprites;

    public Sprite cardBack;

    public List<CardMono> cards;

    // 游戏状态
    public GameState currentState = GameState.WaitingForAITurn;

    public CardMono cardPrefab;
    public Transform cardParent;

    public Transform AIPos;
    public Transform MyPos;

    public float f;


    // UI按钮
    public Button playerDrawCardBtn; // 玩家摸牌按钮
    public Button playerStandBtn; // 玩家不摸按钮
    public Button revealCardsBtn; // 开牌按钮
    public Text playerCardsText;
    public Text aiCardsText;


    private void SetupUIButtons()
    {
        playerCardsText.text = "";
        aiCardsText.text = "";

        playerDrawCardBtn.onClick.RemoveAllListeners();
        playerDrawCardBtn.onClick.AddListener(OnPlayerDrawCard);


        playerStandBtn.onClick.RemoveAllListeners();
        playerStandBtn.onClick.AddListener(OnPlayerStand);


        revealCardsBtn.onClick.RemoveAllListeners();
        revealCardsBtn.onClick.AddListener(OnRevealCards);


        UpdateUIButtons();
    }

    /// <summary>
    /// 更新UI按钮状态
    /// </summary>
    public void UpdateUIButtons()
    {
        bool canPlayerDraw = currentState == GameState.WaitingForPlayerTurn && CanPlayerDrawCard();
        bool canPlayerStand = currentState == GameState.WaitingForPlayerTurn;
        bool canReveal = currentState == GameState.BothStand;


        playerDrawCardBtn.interactable = canPlayerDraw && canPlayerStand;
        playerStandBtn.interactable = canPlayerStand;
        revealCardsBtn.interactable = canReveal;


        var (playerPoints, playerPoints1) = GetPlayerPoints();
        if (playerPoints == playerPoints1)
        {
            playerCardsText.text = playerPoints.ToString();
            if (playerPoints1 > 21)
            {
                playerCardsText.color = Color.red;
            }
            else
            {
                playerCardsText.color = Color.white;
            }
        }
        else
        {
            playerCardsText.text = $"{playerPoints} {playerPoints1}";
            playerCardsText.color = Color.white;
        }
    }

    private List<int> ExpandCardDictionary(Dictionary<int, int> cardDict)
    {
        List<int> result = new List<int>();
        foreach (var kvp in cardDict)
        {
            int cardNum = kvp.Key; // 卡牌数字（1-10）
            int cardCount = kvp.Value; // 该数字的卡牌数量
            // 按数量添加卡牌，比如数字1添加3次，数字10添加9次
            for (int i = 0; i < cardCount; i++)
            {
                result.Add(cardNum);
            }
        }

        return result;
    }

    private void ShuffleCardList(List<int> cardList)
    {
        // 从列表末尾向前遍历，逐个交换随机位置的元素
        for (int i = cardList.Count - 1; i > 0; i--)
        {
            // 生成0到i（包含i）的随机索引，确保每一步概率均等
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            // 交换当前索引i和随机索引的元素
            (cardList[i], cardList[randomIndex]) = (cardList[randomIndex], cardList[i]);
            // 若使用C#7.0以下版本，替换为传统交换：
            // int temp = cardList[i];
            // cardList[i] = cardList[randomIndex];
            // cardList[randomIndex] = temp;
        }
    }

    public Sprite GetSprite(int num)
    {
        return cardSprites[num - 1];
    }

    public Sprite GetBack()
    {
        return cardBack;
    }


    public int cardIndex;
    public int AICardNum;
    public float ff;

    // 预计算的8个位置
    private const int MAX_CARDS = 8;
    private Vector2[] aiCardPositions = new Vector2[MAX_CARDS];
    private Vector2[] myCardPositions = new Vector2[MAX_CARDS];

    // 管理mine卡牌列表
    public List<CardMono> mineCards = new List<CardMono>();

    // 管理AI卡牌列表
    public List<CardMono> aiCards = new List<CardMono>();

    // AI决策阈值（超过这个点数就不摸牌）
    public int aiThreshold = 17;

    public void Init()
    {
        var config = DeckConfig.CardNumbers;
        var list = ExpandCardDictionary(config);
        ShuffleCardList(list);
        cardIndex = list.Count - 1;

        // 预计算AI和玩家的8个位置
        CalculateCardPositions();

        for (int i = 0; i < list.Count; i++)
        {
            var cardNum = list[i];
            Vector2 cardPos = (Vector2)cardParent.position + new Vector2(0, -i * f);
            CardMono card = Instantiate(cardPrefab, cardPos, Quaternion.identity, cardParent);
            card.cardManager = this;
            card.cardNumber = cardNum;
            card.isBack = true;
            card.ChangeSprite();
            card.spriteRenderer.sortingOrder = i;
            cards.Add(card);
        }

        SetupUIButtons();
        // 开始游戏流程
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (currentState != GameState.GameOver)
        {
            // AI回合
            if (currentState == GameState.WaitingForAITurn ||
                currentState == GameState.WaitingForAISolo)
            {
                yield return new WaitForSeconds(0.5f);
                AITurn();
                yield return new WaitForSeconds(0.5f);
            }

            // 玩家回合
            if (currentState == GameState.WaitingForPlayerTurn)
            {
                yield return new WaitForSeconds(0.5f);
                UpdateUIButtons();
                yield return new WaitForSeconds(0.5f);
                // 等待玩家操作（通过按钮）
                yield return new WaitUntil(() => currentState != GameState.WaitingForPlayerTurn);
            }

            // 检查是否可以开牌
            if (currentState == GameState.BothStand)
            {
                yield return new WaitForSeconds(0.5f);
                UpdateUIButtons();
                yield return new WaitForSeconds(0.5f);
                yield return new WaitUntil(() => currentState != GameState.BothStand);
            }

            yield return null;
        }
    }

    /// <summary>
    /// AI回合
    /// </summary>
    private void AITurn()
    {
        bool aiCanDraw = ShouldAIDrawCard();
        // AI决策：是否摸牌
        if (aiCanDraw)
        {
            // AI摸一张牌
            SendAICard();
            if (currentState == GameState.WaitingForAITurn)
            {
                currentState = GameState.WaitingForPlayerTurn;
            }
        }
        else
        {
            if (currentState == GameState.WaitingForAITurn)
            {
                currentState = GameState.WaitingForPlayerTurn;
            }
            else
            {
                currentState = GameState.BothStand;
            }
        }


        UpdateUIButtons();
    }

    /// <summary>
    /// 玩家摸牌
    /// </summary>
    public void OnPlayerDrawCard()
    {
        if (currentState != GameState.WaitingForPlayerTurn)
        {
            return;
        }

        SendMineCard();
        currentState = GameState.WaitingForAITurn;
        UpdateUIButtons();
    }

    /// <summary>
    /// 玩家不摸牌
    /// </summary>
    public void OnPlayerStand()
    {
        if (currentState != GameState.WaitingForPlayerTurn)
        {
            return;
        }

        bool aiCanDraw = ShouldAIDrawCard();

        if (aiCanDraw)
        {
            // AI可以继续摸，转到AI回合
            currentState = GameState.WaitingForAISolo;
        }
        else
        {
            // 双方都不摸，可以开牌
            currentState = GameState.BothStand;
        }

        UpdateUIButtons();
    }


    /// <summary>
    /// 开牌
    /// </summary>
    public void OnRevealCards()
    {
        if (currentState != GameState.BothStand)
        {
            return;
        }

        // 显示所有AI卡牌

        foreach (var aiCard in aiCards)
        {
            aiCard.isBack = false;
            aiCard.ChangeSprite();
        }


        // 计算并显示结果
        CalculateResults();

        currentState = GameState.GameOver;
        UpdateUIButtons();
    }

    private void CalculateResults()
    {
        var (playerPoints, playerPoints1) = GetPlayerPoints();
        var (aiPoints, aiPoints1) = GetAIPoints();
        var res1 = playerPoints;
        if (res1 < 21)
        {
            if (res1 + 10 <= playerPoints1 && res1 + 10 <= 21)
            {
                res1 += 10;
            }
        }

        var res2 = aiPoints;
        if (res2 < 21)
        {
            if (res2 + 10 <= aiPoints1 && res2 + 10 <= 21)
            {
                res2 += 10;
            }
        }

        playerCardsText.text = res1.ToString();
        aiCardsText.text = res2.ToString();

        Debug.Log($"CardManager结果 - 玩家点数: {playerPoints}, AI点数: {aiPoints}");

        // 判断胜负
        if (res1 > 21 && res2 > 21)
        {
            Debug.Log("玩家爆牌，AI爆牌 ,平局");
        }
        else if (res2 > 21)
        {
            Debug.Log("AI爆牌，玩家获胜");
        }
        else if (res1 > 21)
        {
            Debug.Log("玩家爆牌,AI获胜");
        }
        else if (res1 > res2)
        {
            Debug.Log("玩家获胜");
        }
        else if (res2 > res1)
        {
            Debug.Log("AI获胜");
        }
        else
        {
            Debug.Log("平局");
        }
    }

    /// <summary>
    /// 预计算AI和玩家各8个卡牌位置
    /// </summary>
    private void CalculateCardPositions()
    {
        for (int i = 0; i < MAX_CARDS; i++)
        {
            aiCardPositions[i] = (Vector2)AIPos.position + new Vector2(i * ff, 0);
            myCardPositions[i] = (Vector2)MyPos.position + new Vector2(i * ff, 0);
        }
    }

    public void SendAICard()
    {
        if (AICardNum >= MAX_CARDS)
        {
            Debug.LogWarning("AI卡牌已达到最大数量限制（8张）");
            return;
        }

        if (cardIndex < 0)
        {
            Debug.LogWarning("牌堆已空");
            return;
        }

        cards[cardIndex].transform.DOMove(aiCardPositions[AICardNum], 0.3f);
        cards[cardIndex].spriteRenderer.sortingOrder = AICardNum;
        cards[cardIndex].isBack = true;
        cards[cardIndex].cardState = CardState.AI;
        cards[cardIndex].ChangeSprite();
        aiCards.Add(cards[cardIndex]);
        cardIndex--;
        AICardNum++;
    }

    public int myCardNum;

    public bool SendMineCard()
    {
        if (myCardNum >= MAX_CARDS)
        {
            Debug.LogWarning("玩家卡牌已达到最大数量限制（8张）");
            return false;
        }

        if (cardIndex < 0)
        {
            Debug.LogWarning("牌堆已空");
            return false;
        }


        cards[cardIndex].transform.DOMove(myCardPositions[myCardNum], 0.3f);
        cards[cardIndex].spriteRenderer.sortingOrder = myCardNum;
        cards[cardIndex].isBack = false;
        cards[cardIndex].cardState = CardState.Mine;
        cards[cardIndex].ChangeSprite();
        mineCards.Add(cards[cardIndex]);
        cardIndex--;
        myCardNum++;
        return true;
    }

    /// <summary>
    /// 添加mine卡牌（从其他CardManager转移过来）
    /// </summary>
    public bool AddMineCard(CardMono card)
    {
        if (myCardNum >= MAX_CARDS)
        {
            Debug.LogWarning("玩家卡牌已达到最大数量限制（8张）");
            return false;
        }

        card.cardManager = this;
        card.cardState = CardState.Mine;
        card.isBack = false;
        card.ChangeSprite();
        mineCards.Add(card);

        // 重新排列所有mine卡牌
        RearrangeMineCards();
        return true;
    }

    /// <summary>
    /// 移除mine卡牌
    /// </summary>
    public void RemoveMineCard(CardMono card)
    {
        if (mineCards.Remove(card))
        {
            myCardNum--;
            // 重新排列剩余卡牌
            RearrangeMineCards();
        }
    }

    /// <summary>
    /// 重新排列所有mine卡牌的位置
    /// </summary>
    public void RearrangeMineCards()
    {
        for (int i = 0; i < mineCards.Count; i++)
        {
            mineCards[i].transform.DOMove(myCardPositions[i], 0.3f);
            mineCards[i].spriteRenderer.sortingOrder = i;
        }

        myCardNum = mineCards.Count;
    }

    /// <summary>
    /// 计算卡牌点数（处理A的特殊情况：1或11）
    /// </summary>
    /// <param name="cardList">卡牌列表</param>
    /// <returns>最佳点数（不超过21的最大值）</returns>
    public (int, int) CalculatePoints(List<CardMono> cardList)
    {
        int totalPoints = 0;
        int aceCount = 0;

        foreach (var card in cardList)
        {
            if (card.cardNumber == 1)
            {
                // A先按1计算，记录A的数量
                aceCount++;
                totalPoints += 1;
            }
            else if (card.cardNumber >= 2 && card.cardNumber <= 10)
            {
                totalPoints += card.cardNumber;
            }
        }

        return (totalPoints, totalPoints + aceCount * 10);
    }

    /// <summary>
    /// 获取玩家点数
    /// </summary>
    public (int, int) GetPlayerPoints()
    {
        return CalculatePoints(mineCards);
    }

    /// <summary>
    /// 获取AI点数
    /// </summary>
    public (int, int) GetAIPoints()
    {
        return CalculatePoints(aiCards);
    }

    /// <summary>
    /// AI决策：是否摸牌
    /// </summary>
    public bool ShouldAIDrawCard()
    {
        var (currentPoints, _) = GetAIPoints();
        // 如果点数已经达到或超过阈值，不摸牌
        // 如果点数已经达到或超过21，不摸牌
        return currentPoints < aiThreshold && currentPoints < 21;
    }

    /// <summary>
    /// 玩家是否可以摸牌
    /// </summary>
    public bool CanPlayerDrawCard()
    {
        var (currentPoints, _) = GetPlayerPoints();
        return currentPoints < 21 && myCardNum < MAX_CARDS && cardIndex >= 0;
    }
}