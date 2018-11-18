using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [Header("Height")]
        public float HeightOffset = 10f;
        public float TerrainHeightFactor = 1.0f;
        [Header("Position")]
        public Vector3 ViewTarget = new Vector3(0, 0, 0);
        public float MovementSpeed = 20.0f;
        [Header("Elevation")]
        public float ElevationAngle = 45f;
        public float ElevationSpeed = 15f;
        public float MinElevation = 10f;
        public float MaxElevation = 90f;
        [Header("Rotation")]
        public float Rotation = 45f;
        public float RotationSpeed = 40f;
        [Header("Zoom")]
        public float Distance = 50f;
        public float ZoomSpeed = 15f;
        public float MinDistance = 3f;
        public float MaxDistance = 200f;
        
        private MapGenerator _mapGenerator;

        void Start()
        {
            _mapGenerator = FindObjectOfType<MapGenerator>();
            var navMeshBounds = _mapGenerator.GetComponentInChildren<MeshRenderer>().bounds;
            ViewTarget = navMeshBounds.center;
            _moveCamera();
        }
        
        void Update()
        {
            var xFactor = Input.GetAxis("Horizontal");
            var yFactor = Input.GetAxis("Vertical");
            var rotationFactor = Input.GetAxis("Rotation");
            var zoomFactor = Input.GetAxis("Zoom");
            var elevationFactor = Input.GetAxis("Elevation");

            // if no input, don't move cam
            if (Mathf.Approximately(xFactor, 0.0f) && Mathf.Approximately(yFactor, 0.0f) &&
                Mathf.Approximately(rotationFactor, 0.0f) && Mathf.Approximately(zoomFactor, 0.0f) &&
                Mathf.Approximately(elevationFactor, 0.0f))
            {
                return;
            }

            var movementAmplifier = (float) Math.Log(Distance);
            var rotation = Quaternion.AngleAxis(Rotation, Vector3.up);
            ViewTarget += rotation * Vector3.right * xFactor * MovementSpeed * Time.deltaTime * movementAmplifier;
            ViewTarget += rotation * Vector3.forward * yFactor * MovementSpeed * Time.deltaTime * movementAmplifier;
            ElevationAngle = Mathf.Clamp(ElevationAngle + elevationFactor * ElevationSpeed * Time.deltaTime, MinElevation, MaxElevation);
            Rotation = (Rotation + rotationFactor * RotationSpeed * Time.deltaTime) % 360f;
            Distance = Mathf.Clamp(Distance - zoomFactor * ZoomSpeed * Time.deltaTime * movementAmplifier, MinDistance, MaxDistance);

            _moveCamera();
        }

        private void _moveCamera()
        {
            ViewTarget.y = Mathf.Lerp(HeightOffset, _mapGenerator.GetMapHeight(ViewTarget.x, ViewTarget.z), TerrainHeightFactor);
            var targetToCamera = Quaternion.Euler(ElevationAngle, Rotation, 0) * Vector3.forward * Distance;

            transform.position = ViewTarget - targetToCamera;
            transform.LookAt(ViewTarget, Vector3.up);
        }
    }
}
