using UnityEngine;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{

    private RectTransform reticle; // The RecTransform of reticle UI element.

    public Rigidbody playerRigidbody;

    public float restingSize;
    public float maxSize;
    public float speed;
    private float currentSize;

    PlayerController controller;

    private void Start()
    {

        reticle = GetComponent<RectTransform>();

    }

    private void Update()
    {

        // Check if player is currently moving and Lerp currentSize to the appropriate value.
        if (IsMoving)
        {
            currentSize = Mathf.Lerp(currentSize, maxSize, Time.deltaTime * speed);
        }
        else
        {
            currentSize = Mathf.Lerp(currentSize, restingSize, Time.deltaTime * speed);
        }

        // Set the reticle's size to the currentSize value.
        reticle.sizeDelta = new Vector2(currentSize, currentSize);

    }

    // Bool to check if player is currently moving.
    bool IsMoving
    {

        get
        {

            // If we have assigned a rigidbody, check if its velocity is not zero. If so, return true.
            //if (playerRigidbody != null)
            //    if (playerRigidbody.velocity.sqrMagnitude != 0)
            //        return true;
            //    else
            //        return false;

            if (Input.GetMouseButton(1))
            {
                return false;
            }

            // If not rigidbody is assigned, check Input axis' instead.
            if (
                Input.GetAxis("Horizontal") != 0 ||
                Input.GetAxis("Vertical") != 0 ||
                //Input.GetMouseButton(1) ||
                Input.GetMouseButton(0)
                    )
                return true;
            else
                return false;

            

        }

    }

}