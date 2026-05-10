using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Assets._00._Work.PCM._02._Scripts._TileChange
{
    public class TIleManager : MonoBehaviour
    {
        [SerializeField] private TileChanges[] _tileSet;
        private int _SubjectIndex = 1;
        private void Awake()
        {
            _tileSet = GetComponentsInChildren<TileChanges>();
        }
        public void Update()
        {
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                int id = Random.Range(0, 2);
                Debug.Log("s" + id);
                Debug.Log(id);
                _tileSet[0].TileSetUp(id);
            }
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                int id = Random.Range(0, 2);
                Debug.Log("a"+id);
                _tileSet[1].TileSetUp(id);
            }
        }
        public void TileSet(int id) //어떤거 변경할지 설정하는거임 (ex: Grass, Floor) 
        {
            _SubjectIndex = id;
        }
        public void TileChange(int Tileid)
        {    
            _tileSet[_SubjectIndex].TileSetUp(Tileid);
        }
    }
}