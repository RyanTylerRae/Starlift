#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TravelGraphController : MonoBehaviour
{
    public int gridWidth;

    public float circleRadius;

    public float sectorWidth;
    public float offsetPercent;

    public GameObject nodePrefab;

    private List<GameObject> createdNodes = new();
    private List<GameObject> debugBoxNodes = new();

    private int currentNodeIdx = -1;
    private GameObject? finalNode = null;

    public GameObject? playerShip = null;
    public float shipMoveSpeed;
    public float shipRange;

    public GameObject? startMarker = null;
    public GameObject? endMarker = null;

    public float prevSqrDistance = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        createdNodes.Clear();

        float halfWidth = sectorWidth * 0.5f;

        Vector3 circleCenter = new();
        circleCenter.x = (gridWidth / 2) * sectorWidth;
        circleCenter.z = circleCenter.x;

        for (int i = 0; i < gridWidth; ++i)
        {
            for (int j = 0; j < gridWidth; ++j)
            {
                Vector3 nodePos = new();
                nodePos.x = i * sectorWidth;
                nodePos.z = j * sectorWidth;

                if ((nodePos - circleCenter).sqrMagnitude > circleRadius * circleRadius)
                {
                    continue;
                }

                // offset by a random amount within the grid square and give a random height
                nodePos.x += Random.Range(-halfWidth, halfWidth) * offsetPercent;
                nodePos.y += Random.Range(-halfWidth, halfWidth) * offsetPercent;
                nodePos.z += Random.Range(-halfWidth, halfWidth) * offsetPercent;

                // Instantiate the node prefab at the calculated position
                createdNodes.Add(Instantiate(nodePrefab, nodePos, Quaternion.identity));

                debugBoxNodes.Add(DebugBox.DrawBox(nodePos, 3.4f, 0.1f, Color.blue));
                debugBoxNodes.Last().SetActive(false);
            }
        }

        currentNodeIdx = 0;
        finalNode = createdNodes.Last();
        var currentNode = createdNodes[currentNodeIdx];

        if (playerShip != null)
        {
            playerShip.transform.position = currentNode.transform.position;
        }

        if (startMarker != null)
        {
            startMarker.transform.position = currentNode.transform.position;
        }

        if (endMarker != null)
        {
            endMarker.transform.position = finalNode.transform.position;
        }
    }

    // Update is called once per frame
    public void Update()
    {
        var currentNode = createdNodes[currentNodeIdx];

        if (playerShip != null && currentNode != null)
        {
            Vector3 dir = currentNode.transform.position - playerShip.transform.position;
            dir.Normalize();

            if (dir.sqrMagnitude > 0.0f)
            {
                playerShip.transform.position += dir * shipMoveSpeed * Time.deltaTime;
            }

            for (int i = 0; i < debugBoxNodes.Count; ++i)
            {
                var debugBoxObj = debugBoxNodes[i];

                Vector3 vec = debugBoxObj.transform.position - playerShip.transform.position;
                if (vec.sqrMagnitude < shipRange * shipRange && i != currentNodeIdx)
                {
                    debugBoxObj.SetActive(true);
                }
                else
                {
                    debugBoxObj.SetActive(false);
                }
            }
        }

        if (playerShip != null && currentNode != null)
        {
            Vector3 vec = currentNode.transform.position - playerShip.transform.position;

            if (vec.sqrMagnitude < 0.01f & prevSqrDistance > 0.01f)
            {
                OnTraveledToNode();
            }

            prevSqrDistance = vec.sqrMagnitude;
        }
    }

    private void OnTraveledToNode()
    {
        SceneManager.LoadScene("GameEntry");
    }

    public void SetCurrentNode(GameObject newNode)
    {
        var currentNode = createdNodes[currentNodeIdx];

        if (playerShip != null && currentNode != null)
        {
            Vector3 vec = currentNode.transform.position - playerShip.transform.position;
            Vector3 vec2 = newNode.transform.position - playerShip.transform.position;
            bool canTravelTo = vec2.sqrMagnitude < shipRange * shipRange;

            if (System.Math.Abs(vec.sqrMagnitude) < 0.01f && canTravelTo && createdNodes.Contains(newNode))
            {
                currentNodeIdx = createdNodes.IndexOf(newNode);
            }
        }
    }
}
