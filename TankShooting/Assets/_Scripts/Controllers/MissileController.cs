using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public GameObject ExplosionPrefab; // 폭발 파티클 프리팹
    public AudioSource FireSound;      // 발포 사운드

    private float _speed = 15.0f;      // 이동 속도
    private float _lifeTime = 10.0f;   // 생존 시간

    private void Start()
    {
        Destroy(gameObject, _lifeTime);
        //FireSound.PlayOneShot(FireSound.clip); // 발포 사운드 재생
        AudioSource.PlayClipAtPoint(FireSound.clip, transform.position);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player") && other.GetComponent<PlayerController>().IsMine))
            return;

        GameObject newParticle = Instantiate(ExplosionPrefab, this.transform.position, Quaternion.identity); // 폭발 파티클
        newParticle.transform.SetParent(this.transform.parent);
        AudioSource explosionAudio = newParticle.GetComponent<AudioSource>();
        //explosionAudio.PlayOneShot(explosionAudio.clip); // 폭발 사운드 재생
        Debug.Log(explosionAudio.clip.name);

        Destroy(newParticle, 5.0f);
        // 미사일 파괴 위치에서 사운드를 독립적으로 재생
        AudioSource.PlayClipAtPoint(explosionAudio.clip, transform.position);
        Destroy(this.gameObject);
    }
}
