using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JankecUvodBehaviour : JankecAnimationBehaviour
{
    [SerializeField] private float m_ShowUITime = 10;
    private bool m_CheckTime = true;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_CheckTime)
        {
            if (stateInfo.length * stateInfo.normalizedTime > m_ShowUITime)
            {
                m_CheckTime = false;
#if UNITY_WEBGL && !UNITY_EDITOR
                LibraryManager.JSShowUI();
#endif
            } 
        }
    }
}
