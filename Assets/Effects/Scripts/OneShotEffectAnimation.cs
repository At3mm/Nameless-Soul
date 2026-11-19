using UnityEngine;

/// <summary>Reusable visual-only Animator effect; disable on completion so it can be played again.</summary>
[RequireComponent(typeof(Animator))]
public sealed class OneShotEffectAnimation : MonoBehaviour
{
    public bool deactivateOnComplete=true;
    private Animator animator;
    private void Awake(){animator=GetComponent<Animator>();}
    private void OnEnable()
    {
        if(animator==null)animator=GetComponent<Animator>();
        if(animator.runtimeAnimatorController==null)return;
        animator.Rebind();animator.Play(0,0,0);animator.Update(0);
    }
    private void LateUpdate()
    {
        if(!deactivateOnComplete || animator.runtimeAnimatorController==null)return;
        var state=animator.GetCurrentAnimatorStateInfo(0);
        if(!state.loop && state.normalizedTime>=1)gameObject.SetActive(false);
    }
}
