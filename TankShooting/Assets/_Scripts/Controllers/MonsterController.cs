using Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public GameObject Canvas; // 캔버스(hp바 + @)
    public Image HPBar;       // hp바
    public Vector3 SpawnPos;  // 스폰 위치
    public Vector3 targetPos; // 이동 목표 위치

    private GameObject _player = null; // 플레이어 거리에 따라 캔버스 on/off
    private float _hpBarVisibleDistance = 30.0f; // hp바 가시거리
    private int _curHP;   // 현재 hp
    private int _maxHP;   // 최대 hp
    private float _speed; // 이동 속도
    private bool _isHit = false;  // 피격 여부
    private bool _onMove = false; // 이동 관리

    // 몬스터 유형
    enum MonsterKey
    {
        Ray = 1001, // 가오리 몬스터
        Bee = 2001, // 벌 몬스터
    }

    void Start()
    {
        // 이동이나 공격 등의 수치는 서버에서 관리하기 때문에 클라이언트에서는 애니메이션과 같은 것들을 관리
        StartCoroutine(ConnectWithPlayer());
    }

    void Update()
    {
        if (_player != null)
        {
            // 거리에 따라 hp바 on/off
            if (Canvas.gameObject.activeSelf  && Vector3.Distance(_player.transform.position, gameObject.transform.position) > _hpBarVisibleDistance)
                Canvas.gameObject.SetActive(false);
            if (!Canvas.gameObject.activeSelf && Vector3.Distance(_player.transform.position, gameObject.transform.position) < _hpBarVisibleDistance)
                Canvas.gameObject.SetActive(true);

            if (Canvas.gameObject.activeSelf) // hp바가 플레이어를 향하도록
                Canvas.gameObject.transform.LookAt(_player.transform.position);
        }

        Hit();  // 피격
        Move(); // 이동
    }

    // 초기화
    public void Init(ObjectInfo info)
    {
        switch (info.ObjType)
        {
            case (ushort)MonsterKey.Ray:  _speed = 1.5f;  break;
            case (ushort)MonsterKey.Bee:  _speed = 2.0f;  break;
        }
        targetPos = SpawnPos = info.Position;
    }

    // 플레이어와 연결
    IEnumerator ConnectWithPlayer()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _player = PlayerManager.Instance.PlayerObjDic[ClientProgram.Instance.ClientId].gameObject;
    }

    // 피격 트리거
    public void OnTriggerHit(int damage, int curHP, int maxHP)
    {
        _isHit = true;
        _curHP = curHP;
        _maxHP = maxHP;
    }

    // 피격
    public void Hit()
    {
        if (!_isHit)
            return;

        _isHit = false;
        HPBar.fillAmount = (float)_curHP / (float)_maxHP;
        Debug.Log($"{HPBar.fillAmount} {_curHP}/{_maxHP}");
    }

    // 이동
    public void Move()
    {
        if (!_onMove)
            return;

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            return;

        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    // 이동 위치 설정
    public void SetTargetPos(Vector3 pos)
    {
        transform.LookAt(pos);
        targetPos = pos;
        _onMove = true;
    }

    // 사망
    public void Die()
    {
        _curHP = _maxHP;
        HPBar.fillAmount = 1.0f;
        transform.position = SpawnPos;
        gameObject.SetActive(false);
    }
}
