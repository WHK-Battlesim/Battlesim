using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Input = UnityEngine.Input;
using MeshRenderer = UnityEngine.MeshRenderer;

namespace Assets.Scripts
{
    public class UnitManager : Loadable
    {
        #region Inspector

        public GameObject UnitPrefab;

        #endregion Inspector

        #region Private
        
        private readonly List<NavMeshAgent> _agents = new List<NavMeshAgent>();
        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;

        #endregion Private

        #region Loadable

        public override void Initialize()
        {
            Steps = new List<LoadableStep>()
            {
                new LoadableStep()
                {
                    Name = "Preparing dependecies",
                    ProgressValue = 1,
                    Action = _prepareDependencies
                },
                new LoadableStep()
                {
                    Name = "Spawning units",
                    ProgressValue = 4,
                    Action = _spawnUnits
                }
            };
            EnableType = LoadingDirector.EnableType.WholeGameObject;
            Weight = 10f;
            MaxProgress = Steps.Sum(s => s.ProgressValue);
        }

        #endregion Loadable

        #region Start

        private object _prepareDependencies(object state)
        {
            _mapGenerator = FindObjectOfType<MapGenerator>();
            _camera = FindObjectOfType<Camera>();

            return state;
        }

        private object _spawnUnits(object state)
        {
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

            return state;
        }

        #endregion Start

        private void Update()
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