public struct Proxemic
{
    public float Distance;
    public int RaysNumberPerSide;
    public int ActualRaysNumber => (2 * RaysNumberPerSide) + 1;

    public Proxemic(float distance, int raysNumberPerSide)
    {
        Distance = distance;
        RaysNumberPerSide = raysNumberPerSide;
    }
}


