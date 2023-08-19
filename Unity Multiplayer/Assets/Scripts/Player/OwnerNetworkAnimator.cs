using Unity.Netcode.Components;

public class OwnerNetworkAnimator : NetworkAnimator
{
    //Modified override, Host can now see the clients Animation.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}