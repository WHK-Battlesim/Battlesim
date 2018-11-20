using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.AI;
using Debug = System.Diagnostics.Debug;
using Object = System.Object;

namespace Assets.Scripts
{
    public class MapGenerator : Loadable
    {
        #region Inspector

        public Texture2D DefaultHeightMap;
        public Texture2D DefaultFeatureMap;
        public TextAsset DefaultExtents;
        public Material Material;

        public Vector3 Offset = Vector3.zero;
        public Vector3 Scale = new Vector3(0.01f, 0.1f, 0.01f);
        public int NumberOfVertices = 50000;

        public List<TerrainFeature> TerrainFeatures = new List<TerrainFeature>()
        {
            new TerrainFeature()
            {
                Name = "Land",
                FeatureMapColor = Color.white,
                MeshColor = new Color(0.516f, 0.867f, 0.279f)
            },
            new TerrainFeature()
            {
                Name = "River",
                FeatureMapColor = Color.green,
                MeshColor = new Color(0.2f, 0.2f, 0.8f)
            },
            new TerrainFeature()
            {
                Name = "Lake",
                FeatureMapColor = Color.red,
                MeshColor = new Color(0.2f, 0.2f, 0.8f)
            },
            new TerrainFeature()
            {
                Name = "Fallback",
                FeatureMapColor = Color.magenta,
                MeshColor = Color.magenta
            },
        };

        #endregion Inspector

        #region Private
        
        private Texture2D _heightMap;
        private Texture2D _featureMap;
        private Extents _extents;

        #endregion Private
        
        #region Helper Classes

        [Serializable]
        public class Extents
        {
            public static implicit operator Extents(TextAsset extentJson)
            {
                return JsonUtility.FromJson<Extents>(extentJson.text);
            }
            
            public double MinX;
            public double MaxX;
            public double MinY;
            public double MaxY;

            public Vector3 Scale => new Vector3((float) (MaxX - MinX), 1, (float) (MaxY - MinY));
        }

        [Serializable]
        public class TerrainFeature
        {
            [HideInInspector]
            public string Name;
            public Color FeatureMapColor;
            public Color MeshColor;
        }

        #endregion Helper Classes

        #region Loadable

        public override void Initialize()
        {
            Steps = new List<LoadableStep>()
            {
                new LoadableStep()
                {
                    Name = "Preparing dependencies",
                    ProgressValue = 1,
                    Action = _prepareDependencies
                },
                new LoadableStep()
                {
                    Name = "Randomizing terrain",
                    ProgressValue = 2,
                    Action = _randomizeTerrain
                },
                new LoadableStep()
                {
                    Name = "Triangulating terrain",
                    ProgressValue = 4,
                    Action = _triangulateTerrain
                },
                new LoadableStep()
                {
                    Name = "Building terrain mesh",
                    ProgressValue = 2,
                    Action = _buildTerrainMesh
                },
                new LoadableStep()
                {
                    Name = "Building navmesh",
                    ProgressValue = 10,
                    Action = _buildNavmesh
                }
            };
            EnableType = LoadingDirector.EnableType.WholeGameObject;
            Weight = 100f;
            MaxProgress = Steps.Sum(s => s.ProgressValue);
        }

        #endregion Loadable

        #region Start

        private class SetupState
        {
            public GameObject Terrain;
            public Mesh Mesh;
            public Polygon Polygon;
            public MeshRenderer MeshRenderer;
            public IMesh TriangulatedMesh;
        }

        private object _prepareDependencies(object state)
        {
            var setupState = new SetupState();

            // TODO: read these from static storage (will be set by map select screen)
            _heightMap = DefaultHeightMap;
            _featureMap = DefaultFeatureMap;
            _extents = DefaultExtents;

            setupState.Terrain = new GameObject("Terrain");
            setupState.Terrain.transform.SetParent(transform);

            var meshFilter = setupState.Terrain.AddComponent<MeshFilter>();
            setupState.MeshRenderer = setupState.Terrain.AddComponent<MeshRenderer>();

