using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public CardManager[] cardManagers;
    
   
    
   
    
    // 当前操作的CardManager索引（如果有多个CardManager）
    private int currentManagerIndex = 0;

    public void Start()
    {
        instance = this;
        foreach (CardManager cardManager in cardManagers)
        {
            cardManager.Init();
        }
        
        // 给ai发两张牌
        foreach (CardManager cardManager in cardManagers)
        {
            // cardManager.SendAICard();
            // cardManager.SendAICard();
            //
            // cardManager.SendMineCard();
            // cardManager.SendMineCard();
        }
        

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

    private void Update()
    {
        
    }
}