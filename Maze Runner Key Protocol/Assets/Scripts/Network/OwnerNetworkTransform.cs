using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative NetworkTransform. The owner client drives position/rotation
/// and syncs to server + other clients. Required for CharacterController-based movement
/// on non-host clients — default NetworkTransform is server-authoritative and will
/// override CharacterController.Move() on the client.
/// </summary>
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
