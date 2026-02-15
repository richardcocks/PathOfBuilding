using PathOfBuilding.Core.Calculation;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcHitDamageTests
{
    // ─── ApplyResistAndPen ───

    [Fact]
    public void ApplyResistAndPen_ZeroResist_FullDamage()
    {
        double result = CalcHitDamage.ApplyResistAndPen(1000, 0, 0);
        Assert.Equal(1000, result);
    }

    [Fact]
    public void ApplyResistAndPen_75Resist_25Percent()
    {
        double result = CalcHitDamage.ApplyResistAndPen(1000, 75, 0);
        Assert.Equal(250, result);
    }

    [Fact]
    public void ApplyResistAndPen_ResistWithPen()
    {
        // 75% resist - 20% pen = 55% effective resist
        double result = CalcHitDamage.ApplyResistAndPen(1000, 75, 20);
        Assert.Equal(450, result, 5);
    }

    [Fact]
    public void ApplyResistAndPen_NegativeResist_MoreThanBase()
    {
        // -50% resist: mult = 1 - (-50)/100 = 1.5
        double result = CalcHitDamage.ApplyResistAndPen(1000, -50, 0);
        Assert.Equal(1500, result);
    }

    [Fact]
    public void ApplyResistAndPen_WithTakenInc()
    {
        // 0 resist, 10% increased damage taken
        double result = CalcHitDamage.ApplyResistAndPen(1000, 0, 0, takenInc: 10);
        Assert.Equal(1100, result);
    }

    [Fact]
    public void ApplyResistAndPen_WithTakenMore()
    {
        // 0 resist, 50% more damage taken
        double result = CalcHitDamage.ApplyResistAndPen(1000, 0, 0, takenMore: 1.5);
        Assert.Equal(1500, result);
    }

    [Fact]
    public void ApplyResistAndPen_AllFactors()
    {
        // 75% resist, 20% pen, 10% taken inc, 1.5 taken more
        // effectiveResist = 75 - 20 = 55
        // effMult = (1 - 55/100) * (1 + 10/100) * 1.5 = 0.45 * 1.1 * 1.5 = 0.7425
        double result = CalcHitDamage.ApplyResistAndPen(1000, 75, 20, 10, 1.5);
        Assert.Equal(742.5, result, 1);
    }

    // ─── AverageCritDamage ───

    [Fact]
    public void AverageCritDamage_ZeroCrit_ReturnsNonCrit()
    {
        double result = CalcHitDamage.AverageCritDamage(100, 200, 0);
        Assert.Equal(100, result);
    }

    [Fact]
    public void AverageCritDamage_100Crit_ReturnsCrit()
    {
        double result = CalcHitDamage.AverageCritDamage(100, 200, 100);
        Assert.Equal(200, result);
    }

    [Fact]
    public void AverageCritDamage_50Crit_Average()
    {
        double result = CalcHitDamage.AverageCritDamage(100, 200, 50);
        Assert.Equal(150, result);
    }

    // ─── EffectiveCritChance ───

    [Fact]
    public void EffectiveCritChance_BaseOnly()
    {
        double result = CalcHitDamage.EffectiveCritChance(5);
        Assert.Equal(5, result);
    }

    [Fact]
    public void EffectiveCritChance_WithInc()
    {
        // 5% base, 100% increased = 5 * 2 = 10%
        double result = CalcHitDamage.EffectiveCritChance(5, critChanceInc: 100);
        Assert.Equal(10, result);
    }

    [Fact]
    public void EffectiveCritChance_Capped()
    {
        double result = CalcHitDamage.EffectiveCritChance(5, critChanceInc: 10000);
        Assert.Equal(100, result);
    }

    [Fact]
    public void EffectiveCritChance_CustomCap()
    {
        double result = CalcHitDamage.EffectiveCritChance(50, critChanceInc: 200, critChanceCap: 80);
        Assert.Equal(80, result);
    }

    [Fact]
    public void EffectiveCritChance_WithMore()
    {
        // 5% base, 0% inc, 2.0 more = 10%
        double result = CalcHitDamage.EffectiveCritChance(5, critChanceMore: 2.0);
        Assert.Equal(10, result);
    }

    // ─── CritMultiplier ───

    [Fact]
    public void CritMultiplier_Base150()
    {
        double result = CalcHitDamage.CritMultiplier(150);
        Assert.Equal(150, result);
    }

    [Fact]
    public void CritMultiplier_WithAdditions()
    {
        double result = CalcHitDamage.CritMultiplier(150, 50);
        Assert.Equal(200, result);
    }

    // ─── LuckyDamage ───

    [Fact]
    public void LuckyDamage_NoLuck_SimpleAverage()
    {
        // luckyChance = 0: rolls = 2, avg = min/2 + max/2
        double result = CalcHitDamage.LuckyDamage(100, 200, 0);
        Assert.Equal(150, result);
    }

    [Fact]
    public void LuckyDamage_FullyLucky_BiasedToMax()
    {
        // luckyChance = 1: rolls = 3
        // avgNotLucky = 100/3 + 200/3 = 100
        // avgLucky = 100/3 + 2*200/3 = 166.67
        // result = 100 * 0 + 166.67 * 1 = 166.67
        double result = CalcHitDamage.LuckyDamage(100, 200, 1);
        Assert.True(result > 150); // Lucky biases toward max
        Assert.Equal(166.67, result, 1);
    }

    [Fact]
    public void LuckyDamage_FullyUnlucky_BiasedToMin()
    {
        // luckyChance = -1: rolls = 3
        // avgNotLucky = 100/3 + 200/3 = 100
        // avgUnlucky = 2*100/3 + 200/3 = 133.33
        // result = 100 * 0 + 133.33 * 1 = 133.33
        double result = CalcHitDamage.LuckyDamage(100, 200, -1);
        Assert.True(result < 150); // Unlucky biases toward min
        Assert.Equal(133.33, result, 1);
    }

    // ─── DoubleDamageEffect ───

    [Fact]
    public void DoubleDamageEffect_NoChance_ReturnsOne()
    {
        double result = CalcHitDamage.DoubleDamageEffect(0);
        Assert.Equal(1, result);
    }

    [Fact]
    public void DoubleDamageEffect_100Percent_ReturnsTwo()
    {
        double result = CalcHitDamage.DoubleDamageEffect(100);
        Assert.Equal(2, result);
    }

    [Fact]
    public void DoubleDamageEffect_50Percent_Returns1Point5()
    {
        double result = CalcHitDamage.DoubleDamageEffect(50);
        Assert.Equal(1.5, result);
    }

    [Fact]
    public void DoubleDamageEffect_WithTriple()
    {
        // 50% double + 20% triple = 1 + 0.5 + 0.2 = 1.7
        double result = CalcHitDamage.DoubleDamageEffect(50, 20);
        Assert.Equal(1.7, result);
    }
}
