using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Dev;

/// <summary>
/// Dev-only UI snapshot tool. When DEATHBORN_UI_PREVIEW is set to a folder, the
/// client draws the HUD, windows and overlays with sample data into PNG files
/// and exits. No server or login is needed, so UI changes can be reviewed fast.
/// </summary>
public static class UiPreview
{
    private const int Width = 1280;
    private const int Height = 800;
    private static readonly Color Grass = new(122, 190, 82);

    public static string? OutputDir =>
        Environment.GetEnvironmentVariable("DEATHBORN_UI_PREVIEW") is { Length: > 0 } dir ? dir : null;

    public static void RenderAll(DeathbornGame game)
    {
        var dir = OutputDir!;
        Directory.CreateDirectory(dir);
        GameViewport.Set(Width, Height);

        var skills = SampleSkills();
        var stats = new CharacterStats { Hp = 82, Stamina = 64, Mana = 97, ExpPercent = 38.5f };
        var player = new PlayerEntity { Id = 1, Name = "Akiru", IsLocal = true };
        var inventory = SampleInventory();
        var buffs = new BuffTracker();
        buffs.Apply("battle_shout", 257f, 480f);
        buffs.Apply("iron_skin", 378f, 600f);
        var hud = new PlayerStatusHud();
        var hotbar = new Hotbar();

        void DrawHud(SpriteBatch sb, SpriteFont font)
        {
            hud.Draw(sb, font, player, skills);
            hotbar.Draw(sb, font, inventory, 0);
        }

        Render(game, Path.Combine(dir, "1-hud-windows.png"), (sb, font) =>
        {
            DrawHud(sb, font);
            var overlay = new BuffBarOverlay();
            overlay.Draw(sb, font, buffs);

            var banner = new ZoneBannerOverlay();
            banner.SetPersistentZone("Starter Town", true);
            banner.ShowEnter("Starter Town", "PvP disabled - safe haven");
            banner.Update(1.0f);
            banner.Draw(sb, font);

            var skillsWindow = new SkillsWindow();
            skillsWindow.Bind(() => skills);
            skillsWindow.Open();
            skillsWindow.MoveTo(new Point(410, 170));
            skillsWindow.Draw(sb, font);

            var toasts = new GameNotificationOverlay();
            toasts.Push("Gathered", "+1 Logs", NotificationKind.Success);
            toasts.Push("Friend Request", "Rowan wants to be friends.");
            toasts.Push("Gathered", "+1 Logs", NotificationKind.Success);
            toasts.Push("Level Up!", "Woodcutting is now level 2.", NotificationKind.Success);
            toasts.Update(0.5f);
            toasts.Draw(sb, font);
        });

        Render(game, Path.Combine(dir, "2-inventory-spellbook.png"), (sb, font) =>
        {
            DrawHud(sb, font);
            var dragDrop = new DragDropManager();
            var inventoryWindow = new InventoryWindow();
            inventoryWindow.Bind(dragDrop, inventory);
            inventoryWindow.Open();
            inventoryWindow.MoveTo(new Point(60, 150));
            inventoryWindow.Draw(sb, font);

            var spellBook = new SpellBookWindow();
            spellBook.Bind(dragDrop);
            spellBook.Open();
            spellBook.MoveTo(new Point(470, 150));
            spellBook.Draw(sb, font);

            var character = new CharacterWindow();
            character.Bind(() => stats, () => "Akiru", () => skills);
            character.Open();
            character.MoveTo(new Point(900, 150));
            character.Draw(sb, font);
        });

        Render(game, Path.Combine(dir, "3-esc-menu.png"), (sb, font) =>
        {
            DrawHud(sb, font);
            var menu = new EscMenuOverlay
            {
                OnOpenCharacter = () => { }, OnOpenSpellBook = () => { }, OnOpenInventory = () => { },
                OnOpenSkills = () => { }, OnBuildHouse = () => { }, OnLogout = () => { },
            };
            menu.Open();
            menu.Draw(sb, font, ["FPS: 144", "Server: 127.0.0.1:8080", "Players online: 3"]);
        });

        Render(game, Path.Combine(dir, "4-social-tooltips.png"), (sb, font) =>
        {
            DrawHud(sb, font);
            var friends = new FriendsWindow();
            var friendList = new PlayerFriends();
            friendList.Apply(new FriendsData
            {
                Friends =
                [
                    new FriendEntryState { AccountId = 2, CharacterId = 12, Name = "Rowan", Online = true },
                    new FriendEntryState { AccountId = 3, CharacterId = 13, Name = "Elowen the Unbroken", Online = false },
                    new FriendEntryState { AccountId = 4, CharacterId = 14, Name = "Brakka", PendingIn = true, Online = true },
                ],
            });
            friendList.SelectFriend(friendList.Entries[0]);
            friends.Bind(friendList, () => 1, (_, _) => { }, (_, _) => { }, _ => { });
            friends.Open();
            friends.MoveTo(new Point(120, 150));
            friends.Draw(sb, font);

            var context = new PlayerContextMenuOverlay();
            context.Open(new Point(860, 220), 2, "Rowan", canAddFriend: true, canWhisper: true);
            context.Draw(sb, font);

            if (ItemCatalog.Get("health_potion") is { } potion)
                AbilityTooltipDraw.DrawItem(sb, font, potion, new Rectangle(860, 480, 40, 40), new Point(Width, Height));
        });

        RenderWidgets(game, dir);
        RenderOverlays(game, dir);
        RenderHomestead(game, dir);
    }

