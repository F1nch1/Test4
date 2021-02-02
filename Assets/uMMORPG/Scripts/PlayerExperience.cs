using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerChat))]
[RequireComponent(typeof(PlayerParty))]
public class PlayerExperience : Experience
{
    [Header("Components")]
    public PlayerChat chat;
    public PlayerParty party;

    [Server]
    public override void OnDeath()
    {
        // call base logic
        base.OnDeath();

        // send an info chat message
        string message = "You died and lost experience.";
        chat.TargetMsgInfo(message);
    }

    // events //////////////////////////////////////////////////////////////////
    [Server]
    public void OnKilledEnemy(Entity victim)
    {
        // killed a monster
        if (victim is Monster)
        {
            // gain exp if not in a party or if in a party without exp share
            if (!party.InParty() || !party.party.shareExperience)
                current += BalanceExpReward(((Monster)victim).rewardExperience, level.current, victim.level.current);
        }
    }
}
