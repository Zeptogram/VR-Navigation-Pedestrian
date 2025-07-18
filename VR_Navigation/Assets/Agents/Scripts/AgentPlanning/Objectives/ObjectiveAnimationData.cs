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
    
    [Header("Look At Settings")]
    [Tooltip("If true, the agent will smoothly look at the specified target when reaching this objective")]
    public bool enableLookAt = false;
    
    [Tooltip("The transform target to look at. If null, no look at will be performed")]
    public Transform lookAtTarget = null;
    
    [Tooltip("Speed of the smooth rotation (degrees per second)")]
    [Range(30f, 360f)]
    public float lookAtSpeed = 90f;
    
    [Tooltip("If true, the look at happens before the animations. If false, it happens during the first animation")]
    public bool lookAtBeforeAnimations = true;
    
    [Tooltip("If true, only rotates on the Y axis (horizontal look). If false, full 3D rotation")]
    public bool horizontalLookOnly = true;
}