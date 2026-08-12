using System;
using System.IO;
using UnityEngine;

namespace TaskTown.KDH
{
    public enum GameSaveLoadStatus
    {
        Success,
        NotFound,
        Failed
    }

    /// <summary>
    /// 게임 저장 파일의 JSON 입출력만 담당합니다.
    /// 로드된 데이터는 반환 전에 정규화하여 이전 버전의 누락 필드를 보정합니다.
    /// </summary>
    public static class GameSaveStorage
    {
        public const string SaveFileName = "gamesave.json";

        public static string DefaultSavePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool Exists()
        {
            return File.Exists(DefaultSavePath);
        }

        public static GameSaveLoadStatus Load(
            out GameSaveData data,
            out string errorMessage)
        {
            return LoadFromPath(DefaultSavePath, out data, out errorMessage);
        }

        public static GameSaveLoadStatus LoadFromPath(
            string path,
            out GameSaveData data,
            out string errorMessage)
        {
            data = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "저장 파일 경로가 비어 있습니다.";
                return GameSaveLoadStatus.Failed;
            }

            if (!File.Exists(path))
                return GameSaveLoadStatus.NotFound;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    errorMessage = "저장 파일 내용이 비어 있습니다.";
                    return GameSaveLoadStatus.Failed;
                }

                data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    errorMessage = "저장 데이터를 역직렬화하지 못했습니다.";
                    return GameSaveLoadStatus.Failed;
                }

                data.Normalize();
                return GameSaveLoadStatus.Success;
            }
            catch (Exception exception)
            {
                data = null;
                errorMessage = exception.Message;
                return GameSaveLoadStatus.Failed;
            }
        }

        public static bool Save(GameSaveData data, out string errorMessage)
        {
            return SaveToPath(DefaultSavePath, data, out errorMessage);
        }

        public static bool SaveToPath(
            string path,
            GameSaveData data,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "저장 파일 경로가 비어 있습니다.";
                return false;
            }

            if (data == null)
            {
                errorMessage = "저장할 게임 데이터가 없습니다.";
                return false;
            }

            try
            {
                data.Normalize();

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, JsonUtility.ToJson(data));
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        public static bool Delete(out string errorMessage)
        {
            return DeleteFromPath(DefaultSavePath, out errorMessage);
        }

        public static bool DeleteFromPath(string path, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "삭제할 저장 파일 경로가 비어 있습니다.";
                return false;
            }

            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }
    }
}
