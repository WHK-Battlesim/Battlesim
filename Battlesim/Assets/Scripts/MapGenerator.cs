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

        public bool EditorMode;

        public Texture2D DefaultHeightMap;
        public Texture2D DefaultFeatureMap;
        public Texture2D DefaultDecorationMap;
        public TextAsset DefaultExtents;
        public Material Material;

        public Vector3 Offset = Vector3.zero;
        public Vector3 Scale = new Vector3(0.01f, 0.1f, 0.01f);
        public int NumberOfVertices = 50000;
        public Vector2Int BucketCount = new Vector2Int(20, 20);

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

        public List<Decoration> Decorations = new List<Decoration>()
        {
            new Decoration()
            {
                Name = "House",
                DecorationMapColor = Color.red
            },
            new Decoration()
            {
                Name = "Tree",
                DecorationMapColor = Color.green
            },
            new Decoration()
            {
                Name = "Stone",
                DecorationMapColor = Color.black
            },
            new Decoration()
            {
                Name = "None",
                DecorationMapColor = Color.white
            }
        };

        #endregion Inspector

        #region Public

        [HideInInspector] public Dictionary<Class, NavMeshSurface> NavMeshDictionary;

        #endregion Public

        #region Private
        
        private Texture2D _heightMap;
        private Texture2D _featureMap;
        private Texture2D _decorationMap;
        private Extents _extents;
        private List<List<Bucket>> _buckets;

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

        [Serializable]
        public class Decoration
        {
            [HideInInspector]
            public string Name;
            public Color DecorationMapColor;
            public float MinScale = 1.0f;
            public float MaxScale = 1.0f;
            public int RotationSteps = 1;
            public int Amount = 0;
            [HideInInspector]
            public int AlreadyPlaced = 0;
            public List<GameObject> Prefabs;
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
                EditorMode ? null : new LoadableStep()
                {
                    Name = "Preparing navmeshes",
                    ProgressValue = 1,
                    Action = _prepareNavmeshes
                },
                EditorMode ? null : new LoadableStep()
                {
                    Name = "Building infantry navmesh",
                    ProgressValue = 10,
                    Action = _buildInfantryNavmesh
                },
                EditorMode ? null : new LoadableStep()
                {
                    Name = "Building cavalry navmesh",
                    ProgressValue = 10,
                    Action = _buildCavalryNavmesh
                },
                EditorMode ? null : new LoadableStep()
                {
                    Name = "Building artillery navmesh",
                    ProgressValue = 10,
                    Action = _buildArtilleryNavmesh
                },
                EditorMode ? null : new LoadableStep()
                {
                    Name = "Filling buckets",
                    ProgressValue = 2,
                    Action = _setUpBuckets
                },
                new LoadableStep()
                {
                    Name = "Adding decoration",
                    ProgressValue = 2,
                    Action = _addDecoration
                },
                !EditorMode ? null : new LoadableStep()
                {
                    Name = "Cleaning up",
                    ProgressValue = 1,
                    Action = _removeNavMesh
                }
            }.Where(s => s != null).ToList();
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
            _decorationMap = DefaultDecorationMap;

            setupState.Terrain = new GameObject("Terrain");
            setupState.Terrain.transform.SetParent(transform);
            setupState.Terrain.layer = LayerMask.NameToLayer("Terrain");

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
                    .Select(vertex => GetPostionForTextureCoord((float) vertex.x, (float) vertex.y))
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

            var meshCollider = setupState.Terrain.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = setupState.Mesh;

            return setupState;
        }

        private object _prepareNavmeshes(object state)
        {
            var setupState = state as SetupState;
            Debug.Assert(setupState != null, nameof(setupState) + " != null");

            NavMeshDictionary = GetComponents<NavMeshSurface>()
                .ToDictionary(navMeshSurface => (Class)Enum.Parse(typeof(Class), NavMesh.GetSettingsNameFromID(navMeshSurface.agentTypeID)));

            return setupState;
        }

        private object _buildInfantryNavmesh(object state)
        {
            NavMeshDictionary[Class.Infantry].BuildNavMesh();

            return state;
        }

        private object _buildCavalryNavmesh(object state)
        {
            NavMeshDictionary[Class.Cavalry].BuildNavMesh();

            return state;
        }

        private object _buildArtilleryNavmesh(object state)
        {
            NavMeshDictionary[Class.Artillery].BuildNavMesh();

            return state;
        }

        private object _removeNavMesh(object state)
        {
            foreach (var navMeshSurface in GetComponents<NavMeshSurface>())
            {
                Destroy(navMeshSurface);
            }

            return state;
        }

        private object _addDecoration(object state)
        {
            var decoWrapper = new GameObject("Decoration").transform;
            decoWrapper.SetParent(transform);

            var random = new System.Random();
            var done = false;

            while(!done)
            {
                // generate some positions in range 0-1 (see _randomizeTerrain)
                var pos = new Vector2((float)random.NextDouble(), (float)random.NextDouble()); // use random.NextDouble() - yeah, you'll have to cast
                                                                                               // read the decoration map and find the correct decoration
                var color = _decorationMap.GetPixel(
                    (int)Math.Round(_decorationMap.width * pos.x),
                    (int)Math.Round(_decorationMap.height * pos.y));
                // get the index first, since "pos % count" allows to map invalid values to last entry
                var index = Decorations.FindIndex(decoration => decoration.DecorationMapColor == color) % Decorations.Count;
                var deco = Decorations[index];
                if (deco.AlreadyPlaced >= deco.Amount) continue;

                var prefabs = deco.Prefabs;
                if (prefabs.Count < 1) continue;

                // instantiate a random one
                var instance = Instantiate(
                    prefabs[random.Next(0, prefabs.Count)],
                    decoWrapper);
                // the object already contains a rotation, so we can't just pass one to instantiate
                instance.transform.position = GetPostionForTextureCoord(pos.x, pos.y);
                var euler = instance.transform.localRotation.eulerAngles;
                euler.y = random.Next(0, deco.RotationSteps-1) * 360f / deco.RotationSteps;
                instance.transform.localRotation = Quaternion.Euler(euler);
                instance.transform.localScale *= deco.MinScale + (float)random.NextDouble() * (deco.MaxScale - deco.MinScale);
                deco.AlreadyPlaced++;
                
                done = true;
                foreach(var d in Decorations)
                {
                    done = done && d.AlreadyPlaced >= d.Amount;
                }
            }
            
            return state;
        }

        private object _setUpBuckets(object state)
        {
            var size = Vector3.Scale(Scale, _extents.Scale);
            var xSize = size.x / BucketCount.x;
            var ySize = size.z / BucketCount.y;
            var bucketSize = new Vector2(xSize, ySize);

            _buckets = new List<List<Bucket>>();
            for (var x = 0; x < BucketCount.x; x++)
            {
                _buckets.Add(new List<Bucket>());
                for (var y = 0; y < BucketCount.y; y++)
                {
                    _buckets[x].Add(new Bucket(new Vector2(xSize * x, ySize * y), bucketSize));
                }
            }

            return state;
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

        private Vector3 GetPostionForTextureCoord(float x, float z)
        {
            return Offset +
                   Vector3.Scale(
                       new Vector3(
                           x,
                           _heightMap.GetPixelBilinear(x, z).r * 255,
                           z),
                       Vector3.Scale(_extents.Scale, Scale));
        }

        public Vector3 RealWorldToUnity(Vector2 position)
        {
            var x = (float)(position.x - _extents.MinX);
            var z = (float)(position.y - _extents.MinY);
            return Offset +
                   Vector3.Scale(
                       new Vector3(
                           x,
                           _heightMap.GetPixelBilinear(x / _extents.Scale.x, z / _extents.Scale.z).r * 255,
                           z),
                       Scale);
        }

        public Vector2 UnityToRealWorld(Vector3 position)
        {
            return new Vector2((float) ((position.x - Offset.x) / Scale.x + _extents.MinX), (float) ((position.z - Offset.z) / Scale.z + _extents.MinY));
        }

        public Bucket GetBucket(Vector3 position)
        {
            var id = GetBucketId(position);
            return _buckets[id.x][id.y];
        }

        private Vector2Int GetBucketId(Vector3 position)
        {
            var size = Vector3.Scale(Scale, _extents.Scale);
            var x = Mathf.RoundToInt(Mathf.Clamp((position.x - Offset.x) * BucketCount.x / size.x, 0, BucketCount.x - 1));
            var y = Mathf.RoundToInt(Mathf.Clamp((position.z - Offset.z) * BucketCount.y / size.z, 0, BucketCount.y - 1));
            return new Vector2Int(x, y);
        }

        private List<Bucket> GetBucketsInRange(Vector2Int center, float rangeInBucketCountSpace)
        {
            var result = new List<Bucket>();

            var lower = Mathf.RoundToInt(-rangeInBucketCountSpace);
            var upper = Mathf.RoundToInt(rangeInBucketCountSpace);

            for (var x = lower; x <= upper; x++)
            {
                for (var y = lower; y <= upper; y++)
                {
                    var xPos = center.x + x;
                    var yPos = center.y + y;
                    if (Math.Sqrt(x * x + y * y) < rangeInBucketCountSpace &&
                        0 <= xPos && xPos < BucketCount.x &&
                        0 <= yPos && yPos < BucketCount.x)
                    {
                        result.Add(_buckets[xPos][yPos]);
                    }
                }
            }

            return result;
        }

        public Unit GetNearestUnit(Vector3 position, Faction faction)
        {
            var id = GetBucketId(position);
            var alreadyTested = new List<Bucket>();

            Unit nearestUnit = null;
            var minDist = float.MaxValue;
            var range = 0.45f;

            while (nearestUnit == null)
            {
                var buckets = GetBucketsInRange(id, range++);
                buckets.RemoveAll(bucket => alreadyTested.Contains(bucket));
                if (buckets.Count == 0) break;

                foreach (var bucket in buckets)
                {
                    if (!bucket.ContainsAny(faction)) continue;

                    foreach (var unit in bucket.GetUnits(faction))
                    {
                        var dist = (unit.transform.position - position).magnitude;
                        if (!(dist < minDist)) continue;
                        minDist = dist;
                        nearestUnit = unit;
                    }
                }

                alreadyTested.AddRange(buckets);
            }

            return nearestUnit;
        }
    }
}
