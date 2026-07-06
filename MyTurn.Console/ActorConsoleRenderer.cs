using MyTurn.Application;
using MyTurn.Console.Input;
using MyTurn.Domain;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace MyTurn.Console;

internal static class ActorConsoleRenderer
{
    public static void ShowTitle()
    {
        AnsiConsole.Write(new Rule("[bold yellow]My Turn[/]").RuleStyle("grey"));
        AnsiConsole.Write(
            new Panel("[green]A turn-based console RPG foundation.[/]")
                .Header("Welcome")
                .BorderColor(Color.Green));
    }

    public static void ShowCharacterPreview(CreateActorRequest request)
    {
        AnsiConsole.Write(new Rule("[bold yellow]Review Character[/]").RuleStyle("grey"));
        AnsiConsole.Write(BuildCharacterTable(
            request.Name,
            request.Age,
            request.Gender.GetDisplayName(),
            request.Species.GetDisplayName(),
            request.CharacterClass.GetDisplayName(),
            "0",
            "Selected after creation"));
    }

    public static void ShowCreated(Actor actor)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Character created![/] Name: [yellow]{actor.Name}[/], Age: [yellow]{actor.Age}[/], Gender: [yellow]{actor.Gender.GetDisplayName()}[/], Species: [yellow]{actor.Species.GetDisplayName()}[/], Character Class: [yellow]{actor.CharacterClass.GetDisplayName()}[/]");
    }

    public static void ShowCreated(Party party)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Party created![/] Leader: [yellow]{party.Leader.Name}[/], Active Members: [yellow]{party.ActiveMembers.Count}[/]");
        ShowParty(party);
    }

    public static void ShowLoaded(Actor actor)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Save loaded![/] Name: [yellow]{actor.Name}[/], Class: [yellow]{actor.CharacterClass.GetDisplayName()}[/], Steps: [yellow]{actor.Steps}[/]");
    }

    public static void ShowLoaded(Party party)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Save loaded![/] Leader: [yellow]{party.Leader.Name}[/], Active Members: [yellow]{party.ActiveMembers.Count}[/], Steps: [yellow]{party.Steps}[/]");
    }

    public static void ShowHub(Party party, IReadOnlyList<string> options, int selectedIndex, InputSnapshot snapshot)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold yellow]Camp[/]").RuleStyle("grey"));

        var layout = new Grid();
        layout.AddColumn();
        layout.AddColumn();
        layout.AddRow(new IRenderable[]
        {
            BuildPartySummaryPanel(party),
            BuildHubMenuPanel(options, selectedIndex)
        });

        AnsiConsole.Write(layout);
        ConsoleMenu.RenderFooter("[grey]Move:[/] D-pad/left stick or WASD/arrows  [grey]Select:[/] A/Enter  [grey]Back:[/] B/Esc", snapshot);
    }

    public static void ShowParty(Party party)
    {
        var table = new Table()
            .Title("Party")
            .AddColumn("Status")
            .AddColumn("Name")
            .AddColumn("Class")
            .AddColumn(new TableColumn("HP").RightAligned())
            .AddColumn("Weapon");

        foreach (var member in party.ActiveMembers)
        {
            table.AddRow(
                member == party.Leader ? "Leader" : "Active",
                member.Name,
                member.CharacterClass.GetDisplayName(),
                $"{member.Stats[StatType.Health].CurrentValue}/{member.Stats[StatType.Health].MaxValue}",
                member.Equipment.EquippedWeapon.Name);
        }

        foreach (var member in party.ReserveMembers)
        {
            table.AddRow(
                "Reserve",
                member.Name,
                member.CharacterClass.GetDisplayName(),
                $"{member.Stats[StatType.Health].CurrentValue}/{member.Stats[StatType.Health].MaxValue}",
                member.Equipment.EquippedWeapon.Name);
        }

        AnsiConsole.Write(table);
    }

    public static void ShowCharacter(Actor actor)
    {
        AnsiConsole.Write(new Rule("[bold yellow]Character[/]").RuleStyle("grey"));
        AnsiConsole.Write(BuildCharacterTable(
            actor.Name,
            actor.Age,
            actor.Gender.GetDisplayName(),
            actor.Species.GetDisplayName(),
            actor.CharacterClass.GetDisplayName(),
            actor.Steps.ToString(),
            actor.Equipment.EquippedWeapon.Name));
    }

    public static void ShowStats(Actor actor)
    {
        var table = new Table()
            .Title($"{actor.Name} Stats")
            .AddColumn("Stat")
            .AddColumn(new TableColumn("Current").RightAligned())
            .AddColumn(new TableColumn("Max").RightAligned());

        foreach (var stat in actor.Stats)
        {
            table.AddRow(
                stat.StatType.GetDisplayName(),
                stat.CurrentValue.ToString(),
                stat.MaxValue.ToString());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowInventory(Actor actor)
    {
        var table = new Table()
            .Title($"{actor.Name} Inventory")
            .AddColumn("Item")
            .AddColumn("Kind")
            .AddColumn(new TableColumn("Qty").RightAligned());

        table.AddRow("Currency", ItemKind.Currency.GetDisplayName(), actor.Inventory.Currency.ToString());

        foreach (var stack in actor.Inventory.Items.OrderBy(stack => stack.Item.Kind).ThenBy(stack => stack.Item.Name))
        {
            table.AddRow(stack.Item.Name, stack.Item.Kind.GetDisplayName(), stack.Quantity.ToString());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowInventory(Party party)
    {
        var table = new Table()
            .Title("Party Inventory")
            .AddColumn("Item")
            .AddColumn("Kind")
            .AddColumn(new TableColumn("Qty").RightAligned());

        table.AddRow("Currency", ItemKind.Currency.GetDisplayName(), party.Inventory.Currency.ToString());

        foreach (var stack in party.Inventory.Items.OrderBy(stack => stack.Item.Kind).ThenBy(stack => stack.Item.Name))
        {
            table.AddRow(stack.Item.Name, stack.Item.Kind.GetDisplayName(), stack.Quantity.ToString());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowSkills(Actor actor)
    {
        var table = new Table()
            .Title($"{actor.Name} Skills")
            .AddColumn("Skill")
            .AddColumn(new TableColumn("Level").RightAligned())
            .AddColumn(new TableColumn("Experience").RightAligned())
            .AddColumn(new TableColumn("Next").RightAligned());

        foreach (var skill in actor.Skills)
        {
            table.AddRow(
                skill.SkillType.GetDisplayName(),
                skill.Leveling.CurrentLevel.ToString(),
                skill.Leveling.Experience.ToString(),
                skill.Leveling.ExperienceToNextLevel.ToString());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowEquipment(Actor actor)
    {
        var table = new Table()
            .Title($"{actor.Name} Equipment")
            .AddColumn("Slot")
            .AddColumn("Name")
            .AddColumn("Details");

        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var item = actor.Equipment[slot];
            var details = item switch
            {
                null => "-",
                IWeapon weapon => $"{weapon.WeaponType.GetDisplayName()} {weapon.MinDamage}-{weapon.MaxDamage} damage",
                _ => string.Join(", ", item.StatModifiers.Select(modifier => $"{modifier.StatType.GetDisplayName()} {modifier.Value:+#;-#;0}"))
            };

            table.AddRow(slot.GetDisplayName(), item?.Name ?? "-", details);
        }

        AnsiConsole.Write(table);
    }

    public static void ShowGearChanged(IEquipmentItem item)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]Equipped[/] [yellow]{item.Name}[/] in [yellow]{item.Slot.GetDisplayName()}[/].");
    }

    public static void ShowExit()
    {
        AnsiConsole.MarkupLine("[red]Exiting...[/]");
    }

    public static void ShowConsumablesAreCombatOnly()
    {
        AnsiConsole.MarkupLine("[yellow]Consumables are available during combat when there is health to restore.[/]");
    }

    public static void ShowEncounter(Encounter encounter)
    {
        AnsiConsole.Write(new Rule($"[bold red]Encounter Seed {encounter.Seed}[/]").RuleStyle("grey"));

        var table = new Table()
            .AddColumn("Enemy")
            .AddColumn(new TableColumn("XP").RightAligned());

        foreach (var enemy in encounter.Enemies)
        {
            table.AddRow(enemy.Name, enemy.ExperienceReward.ToString());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowBattleScreen(
        CombatState state,
        Combatant? actingCombatant,
        IReadOnlyList<string> battleLog,
        IReadOnlyList<string>? commandOptions = null,
        int selectedCommandIndex = -1,
        InputSnapshot? snapshot = null)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold red]Battle Seed {state.Seed}[/]").RuleStyle("grey"));

        var topGrid = new Grid();
        topGrid.AddColumn();
        topGrid.AddColumn();
        topGrid.AddRow(new IRenderable[]
        {
            BuildEnemyBattlePanel(state, actingCombatant),
            BuildPartyBattlePanel(state, actingCombatant)
        });

        var bottomGrid = new Grid();
        bottomGrid.AddColumn();
        bottomGrid.AddColumn();
        bottomGrid.AddRow(new IRenderable[]
        {
            BuildBattleLogPanel(battleLog),
            BuildBattleInfoPanel(state, actingCombatant)
        });

        AnsiConsole.Write(topGrid);
        AnsiConsole.Write(bottomGrid);

        if (commandOptions is not null)
        {
            AnsiConsole.Write(BuildCommandMenuPanel(commandOptions, selectedCommandIndex));
        }

        if (snapshot is not null)
        {
            ConsoleMenu.RenderFooter("[grey]Navigate:[/] D-pad/left stick or WASD/arrows  [grey]Select:[/] A/Enter  [grey]Back:[/] B/Esc", snapshot);
        }
    }

    public static void ShowCombatants(CombatState state)
    {
        var table = new Table()
            .Title("Combatants")
            .AddColumn("Team")
            .AddColumn("Name")
            .AddColumn(new TableColumn("HP").RightAligned())
            .AddColumn(new TableColumn("Speed").RightAligned())
            .AddColumn("Status");

        foreach (var combatant in state.Enemies.Concat(state.PartyCombatants))
        {
            table.AddRow(
                combatant.Team.GetDisplayName(),
                combatant.Name,
                $"{combatant.CurrentHealth}/{combatant.MaxHealth}",
                combatant.Stats[StatType.Speed].CurrentValue.ToString(),
                combatant.IsAlive ? combatant.IsDefending ? "Defending" : "Ready" : "Defeated");
        }

        AnsiConsole.Write(table);
    }

    public static void ShowDamage(DamageResult result)
    {
        var critical = result.IsCritical ? " [bold yellow]Critical![/]" : string.Empty;
        var defeated = result.TargetDefeated ? " [red]Defeated![/]" : string.Empty;

        AnsiConsole.MarkupLineInterpolated(
            $"[yellow]{result.Attacker.Name}[/] hits [yellow]{result.Target.Name}[/] for [red]{result.Damage}[/] damage.{critical}{defeated}");
    }

    public static void ShowHealing(HealingResult result)
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[green]{result.Target.Name}[/] uses [yellow]{result.Consumable.Name}[/] and heals [green]{result.AmountHealed}[/] HP.");
    }

    public static void ShowDefend(Combatant combatant)
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]{combatant.Name}[/] defends.");
    }

    public static void ShowNoAvailableItems(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]{message}[/]");
    }

    public static void ShowBattleOutcome(BattleOutcome outcome)
    {
        if (outcome.OutcomeType == BattleOutcomeType.Defeat)
        {
            AnsiConsole.Write(
                new Panel("[red]You were defeated and return to safety with no rewards.[/]")
                    .Header("Defeat")
                    .BorderColor(Color.Red));
            return;
        }

        var rewardLines = new List<string>
        {
            $"Currency: {outcome.Reward.Currency}",
            $"Experience: {outcome.ExperienceAwarded} {outcome.ExperienceSkill?.GetDisplayName() ?? "Skill"} XP"
        };

        rewardLines.AddRange(outcome.Reward.Items.Select(item => $"{item.Item.Name} x{item.Quantity}"));

        AnsiConsole.Write(
            new Panel(string.Join(Environment.NewLine, rewardLines))
                .Header("Victory")
                .BorderColor(Color.Green));
    }

    public static void ShowWorld(Actor actor, WorldSession session, MinimapSnapshot minimap)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold green]World Seed {session.Map.Seed}[/]").RuleStyle("grey"));

        var worldView = new Grid();
        worldView.AddColumn();
        worldView.AddColumn();
        worldView.AddRow(new IRenderable[]
        {
            BuildMapPanel(session, minimap),
            BuildWorldStatusPanel(actor, session)
        });

        AnsiConsole.Write(worldView);
        AnsiConsole.MarkupLine("[grey]Move with [bold]WASD[/] or arrow keys. Press [bold]Q[/] or [bold]Escape[/] to return to the hub.[/]");
    }

    public static void ShowWorld(
        Party party,
        WorldSession session,
        MinimapSnapshot minimap,
        ExplorationResult? result = null,
        InputSnapshot? snapshot = null)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold green]World Seed {session.Map.Seed}[/]").RuleStyle("grey"));

        var worldView = new Grid();
        worldView.AddColumn();
        worldView.AddColumn();
        worldView.AddRow(new IRenderable[]
        {
            BuildMapPanel(session, minimap),
            BuildWorldStatusPanel(party, session, result)
        });

        AnsiConsole.Write(worldView);
        ConsoleMenu.RenderFooter("[grey]Move:[/] D-pad/left stick or WASD/arrows  [grey]Hub:[/] B/Start/Esc", snapshot ?? InputSnapshot.Empty);
    }

    public static void ShowWorldMessage(ExplorationResult result)
    {
        var color = result.State switch
        {
            ExplorationState.Blocked => "red",
            ExplorationState.EnemyEncounter => "red",
            ExplorationState.TreasureFound => "yellow",
            ExplorationState.ExitReached => "green",
            _ => "grey"
        };

        AnsiConsole.MarkupLineInterpolated($"[{color}]{result.Message}[/]");

        if (result.Reward != LootReward.Empty)
        {
            var rewards = new List<string> { $"Currency: {result.Reward.Currency}" };
            rewards.AddRange(result.Reward.Items.Select(item => $"{item.Item.Name} x{item.Quantity}"));
            AnsiConsole.Write(
                new Panel(string.Join(Environment.NewLine, rewards))
                    .Header("Treasure")
                    .BorderColor(Color.Yellow));
        }
    }

    public static void ShowWorldCompleted(WorldSession session)
    {
        AnsiConsole.Write(
            new Panel($"World Seed: {session.Map.Seed}{Environment.NewLine}Exit: X {session.CurrentPosition.X}, Y {session.CurrentPosition.Y}")
                .Header("World Complete")
                .BorderColor(Color.Green));
    }

    private static Table BuildCharacterTable(
        string name,
        int age,
        string gender,
        string species,
        string characterClass,
        string steps,
        string equippedWeapon)
    {
        return new Table()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("Name", name)
            .AddRow("Age", age.ToString())
            .AddRow("Gender", gender)
            .AddRow("Species", species)
            .AddRow("Class", characterClass)
            .AddRow("Steps", steps)
            .AddRow("Equipped Weapon", equippedWeapon);
    }

    private static Panel BuildPartySummaryPanel(Party party)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn("Name")
            .AddColumn("Class")
            .AddColumn(new TableColumn("HP").RightAligned());

        foreach (var member in party.ActiveMembers)
        {
            table.AddRow(
                Markup.Escape(member.Name),
                member.CharacterClass.GetDisplayName(),
                $"{member.Stats[StatType.Health].CurrentValue}/{member.Stats[StatType.Health].MaxValue}");
        }

        table.AddEmptyRow();
        table.AddRow("[grey]Currency[/]", string.Empty, party.Inventory.Currency.ToString());
        table.AddRow("[grey]Steps[/]", string.Empty, party.Steps.ToString());

        return new Panel(table)
            .Header("Party")
            .BorderColor(Color.Green);
    }

    private static Panel BuildHubMenuPanel(IReadOnlyList<string> options, int selectedIndex)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn(" ")
            .AddColumn("Action");

        for (var i = 0; i < options.Count; i++)
        {
            var marker = i == selectedIndex ? "[bold yellow]>[/]" : " ";
            var label = i == selectedIndex
                ? $"[bold yellow]{Markup.Escape(options[i])}[/]"
                : Markup.Escape(options[i]);
            table.AddRow(marker, label);
        }

        return new Panel(table)
            .Header("Actions")
            .BorderColor(Color.Blue);
    }

    private static Panel BuildEnemyBattlePanel(CombatState state, Combatant? actingCombatant)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn("Enemy")
            .AddColumn(new TableColumn("HP").RightAligned())
            .AddColumn("Status");

        foreach (var enemy in state.Enemies)
        {
            table.AddRow(
                FormatCombatantName(enemy, actingCombatant),
                FormatHpBar(enemy.CurrentHealth, enemy.MaxHealth),
                FormatCombatantStatus(enemy));
        }

        return new Panel(table)
            .Header("Enemies")
            .BorderColor(Color.Red);
    }

    private static Panel BuildPartyBattlePanel(CombatState state, Combatant? actingCombatant)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn("Party")
            .AddColumn(new TableColumn("HP").RightAligned())
            .AddColumn("Status");

        foreach (var member in state.PartyCombatants)
        {
            table.AddRow(
                FormatCombatantName(member, actingCombatant),
                FormatHpBar(member.CurrentHealth, member.MaxHealth),
                FormatCombatantStatus(member));
        }

        return new Panel(table)
            .Header("Party")
            .BorderColor(Color.Green);
    }

    private static Panel BuildBattleLogPanel(IReadOnlyList<string> battleLog)
    {
        var lines = battleLog.Count == 0
            ? new[] { "[grey]Battle begins.[/]" }
            : battleLog.TakeLast(8);

        return new Panel(string.Join(Environment.NewLine, lines))
            .Header("Log")
            .BorderColor(Color.Grey);
    }

    private static Panel BuildBattleInfoPanel(CombatState state, Combatant? actingCombatant)
    {
        var potionCount = state.Party.Inventory.Items
            .Where(stack => stack.Item is ConsumableDefinition)
            .Sum(stack => stack.Quantity);
        var actorLine = actingCombatant is null
            ? "[grey]Resolving...[/]"
            : $"{Markup.Escape(actingCombatant.Name)} / {actingCombatant.Team.GetDisplayName()}";
        var details = new Table()
            .NoBorder()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("[grey]Turn[/]", actorLine)
            .AddRow("[grey]Potions[/]", potionCount.ToString())
            .AddRow("[grey]Currency[/]", state.Party.Inventory.Currency.ToString())
            .AddRow("[grey]Victory[/]", $"{state.LivingEnemies.Count} enemies left")
            .AddRow("[grey]Defeat[/]", $"{state.LivingPartyMembers.Count} allies standing");

        return new Panel(details)
            .Header("Command")
            .BorderColor(Color.Blue);
    }

    private static Panel BuildCommandMenuPanel(IReadOnlyList<string> options, int selectedIndex)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        for (var i = 0; i < options.Count; i += 2)
        {
            var left = FormatCommandOption(options[i], i == selectedIndex);
            var right = i + 1 < options.Count
                ? FormatCommandOption(options[i + 1], i + 1 == selectedIndex)
                : string.Empty;
            grid.AddRow(left, right);
        }

        return new Panel(grid)
            .Header("Actions")
            .BorderColor(Color.Blue);
    }

    private static string FormatCommandOption(string label, bool isSelected)
    {
        var escaped = Markup.Escape(label);
        return isSelected ? $"[bold yellow]> {escaped}[/]" : $"  {escaped}";
    }

    private static string FormatCombatantName(Combatant combatant, Combatant? actingCombatant)
    {
        var name = Markup.Escape(combatant.Name);

        if (actingCombatant?.Id == combatant.Id)
        {
            return $"[bold yellow]> {name}[/]";
        }

        return combatant.IsAlive ? name : $"[grey]{name}[/]";
    }

    private static string FormatHpBar(int currentHealth, int maxHealth)
    {
        const int width = 14;

        var safeMax = Math.Max(1, maxHealth);
        var safeCurrent = Math.Clamp(currentHealth, 0, safeMax);
        var filled = (int)Math.Round((double)safeCurrent / safeMax * width);
        var color = safeCurrent <= safeMax * 0.25 ? "red" : safeCurrent <= safeMax * 0.5 ? "yellow" : "green";

        return $"[{color}]{new string('#', filled)}[/][grey]{new string('-', width - filled)}[/] {safeCurrent}/{safeMax}";
    }

    private static string FormatCombatantStatus(Combatant combatant)
    {
        if (!combatant.IsAlive)
        {
            return "[red]Defeated[/]";
        }

        return combatant.IsDefending ? "[yellow]Guard[/]" : "[grey]Ready[/]";
    }

    private static Panel BuildMapPanel(WorldSession session, MinimapSnapshot minimap)
    {
        var mapGrid = new Grid();

        for (var x = minimap.MinCoordinate; x <= minimap.MaxCoordinate; x++)
        {
            mapGrid.AddColumn();
        }

        for (var y = minimap.MaxCoordinate; y >= minimap.MinCoordinate; y--)
        {
            var row = Enumerable.Range(minimap.MinCoordinate, minimap.MaxCoordinate - minimap.MinCoordinate + 1)
                .Select(x => FormatMapCell(session, minimap.GetCell(new WorldPosition(x, y))))
                .ToArray();
            mapGrid.AddRow(row);
        }

        return new Panel(mapGrid)
            .Header("Map")
            .BorderColor(Color.Green);
    }

    private static Panel BuildWorldStatusPanel(Actor actor, WorldSession session)
    {
        var room = session.CurrentRoom;
        var exploredRooms = session.Map.Rooms.Values.Count(currentRoom => currentRoom.IsVisited);
        var totalRooms = session.Map.Rooms.Count;
        var status = new Table()
            .NoBorder()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("[grey]Position[/]", $"[white]X {session.CurrentPosition.X}, Y {session.CurrentPosition.Y}[/]")
            .AddRow("[grey]Room[/]", FormatRoomType(room.RoomType))
            .AddRow("[grey]Status[/]", FormatRoomStatus(room))
            .AddRow("[grey]Steps[/]", $"[yellow]{actor.Steps}[/]")
            .AddRow("[grey]Explored[/]", $"[green]{exploredRooms}[/]/[grey]{totalRooms}[/]");

        var legend = new Grid();
        legend.AddColumn();
        legend.AddColumn();
        AddLegendRow(legend, "[bold white on green] @ [/]", "You");
        AddLegendRow(legend, "[bold white on blue] S [/]", "Start");
        AddLegendRow(legend, "[bold white on purple] E [/]", "Exit");
        AddLegendRow(legend, "[bold white on red] ! [/]", "Enemy");
        AddLegendRow(legend, "[bold black on yellow] $ [/]", "Treasure");
        AddLegendRow(legend, "[white on black] . [/]", "Cleared");
        AddLegendRow(legend, "[grey on black] ? [/]", "Scouted");

        var panelGrid = new Grid();
        panelGrid.AddColumn();
        panelGrid.AddRow(new IRenderable[] { status });
        panelGrid.AddEmptyRow();
        panelGrid.AddRow(new IRenderable[] { new Markup("[bold]Legend[/]") });
        panelGrid.AddRow(new IRenderable[] { legend });

        return new Panel(panelGrid)
            .Header("Status")
            .BorderColor(GetRoomBorderColor(room));
    }

    private static Panel BuildWorldStatusPanel(Party party, WorldSession session, ExplorationResult? result)
    {
        var room = session.CurrentRoom;
        var exploredRooms = session.Map.Rooms.Values.Count(currentRoom => currentRoom.IsVisited);
        var totalRooms = session.Map.Rooms.Count;
        var status = new Table()
            .NoBorder()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("[grey]Leader[/]", $"[yellow]{party.Leader.Name}[/]")
            .AddRow("[grey]Active[/]", $"[yellow]{party.ActiveMembers.Count}[/]/[grey]{Party.MaxActiveMembers}[/]")
            .AddRow("[grey]Position[/]", $"[white]X {session.CurrentPosition.X}, Y {session.CurrentPosition.Y}[/]")
            .AddRow("[grey]Room[/]", FormatRoomType(room.RoomType))
            .AddRow("[grey]Status[/]", FormatRoomStatus(room))
            .AddRow("[grey]Steps[/]", $"[yellow]{party.Steps}[/]")
            .AddRow("[grey]Explored[/]", $"[green]{exploredRooms}[/]/[grey]{totalRooms}[/]");

        var legend = new Grid();
        legend.AddColumn();
        legend.AddColumn();
        AddLegendRow(legend, "[bold white on green] @ [/]", "Party");
        AddLegendRow(legend, "[bold white on blue] S [/]", "Start");
        AddLegendRow(legend, "[bold white on purple] E [/]", "Exit");
        AddLegendRow(legend, "[bold white on red] ! [/]", "Enemy");
        AddLegendRow(legend, "[bold black on yellow] $ [/]", "Treasure");
        AddLegendRow(legend, "[white on black] . [/]", "Cleared");
        AddLegendRow(legend, "[grey on black] ? [/]", "Scouted");

        var panelGrid = new Grid();
        panelGrid.AddColumn();
        panelGrid.AddRow(new IRenderable[] { status });
        panelGrid.AddEmptyRow();
        panelGrid.AddRow(new IRenderable[] { new Markup("[bold]Legend[/]") });
        panelGrid.AddRow(new IRenderable[] { legend });

        if (result is not null)
        {
            panelGrid.AddEmptyRow();
            panelGrid.AddRow(new IRenderable[] { BuildWorldEventPanel(result) });
        }

        return new Panel(panelGrid)
            .Header("Status")
            .BorderColor(GetRoomBorderColor(room));
    }

    private static Panel BuildWorldEventPanel(ExplorationResult result)
    {
        var color = result.State switch
        {
            ExplorationState.Blocked => Color.Red,
            ExplorationState.EnemyEncounter => Color.Red,
            ExplorationState.TreasureFound => Color.Yellow,
            ExplorationState.ExitReached => Color.Green,
            _ => Color.Grey
        };
        var lines = new List<string>
        {
            Markup.Escape(result.Message)
        };

        if (result.Reward != LootReward.Empty)
        {
            lines.Add($"Currency: {result.Reward.Currency}");
            lines.AddRange(result.Reward.Items.Select(item => $"{Markup.Escape(item.Item.Name)} x{item.Quantity}"));
        }

        return new Panel(string.Join(Environment.NewLine, lines))
            .Header("Event")
            .BorderColor(color);
    }

    private static void AddLegendRow(Grid legend, string sample, string label)
    {
        legend.AddRow(sample, $"[grey]{label}[/]");
    }

    private static string FormatMapCell(WorldSession session, MinimapCell cell)
    {
        if (!cell.IsVisible)
        {
            return "[black on black]   [/]";
        }

        if (cell.Symbol == '@')
        {
            return "[bold white on green] @ [/]";
        }

        var room = session.Map.GetRoom(cell.Position);

        return cell.Symbol switch
        {
            'S' => "[bold white on blue] S [/]",
            'E' => "[bold white on purple] E [/]",
            '!' => "[bold white on red] ! [/]",
            '$' => "[bold black on yellow] $ [/]",
            '?' => "[grey on black] ? [/]",
            _ => room.RoomType switch
            {
                RoomType.Start => "[white on blue] S [/]",
                RoomType.Exit => "[white on purple] E [/]",
                RoomType.Enemy => "[white on black] . [/]",
                RoomType.Treasure => "[white on black] . [/]",
                _ => "[white on black] . [/]"
            }
        };
    }

    private static string FormatRoomType(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => "[blue]Start[/]",
            RoomType.Exit => "[purple]Exit[/]",
            RoomType.Enemy => "[red]Enemy[/]",
            RoomType.Treasure => "[yellow]Treasure[/]",
            _ => "[white]Empty[/]"
        };
    }

    private static string FormatRoomStatus(WorldRoom room)
    {
        return room.RoomType switch
        {
            RoomType.Enemy => room.IsCleared ? "[green]Cleared[/]" : "[red]Hostile[/]",
            RoomType.Treasure => room.IsLooted ? "[green]Looted[/]" : "[yellow]Unlooted[/]",
            RoomType.Exit => "[purple]Exit[/]",
            RoomType.Start => "[blue]Safe[/]",
            _ => room.IsVisited ? "[green]Visited[/]" : "[grey]Unvisited[/]"
        };
    }

    private static Color GetRoomBorderColor(WorldRoom room)
    {
        return room.RoomType switch
        {
            RoomType.Enemy when !room.IsCleared => Color.Red,
            RoomType.Treasure when !room.IsLooted => Color.Yellow,
            RoomType.Exit => Color.Purple,
            RoomType.Start => Color.Blue,
            _ => Color.Grey
        };
    }

}
