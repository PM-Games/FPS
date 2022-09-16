using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    public Transform blockShootingPoint;
    public GameObject blockPrefab;


    public Material normalColour;
    public Material highlightedColour;

    GameObject lastHighlightedBlock;

    bool canBuild = true;

    void Update()
    {
        if (Input.GetMouseButton(0) && canBuild)
        {
            //BuildBlock(blockPrefab);
            StartCoroutine(nameof(BuildBlockAndWait));
        }
        if (Input.GetMouseButton(1))
        {
            DestroyBlock();
        }
        HighlightBlock();
    }

    IEnumerator BuildBlockAndWait()
    {
        BuildBlock(blockPrefab);
        canBuild = false;
        yield return new WaitForSeconds(0.075f);
        canBuild = true;
    }


    void BuildBlock(GameObject block)
    {
        if (Physics.Raycast(blockShootingPoint.position, blockShootingPoint.forward, out RaycastHit hitInfo, 10)) 
        {
            if (hitInfo.transform.tag == "BuildingBlock")
            {
                Vector3 spawnPosition = new Vector3(Mathf.RoundToInt(hitInfo.point.x + hitInfo.normal.x/2), Mathf.RoundToInt(hitInfo.point.y + hitInfo.normal.y/2), Mathf.RoundToInt(hitInfo.point.z + hitInfo.normal.z/2));
                Instantiate(block, spawnPosition, Quaternion.identity);
            }
            else //if is the ground
            {
                Vector3 spawnPosition = new Vector3(Mathf.RoundToInt(hitInfo.point.x), Mathf.RoundToInt(hitInfo.point.y), Mathf.RoundToInt(hitInfo.point.z));
                Instantiate(block, spawnPosition, Quaternion.identity);
            }
        }
    }

    void DestroyBlock()
    {
        if (Physics.Raycast(blockShootingPoint.position, blockShootingPoint.forward, out RaycastHit hitInfo, 10))
        {
            if (hitInfo.transform.tag == "BuildingBlock")
            {
                Destroy(hitInfo.transform.gameObject);
            }            
        }
    }

    void HighlightBlock()
    {
        if (Physics.Raycast(blockShootingPoint.position, blockShootingPoint.forward, out RaycastHit hitInfo, 10))
        {
            if (hitInfo.transform.tag == "BuildingBlock")
            {
                if (lastHighlightedBlock == null)
                {
                    lastHighlightedBlock = hitInfo.transform.gameObject;
                    hitInfo.transform.gameObject.GetComponent<MeshRenderer>().material = highlightedColour;
                }
                else if (lastHighlightedBlock != hitInfo.transform.gameObject)
                {
                    lastHighlightedBlock.GetComponent<MeshRenderer>().material = normalColour;
                    hitInfo.transform.gameObject.GetComponent<MeshRenderer>().material = highlightedColour;

                    lastHighlightedBlock = hitInfo.transform.gameObject;
                }
            }
        }
    }
}
