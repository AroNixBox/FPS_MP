using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public List<Transform> teamBlueSpawnPoints = new List<Transform>();
    public List<Transform> teamRedSpawnPoints = new List<Transform>();

    // Events
    public delegate void RequestSpawnPointHandler(PlayerType team, out Transform spawnPoint);
    public event RequestSpawnPointHandler OnRequestSpawnPoint;

    private void Awake()
    {
        OnRequestSpawnPoint += HandleRequestSpawnPoint;
    }

    private void HandleRequestSpawnPoint(PlayerType team, out Transform spawnPoint)
    {
        spawnPoint = null;

        if (team == PlayerType.TeamBlue && teamBlueSpawnPoints.Count > 0)
        {
            spawnPoint = teamBlueSpawnPoints[0];
            teamBlueSpawnPoints.RemoveAt(0);
        }
        else if (team == PlayerType.TeamRed && teamRedSpawnPoints.Count > 0)
        {
            spawnPoint = teamRedSpawnPoints[0];
            teamRedSpawnPoints.RemoveAt(0);
        }
    }
    public Transform RequestSpawnPointForTeam(PlayerType team)
    {
        if (OnRequestSpawnPoint != null)
        {
            Transform spawnPoint;
            OnRequestSpawnPoint(team, out spawnPoint);
            return spawnPoint;
        }

        return null;
    }

}