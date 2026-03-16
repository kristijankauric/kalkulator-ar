using UnityEngine;

public class DigitronKeyHitTarget : MonoBehaviour
{
    private DigitronCalculatorController m_Controller;
    public DigitronKeyId KeyId { get; private set; }
    public Transform PressTarget { get; private set; }
    public Vector3 PressLocalOffset { get; private set; }
    public Vector3 RestLocalPosition { get; private set; }

    public void Initialize(DigitronCalculatorController controller, DigitronKeyId keyId, Transform pressTarget = null, Vector3? pressLocalOffset = null)
    {
        m_Controller = controller;
        KeyId = keyId;
        PressTarget = pressTarget;
        PressLocalOffset = pressLocalOffset ?? Vector3.zero;
        RestLocalPosition = PressTarget != null ? PressTarget.localPosition : Vector3.zero;
    }


}
