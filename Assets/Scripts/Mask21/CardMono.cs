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

    private void OnMouseDown()
    {
        if (cardState == CardState.Mine)
        {
            // 计算：物体世界坐标 - 鼠标转世界后的坐标 = 偏移量
            _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        }
    }

    // 鼠标按住并拖拽时持续触发（OnMouseDown后才会执行）
    private void OnMouseDrag()
    {
        if (cardState == CardState.Mine)
        {
            // 实时更新物体位置：鼠标世界坐标 + 偏移量（保持鼠标相对物体的位置不变）
            transform.position = GetMouseWorldPosition() + _mouseOffset;
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