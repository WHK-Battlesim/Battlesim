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
        public bool EditorMode;
        public List<FactionPrefabs> Prefabs = FactionPrefabs.All();

        #endregion Inspector

        #region Private
        
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

            // create copies of prefabs to avoid changing the actual prefabs
            var prefabsWrapper = transform.Find("Prefabs");
            var prefabDict = new Dictionary<Unit.Faction, Dictionary<Unit.Class, GameObject>>();
            for (var faction = 0; faction < Prefabs.Count; faction++)
            {
                var factionPrefabs = Prefabs[faction];
                var factionDict = new Dictionary<Unit.Class, GameObject>();
                for (var unitClass = 0; unitClass < factionPrefabs.Prefabs.Count; unitClass++)
                {
                    var classPrefab = factionPrefabs.Prefabs[unitClass];
                    factionDict.Add((Unit.Class) unitClass, Instantiate(classPrefab.Prefab, prefabsWrapper));
                }
                prefabDict.Add((Unit.Faction) faction, factionDict);
            }

            foreach (var faction in prefabDict.Values)
            {
                foreach (var unitClass in faction)
                {
                    if(!EditorMode)
                    {
                        unitClass.Value.GetComponent<NavMeshAgent>().agentTypeID =
                            _mapGenerator.NavMeshDictionary[unitClass.Key].agentTypeID;
                    }
                    else
                    {
                        // remove all unnecessary 
                        Destroy(unitClass.Value.GetComponent<Unit>());
                        Destroy(unitClass.Value.GetComponent<Animator>());
                        Destroy(unitClass.Value.GetComponent<NavMeshAgent>());
                    }
                }
            }

            foreach (var stat in _situation.Stats)
            {
                // TODO: read faction-specific stats
                stat.ApplyTo(prefabDict[Unit.Faction.Prussia][stat.Class]);
                stat.ApplyTo(prefabDict[Unit.Faction.Austria][stat.Class]);
            }

            var unitsWrapper = transform.Find("Units");
            foreach (var unit in _situation.Units)
            {
                Instantiate(
                    prefabDict[unit.Faction][unit.Class],
                    _mapGenerator.RealWorldToUnity(unit.Position),
                    Quaternion.identity,
                    unitsWrapper);
            }

            return state;
        }

        #endregion Start

        private void Update()
        {
            var left = Input.GetMouseButtonDown(0);
            var right = Input.GetMouseButtonDown(1);

            if (!(left || right)) return;

            if (EditorMode)
            {
                HandleUserInteractionForEditor(left, right);
            }
            else
            {
                HandleUserInteractionForSimulation(left, right);
            }
        }

        private void HandleUserInteractionForSimulation(bool left, bool right)
        {
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

        private void HandleUserInteractionForEditor(bool left, bool right)
        {
            // TODO
        }
    }
}