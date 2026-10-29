using System.IO;
using UnityEngine;
using SlimeHub.Core;

namespace SlimeHub.Data
{
    public interface IDataService
    {
        PlayerData Data { get; }
        void LoadSync();
        void SaveSoon();
        void Flush();
        void TickSave();
        void AddBiomass(int amount);
        bool TrySpend(int cost);
        bool TrySetHighScore(string boardId, int score);
        int GetHighScore(string boardId);
    }

    /// <summary>
    /// JSON em persistentDataPath. O arquivo cabe em poucos KB, então a leitura
    /// fica no thread principal depois do primeiro frame do boot — JsonUtility
    /// não é seguro fora da main thread.
    /// </summary>
    public sealed class SaveSystem : IDataService
    {
        public const string FileName = "slimehub_save.json";
        readonly string path;
        float dirtyTimer = -1f;

        public PlayerData Data { get; private set; }

        public SaveSystem()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            Data = CreateDefault();
        }

        public static PlayerData CreateDefault()
        {
            var data = new PlayerData();
            data.EnsureCollections();
            data.Unlock("color_menta");
            data.Unlock("tex_puro");
            data.Unlock("core_none");
            return data;
        }

        public void LoadSync()
        {
            if (!File.Exists(path))
            {
                Data = CreateDefault();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var loaded = JsonUtility.FromJson<PlayerData>(json);
                if (loaded == null)
                {
                    Data = CreateDefault();
                    return;
                }
                loaded.EnsureCollections();
                Data = loaded;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Save do Slime Hub ilegível, criando um novo. " + exception.Message);
                Data = CreateDefault();
            }
        }

        public async Awaitable LoadAsync()
        {
            await Awaitable.NextFrameAsync();
            LoadSync();
        }

        public void SaveSoon()
        {
            dirtyTimer = 0.35f;
        }

        public void TickSave()
        {
            if (dirtyTimer < 0f)
                return;
            dirtyTimer -= Time.unscaledDeltaTime;
            if (dirtyTimer <= 0f)
                Flush();
        }

        public void Flush()
        {
            dirtyTimer = -1f;
            try
            {
                Data.EnsureCollections();
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(path, json);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Falha ao gravar o save. " + exception.Message);
            }
        }

        public void AddBiomass(int amount)
        {
            Data.bioMassCurrency = Mathf.Max(0, Data.bioMassCurrency + amount);
            SaveSoon();
            GameEvents.RaiseBiomass(Data.bioMassCurrency);
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0 || Data.bioMassCurrency < cost)
                return false;
            Data.bioMassCurrency -= cost;
            SaveSoon();
            GameEvents.RaiseBiomass(Data.bioMassCurrency);
            return true;
        }

        public bool TrySetHighScore(string boardId, int score)
        {
            if (score <= Data.GetScore(boardId))
                return false;
            Data.SetScore(boardId, score);
            SaveSoon();
            return true;
        }

        public int GetHighScore(string boardId) => Data.GetScore(boardId);
    }
}
