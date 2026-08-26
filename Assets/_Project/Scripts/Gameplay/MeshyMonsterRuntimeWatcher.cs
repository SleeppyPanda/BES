using UnityEngine;

namespace BES.Gameplay
{
    public class MeshyMonsterRuntimeWatcher : MonoBehaviour
    {
        private Animator anim;
        private Transform leftLeg;
        private Transform rightLeg;
        private float logTimer;

        void Start()
        {
            anim = GetComponentInChildren<Animator>();
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                leftLeg = visual.Find("Armature/Hips/LeftUpLeg");
                rightLeg = visual.Find("Armature/Hips/RightUpLeg");
            }
            
            Debug.Log($"[RUNTIME WATCHER START] {name} | Animator: {(anim != null ? anim.name : "NULL")} | Controller: {(anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL")} | LeftLeg: {(leftLeg != null ? "FOUND" : "MISSING")}");
        }

        void Update()
        {
            logTimer += Time.deltaTime;
            if (logTimer >= 1.0f)
            {
                logTimer = 0f;
                if (anim != null)
                {
                    var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                    float speedParam = 0f;
                    try { speedParam = anim.GetFloat("Speed"); } catch {}
                    
                    Vector3 lRot = leftLeg != null ? leftLeg.localEulerAngles : Vector3.zero;
                    Vector3 rRot = rightLeg != null ? rightLeg.localEulerAngles : Vector3.zero;
                    
                    Debug.Log($"[RUNTIME STATE] {name} | StateHash: {stateInfo.shortNameHash} | NormalizedTime: {stateInfo.normalizedTime:F2} | SpeedParam: {speedParam:F2} | LeftLegRot: {lRot} | RightLegRot: {rRot}");
                }
            }
        }
    }
}
