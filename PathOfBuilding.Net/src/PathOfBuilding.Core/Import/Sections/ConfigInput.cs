namespace PathOfBuilding.Core.Import.Sections;

public enum ConfigInputKind { Boolean, Number, String }

public class ConfigInput
{
    public string Name { get; set; } = "";
    public ConfigInputKind Kind { get; set; }
    public bool BooleanValue { get; set; }
    public double NumberValue { get; set; }
    public string StringValue { get; set; } = "";
}
