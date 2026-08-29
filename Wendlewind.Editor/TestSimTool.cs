using Wendlewind.NetCode;
using Wendlewind.PawnLayout;
using Wendlewind.Sim.Combat;
using Num = System.Numerics;

namespace Wendlewind.Editor;

public sealed class TestSimTool
{
    private readonly GameContext _context;
    private readonly EditorPawnRenderer _attackerRenderer;
    private readonly EditorPawnRenderer _defenderRenderer;
    private readonly string[] _buildIds;
    private int _attackerBuild;
    private int _defenderBuild;
    private int _seed = 384710648;
    private int _speed = 1;
    private bool _paused;
    private bool _running;
    private float _accumulator;
    private string? _status;
    private readonly List<string> _log = [];
    private Action<string>? _logHandler;

    public TestSimTool(GameContext context, EditorPawnRenderer attacker, EditorPawnRenderer defender)
    {
        _context = context;
        _attackerRenderer = attacker;
        _defenderRenderer = defender;
        _buildIds = BuildTemplates.All.Select(t => t.BuildId).ToArray();
        _attackerBuild = Math.Max(0, Array.IndexOf(_buildIds, "TankRegen"));
        _defenderBuild = Math.Max(0, Array.IndexOf(_buildIds, "AcidRusher"));
    }

    public void Update(GameTime gameTime)
    {
        if (!_running || _paused)
        {
            return;
        }

        var encounter = _context.CurrentZone?.ActiveEncounter;
        if (encounter is not { State: EncounterState.InProgress })
        {
            _running = false;
            _status = DescribeResult();
            return;
        }

        _accumulator += (float)gameTime.ElapsedGameTime.TotalSeconds * Math.Max(_speed, 1);
        var step = 1f / GameContext.TicksPerSecond;
        var guard = 0;
        while (_accumulator >= step && guard < 8)
        {
            _context.IsPaused = false;
            _context.Tick();
            _accumulator -= step;
            guard++;
        }
    }

    public void PreRender()
    {
        var attacker = TryGetAttacker();
        var defender = TryGetDefender();
        _attackerRenderer.Render(attacker, attacker != null ? BodyPartLayoutRegistry.GetLayoutFor(attacker.Body) : null);
        _defenderRenderer.Render(defender, defender != null ? BodyPartLayoutRegistry.GetLayoutFor(defender.Body) : null);
    }

    public void Draw()
    {
        DrawToolbar();
        ImGui.Separator();
        DrawCombatView();
    }

