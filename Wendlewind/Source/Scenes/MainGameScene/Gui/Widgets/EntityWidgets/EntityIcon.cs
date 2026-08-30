namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityIcon : Image
{
    public EntityIcon(EntityDef def, int? size = null)
    {
        size ??= BaseContent.IconSizes.Large;
        Background = def.GetIconImage();
        Width = size;
        Height = size;
    }

    public EntityIcon(Entity entity, int? size = null)
    {
        size ??= BaseContent.IconSizes.Large;
        Background = entity.GetIconImage();
        Width = size;
        Height = size;
    }
}