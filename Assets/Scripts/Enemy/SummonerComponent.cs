using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(EnemyEntity),typeof(EnemyMover))]
public class SummonerComponent : MonoBehaviour
{
    [Header("Summoning Settings")]
    [SerializeField] private GameObject[] prefabsToSummon;
    [SerializeField] private float summonCooldown=6f;
    [SerializeField] private float castTime=1f;
    [SerializeField] private float pathSpacing=0.02f;

    private EnemyMover mover;
    private EnemyEntity entity;
    private SplineAnimate splineAnimate;
    private float lastSummonTime;
    private bool isSummoning;

    private void Awake()
    {
        mover=GetComponent<EnemyMover>();
        entity=GetComponent<EnemyEntity>();
        splineAnimate=GetComponent<SplineAnimate>();
    }

    private void Start()
    {
        lastSummonTime=Time.time;
    }

    private void Update()
    {
        if (isSummoning) return;

        if (Time.time - lastSummonTime >= summonCooldown)
        {
            StartCoroutine(SummonRoutine());
        }
    }

    private IEnumerator SummonRoutine()
    {
        isSummoning=true;
        mover.PauseMovement();
        Debug.Log("Summoning minions...");
        
        yield return new WaitForSeconds(castTime);

        ExecuteSummon();

        mover.ResumeMovement();
        lastSummonTime=Time.time;
        isSummoning=false;
    }

    private void ExecuteSummon()
    {
        float currentNormTime=splineAnimate!=null?splineAnimate.NormalizedTime:0f;
        for (int i = 0; i < prefabsToSummon.Length; i++)
        {
            if (prefabsToSummon[i] != null)
            {
                GameObject summoned=Instantiate(prefabsToSummon[i],transform.position,transform.rotation);
                EnemyEntity childEntity=summoned.GetComponent<EnemyEntity>();
                if (childEntity != null)
                {
                    childEntity.BaseTarget=entity.BaseTarget;
                }

                SplineAnimate childSpline=summoned.GetComponent<SplineAnimate>();
                if (childSpline != null && splineAnimate != null)
                {
                    childSpline.Container=splineAnimate.Container;
                    float offsetTime=Mathf.Max(0f,currentNormTime-(i*pathSpacing));
                    childEntity.StartCoroutine(ForcePositionNextFrame(childSpline,offsetTime));
                }
                
            }
        }
    }

    private IEnumerator ForcePositionNextFrame(SplineAnimate childSpline,float offsetTime)
    {
        yield return null;
        if (childSpline != null)
        {
            childSpline.NormalizedTime=offsetTime;
            childSpline.Play();
        }
    }
}