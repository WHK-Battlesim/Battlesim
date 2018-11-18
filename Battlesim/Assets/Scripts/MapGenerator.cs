using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using UnityEngine.AI;

namespace Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
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

        private void Start()
        {
            BuildTerrain(DefaultHeightMap, DefaultFeatureMap, DefaultExtents);
        }

        private int GetFeatureId(Triangle triangle)
        {
            var avgX = (triangle.vertices[0].X + triangle.vertices[1].X + triangle.vertices[2].X) / 3;
            var avgY = (triangle.vertices[0].Y + triangle.vertices[1].Y + triangle.vertices[2].Y) / 3;
            var color = _featureMap.GetPixel((int)Math.Round(_featureMap.width * avgX),
                                             (int)Math.Round(_featureMap.height * avgY));
            var index = TerrainFeatures.FindIndex(terrainFeature => terrainFeature.FeatureMapColor == color) % TerrainFeatures.Count;
            return index;
        }

        public void BuildTerrain(Texture2D heightMap, Texture2D featureMap, Extents extents)
        {
            _heightMap = heightMap;
            _featureMap = featureMap;
            _extents = extents;

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(transform);

            var meshFilter = terrain.AddComponent<MeshFilter>();
            var meshRenderer = terrain.AddComponent<MeshRenderer>();

            var mesh = meshFilter.mesh;
            mesh.Clear();

            var polygon = new Polygon();
            var random = new System.Random();
            for (var i = 0; i < NumberOfVertices; i++)
            {
                polygon.Add(new Vertex(random.NextDouble(), random.NextDouble()));
            }

            var options = new ConstraintOptions { ConformingDelaunay = true };
            var triangulatedMesh = polygon.Triangulate(options);

            mesh.SetVertices(
                triangulatedMesh
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
                triangulatedMesh
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
            mesh.subMeshCount = triangleGroupCount;

            var materials = new Material[triangleGroupCount];
            foreach (var vertexGroup in triangleGroupVertices)
            {
                mesh.SetTriangles(vertexGroup.Item2.ToArray(), vertexGroup.Item1);

                materials[vertexGroup.Item1] = new UnityEngine.Material(Material)
                {
                    color = TerrainFeatures[vertexGroup.Item1].MeshColor
                };
            }

            meshRenderer.materials = materials;
            mesh.RecalculateNormals();
            
            var meshCollider = terrain.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            var navMesh = GetComponent<NavMeshSurface>();

            navMesh.BuildNavMesh();
        }

        public float GetMapHeight(float x, float z)
        {
            var texturePosX = (x - Offset.x) / Scale.x / _extents.Scale.x;
            var texturePosY = (z - Offset.z) / Scale.z / _extents.Scale.z;
            return _heightMap.GetPixelBilinear(texturePosX, texturePosY).r * 255 * _extents.Scale.y * Scale.y + Offset.y;
        }
    }
}
