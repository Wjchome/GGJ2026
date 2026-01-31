using System;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    public CardManager[] cardManagers;

    public void Start()
    {
        foreach (CardManager cardManager in cardManagers)
        {
            cardManager.Init();
        }
        //给ai发两张牌
        foreach (CardManager cardManager in cardManagers)
        {
            cardManager.SendAICard();
            cardManager.SendAICard();

            cardManager.SendMineCard();
            cardManager.SendMineCard();
        }
    }


    private void Update()
    {
        
    }
}