using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
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
        /*
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        */
        Instance = this;
    }

    private void Start()
    {
        _missilerParent = new GameObject("Missiles");
    }

    private void Update()
    {
        CreateMissile();
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
    public void CreateMissile()
    {
        if (!_onCreateMissile)
            return;
        _onCreateMissile = false;

        Debug.Log($"shooter: {_shooterId}, pos: {PlayerManager.Instance.PlayerObjDic[_shooterId].transform.position}, rot: {PlayerManager.Instance.PlayerObjDic[_shooterId].transform.rotation}");

        GameObject newMissile = Instantiate(MissilePrefab, PlayerManager.Instance.PlayerObjDic[_shooterId].Muzzle.position, PlayerManager.Instance.PlayerObjDic[_shooterId].transform.rotation);
        newMissile.transform.parent = _missilerParent.transform;
    }
}
