using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Comparers;

namespace Assets.Scripts
{
    [Serializable]
    public class Situation
    {
        public SerializableUnit[] Units;
        public UnitStats[] Stats;

        [Serializable]
        public class SerializableUnit
        {
            public Unit.Class Class;
            public Unit.Faction Faction;
            public Vector2 Position;
        }

        [Serializable]
        public class UnitStats
        {
            public Unit.Class Class;
            public Unit.Faction Faction;
            public UnitMovementStats Movement;
            public UnitCombatStats Combat;

            public void ApplyTo(GameObject gameObject)
            {
                var agent = gameObject.GetComponent<NavMeshAgent>();
                var unit = gameObject.GetComponent<Unit>();

                agent.speed = Movement.Speed;
                agent.angularSpeed = Movement.AngularSpeed;
                agent.acceleration = Movement.Acceleration;

                unit.InitialCount = Combat.Count;
                unit.InitialMorale = Combat.Morale;

                unit.Health = Combat.Health;
                unit.Accuracy = Combat.Accuracy;
                unit.Damage = Combat.Damage;

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
            public float Morale;

            public float Health;
            public float Damage;
            public float Accuracy;
            public float Range;
            public float MoraleDamage;
            public float AreaDamage;
            public float Spacing;
        }
    }
}
