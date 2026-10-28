using System.Collections.Generic;
using AMD.Schola;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// Demo game: "Return to Base".
/// Spawns several drones, each a real ScholaAgent (PositionSensor + TranslateActuator).
/// One trained ONNX model (SentisPolicy) decides every drone's move, and one SimpleStepper
/// runs observe -> think -> act for all of them. Click a drone to knock it away or drag it; it flies back.
/// Put this on an empty GameObject and assign Model (move_to_origin.onnx).
/// AI usage: I used GPT-6 Astra to help generate this test scene.
/// </summary>
public class ReturnToBaseGame : MonoBehaviour
{
    [SerializeField]
    ModelAsset m_Model;

    [SerializeField, Range(1, 12)]
    int m_DroneCount = 6;

    [SerializeField, Range(1f, 60f)]
    float m_StepsPerSecond = 15f;

    // Must match MAX_STEP and START_RANGE in train_move_to_origin.py.
    const float MaxStep = 0.2f;
    const float SpawnRange = 5f;
    const float HomeDistance = 0.4f;

    readonly List<ScholaAgent> m_Drones = new List<ScholaAgent>();
    readonly List<Renderer> m_DroneRenderers = new List<Renderer>();

    SimpleStepper m_Stepper;
    SentisPolicy m_SentisPolicy;
    string m_BrainName = "none";
    float m_StepTimer;
    int m_Knocks;
    int m_Drags;

    const float DragThresholdPixels = 6f;
    Transform m_Grabbed;
    bool m_IsDragging;
    Vector3 m_PressPosition;
    Vector3 m_DragTarget;
    Plane m_DragPlane;

    void Start()
    {
        SetUpCamera();
        CreateBase();

        for (var i = 0; i < m_DroneCount; i++)
        {
            m_Drones.Add(CreateDrone(i));
        }

        // All drones share the same sensors and actuators, so one definition fits all.
        var first = m_Drones[0];
        var definition = new InteractionDefinition(first.Define(), first.DefineActionSpace());
        var policy = CreatePolicy(definition);

        var agents = new List<IAgent>();
        foreach (var drone in m_Drones)
        {
            agents.Add(drone);
        }

        m_Stepper = new SimpleStepper();
        if (!m_Stepper.Init(agents, policy))
        {
            Debug.LogError("ReturnToBase: stepper failed to start, see the error above.");
            m_Stepper = null;
        }
    }

    void Update()
    {
        HandleInput();

        if (m_Stepper != null)
        {
            m_StepTimer += Time.deltaTime;
            var interval = 1f / m_StepsPerSecond;
            while (m_StepTimer >= interval)
            {
                m_StepTimer -= interval;
                if (!m_Stepper.Step())
                {
                    Debug.LogError("ReturnToBase: step failed, stopping.");
                    m_Stepper = null;
                    break;
                }
            }
        }

        // The held drone follows the mouse; this runs after the step so the hand wins
        // while dragging, and the policy takes over again on release.
        if (m_Grabbed != null && m_IsDragging)
        {
            m_Grabbed.position = m_DragTarget;
        }

        UpdateColours();
    }

    void OnDestroy()
    {
        if (m_SentisPolicy != null)
        {
            m_SentisPolicy.Dispose();
        }
    }

    IPolicy CreatePolicy(InteractionDefinition definition)
    {
        if (m_Model != null)
        {
            m_SentisPolicy = new SentisPolicy(m_Model);
            if (m_SentisPolicy.Init(definition))
            {
                m_BrainName = "Trained ONNX model (" + m_Model.name + ")";
                // The exported model does not clip its outputs, but it was trained with
                // moves clipped to +/- MaxStep, so apply the same limit here.
                return new ClampedPolicy(m_SentisPolicy, MaxStep);
            }

            Debug.LogWarning("ReturnToBase: model rejected, falling back to the stand-in policy.");
            m_SentisPolicy.Dispose();
            m_SentisPolicy = null;
        }

        m_BrainName = "Stand-in policy (no model assigned)";
        var fallback = new MoveToOriginPolicy();
        fallback.Init(definition);
        return fallback;
    }

