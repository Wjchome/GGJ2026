using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CardManager : MonoBehaviour
{
    public Sprite[] cardSprites;

    public Sprite cardBack;

    public List<CardMono> cards;
    

    public CardMono cardPrefab;
    public Transform cardParent;

    public Transform AIPos;
    public Transform MyPos;

    public float f;


    public Button getCardBtn;

    public void Init()
    {
        var config = DeckConfig.CardNumbers;
        var list = ExpandCardDictionary(config);
        ShuffleCardList(list);
        cardIndex = list.Count - 1;
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
        
        getCardBtn.onClick.RemoveAllListeners();
        getCardBtn.onClick.AddListener(() => SendMineCard());
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
    public void SendAICard()
    { 
        cards[cardIndex].transform.position = (Vector2)AIPos.position+new Vector2(0,-AICardNum*ff);
        cards[cardIndex].spriteRenderer.sortingOrder = AICardNum;
        cards[cardIndex].isBack = true;
        cards[cardIndex].cardState = CardState.AI;
        cards[cardIndex].ChangeSprite();
        cardIndex--;
        AICardNum++;
    }



    public int myCardNum;
    public void SendMineCard()
    {
        cards[cardIndex].transform.position = (Vector2)MyPos.position+new Vector2(0,-myCardNum*ff);
        cards[cardIndex].spriteRenderer.sortingOrder =myCardNum;
        cards[cardIndex].isBack = false;
        cards[cardIndex].cardState = CardState.Mine;
        cards[cardIndex].ChangeSprite();
        cardIndex--;
        myCardNum++;
    }
}