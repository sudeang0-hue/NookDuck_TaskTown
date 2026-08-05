/*
 * 역할:
 * - sound_settings.json 파일 저장/로드를 담당하는 정적 저장소입니다.
 *
 * 주요 기능:
 * - Application.persistentDataPath에 사용자별 사운드 설정을 저장합니다.
 * - 파일이 없거나 손상되면 기본값으로 복구합니다.
 * - Git 프로젝트 폴더가 아닌 로컬 실행 데이터 경로를 사용합니다.
 */
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

    public static float GetLastNonZeroVolume(SoundVolumeChannel channel)
    {
        EnsureLoaded();
        return cachedData.GetLastNonZeroVolume(channel);
    }

    public static BGMPlaybackMode GetBGMPlaybackMode()
    {
        EnsureLoaded();
        return cachedData.GetBGMPlaybackMode();
    }

    public static void SetVolume(SoundVolumeChannel channel, float percent)
    {
        EnsureLoaded();
        cachedData.SetVolume(channel, percent);
        Save();
    }

    public static void SetBGMPlaybackMode(BGMPlaybackMode mode)
    {
        EnsureLoaded();
        cachedData.SetBGMPlaybackMode(mode);
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
