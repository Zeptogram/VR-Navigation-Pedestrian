using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MyConstants
{
    //settings
    public const int viewAngle = 180;
    public const int rayLength = 30;
    public const int numberOfRaysPerSide = 11;
    //speed
    public static Vector2 minMaxSpeed = new Vector2 (0f, 1.7f);
    public static Vector2 speedMaxRange = new Vector2 (1.3f, 1.7f);
    //angle
    public const int angleRange = 25;

    public const bool discrete = false;

    //
    public const float MAXIMUM_VIEW_DISTANCE = 14f; 
    public const float MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE = 6f; 
    //reward
    public const float step_reward = -0.0001f;
    public const float step_finished_reward = -6f;
    public const float finale_target_reward = 6f;
    public const float new_target_reward = 0.5f;
    public const float already_taken_target_reward = -1f;
    public const float target_taken_incorrectly_reward = -1f;
    public const float not_watching_target_reward = -0.5f;
    //Proxemix distances
    public static float proxemic_small_distance = 0.6f - rayOffset;
    public static float proxemic_medium_distance = 1.0f - rayOffset;
    public static float proxemic_large_distance = 1.4f - rayOffset;
    //Proxemix ray
    public const float proxemic_small_ray = 11;
    public const float proxemic_medium_ray = 10;
    public const float proxemic_large_ray = 9;
    //Proxemix rewards
    public const float proxemic_large_agent_reward = -0.001f;
    public const float proxemic_medium_agent_reward = -0.005f;
    public const float proxemic_small_agent_reward = -0.5f;
    public const float proxemic_small_wall_reward = -0.5f;
    public static float rayOffset = 0.04f;
    public static float verticalRayOffset = -0.5f;
    //Proxemic
    public static readonly Proxemic[] Proxemics = new Proxemic[]{
       new Proxemic(proxemic_small_distance, 11),
       new Proxemic(proxemic_medium_distance, 7),
       new Proxemic(proxemic_large_distance, 6),
    };
}
