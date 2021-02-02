using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerNpcTeleport : NetworkBehaviourNonAlloc
{
    [Header("Components")]
    public Player player;

    [Command]
    public void CmdNpcTeleport()
    {
        // validate
        if (player.state == "IDLE" &&
            player.target != null &&
            player.target.health.current > 0 &&
            player.target is Npc &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange &&
            ((Npc)player.target).teleport.destination != null)
        {
            // using agent.Warp is recommended over transform.position
            // (the latter can cause weird bugs when using it with an agent)
            player.movement.Warp(((Npc)player.target).teleport.destination.position);

            // clear target. no reason to keep targeting the npc after we
            // teleported away from it
            player.target = null;
        }
    }
}
