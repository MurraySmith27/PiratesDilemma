using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CrabController : MonoBehaviour
{
    [SerializeField] public SplineContainer spline;
    [SerializeField] public float speed = 1f;
    private float distancePercentage = 0f;
    private float splineLength;
    [SerializeField] private float increment = 0.05f;

    private void Start(){
        splineLength = spline.CalculateLength();
    }
    void Update(){
        distancePercentage += speed * Time.deltaTime / splineLength;
        Vector3 currentPosition = spline.EvaluatePosition(distancePercentage);
        transform.position = currentPosition;
        if (distancePercentage > 1f) {
            distancePercentage = 0f;
        }
        Vector3 nextPosition = spline.EvaluatePosition(distancePercentage + increment);
        Vector3 direction = nextPosition - currentPosition;
        transform.rotation = Quaternion.LookRotation(direction, transform.up);
    }
}