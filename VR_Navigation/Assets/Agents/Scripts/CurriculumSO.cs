using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[CreateAssetMenu(fileName = "CV_", menuName = "ScriptableObjects/SpawnManagerScriptableObject", order = 1)]

public class CurriculumSO : ScriptableObject
{
    public EnvironmentStruct[] Environments;
}
[Serializable]
public struct EnvironmentStruct
{
    public Environment envGameObject;
    public float minScore;
    public int maxEpisodes;
}