using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Input = UnityEngine.Input;
using MeshRenderer = UnityEngine.MeshRenderer;

namespace Assets.Scripts
{
    public class UnitManager : MonoBehaviour
    {
        public GameObject UnitPrefab;

        private readonly List<NavMeshAgent> _agents = new List<NavMeshAgent>();
        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;

        void Start()
        {
            _mapGenerator = FindObjectOfType<MapGenerator>();
            _camera = FindObjectOfType<Camera>();

            var navMeshBounds = _mapGenerator.GetComponentInChildren<MeshRenderer>().bounds;

            for (var z = -5; z < 5; z++)
            {
                for (var x = -5; x < 5; x++)
                {
                    var xCoord = navMeshBounds.center.x + x;
                    var zCoord = navMeshBounds.center.z + z;
                    _agents.Add(Instantiate(UnitPrefab, new Vector3(xCoord, _mapGenerator.GetMapHeight(xCoord, zCoord), zCoord), Quaternion.identity, transform)
                        .GetComponent<NavMeshAgent>());
                }
            }
        }

        void Update()
        {
            var left = Input.GetMouseButtonDown(0);
            var right = Input.GetMouseButtonDown(1);

            if (!(left || right)) return;
            
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit)) return;

            if (left)
            {
                _activeAgent = hit.collider.GetComponentInParent<NavMeshAgent>();
            }
            else // right
            {
                if (_activeAgent != null)
                {
                    _activeAgent.SetDestination(hit.point);
                }
            }
        }
    }
}