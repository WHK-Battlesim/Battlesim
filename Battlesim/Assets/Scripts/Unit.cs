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
        public double Health;
        public double Damage;
        public double Accuracy;
        public double Range;

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
        private int _health;
        private double _moral;
        private Unit _target;
        
        private int _msUntilRepath;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition = false;
            transform.position = _agent.nextPosition;
            
            RandomizeRepathTime();
        }

        private void Update()
        {
            if (UnitManager == null || !UnitManager.Running) return;

            var moving = _agent.remainingDistance > _agent.stoppingDistance;
            _animator.SetBool("Moving", moving);
            if (!moving)
            {
                _agent.velocity = Vector3.zero;
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
    }
}
