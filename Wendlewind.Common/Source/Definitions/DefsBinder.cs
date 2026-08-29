using System.Reflection;

namespace Wendlewind.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class DefLocator : Attribute { }

public static class DefsBinder
{
    public static void BindLocators()
    {
        foreach (Type item in GenTypes.AllTypesWithAttribute<DefLocator>())
        {
            BindDefsToStaticClass(item);
        }
    }

    private static void BindDefsToStaticClass(IReflect type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo fieldInfo in fields)
        {
            Def def = (Def)typeof(DefRepository<>)
                .MakeGenericType(fieldInfo.FieldType)
                .GetMethod("GetByMoniker")!
                .Invoke(null, new object[] { fieldInfo.Name, true })!;
            fieldInfo.SetValue(null, def);
        }
    }
}
