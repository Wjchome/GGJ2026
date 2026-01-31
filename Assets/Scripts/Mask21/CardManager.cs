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
    WaitForNextRound,
    GameOver // 游戏结束
}

/// <summary>
/// 小局结果
/// </summary>
public enum RoundResult
{
    Win, // 胜利 +1
    Draw, // 平局 0
    Lose // 失败 -1
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
    public Transform SceneCardsPos; // 场上已打出牌的位置（小局结束后显示）

    public float f; //堆叠偏移
    public float playedCardScale = 0.7f; // 场上牌的缩放比例


    public CardUIManager cardUIManager;

    private void SetupUIButtons()
    {
        cardUIManager.playerCardsText.text = "";
        cardUIManager.aiCardsText.text = "";
        cardUIManager.endPanel.gameObject.SetActive(false);

        cardUIManager.playerDrawCardBtn.onClick.RemoveAllListeners();
        cardUIManager.playerDrawCardBtn.onClick.AddListener(OnPlayerDrawCard);


        cardUIManager.playerStandBtn.onClick.RemoveAllListeners();
        cardUIManager.playerStandBtn.onClick.AddListener(OnPlayerStand);


        cardUIManager.revealCardsBtn.onClick.RemoveAllListeners();
        cardUIManager.revealCardsBtn.onClick.AddListener(OnRevealCards);

        cardUIManager.endBtn.onClick.RemoveAllListeners();
        cardUIManager.endBtn.onClick.AddListener(OnEndRound);


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


        cardUIManager.playerDrawCardBtn.interactable = canPlayerDraw && canPlayerStand;
        cardUIManager.playerStandBtn.interactable = canPlayerStand;
        cardUIManager.revealCardsBtn.interactable = canReveal;


        var (playerPoints, playerPoints1) = GetPlayerPoints();
        if (playerPoints == playerPoints1)
        {
            cardUIManager.playerCardsText.text = playerPoints.ToString();
            if (playerPoints1 > 21)
            {
                cardUIManager.playerCardsText.color = Color.red;
            }
            else
            {
                cardUIManager.playerCardsText.color = Color.white;
            }
        }
        else
        {
            cardUIManager.playerCardsText.text = $"{playerPoints} {playerPoints1}";
            cardUIManager.playerCardsText.color = Color.white;
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

    private List<int> ExpandCardDictionary(List<int> cardDict)
    {
        List<int> result = new List<int>();
        for (int i = 1; i < cardDict.Count; i++)
        {
            int cardCount = cardDict[i];
            // 按数量添加卡牌，比如数字1添加3次，数字10添加9次
            for (int j = 0; j < cardCount; j++)
            {
                result.Add(i);
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


    public int cardIndex; //目前指向牌堆的index
    public int AICardNum; //AI有多少张牌在手上
    public float ff; //水平位置偏移

    // 预计算的8个位置
    private const int MAX_CARDS = 8;
    private Vector2[] aiCardPositions = new Vector2[MAX_CARDS];
    private Vector2[] myCardPositions = new Vector2[MAX_CARDS];
    private Vector2[] scenePositions = new Vector2[10 * MAX_CARDS];

    // 管理mine卡牌列表
    public List<CardMono> mineCards = new List<CardMono>();

    // 管理AI卡牌列表
    public List<CardMono> aiCards = new List<CardMono>();

    [Header("AI Audit")] public AISettings aiSettings;

    // ========== 小局管理功能（合并自RoundManager） ==========
    // 小局相关
    private int currentRoundIndex = 0;

    private List<RoundResult> roundResults = new List<RoundResult>();

    // 暴露检测相关
    private int cardsDrawnCount = 0; // 本小局摸牌数
    private Dictionary<int, int> usedCardsCount = new Dictionary<int, int>(); // 已打出的牌统计（跨小局）
    private List<CardMono> usedCards = new List<CardMono>(); // 所有已打出的牌（不回归牌堆）


    public void Init(AISettings settings)
    {
        aiSettings = settings;
        var list = ExpandCardDictionary(aiSettings.CardNumbers);
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

        // 初始化小局
        InitRounds();

        SetupUIButtons();
        // 开始游戏流程
        StartCoroutine(GameLoop());
    }

    public void WaitForNextRound()
    {
        cardUIManager.endPanel.SetActive(true);
    }

    private IEnumerator GameLoop()
    {
        while (currentState != GameState.GameOver)
        {
            if (currentState == GameState.WaitForNextRound)
            {
                WaitForNextRound();
                yield return new WaitUntil(() => currentState != GameState.WaitForNextRound);
            }


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

        // 结束当前小局（在这里检测暴露）
        RoundResult result = EndRound();
        Debug.Log($"小局结果：{result}");

        // 如果所有小局都完成了，通知GameManager
        if (IsAllRoundsFinished())
        {
            cardUIManager.endBtnText.text = $"单局结束";
            currentState = GameState.GameOver;
            GameManager.instance.OnGameFinished();
        }
        else
        {
            cardUIManager.endBtnText.text = $"还剩{aiSettings.roundsPerGame - currentRoundIndex}局";
            // 开始新的小局
            StartNewRound();
            // 重置游戏状态
            currentState = GameState.WaitForNextRound;
        }

        UpdateUIButtons();
    }


    public void OnEndRound()
    {
        if (IsAllRoundsFinished())
        {
        }
        else
        {
            cardUIManager.endPanel.SetActive(false);
            currentState = GameState.WaitingForAITurn;
        }
    }

    public float fff;
    public int constW = 7;

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

        for (int i = 0; i < 10 * MAX_CARDS; i++)
        {
            scenePositions[i] = (Vector2)SceneCardsPos.position +
                                new Vector2((i % constW) * fff, -(i / constW) * 2 * fff);
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

        // 记录摸牌
        RecordCardDrawn();

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
            int cardValue = aiSettings.GetCardValue(card.cardNumber);

            if (card.cardNumber == 1)
            {
                // A先按1计算，记录A的数量
                aceCount++;
                totalPoints += 1;
            }
            else
            {
                totalPoints += cardValue;
            }
        }

        // 计算最佳点数（A可以当作11，但不超过21）
        int bestPoints = totalPoints;
        for (int i = 0; i < aceCount; i++)
        {
            if (bestPoints + 10 <= 21)
            {
                bestPoints += 10;
            }
            else
            {
                break;
            }
        }

        return (totalPoints, bestPoints);
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
        return currentPoints < aiSettings.aiThreshold && currentPoints < 21;
    }

    /// <summary>
    /// 玩家是否可以摸牌
    /// </summary>
    public bool CanPlayerDrawCard()
    {
        var (currentPoints, _) = GetPlayerPoints();
        return currentPoints < 21 && myCardNum < MAX_CARDS && cardIndex >= 0;
    }

    // ========== 小局管理方法（合并自RoundManager） ==========

    /// <summary>
    /// 初始化小局
    /// </summary>
    private void InitRounds()
    {
        currentRoundIndex = 0;
        roundResults.Clear();
        usedCardsCount.Clear();
        usedCards.Clear();
        cardsDrawnCount = 0;
    }

    /// <summary>
    /// 开始新的小局
    /// </summary>
    private void StartNewRound()
    {
        if (currentRoundIndex >= aiSettings.roundsPerGame)
        {
            Debug.LogWarning("所有小局已完成");
            return;
        }

        cardUIManager.aiCardsText.text = "";
        currentRoundIndex++;
        cardsDrawnCount = 0;

        // 清空手牌，准备新小局
        mineCards.Clear();
        aiCards.Clear();
        myCardNum = 0;
        AICardNum = 0;

        Debug.Log($"开始第{currentRoundIndex}个小局");
    }

    /// <summary>
    /// 记录摸牌
    /// </summary>
    private void RecordCardDrawn()
    {
        cardsDrawnCount++;
    }

    /// <summary>
    /// 结束当前小局（在这里检测暴露）
    /// </summary>
    private RoundResult EndRound()
    {
        int cardsPlayedCount = mineCards.Count;
        bool exposed = false;

        // 检测1：手牌数量暴露
        if (aiSettings.checkHandCountExposure && cardsPlayedCount != cardsDrawnCount)
        {
            cardUIManager.endText.text = $"手牌数量暴露！摸了{cardsDrawnCount}张牌，但打出了{cardsPlayedCount}张牌";
            exposed = true;
        }

        // 检测2：单牌数量暴露（AI审查：场上牌数量+AI牌数量）
        if (!exposed && aiSettings.checkCardCountExposure && AICheckCardCount())
        {
            exposed = true;
        }


        // 记录打出的牌（不回归牌堆）
        foreach (var card in mineCards)
        {
            usedCards.Add(card);

            // 统计每种牌的数量
            if (!usedCardsCount.ContainsKey(card.cardNumber))
            {
                usedCardsCount[card.cardNumber] = 0;
            }

            usedCardsCount[card.cardNumber]++;
        }

        foreach (var card in aiCards)
        {
            usedCards.Add(card);

            // 统计每种牌的数量
            if (!usedCardsCount.ContainsKey(card.cardNumber))
            {
                usedCardsCount[card.cardNumber] = 0;
            }

            usedCardsCount[card.cardNumber]++;
        }

        // 将牌移到场上显示区域并缩小
        MoveCardsToField();

        if (exposed)
        {
            roundResults.Add(RoundResult.Lose);
            return RoundResult.Lose;
        }

        if (!exposed)
        {
            RoundResult result = RoundResult.Win;
            // 计算小局结果
            var (playerMin, playerMax) = GetPlayerPoints();
            var (aiMin, aiMax) = GetAIPoints();

            int playerPoints = playerMax <= 21 ? playerMax : playerMin;
            int aiPoints = aiMax <= 21 ? aiMax : aiMin;

            cardUIManager.aiCardsText.text = aiPoints.ToString();
            // 判断胜负
            if (playerPoints > 21 && aiPoints > 21)
            {
                result = RoundResult.Draw;
                cardUIManager.endText.text = "双方点数都超过21点，平局";
            }
            else if (aiPoints > 21)
            {
                result = RoundResult.Win; // AI爆牌
                cardUIManager.endText.text = "对手点数都超过21点，获得1分";
            }
            else if (playerPoints > 21)
            {
                result = RoundResult.Lose;
                cardUIManager.endText.text = "点数都超过21点，减去1分";
            }
            else if (playerPoints > aiPoints)
            {
                result = RoundResult.Win;
                cardUIManager.endText.text = "点数超过对手，获得1分";
            }
            else if (aiPoints > playerPoints)
            {
                result = RoundResult.Lose;
                cardUIManager.endText.text = "点数未超过对手，减去1分";
            }
            else
            {
                result = RoundResult.Draw;
                cardUIManager.endText.text = "点数相同，平局";
            }

            roundResults.Add(result);
            Debug.Log($"第{currentRoundIndex}个小局结束：{result} (玩家{playerPoints}点 vs AI{aiPoints}点)");
            return result;
        }

        return RoundResult.Win;
    }

    public int indexScene;

    /// <summary>
    /// 将牌移到场上显示区域并缩小
    /// </summary>
    private void MoveCardsToField()
    {
        // 移动玩家牌
        foreach (var card in mineCards)
        {
            card.cardState = CardState.InScene;
            card.transform.DOMove(scenePositions[indexScene], 0.5f);
            card.transform.DOScale(playedCardScale, 0.5f);
            indexScene++;
        }

        // 移动AI牌
        foreach (var card in aiCards)
        {
            card.cardState = CardState.InScene;
            card.transform.DOMove(scenePositions[indexScene], 0.5f);
            card.transform.DOScale(playedCardScale, 0.5f);
            indexScene++;
        }
    }

    /// <summary>
    /// AI审查牌型（检测场上牌数量+AI牌数量）
    /// </summary>
    private bool AICheckCardCount()
    {
        Dictionary<int, int> totalCardCount = new Dictionary<int, int>();

        // 统计已打出的牌（场上牌）
        foreach (var kvp in usedCardsCount)
        {
            totalCardCount[kvp.Key] = kvp.Value;
        }

        // 统计玩家当前手牌
        foreach (var card in mineCards)
        {
            if (!totalCardCount.ContainsKey(card.cardNumber))
            {
                totalCardCount[card.cardNumber] = 0;
            }

            totalCardCount[card.cardNumber]++;
        }

        // 统计AI手牌（AI审查时会检查自己的牌）
        foreach (var card in aiCards)
        {
            if (!totalCardCount.ContainsKey(card.cardNumber))
            {
                totalCardCount[card.cardNumber] = 0;
            }

            totalCardCount[card.cardNumber]++;
        }

        // 检查是否有牌超过限制
        foreach (var kvp in totalCardCount)
        {
            int cardNumber = kvp.Key;
            int count = kvp.Value;
            int maxCount = aiSettings.GetMaxCardCount(cardNumber);

            if (count > maxCount)
            {
                cardUIManager.endText.text = $"AI审查发现单牌数量暴露！牌{cardNumber}总共{count}张" +
                                             $"（场上{usedCardsCount.GetValueOrDefault(cardNumber, 0)}张+玩家手牌{mineCards.FindAll(c => c.cardNumber == cardNumber).Count}张+" +
                                             $"AI手牌{aiCards.FindAll(c => c.cardNumber == cardNumber).Count}张），" +
                                             $"但最多只有{maxCount}张";

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 是否所有小局都已完成
    /// </summary>
    public bool IsAllRoundsFinished()
    {
        return currentRoundIndex >= aiSettings.roundsPerGame;
    }

    /// <summary>
    /// 获取总分数
    /// </summary>
    public int GetTotalScore()
    {
        int score = 0;
        foreach (var result in roundResults)
        {
            switch (result)
            {
                case RoundResult.Win:
                    score += 1;
                    break;
                case RoundResult.Draw:
                    score += 0;
                    break;
                case RoundResult.Lose:
                    score -= 1;
                    break;
            }
        }

        return score;
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 size = new Vector3(1, 1, 1);

        if (cardParent != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(cardParent.position, size);
        }

        if (AIPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(AIPos.position, size);
        }

        if (MyPos != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(MyPos.position, size);
        }

        if (SceneCardsPos != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(SceneCardsPos.position, size);
        }
    }
}