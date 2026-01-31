using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 牌堆配置 - 静态类，定义所有牌的数量
/// 每张牌只有3张（AI知道这个规则）
/// </summary>
public static class DeckConfig
{
    public static Dictionary<int, int> CardNumbers = new Dictionary<int, int>
    {
        { 1, 3 },
        { 2, 3 },
        { 3, 3 },
        { 4, 3 },
        { 5, 3 },
        { 6, 3 },
        { 7, 3 },
        { 8, 3 },
        { 9, 3 },
        { 10, 9 },
    };
    
}