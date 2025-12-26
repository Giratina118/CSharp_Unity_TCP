using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    private float _speed = 10.0f;

    private void Start()
    {
        Destroy(gameObject, 10.0f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }
}
