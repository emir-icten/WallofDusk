using UnityEngine;

public class KnightAnimRelay : MonoBehaviour
{
    public PlayerKnight playerKnightRoot;

    public void AnimEvent_Hit()
    {
        if (playerKnightRoot != null)
            playerKnightRoot.AnimEvent_Hit();
    }

    public void AnimEvent_EndAttack()
    {
        if (playerKnightRoot != null)
            playerKnightRoot.AnimEvent_EndAttack();
    }
}
