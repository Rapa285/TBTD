using UnityEngine;

[CreateAssetMenu(fileName ="New Enemy Data",menuName ="TBTD/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyName;
    public EnemyType enemyType;
    public string specialAbilityDescription;
    public GameObject displayModelPrefab;
    public Color typeColor;
}