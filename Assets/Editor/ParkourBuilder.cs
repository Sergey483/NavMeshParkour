using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Рівень 3: багаторівнева локація — земля, платформи-вежі, NavMesh Link і патруль зі стрибками.
public static class ParkourBuilder
{
    private const string ScenePath = "Assets/Scenes/Parkour.unity";

    [MenuItem("Parkour/Build Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material groundMat = MakeMaterial("Ground", new Color(0.5f, 0.58f, 0.45f));
        Material towerMat = MakeMaterial("Tower", new Color(0.55f, 0.5f, 0.45f));
        Material topMat = MakeMaterial("PlatformTop", new Color(0.85f, 0.75f, 0.45f));
        Material guardMat = MakeMaterial("Guard", new Color(0.9f, 0.25f, 0.2f));
        Material markerMat = MakeMaterial("Waypoint", new Color(0.2f, 0.6f, 1f));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        ground.isStatic = true;

        // Платформи-вежі різної висоти.
        Transform towers = new GameObject("Towers").transform;
        MakeTower(towers, "Tower_A", new Vector3(-10f, 0f, 8f), new Vector3(6f, 4f, 6f), towerMat, topMat);
        MakeTower(towers, "Tower_B", new Vector3(10f, 0f, 8f), new Vector3(6f, 6f, 6f), towerMat, topMat);
        MakeTower(towers, "Tower_C", new Vector3(0f, 0f, -10f), new Vector3(8f, 3f, 5f), towerMat, topMat);

        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        AssetDatabase.DeleteAsset("Assets/Scenes/Parkour_NavMesh.asset");
        AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/Parkour_NavMesh.asset");

        // NavMesh Link: земля ↔ верх кожної вежі (в обидва боки).
        Transform links = new GameObject("NavMeshLinks").transform;
        MakeLink(links, "Link_Ground_A", new Vector3(-10f, 0f, 3f), new Vector3(-10f, 4f, 6f));
        MakeLink(links, "Link_Ground_B", new Vector3(10f, 0f, 3f), new Vector3(10f, 6f, 6f));
        MakeLink(links, "Link_Ground_C", new Vector3(0f, 0f, -5.5f), new Vector3(0f, 3f, -8.5f));

        // Точки маршруту: частина на землі, частина на платформах.
        Transform route = new GameObject("Waypoints").transform;
        Transform[] points =
        {
            MakeWaypoint(route, "WP_1_Ground", new Vector3(-15f, 0f, -15f), markerMat),
            MakeWaypoint(route, "WP_2_TowerA", new Vector3(-10f, 4f, 9f), markerMat),
            MakeWaypoint(route, "WP_3_Ground", new Vector3(0f, 0f, 15f), markerMat),
            MakeWaypoint(route, "WP_4_TowerB", new Vector3(10f, 6f, 9f), markerMat),
            MakeWaypoint(route, "WP_5_Ground", new Vector3(15f, 0f, -15f), markerMat),
            MakeWaypoint(route, "WP_6_TowerC", new Vector3(0f, 3f, -10f), markerMat),
        };

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        guard.name = "Guard";
        guard.transform.position = new Vector3(-15f, 1f, -10f);
        guard.GetComponent<Renderer>().sharedMaterial = guardMat;
        Object.DestroyImmediate(guard.GetComponent<Collider>());
        NavMeshAgent agent = guard.AddComponent<NavMeshAgent>();
        agent.speed = 4f;
        agent.radius = 0.5f;
        agent.autoTraverseOffMeshLink = false;
        guard.AddComponent<AgentJumper>();

        WaypointPatrol patrol = guard.AddComponent<WaypointPatrol>();
        SerializedObject patrolObject = new SerializedObject(patrol);
        SerializedProperty waypoints = patrolObject.FindProperty("waypoints");
        waypoints.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
            waypoints.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        patrolObject.ApplyModifiedPropertiesWithoutUndo();

        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0f, 26f, -34f);
        camera.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.runInBackground = true;
        Debug.Log("[ParkourBuilder] Scene built");
    }

    private static void MakeTower(Transform parent, string name, Vector3 basePosition, Vector3 size, Material sideMat, Material topMat)
    {
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = name;
        tower.transform.SetParent(parent, false);
        tower.transform.position = basePosition + Vector3.up * size.y * 0.5f;
        tower.transform.localScale = size;
        tower.GetComponent<Renderer>().sharedMaterial = sideMat;
        tower.isStatic = true;

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "Top";
        top.transform.SetParent(tower.transform, false);
        top.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        top.transform.localScale = new Vector3(1.02f, 0.02f, 1.02f);
        top.GetComponent<Renderer>().sharedMaterial = topMat;
        Object.DestroyImmediate(top.GetComponent<Collider>());
    }

    private static void MakeLink(Transform parent, string name, Vector3 start, Vector3 end)
    {
        GameObject linkObject = new GameObject(name);
        linkObject.transform.SetParent(parent, false);
        NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
        link.startPoint = start;
        link.endPoint = end;
        link.width = 1.5f;
        link.bidirectional = true;
    }

    private static Transform MakeWaypoint(Transform parent, string name, Vector3 position, Material material)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.SetParent(parent, false);
        marker.transform.position = position + Vector3.up * 0.05f;
        marker.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
        marker.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        return marker.transform;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}