            setupState.Mesh = meshFilter.mesh;
            setupState.Mesh.Clear();

            return setupState;
        }

        private object _randomizeTerrain(object state)
        {
            var setupState = state as SetupState;
            Debug.Assert(setupState != null, nameof(setupState) + " != null");

            setupState.Polygon = new Polygon();
            var random = new System.Random();
            for (var i = 0; i < NumberOfVertices; i++)
            {
                setupState.Polygon.Add(new Vertex(random.NextDouble(), random.NextDouble()));
            }

            return setupState;
        }

        private object _triangulateTerrain(object state)
        {
            var setupState = state as SetupState;
            Debug.Assert(setupState != null, nameof(setupState) + " != null");

            var options = new ConstraintOptions { ConformingDelaunay = true };
            setupState.TriangulatedMesh = setupState.Polygon.Triangulate(options);

            return setupState;
        }

        private object _buildTerrainMesh(object state)
        {
            var setupState = state as SetupState;
            Debug.Assert(setupState != null, nameof(setupState) + " != null");

            setupState.Mesh.SetVertices(
                setupState.TriangulatedMesh
                    .Vertices
                    .Select(
                        vertex =>
                            Offset +
                            Vector3.Scale(
                                new Vector3(
                                    (float)vertex.X,
                                    _heightMap.GetPixelBilinear((float)vertex.X, (float)vertex.Y).r * 255,
                                    (float)vertex.Y),
                                Vector3.Scale(_extents.Scale, Scale)))
                    .ToList());

            var triangleGroupVertices =
                setupState.TriangulatedMesh
                    .Triangles
                    .GroupBy(
                        GetFeatureId)
                    .Select(
                        grouping => new Tuple<int, IEnumerable<int>>(
                            grouping.Key,
                            grouping.SelectMany(
                                triangle => triangle.vertices.Reverse(),
                                (triangle, vertex) => vertex.ID)))
                    .ToList();

            var triangleGroupCount = triangleGroupVertices.Count();
            setupState.Mesh.subMeshCount = triangleGroupCount;

            var materials = new Material[triangleGroupCount];
            foreach (var vertexGroup in triangleGroupVertices)
            {
                setupState.Mesh.SetTriangles(vertexGroup.Item2.ToArray(), vertexGroup.Item1);

                materials[vertexGroup.Item1] = new UnityEngine.Material(Material)
                {
                    color = TerrainFeatures[vertexGroup.Item1].MeshColor
                };
            }

            setupState.MeshRenderer.materials = materials;
            setupState.Mesh.RecalculateNormals();

            return setupState;
        }

        private object _buildNavmesh(object state)
        {
            var setupState = state as SetupState;
            Debug.Assert(setupState != null, nameof(setupState) + " != null");

            var meshCollider = setupState.Terrain.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = setupState.Mesh;
            
            var navMesh = GetComponent<NavMeshSurface>();
            navMesh.BuildNavMesh();

            return setupState;
        }

        #endregion Start

        private int GetFeatureId(Triangle triangle)
        {
            var avgX = (triangle.vertices[0].X + triangle.vertices[1].X + triangle.vertices[2].X) / 3;
            var avgY = (triangle.vertices[0].Y + triangle.vertices[1].Y + triangle.vertices[2].Y) / 3;
            var color = _featureMap.GetPixel((int)Math.Round(_featureMap.width * avgX),
                                             (int)Math.Round(_featureMap.height * avgY));
            var index = TerrainFeatures.FindIndex(terrainFeature => terrainFeature.FeatureMapColor == color) % TerrainFeatures.Count;
            return index;
        }

        public float GetMapHeight(float x, float z)
        {
            var texturePosX = (x - Offset.x) / Scale.x / _extents.Scale.x;
            var texturePosY = (z - Offset.z) / Scale.z / _extents.Scale.z;
            return _heightMap.GetPixelBilinear(texturePosX, texturePosY).r * 255 * _extents.Scale.y * Scale.y + Offset.y;
        }
    }
}
