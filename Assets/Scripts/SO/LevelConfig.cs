using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mask21/Level Config", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Tooltip("One entry per CardManager (table). If less than tables, remaining tables will be disabled.")]
    public List<AISettings> aiSettings = new List<AISettings>();
}
