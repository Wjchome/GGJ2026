using System;
using System.Collections.Generic;
using UnityEngine;


public class CardManager : MonoBehaviour
{
    public Sprite[] cardSprites;

    public Sprite cardBack;

    public List<CardMono> cards;

    public CardMono cardPrefab;
    public Transform cardParent;

    public float f;

    public void Start()
    {
        var config = DeckConfig.CardNumbers;
        var list = ExpandCardDictionary(config);
        ShuffleCardList(list);
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
    }

    /// <summary>
    /// 将卡牌配置的Dictionary展开为完整列表（键=卡牌数字，值=数量）
    /// </summary>
    /// <param name="cardDict">DeckConfig中的卡牌配置字典</param>
    /// <returns>包含所有卡牌的原始列表（未打乱）</returns>
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

    /// <summary>
    /// Fisher-Yates（费雪耶茨）洗牌算法：原地打乱卡牌列表，公平无偏置
    /// </summary>
    /// <param name="cardList">需要打乱的卡牌列表</param>
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
}