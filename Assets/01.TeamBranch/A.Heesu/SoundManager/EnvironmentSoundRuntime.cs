using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSoundRuntime : MonoBehaviour
{
    public static EnvironmentSoundRuntime Instance { get; private set; }

    [SerializeField] private SoundManager soundManager;
    [SerializeField] private bool playRandomOnStart;
    [SerializeField, Min(0.1f)] private float minRandomInterval = 8f;
    [SerializeField, Min(0.1f)] private float maxRandomInterval = 20f;
    [SerializeField] private string[] defaultRandomSoundIds;

    private readonly List<string> registeredRandomSoundIds = new List<string>();
    private Coroutine randomLoopCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (playRandomOnStart)
        {
            StartRandomLoop();
        }
    }

    private void OnDisable()
    {
        StopRandomLoop();
    }

    public void StartRandomLoop()
    {
        if (randomLoopCoroutine != null)
        {
            return;
        }

        randomLoopCoroutine = StartCoroutine(RandomLoop());
    }

    public void StopRandomLoop()
    {
        if (randomLoopCoroutine == null)
        {
            return;
        }

        StopCoroutine(randomLoopCoroutine);
        randomLoopCoroutine = null;
    }

    public void RegisterRandomSound(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId) || registeredRandomSoundIds.Contains(soundId))
        {
            return;
        }

        registeredRandomSoundIds.Add(soundId);
    }

    public void UnregisterRandomSound(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            return;
        }

        registeredRandomSoundIds.Remove(soundId);
    }

    public bool PlayEnvironment(string soundId)
    {
        SoundManager manager = GetSoundManager();

        if (manager == null)
        {
            Debug.LogWarning("SoundManager is not available. Environment sound cannot be played.");
            return false;
        }

        return manager.PlayEnvironment(soundId);
    }

    public bool PlayRandomEnvironment()
    {
        string soundId = GetRandomRegisteredSoundId();

        if (!string.IsNullOrWhiteSpace(soundId))
        {
            return PlayEnvironment(soundId);
        }

        SoundManager manager = GetSoundManager();

        if (manager == null)
        {
            Debug.LogWarning("SoundManager is not available. Random Environment sound cannot be played.");
            return false;
        }

        return manager.PlayRandomEnvironment();
    }

    private IEnumerator RandomLoop()
    {
        while (enabled)
        {
            float min = Mathf.Min(minRandomInterval, maxRandomInterval);
            float max = Mathf.Max(minRandomInterval, maxRandomInterval);
            yield return new WaitForSeconds(Random.Range(min, max));
            PlayRandomEnvironment();
        }
    }

    private string GetRandomRegisteredSoundId()
    {
        List<string> candidates = new List<string>();

        for (int i = 0; i < registeredRandomSoundIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(registeredRandomSoundIds[i]))
            {
                candidates.Add(registeredRandomSoundIds[i]);
            }
        }

        if (defaultRandomSoundIds != null)
        {
            for (int i = 0; i < defaultRandomSoundIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(defaultRandomSoundIds[i]))
                {
                    candidates.Add(defaultRandomSoundIds[i]);
                }
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private SoundManager GetSoundManager()
    {
        return soundManager != null ? soundManager : SoundManager.Instance;
    }
}
