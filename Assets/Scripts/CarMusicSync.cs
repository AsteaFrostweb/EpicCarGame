using UnityEngine;

public class CarMusicSync : MonoBehaviour
{
    [SerializeField] private SimpleWheelCarController carController;
    [SerializeField] private AudioSource audioSource;

    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;

    void Start()
    {
        if (carController == null)
        {
            carController = GetComponent<SimpleWheelCarController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (carController == null || audioSource == null)
        {
            return;
        }

        float pitchT = Mathf.Clamp01(carController.Speed01 * 1.75f + carController.EngineLoad01 * 0.25f);
        audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, pitchT);
    }
}
