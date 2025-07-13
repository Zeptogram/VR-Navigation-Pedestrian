
public enum TargetType
{
    Final,
    Intermediate
}

public static class TargetTypeExtension
{
    public static string GetLayerName( this TargetType targetType, Group group)
    {
        switch (targetType)
        {
            case TargetType.Final :
                return group.GetFinalTargetLayerName();
            case TargetType.Intermediate:
                return group.GetIntermediateTargetLayerName(); ;
            default:
                throw new System.Exception("TargetType not found : " + targetType.ToString());
        }
    }
}