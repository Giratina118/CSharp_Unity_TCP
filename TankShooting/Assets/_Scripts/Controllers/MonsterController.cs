using Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public GameObject Canvas;
    public Image HPBar;
    public Vector3 SpawnPos;
    public Vector3 targetPos;

    private GameObject _player = null;
    private bool _isHit = false;
    private int _curHP;
    private int _maxHP;
    private float _speed;
    private float _hpBarVisibleDistance = 30.0f; // hp바 가시거리
    private bool _onMove = false;

    enum MonsterKey
    {
        Ray = 1001,
        Bee = 2001,
    }

    void Start()
    {
        // 이동이나 공격 등의 수치는 서버에서 관리하기 때문에
        // 클라이언트에서는 애니메이션과 같은 것들을 관리
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

            if (Canvas.gameObject.activeSelf)
                Canvas.gameObject.transform.LookAt(_player.transform.position);
        }

        Hit();
        Move();
    }

    public void Init(ObjectInfo info)
    {
        switch (info.ObjType)
        {
            case (ushort)MonsterKey.Ray:  _speed = 1.5f;  break;
            case (ushort)MonsterKey.Bee:  _speed = 2.0f;  break;
        }
        targetPos = SpawnPos = info.Position;
        _speed /= 2.0f;
    }

    IEnumerator ConnectWithPlayer()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _player = PlayerManager.Instance.PlayerObjDic[ClientProgram.Instance.ClientId].gameObject;
    }

    public void OnTriggerHit(int damage, int curHP, int maxHP)
    {
        _isHit = true;
        _curHP = curHP;
        _maxHP = maxHP;
    }

    public void Hit()
    {
        if (!_isHit)
            return;

        _isHit = false;
        HPBar.fillAmount = (float)_curHP / (float)_maxHP;
        Debug.Log($"{HPBar.fillAmount} {_curHP}/{_maxHP}");
    }

    public void Move()
    {
        if (!_onMove)
            return;

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            return;

        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    public void SetTargetPos(Vector3 pos)
    {
        transform.LookAt(pos);
        targetPos = pos;
        _onMove = true;
    }
}
