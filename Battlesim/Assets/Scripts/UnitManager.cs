using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Input = UnityEngine.Input;
using MeshRenderer = UnityEngine.MeshRenderer;

namespace Assets.Scripts
{
    public class UnitManager : MonoBehaviour
    {
        public MapGenerator MapGenerator;
        public GameObject UnitPrefab;
        public Camera Camera;

        private readonly List<NavMeshAgent> _agents = new List<NavMeshAgent>();
        private NavMeshAgent _activeAgent;

        void Start()
        {
            var navMeshBounds = FindObjectOfType<MapGenerator>().GetComponentInChildren<MeshRenderer>().bounds;

            for (var z = -5; z < 5; z++)
            {
                for (var x = -5; x < 5; x++)
                {
                    var xCoord = navMeshBounds.center.x + x;
                    var zCoord = navMeshBounds.center.z + z;
                    _agents.Add(Instantiate(UnitPrefab, new Vector3(xCoord, MapGenerator.GetMapHeight(xCoord, zCoord), zCoord), Quaternion.identity, transform)
                        .GetComponent<NavMeshAgent>());
                }
            }
        }

        void Update()
        {
            var left = Input.GetMouseButtonDown(0);
            var right = Input.GetMouseButtonDown(1);

            if (!(left || right)) return;
            
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit)) return;

            if (left)
            {
                _activeAgent = hit.collider.GetComponent<NavMeshAgent>();
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