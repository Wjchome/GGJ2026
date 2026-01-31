using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏管理器 - 管理两个独立局的21点游戏
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public CardManager[] cardManagers; // 两个独立的CardManager
    
    // UI显示
    public Text totalScoreText;
    public Text gameStatusText;
    
    // 游戏状态
    private bool isGameFinished = false;

    public void Start()
    {
        instance = this;
        
        // 确保有两个CardManager
        if (cardManagers == null || cardManagers.Length != 2)
        {
            Debug.LogError("GameManager需要2个CardManager！");
            return;
        }
        
        // 初始化两个独立局
        foreach (CardManager cardManager in cardManagers)
        {
            cardManager.Init();
        }
        
        UpdateGameStatus();
    }
    
    /// <summary>
    /// 将卡牌从一个CardManager转移到另一个CardManager
    /// </summary>
    /// <param name="card">要转移的卡牌</param>
    /// <param name="fromManager">源CardManager</param>
    /// <param name="toManager">目标CardManager</param>
    public void TransferCard(CardMono card, CardManager fromManager, CardManager toManager)
    {
        // 如果源和目标相同，不处理
        if (fromManager == toManager)
        {
            return;
        }
        
        // 如果目标CardManager已满，不转移
        if (toManager.myCardNum >= 8)
        {
            Debug.LogWarning("目标CardManager已满，无法转移卡牌");
            return;
        }
        
        // 从源CardManager移除
        if (fromManager != null)
        {
            fromManager.RemoveMineCard(card);
        }
        
        // 添加到目标CardManager
        if (toManager != null)
        {
            bool success = toManager.AddMineCard(card);
            if (!success)
            {
                // 如果添加失败，尝试恢复到源CardManager
                if (fromManager != null)
                {
                    fromManager.AddMineCard(card);
                }
            }
        }

        fromManager.UpdateUIButtons();
        toManager.UpdateUIButtons();
    }
    
    // 注意：暴露检测现在在小局结束时进行，不再需要实时检测
    
    /// <summary>
    /// 游戏结束时的处理
    /// </summary>
    public void OnGameFinished()
    {
        if (isGameFinished)
        {
            return;
        }
        
        isGameFinished = true;
        
        // 计算总分数
        int totalScore = GetTotalScore();
        
        // 判断胜利
        bool isVictory = totalScore > 0;
        
        Debug.Log($"游戏结束！总分数：{totalScore}，结果：{(isVictory ? "胜利" : "失败")}");
        
        UpdateGameStatus();
    }
    
    /// <summary>
    /// 获取总分数
    /// </summary>
    public int GetTotalScore()
    {
        int totalScore = 0;
        
        foreach (var cardManager in cardManagers)
        {
            totalScore += cardManager.GetTotalScore();
        }
        
        return totalScore;
    }
    
    /// <summary>
    /// 更新游戏状态显示
    /// </summary>
    private void UpdateGameStatus()
    {
        if (totalScoreText != null)
        {
            int totalScore = GetTotalScore();
            totalScoreText.text = $"总分数：{totalScore}";
            
            if (isGameFinished)
            {
                bool isVictory = totalScore > 0;
                totalScoreText.text += $" - {(isVictory ? "胜利！" : "失败...")}";
            }
        }
        
        if (gameStatusText != null)
        {
            string status = "";
            foreach (var cardManager in cardManagers)
            {
                int score = cardManager.GetTotalScore();
                status += $"局{Array.IndexOf(cardManagers, cardManager) + 1}：{score}分  ";
            }
            gameStatusText.text = status;
        }
    }

    private void Update()
    {
        // 可以在这里添加实时更新
    }
}
