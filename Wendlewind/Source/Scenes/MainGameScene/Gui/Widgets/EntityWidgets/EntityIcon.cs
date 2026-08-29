namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityIcon : Image
{
    public EntityIcon(EntityDef def, int? size = null)
    {
        size ??= BaseContent.IconSizes.Large;
        Background = new TextureRegion(def.GetIcon());
        Width = size;
        Height = size;
    }

    public EntityIcon(Entity entity, int? size = null)
    {
        size ??= BaseContent.IconSizes.Large;
        Background = new TextureRegion(entity.GetIcon());
        Width = size;
        Height = size;
    }
}