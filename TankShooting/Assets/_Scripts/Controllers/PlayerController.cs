using Client;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Transform[] WheelObjects;  // 바퀴 오브젝트들
    public Transform Muzzle;          // 총구
    public bool IsMine = false;       // 본인 오브젝트인지

    private Camera _camera; // 카메라
    private long _clientId; // 클라이언트 아이디
    private string _name;   // 이름

    private ObjectInfo _playerInfo;  // 해당 플레이어 오브젝트의 현재 위치, 회전 정보
    private ObjectInfo _beforeInfo;  // 해당 플레이어 오브젝트의 이전 위치, 회전 정보
    private Vector3 _translate;      // 이동값(보간)
    private Vector3 _rotate;         // 회전값(보간)
    private float _moveSpeed = 4.0f; // 이동 속도
    private float _rotSpeed = 90.0f; // 회전 속도
    private float _wheelSpeed = 150; // 바퀴 회전 속도

    private bool _isRollback = false;      // 롤백 여부
    private bool _isUpdatePos = false;     // 위치 업데이트 여부
    private float _updateInterval = 0.25f; // 위치 업데이트 주기
    private float _updateTimer = 0.0f;     // 위치 업데이트 타이머

    [SerializeField]
    private Image _hpBar;
    private int _maxHp = 100;
    private int _curHp = 100;
    private bool _onUpdateHpBar = false;


    void Update()
    {
        UpdateOtherPos(); // 위치 업데이트 (본인 외의 오브젝트)
        Movement();       // 이동 (본인 오브젝트)
        UpdateHpbar();    // 체력 업데이트 (본인)

        // 휠 입력받아 캐논 부분 바꾸기
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
    }

    // 플레이어 오브젝트 초기화
    public void SetMine(bool isMine, long playerId)
    {
        IsMine = isMine;
        _clientId = playerId;
        GetComponent<Collider>().enabled = true;

        if (isMine)
        {
            _camera = Camera.main; // 카메라
            _camera.transform.position = new Vector3(0, 3, -2);
            _camera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            _camera.transform.SetParent(this.transform);

            _hpBar = GameObject.Find("HpBar").GetComponent<Image>();

            StartCoroutine(SendMove()); // 위치 정보 갱신
        }
    }

    // 다른 플레이어 위치 업데이트 트리거
    public void OnTriggerUpdateOtherPos(ObjectInfo info)
    {
        if (info.Id != _clientId || IsMine)
            return;

        //Debug.Log($"Update Other Pos | my id: {ClientProgram.Instance.ClientId}, recv id: {info.id}");

        _playerInfo.Position = info.Position;
        _playerInfo.Rotation = info.Rotation;
        _isUpdatePos = true;
    }

    // 다른 플레이어 위치 업데이트
    public void UpdateOtherPos()
    {
        if (IsMine)
            return;

        _updateTimer += Time.deltaTime;

        if (_isUpdatePos) // 1초에 4번 값 받아왔을 때만 작동
        {
            _isUpdatePos = false;
            _updateTimer = 0f; // 갱신 시 타이머 초기화
            _beforeInfo.Position = transform.position;
            _beforeInfo.Rotation = transform.rotation.eulerAngles;

            _translate = (_playerInfo.Position - _beforeInfo.Position) / _updateInterval; // 위치값 변화량
            _rotate = (_playerInfo.Rotation - _beforeInfo.Rotation) / _updateInterval;    // 회전값 변화량
            _rotate.y = Mathf.DeltaAngle(_beforeInfo.Rotation.y, _playerInfo.Rotation.y) / _updateInterval; // 0도/360도 부근을 통과할때 역회전이 발생하는 문제 방지
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
        if (!IsMine) // 롤백이 아니면 넘김
            return;

        _isRollback = true;

        Debug.Log("롤백");
        _playerInfo.Position = rollbackInfo.Position;
        _playerInfo.Rotation = rollbackInfo.Rotation;
    }

    // 이동 (본인 오브젝트)
    public void Movement()
    {
        if (!IsMine)
            return;

        if (_isRollback)
        {
            _isRollback = false;

            transform.position = _playerInfo.Position;
            transform.rotation = Quaternion.Euler(_playerInfo.Rotation);
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
        movePacket.MessageType = (ushort)MsgType.MovePlayer;
        movePacket.ObjInfo.Id = _clientId;
        movePacket.ObjInfo.Position = transform.position;
        movePacket.ObjInfo.Rotation = transform.rotation.eulerAngles;

        //Debug.Log($"pos: {{{movePacket.playerInfo.position.x}, {movePacket.playerInfo.position.y}, {movePacket.playerInfo.position.z}}}, " +
        //    $"rot: {{{movePacket.playerInfo.rotation.x}, {movePacket.playerInfo.rotation.y}, {movePacket.playerInfo.rotation.z}}}");

        ArraySegment<byte> segment = movePacket.Write();
        ClientProgram.Instance.Connector.CurrentSession.Send(segment);
        StartCoroutine(SendMove());
    }

    // 바퀴 회전
    public void RotateWheel(float h, float v)
    {
        if (!IsMine)
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

    // hp바 업데이트 트리거
    public void OnTriggerUpdateHpbar(int curHp)
    {
        _curHp = Mathf.Clamp(curHp, 0, _maxHp);
        _onUpdateHpBar = true;
    }

    // hp바 업데이트
    public void UpdateHpbar()
    {
        if (!_onUpdateHpBar)
            return;

        _onUpdateHpBar = false;
        Debug.Log($"{_curHp} / {_maxHp}");
        _hpBar.fillAmount = (float)_curHp / _maxHp; ;
    }
}