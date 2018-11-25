using System;
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

        public TextAsset DefaultSituation;
        public List<FactionPrefabs> Prefabs = FactionPrefabs.All();

        #endregion Inspector

        #region Private
        
        private readonly List<NavMeshAgent> _agents = new List<NavMeshAgent>();
        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;
        private Situation _situation;

        #endregion Private

        #region HelperClasses

        [Serializable]
        public class FactionPrefabs
        {
            public static List<FactionPrefabs> All()
            {
                return ((Unit.Faction[])Enum.GetValues(typeof(Unit.Faction))).Select(faction => new FactionPrefabs(faction)).ToList();
            }

            private FactionPrefabs(Unit.Faction faction)
            {
                Name = faction.ToString();
                Prefabs = ClassPrefab.All();
            }

            [HideInInspector]
            public string Name;
            public List<ClassPrefab> Prefabs;
        }

        [Serializable]
        public class ClassPrefab
        {
            public static List<ClassPrefab> All()
            {
                return ((Unit.Class[])Enum.GetValues(typeof(Unit.Class))).Select(@class => new ClassPrefab(@class)).ToList();
            }

            private ClassPrefab(Unit.Class @class)
            {
                Name = @class.ToString();
            }

            [HideInInspector]
            public string Name;
            public GameObject Prefab;
        }

        #endregion HelperClasses

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

            _situation = JsonUtility.FromJson<Situation>(DefaultSituation.text);

            return state;
        }

        private object _spawnUnits(object state)
        {
            var navMeshBounds = _mapGenerator.GetComponentInChildren<MeshRenderer>().bounds;

            foreach (var unitClass in (Unit.Class[]) Enum.GetValues(typeof(Unit.Class)))
            {
                foreach (var prefab in Prefabs)
                {
                    prefab.Prefabs[(int) unitClass].Prefab.GetComponent<NavMeshAgent>().agentTypeID =
                        _mapGenerator.NavMeshDictionary[unitClass].agentTypeID;
                }
            }

            foreach (var stat in _situation.Stats)
            {
                // TODO: read faction-specific stats
                stat.ApplyTo(Prefabs[0].Prefabs[(int) stat.Class].Prefab);
                stat.ApplyTo(Prefabs[1].Prefabs[(int) stat.Class].Prefab);
            }

            foreach (var unit in _situation.Units)
            {
                var unitInstance = Instantiate(
                    Prefabs[(int) unit.Faction].Prefabs[(int) unit.Class].Prefab,
                    _mapGenerator.RealWorldToUnity(unit.Position),
                    Quaternion.identity,
                    transform);
                var navMeshAgent = unitInstance.GetComponent<NavMeshAgent>();
                _agents.Add(navMeshAgent);
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