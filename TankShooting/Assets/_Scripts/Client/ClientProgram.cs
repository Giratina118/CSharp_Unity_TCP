using JetBrains.Annotations;
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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = System.Random;

namespace Client
{
    class ClientProgram : MonoBehaviour
    {
        public static ClientProgram Instance { get; private set; }

        public GameObject loadingScreen; // 로딩 화면

        public Connector Connector; // 커넥터
        public long ClientId;   // 본인 id
        public string NickName; // 이름
        public int Score = 0;   // 점수

        private bool _isGameOver = false; // 게임 오버 여부
        private string  _host;       // 호스트
        private IPHostEntry _ipHost; // 호스트 ip
        private IPAddress _ipAddr;   // ip 주소
        private IPEndPoint _endPoint;// ip 포트 접근

        void Awake()
        {
            Screen.SetResolution(1280, 720, false); // false: 창모드

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _host = Dns.GetHostName();
            _ipHost = Dns.GetHostEntry(_host);
            _ipAddr = _ipHost.AddressList[0];
            _endPoint = new IPEndPoint(_ipAddr, 7777);
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().buildIndex != 1)
                return;

            try
            {
                StructureManager.Instance.CreateStructureAll(); // 건물 생성
                MonsterManager.Instance.CreateMonsterAll();     // 모든 몬스터 생성
                PlayerManager.Instance.CreateCharacterAll();    // 다른 플레이어 캐릭터 생성
                PlayerManager.Instance.CreateCharacter();       // 본인 캐릭터 생성
                MissileManager.Instance.CreateMissile();        // 미사일 생성
                ChatManager.Instance.UpdateChatting();          // 채팅 업데이트

                if (Input.GetKeyDown(KeyCode.Return))
                    ChatManager.Instance.SendChatting();        // 문자 보내기

                PlayerManager.Instance.RemoveExitCharacter();   // 나간 플레이어 제거
                OnDisconnectServer();                           // 게임 오버 체크
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        // 서버 연결
        public void OnConnectServer(long id, string name)
        {
            NickName = name;
            ClientId = id;

            loadingScreen.SetActive(true);
            StartCoroutine(LoadingScreen());
            SceneManager.LoadSceneAsync(1);

            // 커넥터 생성
            Connector = new Connector();
            Connector.Connect(_endPoint, () => { return new ServerSession(); });
        }

        // 서버 연결 해제
        public void OnDisconnectServer()
        {
            if (_isGameOver && Connector != null && Connector.CurrentSession != null)
            {
                _isGameOver = false;

                // 서버에 연결 해제 요청
                Camera camera = Camera.main;
                camera.transform.parent = new GameObject("CameraParent").transform;

                Connector.CurrentSession.Disconnect();

                // 점수창
                UIManager.Instance.OpenResultScreen();
            }
        }

        // 게임 오버
        public void GameOver()
        {
            _isGameOver = true;
        }

        // 로딩화면
        IEnumerator LoadingScreen()
        {
            yield return new WaitForSecondsRealtime(2.0f);
            loadingScreen.SetActive(false);
        }
    }
}