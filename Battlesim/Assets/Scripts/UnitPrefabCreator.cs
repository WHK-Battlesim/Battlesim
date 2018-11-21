using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts
{
    public class UnitPrefabCreator : EditorWindow
    {
        private GameObject _selection;
        private bool _selectionValid;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Shader _flatShader;
        private string _prefabParentFolder = "Assets/Prefabs";
        private string _prefabFolderName = "Units";
        private string _shaderParentFolder = "Assets/Materials";
        private string _shaderFolderName = "Units";
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

            if (_selection == null || _selection.GetComponent<Animator>() == null)
            {
                _selectionValid = false;
                return;
            }

            _selectionValid = true;
            _skinnedMeshRenderer = _selection.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI ()
        {
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
            _prefabFolderName = EditorGUILayout.TextField("Folder name", _prefabFolderName);
            _overridePrefab = EditorGUILayout.Toggle("Override existing prefab", _overridePrefab);
            EditorGUILayout.LabelField("Shader options");
            _shaderParentFolder = EditorGUILayout.TextField("Parent folder", _shaderParentFolder);
            _shaderFolderName = EditorGUILayout.TextField("Folder name", _shaderFolderName);
            _overrideMaterials = EditorGUILayout.Toggle("Override existing materials", _overrideMaterials);
        }

        private void _replace()
        {
            Debug.Log("Processing " + _selection.name + "...");

            _checkFolder(_prefabParentFolder, _prefabFolderName);
            _checkFolder(_shaderParentFolder, _shaderFolderName);

            var prefabPath = _prefabParentFolder + "/" + _prefabFolderName + "/" + _selection.name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null && !_overridePrefab)
            {
                Debug.Log("Connecting to existing prefab at " + prefabPath + ".");
                PrefabUtility.ConnectGameObjectToPrefab(_selection, prefab);
                PrefabUtility.ResetToPrefabState(_selection);
                return;
            }

            var oldSharedMaterials = _skinnedMeshRenderer.sharedMaterials;
            var newSharedMaterials = new Material[oldSharedMaterials.Length];
            
            _checkFolder(_shaderParentFolder, _shaderFolderName);
            var folderPath = _shaderParentFolder + "/" + _shaderFolderName;

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

            _skinnedMeshRenderer.sharedMaterials = newSharedMaterials;

            if (prefab == null)
            {
                Debug.Log("Creating new prefab at " + prefabPath + ".");
                prefab = PrefabUtility.CreatePrefab(prefabPath, _selection);
            }
            else
            {
                Debug.Log("Replacing existing prefab at " + prefabPath + ".");
            }

            if (_selection.GetComponent<NavMeshAgent>() == null)
            {
                _selection.AddComponent<NavMeshAgent>();
            }

            if (_selection.GetComponent<Unit>() == null)
            {
                _selection.AddComponent<Unit>();
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
