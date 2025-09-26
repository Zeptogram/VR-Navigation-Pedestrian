using UnityEngine;

public interface IAgentConstants
{
    // Settings
    int viewAngle { get; }
    int rayLength { get; }
    int numberOfRaysPerSide { get; }

    // Speed
    Vector2 minMaxSpeed { get; }
    Vector2 speedMaxRange { get; }

    // Angle
    int angleRange { get; }

    // Discrete/Continuous
    bool discrete { get; }

    // View distances
    float MAXIMUM_VIEW_DISTANCE { get; }
    float MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE { get; }

    // Rewards
    float step_reward { get; }
    float step_finished_reward { get; }
    float finale_target_reward { get; }
    float new_target_reward { get; }
    float already_taken_target_reward { get; }
    float target_taken_incorrectly_reward { get; }
    float not_watching_target_reward { get; }

    // Planning-specific rewards (puoi lasciarli vuoti in Base se non servono)
    float objective_completed_reward { get; }
    float finale_target_incomplete_objectives_reward { get; }
    float finale_target_all_objectives_completed_reward { get; }
    float wrong_direction_reward { get; }
    float incomplete_task_step_reward { get; }
    float target_alredy_crossed_reward { get; }

    // Proxemics distances
    float proxemic_small_distance { get; }
    float proxemic_medium_distance { get; }
    float proxemic_large_distance { get; }

    // Proxemics rays
    float proxemic_small_ray { get; }
    float proxemic_medium_ray { get; }
    float proxemic_large_ray { get; }

    // Proxemics rewards
    float proxemic_large_agent_reward { get; }
    float proxemic_medium_agent_reward { get; }
    float proxemic_small_agent_reward { get; }
    float proxemic_small_wall_reward { get; }

    // Ray offsets
    float rayOffset { get; }
    float verticalRayOffset { get; }

    // Proxemics array
    Proxemic[] Proxemics { get; }
}