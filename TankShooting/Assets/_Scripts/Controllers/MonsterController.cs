using Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public GameObject Canvas;
    public Image HPBar;

    private GameObject _player = null;
    private bool _isHit = false;
    private int _damage;
    private int _curHP;
    private int _maxHP;

    void Start()
    {
        // 이동이나 공격 등의 수치는 서버에서 관리하기 때문에
        // 클라이언트에서는 애니메이션과 같은 것들을 관리
        StartCoroutine(ConnectWithPlayer());
    }

    void Update()
    {
        if (_player != null)
            Canvas.transform.LookAt(_player.transform);
        if (_isHit)
            Hit();
    }

    IEnumerator ConnectWithPlayer()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _player = PlayerManager.Instance.PlayerObjDic[ClientProgram.Instance.ClientId].gameObject;
    }

    public void OnTriggerHit(int damage, int curHP, int maxHP)
    {
        _isHit = true;
        _damage = damage;
        _curHP = curHP;
        _maxHP = maxHP;
    }

    public void Hit()
    {
        _isHit = false;
        HPBar.fillAmount = (float)_curHP / (float)_maxHP;
        Debug.Log($"{HPBar.fillAmount} {_curHP}/{_maxHP}");
    }
}
