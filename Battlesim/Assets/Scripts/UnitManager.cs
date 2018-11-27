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
        public GameObject EditorSelectionMarker;
        public List<FactionPrefabs> Prefabs = FactionPrefabs.All();

        #endregion Inspector

        #region Private

        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;
        private Situation _situation;
        
        private Transform _selection;
        private GameObject _editorSelectionMarker;
        private Quaternion _startRotation;
        private Vector2 _startMousePosition;

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
                    Quaternion.AngleAxis(unit.Rotation, Vector3.up),
                    unitsWrapper);
            }

            return state;
        }

        #endregion Start

        private void Update()
        {
            var leftDown = Input.GetMouseButtonDown(0);
            var rightDown = Input.GetMouseButtonDown(1);
            var rightHold = Input.GetMouseButton(1);
            var middleDown = Input.GetMouseButtonDown(2);
            var middleHold = Input.GetMouseButton(2);
            
            if (!(leftDown || rightHold || rightDown || middleDown || middleHold)) return;

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit unitHit;
            var unitRaycastHit = Physics.Raycast(ray, out unitHit, Mathf.Infinity, LayerMask.GetMask("Units"));
            RaycastHit terrainHit;
            var terrainRaycastHit = Physics.Raycast(ray, out terrainHit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

            if (EditorMode)
            {
                if (leftDown)
                {
                    if (unitRaycastHit && IsUnit(unitHit.collider))
                    {
                        _selection = unitHit.transform.parent;
                        ShowHandle(_selection);
                    }
                    else
                    {
                        _selection = null;
                        HideHandle();
                    }
                }
                else if (rightHold)
                {
                    if (_selection == null) return;
                    
                    if (terrainRaycastHit)
                    {
                        _selection.position = terrainHit.point;
                    }
                }
                else if (middleDown)
                {
                    if (_selection == null) return;

                    _startRotation = _selection.rotation;
                    _startMousePosition = Input.mousePosition;
                }
                else if (middleHold)
                {
                    if (_selection == null) return;

                    _selection.rotation = _startRotation * Quaternion.AngleAxis(_startMousePosition.x - Input.mousePosition.x, Vector3.up);
                }
            }
            else
            {
                if (leftDown)
                {
                    _activeAgent = unitRaycastHit ? unitHit.collider.GetComponentInParent<NavMeshAgent>() : null;
                }
                else if(rightDown)
                {
                    if (_activeAgent != null)
                    {
                        _activeAgent.SetDestination(unitHit.point);
                    }
                }
            }
        }

        private static bool IsUnit(Component hitCollider)
        {
            return hitCollider is MeshCollider
                   && hitCollider.GetComponent<SkinnedMeshRenderer>() != null;
        }

        private void ShowHandle(Transform parent)
        {
            if (_editorSelectionMarker == null)
            {
                _editorSelectionMarker = Instantiate(EditorSelectionMarker);
            }

            var markerTransform = _editorSelectionMarker.transform;
            
            markerTransform.SetParent(parent, false);

            var parentRotation = parent.rotation;
            parent.rotation = Quaternion.identity;

            var boundingBox = parent.GetComponentInChildren<Renderer>().bounds;
            markerTransform.position = boundingBox.center;
            var globalScale = boundingBox.extents;
            markerTransform.localScale = Vector3.one;
            markerTransform.localScale = new Vector3(
                globalScale.x / transform.lossyScale.x,
                globalScale.y / transform.lossyScale.y,
                globalScale.z / transform.lossyScale.z);

            parent.rotation = parentRotation;

            _editorSelectionMarker.SetActive(true);
        }

        private void HideHandle()
        {
            if(_editorSelectionMarker != null)
            {
                _editorSelectionMarker.SetActive(false);
            }
        }
    }
}