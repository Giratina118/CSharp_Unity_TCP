using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public Dictionary<long, PlayerController> PlayerObjDic = new Dictionary<long, PlayerController>(); // 클라(플레이어) 번호, 오브젝트
    public GameObject PlayerPrefabs;         // 플레이어 오브젝트 프리팹

    private List<ObjectInfo> _infosTemp;     // 모든 캐릭터 생성 시 정보 받아서 저장
    private ObjectInfo _newPlayerInfo;       // 새로 생성하는 캐릭터 정보
    private GameObject _playerParent;        // 플레이어들 저장할 empty
    private bool _onCreatePlayer = false;    // 단일 캐릭터 생성 트리거
    private bool _onCreateAllPlayer = false; // 모든 캐릭터 생성 트리거
    private bool _isMine = false;            // 지금 만드는 캐릭터가 내 캐릭터인지
    private long _exitId = -1;               // 나가는 플레이어 아이디(-1일 경우 나가는 플레이어X)
    object _lock = new object();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _playerParent = new GameObject("Players");
    }

    // 이미 서버에 들어와있던 모든 플레이어 생성(트리거)
    public void OnTriggerCreateCharacterAll(List<ObjectInfo> infos)
    {
        _infosTemp = new List<ObjectInfo>(infos);
        _onCreateAllPlayer = true;
    }

    // 이미 서버에 들어와있던 모든 플레이어 생성
    public void CreateCharacterAll()
    {
        if (!_onCreateAllPlayer)
            return;

        int idCount = _infosTemp.Count;
        for (int i = 0; i < idCount; i++)
        {
            OnTriggerCreateCharacter(_infosTemp[i]);
            CreateCharacter();
        }

        // 다른 플레이어의 오브젝트를 모두 생성한 후에 플레이어의 오브젝트를 모두에게 생성하도록 요청
        CreateRemovePacket crPacket = new CreateRemovePacket()
        { PlayerId = ClientProgram.Instance.ClientId, MessageType = (ushort)MsgType.CreatePlayer };
        ArraySegment<byte> segment = crPacket.Write();
        ClientProgram.Instance.Connector.CurrentSession.Send(segment);

        _onCreateAllPlayer = false;
    }

    // 캐릭터 생성(트리거)
    public void OnTriggerCreateCharacter(ObjectInfo playerInfo)
    {
        lock (_lock)
        {
            if (ClientProgram.Instance.ClientId == playerInfo.Id) _isMine = true;
            else _isMine = false;
            _onCreatePlayer = true;
            _newPlayerInfo = playerInfo;
        }
    }

    // 캐릭터 생성
    public void CreateCharacter()
    {
        lock (_lock)
        {
            if (!_onCreatePlayer)
                return;
            _onCreatePlayer = false;

            // 새 캐릭터 생성
            GameObject newObj = Instantiate(PlayerPrefabs, _newPlayerInfo.Position, Quaternion.identity);
            newObj.transform.parent = _playerParent.transform;
            newObj.GetComponent<PlayerController>().SetMine(_isMine, _newPlayerInfo.Id);

            // 딕셔너리에 새 캐릭터 추가
            PlayerObjDic.Add(_newPlayerInfo.Id, newObj.GetComponent<PlayerController>());
        }
    }

    // 다른 플레이어가 나갔을 때 나간 플레이어 오브젝트 삭제(트리거)
    public void OnTriggerRemoveExitCharacter(long exitId)
    {
        _exitId = exitId;
    }

    // 다른 플레이어가 나갔을 때 나간 플레이어 오브젝트 삭제
    public void RemoveExitCharacter()
    {
        if (_exitId == -1)
            return;

        Destroy(PlayerObjDic[_exitId].gameObject);
        PlayerObjDic.Remove(_exitId);

        _exitId = -1;
    }

    // 본인이 나갔을 때 모든 플레이어 오프젝트 삭제
    public void RemovePlayerAll()
    {
        ChatManager.Instance.WriteChatContent($"\n{ClientProgram.Instance.NickName}님이 퇴장하셨습니다.");

        foreach (var player in PlayerObjDic)
            Destroy(player.Value.gameObject);
        PlayerObjDic.Clear();
    }
}
