public enum GizmosTagPlanning
{
    Wall,
    Agent,
    ValidObjective,
    InvalidObjective,
    ValidDirectionFinalTarget,
    InvalidDirectionFinalTarget,
    ValidDirectionIntermediateTarget,
    InvalidDirectionIntermediateTarget
}
public static class MyGizmosTagExtensionsPlanning
{
    public static GizmosTagPlanning ToMyGizmosTag(this Tag tag, int button = -1)
    {
        switch (tag)
        {
            case Tag.Wall:
                return GizmosTagPlanning.Wall;
            
            case Tag.Agent:
                return GizmosTagPlanning.Agent;
            
            case Tag.Objective:
                if (button == 2)
                    return GizmosTagPlanning.InvalidObjective;
                return GizmosTagPlanning.ValidObjective;
            
            case Tag.Target:
                switch(button)
                {
                    case 1:
                        return GizmosTagPlanning.ValidDirectionIntermediateTarget;

                    case 2:
                        return GizmosTagPlanning.InvalidDirectionIntermediateTarget;
                    
                    case 3:
                        return GizmosTagPlanning.ValidDirectionFinalTarget;
                    
                    case 4:
                        return GizmosTagPlanning.InvalidDirectionFinalTarget;
                    
                    default:
                        throw new System.NotImplementedException("Button is incorrect");
                }
            default:
                throw new System.NotImplementedException($"GizmosTag: {tag} not implemented");
        }
    }
}