using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CoinSpawn : MonoBehaviour {

    public GameObject[] coin;
    public Transform spawnPoint;

    private float spawnTime = 3f;
    private float spawnNext = 13f;

    private float maxHeight = 2f;
    private float minHeight = -2f;

    private float nextSpawnTime;

    void Start () {
        nextSpawnTime = Time.time + spawnTime;
    }
	
	void Update () {

        if (Time.time >= nextSpawnTime)
        {
            Debug.Log("Ciccio");
            transform.position = new Vector3(spawnPoint.position.x, Random.Range(maxHeight, minHeight));
            Quaternion spawnRotation = Quaternion.identity;
            int rand = Random.Range(0, 2);
            Instantiate(coin[rand], transform.position, transform.rotation);
            nextSpawnTime = Time.time + spawnNext;
        }
    }

    public void Reset()
    {
        nextSpawnTime = Time.time + spawnTime;
    }
}
