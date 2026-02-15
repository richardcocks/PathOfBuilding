using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Modifiers;

public class ModListTests
{
    [Fact]
    public void Sum_LinearScan()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        list.AddMod(ModHelper.CreateMod("Mana", ModType.Base, 30));
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 20));

        Assert.Equal(70, list.Sum(ModType.Base, null, "Life"));
        Assert.Equal(30, list.Sum(ModType.Base, null, "Mana"));
    }

    [Fact]
    public void MergeMod_CombinesIdenticalParams()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        list.MergeMod(ModHelper.CreateMod("Life", ModType.Base, 30));

        Assert.Equal(1, list.Count);
        Assert.Equal(80, list.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void MergeMod_DifferentParams_AddsSeparately()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        list.MergeMod(ModHelper.CreateMod("Life", ModType.Inc, 10));

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void MergeMod_SkipNonAdditive()
    {
        var list = new ModList();
        list.MergeMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true), skipNonAdditive: true);

        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void More_Works()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50));

        Assert.Equal(1.5, list.More(null, "Damage"));
    }

    [Fact]
    public void Flag_Works()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true));

        Assert.True(list.Flag(null, "Condition:LowLife"));
        Assert.False(list.Flag(null, "Condition:FullLife"));
    }

    [Fact]
    public void ParentChaining()
    {
        var parent = new ModDB();
        parent.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        var list = new ModList(parent);
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 30));

        Assert.Equal(80, list.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Enumerable()
    {
        var list = new ModList();
        list.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        list.AddMod(ModHelper.CreateMod("Mana", ModType.Base, 30));

        var names = list.Select(m => m.Name).ToArray();
        Assert.Equal(["Life", "Mana"], names);
    }

    [Fact]
    public void AddList_AppendsAll()
    {
        var list1 = new ModList();
        list1.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        var list2 = new ModList();
        list2.AddMod(ModHelper.CreateMod("Life", ModType.Base, 30));

        list1.AddList(list2);
        Assert.Equal(80, list1.Sum(ModType.Base, null, "Life"));
    }
}
