namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EssenceShardPanel : EntityPanelBase {
    private readonly Item _shard;

    public EssenceShardPanel(BaseGui gui, EssenceShard shard, EntityPanelProperties? properties = null) : base(gui, shard, properties) {
        _shard = shard;
        Padding = new Thickness(20);
        MinWidth = 300;
        Widgets.Add(new Image { Background = new TextureRegion(shard.Icon), Width = 48, Height = 48 });
        Widgets.Add(new Label("small") { Text = shard.Def.Description, Wrap = true });
        // todo track kills
        // track severed limbs / destroyed parts
        // track trinkets
        
        
    }

    public override void Update() { }
}