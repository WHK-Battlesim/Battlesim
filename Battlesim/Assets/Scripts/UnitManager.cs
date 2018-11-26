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
        public GameObject EditorHandle;
        public List<FactionPrefabs> Prefabs = FactionPrefabs.All();

        #endregion Inspector

        #region Private
        
        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;
        private Situation _situation;

        private GameObject _editorHandle;
        private SphereCollider _xAxisCollider;
        private SphereCollider _zAxisCollider;
        private SphereCollider _rotationCollider;
        private SphereCollider _activeHandle;
        private Vector3 _dragStartPosition;

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
            var leftHold = Input.GetMouseButton(0);
            var leftUp = Input.GetMouseButtonUp(0);
            var rightDown = Input.GetMouseButtonDown(1);
            var rightHold = Input.GetMouseButton(1);
            var rightUp = Input.GetMouseButtonUp(1);
            
            var down = leftDown || rightDown;
            var up = leftUp || rightUp;
            var left = leftDown || leftHold || leftUp;
            var right = rightDown || rightHold || rightUp;
            
            if (!(left || right)) return;

            var clickType = down ? 1 : (up ? -1 : 0);
            var mousePosition = Input.mousePosition;

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            var raycastHit = Physics.Raycast(ray, out hit);

            if (EditorMode)
            {
                HandleUserInteractionForEditor(clickType, left, mousePosition, raycastHit, hit);
            }
            else
            {
                HandleUserInteractionForSimulation(clickType, left, mousePosition, raycastHit, hit);
            }
        }

        private void HandleUserInteractionForSimulation(int clickType, bool left, Vector3 mousePosition, bool raycastHit, RaycastHit hit)
        {
            if (left)
            {
                if(raycastHit)
                {
                    _activeAgent = hit.collider.GetComponentInParent<NavMeshAgent>();
                }
            }
            else // right
            {
                if (_activeAgent != null)
                {
                    _activeAgent.SetDestination(hit.point);
                }
            }
        }

        private void HandleUserInteractionForEditor(int clickType, bool left, Vector3 mousePosition, bool raycastHit, RaycastHit hit)
        {
            if (!left) return;

            if (IsUnit(hit.collider))
            {
                if (clickType == 1)
                {
                    ShowHandle(hit.transform);
                }
            }
            else if(IsHandle(hit.collider))
            {
                switch (clickType)
                {
                    case 1:
                        _activeHandle = hit.collider as SphereCollider;
                        _dragStartPosition = _camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _camera.transform.position.z));
                        break;
                    case 0:
                        HandleMoved(mousePosition);
                        break;
                    case -1:
                        _activeHandle = null;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                HideHandle();
            }
        }

        private static bool IsUnit(Component hitCollider)
        {
            return hitCollider is MeshCollider
                   && hitCollider.GetComponent<SkinnedMeshRenderer>() != null;
        }

        private static bool IsHandle(Component hitCollider)
        {
            return hitCollider is SphereCollider
                   && hitCollider.GetComponent<MeshRenderer>() != null
                   && hitCollider.GetComponent<MeshFilter>() != null;
        }

        private void ShowHandle(Transform parent)
        {
            if (_editorHandle == null)
            {
                _editorHandle = Instantiate(EditorHandle);
                _xAxisCollider = _editorHandle.transform.Find("XAxisHandle").gameObject.GetComponent<SphereCollider>();
                _zAxisCollider = _editorHandle.transform.Find("ZAxisHandle").gameObject.GetComponent<SphereCollider>();
                _rotationCollider = _editorHandle.transform.Find("RotationHandle").gameObject.GetComponent<SphereCollider>();
            }

            _editorHandle.transform.SetParent(parent, false);
            _editorHandle.SetActive(true);
        }

        private void HideHandle()
        {
            _editorHandle.SetActive(false);
        }

        private void HandleMoved(Vector3 newMousePosition)
        {
            var draggedTo = _camera.ScreenToWorldPoint(new Vector3(newMousePosition.x, newMousePosition.y, _camera.transform.position.z));

            _editorHandle.transform.position += draggedTo - _dragStartPosition;
            _dragStartPosition = draggedTo;
        }
    }
}