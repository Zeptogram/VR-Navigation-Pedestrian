public enum Group
{
    Generic,
    First,
    Second,
    Third,
    Fourth
}
public static class GroupExtension
{
    public static string GetFinalTargetLayerName(this Group group)
    {
        switch (group)
        {
            case Group.Generic:
                return "TargetFine";
            case Group.First:
                return "TargetFine1";
            case Group.Second:
                return "TargetFine2";
            case Group.Third:
                return "TargetFine3";
            case Group.Fourth:
                return "TargetFine4";
            default:
                throw new System.Exception("Group not found : " + group.ToString());
        }
    }

    public static string GetIntermediateTargetLayerName(this Group group)
    {
        switch (group)
        {
            case Group.Generic:
                return "TargetMid";
            case Group.First:
                return "TargetMid1";
            case Group.Second:
                return "TargetMid2";
            case Group.Third:
                return "TargetMid3";
            case Group.Fourth:
                return "TargetMid4";
            default:
                throw new System.Exception("Group not found : " + group.ToString());
        }
    }

    public static string GetObjectiveLayerName(this Group group)
    {
        switch (group)
        {
            case Group.Generic:
                return "Obiettivo";
            case Group.First:
                return "ObiettivoGruppo1";
            case Group.Second:
                return "ObiettivoGruppo2";
            case Group.Third:
                return "ObiettivoGruppo3";
            case Group.Fourth:
                return "ObiettivoGruppo4";
            default:
                return "Obiettivo";
        }
    }
}