    /// <summary>Furnished interior (with decorate overlay) and the yard with a player behind the cottage.</summary>
    private static void RenderHomestead(DeathbornGame game, string dir)
    {
        var house = new HousePlotZone
        {
            Id = 1, OwnerId = 1, OwnerName = "Akiru", Center = Vector2.Zero,
            Furniture =
            [
                new() { Type = "rug", Position = new(0, 4) },
                new() { Type = "bed", Position = new(-168, -36) },
                new() { Type = "dresser", Position = new(-112, -56) },
                new() { Type = "plant", Position = new(-96, 88) },
                new() { Type = "bookshelf", Position = new(-48, -54) },
                new() { Type = "sofa", Position = new(0, -24) },
                new() { Type = "armchair", Position = new(40, -24) },
                new() { Type = "lamp", Position = new(48, -52) },
                new() { Type = "piano", Position = new(-16, 96) },
                new() { Type = "kitchen", Position = new(136, -48) },
                new() { Type = "table", Position = new(128, 24) },
                new() { Type = "chair", Position = new(104, 24) },
                new() { Type = "fireplace", Position = new(168, -48) },
            ],
        };
        var player = new PlayerEntity { Id = 1, Name = "Akiru", IsLocal = true, Position = new(24, 60) };

        Render(game, Path.Combine(dir, "9-interior.png"), (sb, font) =>
        {
            const float zoom = 2.5f;
            var screenCenter = new Vector2(Width / 2f, Height / 2f + 20);
            Rendering.HouseInteriorRenderer.Draw(sb, font, house, Vector2.Zero, screenCenter, zoom, false, "Your Homestead");
            foreach (var item in house.Furniture)
                Rendering.HouseRenderer.DrawFurnitureItem(sb, item, house.Center, Vector2.Zero, screenCenter, zoom, 1f);
            player.Draw(sb, font, screenCenter + player.Position * zoom, zoom);
            var decorate = new HousingDecorateOverlay();
            decorate.Activate();
            decorate.Draw(sb, font);
        });

        Render(game, Path.Combine(dir, "10-yard.png"), (sb, font) =>
        {
            const float positionZoom = 6f;
            const float visualZoom = positionZoom * 0.5f;
            var camera = new Vector2(0, -10);
            var screenCenter = new Vector2(Width / 2f, Height / 2f);
            Rendering.HouseRenderer.Draw(sb, camera, screenCenter, positionZoom, visualZoom, [house]);
            var cells = new List<Rendering.FarmRpgFenceSprites.FenceCell>();
            Rendering.FarmRpgFenceSprites.AppendCells(house.Center, cells);
            foreach (var cell in cells)
                Rendering.FarmRpgFenceSprites.DrawCell(sb, cell, camera, screenCenter, positionZoom);
            // Player behind the cottage, cottage faded as the game does.
            var behind = new PlayerEntity { Id = 2, Name = "Akiru", IsLocal = true, Position = new(6, -30) };
            behind.Draw(sb, font, screenCenter + (behind.Position - camera) * positionZoom, visualZoom);
            Rendering.HouseRenderer.DrawCottage(sb, house.Center, camera, screenCenter, positionZoom, visualZoom, 0.45f);
        });
    }

