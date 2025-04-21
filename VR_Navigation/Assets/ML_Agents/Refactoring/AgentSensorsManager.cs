using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class AgentSensorsManager : MonoBehaviour
{
    public enum SensorName { WallsAndTargets, WallsAndAgents }

    [Serializable]
    public class Sensore
    {
        [SerializeField] public SensorName SensorName;
        [SerializeField] public LayerMask RayLayeredMask;

        public RaycastHit GetRayInfo(Vector3 startingPos, Vector3 rayDirection, float rayLength)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(startingPos + Vector3.up, rayDirection, out hitInfo, rayLength, RayLayeredMask))
            {
                return hitInfo;
            }
            return default(RaycastHit);
        }
    }

    [SerializeField] private List<Sensore> _sensors = new List<Sensore>();
    private int _viewAngle = MyConstants.viewAngle;
    private int _rayLength = MyConstants.rayLength;

    private int numberOfRaysPerSide = MyConstants.numberOfRaysPerSide;

    private Dictionary<Sensore, RaycastHit[]> _sensorsObservations;


    //lasciare gia selezionati i target Generici
    public void UpdateTargetSensorVision(Group agentGroup)
    {
        Sensore sensorWallsAndTargets = _sensors.Single(x => x.SensorName == SensorName.WallsAndTargets);

        String finalTargetLayerName = TargetType.Final.GetLayerName(agentGroup);
        int finalTargetLayer = LayerMask.NameToLayer(finalTargetLayerName);
        sensorWallsAndTargets.RayLayeredMask |= 1 << finalTargetLayer;

        String intermediateTargetLayerName = TargetType.Intermediate.GetLayerName(agentGroup);
        int intermediateTargetLayer = LayerMask.NameToLayer(intermediateTargetLayerName);
        sensorWallsAndTargets.RayLayeredMask |= 1 << intermediateTargetLayer;

    }

    private void Awake()
    {
        _sensorsObservations = new Dictionary<Sensore, RaycastHit[]>();
        _sensors.ForEach(sensor => _sensorsObservations.Add(sensor, null));
    }

    public Dictionary<Sensore, RaycastHit[]> ComputeSensorResults()
    {
        foreach (Sensore sensor in _sensors)
        {
            RaycastHit[] observations = CalculateResultOfSensor(sensor);
            _sensorsObservations[sensor] = observations;
        }

        return _sensorsObservations;
    }

    private RaycastHit[] CalculateResultOfSensor(Sensore sensor)
    {
        RaycastHit[] sensorResults = new RaycastHit[(numberOfRaysPerSide * 2) + 1];
        int currentIndex = 0;
        for (int i = 0; i <= numberOfRaysPerSide; i++)
        {
            foreach (int sign in new int[] { 1, -1 })
            {
                if (i == 0 && sign == -1) { continue; }

                Vector3 rayDirection = CalculateRayDirection(i, sign);
                Vector3 offSettedPosition = transform.position + (rayDirection * MyConstants.rayOffset);

                RaycastHit hit = sensor.GetRayInfo(offSettedPosition, rayDirection, _rayLength);

                if (hit.collider == null)
                {
                    Debug.LogError($"Ray of sensor {sensor.SensorName} returned null!");
                }

                sensorResults[currentIndex] = hit;
                currentIndex++;
            }
        }

        return sensorResults;
    }
    //ALTERANATIVA
    //public Vector3 CalculateRayDirection(int i, int sign)
    //{
    //    float alpha = i == 0 ? 0 : -(_viewAngle / 2f) / Mathf.Pow(2f, numberOfRaysPerSide - i);
    //    alpha = (alpha * sign) - (-transform.eulerAngles.y);
    //    Vector3 rayDirection = new Vector3(Mathf.Sin(Mathf.Deg2Rad * alpha), 0, Mathf.Cos(Mathf.Deg2Rad * alpha));
    //    return rayDirection;
    //}
    public Vector3 CalculateRayDirection(int i, int sign)
    {
        float alpha = Mathf.Min(_viewAngle / 2f, (0.75f * Mathf.Pow(i, 2)) + 0.75f * i);
        alpha = (alpha * sign) - (-transform.eulerAngles.y);
        Vector3 rayDirection = new Vector3(Mathf.Sin(Mathf.Deg2Rad * alpha), 0, Mathf.Cos(Mathf.Deg2Rad * alpha));
        return rayDirection;
    }

}
