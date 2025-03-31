public enum Tag
{
    Wall,
    Target,
    Agent
}

public static class MyTagsExtensions
{
    public static Tag ToMyTags(this string tag)
    {
        return
            tag == "Muro" ? Tag.Wall :
            tag == "Target" ? Tag.Target :
            tag == "Agente" ? Tag.Agent :
            throw new System.NotImplementedException($"Tag: {tag} not implemented");
    }
}
