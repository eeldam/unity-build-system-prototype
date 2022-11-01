/*

- torches (single snap point, attached light)
- allow destroying
- resource cost (start with no-inventory system, just have to collect and use)
- crosshairs and inventory display
- prototype out the "resource gathering area" mechanic

*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildController : MonoBehaviour
{
    public GameObject container;

    public GameObject[] buildables;

    public int selectedBuildableIndex = 0;

    public float yAxisRotation = 0f;

    private const float Y_AXIS_ROTATION_INCREMENT = 22.5f;

    private GameObject buildCursor;

    public bool isBuilding = false;

    private BuildableObject cursorBuildable;

    public InputAction leftClick;

    public InputAction rightClick;

    public InputAction mouseWheel;

    public InputAction toggleBuild;

    public int hitStrength = 50;

    private void OnEnable() {
        leftClick.Enable();
        rightClick.Enable();
        mouseWheel.Enable();
        toggleBuild.Enable();
    }

    private void OnDisable() {
        leftClick.Disable();
        rightClick.Disable();
        mouseWheel.Disable();
        toggleBuild.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (this.isBuilding) {
            SetBuildCursor();
        }
    }

    private void SetBuildCursor() {
        if (buildCursor != null) {
            GameObject.Destroy(buildCursor);
        }

        buildCursor = GameObject.Instantiate(buildables[selectedBuildableIndex]);
        Collider collider = buildCursor.GetComponentInChildren<Collider>();
        if (collider) {
            collider.enabled = false;
        }
        cursorBuildable = buildCursor.GetComponent<BuildableObject>();
    }

    private Vector3 _hitPosition;
    private Vector3 _realPosition;
    private List<Vector3> _snapPoints = new List<Vector3>();

    private void ToggleBuilding() {
        this.isBuilding = !this.isBuilding;
        if (this.isBuilding) {
            SetBuildCursor();
        } else if (buildCursor != null) {
            GameObject.Destroy(buildCursor);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (toggleBuild.WasPressedThisFrame()) {
            ToggleBuilding();
            return;
        }

        if (isBuilding) {
            BuildUpdate();
        } else {
            NormalUpdate();
        }
    }

    private Vector3 _hitPoint;

    private void NormalUpdate() {
        LayerMask rayMask = 1 << LayerMask.NameToLayer("Player");

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width/2f, Screen.height/2f, 0f)
        );
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 20f, ~rayMask)) {
            _hitPoint = hit.point;

            if (leftClick.triggered) {
                Destructable target = hit.collider.gameObject.GetComponentInParent<Destructable>();
                if (target != null) {
                    target.OnDamage(hitStrength);
                }
            }
        }
    }

    private void BuildUpdate() {
        if (!isBuilding) return;

        if (rightClick.triggered) {
            selectedBuildableIndex = (selectedBuildableIndex + 1) % buildables.Length;
            SetBuildCursor();
        }

        float scrollWheel = mouseWheel.ReadValue<float>();
        if (scrollWheel != 0f) {
            float rotation = 0f;
            if (scrollWheel > 0f) {
                rotation = Y_AXIS_ROTATION_INCREMENT;
            } else {
                rotation = -Y_AXIS_ROTATION_INCREMENT;
            }

            buildCursor.transform.Rotate(new Vector3(0f, rotation, 0f), Space.Self);
        }

        _snapPoints.Clear();

        LayerMask rayMask = 1 << LayerMask.NameToLayer("Player");

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width/2f, Screen.height/2f, 0f)
        );
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 20f, ~rayMask)) {

            Vector3 buildPosition = hit.point;
            
            if (cursorBuildable != null) {
                buildPosition -= cursorBuildable.buildOffset;
            }

            buildCursor.transform.position = hit.point;
            _hitPosition = hit.point;
            _realPosition = buildPosition;

            // Snapping logic
            BuildableObject other = hit.collider.gameObject.GetComponentInParent<BuildableObject>();

            if (cursorBuildable == null) {
                // Do nothing
            } else if (other == null) {
                buildCursor.transform.position -= cursorBuildable.buildOffset;
            } else {
                Transform otherTransform = other.transform;
                Vector3 otherPosition = otherTransform.position;

                // First, find the closest joints on the target object to our cursor's center
                List<Vector3> otherJoints = other.joints.Select(
                    j => otherPosition + (otherTransform.rotation * j)
                ).OrderBy(
                    j => {
                        Vector3 delta = j - hit.point;
                        return Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2) + Mathf.Pow(delta.z, 2);
                    }
                ).ToList();

                // Second, find the closest joints on our cursor object to the target's center
                List<Vector3> thisJoints = cursorBuildable.joints.Select(
                    j => hit.point + (buildCursor.transform.rotation * j)
                ).OrderBy(
                    j => {
                        Vector3 delta = j - otherPosition;
                        return Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2) + Mathf.Pow(delta.z, 2);
                    }
                ).ToList();

                Vector3 closestOther = otherJoints[0];
                Vector3 closestThis = thisJoints[0];
                float smallestDistance = float.PositiveInfinity;

                for (int i = 0; i < 2; i++) {
                    Vector3 otherPoint = otherJoints[i];
                    _snapPoints.Add(otherPoint);
                    for (int j = 0; j < 2; j++) {
                        Vector3 thisPoint = thisJoints[j];
                        Vector3 delta = thisPoint - otherPoint;
                        float distance =  Mathf.Pow(delta.x, 2) + Mathf.Pow(delta.y, 2) + Mathf.Pow(delta.z, 2);
                        if (distance < smallestDistance) {
                            closestOther = otherPoint;
                            closestThis = thisPoint;
                            smallestDistance = distance;
                        }
                    }
                }

                for (int i = 0; i < 2; i++) {
                    _snapPoints.Add(thisJoints[i]);
                }

                Vector3 snapOffest = closestOther - closestThis;
                buildCursor.transform.position += snapOffest;
            }
        }

        if (leftClick.triggered) {
            GameObject newInstance = GameObject.Instantiate(buildables[selectedBuildableIndex]);
            newInstance.transform.parent = container.transform;
            newInstance.transform.position = buildCursor.transform.position;
            newInstance.transform.rotation = buildCursor.transform.rotation;
        }
    }

    void OnDrawGizmos() {
        if (_hitPoint != null) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(_hitPoint, Vector3.one * .15f);
        }
        if (_hitPosition != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_hitPosition, 0.15f);
        }
        if (_realPosition != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(_realPosition, 0.15f);
        }
        Gizmos.color = Color.yellow;
        foreach (Vector3 snap in _snapPoints) {
            Gizmos.DrawSphere(snap, 0.15f);
        }
    }
}
