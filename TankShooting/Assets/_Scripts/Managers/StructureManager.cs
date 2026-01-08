using Client;
using SerializableDictionary.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance { get; private set; }

    [SerializeField]
    private SerializableDictionary<int, GameObject> StructurePrefabs; // 건물 프리팹
    private GameObject _structureParent;
    private List<ObjectInfo> _structureInfosTemp; // 모든 건물 생성 시 정보 받아서 저장
    private bool _onCreateAllStructure = false;   // 모든 건물 생성 트리거

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _structureParent = new GameObject("Structures");
    }

    
    // 이미 생성되어 있던 모든 몬스터 생성(트리거)
    public void OnTriggerCreateStructureAll(List<ObjectInfo> infos)
    {
        Debug.Log($"OnTriggerCreateStructureAll");
        _structureInfosTemp = infos;
        _onCreateAllStructure = true;
    }

    // 이미 생성되어 있던 모든 몬스터 생성
    public void CreateStructureAll()
    {
        if (!_onCreateAllStructure)
            return;

        Debug.Log($"CreateStructureAll, count: {_structureInfosTemp.Count}");
        _onCreateAllStructure = false;
        int idCount = _structureInfosTemp.Count;

        for (int i = 0; i < idCount; i++)
        {
            ObjectInfo test = _structureInfosTemp[i];
            Debug.Log($"objType: {test.ObjType}, id: {test.Id}, pos: {test.Position}, rot: {test.Rotation}");
        }

        for (int i = 0; i < idCount; i++)
        {
            CreateStructure(_structureInfosTemp[i]);
        }
    }
    
    // 건물 생성
    public void CreateStructure(ObjectInfo structInfo)
    {
        Debug.Log($"CreateStructure {structInfo.ObjType}");

        // 새 건물 생성
        GameObject newStructure = Instantiate(StructurePrefabs.Get((int)structInfo.Id), structInfo.Position, Quaternion.identity);
        newStructure.transform.parent = _structureParent.transform;
    }
}
