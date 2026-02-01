
    using System;
    using UnityEngine;

    public class DontDes:MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
