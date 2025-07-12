using UnityEngine;
using System;

[Serializable]
public class AnimationAction
{
    public string animationTrigger = "isIdle";  
    
    [Header("Stop Duration")]
    public float duration = 2f;
    
    [Header("Optional Description")]
    public string description = "";
}

public class ObjectiveAnimationData : MonoBehaviour
{
    public AnimationAction[] animationActions = new AnimationAction[]
    {
        new AnimationAction { animationTrigger = "isIdle", duration = 2f, description = "Rimani fermo per 2 secondi" }
    };
    
    [Header("Sequences")]
    public bool playInSequence = true;
    
    [Header("Delay")]
    public float delayBetweenAnimations = 0.5f;
    
    [Header("Stop")]
    public bool stopAgentDuringAnimations = true;
    
    [Header("Ignores the invidual durations, -1 means disabled")]
    public float totalDuration = -1f;
}