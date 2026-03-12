using System;

[Serializable]
public class GameProfileResponse
{
    public string username;
    public long balance;
    public int tapUpgradeLevel;
    public int tapReward;
    public int tapBonusPercent;
    public long nextTapUpgradeCost;
}
