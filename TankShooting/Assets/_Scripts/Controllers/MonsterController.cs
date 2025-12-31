using Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public GameObject Canvas;
    private GameObject _player = null;

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
    }

    IEnumerator ConnectWithPlayer()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _player = PlayerManager.Instance.PlayerObjDic[ClientProgram.Instance.ClientId].gameObject;
    }
}
