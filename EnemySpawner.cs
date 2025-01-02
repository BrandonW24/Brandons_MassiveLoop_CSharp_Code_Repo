using ML.SDK;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public MLClickable StartButton;
    public MLClickable EndButton;
    public GameObject EnemyPrefab;
    public Transform[] RandomEnemySpawnPoints; // Enemy spawn points
    public Transform[] RandomEnemyWayPoints;  // Enemy patrol waypoints

    [Header("Wave Settings")]
    public int enemiesPerWave = 5;            // Number of enemies per wave
    public float timeBetweenWaves = 10f;      // Time delay between waves
    public float timeBetweenEnemySpawns = 1f; // Time delay between enemy spawns in a wave

    private bool spawning = false;

    const string EVENT_SPAWNENEMY = "EventSpawnEnemy";
    private EventToken tokenSpawnEnemy;

    const string EVENT_SET_ENEMY_WAYPOINTS = "EventSetWayPoints";
    private EventToken tokenWayPointSetup;

    public void OnNetworkSetupWayPoints(object[] args)
    {
        /*
        if (this == null || gameObject == null || gameObject.name == null || args[0] == null)
        {
            return;
        }*/

        Debug.Log("SETUP waypoints function called");

        Transform[] transferPoints = (Transform[])args[1];
        GameObject PassedinEnemy = (GameObject)args[0];

        if (transferPoints.Length < 3)
        {
            Debug.LogError("Not enough waypoints to assign 3 random patrol points.");
            return;
        }

        Debug.Log($"Passed in way points : {transferPoints}");
        Debug.Log($"Passed in enemy  : {PassedinEnemy}");


        // Set the patrol points for the enemy
        EnemyAi.EnemyAi enemyAI = (EnemyAi.EnemyAi)PassedinEnemy.GetComponent(typeof(EnemyAi.EnemyAi));
        if (enemyAI != null)
        {
            //  MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);

            enemyAI.PatrolPoints = transferPoints; // Assign the selected 3 patrol points to the enemy
            Debug.Log("Assigned 3 random patrol points to enemy.");
            //   enemyAI.RandomizeRobotColors();
            //  enemyAI.TargetTransform = nearestPlayer.PlayerRoot.transform;

        }

    }

    public void OnNetworkSpawnObject(object[] args)
    {
        /*
        if (this == null || gameObject == null || gameObject.name == null || args[0] == null)
        {
            return;
        }*/

        SpawnObject();
    }

    public void OnPlayerClickStart_EnemySpawn(MLPlayer player)
    {
        /* if (!spawning)
         {
             spawning = true;
             StartCoroutine(SpawnWaves());
         }*/

        this.InvokeNetwork(EVENT_SPAWNENEMY, EventTarget.All, null);
    }

    public void OnPlayerClickEndGame(MLPlayer player)
    {
        spawning = false;
    }

    // Modify SpawnObject() to set the reference to the parent script
    public void SpawnObject()
    {
      //  Count += 1;
        if (MassiveLoopClient.IsMasterClient)
        {
            Debug.Log("Master client attempting to spawn object");

            int spawnIndex = Random.Range(0, RandomEnemySpawnPoints.Length);
            GameObject enemy = Instantiate(EnemyPrefab, RandomEnemySpawnPoints[spawnIndex].position, Quaternion.identity);

            // Select three random patrol points
            Transform[] randomPatrolPoints = new Transform[3];

            for (int i = 0; i < 3; i++)
            {
                int randomIndex = Random.Range(0, RandomEnemyWayPoints.Length);
                randomPatrolPoints[i] = RandomEnemyWayPoints[randomIndex];
            }

            if (RandomEnemyWayPoints == null || RandomEnemyWayPoints.Length == 0)
            {
                Debug.LogError("RandomEnemyWayPoints is null or empty. Ensure waypoints are assigned in the spawner.");
                return;
            }

            this.InvokeNetwork(EVENT_SET_ENEMY_WAYPOINTS, EventTarget.All, null, enemy, randomPatrolPoints[0] , randomPatrolPoints[1], randomPatrolPoints[2]);

            
        }
    }


    private IEnumerator SpawnWaves()
    {
        while (spawning)
        {
            // Spawn a single wave
            for (int i = 0; i < enemiesPerWave; i++)
            {
                if (!spawning) break; // Stop spawning if the game ends

                // SpawnEnemy();
                this.InvokeNetwork(EVENT_SPAWNENEMY, EventTarget.All, null);

                // Wait before spawning the next enemy in the wave
                yield return new WaitForSeconds(timeBetweenEnemySpawns);
            }

            // Wait for the next wave
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnEnemy()
    {
        // Spawn enemy at a random spawn point
        int spawnIndex = Random.Range(0, RandomEnemySpawnPoints.Length);
        GameObject enemy = Instantiate(EnemyPrefab, RandomEnemySpawnPoints[spawnIndex].position, Quaternion.identity);

        // Select three random patrol points
        Transform[] randomPatrolPoints = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, RandomEnemyWayPoints.Length);
            randomPatrolPoints[i] = RandomEnemyWayPoints[randomIndex];
        }

        if (RandomEnemyWayPoints == null || RandomEnemyWayPoints.Length == 0)
        {
            Debug.LogError("RandomEnemyWayPoints is null or empty. Ensure waypoints are assigned in the spawner.");
            return;
        }

        if (RandomEnemyWayPoints.Length < 3)
        {
            Debug.LogError("Not enough waypoints to assign 3 random patrol points.");
            return;
        }

        // Set the patrol points for the enemy
        EnemyAi.EnemyAi enemyAI = (EnemyAi.EnemyAi)enemy.GetComponent(typeof(EnemyAi.EnemyAi));
        if (enemyAI != null)
        {
          //  MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);

            enemyAI.PatrolPoints = randomPatrolPoints; // Assign the selected 3 patrol points to the enemy
            Debug.Log("Assigned 3 random patrol points to enemy.");
            //   enemyAI.RandomizeRobotColors();
            //  enemyAI.TargetTransform = nearestPlayer.PlayerRoot.transform;

        }
    }
    void Start()
    {
        StartButton.OnPlayerClick.AddListener(OnPlayerClickStart_EnemySpawn);
        EndButton.OnPlayerClick.AddListener(OnPlayerClickEndGame);

        tokenSpawnEnemy = this.AddEventHandler(EVENT_SPAWNENEMY, OnNetworkSpawnObject);
        tokenWayPointSetup = this.AddEventHandler(EVENT_SET_ENEMY_WAYPOINTS, OnNetworkSetupWayPoints);

    }

    void Update()
    {
        // Update logic if needed
    }
}
