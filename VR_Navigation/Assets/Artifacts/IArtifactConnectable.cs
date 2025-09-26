// IArtifactConnectable.cs
// Interface for artifacts that can connect to other artifacts
public interface IArtifactConnectable
{
    void ConnectTo(Artifact other);
    void DisconnectFrom(Artifact other);
}