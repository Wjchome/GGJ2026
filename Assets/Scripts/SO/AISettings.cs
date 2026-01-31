using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AISettings
{
    [Header("Round Rules")] public int roundsPerGame;

    [Header("Audit Rules")] public bool checkHandCountExposure = false;
    public bool checkCardCountExposure = false;

    [Header("Play Style")] [Range(1, 20)] public int aiThreshold = 17;

    public List<int> CardNumbers;
    
    
    public int GetCardValue(int cardNumber)
    {
        if (cardNumber >= 1 && cardNumber <= 10)
        {
            return cardNumber;
        }
        else if (cardNumber >= 11 && cardNumber <= 13)
        {
            return 10; // J, Q, K都算10点
        }
        return 0;
    }
    
    /// <summary>
    /// 获取卡牌的最大数量（每种牌都是3张）
    /// </summary>
    public int GetMaxCardCount(int cardNumber)
    {
        return CardNumbers[cardNumber];
    }
}