using Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    public Dictionary<long, GameObject> MonsterObjDic = new Dictionary<long, GameObject>(); // 몬스터 번호, 오브젝트
    public List<GameObject> MonsterPrefabs;     // 몬스터 프리팹

    private List<ObjectInfo> _monsterInfosTemp; // 모든 몬스터 생성 시 정보 받아서 저장
    private ObjectInfo _newMonsterInfo;         // 새로 생성하는 몬스터 정보
    private GameObject _monsterParent;          // 몬스터들 저장할 empty
    private bool _onCreateMonster = false;      // 단일 몬스터 생성 트리거
    private bool _onCreateAllMonster = false;   // 모든 몬스터 생성 트리거

    enum MonsterKey
    {
        Ray = 1001,
        Bee = 2001,
    }

    void Awake()
    {
        /*
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        */
        Instance = this;
    }

    void Start()
    {
        _monsterParent = new GameObject("Monsters");
    }

    // 이미 생성되어 있던 모든 몬스터 생성(트리거)
    public void OnTriggerCreateMonsterAll(List<ObjectInfo> infos)
    {
        Debug.Log($"OnTriggerCreateMonsterAll");
        _monsterInfosTemp = infos;
        _onCreateAllMonster = true;
    }

    // 이미 생성되어 있던 모든 몬스터 생성
    public void CreateMonsterAll()
    {
        if (!_onCreateAllMonster)
            return;

        Debug.Log($"CreateMonsterAll, count: {_monsterInfosTemp.Count}");
        _onCreateAllMonster = false;
        int idCount = _monsterInfosTemp.Count;

        for (int i = 0; i < idCount; i++)
        {
            ObjectInfo test = _monsterInfosTemp[i];
            Debug.Log($"objType: {test.ObjType}, id: {test.Id}, pos: {test.Position}, rot: {test.Rotation}");
        }

        for (int i = 0; i < idCount; i++)
        {
            OnTriggerCreateMonster(_monsterInfosTemp[i]);
            CreateMonster();
        }
    }

    // 몬스터 생성(트리거)
    public void OnTriggerCreateMonster(ObjectInfo info)
    {
        Debug.Log($"OnTriggerCreateMonster");
        _onCreateMonster = true;
        _newMonsterInfo = info;
    }

    // 몬스터 생성
    public void CreateMonster()
    {
        if (!_onCreateMonster)
            return;
        Debug.Log($"CreateMonster {_newMonsterInfo.ObjType}");
        _onCreateMonster = false;

        // 새 몬스터 생성
        int monsterType = 0;
        if (_newMonsterInfo.ObjType == (int)MonsterKey.Bee)
            monsterType = 1;

        GameObject newMonster = Instantiate(MonsterPrefabs[monsterType], _newMonsterInfo.Position, Quaternion.identity);
        newMonster.transform.parent = _monsterParent.transform;

        // 딕셔너리에 새 캐릭터 추가
        MonsterObjDic.Add(_newMonsterInfo.Id, newMonster);
    }
}
