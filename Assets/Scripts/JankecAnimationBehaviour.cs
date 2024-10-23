using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class JankecAnimationBehaviour : StateMachineBehaviour
{
    [SerializeField] protected AudioClip m_AudioClip;
    protected JankecController m_JankecController;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_JankecController = animator.GetComponent<JankecController>();

        if (m_AudioClip)
        {
            m_JankecController.JAudioSource.Stop();
            m_JankecController.JAudioSource.PlayOneShot(m_AudioClip);
        }
    }
}
