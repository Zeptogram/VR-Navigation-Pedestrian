public enum GizmosTag
{
    Wall,
    NewTarget,
    TakenTarget,
    Agent
}

public static class MyGizmosTagExtensions
{
    public static GizmosTag ToMyGizmosTag(this Tag tag, bool taken = false)
    {
        return
            tag == Tag.Wall ? GizmosTag.Wall :
            (tag == Tag.Target && !taken) ? GizmosTag.NewTarget :
            (tag == Tag.Target && taken) ? GizmosTag.TakenTarget :
            tag == Tag.Agent ? GizmosTag.Agent :
            throw new System.NotImplementedException($"GizmosTag: {tag} not implemented");
    }
}

