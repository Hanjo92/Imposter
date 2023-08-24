using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    private Vector3 lookCameraDirection => (targetCamera.transform.position - transform.position).normalized;
    [SerializeField] Vector3 faceEuler = new Vector3(0f, 180f, 0f);

	[SerializeField] private bool snapHorizontal = false;
    [SerializeField] private int snapHorizontalCount = 8;
	[SerializeField] private Vector2 yAngleRange = new Vector2(-80f, 80f);

    [SerializeField] private bool snapVertical = false;
	[SerializeField] private int snapVerticalCount = 4;
    [SerializeField] private Vector2 xAngleRange = new Vector2(-90f, 90f);

	private void Start()
    {
        if(targetCamera == null) targetCamera = Camera.main;
    }


    private void LateUpdate()
    {
        var lookRotation = Quaternion.LookRotation(lookCameraDirection) * Quaternion.Euler(faceEuler);
        var euler = lookRotation.eulerAngles;
        euler.y = ConvertAngleTo180Range(euler.y);
        euler.y = Mathf.Clamp(euler.y, yAngleRange.x, yAngleRange.y);
        if(snapHorizontal)
        {
            var snapAngle = (int)(yAngleRange.y - yAngleRange.x) / snapHorizontalCount;
            euler.y = Mathf.Round(euler.y / snapAngle) * snapAngle;
		}
		euler.x = ConvertAngleTo180Range(euler.x);
		euler.x = Mathf.Clamp(euler.x, xAngleRange.x, xAngleRange.y);
        if(snapVertical)
        {
			var snapAngle = (int)(xAngleRange.y - xAngleRange.x) / snapVerticalCount;
			euler.x = Mathf.Round(euler.x / snapAngle) * snapAngle;
		}

        lookRotation = Quaternion.Euler(euler);
        transform.rotation = lookRotation;
	}

	private static float ConvertAngleTo360Range(float angle) => angle % 360f;
	private static float ConvertAngleTo180Range(float angle)
    {
        var range360 = ConvertAngleTo360Range(angle);
        if(range360 < 0)
        {
            return (range360 < -180f) ? (360 + range360) : range360;
        }
        return (range360 > 180) ? (range360 -360) : range360;
    }
}
