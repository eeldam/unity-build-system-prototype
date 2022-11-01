using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildableObject : MonoBehaviour
{
    public Vector3[] joints;

    public Vector3 buildOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Vector3 joint in joints) {
            Vector3 point = transform.position + (transform.rotation * joint);
            Gizmos.DrawSphere(point, .1f);
        }

        if (buildOffset != Vector3.zero) {
            Gizmos.color = Color.cyan;
            Vector3 point = transform.position + (transform.rotation * buildOffset);
            Gizmos.DrawSphere(point, .1f);
        }
    }
}
