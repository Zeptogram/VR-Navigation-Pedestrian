public enum GizmosTag
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
public static class MyGizmosTagExtensions
{
    public static GizmosTag ToMyGizmosTag(this Tag tag, int button = -1)
    {
        switch (tag)
        {
            case Tag.Wall:
                return GizmosTag.Wall;
            
            case Tag.Agent:
                return GizmosTag.Agent;
            
            case Tag.Objective:
                if (button == 2)
                    return GizmosTag.InvalidObjective;
                return GizmosTag.ValidObjective;
            
            case Tag.Target:
                switch(button)
                {
                    case 1:
                        return GizmosTag.ValidDirectionIntermediateTarget;

                    case 2:
                        return GizmosTag.InvalidDirectionIntermediateTarget;
                    
                    case 3:
                        return GizmosTag.ValidDirectionFinalTarget;
                    
                    case 4:
                        return GizmosTag.InvalidDirectionFinalTarget;
                    
                    default:
                        throw new System.NotImplementedException("Button is incorrect");
                }
            default:
                throw new System.NotImplementedException($"GizmosTag: {tag} not implemented");
        }
    }
}