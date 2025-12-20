using UnityEngine;

public class MageAnimEventRelay : MonoBehaviour
{
    public PlayerMage mage; // root'taki PlayerMage

    // Cast anim clip'ine ekleyeceğin event fonksiyonu BU olacak
    public void AnimEvent_Fire()
    {
        if (mage != null)
            mage.AnimEvent_Fire();
        else
            Debug.LogWarning("[MageAnimEventRelay] mage referansı boş!");
    }
}
