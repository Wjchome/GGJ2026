using System;
using UnityEngine;

public enum CardState
{
    AI,
    Mine,
    InScene,
    InDeck //牌堆
}

public class CardMono : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public CardManager cardManager;
    public int cardNumber;
    public bool isBack = false;

    public CardState cardState = CardState.InDeck;
    // private void Start()
    // {
    //     ChangeSprite();
    // }

    public void ChangeSprite()
    {
        if (isBack)
        {
            spriteRenderer.sprite = cardManager.GetBack();
        }
        else
        {
            spriteRenderer.sprite = cardManager.GetSprite(cardNumber);
        }
    }

    public Vector2 _mouseOffset;
    private Vector2 _dragStartPosition;
    private bool _isDragging = false;

    private void OnMouseDown()
    {
        if (cardState == CardState.Mine)
        {
            // 计算：物体世界坐标 - 鼠标转世界后的坐标 = 偏移量
            _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
            _dragStartPosition = transform.position;
            _isDragging = true;
        }
    }

    // 鼠标按住并拖拽时持续触发（OnMouseDown后才会执行）
    private void OnMouseDrag()
    {
        if (cardState == CardState.Mine && _isDragging)
        {
            // 实时更新物体位置：鼠标世界坐标 + 偏移量（保持鼠标相对物体的位置不变）
            transform.position = GetMouseWorldPosition() + _mouseOffset;
        }
    }
    
    private void OnMouseUp()
    {
        if (cardState == CardState.Mine && _isDragging)
        {
            _isDragging = false;
            
            // 检测卡牌是否拖拽到屏幕左右两侧
            // 直接使用屏幕坐标判断，更准确
            float mouseScreenX = Input.mousePosition.x;
            float screenCenterX = Screen.width / 2f;
            
            // 如果拖拽到右边屏幕，转移到CardManager[1]
            // 如果拖拽到左边屏幕，转移到CardManager[0]
            if (mouseScreenX > screenCenterX)
            {
                // 右边屏幕，转移到CardManager[1]
                TransferCardToManager(1);
            }
            else
            {
                // 左边屏幕，转移到CardManager[0]
                TransferCardToManager(0);
            }
        }
    }
    
    /// <summary>
    /// 将卡牌转移到指定的CardManager
    /// </summary>
    private void TransferCardToManager(int managerIndex)
    {
        // 通过GameManager来转移卡牌
        GameManager gameManager = GameManager.instance;
        if ( managerIndex >= 0 && managerIndex < gameManager.cardManagers.Length)
        {
            CardManager targetManager = gameManager.cardManagers[managerIndex];
            
            // 如果目标CardManager就是当前CardManager，返回原位置
            if (targetManager == cardManager)
            {
                transform.position = _dragStartPosition;
                return;
            }
            
            gameManager.TransferCard(this, cardManager, targetManager);
        }
        else
        {
            // 如果找不到GameManager或索引无效，返回原位置
            transform.position = _dragStartPosition;
        }
    }

    /// <summary>
    /// 屏幕坐标转2D世界坐标（关键：z轴设为相机到2D平面的距离，避免坐标穿透）
    /// </summary>
    /// <returns>鼠标在2D世界中的坐标</returns>
    private Vector2 GetMouseWorldPosition()
    {
        // 获取鼠标屏幕坐标（x=横向，y=纵向，z=0）
        Vector2 mouseScreenPos = Input.mousePosition;
        // 2D场景关键：设置z轴为相机的z轴绝对值（相机在2D层，z为负数，保证坐标在2D平面）
        // 屏幕坐标转世界坐标，返回2D平面有效坐标
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}