using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Client
{
    class ClientProgram : MonoBehaviour
    {
        public static ClientProgram Instance { get; private set; }

        public Dictionary<long, PlayerController> playerObjDic = new Dictionary<long, PlayerController>(); // 클라(플레이어) 번호, 오브젝트
        public List<ObjectInfo> infosTemp;    // 모든 캐릭터 생성 시 정보 받아서 저장
        public TMP_InputField NameInputField; // 닉네임 입력
        public TMP_InputField ChatInputField; // 채팅 입력
        public TMP_Text ChatContent;     // 채팅 로그
        public Scrollbar ChatScrollbar;  // 채팅 로그 스크롤바
        public GameObject PlayerPrefabs; // 플레이어 오브젝트 프리팹

        public long ClientId;   // 본인 id
        public string NickName; // 이름

        private ObjectInfo _newPlayerInfo; // 새로 생성하는 캐릭터 정보
        private string chatTemp;   // 새로 들어온 채팅

        private bool _isConnect   = false; // 서버 연결 여부
        private bool _onCreate    = false; // 단일 캐릭터 생성 트리거
        private bool _onCreateAll = false; // 모든 캐릭터 생성 트리거
        private bool _isMine      = false; // 지금 만드는 캐릭터가 내 캐릭터인지
        private bool _onChat      = false; // 채팅 갱신 정보가 들어왔는지
        private long _exitId = -1; // 나가는 플레이어 아이디(-1일 경우 나가는 플레이어X)

        private GameObject _playerParent; // 플레이어들 저장할 empty

        string host;
        IPHostEntry ipHost;
        IPAddress ipAddr;
        IPEndPoint endPoint;
        public Connector connector;
        object _lock = new object();

        void Awake()
        {
            Screen.SetResolution(1280, 720, false); // false: 창모드
            Instance = this;
        }
        void Start()
        {
            host = Dns.GetHostName();
            ipHost = Dns.GetHostEntry(host);
            ipAddr = ipHost.AddressList[0];
            endPoint = new IPEndPoint(ipAddr, 7777);
            _playerParent = new GameObject("Players");
        }

        private void Update()
        {
            try
            {
                CreateCharacterAll();  // 다른 플레이어 캐릭터 생성
                CreateCharacter();     // 본인 캐릭터 생성
                UpdateChatting();      // 채팅 업데이트

                if (Input.GetKeyDown(KeyCode.Return))
                    SendChatting();    // 문자 보내기

                RemoveExitCharacter(); // 나간 플레이어 제거
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        // 서버 연결 버튼 클릭
        public void OnClickConnectButton()
        {
            if (_isConnect)
                return;

            Debug.Log(host);

            // 닉네임 저장
            if (NameInputField.text == "")
            {
                Random rand = new Random();
                NickName = "Guest" + rand.Next(1000);
            }
            else
                NickName = NameInputField.text;

            // 커넥터 생성
            connector = new Connector();
            connector.Connect(endPoint, () => { return new ServerSession(); });
            _isConnect = true;
        }

        // 서버 연결 해제 버튼 클릭
        public void OnClickDisconnectButton()
        {
            if (connector != null && connector.CurrentSession != null)
            {
                // 서버에 연결 해제 요청
                connector.CurrentSession.Disconnect();
                _isConnect = false;
            }
        }


        // 플레이어 생성/삭제
        // 이미 서버에 들어와있던 모든 플레이어 생성(트리거)
        public void OnTriggerCreateCharacterAll(List<ObjectInfo> infos)
        {
            infosTemp = new List<ObjectInfo>(infos);
            _onCreateAll = true;
        }

        // 이미 서버에 들어와있던 모든 플레이어 생성
        public void CreateCharacterAll()
        {
            if (!_onCreateAll)
                return;

            int idCount = infosTemp.Count;
            for (int i = 0; i < idCount; i++)
            {
                OnTriggerCreateCharacter(infosTemp[i]);
                CreateCharacter();
            }

            // 다른 플레이어의 오브젝트를 모두 생성한 후에 플레이어의 오브젝트를 모두에게 생성하도록 요청
            PlayerCreateRemovePacket crPacket = new PlayerCreateRemovePacket()
            { playerId = ClientProgram.Instance.ClientId, messageType = (ushort)MsgType.Create};
            ArraySegment<byte> segment = crPacket.Write();
            connector.CurrentSession.Send(segment);

            _onCreateAll = false;
        }

        // 캐릭터 생성(트리거)
        public void OnTriggerCreateCharacter(ObjectInfo playerInfo)
        {
            lock (_lock)
            {
                if (ClientId == playerInfo.id) _isMine = true;
                else _isMine = false;
                _onCreate = true;
                _newPlayerInfo = playerInfo;
            }
        }

        // 캐릭터 생성
        public void CreateCharacter()
        {
            lock ( _lock)
            {
                if (!_onCreate)
                    return;
                _onCreate = false;

                // 새 캐릭터 생성
                GameObject newObj = Instantiate(PlayerPrefabs, _newPlayerInfo.position, Quaternion.identity);
                newObj.transform.parent = _playerParent.transform;
                newObj.GetComponent<PlayerController>().SetMine(_isMine, _newPlayerInfo.id);
                
                // 딕셔너리에 새 캐릭터 추가
                playerObjDic.Add(_newPlayerInfo.id, newObj.GetComponent<PlayerController>());
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

            Destroy(playerObjDic[_exitId].gameObject);
            playerObjDic.Remove(_exitId);

            _exitId = -1;
        }

        // 본인이 나갔을 때 모든 플레이어 오프젝트 삭제
        public void RemovePlayerAll()
        {
            ChatContent.text += $"\n{NickName}님이 퇴장하셨습니다.";

            foreach (var player in playerObjDic)
                Destroy(player.Value.gameObject);
            playerObjDic.Clear();
        }


        // 채팅
        // 채팅 전송
        public void SendChatting()
        {
            if (ChatInputField.text == "")
                return;

            ChatPacket packet = new ChatPacket() { playerId = ClientId, chat = ChatInputField.text };
            ArraySegment<byte> segment = packet.Write();
            ChatInputField.text = ""; // 인풋 필드 지우기
            connector.CurrentSession.Send(segment);
        }

        // 채팅 받기
        public void RecvChatting(string chat)
        {
            _onChat = true;
            chatTemp = chat;
        }

        // 채팅 업데이트
        public void UpdateChatting()
        {
            if (!_onChat)
                return;
            _onChat = false;

            if (ChatContent.text == "")
                ChatContent.text += chatTemp;
            else
                ChatContent.text += "\n" + chatTemp;

            // 채팅창 화면 업데이트를 위함
            StartCoroutine(ScrollToBottom());

            Canvas.ForceUpdateCanvases();
            ChatScrollbar.value = 0;
        }

        // 채팅창 아래로 내리기(가장 최근 글이 맨 밑에 보이도록)
        private IEnumerator ScrollToBottom()
        {
            // 레이아웃이 확실히 갱신되도록 한 프레임 대기
            yield return null;

            // Canvas 강제 갱신
            Canvas.ForceUpdateCanvases();

            // ScrollRect 맨 아래
            ChatScrollbar.value = 0.0f;
        }
    }
}