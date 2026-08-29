using Wendlewind.Scenes.MainGameScene.Gui;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

namespace Wendlewind.Presentation;

public static class EntityPanelFactory
{
    public static EntityPanelBase Create(BaseGui gui, Entity entity, EntityPanelProperties? properties = null)
    {
        var type = entity.Def.UiClass ?? typeof(EntityPanel);
        return (EntityPanelBase)Activator.CreateInstance(type, gui, entity, properties)!;
    }
}

public static class DefPanelFactory
{
    public static DefPanelBase Create(Def def, DefPanelProperties? properties = null)
    {
        var type = def switch
        {
            ItemDef => typeof(ItemDefPanel),
            SkillDef => typeof(SkillDefPanel),
            _ => typeof(DefPanel)
        };
        return (DefPanelBase)Activator.CreateInstance(type, def, properties)!;
    }
}
