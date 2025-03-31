using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class SnapMove : MonoBehaviour
{
    List<InputDevice> inputDevices;
    List<GameObject> obstructions = new List<GameObject>();
    InputDeviceRole deviceRole = InputDeviceRole.LeftHanded;
    InputFeatureUsage<Vector2> inputFeatureAxis = CommonUsages.primary2DAxis;
    float time = 0;
    float lastTime = 0;
    //bool obstructed = false;
    [SerializeField] GameObject[] blackList;
    [SerializeField] string[] blackListTag;
    Vector2 axisValue;
    Camera mainCamera;
    void Awake()
    {
        inputDevices = new List<InputDevice>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Moves the XR Origin forward if the joystick of the left controller is pointing forward
        if (mainCamera.isActiveAndEnabled)
        {
            Vector3 forward = Camera.main.transform.forward + Camera.main.transform.position;
            gameObject.transform.LookAt(new Vector3(forward.x, gameObject.transform.position.y, forward.z));
            time += Time.deltaTime;

            // Gets the left hand controller
            InputDeviceCharacteristics deviceCharacteristics = (InputDeviceCharacteristics)deviceRole;
            InputDevices.GetDevicesWithCharacteristics(deviceCharacteristics, inputDevices);

            for (int i = 0; i < inputDevices.Count; i++)
            {
                if (inputDevices[i].TryGetFeatureValue(inputFeatureAxis, out axisValue))
                {
                    // Checks if the joystic is pointing forward and if enough time passed from the last movement
                    if (axisValue.y > 0.6 && (time - lastTime) > 0.5 && obstructions.Count == 0)
                    {
                        GameObject origin = GameObject.Find("Complete XR Origin Set Up");
                        lastTime = time = 0;
                        Vector3 cameraF = Camera.main.transform.forward;
                        origin.transform.position = origin.transform.position + new Vector3(cameraF.x, origin.transform.position.y, cameraF.z);
                    }
                }
            }
        }
    }

    // Checks if there are any object or person in front of the user to block it from moving through them
    private void OnTriggerEnter(Collider other)
    {
        bool notBlackListed = true;
        foreach (GameObject obj in blackList)
        {
            if (obj == other.gameObject)
            {
                notBlackListed = false;
            }
        }
        foreach (string tag in blackListTag)
        {
            if (tag == other.tag)
            {
                notBlackListed = false;
            }
        }
        if (other.isTrigger) { notBlackListed = false; }
        if (notBlackListed && other.gameObject.tag != "Target")
        {
            if (!obstructions.Contains(other.gameObject))
            {
                obstructions.Add(other.gameObject);
            }
            //obstructed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (obstructions.Contains(other.gameObject))
        {
            obstructions.Remove(other.gameObject);
            if(obstructions.Count == 0)
            {
                //obstructed = false;
            }
        }
    }
}
