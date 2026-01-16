using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissileManager : MonoBehaviour
{
    public static MissileManager Instance { get; private set; }

    public Dictionary<int, MissileController> MissileDicObj = new Dictionary<int, MissileController>(); // 총알 오브젝트
    public GameObject MissilePrefab;       // 미사일 프리팹
    public Button FireButton;              // 발사 버튼

    private GameObject _missilerParent;    // 미사일들 저장할 empty
    private long _shooterId;               // 발사한 플레이어 아이디
    private bool _onCreateMissile = false; // 미사일 생성 트리거
    private float _fireTimer = 0.0f;       // 미사일 발사 타이머
    private float _fireInterval = 1.0f;    // 미사일 발사 주기

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _missilerParent = new GameObject("Missiles");
    }

    private void Update()
    {
        CreateMissile();    // 미사일 생성
        MissileFireTimer(); // 미사일 타이머(발사 딜레이)
    }

    // 미사일 발사 타이머
    public void MissileFireTimer()
    {
        if (FireButton.interactable)
            return;

        _fireTimer += Time.deltaTime;
        _fireTimer += Time.deltaTime;
        if (_fireInterval < _fireTimer)
        {
            _fireTimer = 0.0f;
            FireButton.interactable = true;
        }
    }

    // 미사일 발사 버튼
    public void OnClickFireButton()
    {
        FireButton.interactable = false;
        SendMissile();
    }

    // 미사일 생성 전송
    public void SendMissile()
    {
        CreateRemovePacket packet = new CreateRemovePacket() { Id = ClientProgram.Instance.ClientId, MessageType = (ushort)MsgType.CreateMissile };
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

        GameObject newMissile = Instantiate(MissilePrefab, PlayerManager.Instance.PlayerObjDic[_shooterId].Muzzle.position, PlayerManager.Instance.PlayerObjDic[_shooterId].transform.rotation);
        newMissile.transform.parent = _missilerParent.transform;

        //PlayerManager.Instance.PlayerObjDic[_shooterId].OnSmoke();
    }

    // 미사일 접촉
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject.GetComponent<PlayerController>().IsMine)
            return;

        Destroy(gameObject);
    }
}
