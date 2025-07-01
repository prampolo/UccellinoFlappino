using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSpawn : MonoBehaviour {

    public GameObject health;
    public Transform spawnPoint;

    private float spawnTime = 11f;
    private float spawnNext = 17f;

    private float maxHeight = 2f;
    private float minHeight = -2f;

    private float nextSpawnTime;


    void Start () {
        nextSpawnTime = Time.time + spawnTime;
    }
	
	void Update () {
        if (Time.time >= nextSpawnTime)
        {
            transform.position = new Vector3(spawnPoint.position.x, Random.Range(maxHeight, minHeight));
            Quaternion spawnRotation = Quaternion.identity;
            int rand = Random.Range(0, 2);
            Instantiate(health, transform.position, transform.rotation);
            nextSpawnTime = Time.time + spawnNext;
        }
    }

    public void Reset()
    {
        nextSpawnTime = Time.time + spawnTime;
    }
}
