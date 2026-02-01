using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 菜单管理器
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("UI组件")] public Button exitGameBtn; // 退出游戏按钮
    public Button creditsBtn; // 制作者按钮
    public List<Button> levelButtons = new List<Button>(); // 5个关卡按钮

    [Header("制作者面板")] public GameObject creditsPanel; // 制作者面板
    public Button closeCreditsBtn; // 关闭制作者面板按钮

    [Header("关卡配置")] public List<LevelConfig> levelConfigNames = new List<LevelConfig>();

    private void Start()
    {
        SetupButtons();

        // 初始化时关闭制作者面板
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        // 退出游戏按钮
        if (exitGameBtn != null)
        {
            exitGameBtn.onClick.RemoveAllListeners();
            exitGameBtn.onClick.AddListener(OnExitGame);
        }

        // 制作者按钮
        if (creditsBtn != null)
        {
            creditsBtn.onClick.RemoveAllListeners();
            creditsBtn.onClick.AddListener(OnShowCredits);
        }

        // 关闭制作者面板按钮
        if (closeCreditsBtn != null)
        {
            closeCreditsBtn.onClick.RemoveAllListeners();
            closeCreditsBtn.onClick.AddListener(OnCloseCredits);
        }

        // 关卡按钮
        for (int i = 0; i < levelButtons.Count && i < levelConfigNames.Count; i++)
        {
            if (levelButtons[i] != null)
            {
                int levelIndex = i; // 闭包需要局部变量
                levelButtons[i].onClick.RemoveAllListeners();
                levelButtons[i].onClick.AddListener(() => OnSelectLevel(levelIndex));
            }
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void OnExitGame()
    {
#if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建版本中退出应用
        Application.Quit();
#endif
    }

    /// <summary>
    /// 显示制作者信息
    /// </summary>
    private void OnShowCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }

    }

    /// <summary>
    /// 关闭制作者面板
    /// </summary>
    private void OnCloseCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 选择关卡
    /// </summary>
    private void OnSelectLevel(int levelIndex)
    {

        
        // 从Resources加载LevelConfig
        LevelConfig levelConfig = levelConfigNames[levelIndex];
        

        // 检查配置支持的人数（2人或3人）
        int playerCount = levelConfig.aiSettings.Count;

        if (playerCount < 2 || playerCount > 3)
        {
            return;
        }

        // 保存关卡配置到静态变量，供GameManager使用
        GameManager.selectedLevelConfig = levelConfig;

        // 根据人数加载对应场景
        string sceneName = playerCount == 2 ? "TwoAIScene" : "ThreeAIScene";
        SceneManager.LoadScene(sceneName);
    }
    
}