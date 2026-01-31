using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 牌堆配置 - 静态类，定义所有牌的数量
/// 每张牌只有3张（AI知道这个规则）
/// 1=A, 2-10=数字牌, 11=J, 12=Q, 13=K
/// </summary>
public static class DeckConfig
{
    public static Dictionary<int, int> CardNumbers = new Dictionary<int, int>
    {
        { 1, 3 },   // A
        { 2, 3 },
        { 3, 3 },
        { 4, 3 },
        { 5, 3 },
        { 6, 3 },
        { 7, 3 },
        { 8, 3 },
        { 9, 3 },
        { 10, 3 },  // 10, J, Q, K都算10点
        { 11, 3 },  // J
        { 12, 3 },  // Q
        { 13, 3 },  // K
    };
    
    /// <summary>
    /// 获取卡牌的点数（J/Q/K都算10点）
    /// </summary>
    public static int GetCardValue(int cardNumber)
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
    public static int GetMaxCardCount(int cardNumber)
    {
        return CardNumbers[cardNumber];
    }
}