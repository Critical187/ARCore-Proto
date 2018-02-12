using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using GoogleARCore.HelloAR;
using UnityEngine.EventSystems;

public class MainController : MonoBehaviour {

    public GameObject TrackedPlanePrefab;
    public GameObject PrefabToCreate;
    public Text Distance;

    private Camera mainCam;
    private List<TrackedPlane> newPlanes = new List<TrackedPlane>();
    private List<Vector3> markerPositions = new List<Vector3>();
    private bool isQuitting = false;

    // Use this for initialization
    void Start () {
        mainCam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
        // Quit on denied permission or timeout
        _QuitOnConnectionErrors();

        // Check that motion tracking is tracking.
        if (Frame.TrackingState != TrackingState.Tracking)
        {
            const int lostTrackingSleepTimeout = 15;
            Screen.sleepTimeout = lostTrackingSleepTimeout;
            return;
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Find all new planes
        _FindAllPlanes(newPlanes);

        _InstantiateObjectOnTouch();
    }

    private void _FindAllPlanes(List<TrackedPlane> NewPlanes)
    {
        Frame.GetPlanes(NewPlanes, TrackableQueryFilter.New);
        for (int i = 0; i < NewPlanes.Count; i++)
        {
            // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
            // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
            // coordinates.
            GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                transform);
            planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(NewPlanes[i]);
        }
    }

    private void _InstantiateObjectOnTouch()
    {
        // If the player has not touched the screen, we are done with this update.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        // Raycast against the location the player touched to search for planes.
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon;

        if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            if (Session.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                // Create marker object, set its tag for easier deletion and add its position to a list for distance 
                // calculation
                var markerObject = Instantiate(PrefabToCreate, hit.Pose.position, hit.Pose.rotation);
                markerObject.tag = "marker";
                markerPositions.Add(markerObject.transform.position);
                // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
                // world evolves.
                var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                // Andy should look at the camera but still be flush with the plane.
                markerObject.transform.LookAt(mainCam.transform);
                markerObject.transform.rotation = Quaternion.Euler(0.0f,
                    markerObject.transform.rotation.eulerAngles.y, markerObject.transform.rotation.z);

                // Make Andy model a child of the anchor.
                markerObject.transform.parent = anchor.transform;
                Distance.text = _GetTotalDistance().ToString();
            }
        }
    }

    

    private float _GetTotalDistance()
    {
        float distance = 0;
        if (markerPositions.Count < 2)
        {
            return distance;
        }
        for (int i = 0; i < markerPositions.Count; i++)
        {
            distance += Vector3.Distance(markerPositions[i], markerPositions[i + 1]);
        }
        return distance * 100;
    }

    public void ClearMarkers()
    {
        markerPositions.Clear();
        _DestroyGameObjectsWithTag("marker");
        Distance.text = "0";
    }

    private void _DestroyGameObjectsWithTag(string tag)
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject target in gameObjects)
        {
            GameObject.Destroy(target);
        }
    }
    private void _QuitOnConnectionErrors()
    {
        if (isQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
        if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
        {
            _ShowAndroidToastMessage("Camera permission is needed to run this application.");
            isQuitting = true;
            Invoke("DoQuit", 0.5f);
        }
        else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
        {
            _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
            isQuitting = true;
            Invoke("DoQuit", 0.5f);
        }
    }

    private void _DoQuit()
    {
        Application.Quit();
    }

    private void _ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                    message, 0);
                toastObject.Call("show");
            }));
        }
    }
}
