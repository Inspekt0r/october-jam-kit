using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamKit
{
    public sealed class WorldProgress
    {
        public const string StorageKey = "JamKit.WorldProgress.v1";
        [Serializable]
        private sealed class SaveData { public List<string> visited = new List<string>(); }
        private readonly HashSet<string> visited = new HashSet<string>();
        public bool Visited(string id) => visited.Contains(id);

        public void Load()
        {
            visited.Clear();
            try
            {
                var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(StorageKey, "{}"));
                if (data?.visited != null)
                    foreach (var id in data.visited) if (!string.IsNullOrWhiteSpace(id)) visited.Add(id);
            }
            catch (ArgumentException) { /* Invalid save: start with an empty map. */ }
        }

        public void Visit(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !visited.Add(id)) return;
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(new SaveData { visited = new List<string>(visited) }));
            PlayerPrefs.Save();
        }
    }
}