    private static void RenderWidgets(DeathbornGame game, string dir)
    {
        Render(game, Path.Combine(dir, "5-widgets-death.png"), (sb, font) =>
        {
            var x = 60;
            var y = 60;
            var field = new TextField { Placeholder = "Email", Bounds = new Rectangle(x, y, 320, 36) };
            field.Draw(sb, font);
            var check = new Checkbox { Label = "Remember email and password", Checked = true, BoxBounds = new Rectangle(x, y + 56, 20, 20) };
            check.Draw(sb, font, false);
            var login = new Button { Label = "Login", Bounds = new Rectangle(x, y + 96, 150, 36) };
            login.Draw(sb, font, false);
            var register = new Button { Label = "Create account", Bounds = new Rectangle(x + 170, y + 96, 150, 36) };
            register.Draw(sb, font, true);

            var menu = new EscMenuOverlay { OnLogout = () => { }, OnNewLife = () => { } };
            menu.SetDeathMenuMode(true);
            menu.Open();
            menu.Draw(sb, font);
        });
    }

    private static void RenderOverlays(DeathbornGame game, string dir)
    {
        Render(game, Path.Combine(dir, "6-fishing-housing.png"), (sb, font) =>
        {
            var fishing = new FishingMinigameOverlay();
            fishing.Start("spot", "Riverbank", 6);
            fishing.Draw(sb, font);
            var decorate = new HousingDecorateOverlay();
            decorate.Activate();
            decorate.Draw(sb, font);
        });

        Render(game, Path.Combine(dir, "7-death.png"), (sb, font) =>
        {
            var death = new DeathGhostOverlay();
            death.SetDeathInfo("Slain by Rowan", "Total level 75, 2h 14m survived, 3 kills");
            death.Draw(sb, font);
        });

        Render(game, Path.Combine(dir, "8-disconnect.png"), (sb, font) =>
        {
            var disconnect = new ServerDisconnectOverlay();
            disconnect.Draw(sb, font);
        });
    }

    private static void Render(DeathbornGame game, string path, Action<SpriteBatch, SpriteFont> draw)
    {
        var device = game.GraphicsDevice;
        using var target = new RenderTarget2D(device, Width, Height);
        device.SetRenderTarget(target);
        device.Clear(Grass);
        var sb = game.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp);
        draw(sb, game.Font);
        sb.End();
        device.SetRenderTarget(null);
        using var file = File.Create(path);
        target.SaveAsPng(file, Width, Height);
    }

    private static PlayerSkills SampleSkills()
    {
        var skills = new PlayerSkills();
        var xp = new Dictionary<string, long>
        {
            ["attack"] = 1_850, ["strength"] = 2_400, ["defense"] = 900, ["hitpoints"] = 3_100,
            ["woodcutting"] = 38, ["mining"] = 0, ["fishing"] = 640, ["farming"] = 1_200,
            ["cooking"] = 410, ["smithing"] = 0,
        };
        skills.ApplySnapshot(xp, xp.Values.Sum());
        return skills;
    }

    private static PlayerInventory SampleInventory()
    {
        var inventory = new PlayerInventory();
        inventory.ApplyFromServer(
        [
            new InventoryItemState { Slot = 0, ItemId = "health_potion", Count = 5 },
            new InventoryItemState { Slot = 1, ItemId = "bandage", Count = 3 },
            new InventoryItemState { Slot = 2, ItemId = "logs", Count = 12 },
            new InventoryItemState { Slot = 3, ItemId = "fishing_rod", Count = 1 },
            new InventoryItemState { Slot = 4, ItemId = "worm_bait", Count = 20 },
            new InventoryItemState { Slot = 6, ItemId = "bread", Count = 2 },
            new InventoryItemState { Slot = 7, ItemId = "copper_ore", Count = 4 },
            new InventoryItemState { Slot = 8, ItemId = "iron_bar", Count = 1 },
            new InventoryItemState { Slot = 9, ItemId = "bone", Count = 2 },
            new InventoryItemState { Slot = 10, ItemId = "slime_gel", Count = 6 },
            new InventoryItemState { Slot = 11, ItemId = "boss_trophy", Count = 1 },
        ]);
        return inventory;
    }
}
