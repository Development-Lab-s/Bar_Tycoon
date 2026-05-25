using System;
using System.IO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.SaveCode
{
    [CreateAssetMenu(fileName = "SavePath", menuName = "SO/Save/SavePath", order = 0)]
    public class SaveFileNameSO : ScriptableObject
    {
        [SerializeField] private string savePath;
        private const string Extension = ".json";
        private const string SaveFolder = "Saves";
        
        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFolder, savePath + Extension);
    }
}