using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class Marker {

    private GameObject MarkerPrefab;

    public Vector3 position;

    private bool placed;

    public Marker(GameObject markerPrefab)
    {
        this.MarkerPrefab = markerPrefab;
    }

    public void Reset()
    {
        position = Vector3.zero;
        placed = false;
    }

    public GameObject Create(Pose pose)
    {
        GameObject marker = GameObject.Instantiate(MarkerPrefab, pose.position, pose.rotation);
        this.position = pose.position;
        return marker;
    }
}
