using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour {

    public GameObject[] enemy;

    public Transform spawnPoint;

    private float maxHeight = 2f;
    private float minHeight = -2f;

    private float spawnTime = 5f;
    private float spawnNext = 5f;
    
    private float nextSpawnTime;


    void Start () {
        nextSpawnTime = Time.time + spawnTime;
    }

    void Update () {

        if (Time.time >= nextSpawnTime) {
            transform.position = new Vector3(spawnPoint.position.x, Random.Range(maxHeight, minHeight));
            Quaternion spawnRotation = Quaternion.identity;
            int rand = 2;//Random.Range(0, 2);
            Instantiate(enemy[rand], transform.position, transform.rotation);
            nextSpawnTime = Time.time + spawnNext;
        }
    }

    public void Reset()
    {
        nextSpawnTime = Time.time + spawnTime;
    }
}
