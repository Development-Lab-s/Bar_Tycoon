using System.IO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.SaveCode
{
    public class JsonSaveService
    {
        private readonly SaveFileNameSO _saveFileNameSo;

        public JsonSaveService(SaveFileNameSO saveFileNameSo)
        {
            _saveFileNameSo = saveFileNameSo;
        }

        public void Save<T>(T data)
        {
            string saveData = JsonUtility.ToJson(data, true);
            File.WriteAllText(_saveFileNameSo.SavePath, saveData);
            Debug.Log($"The file {_saveFileNameSo.SavePath} was saved.");
        }

        public T Load<T>()
        {
            if (!File.Exists(_saveFileNameSo.SavePath))
                return default;
            
            string saveData = File.ReadAllText(_saveFileNameSo.SavePath);
            return JsonUtility.FromJson<T>(saveData);
        }
    }
}