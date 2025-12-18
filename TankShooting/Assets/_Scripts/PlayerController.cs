using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Transform[] WheelObjects;   // 바퀴 오브젝트들
    public MissileController Missile;  // 미사일 프리팹
    public Transform Muzzle;           // 총구

    private Button FireMissileButton;  // 발사 버튼

    [SerializeField]
    private bool _isMine = false;      // 본인 오브젝트인지 여부
    private bool _isRollbackPos = false;
    [SerializeField]
    private long _playerId;
    [SerializeField]
    public ObjectInfo _playerInfo;    // 플레이어 아이디, 위치, 회전 정보
    private ObjectInfo _beforeInfo;    // 플레이어 아이디, 위치, 회전 정보

    private Vector3 _targetPos;
    private Quaternion _targetRot;

    private bool _isUpdatePos = false; // 업데이트 여부
    [SerializeField]
    private float _updateInterval = 0.25f; // 위치 업데이트 주기

    [SerializeField]
    private float _moveSpeed = 3.0f;   // 이동 속도
    private float _rotSpeed = 90.0f;   // 회전 속도
    private float _wheelSpeed = 150;   // 바퀴 회전 속도
    private Vector3 _translate; // 이동값(보간)
    private Vector3 _rotate;    // 회전값(보간)
    private float _updateTimer = 0.0f;

    [SerializeField]
    private Camera _camera;

    private void Start()
    {
        // 미사일 발사 버튼
        FireMissileButton = GameObject.Find("FireMissileButton").GetComponent<Button>();
        FireMissileButton.onClick.AddListener(OnClickFireButton);
    }

    void Update()
    {
        UpdateOtherPos(); // 위치 업데이트 (본인 외의 오브젝트)
        Movement();       // 이동 (본인 오브젝트)

        // 휠 입력받아 캐논 부분 바꾸기
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
    }

    // 플레이어 오브젝트 초기화
    public void SetMine(bool isMine, long playerId)
    {
        _isMine = isMine;
        _playerId = playerId;

        if (isMine)
        {
            _camera = Camera.main; // 카메라
            _camera.transform.position = new Vector3(0, 2, -2);
            _camera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            _camera.transform.SetParent(this.transform);

            if (_isMine)
                StartCoroutine(SendMove()); // 위치 정보 갱신
        }
    }

    // 다른 플레이어 위치 업데이트 트리거
    public void OnTriggerUpdateOtherPos(ObjectInfo info)
    {
        if (info.id != _playerId || _isMine)
            return;

        Debug.Log($"Update Other Pos | my id: {ClientProgram.Instance.ClientId}, recv id: {info.id}");

        _playerInfo.position = info.position;
        _playerInfo.rotation = info.rotation;
        _isUpdatePos = true;
    }

    // 다른 플레이어 위치 업데이트
    public void UpdateOtherPos()
    {
        if (_isMine)
            return;

        _updateTimer += Time.deltaTime;

        if (_isUpdatePos) // 1초에 4번 값 받아왔을 때만 작동
        {
            _isUpdatePos = false;
            _updateTimer = 0f; // 갱신 시 타이머 초기화
            _beforeInfo.position = transform.position;
            _beforeInfo.rotation = transform.rotation.eulerAngles;

            _translate = (_playerInfo.position - _beforeInfo.position) / _updateInterval; // 위치값 변화량
            _rotate = (_playerInfo.rotation - _beforeInfo.rotation) / _updateInterval;    // 회전값 변화량
            _rotate.y = Mathf.DeltaAngle(_beforeInfo.rotation.y, _playerInfo.rotation.y) / _updateInterval; // 0도/360도 부근을 통과할때 역회전이 발생하는 문제 방지
        }

        // 0.25초(혹은 _updateInterval) 이상 갱신이 없으면 멈춤
        if (_updateTimer > _updateInterval)
        {
            _translate = Vector3.zero;
            _rotate = Vector3.zero;
        }

        transform.position += _translate * Time.deltaTime;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + _rotate * Time.deltaTime);
    }

    // 롤백 트리거
    public void OnTriggerPlayerRollback(ObjectInfo rollbackInfo)
    {
        if (!_isMine) // 롤백이 아니면 넘김
            return;

        _isRollbackPos = true;

        Debug.Log("롤백");
        _playerInfo.position = rollbackInfo.position;
        _playerInfo.rotation = rollbackInfo.rotation;
    }

    // 이동 (본인 오브젝트)
    public void Movement()
    {
        if (!_isMine)
            return;

        if (_isRollbackPos)
        {
            _isRollbackPos = false;

            transform.position = _playerInfo.position;
            transform.rotation = Quaternion.Euler(_playerInfo.rotation);
        }

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            transform.Translate(new Vector3(0, 0, v) * _moveSpeed * Time.deltaTime);
            transform.Rotate(new Vector3(0, h, 0) * _rotSpeed * Time.deltaTime);

            RotateWheel(h, v); // 바퀴 회전
        }
    }

    // 위치 정보 서버에 전송
    IEnumerator SendMove()
    {
        // 자신의 위치 서버에 전송(서버에 등록 -> 서버에서 다른 클라에 전송)
        yield return new WaitForSecondsRealtime(_updateInterval); // 1초에 4번 전송

        MovePacket movePacket = new MovePacket();
        movePacket.messageType = (ushort)MsgType.MovePlayer;
        movePacket.objInfo.id = _playerId;
        movePacket.objInfo.position = transform.position;
        movePacket.objInfo.rotation = transform.rotation.eulerAngles;

        //Debug.Log($"pos: {{{movePacket.playerInfo.position.x}, {movePacket.playerInfo.position.y}, {movePacket.playerInfo.position.z}}}, " +
        //    $"rot: {{{movePacket.playerInfo.rotation.x}, {movePacket.playerInfo.rotation.y}, {movePacket.playerInfo.rotation.z}}}");

        ArraySegment<byte> segment = movePacket.Write();
        ClientProgram.Instance.connector.CurrentSession.Send(segment);
        StartCoroutine(SendMove());
    }

    // 바퀴 회전
    public void RotateWheel(float h, float v)
    {
        if (!_isMine)
            return;

        Vector3 rightWheelRotate = Vector3.zero, leftWheelRotate = Vector3.zero;
        float wheelRotateR = 0.0f, wheelRotateL = 0.0f;

        if (v > 0) // 전진
            wheelRotateR = wheelRotateL = _wheelSpeed;
        else if (v < 0) // 후진
            wheelRotateR = wheelRotateL = -_wheelSpeed;

        if (h > 0) // 우회전
        {
            wheelRotateR -= _wheelSpeed / 2.0f;
            wheelRotateL += _wheelSpeed / 2.0f;
        }
        else if (h < 0) // 좌회전
        {
            wheelRotateL -= _wheelSpeed / 2.0f;
            wheelRotateR += _wheelSpeed / 2.0f;
        }

        Mathf.Clamp(wheelRotateL, -_wheelSpeed, _wheelSpeed);
        Mathf.Clamp(wheelRotateR, -_wheelSpeed, _wheelSpeed);

        WheelObjects[0].Rotate(new Vector3(wheelRotateL, 0, 0) * Time.deltaTime);
        WheelObjects[1].Rotate(new Vector3(wheelRotateR, 0, 0) * Time.deltaTime);
        WheelObjects[2].Rotate(new Vector3(wheelRotateL, 0, 0) * Time.deltaTime);
        WheelObjects[3].Rotate(new Vector3(wheelRotateR, 0, 0) * Time.deltaTime);
    }

    // 미사일 발사 버튼
    public void OnClickFireButton()
    {
        if (!_isMine)
            return;

        // 직접 만들지 말고 서버에 메시지 전송하면 서버에서 모두에게 만듦
        // 발사한 플레이어의 총구 위치, 바라보는 방향(회전값) 정보 전달
        //Instantiate(Missile, Muzzle.position, this.transform.rotation);
        ClientProgram.Instance.SendMissile();

    }


    // 미사일 생성 트리거


    // 미사일 생성


    // 미사일 업데이트 트리거


    // 미사일 업데이트


}