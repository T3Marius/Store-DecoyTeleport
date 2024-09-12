using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using StoreApi;
using System.Collections.Generic;

namespace Store;

public class Item_DecoyTeleport : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[Store Module] DecoyTeleport";
    public override string ModuleVersion => "1.0";

    private IStoreApi? StoreApi { get; set; }

    // Dictionary to keep track of which decoys have already teleported
    private Dictionary<int, bool> decoyTeleported = new Dictionary<int, bool>();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get();

        if (StoreApi == null)
        {
            return;
        }

        StoreApi.RegisterType("DecoyTeleport", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
        RegisterEventHandler<EventDecoyFiring>(OnDecoyFiring);
    }

    public void OnMapStart() { }
    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        var StoreApi1 = IStoreApi.Capability.Get();
        if (StoreApi1 == null)
            return;

        List<KeyValuePair<string, Dictionary<string, string>>> items = StoreApi1.GetItemsByType("DecoyTeleport");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            if (item.Value.TryGetValue("DecoyModel", out var model))
            {
                manifest.AddResource(item.Value["model"]);
            }
        }
        RegisterEventHandler<EventDecoyFiring>(OnDecoyFiring);
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        CDecoyGrenade? decoy = player.GiveNamedItem<CDecoyGrenade>("weapon_decoy");

        if (decoy == null)
        {
            return false;
        }

        decoy.Globalname = "decoy_teleport";
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public HookResult OnDecoyFiring(EventDecoyFiring @event, GameEventInfo info)
    {
        if (@event.Userid == null)
            return HookResult.Continue;

        var controller = @event.Userid;
        var entityIndex = controller.Index;

        var pDecoyFiring = @event;
        var playerPawn = controller.PlayerPawn.Value;

        if (playerPawn == null)
            return HookResult.Continue;

        if (!decoyTeleported.TryGetValue(pDecoyFiring.Entityid, out bool hasTeleported) || !hasTeleported)
        {
            playerPawn.Teleport(new Vector(pDecoyFiring.X, pDecoyFiring.Y, pDecoyFiring.Z), playerPawn.AbsRotation,
                playerPawn.AbsVelocity);

            decoyTeleported[pDecoyFiring.Entityid] = true;
        }

        var decoyIndex = NativeAPI.GetEntityFromIndex(pDecoyFiring.Entityid);

        if (decoyIndex == IntPtr.Zero)
            return HookResult.Continue;

        return HookResult.Continue;
    }
}