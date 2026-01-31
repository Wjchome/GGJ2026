using System;
using UnityEngine;


public class CardMono : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public CardManager cardManager;
    public int cardNumber;
    public bool isBack = false;

    private void Start()
    {
        ChangeSprite();
    }

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
}