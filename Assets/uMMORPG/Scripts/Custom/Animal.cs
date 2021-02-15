using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Animal : NetworkBehaviourNonAlloc, Interactable
{
    public string GetInteractionText() { return "Pet"; }


    public void OnInteractClient(Player player)
    {
        Debug.Log("You pet the " + gameObject.name);
        player.animator.SetBool("Pet Animal", true);
        Debug.Log(player.animator.GetBool("Pet Animal"));
        StartCoroutine(WaitToFinishPetting(player));
    }

    [Server]
    public void OnInteractServer(Player player)
    {
        
    }

    IEnumerator WaitToFinishPetting(Player player)
    {
        yield return new WaitForSeconds(1);
        player.animator.SetBool("Pet Animal", false);
        Debug.Log(player.animator.GetBool("Pet Animal"));
    }
}
