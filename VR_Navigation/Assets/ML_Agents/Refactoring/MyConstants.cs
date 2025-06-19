using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MyConstants
{
    //settings
    public const int viewAngle = 180;
    public const int rayLength = 43;
    public const int numberOfRaysPerSide = 11;
    //speed
    public static Vector2 minMaxSpeed = new Vector2 (0f, 1.7f);
    //float randomValue = RLAgent.RandomGaussian(0.65f, 2.15f); centrata in 1.4 con deviazione di 0.25
    //Range di variazione della velocit� massima (viene sovrascitta la velocit� massima)
    public static Vector2 speedMaxRange = new Vector2 (0.9f, 2.1f);
    //angle
    public const int angleRange = 25;

    public const bool discrete = false;

    
    public const float MAXIMUM_VIEW_DISTANCE = 43f; 
    public const float MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE = 6f; 
    //reward
    public const float step_reward = -0.0001f;
    public const float step_finished_reward = -6f;
    public const float finale_target_reward = 6f;
    public const float new_target_reward = 0.5f;
    public const float already_taken_target_reward = -1f;
    public const float target_taken_incorrectly_reward = -1f;
    public const float not_watching_target_reward = -0.5f;

    //public const float verticalRayOffset = -0.05f; // offset per il raycast verticale
  
    // reward pianificazione
    public const float objective_completed_reward = 2f;
    public const float finale_target_incomplete_objectives_reward = -3f;
    public const float finale_target_all_objectives_completed_reward = 8f;
    public const float wrong_direction_reward = -0.08f;
    public const float incomplete_task_step_reward = -0.06f;
    public const float target_alredy_crossed_reward = -0.03f;
    
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
    //Proxemic
    public static readonly Proxemic[] Proxemics = new Proxemic[]{
       new Proxemic(proxemic_small_distance, 11),
       new Proxemic(proxemic_medium_distance, 7),
       new Proxemic(proxemic_large_distance, 6),
    };
}