using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConstantsPlanning: IAgentConstants
{
    //settings
    public int viewAngle { get; } = 180;
    public int rayLength { get; } = 43;
    public int numberOfRaysPerSide  { get; }= 11;
    //speed
    public Vector2 minMaxSpeed  { get; } = new Vector2 (0f, 1.7f);
    //float randomValue = RLAgent.RandomGaussian(0.65f, 2.15f); centrata in 1.4 con deviazione di 0.25
    //Range di variazione della velocità massima (viene sovrascitta la velocità massima)
    public Vector2 speedMaxRange  { get; } = new Vector2 (0.9f, 2.1f);
    //angle
    public int angleRange  { get; } = 25;

    public bool discrete  { get; } = false;

    
    public float MAXIMUM_VIEW_DISTANCE  { get; }= 43f; 
    public float MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE  { get; } = 6f; 
    //reward
    public float step_reward  { get; } = -0.0001f;
    public float step_finished_reward  { get; } = -6f;
    public float finale_target_reward  { get; } = 6f;
    public float new_target_reward  { get; } = 0.5f;
    public float already_taken_target_reward  { get; } = -1f;
    public float target_taken_incorrectly_reward  { get; } = -1f;
    public float not_watching_target_reward  { get; } = -0.5f;
    public float verticalRayOffset  { get; } = -0.5f;
  
    // reward pianificazione
    public float objective_completed_reward  { get; } = 2f;
    public float finale_target_incomplete_objectives_reward  { get; } = -4f;
    public float finale_target_all_objectives_completed_reward  { get; } = 8f;
    public float wrong_direction_reward  { get; } = -0.08f;
    public float incomplete_task_step_reward { get; } = -0.06f;
    public float target_alredy_crossed_reward { get; } = -0.03f;


    //Proxemix distances
    public float proxemic_small_distance => 0.6f - rayOffset;
    public float proxemic_medium_distance => 1.0f - rayOffset;
    public float proxemic_large_distance => 1.4f - rayOffset;
    //Proxemix ray
    public float proxemic_small_ray { get; } = 11;
    public float proxemic_medium_ray { get; } = 10;
    public float proxemic_large_ray { get; } = 9;
    //Proxemix rewards
    public float proxemic_large_agent_reward { get; } = -0.001f;
    public float proxemic_medium_agent_reward { get; } = -0.005f;
    public float proxemic_small_agent_reward { get; } = -0.5f;
    public float proxemic_small_wall_reward { get; } = -0.5f;
    public float rayOffset  { get; } = 0.04f;
    //Proxemic
    public Proxemic[] Proxemics => new Proxemic[]{
       new Proxemic(proxemic_small_distance, 11),
       new Proxemic(proxemic_medium_distance, 7),
       new Proxemic(proxemic_large_distance, 6),
    };
}