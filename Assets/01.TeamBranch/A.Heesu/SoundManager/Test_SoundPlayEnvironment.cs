using UnityEngine;

public class Test_SoundPlayEnvironment : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float loopTime = 5.0f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private EnvironmentSoundHandle handle;

    private Coroutine loopCoroutine;

    private void Awake()
    {
        if (handle == null)
        {
            TryGetComponent(out handle);
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartLoop();
        }
    }

    private void OnDisable()
    {
        StopLoop();
    }

    public void PlayOnce()
    {
        if (handle == null)
        {
            Debug.LogWarning("EnvironmentSoundHandle is not assigned.");
            return;
        }

        handle.Play();
    }

    public void StartLoop()
    {
        if (!loop || loopCoroutine != null)
        {
            return;
        }

        loopCoroutine = StartCoroutine(LoopRoutine());
    }

    public void StopLoop()
    {
        if (loopCoroutine == null)
        {
            return;
        }

        StopCoroutine(loopCoroutine);
        loopCoroutine = null;
    }

    public void SetLoop(bool isLoop)
    {
        loop = isLoop;

        if (loop)
        {
            StartLoop();
        }
        else
        {
            StopLoop();
        }
    }

    private System.Collections.IEnumerator LoopRoutine()
    {
        while (loop)
        {
            PlayOnce();
            yield return new WaitForSeconds(loopTime);
        }

        loopCoroutine = null;
    }

    private void OnValidate()
    {
        loopTime = Mathf.Max(0.1f, loopTime);
    }
}