    private void DrawToolbar()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Attacker");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        ImGui.Combo("##atk", ref _attackerBuild, _buildIds, _buildIds.Length);
        ImGui.SameLine();
        ImGui.Text("Defender");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        ImGui.Combo("##def", ref _defenderBuild, _buildIds, _buildIds.Length);
        ImGui.SameLine();
        ImGui.Text("Seed");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140);
        ImGui.InputInt("##seed", ref _seed);

        if (ImGui.Button("Start", new Num.Vector2(110, 0)))
        {
            StartEncounter();
        }

        ImGui.SameLine();
        if (ImGui.Button("Rematch", new Num.Vector2(110, 0)))
        {
            StartEncounter();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reroll", new Num.Vector2(110, 0)))
        {
            _seed++;
            StartEncounter();
        }

        ImGui.SameLine();
        if (ImGui.Button("Swap", new Num.Vector2(110, 0)))
        {
            (_attackerBuild, _defenderBuild) = (_defenderBuild, _attackerBuild);
            StartEncounter();
        }

        ImGui.SameLine();
        if (ImGui.Button(_paused ? "Resume" : "Pause", new Num.Vector2(110, 0)))
        {
            _paused = !_paused;
            _context.IsPaused = _paused;
        }

        ImGui.SameLine();
        ImGui.Text("Speed");
        ImGui.SameLine();
        foreach (var speed in new[] { 1, 2, 4 })
        {
            if (speed != 1)
            {
                ImGui.SameLine();
            }

            if (ImGui.RadioButton($"{speed}x", _speed == speed))
            {
                _speed = speed;
            }
        }

        if (!string.IsNullOrEmpty(_status))
        {
            ImGui.SameLine();
            ImGui.TextColored(new Num.Vector4(0.85f, 0.75f, 0.4f, 1f), _status);
        }
    }

    private void DrawCombatView()
    {
        var attacker = TryGetAttacker();
        var defender = TryGetDefender();
        var attackerLayout = attacker != null ? BodyPartLayoutRegistry.GetLayoutFor(attacker.Body) : null;
        var defenderLayout = defender != null ? BodyPartLayoutRegistry.GetLayoutFor(defender.Body) : null;

        if (ImGui.BeginTable("duel", 3, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("atk", ImGuiTableColumnFlags.WidthFixed, 400);
            ImGui.TableSetupColumn("mid", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("def", ImGuiTableColumnFlags.WidthFixed, 400);
            ImGui.TableNextColumn();
            DrawPawnColumn("Attacker", attacker, _attackerRenderer, attackerLayout);
            ImGui.TableNextColumn();
            ImGui.Text(DescribeState());
            ImGui.BeginChild("combat-log", new Num.Vector2(0, 420), ImGuiChildFlags.Borders);
            foreach (var line in _log)
            {
                ImGui.TextWrapped(line);
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 8)
            {
                ImGui.SetScrollHereY(1f);
            }

            ImGui.EndChild();
            ImGui.TableNextColumn();
            DrawPawnColumn("Defender", defender, _defenderRenderer, defenderLayout);
            ImGui.EndTable();
        }
    }

    private static void DrawPawnColumn(string title, Pawn? pawn, EditorPawnRenderer renderer, IBodyPartLayout? layout)
    {
        ImGui.Text(title);
        if (pawn == null)
        {
            ImGui.TextDisabled("No pawn");
            return;
        }

        ImGui.Text($"{pawn.LabelShort}   {pawn.Body.HitPoints:F0}/{pawn.Body.MaxHitPoints:F0} HP");
        renderer.DrawImage(360, layout?.NativeSize ?? 512, out _);
    }

    private void StartEncounter()
    {
        DetachLog();
        _log.Clear();
        _accumulator = 0;
        _paused = false;
        _context.IsPaused = false;
        _context.Initialize(_seed);
        var zone = _context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        _context.EnterZone(zone.ZoneDef);

        var attacker = _context.PlayerPawn;
        var defender = CreateOpponent();
        BuildSnapshotFactory.Apply(attacker, BuildTemplates.Get(_buildIds[_attackerBuild]));
        BuildSnapshotFactory.Apply(defender, BuildTemplates.Get(_buildIds[_defenderBuild]));
        _context.CurrentZone!.StartHumanDuel(attacker, defender, _seed);
        AttachLog();
        _running = true;
        _status = "Fighting...";
    }

    private Pawn CreateOpponent()
    {
        var emptyLoadout = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout", raiseError: false)
                           ?? Defs.PawnLoadouts.DefaultStarterLoadout;
        return PawnGenerator.CreatePawn(
            _context,
            new PawnRequest("Chuggins", DefRepository<PawnDef>.GetByMoniker("HumanA")!, emptyLoadout, PawnType.Enemy));
    }

    private Pawn? TryGetAttacker() =>
        _context.CurrentZone?.ActiveEncounter?.PlayerPawns.FirstOrDefault() ?? _context.PlayerPawn;

    private Pawn? TryGetDefender() =>
        _context.CurrentZone?.ActiveEncounter?.EnemyPawns.FirstOrDefault();

    private string DescribeState()
    {
        var encounter = _context.CurrentZone?.ActiveEncounter;
        if (encounter == null)
        {
            return "Idle. Pick builds and press Start.";
        }

        return encounter.State == EncounterState.InProgress
            ? $"In progress  —  tick {encounter.Ticks}"
            : DescribeResult();
    }

    private string DescribeResult()
    {
        var encounter = _context.CurrentZone?.ActiveEncounter;
        if (encounter == null)
        {
            return "No encounter.";
        }

        var attackerDead = encounter.PlayerPawns.All(p => p.IsDead);
        var defenderDead = encounter.EnemyPawns.All(p => p.IsDead);
        if (attackerDead && defenderDead)
        {
            return "Draw.";
        }

        if (defenderDead)
        {
            return "Attacker wins.";
        }

        if (attackerDead)
        {
            return "Defender wins.";
        }

        return $"Finished ({encounter.State}).";
    }

    private void AttachLog()
    {
        var handler = _context.CurrentZone?.ActiveEncounter?.CombatHandler;
        if (handler == null)
        {
            return;
        }

        _logHandler = message => _log.Add(StripRichText(message));
        handler.CombatLogMessageAdded += _logHandler;
    }

    private void DetachLog()
    {
        var handler = _context.CurrentZone?.ActiveEncounter?.CombatHandler;
        if (handler != null && _logHandler != null)
        {
            handler.CombatLogMessageAdded -= _logHandler;
        }

        _logHandler = null;
    }

    private static string StripRichText(string text)
    {
        var chars = text.ToCharArray();
        var output = new char[chars.Length];
        var n = 0;
        var skip = false;
        foreach (var c in chars)
        {
            if (c == '/')
            {
                skip = true;
                continue;
            }

            if (skip)
            {
                if (c == ' ' || c == '\n')
                {
                    skip = false;
                    output[n++] = c;
                }

                continue;
            }

            output[n++] = c;
        }

        return new string(output, 0, n).Trim();
    }
}
