using UnityEngine;

public class AnimatorStateDebugger : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private int layer = 0;

    [SerializeField] private float pollInterval = 0.1f;
    private float timer;

    private int lastStateHash;
    private int lastNextHash;
    private bool lastInTransition;

    void Reset()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!anim) return;

        timer += Time.deltaTime;
        if (timer < pollInterval) return;
        timer = 0f;

        var cur = anim.GetCurrentAnimatorStateInfo(layer);
        bool inTrans = anim.IsInTransition(layer);

        int curHash = cur.shortNameHash;
        int nextHash = 0;

        if (inTrans)
        {
            var next = anim.GetNextAnimatorStateInfo(layer);
            nextHash = next.shortNameHash;
        }

        if (curHash != lastStateHash || nextHash != lastNextHash || inTrans != lastInTransition)
        {
            bool moving = anim.GetBool("Moving");
            bool shoot = anim.GetBool("Shoot");

            string curName = GetStateName(curHash);
            string nextName = inTrans ? GetStateName(nextHash) : "-";

            Debug.Log("[ANIM] Cur=" + curName +
                      " InTransition=" + inTrans +
                      " Next=" + nextName +
                      " Moving=" + moving +
                      " Shoot=" + shoot);

            lastStateHash = curHash;
            lastNextHash = nextHash;
            lastInTransition = inTrans;
        }
    }

    private string GetStateName(int hash)
    {
        if (hash == Animator.StringToHash("Idle")) return "Idle";
        if (hash == Animator.StringToHash("Fly")) return "Fly";
        if (hash == Animator.StringToHash("Shooting")) return "Shooting";
        if (hash == Animator.StringToHash("Fly_Shooting")) return "Fly_Shooting";
        if (hash == Animator.StringToHash("Hit")) return "Hit";
        if (hash == Animator.StringToHash("Die")) return "Die";
        return hash.ToString();
    }
}
