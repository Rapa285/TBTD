using UnityEngine;

[CreateAssetMenu(fileName ="New Enemy Data",menuName ="TBTD/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyName;
    public Sprite enemyIcon;
    // public string flavorText;
    public string specialAbilityDescription;
}