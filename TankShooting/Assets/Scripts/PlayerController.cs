using DummyClient;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public Transform[] WheelObjects = new Transform[4]; // 바퀴 오브젝트들

    [SerializeField]
    private bool _isMine = false;      // 본인 오브젝트인지 여부
    [SerializeField]
    private PlayerInfo _playerInfo;    // 플레이어 아이디, 위치, 회전 정보
    private PlayerInfo _beforeInfo;    // 플레이어 아이디, 위치, 회전 정보

    private bool _isUpdatePos = false;
    private float _updateInterval = 0.25f;
    private float _moveSpeed = 3.0f;   // 이동 속도
    private float _rotSpeed = 90.0f;   // 회전 속도
    private float _wheelSpeed = 150;   // 바퀴 회전 속도
    private Vector3 _translate;
    private Vector3 _rotate;


    private void Start()
    {
        StartCoroutine(SendMove()); // 위치 정보 갱신
    }

    void Update()
    {
        UpdateOtherPos(); // 위치 업데이트 (본인 외의 오브젝트)
        Movement();       // 이동 (본인 오브젝트)
    }

    // 플레이어 오브젝트 초기화
    public void SetMine(bool isMine, long clientId)
    {
        _isMine = isMine;
        _playerInfo.id = clientId;
    }

    // 다른 플레이어 위치 업데이트 트리거
    public void OnTriggerUpdateOtherPos(PlayerInfo info)
    {
        Debug.Log($"{_playerInfo.id}, pos: {info.position}, rot: {info.rotation}");
        _isUpdatePos = true;

        _playerInfo.position = info.position;
        _playerInfo.rotation = info.rotation;
    }

    // 다른 플레이어 위치 업데이트
    public void UpdateOtherPos()
    {
        if (_isMine)
            return;

        if (_isUpdatePos) // 1초에 4번 값 받아왔을 때만 작동
        {
            _isUpdatePos = false;
            _beforeInfo.position.FromVector3(transform.position);
            _beforeInfo.rotation.FromVector3(transform.rotation.eulerAngles);

            _translate = (_playerInfo.position.ToVector3() - _beforeInfo.position.ToVector3()) / _updateInterval; // 위치값 변화량
            _rotate = (_playerInfo.rotation.ToVector3() - _beforeInfo.rotation.ToVector3()) / _updateInterval;    // 회전값 변화량
            _rotate.y = Mathf.DeltaAngle(_beforeInfo.rotation.ToVector3().y, _playerInfo.rotation.ToVector3().y) / _updateInterval; // 0도/360도 부근을 통과할때 역회전이 발생하는 문제 방지
        }

        transform.position += _translate * Time.deltaTime;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + _rotate * Time.deltaTime);

        /*
        if (_isUpdatePos)    // 1초에 4번 값이 들어올 때 (새 타겟 위치/회전 정보 수신)
        {
            _isUpdatePos = false;

            // 위치
            _beforePos = transform.position;
            _targetPos = _playerInfo.position.ToVector3();

            // 회전(Quaternion으로 변환)
            _beforeRot = transform.rotation;
            _targetRot = Quaternion.Euler(_playerInfo.rotation.ToVector3());

            _elapsed = 0f;
        }

        // 시간 증가
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _updateInterval);

        // ★ 위치 자연스러운 보간
        transform.position = Vector3.Lerp(_beforePos, _targetPos, t);

        // ★ 회전을 Slerp로 자연스럽게 보간
        transform.rotation = Quaternion.Slerp(_beforeRot, _targetRot, t);
        */
    }

    // 이동 (본인 오브젝트)
    public void Movement()
    {
        if (!_isMine)
            return;

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            transform.Translate(new Vector3(0, 0, v) * _moveSpeed * Time.deltaTime);
            transform.Rotate(new Vector3(0, h, 0) * _rotSpeed * Time.deltaTime);

            RotateWheel(h, v); // 바퀴 회전
        }
    }

    IEnumerator SendMove()
    {
        // 자신의 위치 서버에 전송(서버에 등록 -> 서버에서 다른 클라에 전송)
        yield return new WaitForSecondsRealtime(_updateInterval); // 1초에 4번 전송

        PlayerMovePacket movePacket = new PlayerMovePacket();
        movePacket.playerInfo.id = _playerInfo.id;
        movePacket.playerInfo.position.FromVector3(transform.position);
        movePacket.playerInfo.rotation.FromVector3(transform.rotation.eulerAngles);

        Debug.Log($"pos: {{{movePacket.playerInfo.position.X}, {movePacket.playerInfo.position.Y}, {movePacket.playerInfo.position.Z}}}, " +
            $"rot: {{{movePacket.playerInfo.rotation.X}, {movePacket.playerInfo.rotation.Y}, {movePacket.playerInfo.rotation.Z}}}");

        ArraySegment<byte> segment = movePacket.Write();
        ClientProgram.Instance.connector.CurrentSession.Send(segment);
        StartCoroutine(SendMove());
    }

    // 바퀴 회전
    public void RotateWheel(float h, float v)
    {
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
}