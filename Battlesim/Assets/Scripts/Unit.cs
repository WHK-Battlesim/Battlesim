using System;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace Assets.Scripts
{
    public enum Class
    {
        Infantry,
        Cavalry,
        Artillery
    }

    public enum Faction
    {
        Prussia,
        Austria
    }
    
    public class Unit : MonoBehaviour
    {
        public Class Class;
        public Faction Faction;
        public Faction TargetFaction;

        /*
         * The overall count of soldiers in this unit an the unit's moral value.
         * If the count is reduced to zero, the unit is destroyed.
         * Reducing the count also reduces the damage dealt to enemies.
         * If the moral falls to zero, the unit surrenders.
         */
        public int InitialCount;
        public double InitialMorale;

        /*
         * Physical damage stats:
         * The lower the unit's health, the higher the chance a soldier is killed.
         * Damage reduces this threshold over time.
         * Accuracy is the offensive counterpart: Higher accuracy leads to higher chance of killing a soldier.
         * The range states how close a unit has to get to the enemy to start doing damage.
         */
        public double InitialHealth;
        public double Damage;
        public double Accuracy;
        public double ReloadTime;

        /*
         * Psychological damage stats:
         * How much of the enemy's moral is depleted when attacking.
         */
        public double MoraleDamage;

        /*
         * Area damage stats:
         * Artillery has a higher AreaDamage value, meaning it can harm multiple soldiers in one attack.
         * This is modified by the defending unit's Spacing value: If higher, the soldiers are spread out more and less prone to area damage.
         */
        public double AreaDamage;
        public double Spacing;
        
        [HideInInspector] public MapGenerator MapGenerator;
        [HideInInspector] public UnitManager UnitManager;
        [HideInInspector] public Bucket Bucket;
        [HideInInspector] public int MinMsUntilRepath;
        [HideInInspector] public int MaxMsUntilRepath;
        [HideInInspector] public Random Random;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Unit _target;
        private int _count;
        private double _morale;
        private double _health;
        
        private int _msUntilRepath;
        private double _sUntilAttack;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition = false;
            transform.position = _agent.nextPosition;
            
            RandomizeRepathTime();

            _count = InitialCount;
            _health = InitialHealth;
            _morale = InitialMorale;
            _sUntilAttack = ReloadTime;
        }

        private void Update()
        {
            if (UnitManager == null || !UnitManager.Running) return;

            var moving = _agent.remainingDistance > _agent.stoppingDistance;
            _animator.SetBool("Moving", moving);
            if (!moving)
            {
                _agent.velocity = Vector3.zero;

                if(_target != null)
                {
                    Attack();
                    _sUntilAttack -= Time.deltaTime;
                }
            }

            _msUntilRepath -= (int) (Time.deltaTime * 1000);
            if (_msUntilRepath > 0) return;

            _target = MapGenerator.GetNearestUnit(transform.position, TargetFaction);
            if (_target != null)
            {
                _agent.SetDestination(_target.transform.position);
            }

            RandomizeRepathTime();
        }

        private void OnAnimatorMove()
        {
            if (UnitManager == null || !UnitManager.Running) return;

            if (_agent == null) return;

            transform.position = _agent.nextPosition;

            if (!Bucket.Contains(transform.position))
            {
                UpdateBucket();
            }
        }

        private void UpdateBucket()
        {
            Bucket.Leave(this);
            Bucket = MapGenerator.GetBucket(transform.position);
            Bucket.Enter(this);
        }

        public void RandomizeRepathTime()
        {
            _msUntilRepath = Random.Next(MinMsUntilRepath, MaxMsUntilRepath);
        }

        private void Attack()
        {
            if (!(_sUntilAttack <= 0)) return;
            _sUntilAttack += ReloadTime;

            _animator.SetTrigger("Fight");

            _target._health -= Damage * _count * AreaDamage;

            var killCount = (int)Math.Floor(_target._health / _target.InitialHealth);

            _target._count -= killCount;
            _target._health += killCount * _target.InitialHealth;

            if (_target._count <= 0)
            {
                _target.Die();
            }
        }

        private void Die()
        {
            _animator.SetTrigger("Die");
            _agent.enabled = false;
            enabled = false;
        }

        public bool Alive()
        {
            return _count > 0;
        }
    }
}
