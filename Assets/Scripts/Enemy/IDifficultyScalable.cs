public interface IDifficultyScalable
{
    /// <summary>
    /// For scaling the enemy's stats based on the current difficulty level. The multiplier can be used to increase health, damage, speed, etc. as needed.
    /// </summary>
    void ScaleDifficulty(float multiplier);
}