using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Comparers;

namespace Assets.Scripts
{
    [Serializable]
    public class Situation
    {
        public List<SerializableUnit> Units;
        public List<UnitStats> Stats;

        [Serializable]
        public class SerializableUnit
        {
            public Class Class;
            public Faction Faction;
            public Vector2 Position;
            public float Rotation;
        }

        [Serializable]
        public class UnitStats
        {
            public Class Class;
            public Faction Faction;
            public Faction TargetFaction;
            public UnitMovementStats Movement;
            public UnitCombatStats Combat;

            public void ApplyTo(GameObject gameObject)
            {
                var agent = gameObject.GetComponent<NavMeshAgent>();
                var unit = gameObject.GetComponent<Unit>();

                unit.Class = Class;
                unit.Faction = Faction;
                unit.TargetFaction = TargetFaction;

                agent.speed = Movement.Speed;
                agent.angularSpeed = Movement.AngularSpeed;
                agent.acceleration = Movement.Acceleration;

                unit.InitialCount = Combat.Count;
                unit.InitialMorale = Combat.Morale;

                unit.InitialHealth = Combat.Health;
                unit.Accuracy = Combat.Accuracy;
                unit.Damage = Combat.Damage;
                unit.ReloadTime = Combat.ReloadTime;

                // let range be handled by the NavMeshAgent
                agent.stoppingDistance = Combat.Range;

                unit.MoraleDamage = Combat.MoraleDamage;

                unit.AreaDamage = Combat.AreaDamage;
                unit.Spacing = Combat.Spacing;
            }
        }
        
        [Serializable]
        public class UnitMovementStats
        {
            public float Speed;
            public float AngularSpeed;
            public float Acceleration;
        }
        
        [Serializable]
        public class UnitCombatStats
        {
            public int Count;
            public double Morale;

            public double Health;
            public double Damage;
            public double Accuracy;
            public float Range;
            public double ReloadTime;
            public double MoraleDamage;
            public double AreaDamage;
            public double Spacing;
        }

        public static Situation FromCurrentScene(
            Transform unitWrapper,
            Dictionary<Faction, Dictionary<Class, GameObject>> prefabDict,
            Func<Vector3, Vector2> postionMapping)
        {
            var result = new Situation
            {
                Units = new List<SerializableUnit>(),
                Stats = new List<UnitStats>()
            };

            var unitInstances = unitWrapper.GetComponentsInChildren<Unit>();

            foreach (var unit in unitInstances)
            {
                var transform = unit.GetComponent<Transform>();

                result.Units.Add(new SerializableUnit()
                {
                    Class = unit.Class,
                    Faction = unit.Faction,
                    Position = postionMapping(transform.position),
                    Rotation = transform.rotation.eulerAngles.y
                });
            }

            foreach (var factionPrefabs in prefabDict)
            {
                var faction = factionPrefabs.Key;

                foreach (var classPrefab in factionPrefabs.Value)
                {
                    var @class = classPrefab.Key;
                    var navMeshAgent = classPrefab.Value.GetComponent<NavMeshAgent>();
                    var unit = classPrefab.Value.GetComponent<Unit>();

                    result.Stats.Add(new UnitStats()
                    {
                        Class = @class,
                        Faction = faction,
                        TargetFaction = unit.TargetFaction,
                        Movement = new UnitMovementStats()
                        {
                            Speed = navMeshAgent.speed,
                            AngularSpeed = navMeshAgent.angularSpeed,
                            Acceleration = navMeshAgent.acceleration
                        },
                        Combat = new UnitCombatStats()
                        {
                            Count = unit.InitialCount,
                            Damage = unit.Damage,
                            Health = unit.InitialHealth,
                            Spacing = unit.Spacing,
                            AreaDamage = unit.AreaDamage,
                            Accuracy = unit.Accuracy,
                            Range = navMeshAgent.stoppingDistance,
                            ReloadTime = unit.ReloadTime,
                            Morale = unit.InitialMorale,
                            MoraleDamage = unit.MoraleDamage
                        }
                    });
                }
            }

            return result;
        }
    }
}
