using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-100)]
public sealed class EndlessCorridor : MonoBehaviour
{
    [SerializeField] private Transform stageSection;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPoints;

    
    private const float SectionLength = 48f;
    private const int SectionsPerSide = 3;
    private readonly Transform[] sections = new Transform[SectionsPerSide * 2 + 1];
    private readonly Transform[] propGroups = new Transform[SectionsPerSide * 2 + 1];
    private CorridorPropSet propSet;
    private int propSeed;
    private int centreIndex;
    private Vector3 initialSpawnPosition;
    private float initialPlayerX;
    private NavMeshData navigation;
    private NavMeshDataInstance navigationInstance;

    private void Awake()
    {
        initialPlayerX = player.position.x;
        initialSpawnPosition = spawnPoints.position;
        sections[0] = stageSection;
        for (int i = 1; i < sections.Length; i++)
            sections[i] = Instantiate(stageSection, transform);

        propSet = Resources.Load<CorridorPropSet>("CorridorPropSet");
        if (propSet == null)
            Debug.LogWarning("CorridorPropSet is missing from Resources.", this);
        propSeed = System.Guid.NewGuid().GetHashCode();
        PlaceSections();
        Physics.SyncTransforms();

        
        var sources = new List<NavMeshBuildSource>();
        // The base floor repeats. Props carve their own holes at each section's actual placement.
        var markups = new List<NavMeshBuildMarkup>();
        foreach (var group in propGroups)
            markups.Add(new NavMeshBuildMarkup { root = group, ignoreFromBuild = true });
        NavMeshBuilder.CollectSources(transform, ~0, NavMeshCollectGeometry.PhysicsColliders,
            0, markups, sources);
        var colliders = GetComponentsInChildren<Collider>();
        Bounds navigationBounds = colliders[0].bounds;
        foreach (var collider in colliders)
            navigationBounds.Encapsulate(collider.bounds);
        navigationBounds.center -= transform.position;
        navigationBounds.Expand(2f);

        navigation = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources,
            navigationBounds, transform.position, Quaternion.identity);
        navigationInstance = NavMesh.AddNavMeshData(navigation, transform.position, Quaternion.identity);
    }

    private void Update()
    {
        float travel = player.position.x - initialPlayerX;
        spawnPoints.position = initialSpawnPosition + Vector3.right * travel;

        float sectionOffset = (player.position.x - transform.position.x) / SectionLength;
       
        if (Mathf.Abs(sectionOffset - centreIndex) < 0.75f)
            return;

        centreIndex = Mathf.RoundToInt(sectionOffset);
        PlaceSections();
        MoveNavigation();
    }

    private void PlaceSections()
    {
        for (int index = centreIndex - SectionsPerSide; index <= centreIndex + SectionsPerSide; index++)
        {
            int slot = (index % sections.Length + sections.Length) % sections.Length;
            Vector3 position = Vector3.right * (index * SectionLength);
            if (sections[slot].localPosition == position && propGroups[slot] != null)
                continue;

            sections[slot].localPosition = position;
            if (propGroups[slot] == null)
            {
                propGroups[slot] = new GameObject("Props").transform;
                propGroups[slot].SetParent(sections[slot], false);
            }
            if (propSet != null)
                propSet.Populate(propGroups[slot], index, propSeed, SectionLength);
        }
    }

    private void MoveNavigation()
    {
        Vector3 centre = transform.position + Vector3.right * (centreIndex * SectionLength);
        navigationInstance.Remove();
        navigationInstance = NavMesh.AddNavMeshData(navigation, centre, Quaternion.identity);
        float halfLength = sections.Length * SectionLength * 0.5f;
        foreach (var agent in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
        {
            if (!agent.isActiveAndEnabled || agent.gameObject.scene != gameObject.scene)
                continue;

            Vector3 position = agent.transform.position;
            position.x = Mathf.Clamp(position.x, centre.x - halfLength + 2f, centre.x + halfLength - 2f);
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, agent.areaMask))
                agent.Warp(hit.position);
        }
    }

    private void OnDestroy()
    {
        if (navigationInstance.valid)
            navigationInstance.Remove();
        if (navigation != null)
            Destroy(navigation);
    }
}
