using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class MissileManager : MonoBehaviour
{
    public static MissileManager Instance { get; private set; }

    public Dictionary<int, MissileController> MissileDicObj = new Dictionary<int, MissileController>(); // 총알 오브젝트
    public GameObject MissilePrefab;       // 미사일 프리팹

    private GameObject _missilerParent;    // 미사일들 저장할 empty
    private long _shooterId;               // 발사한 플레이어 아이디
    private bool _onCreateMissile = false; // 미사일 생성 트리거

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _missilerParent = new GameObject("Missiles");
    }

    // 미사일 발사 버튼
    public void OnClickFireButton()
    {
        MissileManager.Instance.SendMissile();
    }

    // 미사일 생성 전송
    public void SendMissile()
    {
        CreateRemovePacket packet = new CreateRemovePacket() { PlayerId = ClientProgram.Instance.ClientId, MessageType = (ushort)MsgType.CreateMissile };
        ArraySegment<byte> segment = packet.Write();
        ClientProgram.Instance.Connector.CurrentSession.Send(segment);
    }

    // 미사일 생성 트리거
    public void OnTriggerCreateMissile(long shooterId)
    {
        _onCreateMissile = true;
        _shooterId = shooterId;
    }

    // 미사일 생성
    public void CreateMissile(Dictionary<long, PlayerController> playerObjDic)
    {
        if (!_onCreateMissile)
            return;
        _onCreateMissile = false;

        Debug.Log($"shooter: {_shooterId}, pos: {playerObjDic[_shooterId].transform.position}, rot: {playerObjDic[_shooterId].transform.rotation}");

        GameObject newMissile = Instantiate(MissilePrefab, playerObjDic[_shooterId].Muzzle.position, playerObjDic[_shooterId].transform.rotation);
        newMissile.transform.parent = _missilerParent.transform;
    }
}
