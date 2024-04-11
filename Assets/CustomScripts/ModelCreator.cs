using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Vuforia;

public class TouchTriggeredHouseCreation : MonoBehaviour
{
    public GameObject[] cornerTargets;
    public GameObject houseTarget;
    public float distanceThreshold = 0.5f;
    public GameObject wallPrefab;
    public GameObject[] openingTargets;
    public GameObject[] furnitureTargets;
    public Material floorMaterial;
    public float floorOpacity = 0.5f;

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CreateHouse();
        }
    }

    void CreateHouse()
    {
        if (AreTargetsTracked())
        {
            CreateBase(); // NEEDS ROTATED
            CreateWalls(); // NEEDS TO BE PLACED CORRECTLY
            CreateOpenings();
            CreateFurniture();
            CreateFloor();
        }
        else
        {
            Debug.LogWarning("REQUIRES ALL CORNERS TO BE TRACKED!");
        }
    }

    bool AreTargetsTracked()
    {
        foreach (GameObject target in cornerTargets)
        {
            if (target.GetComponent<TrackableBehaviour>().CurrentStatus != TrackableBehaviour.Status.TRACKED)
                return false;
        }
        return true;
    }

    void CreateBase()
    {
        Vector3 origin = Vector3.zero;
        foreach (GameObject target in cornerTargets)
        {
            origin += target.transform.position;
        }
        origin /= cornerTargets.Length;
        Vector3[] relativePositions = new Vector3[cornerTargets.Length];
        for (int i = 0; i < cornerTargets.Length; i++)
        {
            relativePositions[i] = cornerTargets[i].transform.position - origin;
        }
        for (int i = 0; i < cornerTargets.Length; i++)
        {
            DestroyExistingChild(houseTarget, cornerTargets[i].name + "(Clone)");
            GameObject childCopy = Instantiate(cornerTargets[i], houseTarget.transform);
            childCopy.transform.localPosition = relativePositions[i];
        }
    }

    void CreateWalls()
    {
        for (int i = 0; i < cornerTargets.Length; i++)
        {
            GameObject currentTarget = cornerTargets[i];
            GameObject nextTarget = cornerTargets[(i + 1) % cornerTargets.Length];
            if (currentTarget.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED &&
                nextTarget.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
            {
                float distanceX = Mathf.Abs(nextTarget.transform.position.x - currentTarget.transform.position.x);
                float distanceZ = Mathf.Abs(nextTarget.transform.position.z - currentTarget.transform.position.z);
                if (distanceX <= distanceThreshold || distanceZ <= distanceThreshold)
                {
                    Vector3 centerPosition = (currentTarget.transform.position + nextTarget.transform.position) / 2f;
                    Vector3 origin = Vector3.zero;
                    foreach (GameObject target in cornerTargets)
                    {
                        origin += target.transform.position;
                    }
                    origin /= cornerTargets.Length;
                    Vector3 relativeWallPosition = centerPosition - origin;
                    if (!WallExistsAtPosition(centerPosition))
                    {
                        string wallName = "Wall (" + currentTarget.name + nextTarget.name + ")";
                        DestroyExistingChild(houseTarget, wallName);
                        GameObject wall = Instantiate(wallPrefab, houseTarget.transform);
                        wall.name = wallName;
                        wall.transform.localPosition = relativeWallPosition;
                        Vector3 scale = Vector3.zero;
                        if (distanceX <= distanceThreshold)
                        {
                            scale = new Vector3(distanceX, 0.2487f, 0.2487f);
                            wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        }
                        else if (distanceZ <= distanceThreshold)
                        {
                            scale = new Vector3(distanceZ, 0.2487f, 0.2487f);
                            wall.transform.rotation = Quaternion.identity;
                        }
                        wall.transform.localScale = scale;
                        wall.transform.parent = houseTarget.transform;
                    }
                }
            }
            else
            {
                if (currentTarget.GetComponent<TrackableBehaviour>().CurrentStatus != TrackableBehaviour.Status.TRACKED)
                {
                    Debug.LogWarning(currentTarget.name + " is not found.");
                }
                if (nextTarget.GetComponent<TrackableBehaviour>().CurrentStatus != TrackableBehaviour.Status.TRACKED)
                {
                    Debug.LogWarning(nextTarget.name + " is not found.");
                }
            }
        }
    }

    bool WallExistsAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.CompareTag("Wall"))
            {
                return true;
            }
        }
        return false;
    }

    void DestroyExistingChild(GameObject targetParent, string childName)
    {
        Transform existingChild = targetParent.transform.Find(childName);
        if (existingChild != null)
        {
            Destroy(existingChild.gameObject);
        }
    }

    void CreateOpenings()
    {
        foreach (GameObject target in openingTargets)
        {
            if (target.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
            {
                Vector3 targetPosition = target.transform.position;
                Vector3 origin = Vector3.zero;
                foreach (GameObject target2 in cornerTargets)
                {
                    origin += target2.transform.position;
                }
                origin /= cornerTargets.Length;
                Vector3 relativePosition = targetPosition - origin;
                GameObject corner1 = null;
                GameObject corner2 = null;
                foreach (GameObject corner in cornerTargets)
                {
                    if (corner.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
                    {
                        bool withinThresholdX = Mathf.Abs(corner.transform.position.x - targetPosition.x) <= distanceThreshold;
                        bool withinThresholdZ = Mathf.Abs(corner.transform.position.z - targetPosition.z) <= distanceThreshold;
                        if (withinThresholdX)
                        {
                            if (corner.transform.position.x < targetPosition.x)
                                corner1 = corner;
                            else if (corner.transform.position.x > targetPosition.x)
                                corner2 = corner;
                        }
                        if (withinThresholdZ)
                        {
                            if (corner.transform.position.z < targetPosition.z)
                                corner1 = corner;
                            else if (corner.transform.position.z > targetPosition.z)
                                corner2 = corner;
                        }
                    }
                }
                if (corner1 != null && corner2 != null)
                {
                    DestroyExistingChild(houseTarget, "Wall (" + corner1.name + corner2.name + ")");
                    string openingName = "Opening (" + corner1.name + corner2.name + ")";
                    DestroyExistingChild(houseTarget, openingName);
                    GameObject opening = Instantiate(target, houseTarget.transform);
                    opening.name = openingName;
                    opening.transform.localPosition = relativePosition;
                    opening.transform.parent = houseTarget.transform;
                    opening.transform.rotation = Quaternion.identity;
                    CreateOpeningWalls(corner1, target);
                    CreateOpeningWalls(target, corner2);
                }
                else
                {
                    Debug.LogWarning("No suitable corners found for placing the target in line.");
                }
            }
            else
            {
                Debug.LogWarning("Target " + target.name + " is not found or not being tracked.");
            }
        }
    }

    void CreateOpeningWalls(GameObject corner1, GameObject corner2)
    {
        Vector3 wallPosition = (corner1.transform.position + corner2.transform.position) / 2f;
        Vector3 origin = Vector3.zero;
        foreach (GameObject target in cornerTargets)
        {
            origin += target.transform.position;
        }
        origin /= cornerTargets.Length;
        Vector3 relativeWallPosition = wallPosition - origin;
        float distanceX = Mathf.Abs(corner2.transform.position.x - corner1.transform.position.x);
        float distanceZ = Mathf.Abs(corner2.transform.position.z - corner1.transform.position.z);
        if (!WallExistsAtPosition(wallPosition))
        {
            string wallName = "Wall (" + corner1.name + corner2.name + ")";
            DestroyExistingChild(houseTarget, wallName);
            GameObject wall = Instantiate(wallPrefab, houseTarget.transform);
            wall.name = wallName;
            wall.transform.localPosition = relativeWallPosition;
            Vector3 scale = Vector3.zero;
            if (distanceX <= distanceThreshold)
            {
                scale = new Vector3(distanceX, 0.2487f, 0.2487f);
                wall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else if (distanceZ <= distanceThreshold)
            {
                scale = new Vector3(distanceZ, 0.2487f, 0.2487f);
                wall.transform.rotation = Quaternion.identity;
            }
            wall.transform.localScale = scale;
            wall.transform.parent = houseTarget.transform;
        }
    }

    void CreateFurniture()
    {
        foreach (GameObject furniture in furnitureTargets)
        {
            if (furniture.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
            {
                Vector3 furniturePosition = furniture.transform.position;
                Vector3 origin = Vector3.zero;
                foreach (GameObject corner in cornerTargets)
                {
                    origin += corner.transform.position;
                }
                origin /= cornerTargets.Length;
                Vector3 relativePosition = furniturePosition - origin;
                if (IsWithinHouseBounds(furniturePosition))
                {
                    string furnitureName = "Furniture (" + furniture.name + ")";
                    DestroyExistingChild(houseTarget, furnitureName);
                    GameObject placedFurniture = Instantiate(furniture, houseTarget.transform);
                    placedFurniture.name = furnitureName;
                    placedFurniture.transform.localPosition = relativePosition;
                    placedFurniture.transform.parent = houseTarget.transform;
                    placedFurniture.transform.rotation = houseTarget.transform.rotation;
                }
                else
                {
                    Debug.LogWarning("Furniture " + furniture.name + " is not within the bounds of the house.");
                }
            }
            else
            {
                Debug.LogWarning("Furniture " + furniture.name + " is not found or not being tracked.");
            }
        }
    }

    bool IsWithinHouseBounds(Vector3 position)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        foreach (GameObject corner in cornerTargets)
        {
            Vector3 cornerPosition = corner.transform.position;
            if (cornerPosition.x < minX)
                minX = cornerPosition.x;
            if (cornerPosition.x > maxX)
                maxX = cornerPosition.x;
            if (cornerPosition.z < minZ)
                minZ = cornerPosition.z;
            if (cornerPosition.z > maxZ)
                maxZ = cornerPosition.z;
        }
        if (position.x >= minX && position.x <= maxX && position.z >= minZ && position.z <= maxZ)
            return true;
        else
            return false;
    }

    void CreateFloor()
    {
        List<Vector3> boundaryPoints = new List<Vector3>();
        foreach (GameObject corner in cornerTargets)
        {
            boundaryPoints.Add(corner.transform.position);
        }
        Vector3 origin = Vector3.zero;
        foreach (Vector3 point in boundaryPoints)
        {
            origin += point;
        }
        origin /= boundaryPoints.Count;
        DestroyExistingChild(houseTarget, "Floor");
        GameObject floorObject = new GameObject("Floor");
        floorObject.transform.SetParent(houseTarget.transform);
        floorObject.transform.localPosition = Vector3.zero;
        List<Vector3> localVertices = new List<Vector3>();
        foreach (Vector3 point in boundaryPoints)
        {
            localVertices.Add(point - origin);
        }
        Mesh mesh = new Mesh();
        Vector3[] vertices = localVertices.ToArray();
        int[] triangles = Triangulate(vertices);
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        mesh.uv = uvs;
        MeshRenderer meshRenderer = floorObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(floorMaterial);
        meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, floorOpacity);
        floorObject.AddComponent<MeshFilter>().mesh = mesh;
        floorObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    int[] Triangulate(Vector3[] vertices)
    {
        List<int> triangles = new List<int>();
        if (vertices.Length < 3)
        {
            Debug.LogWarning("Cannot triangulate less than 3 vertices.");
            return triangles.ToArray();
        }
        for (int i = 1; i < vertices.Length - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        return triangles.ToArray();
    }
}