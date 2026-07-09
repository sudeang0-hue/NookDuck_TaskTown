using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SoundSettingsStore
{
    private const string FileName = "sound_settings.json";

    private static SoundSettingsData cachedData;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SoundSettingsData Load()
    {
        if (cachedData != null)
        {
            return cachedData.Clone();
        }

        cachedData = LoadFromFile();
        return cachedData.Clone();
    }

    public static float GetVolume(SoundVolumeChannel channel)
    {
        EnsureLoaded();
        return cachedData.GetVolume(channel);
    }

    public static void SetVolume(SoundVolumeChannel channel, float percent)
    {
        EnsureLoaded();
        cachedData.SetVolume(channel, percent);
        Save();
    }

    public static void Save()
    {
        EnsureLoaded();

        try
        {
            cachedData.Clamp();
            string json = JsonUtility.ToJson(cachedData, true);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Sound settings save failed. Path: {FilePath}\n{exception.Message}");
        }
    }

    public static void ResetToDefault()
    {
        cachedData = CreateDefault();
        Save();
    }

    private static void EnsureLoaded()
    {
        if (cachedData != null)
        {
            return;
        }

        cachedData = LoadFromFile();
    }

    private static SoundSettingsData LoadFromFile()
    {
        if (!File.Exists(FilePath))
        {
            SoundSettingsData defaultData = CreateDefault();
            cachedData = defaultData;
            Save();
            return defaultData;
        }

        try
        {
            string json = File.ReadAllText(FilePath, Encoding.UTF8);
            SoundSettingsData loadedData = JsonUtility.FromJson<SoundSettingsData>(json);

            if (loadedData == null)
            {
                Debug.LogWarning("Sound settings JSON is empty. Default settings will be restored.");
                return RestoreDefaultFile();
            }

            loadedData.Clamp();
            return loadedData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Sound settings load failed. Default settings will be restored. Path: {FilePath}\n{exception.Message}");
            return RestoreDefaultFile();
        }
    }

    private static SoundSettingsData RestoreDefaultFile()
    {
        SoundSettingsData defaultData = CreateDefault();
        cachedData = defaultData;
        Save();
        return defaultData;
    }

    private static SoundSettingsData CreateDefault()
    {
        return new SoundSettingsData();
    }
}
