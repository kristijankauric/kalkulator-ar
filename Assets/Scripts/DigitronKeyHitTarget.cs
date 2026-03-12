using UnityEngine;

public class DigitronKeyHitTarget : MonoBehaviour
{
    public DigitronKeyId KeyId { get; private set; }
    public Transform PressTarget { get; private set; }
    public Vector3 PressLocalOffset { get; private set; }

    public void Initialize(DigitronCalculatorController controller, DigitronKeyId keyId, Transform pressTarget = null, Vector3? pressLocalOffset = null)
    {
        KeyId = keyId;
        PressTarget = pressTarget;
        PressLocalOffset = pressLocalOffset ?? Vector3.zero;
    }
}
