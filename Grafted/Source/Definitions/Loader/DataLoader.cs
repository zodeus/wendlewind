using System;
using System.IO;
using Grafted.Utils;

namespace Grafted.Definitions.Loader;

public static class DataLoader {
    public static void Load() {
        XmlLoader.LoadXmlAssetsIntoMemory(Path.Combine("Content", "Data", "Definitions"));
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(DirectXmlCrossRefLoader.FailMode.Silent);
        DefsBinder.BindLocators();
        foreach (Type defType in typeof(Def).Subclasses()) {
            //Skip Entity Class for now as it causes double initialization of inherited instances 
            if (defType.Name == "EntityDef") {
                continue;
            }

            GenericHelpers.InvokeStaticMethodOnGenericType(typeof(DefRepository<>), defType, "ResolveDefDependencies");
        }
    }
}