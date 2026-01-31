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

/// <summary>
/// 小局结果
/// </summary>
public enum RoundResult
{
    Win, // 胜利 +1
    Draw, // 平局 0
    Lose // 失败 -1
}

/// <summary>
/// 暴露类型
/// </summary>
public enum ExposureType
{
    None, // 未暴露
    CardCountExposure, // 单牌数量暴露（某种牌超过3张）
    HandCountExposure // 手牌数量暴露（摸牌数和出牌数不匹配）
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

    // AI决策阈值（超过这个点数就不摸牌）
    public int aiThreshold = 17;

    // ========== 小局管理功能（合并自RoundManager） ==========
    // 小局相关
    private int currentRoundIndex = 0;
    private int totalRounds = 0; // 2-3个小局
    private List<RoundResult> roundResults = new List<RoundResult>();

    // 暴露检测相关
    private int cardsDrawnCount = 0; // 本小局摸牌数
    private Dictionary<int, int> usedCardsCount = new Dictionary<int, int>(); // 已打出的牌统计（跨小局）
    private List<CardMono> usedCards = new List<CardMono>(); // 所有已打出的牌（不回归牌堆）

    // 暴露状态
    public ExposureType exposureType = ExposureType.None;
    public bool isExposed = false;

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

        // 初始化小局
        InitRounds();

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

        // 结束当前小局（在这里检测暴露）
        RoundResult result = EndRound();
        Debug.Log($"小局结果：{result}");

        // 如果所有小局都完成了，通知GameManager
        if (IsAllRoundsFinished())
        {
            currentState = GameState.GameOver;
            //GameManager.instance?.OnGameFinished();
        }
        else
        {
            // 开始新的小局
            StartNewRound();
            // 重置游戏状态
            currentState = GameState.WaitingForAITurn;
        }

        UpdateUIButtons();
    }


    public float fff;

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
            scenePositions[i] = (Vector2)SceneCardsPos.position + new Vector2(i * fff, 0);
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
            int cardValue = DeckConfig.GetCardValue(card.cardNumber);

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

    // ========== 小局管理方法（合并自RoundManager） ==========

    /// <summary>
    /// 初始化小局
    /// </summary>
    private void InitRounds(int rounds = 0)
    {
        if (rounds == 0)
        {
            // 随机2-3个小局
            totalRounds = UnityEngine.Random.Range(2, 4);
        }
        else
        {
            totalRounds = rounds;
        }

        currentRoundIndex = 0;
        roundResults.Clear();
        usedCardsCount.Clear();
        usedCards.Clear();
        cardsDrawnCount = 0;
        exposureType = ExposureType.None;
        isExposed = false;

        Debug.Log($"初始化小局：共{totalRounds}个小局");
    }

    /// <summary>
    /// 开始新的小局
    /// </summary>
    private void StartNewRound()
    {
        if (currentRoundIndex >= totalRounds)
        {
            Debug.LogWarning("所有小局已完成");
            return;
        }

        currentRoundIndex++;
        cardsDrawnCount = 0;
        exposureType = ExposureType.None;
        isExposed = false;

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
        RoundResult result = RoundResult.Win;

        // 小局结束时检测暴露
        int cardsPlayedCount = mineCards.Count;

        // 检测1：手牌数量暴露
        if (cardsPlayedCount != cardsDrawnCount)
        {
            exposureType = ExposureType.HandCountExposure;
            isExposed = true;
            Debug.Log($"手牌数量暴露！摸了{cardsDrawnCount}张牌，但打出了{cardsPlayedCount}张牌");
            roundResults.Add(RoundResult.Lose);
            result = RoundResult.Lose;
        }

        // 检测2：单牌数量暴露（AI审查：场上牌数量+AI牌数量）
        if (AICheckCardCount())
        {
            exposureType = ExposureType.CardCountExposure;
            isExposed = true;
            Debug.LogWarning($"单牌数量暴露！");
            roundResults.Add(RoundResult.Lose);
            result = RoundResult.Lose;
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

        if (result == RoundResult.Win)
        {
            // 计算小局结果
            var (playerMin, playerMax) = GetPlayerPoints();
            var (aiMin, aiMax) = GetAIPoints();

            int playerPoints = playerMax <= 21 ? playerMax : playerMin;
            int aiPoints = aiMax <= 21 ? aiMax : aiMin;


            // 判断胜负
            if (playerPoints > 21 && aiPoints > 21)
            {
                result = RoundResult.Draw;
            }
            else if (aiPoints > 21)
            {
                result = RoundResult.Win; // AI爆牌
            }
            else if (playerPoints > 21)
            {
                result = RoundResult.Lose;
            }
            else if (playerPoints > aiPoints)
            {
                result = RoundResult.Win;
            }
            else if (aiPoints > playerPoints)
            {
                result = RoundResult.Lose;
            }
            else
            {
                result = RoundResult.Draw;
            }

            roundResults.Add(result);
            Debug.Log($"第{currentRoundIndex}个小局结束：{result} (玩家{playerPoints}点 vs AI{aiPoints}点)");
        }

        return result;
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
            int maxCount = DeckConfig.GetMaxCardCount(cardNumber);

            if (count > maxCount)
            {
                Debug.LogWarning(
                    $"AI审查发现单牌数量暴露！牌{cardNumber}总共{count}张（场上{usedCardsCount.GetValueOrDefault(cardNumber, 0)}张+玩家手牌{mineCards.FindAll(c => c.cardNumber == cardNumber).Count}张+AI手牌{aiCards.FindAll(c => c.cardNumber == cardNumber).Count}张），但最多只有{maxCount}张");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 是否所有小局都已完成
    /// </summary>
    private bool IsAllRoundsFinished()
    {
        return currentRoundIndex >= totalRounds;
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
}