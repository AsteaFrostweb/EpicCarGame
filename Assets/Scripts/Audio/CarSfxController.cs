using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class CarSfxController : MonoBehaviour, ICarSfxController
{
    private const string DefaultEngineLoopPath = "CarSFX/loop_0";
    private const string DefaultAccelerationLoopPath = "CarSFX/loop_5";
    private const string DefaultBrakeLoopPath = "CarSFX/car1";
    private const string DefaultSkidLoopPath = "CarSFX/loop_1";
    private const string DefaultEngineStartPath = "CarSFX/generic_car_startengine";
    private const string DefaultEngineStopPath = "CarSFX/generic_car_startengine_1";

    [Header("Clips")]
    [SerializeField] private AudioClip engineLoopClip;
    [SerializeField] private AudioClip accelerationLoopClip;
    [SerializeField] private AudioClip brakeLoopClip;
    [SerializeField] private AudioClip skidLoopClip;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineStopClip;

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField] private bool playOnAwake = true;

    [Header("Engine")]
    [SerializeField] private float idlePitch = 0.78f;
    [SerializeField] private float maxEnginePitch = 1.85f;
    [SerializeField] private float engineVolume = 0.75f;
    [SerializeField] private float accelerationVolume = 0.65f;
    [SerializeField] private float accelerationPitchBoost = 0.35f;
    [SerializeField] private float reversePitchDrop = 0.15f;

    [Header("Tyres")]
    [SerializeField] private float brakeVolume = 0.55f;
    [SerializeField] private float skidVolume = 0.85f;
    [SerializeField] private float skidSlipThreshold = 0.35f;
    [SerializeField] private float maxSkidSlip = 1.25f;
    [SerializeField] private float driftSkidBoost = 0.4f;

    [Header("Smoothing")]
    [SerializeField] private float volumeBlendSpeed = 9f;
    [SerializeField] private float pitchBlendSpeed = 7f;

    [Header("Optional Wheel Collider Sampling")]
    [SerializeField] private bool sampleWheelColliders;
    [SerializeField] private WheelCollider[] wheelColliders;

    private AudioSource engineLoopSource;
    private AudioSource accelerationLoopSource;
    private AudioSource brakeLoopSource;
    private AudioSource skidLoopSource;
    private AudioSource oneShotSource;
    private Transform sourceRoot;

    private CarSfxState state;

    public bool EngineRunning => state.engineRunning;

    private void Awake()
    {
        LoadDefaultClips();

        engineLoopSource = CreateLoopSource("Engine Loop", engineLoopClip);
        accelerationLoopSource = CreateLoopSource("Acceleration Loop", accelerationLoopClip);
        brakeLoopSource = CreateLoopSource("Brake Loop", brakeLoopClip);
        skidLoopSource = CreateLoopSource("Skid Loop", skidLoopClip);
        oneShotSource = CreateSource("Car SFX One Shots");

        state.engineRunning = playOnAwake;
    }

    [ContextMenu("Load Default Resource Clips")]
    private void LoadDefaultClips()
    {
        engineLoopClip = LoadClipIfMissing(engineLoopClip, DefaultEngineLoopPath);
        accelerationLoopClip = LoadClipIfMissing(accelerationLoopClip, DefaultAccelerationLoopPath);
        brakeLoopClip = LoadClipIfMissing(brakeLoopClip, DefaultBrakeLoopPath);
        skidLoopClip = LoadClipIfMissing(skidLoopClip, DefaultSkidLoopPath);
        engineStartClip = LoadClipIfMissing(engineStartClip, DefaultEngineStartPath);
        engineStopClip = LoadClipIfMissing(engineStopClip, DefaultEngineStopPath);
    }

    private void OnEnable()
    {
        PlayLoop(engineLoopSource);
        PlayLoop(accelerationLoopSource);
        PlayLoop(brakeLoopSource);
        PlayLoop(skidLoopSource);
    }

    private void FixedUpdate()
    {
        if (sampleWheelColliders && wheelColliders != null && wheelColliders.Length > 0)
        {
            SetWheelSlipFromColliders(wheelColliders);
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        float derivedSpeed01 = state.maxSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(state.speed) / state.maxSpeed) : 0f;
        float speed01 = Mathf.Max(state.speed01, derivedSpeed01);
        float forwardSpeed01 = Mathf.Max(state.forwardSpeed01, derivedSpeed01);
        float throttleOrReverse = Mathf.Max(state.throttle, state.reverse);
        float engineLoad = Mathf.Max(state.engineLoad, throttleOrReverse * 0.6f, forwardSpeed01 * 0.35f);
        float engineTargetVolume = state.engineRunning ? engineVolume : 0f;
        float engineTargetPitch = Mathf.Lerp(idlePitch, maxEnginePitch, engineLoad);
        engineTargetPitch -= state.reverse * reversePitchDrop;

        SetSourceTarget(engineLoopSource, engineTargetVolume, engineTargetPitch, deltaTime);

        float accelerationTargetVolume = state.engineRunning ? throttleOrReverse * accelerationVolume : 0f;
        float accelerationTargetPitch = engineTargetPitch + throttleOrReverse * accelerationPitchBoost;
        SetSourceTarget(accelerationLoopSource, accelerationTargetVolume, accelerationTargetPitch, deltaTime);

        float brakeAmount = Mathf.Max(state.brake, state.handbrake);
        float brakeTargetVolume = state.engineRunning ? brakeAmount * brakeVolume : 0f;
        SetSourceTarget(brakeLoopSource, brakeTargetVolume, 1f, deltaTime);

        float slipAmount = Mathf.Max(Mathf.Abs(state.forwardSlip), Mathf.Abs(state.sidewaysSlip));
        float derivedSkid01 = Mathf.InverseLerp(skidSlipThreshold, maxSkidSlip, slipAmount);
        float skid01 = Mathf.Max(state.skid, derivedSkid01, state.wheelSpin * 0.45f);
        skid01 = Mathf.Clamp01(skid01 + state.driftAmount * driftSkidBoost);
        float skidTargetVolume = state.engineRunning ? skid01 * skidVolume : 0f;
        float skidTargetPitch = Mathf.Lerp(0.9f, 1.25f, Mathf.Clamp01(speed01 + skid01 * 0.35f));
        SetSourceTarget(skidLoopSource, skidTargetVolume, skidTargetPitch, deltaTime);
    }

    public void StartEngine()
    {
        if (state.engineRunning)
        {
            return;
        }

        state.engineRunning = true;
        PlayOneShot(engineStartClip);
    }

    public void StopEngine()
    {
        if (!state.engineRunning)
        {
            return;
        }

        state.engineRunning = false;
        PlayOneShot(engineStopClip);
    }

    public void SetEngineRunning(bool isRunning)
    {
        if (isRunning)
        {
            StartEngine();
        }
        else
        {
            StopEngine();
        }
    }

    public void SetThrottle(float throttle)
    {
        state.throttle = Mathf.Clamp01(throttle);
    }

    public void SetReverse(float reverse)
    {
        state.reverse = Mathf.Clamp01(reverse);
    }

    public void SetBrake(float brake)
    {
        state.brake = Mathf.Clamp01(brake);
    }

    public void SetHandbrake(float handbrake)
    {
        state.handbrake = Mathf.Clamp01(handbrake);
    }

    public void SetSpeed(float speed, float maxSpeed)
    {
        state.speed = speed;
        state.maxSpeed = Mathf.Max(0f, maxSpeed);
        state.speed01 = maxSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed) : 0f;
        state.forwardSpeed01 = state.speed01;
    }

    public void SetNormalizedTelemetry(
        float speed01,
        float forwardSpeed01,
        float steering01,
        float engineLoad01,
        float wheelSpin01,
        float skid01,
        float drift01)
    {
        state.speed01 = Mathf.Clamp01(speed01);
        state.forwardSpeed01 = Mathf.Clamp01(forwardSpeed01);
        state.steering = Mathf.Clamp(steering01, -1f, 1f);
        state.engineLoad = Mathf.Clamp01(engineLoad01);
        state.wheelSpin = Mathf.Clamp01(wheelSpin01);
        state.skid = Mathf.Clamp01(skid01);
        state.driftAmount = Mathf.Clamp01(drift01);
    }

    public void SetDrift(float driftAmount)
    {
        state.driftAmount = Mathf.Clamp01(driftAmount);
    }

    public void SetWheelSlip(float forwardSlip, float sidewaysSlip)
    {
        state.forwardSlip = Mathf.Abs(forwardSlip);
        state.sidewaysSlip = Mathf.Abs(sidewaysSlip);
    }

    public void SetWheelSlipFromColliders(params WheelCollider[] sourceWheelColliders)
    {
        if (sourceWheelColliders == null || sourceWheelColliders.Length == 0)
        {
            SetWheelSlip(0f, 0f);
            return;
        }

        int hitCount = 0;
        float totalForwardSlip = 0f;
        float totalSidewaysSlip = 0f;

        foreach (WheelCollider wheelCollider in sourceWheelColliders)
        {
            if (wheelCollider != null && wheelCollider.GetGroundHit(out WheelHit hit))
            {
                totalForwardSlip += Mathf.Abs(hit.forwardSlip);
                totalSidewaysSlip += Mathf.Abs(hit.sidewaysSlip);
                hitCount++;
            }
        }

        if (hitCount == 0)
        {
            SetWheelSlip(0f, 0f);
            return;
        }

        SetWheelSlip(totalForwardSlip / hitCount, totalSidewaysSlip / hitCount);
    }

    public void SetState(CarSfxState newState)
    {
        bool wasRunning = state.engineRunning;

        state = newState;
        state.throttle = Mathf.Clamp01(state.throttle);
        state.reverse = Mathf.Clamp01(state.reverse);
        state.brake = Mathf.Clamp01(state.brake);
        state.handbrake = Mathf.Clamp01(state.handbrake);
        state.maxSpeed = Mathf.Max(0f, state.maxSpeed);
        state.speed01 = Mathf.Clamp01(state.speed01);
        state.forwardSpeed01 = Mathf.Clamp01(state.forwardSpeed01);
        state.steering = Mathf.Clamp(state.steering, -1f, 1f);
        state.engineLoad = Mathf.Clamp01(state.engineLoad);
        state.wheelSpin = Mathf.Clamp01(state.wheelSpin);
        state.skid = Mathf.Clamp01(state.skid);
        state.driftAmount = Mathf.Clamp01(state.driftAmount);
        state.forwardSlip = Mathf.Abs(state.forwardSlip);
        state.sidewaysSlip = Mathf.Abs(state.sidewaysSlip);

        if (!wasRunning && state.engineRunning)
        {
            PlayOneShot(engineStartClip);
        }
        else if (wasRunning && !state.engineRunning)
        {
            PlayOneShot(engineStopClip);
        }
    }

    private AudioSource CreateLoopSource(string sourceName, AudioClip clip)
    {
        AudioSource source = CreateSource(sourceName);
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        return source;
    }

    private AudioSource CreateSource(string sourceName)
    {
        if (sourceRoot == null)
        {
            GameObject rootObject = new GameObject("Car SFX Sources");
            sourceRoot = rootObject.transform;
            sourceRoot.SetParent(transform, false);
        }

        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(sourceRoot, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = outputGroup;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0.2f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 4f;
        source.maxDistance = 45f;
        source.playOnAwake = false;
        source.volume = 0f;
        return source;
    }

    private static void PlayLoop(AudioSource source)
    {
        if (source != null && source.clip != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (oneShotSource != null && clip != null)
        {
            oneShotSource.PlayOneShot(clip);
        }
    }

    private void SetSourceTarget(AudioSource source, float targetVolume, float targetPitch, float deltaTime)
    {
        if (source == null)
        {
            return;
        }

        float volumeT = 1f - Mathf.Exp(-volumeBlendSpeed * deltaTime);
        float pitchT = 1f - Mathf.Exp(-pitchBlendSpeed * deltaTime);

        source.volume = Mathf.Lerp(source.volume, Mathf.Clamp01(targetVolume), volumeT);
        source.pitch = Mathf.Lerp(source.pitch, Mathf.Max(0.01f, targetPitch), pitchT);
    }

    private static AudioClip LoadClipIfMissing(AudioClip currentClip, string resourcePath)
    {
        return currentClip != null ? currentClip : Resources.Load<AudioClip>(resourcePath);
    }
}
