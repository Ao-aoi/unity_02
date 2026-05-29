using Neuro.Creature.Evaluation;
using UnityEngine;

namespace Neuro.Creature
{
    public class CreatureEvaluator : MonoBehaviour
    {
        [Header("評価スコア")]
        public float totalFitness = 0f;

        [Header("評価プロファイル")]
        [Tooltip("複数のEvaluationRuleを組み合わせる新しい評価プロファイル。設定されていない場合は従来のcurrentCriteriaを使用します。")]
        public EvaluationProfile evaluationProfile;

        [Tooltip("従来互換用の評価ルール。既存シーンを壊さないため残しています。")]
        public EvaluationCriteria currentCriteria;

        private readonly CompositeEvaluator compositeEvaluator = new CompositeEvaluator();
        private CreatureEvaluationContext context;
        private MovementStatistics movementStatistics;
        private ExplorationTracker explorationTracker;
        private Vector3 spawnPosition;
        private Rigidbody2D bodyRb;
        private CreatureAgent agent;
        private float elapsedTime;

        void Start()
        {
            InitializeEvaluator();
        }

        public void InitializeEvaluator()
        {
            spawnPosition = transform.position;
            bodyRb = GetComponent<Rigidbody2D>();
            if (bodyRb == null) bodyRb = GetComponentInChildren<Rigidbody2D>();
            agent = GetComponent<CreatureAgent>();

            if (currentCriteria == null && evaluationProfile == null)
                currentCriteria = new EvaluationCriteria();

            movementStatistics = new MovementStatistics();
            movementStatistics.Initialize(transform.position, agent);
            explorationTracker = new ExplorationTracker();
            explorationTracker.Reset(transform.position);
            context = new CreatureEvaluationContext(this, agent, transform, bodyRb, movementStatistics, explorationTracker, spawnPosition);

            BuildRuntimes();
            elapsedTime = 0f;
        }

        public void SetEvaluationProfile(EvaluationProfile profile)
        {
            evaluationProfile = profile;
            if (context != null)
                BuildRuntimes();
        }

        private void BuildRuntimes()
        {
            compositeEvaluator.Build(evaluationProfile, currentCriteria, context);
        }

        void Update()
        {
            if (context == null)
                InitializeEvaluator();

            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            movementStatistics.Sample(transform.position, agent);
            context.DeltaTime = deltaTime;
            context.ElapsedTime = elapsedTime;

            totalFitness += compositeEvaluator.Tick(context);
        }
    }
}
