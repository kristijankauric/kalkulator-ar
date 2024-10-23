using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JankecController : MonoBehaviour
{
    public AudioSource JAudioSource { get; private set; }
    private Animator m_Animator;

    public void StartAnimation(string stateNameToSet)
    {
        bool isAlreadyPlaying = m_Animator.GetCurrentAnimatorStateInfo(0).IsName(stateNameToSet);
        if (isAlreadyPlaying)
        {
            return;
        }

        m_Animator.SetTrigger(stateNameToSet);
    }

    private void Awake()
    {
        JAudioSource = GetComponent<AudioSource>();
        m_Animator = GetComponent<Animator>();
    }
}
