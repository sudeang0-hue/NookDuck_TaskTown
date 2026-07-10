using UnityEngine;

[CreateAssetMenu(fileName = "SoundClipData", menuName = "A.Heesu/Sound/Sound Clip Data")]
public class SoundClipData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string soundId;
    [SerializeField] private SoundCategory category;

    [Header("Clips")]
    [SerializeField] private AudioClip[] clips;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
    [SerializeField] private bool loop;
    [SerializeField] private bool randomizePitch;
    [SerializeField, Range(0.1f, 3f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.05f;

    public string SoundId => soundId;
    public SoundCategory Category => category;
    public float VolumeScale => volumeScale;
    public bool Loop => loop;
    public bool HasClip => clips != null && clips.Length > 0;

    public AudioClip GetClip()
    {
        if (!HasClip)
        {
            return null;
        }

        if (clips.Length == 1)
        {
            return clips[0];
        }

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetPitch()
    {
        if (!randomizePitch)
        {
            return 1f;
        }

        float min = Mathf.Min(minPitch, maxPitch);
        float max = Mathf.Max(minPitch, maxPitch);
        return Random.Range(min, max);
    }

    private void OnValidate()
    {
        volumeScale = Mathf.Clamp01(volumeScale);

        if (maxPitch < minPitch)
        {
            maxPitch = minPitch;
        }
    }
}
