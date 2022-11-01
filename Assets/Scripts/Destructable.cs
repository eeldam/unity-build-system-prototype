using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    public GameObject spawnOnDestroy;

    public int integrity = 100;

    public void OnDamage(int damage) {
        integrity -= damage;

        if (integrity <= 0) {
            Kill();
        }
    }

    public void Kill() {
        Debug.Log("Killing object");
        if (spawnOnDestroy != null) {
            GameObject spawnedObject = GameObject.Instantiate(spawnOnDestroy);
            spawnedObject.transform.parent = transform.parent;
            spawnedObject.transform.position = transform.position;
        }

        GameObject.Destroy(gameObject);
    }
}