    void HandleInput()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        var held = mouse != null && mouse.leftButton.isPressed;
        var released = mouse != null && mouse.leftButton.wasReleasedThisFrame;
        var mousePosition = mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
        var scatter = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        var pressed = Input.GetMouseButtonDown(0);
        var held = Input.GetMouseButton(0);
        var released = Input.GetMouseButtonUp(0);
        var mousePosition = Input.mousePosition;
        var scatter = Input.GetKeyDown(KeyCode.Space);
#endif

        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        // Press on a drone: grab it. It becomes a drag once the mouse moves a few pixels.
        if (pressed)
        {
            var ray = camera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var drone = hit.collider.GetComponent<ScholaAgent>();
                if (drone != null)
                {
                    m_Grabbed = drone.transform;
                    m_PressPosition = mousePosition;
                    m_IsDragging = false;
                    m_DragPlane = new Plane(Vector3.up, drone.transform.position);
                }
            }
        }

        if (m_Grabbed != null && held)
        {
            if (!m_IsDragging && (mousePosition - m_PressPosition).magnitude > DragThresholdPixels)
            {
                m_IsDragging = true;
            }

            if (m_IsDragging)
            {
                // Move the drone along a flat plane at the height it was grabbed.
                var ray = camera.ScreenPointToRay(mousePosition);
                if (m_DragPlane.Raycast(ray, out var distance))
                {
                    var point = ray.GetPoint(distance);
                    point.x = Mathf.Clamp(point.x, -SpawnRange * 1.5f, SpawnRange * 1.5f);
                    point.z = Mathf.Clamp(point.z, -SpawnRange * 1.5f, SpawnRange * 1.5f);
                    m_DragTarget = point;
                }
            }
        }

        if (m_Grabbed != null && released)
        {
            // A press and release without moving is a click: knock the drone away.
            if (m_IsDragging)
            {
                m_Drags++;
            }
            else
            {
                KnockAway(m_Grabbed);
            }

            m_Grabbed = null;
            m_IsDragging = false;
        }

        if (scatter)
        {
            foreach (var drone in m_Drones)
            {
                KnockAway(drone.transform);
            }
        }
    }

    void KnockAway(Transform drone)
    {
        drone.position = RandomSpawnPoint();
        m_Knocks++;
    }

    static Vector3 RandomSpawnPoint()
    {
        // Keep drones above the base so they always fly down/in toward it.
        var point = new Vector3(
            UnityEngine.Random.Range(-SpawnRange, SpawnRange),
            UnityEngine.Random.Range(1f, 4f),
            UnityEngine.Random.Range(-SpawnRange, SpawnRange));
        if (point.magnitude < 2f)
        {
            point = point.normalized * 3f;
        }

        return point;
    }

    ScholaAgent CreateDrone(int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Drone " + (index + 1);
        go.transform.localScale = Vector3.one * 0.6f;
        go.transform.position = RandomSpawnPoint();

        var sensor = go.AddComponent<PositionSensor>();
        var actuator = go.AddComponent<TranslateActuator>();
        var agent = go.AddComponent<ScholaAgent>();
        agent.SetSensors(new[] { sensor });
        agent.SetActuators(new[] { actuator });
        sensor.InitSensor();
        actuator.InitActuator();

        m_DroneRenderers.Add(go.GetComponent<Renderer>());
        return agent;
    }

    static void CreateBase()
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Base";
        pad.transform.position = new Vector3(0f, -0.45f, 0f);
        pad.transform.localScale = new Vector3(2f, 0.05f, 2f);
        Destroy(pad.GetComponent<Collider>());
        pad.GetComponent<Renderer>().material.color = new Color(0.2f, 0.45f, 0.9f);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = Vector3.one * 2f;
        Destroy(ground.GetComponent<Collider>());
        ground.GetComponent<Renderer>().material.color = new Color(0.85f, 0.84f, 0.8f);
    }

    static void SetUpCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.transform.position = new Vector3(0f, 9f, -11f);
        camera.transform.LookAt(new Vector3(0f, 0.5f, 0f));
    }

    void UpdateColours()
    {
        for (var i = 0; i < m_Drones.Count; i++)
        {
            var home = m_Drones[i].transform.position.magnitude < HomeDistance;
            m_DroneRenderers[i].material.color = home
                ? new Color(0.25f, 0.8f, 0.4f)
                : new Color(0.95f, 0.55f, 0.2f);
        }
    }

    int DronesHome()
    {
        var count = 0;
        foreach (var drone in m_Drones)
        {
            if (drone.transform.position.magnitude < HomeDistance)
            {
                count++;
            }
        }

        return count;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(20, 20, 900, 32), "Return to Base", new GUIStyle(style) { fontSize = 30, fontStyle = FontStyle.Bold });
        GUI.Label(new Rect(20, 60, 900, 30), "Brain: " + m_BrainName, style);
        GUI.Label(new Rect(20, 90, 900, 30), "Drones home: " + DronesHome() + " / " + m_Drones.Count + "    Knocks: " + m_Knocks + "    Drags: " + m_Drags, style);
        GUI.Label(new Rect(20, 120, 900, 30), "Click a drone to knock it away, or drag it somewhere.  Space: scatter all.", style);
    }

    /// <summary>
    /// Wraps a policy and clamps every Box action value to [-limit, limit].
    /// </summary>
    sealed class ClampedPolicy : IPolicy
    {
        readonly IPolicy m_Inner;
        readonly float m_Limit;

        public ClampedPolicy(IPolicy inner, float limit)
        {
            m_Inner = inner;
            m_Limit = limit;
        }

        public bool Init(InteractionDefinition definition)
        {
            return m_Inner.Init(definition);
        }

        public bool Think(Point observations, out Point actions)
        {
            if (!m_Inner.Think(observations, out actions))
            {
                return false;
            }

            if (actions is DictPoint dict)
            {
                foreach (var point in dict.Points.Values)
                {
                    if (point is BoxPoint box)
                    {
                        for (var i = 0; i < box.Values.Count; i++)
                        {
                            box.Values[i] = Mathf.Clamp(box.Values[i], -m_Limit, m_Limit);
                        }
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Fallback when no model is assigned: move a fraction of the way to the origin.
    /// </summary>
    sealed class MoveToOriginPolicy : IPolicy
    {
        public bool Init(InteractionDefinition definition)
        {
            return true;
        }

        public bool Think(Point observations, out Point actions)
        {
            actions = null;
            if (!(observations is DictPoint dict)
                || !dict.Points.TryGetValue("position", out var point)
                || !(point is BoxPoint position)
                || position.Values.Count != 3)
            {
                return false;
            }

            var move = new List<float>();
            for (var i = 0; i < 3; i++)
            {
                move.Add(Mathf.Clamp(-0.1f * position.Values[i], -MaxStep, MaxStep));
            }

            actions = new DictPoint { Points = { ["translate"] = new BoxPoint { Values = move } } };
            return true;
        }
    }
}
