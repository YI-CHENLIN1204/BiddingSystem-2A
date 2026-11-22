using System.Collections;
using System.Reflection;
using 崴正.Services;

var service = new UnitConversionService(null!);
var type = typeof(UnitConversionService);
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var namesField = type.GetField("_unitsByName", flags)!;
var idsField = type.GetField("_unitsById", flags)!;
var basesField = type.GetField("_baseUnitsByCategoryId", flags)!;
var names = (IDictionary)Activator.CreateInstance(namesField.FieldType)!;
var ids = (IDictionary)Activator.CreateInstance(idsField.FieldType)!;
var bases = (IDictionary)Activator.CreateInstance(basesField.FieldType)!;
var definition = idsField.FieldType.GetGenericArguments()[1];
void Add(int id, string name, int category, decimal? factor, bool enabled)
{
    var unit = Activator.CreateInstance(definition)!;
    foreach (var (property, value) in new (string, object?)[] {
        ("UnitId", id), ("UnitName", name), ("UnitCategoryId", category),
        ("AllowConversion", enabled), ("ToBaseFactor", factor) })
        definition.GetProperty(property)!.SetValue(unit, value);
    names.Add(name, unit);
    ids.Add(id, unit);
}
Add(1, "kg", 1, 1, true);
Add(2, "噸", 1, 1000, true);
Add(3, "m", 2, 1, true);
Add(4, "公尺", 2, 1, true);
Add(5, "m²", 3, 1, true);
Add(6, "㎡", 3, 1, true);
Add(7, "m³", 4, 1, true);
Add(8, "立方公尺", 4, 1, true);
Add(9, "式", 5, null, false);
Add(10, "組", 5, null, false);
Add(11, "無係數", 1, null, true);
namesField.SetValue(service, names);
idsField.SetValue(service, ids);
basesField.SetValue(service, bases);
foreach (var (from, to, expected) in new (string?, int, decimal?)[] {
    (" KG ",2,.001m), ("噸",1,1000), ("m",4,1), ("公尺",3,1),
    ("m²",6,1), ("㎡",5,1), ("m³",8,1), ("立方公尺",7,1),
    ("式",9,1), ("式",10,null), ("m²",7,null), ("kg",7,null),
    ("無係數",1,null), ("kg",11,null), ("SYN_UNKNOWN",1,null),
    (null,1,null), (" ",1,null), ("kg",999,null)
})
{
    var result = service.Convert(2, 300, from, to);
    if (result.Success != expected.HasValue ||
        (expected is decimal factor && (result.ConversionFactor != factor ||
        result.Quantity != 2 * factor || result.UnitPrice != 300 / factor)))
        throw new Exception($"Mismatch: {from} => {to}");
    Console.WriteLine($"PASS {from ?? "<null>"} => {to}");
}
