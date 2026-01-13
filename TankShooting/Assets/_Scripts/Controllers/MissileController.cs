using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public GameObject ExplosionPrefab;

    private float _speed = 15.0f;

    private void Start()
    {
        Destroy(gameObject, 10.0f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player") && other.GetComponent<PlayerController>().IsMine))
            return;

        GameObject newParticle = Instantiate(ExplosionPrefab, this.transform.position, Quaternion.identity);
        newParticle.transform.SetParent(this.transform.parent);
        Destroy(newParticle, 5.0f);
        //newParticle.GetComponent<ParticleSystem>().Play();
        Destroy(this.gameObject);
    }
}
