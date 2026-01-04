using UnityEngine;
using UnityEngine.XR;

public class BubblePop : MonoBehaviour
{
    [Header("Haptic")]
    public float hapticAmplitude = 0.6f;
    public float hapticDuration = 0.1f;

    private bool popped = false;
    public AudioClip popSound;

    private void OnTriggerEnter(Collider other)
    {
        if (popped) return;

        ControllerTag controller = other.GetComponent<ControllerTag>();
        if (controller == null) return;

        popped = true;

        TriggerHaptic(controller.handType);
        PopBubble();
    }

    void TriggerHaptic(XRNode hand)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);

        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities caps))
        {
            if (caps.supportsImpulse)
            {
                device.SendHapticImpulse(
                    0,
                    hapticAmplitude,
                    hapticDuration
                );
            }
        }
    }

    void PopBubble()
    {
        AudioSource.PlayClipAtPoint(
            popSound,
            transform.position,
            1f
        );

        Destroy(gameObject);
    }
}
