using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Input = UnityEngine.Input;
using MeshRenderer = UnityEngine.MeshRenderer;
using Random = System.Random;

namespace Assets.Scripts
{
    public class UnitManager : Loadable
    {
        #region Inspector

        public TextAsset DefaultSituation;
        public bool EditorMode;
        public GameObject EditorSelectionMarker;
        public int MinMsUntilRepath = 50;
        public int MaxMsUntilRepath = 100;
        public List<FactionPrefabs> Prefabs = FactionPrefabs.All();

        #endregion Inspector

        [HideInInspector] public bool Running;

        #region Private

        private NavMeshAgent _activeAgent;
        private MapGenerator _mapGenerator;
        private Camera _camera;
        private Situation _situation;
        private Dictionary<Faction, Dictionary<Class, GameObject>> _prefabDict;
        private Transform _unitWrapper;
        
        private Transform _selection;
        private GameObject _editorSelectionMarker;
        private Quaternion _startRotation;
        private Vector2 _startMousePosition;
        private GameObject _activeUnitPrefab;

        #endregion Private

        #region HelperClasses

        [Serializable]
        public class FactionPrefabs
        {
            public static List<FactionPrefabs> All()
            {
                return ((Faction[])Enum.GetValues(typeof(Faction))).Select(faction => new FactionPrefabs(faction)).ToList();
            }

            private FactionPrefabs(Faction faction)
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
                return ((Class[])Enum.GetValues(typeof(Class))).Select(@class => new ClassPrefab(@class)).ToList();
            }

            private ClassPrefab(Class @class)
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

            var situation = Resources.Load<TextAsset>("Maps/" + CurrentMap.Subfolder + "/situations/" + (CurrentMap.Situation == 0 ? "historic" : "custom" + CurrentMap.Situation));
            _situation = JsonUtility.FromJson<Situation>(situation.text);

            return state;
        }

        private object _spawnUnits(object state)
        {
            var navMeshBounds = _mapGenerator.GetComponentInChildren<MeshRenderer>().bounds;

            // create copies of prefabs to avoid changing the actual prefabs
            var prefabWrapepr = transform.Find("Prefabs");
            _prefabDict = new Dictionary<Faction, Dictionary<Class, GameObject>>();
            for (var faction = 0; faction < Prefabs.Count; faction++)
            {
                var factionPrefabs = Prefabs[faction];
                var factionDict = new Dictionary<Class, GameObject>();
                for (var unitClass = 0; unitClass < factionPrefabs.Prefabs.Count; unitClass++)
                {
                    var classPrefab = factionPrefabs.Prefabs[unitClass];
                    factionDict.Add((Class) unitClass, Instantiate(classPrefab.Prefab, prefabWrapepr));
                }
                _prefabDict.Add((Faction) faction, factionDict);
            }

            var random = new Random();
            foreach (var faction in _prefabDict.Values)
            {
                foreach (var unitClass in faction)
                {
                    var unit = unitClass.Value.GetComponent<Unit>();
                    if (!EditorMode) continue;
                    // remove all unnecessary
                    unit.enabled = false;
                    unitClass.Value.GetComponent<NavMeshAgent>().enabled = false;
                    Destroy(unitClass.Value.GetComponent<Animator>());
                }
            }

            foreach (var stat in _situation.Stats)
            {
                stat.ApplyTo(_prefabDict[stat.Faction][stat.Class]);
            }

            _unitWrapper = transform.Find("Units");
            foreach (var unit in _situation.Units)
            {
                var instance = Instantiate(
                    _prefabDict[unit.Faction][unit.Class],
                    _mapGenerator.RealWorldToUnity(unit.Position),
                    Quaternion.AngleAxis(unit.Rotation, Vector3.up),
                    _unitWrapper);
                
                if (EditorMode) continue;

                var unitInstance = instance.GetComponent<Unit>();
                unitInstance.Bucket = _mapGenerator.GetBucket(instance.transform.position);
                unitInstance.Bucket.Enter(unitInstance);
                unitInstance.MapGenerator = _mapGenerator;
                unitInstance.UnitManager = this;
                unitInstance.Random = random;
                unitInstance.MinMsUntilRepath = MinMsUntilRepath;
                unitInstance.MaxMsUntilRepath = MaxMsUntilRepath;
                unitInstance.RandomizeRepathTime();
            }

            return state;
        }

        #endregion Start

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("Main Menu");

            if (EditorMode && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var leftDown = Input.GetMouseButtonDown(0);
            var rightDown = Input.GetMouseButtonDown(1);
            var rightHold = Input.GetMouseButton(1);
            var middleDown = Input.GetMouseButtonDown(2);
            var middleHold = Input.GetMouseButton(2);
            var delDown = Input.GetKeyDown(KeyCode.Delete);
            var spaceDown = Input.GetKeyDown(KeyCode.Space);
            
            if (!(leftDown || rightHold || rightDown || middleDown || middleHold || delDown || spaceDown)) return;

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
                    else if(terrainRaycastHit)
                    {
                        _selection = Instantiate(
                            _activeUnitPrefab,
                            terrainHit.point,
                            Quaternion.identity,
                            _unitWrapper).transform;
                        ShowHandle(_selection);
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
                else if (delDown)
                {
                    if (_selection == null) return;

                    Destroy(_selection.gameObject);
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
                        _activeAgent.SetDestination(terrainHit.point);
                    }
                } else if(spaceDown)
                {
                    Running = !Running;
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

        public void SetActivePrefab(Faction faction, Class @class)
        {
            _activeUnitPrefab = _prefabDict[faction][@class];
        }

        public void SaveCurrentSituation(string map, string situation)
        {
            var result = Situation.FromCurrentScene(_unitWrapper, _prefabDict, _mapGenerator.UnityToRealWorld);

            // TODO: for now, saving is only supported if running in editor
            #if UNITY_EDITOR
            var writer = new StreamWriter("Assets/Resources/Maps/" + map + "/Situations/" + situation + ".json");
            writer.Write(JsonUtility.ToJson(result));
            writer.Close();
            #endif
        }
    }
}