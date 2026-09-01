using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.DefWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

namespace Wendlemire.Presentation;

public static class EntityPanelFactory
{
    public static EntityPanelBase Create(BaseGui gui, Entity entity, EntityPanelProperties? properties = null)
    {
        var type = entity.Def.UiClass ?? typeof(EntityPanel);
        try
        {
            return (EntityPanelBase)Activator.CreateInstance(type, gui, entity, properties)!;
        }
        catch (Exception)
        {
            return entity is Item item
                ? new ArmorPanel(gui, item, properties)
                : new EntityPanel(gui, entity, properties);
        }
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
