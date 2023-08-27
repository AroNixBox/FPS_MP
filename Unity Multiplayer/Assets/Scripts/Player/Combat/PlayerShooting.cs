using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField]
    private float shootingRange = 100f; // Wie weit der Schuss reicht
    [SerializeField]
    private int damageAmount = 10; // Der Schaden, den der Schuss verursacht
    [SerializeField]
    private Transform barrelEnd;  // Das Ende des Laufs der Waffe


    void Update()
    {
        // Überprüft, ob dieser Spieler dem lokalen Client gehört
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0)) // Linksklick
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        // Erstellt einen Strahl, der vom Ende des Laufs nach vorne in die Blickrichtung des Spielers geht
        Ray ray = new Ray(barrelEnd.position, barrelEnd.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shootingRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Informiert den Server über den Schuss und an welchen Spieler er gerichtet war
                PlayerShotServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
            }
            else
            {
                Debug.Log(hit.collider.gameObject);
            }
        }
    }


    [ServerRpc]
    private void PlayerShotServerRpc(ulong targetPlayerId)
    {
        // Der Server validiert den Schuss und wendet ggf. Schaden an
        var targetPlayer = NetworkManager.SpawnManager.SpawnedObjects[targetPlayerId].GetComponent<PlayerHealth>();

        if (targetPlayer)
        {
            // Diese Überprüfung sollte durch tatsächliche Validierungslogik ersetzt werden
            if (IsValidShot(targetPlayer))
            {
                targetPlayer.TakeDamage(damageAmount);
            }

            // Sendet eine Bestätigung an alle Clients oder nur an den schießenden Client,
            // abhängig von der Spiellogik
            ShotValidatedClientRpc();
        }
    }

    private bool IsValidShot(PlayerHealth targetPlayer)
    {
        // Hier könnten Sie komplexere Validierungen hinzufügen,
        // z.B. ob der getroffene Spieler zu dem Zeitpunkt wirklich an dieser Stelle war
        return true;
    }

    [ClientRpc]
    private void ShotValidatedClientRpc()
    {
        Debug.Log("SpawnEffectss");
        // Dies könnte verwendet werden, um z.B. spezielle Effekte auf dem Client zu zeigen
        // oder andere Client-seitige Logik auszuführen, wenn ein Schuss validiert wurde
    }
}
