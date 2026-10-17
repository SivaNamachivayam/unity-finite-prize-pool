using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FinitePrizePool.Persistence
{
    public sealed class JsonSaveStorage
    {
        private readonly string _savePath;
        private readonly string _backupPath;
        private readonly string _tempPath;

        public JsonSaveStorage(string savePath = null)
        {
            string path = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(
                    Application.persistentDataPath,
                    "finite-prize-pool-save.json")
                : savePath;

            _savePath = path;
            _backupPath = _savePath + ".bak";
            _tempPath = _savePath + ".tmp";

            Debug.Log(
                $"[SAVE STORAGE INITIALIZED]\n" +
                $"JSON Location: {_savePath}\n" +
                $"Backup Location: {_backupPath}");
        }

        public string GetSavePath()
        {
            return _savePath;
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string directory = Path.GetDirectoryName(_savePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, true);

            try
            {
                using (FileStream stream = new FileStream(
                           _tempPath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None))
                {
                    byte[] bytes =
                        new UTF8Encoding(false).GetBytes(json);

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                if (File.Exists(_savePath))
                {
                    File.Copy(_savePath, _backupPath, true);
                    File.Copy(_tempPath, _savePath, true);
                    File.Delete(_tempPath);
                }
                else
                {
                    File.Move(_tempPath, _savePath);
                }

                Debug.Log(
                    $"[SAVE SUCCESS]\n" +
                    $"JSON Location: {_savePath}\n" +
                    $"File Exists: {File.Exists(_savePath)}\n" +
                    $"Saved At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SAVE FAILED]\n" +
                    $"JSON Location: {_savePath}\n" +
                    $"Error: {ex}");

                throw;
            }
        }

        public GameSaveData Load()
        {
            GameSaveData data;

            if (TryLoadFile(_savePath, out data))
            {
                Debug.Log(
                    $"[LOAD SUCCESS]\n" +
                    $"JSON Location: {_savePath}");

                return data;
            }

            if (TryLoadFile(_backupPath, out data))
            {
                RestoreBackup();

                Debug.LogWarning(
                    $"[BACKUP LOAD SUCCESS]\n" +
                    $"Backup Location: {_backupPath}");

                return data;
            }

            bool saveExists = File.Exists(_savePath);
            bool backupExists = File.Exists(_backupPath);

            if (saveExists || backupExists)
            {
                Debug.LogError(
                    "[LOAD FAILED] Save file and backup could not be loaded.");

                throw new InvalidDataException(
                    "The save file and backup could not be loaded.");
            }

            Debug.Log(
                "[LOAD INFO] No existing save file. " +
                "A new save will be created when the game saves.");

            return null;
        }

        private bool TryLoadFile(
            string path,
            out GameSaveData data)
        {
            data = null;

            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                GameSaveData loaded =
                    JsonUtility.FromJson<GameSaveData>(json);

                if (loaded == null || loaded.SaveVersion != 1)
                    return false;

                data = loaded;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RestoreBackup()
        {
            try
            {
                if (!File.Exists(_backupPath))
                    return;

                File.Copy(_backupPath, _savePath, true);

                Debug.Log(
                    $"[BACKUP RESTORED]\n" +
                    $"JSON Location: {_savePath}");
            }
            catch (IOException ex)
            {
                Debug.LogWarning(
                    $"[BACKUP RESTORE FAILED] {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogWarning(
                    $"[BACKUP RESTORE FAILED] {ex.Message}");
            }
        }
    }
}