using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏管理器 
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public CardManager[] cardManagers; // 并行的多个CardManager
    public LevelConfig levelConfig; // 编辑器中的默认配置（如果菜单没有选择，则使用此配置）

    // 从菜单选择的关卡配置
    public static LevelConfig selectedLevelConfig;

    // UI显示
    public Text totalScoreText;
    public Text gameStatusText;
    public Button backBtn;

    public bool isGameFinished = false;

    public void Start()
    {
        instance = this;

        if (cardManagers == null || cardManagers.Length == 0)
        {
            Debug.LogError("GameManager需要至少1个CardManager！");
            return;
        }

        // 优先使用从菜单选择的配置，如果没有则使用编辑器中的配置
        LevelConfig configToUse = selectedLevelConfig != null ? selectedLevelConfig : levelConfig;
        
        if (configToUse == null)
        {
            Debug.LogError("GameManager需要LevelConfig配置！");
            return;
        }

        if (configToUse.aiSettings == null || configToUse.aiSettings.Count == 0)
        {
            Debug.LogError("LevelConfig的aiSettings为空！");
            return;
        }

        if (configToUse.aiSettings.Count > cardManagers.Length)
        {
            Debug.LogWarning($"LevelConfig配置了{configToUse.aiSettings.Count}个AI，但只有{cardManagers.Length}个CardManager！");
        }
        
        // 初始化所有启用的CardManager
        int initCount = Mathf.Min(configToUse.aiSettings.Count, cardManagers.Length);
        for (int i = 0; i < initCount; i++)
        {
            CardManager cardManager = cardManagers[i];
            if (cardManager != null)
            {
                cardManager.Init(configToUse.aiSettings[i]);
            }
        }

        // 禁用多余的CardManager
        for (int i = initCount; i < cardManagers.Length; i++)
        {
            if (cardManagers[i] != null)
            {
                cardManagers[i].gameObject.SetActive(false);
            }
        }

        UpdateGameStatus();
        
        backBtn.gameObject.SetActive(false);
    }

    /// <summary>
    /// 将卡牌从一个CardManager转移到另一个CardManager
    /// </summary>
    /// <param name="card">要转移的卡牌</param>
    /// <param name="fromManager">源CardManager</param>
    /// <param name="toManager">目标CardManager</param>
    public bool TransferCard(CardMono card, CardManager fromManager, CardManager toManager)
    {
        // 如果源和目标相同，不处理
        if (fromManager == toManager)
        {
            return false;
        }

        // 如果目标CardManager已满，不转移
        if (toManager.myCardNum >= 8)
        {
            Debug.LogWarning("目标CardManager已满，无法转移卡牌");
            return false;
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
        return true;
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

        isGameFinished = AreAllActiveTablesFinished();

        UpdateGameStatus();
    }

    /// <summary>
    /// 更新游戏状态显示
    /// </summary>
    private void UpdateGameStatus()
    {
        int sum = 0;
        string status = "";
        int tableIndex = 0;
        foreach (var cardManager in cardManagers)
        {
            if (cardManager == null || !cardManager.gameObject.activeInHierarchy)
            {
                continue;
            }

            int score = cardManager.GetTotalScore();
            status += $"局{tableIndex + 1}：{score}分  ";
            sum += score;
            tableIndex++;
        }

        totalScoreText.text = status;

        if (isGameFinished)
        {
            if (sum > 0)
            {
                gameStatusText.text = $"你成功了... 总分:{sum}";
            }
            else
            {
                gameStatusText.text = $"你失败了... 总分:{sum}";
            }
            gameStatusText.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0f, 280f), 0.5f);
            totalScoreText.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0f, 230f), 0.5f);
            backBtn.gameObject.SetActive(true);
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(() => { SceneManager.LoadScene("Enter"); });
            
        }
        else
        {
            gameStatusText.text = $"游戏中... 总分:{sum}";
        }
    }
    

    private bool AreAllActiveTablesFinished()
    {
        foreach (var cardManager in cardManagers)
        {
            if (!cardManager.IsAllRoundsFinished())
            {
                return false;
            }
        }

        return true;
    }
}