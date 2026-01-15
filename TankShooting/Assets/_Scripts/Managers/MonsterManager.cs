using Client;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    public Dictionary<long, MonsterController> MonsterObjDic = new Dictionary<long, MonsterController>(); // 몬스터 번호, 오브젝트
    public List<GameObject> MonsterPrefabs;     // 몬스터 프리팹

    private List<ObjectInfo> _monsterInfosTemp; // 모든 몬스터 생성 시 정보 받아서 저장
    private ObjectInfo _newMonsterInfo;         // 새로 생성하는 몬스터 정보
    private GameObject _monsterParent;          // 몬스터들 저장할 empty
    private bool _onCreateMonster = false;      // 단일 몬스터 생성 트리거
    private bool _onCreateAllMonster = false;   // 모든 몬스터 생성 트리거
    private bool _onUpdatePos = false;          // 몬스터 정보 업데이트 여부
    private bool _onDieMonster = false;         // 몬스터 죽었는지 여부
    private long _dieMonsterId;                 // 죽은 몬스터 id
    private bool _onRespawnMonster = false;     // 몬스터 리스폰 여부
    private long _respawnMonsterId;             // 리스폰된 몬스터 id

    object _lock = new object();

    enum MonsterKey
    {
        Ray = 1001,
        Bee = 2001,
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _monsterParent = new GameObject("Monsters");
    }

    void Update()
    {
        UpdatePos();
        Respawn();
        RemoveMonster();
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
        MonsterController newController = newMonster.GetComponent<MonsterController>();
        MonsterObjDic.Add(_newMonsterInfo.Id, newController);
        newController.Init(_newMonsterInfo);
    }

    // 위치 업데이트 트리거
    public void OnTriggerUpdatePos(List<ObjectInfo> infos)
    {
        if (_onUpdatePos)
            return;

        _monsterInfosTemp.Clear();
        foreach (var info in infos)
            _monsterInfosTemp.Add(info);
        _onUpdatePos = true;
    }

    // 위치 업데이트
    public void UpdatePos()
    {
        if (!_onUpdatePos)
            return;

        lock (_lock)
        {
            foreach (ObjectInfo info in _monsterInfosTemp)
            {
                if (MonsterObjDic[info.Id] != null)
                    MonsterObjDic[info.Id].SetTargetPos(info.Position);
            }

            _onUpdatePos = false;
        }
    }

    // 몬스터 제거 트리거
    public void OnTriggerRemoveMonster(long id)
    {
        if (MonsterObjDic[id] != null)
        {
            _dieMonsterId = id;
            _onDieMonster = true;
        }
    }

    // 몬스터 제거
    public void RemoveMonster()
    {
        if (!_onDieMonster)
            return;

        MonsterObjDic[_dieMonsterId].Die();
        _onDieMonster = false;
    }

    // 몬스터 리스폰 트리거
    public void OnTriggerRespawn(long id)
    {
        if (MonsterObjDic[id] != null)
        {
            _onRespawnMonster = true;
            _respawnMonsterId = id;
        }
    }

    // 몬스터 리스폰
    public void Respawn()
    {
        if (!_onRespawnMonster)
            return;

        MonsterObjDic[_respawnMonsterId].gameObject.SetActive(true);
        _onRespawnMonster = false;
    }
}