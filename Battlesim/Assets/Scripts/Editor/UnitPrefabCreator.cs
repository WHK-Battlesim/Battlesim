using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Editor
{
    public class UnitPrefabCreator : EditorWindow
    {
        private enum ModelType
        {
            Unit,
            House,
            Tree
        }

        private GameObject _selection;
        private bool _selectionValid;
        private Renderer _meshRenderer;
        private Shader _flatShader;
        private ModelType _modelType = ModelType.Unit;
        private string _prefabParentFolder = "Assets/Prefabs";
        private string _shaderParentFolder = "Assets/Materials";
        private bool _overridePrefab;
        private bool _overrideMaterials;

        [MenuItem("Window/Unit Prefab Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(UnitPrefabCreator)) as UnitPrefabCreator;
            if (window == null) return;
            var titleContent = EditorGUIUtility.IconContent("PrefabNormal Icon");
            titleContent.text = "Unit Prefab";
            window.titleContent = titleContent;
        }

        private void OnEnable()
        {
            _flatShader = Shader.Find("Standard (Flat Lighting)");
            _checkSelection();
        }
        
        private void OnSelectionChange() => _checkSelection();

        private void _checkSelection()
        {
            _selection = Selection.activeGameObject;

            switch (_modelType)
            {
                case ModelType.Unit:
                    _checkUnit();
                    break;
                case ModelType.House:
                case ModelType.Tree:
                    _checkStaticModel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_selectionValid)
            {
                _meshRenderer = _selection.GetComponentInChildren<Renderer>();
            }
        }

        private void _checkUnit()
        {
            _selectionValid = (_selection != null &&
                               _selection.GetComponent<Animator>() != null &&
                               _selection.GetComponentInChildren<SkinnedMeshRenderer>() != null);
        }

        private void _checkStaticModel()
        {
            _selectionValid = (_selection != null &&
                               _selection.GetComponentInChildren<MeshRenderer>() != null);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI ()
        {
            EditorGUI.BeginChangeCheck();
            _modelType = (ModelType)EditorGUILayout.EnumPopup("Model Type", _modelType);
            if (EditorGUI.EndChangeCheck())
            {
                _checkSelection();
            }

            EditorGUILayout.LabelField(_selectionValid ? "Selection is valid." : "Selection is invalid.");

            EditorGUI.BeginDisabledGroup(!_selectionValid);
            if (GUILayout.Button("Create prefab"))
            {
                _replace();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Prefab options");
            _prefabParentFolder = EditorGUILayout.TextField("Parent folder", _prefabParentFolder);
            _overridePrefab = EditorGUILayout.Toggle("Override existing prefab", _overridePrefab);
            EditorGUILayout.LabelField("Shader options");
            _shaderParentFolder = EditorGUILayout.TextField("Parent folder", _shaderParentFolder);
            _overrideMaterials = EditorGUILayout.Toggle("Override existing materials", _overrideMaterials);
        }

        private void _replace()
        {
            Debug.Log("Processing " + _selection.name + "...");

            var folderName = Enum.GetName(typeof(ModelType), _modelType) + "s";

            _checkFolder(_prefabParentFolder, folderName);
            _checkFolder(_shaderParentFolder, folderName);

            var prefabPath = _prefabParentFolder + "/" + folderName + "/" + _selection.name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null && !_overridePrefab)
            {
                Debug.Log("Connecting to existing prefab at " + prefabPath + ".");
                PrefabUtility.ConnectGameObjectToPrefab(_selection, prefab);
                PrefabUtility.ResetToPrefabState(_selection);
                return;
            }

            var oldSharedMaterials = _meshRenderer.sharedMaterials;
            var newSharedMaterials = new Material[oldSharedMaterials.Length];
            
            _checkFolder(_shaderParentFolder, folderName);
            var folderPath = _shaderParentFolder + "/" + folderName;

            for (var i = 0; i < oldSharedMaterials.Length; i++)
            {
                var oldMaterial = oldSharedMaterials[i];
                var materialPath = folderPath + "/" + oldMaterial.name + ".mat";
                var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (newMaterial != null && !_overrideMaterials)
                {
                    Debug.Log("Reusing exiting material at " + materialPath + ".");
                }
                else
                {
                    Debug.Log(newMaterial != null ? "Replacing existing material at " + materialPath + "." : "Creating new material at " + materialPath + ".");

                    newMaterial = new Material(oldMaterial) {shader = _flatShader};
                    AssetDatabase.CreateAsset(newMaterial, materialPath);
                }

                newSharedMaterials[i] = newMaterial;
            }

            _meshRenderer.sharedMaterials = newSharedMaterials;

            if (prefab == null)
            {
                Debug.Log("Creating new prefab at " + prefabPath + ".");
                prefab = PrefabUtility.CreatePrefab(prefabPath, _selection);
            }
            else
            {
                Debug.Log("Replacing existing prefab at " + prefabPath + ".");
            }

            if (_modelType == ModelType.Unit)
            {
                if (_selection.GetComponent<NavMeshAgent>() == null)
                {
                    _selection.AddComponent<NavMeshAgent>();
                }

                if (_selection.GetComponent<Unit>() == null)
                {
                    _selection.AddComponent<Unit>();
                }
            }

            PrefabUtility.ReplacePrefab(_selection, prefab, ReplacePrefabOptions.ConnectToPrefab);
        }

        private static void _checkFolder(string parentFolder, string folderName)
        {
            var folderPath = parentFolder + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
    }
}
