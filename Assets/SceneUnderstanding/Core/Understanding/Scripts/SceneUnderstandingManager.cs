// Copyright (c) Microsoft Corporation. All rights reserved.
namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    //System
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    //TextMeshPro
    using TMPro;

    //Unity
    using UnityEngine;
    using UnityEngine.Events;
    

#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
#endif

    /// <summary>
    /// Different rendering modes available for scene objects.
    /// </summary>
    public enum RenderMode
    {
        Quad,
        QuadWithMask,
        Mesh,
        Wireframe
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HolograhicFrameData
    {
        public uint VersionNumber;
        public uint MaxNumberOfCameras;
        public IntPtr ISpatialCoordinateSystemPtr; // Windows::Perception::Spatial::ISpatialCoordinateSystem
        public IntPtr IHolographicFramePtr; // Windows::Graphics::Holographic::IHolographicFrame
        public IntPtr IHolographicCameraPtr; // // Windows::Graphics::Holographic::IHolographicCamera
    }

    public class SceneUnderstandingManager : MonoBehaviour
    {
        #region Public Variables

        [Header("Data Loader Mode")]
        [Tooltip("When enabled, the scene will run using a device (e.g Hololens). Otherwise, a previously saved, serialized scene will be loaded and served from your PC.")]
        public bool RunOnDevice = true;
        [Tooltip("The scene to load when not running on the device (e.g SU_Kitchen in Resources/SerializedScenesForPCPath).")]
        public List<TextAsset> SUSerializedScenePaths = new List<TextAsset>(0);

        [Header("Root GameObject")]
        [Tooltip("GameObject that will be the parent of all Scene Understanding related game objects. If field is left empty an empty gameobject named 'Root' will be created.")]
        public GameObject SceneRoot = null;

        [Header("Data Loader Parameters")]
        [Tooltip("Radius of the sphere around the camera, which is used to query the environment.")]
        [Range(5f, 100f)]
        public float BoundingSphereRadiusInMeters = 10.0f;
        [Tooltip("When enabled, the latest data from Scene Understanding data provider will be displayed periodically (controlled by the AutoRefreshIntervalInSeconds float).")]
        public bool AutoRefresh = true;
        [Tooltip("Interval to use for auto refresh, in seconds.")]
        [Range(1f, 60f)]
        public float AutoRefreshIntervalInSeconds = 10.0f;

        [Header("Render Mode")]
        [Tooltip("Type of visualization to use for scene objects.")]
        public RenderMode SceneObjectRenderMode = RenderMode.Mesh;
        [Tooltip("Level Of Detail for the scene objects.")]
        public SceneUnderstanding.SceneMeshLevelOfDetail RenderQuality = SceneUnderstanding.SceneMeshLevelOfDetail.Medium;

        [Header("Render Colors")]
        [Tooltip("Colors for the Scene Understanding Background objects")]
        public Color ColorForBackgroundObjs = new Color(0.953f, 0.475f, 0.875f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Wall objects")]
        public Color ColorForWallObjs = new Color(0.953f, 0.494f, 0.475f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Floor objects")]
        public Color ColorForFloorObjs = new Color(0.733f, 0.953f, 0.475f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Ceiling objects")]
        public Color ColorForCeilingObjs = new Color(0.475f, 0.596f, 0.953f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Plataform objects")]
        public Color ColorForPlatformsObjs = new Color(0.204f, 0.792f, 0.714f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Unknown objects")]
        public Color ColorForUnknownObjs = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        [Tooltip("Colors for the Scene Understanding Inferred objects")]
        public Color ColorForInferredObjs = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        [Tooltip("Colors for the World mesh")]
        public Color ColorForWorldObjs = new Color(0.0f, 1.0f, 1.0f, 1.0f);

        [Header("Materials")]
        [Tooltip("Material for scene object meshes.")]
        public Material SceneObjectMeshMaterial = null;
        [Tooltip("Material for scene object quads.")]
        public Material SceneObjectQuadMaterial = null;
        [Tooltip("Material for scene object mesh wireframes.")]
        public Material SceneObjectWireframeMaterial = null;
        [Tooltip("Material for scene objects (invisible).")]
        public Material TransparentOcclussion = null;

        [Header("Render Filters")]
        [Tooltip("Toggles display of all scene objects, except for the world mesh.")]
        public bool RenderSceneObjects = true;
        [Tooltip("Display text labels for the scene objects.")]
        public bool DisplayTextLabels = true;
        [Tooltip("Toggles display of large, horizontal scene objects, aka 'Platform'.")]
        public bool RenderPlatformSceneObjects = true;
        [Tooltip("Toggles the display of background scene objects.")]
        public bool RenderBackgroundSceneObjects = true;
        [Tooltip("Toggles the display of unknown scene objects.")]
        public bool RenderUnknownSceneObjects = true;
        [Tooltip("Toggles the display of the world mesh.")]
        public bool RenderWorldMesh = false;
        [Tooltip("When enabled, requests observed and inferred regions for scene objects. When disabled, requests only the observed regions for scene objects.")]
        public bool RequestInferredRegions = true;
        [Tooltip("Toggles the display of completely inferred scene objects.")]
        public bool RenderCompletelyInferredSceneObjects = true;

        [Header("Physics")]
        [Tooltip("Toggles the creation of objects with collider components")]
        public bool AddColliders = false;

        [Header("Occlussion")]
        [Tooltip("Toggle Ghost Mode, (invisible objects that still occlude)")]
        public bool isInGhostMode = false;

        [Header("Events")]
        [Tooltip("User function that get called when a Scene Understanding event happens")]
        public UnityEvent OnLoadStarted;
        [Tooltip("User function that get called when a Scene Understanding event happens")]
        public UnityEvent OnLoadFinished;

        #endregion

        #region Private Variables

        private readonly float minBoundingSphereRadiusInMeters = 5f;
        private readonly float maxBoundingSphereRadiusInMeters = 100f;
        private byte[] latestSUSceneData = null;
        private readonly object SUDataLock = new object();
        private Guid latestSceneGuid;
        private Guid lastDisplayedSceneGuid;
        private bool isDisplayInProgress = false;
        [HideInInspector]
        public float timeElapsedSinceLastAutoRefresh = 0.0f;
        private bool pcDisplayStarted = false;

        #endregion

        #region Unity Start and Update

        private async void Start()
        {
            SceneRoot = SceneRoot == null ? new GameObject("Root") : SceneRoot;

            if(RunOnDevice)
            {
                if(Application.isEditor)
                {
                    Debug.LogError("SceneUnderstandingManager.Start: Running in editor with a device is not supported.\n" +
                                   "To run on editor disable the 'RunOnDevice' Flag in the SceneUnderstandingManager Component");
                    return;
                }

                if (!SceneUnderstanding.SceneObserver.IsSupported())
                {
                    Debug.LogError("SceneUnderstandingDataProvider.Start: Scene Understanding not supported.");
                    return;
                }

                SceneObserverAccessStatus access = await SceneUnderstanding.SceneObserver.RequestAccessAsync();
                if (access != SceneObserverAccessStatus.Allowed)
                {
                    Debug.LogError("SceneUnderstandingDataProvider.Start: Access to Scene Understanding has been denied.\n" +
                                   "Reason: " + access);
                    return;
                }

                try
                {
#pragma warning disable CS4014
                    Task.Run(() => RetrieveDataContinuously());
#pragma warning restore CS4014
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void Update()
        {
            if(RunOnDevice)
            {
                if(AutoRefresh)
                {
                    timeElapsedSinceLastAutoRefresh += Time.deltaTime;
                    if(timeElapsedSinceLastAutoRefresh >= AutoRefreshIntervalInSeconds)
                    {
                        if(GetLatestSUSceneId() != lastDisplayedSceneGuid)
                        {
                            StartDisplay();
                        }
                        timeElapsedSinceLastAutoRefresh = 0.0f;
                    }
                }
            }
            else if(!pcDisplayStarted)
            {
                StartDisplay();
                pcDisplayStarted = true;
            }
        }

        #endregion

        #region Data Querying and Consumption

        private byte[] GetLatestSUScene()
        {
            byte[] SUDataToReturn = null;

            lock(SUDataLock)
            {
                if(latestSUSceneData != null)
                {
                    int SceneLength = latestSUSceneData.Length;
                    SUDataToReturn = new byte [SceneLength];
                    Array.Copy(latestSUSceneData,SUDataToReturn,SceneLength);
                }
            }

            return SUDataToReturn;
        }

        private Guid GetLatestSUSceneId()
        {
            Guid SUSceneIdToReturn;

            lock(SUDataLock)
            {
                SUSceneIdToReturn = latestSceneGuid;
            }

            return SUSceneIdToReturn;
        }

        /// <summary>
        /// Retrieves Scene Understanding data continuously from the runtime.
        /// </summary>
        private void RetrieveDataContinuously()
        {
            // At the beginning, retrieve only the observed scene object meshes.
            RetrieveData(BoundingSphereRadiusInMeters, false, true, false, false, SceneUnderstanding.SceneMeshLevelOfDetail.Coarse);

            while (true)
            {
                // Always request quads, meshes and the world mesh. SceneUnderstandingDisplayManager will take care of rendering only what the user has asked for.
                RetrieveData(BoundingSphereRadiusInMeters, true, true, RequestInferredRegions, true, RenderQuality);
            }
        }

        /// <summary>
        /// Calls into the Scene Understanding APIs, to retrieve the latest scene as a byte array.
        /// </summary>
        /// <param name="enableQuads">When enabled, quad representation of scene objects is retrieved.</param>
        /// <param name="enableMeshes">When enabled, mesh representation of scene objects is retrieved.</param>
        /// <param name="enableInference">When enabled, both observed and inferred scene objects are retrieved. Otherwise, only observed scene objects are retrieved.</param>
        /// <param name="enableWorldMesh">When enabled, retrieves the world mesh.</param>
        /// <param name="lod">If world mesh is enabled, lod controls the resolution of the mesh returned.</param>
        private void RetrieveData(float boundingSphereRadiusInMeters, bool enableQuads, bool enableMeshes, bool enableInference, bool enableWorldMesh, SceneUnderstanding.SceneMeshLevelOfDetail lod)
        {
            Debug.Log("SceneUnderstandingDataProvider.RetrieveData: Started.");

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                SceneUnderstanding.SceneQuerySettings querySettings;
                querySettings.EnableSceneObjectQuads = enableQuads;
                querySettings.EnableSceneObjectMeshes = enableMeshes;
                querySettings.EnableOnlyObservedSceneObjects = !enableInference;
                querySettings.EnableWorldMesh = enableWorldMesh;
                querySettings.RequestedMeshLevelOfDetail = lod;

                // Ensure that the bounding radius is within the min/max range.
                boundingSphereRadiusInMeters = Mathf.Clamp(boundingSphereRadiusInMeters, minBoundingSphereRadiusInMeters, maxBoundingSphereRadiusInMeters);

                SceneBuffer serializedScene = SceneUnderstanding.SceneObserver.ComputeSerializedAsync(querySettings, boundingSphereRadiusInMeters).GetAwaiter().GetResult();
                lock(SUDataLock)
                {
                    latestSUSceneData = new byte[serializedScene.Size];
                    serializedScene.GetData(latestSUSceneData);
                    latestSceneGuid = Guid.NewGuid();
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }

            stopwatch.Stop();
            Debug.Log(string.Format("SceneUnderstandingManager.RetrieveData: Completed. Radius: {0}; Quads: {1}; Meshes: {2}; Inference: {3}; WorldMesh: {4}; LOD: {5}; Bytes: {6}; Time (secs): {7};",
                                    boundingSphereRadiusInMeters,
                                    enableQuads,
                                    enableMeshes,
                                    enableInference,
                                    enableWorldMesh,
                                    lod,
                                    (latestSUSceneData == null ? 0 : latestSUSceneData.Length),
                                    stopwatch.Elapsed.TotalSeconds));
        }

        #endregion

        #region Display Data into Unity

        public void StartDisplay()
        {
            if(isDisplayInProgress)
            {
                Debug.Log("SceneUnderstandingManager.StartDisplay: Display is already in progress.");
                return;
            }

            isDisplayInProgress = true;
            StartCoroutine(DisplayData());
            //Run Callbacks for On Load Started
            OnLoadStarted.Invoke();
        }

        private IEnumerator DisplayData()
        {
            Debug.Log("SceneUnderstandingManager.DisplayData: About to display the latest set of Scene Objects");

            List<SceneUnderstanding.Scene> suScenes = new List<SceneUnderstanding.Scene>();

            if(RunOnDevice)
            {
                byte[] latestSceneSnapShot = GetLatestSUScene();
                Guid latestGuidSnapShot = GetLatestSUSceneId();
                suScenes.Add(SceneUnderstanding.Scene.Deserialize(latestSceneSnapShot));
                //This at the end
                lastDisplayedSceneGuid = latestGuidSnapShot;
            }
            else
            {
                foreach(TextAsset serializedScene in SUSerializedScenePaths)
                {
                    if(serializedScene != null)
                    {
                        byte[] sceneData = serializedScene.bytes;
                        suScenes.Add(SceneUnderstanding.Scene.Deserialize(sceneData));
                    }
                }
            }

            if(suScenes.Count > 0)
            {
                DestroyAllGameObjectsUnderParent(SceneRoot.transform);

                yield return null;

                foreach (SceneUnderstanding.Scene suScene in suScenes)
                {
                    System.Numerics.Matrix4x4 sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(suScene);

                    if(sceneToUnityTransformAsMatrix4x4 != null)
                    {
                        SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4, RunOnDevice);

                        if(!RunOnDevice)
                        {
                            OrientSceneForPC(SceneRoot, suScene);
                        }

                        IEnumerable<SceneUnderstanding.SceneObject> sceneObjects = suScene.SceneObjects;

                        int i = 0;
                        foreach (SceneUnderstanding.SceneObject sceneObject in sceneObjects)
                        {
                            if(DisplaySceneObject(sceneObject))
                            {
                                if(++i % 5 == 0)
                                {
                                    yield return null;
                                }
                            }
                        }
                    }
                }
            }

            isDisplayInProgress = false;

            Debug.Log("SceneUnderStandingManager.DisplayData: Display Completed");

            //Run CallBacks for Onload Finished
            OnLoadFinished.Invoke();
        }

        private bool DisplaySceneObject(SceneUnderstanding.SceneObject suObj)
        {
            if(suObj == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.DisplaySceneObj: Object is null");
                return false;
            }

            if(RenderSceneObjects == false && suObj.Kind != SceneUnderstanding.SceneObjectKind.World)
            {
                return false;
            }

            SceneUnderstanding.SceneObjectKind kind = suObj.Kind;
            switch(kind)
            {
                case SceneUnderstanding.SceneObjectKind.World:
                    if(!RenderWorldMesh)
                        return false;
                    break;
                case SceneUnderstanding.SceneObjectKind.Platform:
                    if(!RenderPlatformSceneObjects)
                        return false;
                    break;
                case SceneUnderstanding.SceneObjectKind.Background:
                    if(!RenderBackgroundSceneObjects)
                        return false;
                    break;
                case SceneUnderstanding.SceneObjectKind.Unknown:
                    if(!RenderUnknownSceneObjects)
                        return false;
                    break;
                case SceneUnderstanding.SceneObjectKind.CompletelyInferred:
                    if(!RenderCompletelyInferredSceneObjects)
                        return false;
                    break;
            }

            GameObject unityParentHolderObj = new GameObject(suObj.Kind.ToString());
            unityParentHolderObj.transform.parent = SceneRoot.transform;

            System.Numerics.Matrix4x4 converted4x4LocationMatrix = ConvertRightHandedMatrix4x4ToLeftHanded(suObj.GetLocationAsMatrix());
            SetUnityTransformFromMatrix4x4(unityParentHolderObj.transform, converted4x4LocationMatrix, true);

            List<GameObject> unityGeometryObjs = null;
            switch(kind)
            {
                case SceneUnderstanding.SceneObjectKind.World:
                    unityGeometryObjs = CreateWorldMeshInUnity(suObj);
                    break;
                default:
                    unityGeometryObjs = CreateSUObjectInUnity(suObj);
                    break;
            }

            foreach(GameObject geometryObj in unityGeometryObjs)
            {
                geometryObj.transform.parent = unityParentHolderObj.transform;
                geometryObj.transform.localPosition = Vector3.zero;
                geometryObj.transform.localRotation = Quaternion.identity;
            }

            if(DisplayTextLabels)
            {
                if(
                    kind == SceneUnderstanding.SceneObjectKind.Wall     ||
                    kind == SceneUnderstanding.SceneObjectKind.Floor    ||
                    kind == SceneUnderstanding.SceneObjectKind.Ceiling  ||  
                    kind == SceneUnderstanding.SceneObjectKind.Unknown  ||
                    kind == SceneUnderstanding.SceneObjectKind.Platform ||
                    kind == SceneUnderstanding.SceneObjectKind.Background
                )
                {
                    AddLabel(unityParentHolderObj, kind.ToString());
                }
            }

            if(RunOnDevice)
            {
                unityParentHolderObj.AddComponent<UnityEngine.XR.WSA.WorldAnchor>();
            }

            return true;
        }

        private List<GameObject> CreateWorldMeshInUnity(SceneUnderstanding.SceneObject suObj)
        {
            IEnumerable<SceneUnderstanding.SceneMesh> suMeshes = suObj.Meshes;
            Mesh unityMesh = GenerateUnityMeshFromSceneObjMeshes(suMeshes);

            GameObject gameObjToReturn = new GameObject(suObj.Kind.ToString());
            AddMeshToUnityObj(gameObjToReturn, unityMesh, ColorForWorldObjs, SceneObjectWireframeMaterial);

            return new List<GameObject> {gameObjToReturn};
        }

        private List<GameObject> CreateSUObjectInUnity(SceneUnderstanding.SceneObject suObj)
        {
            Color? color = GetColor(suObj.Kind);

            List<GameObject> listOfGeometryGameObjToReturn = new List<GameObject>();
            if(SceneObjectRenderMode == RenderMode.Quad || SceneObjectRenderMode == RenderMode.QuadWithMask)
            {
                foreach(SceneUnderstanding.SceneQuad quad in suObj.Quads)
                {
                    Mesh unityMesh = GenerateUnityMeshFromSceneObjQuad(quad);

                    Material tempMaterial = null;
                    if(SceneObjectRenderMode == RenderMode.QuadWithMask)
                    {
                        tempMaterial = Instantiate(SceneObjectQuadMaterial);
                    }
                    else
                    {
                        tempMaterial = Instantiate(SceneObjectMeshMaterial);
                    }

                    GameObject gameObjToReturn = new GameObject(suObj.Kind.ToString());
                    AddMeshToUnityObj(gameObjToReturn, unityMesh, color, tempMaterial);

                    if(SceneObjectRenderMode == RenderMode.QuadWithMask)
                    {
                        ApplyQuadRegionMask(quad, gameObjToReturn, color.Value);
                    }

                    if(AddColliders)
                    {
                        gameObjToReturn.AddComponent<BoxCollider>();
                    }

                    listOfGeometryGameObjToReturn.Add(gameObjToReturn);
                }
            }
            else // if Render.Mode == Mesh or == WireFrame
            {
                for(int i=0; i<suObj.Meshes.Count; i++)
                {
                    SceneUnderstanding.SceneMesh SUGeometryMesh = suObj.Meshes[i];
                    SceneUnderstanding.SceneMesh SUColliderMesh = suObj.ColliderMeshes[i];

                    // Generate the unity mesh for the Scene Understanding mesh.
                    Mesh unityMesh = GenerateUnityMeshFromSceneObjMeshes(new List<SceneUnderstanding.SceneMesh> {SUGeometryMesh});
                    GameObject gameObjToReturn = new GameObject(suObj.Kind.ToString());

                    Material tempMaterial = null;
                    if(SceneObjectRenderMode == RenderMode.Mesh)
                    {
                        tempMaterial = Instantiate(SceneObjectMeshMaterial);
                    }
                    else
                    {
                        tempMaterial = Instantiate(SceneObjectWireframeMaterial);
                    }
                    AddMeshToUnityObj(gameObjToReturn,unityMesh,color,tempMaterial);

                    if(AddColliders)
                    {
                        //Generate a unity mesh for physics
                        Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjMeshes(new List<SceneUnderstanding.SceneMesh> {SUGeometryMesh});

                        MeshCollider col = gameObjToReturn.AddComponent<MeshCollider>();
                        col.sharedMesh = unityColliderMesh;
                    }

                    listOfGeometryGameObjToReturn.Add(gameObjToReturn);
                }
            }

            return listOfGeometryGameObjToReturn;
        }

        private Mesh GenerateUnityMeshFromSceneObjMeshes(IEnumerable<SceneUnderstanding.SceneMesh> suMeshes)
        {
            if(suMeshes == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshFromSceneObjMeshes: Meshes is null.");
                return null;
            }

            List<int> combinedMeshIndices =  new List<int>();
            List<Vector3> combinedMeshVertices = new List<Vector3>();

            foreach(SceneUnderstanding.SceneMesh suMesh in suMeshes)
            {
                if(suMeshes == null)
                {
                    Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshFromSceneObjMeshes: Mesh is null.");
                    continue;
                }

                uint[] meshIndices = new uint[suMesh.TriangleIndexCount];
                suMesh.GetTriangleIndices(meshIndices);

                System.Numerics.Vector3[] meshVertices = new System.Numerics.Vector3[suMesh.VertexCount];
                suMesh.GetVertexPositions(meshVertices);

                uint indexOffset = (uint)combinedMeshIndices.Count;

                for(int i =0; i < meshVertices.Length; i++)
                {
                    combinedMeshVertices.Add(new Vector3(meshVertices[i].X, meshVertices[i].Y, -meshVertices[i].Z));
                }

                for(int i =0; i < meshIndices.Length; i++)
                {
                    combinedMeshIndices.Add((int)(meshIndices[i] + indexOffset));
                }

            }

            Mesh unityMesh = new Mesh();

            // Unity has a limit of 65,535 vertices in a mesh.
            // This limit exists because by default Unity uses 16-bit index buffers.
            // Starting with 2018.1, Unity allows one to use 32-bit index buffers.
            if(combinedMeshVertices.Count > 65535)
            {
                Debug.Log("SceneUnderstandingUtils.GenerateUnityMeshForSceneObjectMeshes: CombinedMeshVertices count is " + combinedMeshVertices.Count + ". Will be using a 32-bit index buffer.");
                unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            unityMesh.SetVertices(combinedMeshVertices);
            unityMesh.SetIndices(combinedMeshIndices.ToArray(), MeshTopology.Triangles, 0);
            unityMesh.RecalculateNormals();

            return unityMesh;
        }

        private Mesh GenerateUnityMeshFromSceneObjQuad(SceneUnderstanding.SceneQuad quad)
        {
            if (quad == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshForSceneObjectQuad: Quad is null.");
                return null;
            }

            float widthInMeters = quad.Extents.X;
            float heightInMeters = quad.Extents.Y;

            // Bounds of the quad.
            List<Vector3> vertices = new List<Vector3>()
            {
                new Vector3(-widthInMeters / 2, -heightInMeters / 2, 0),
                    new Vector3( widthInMeters / 2, -heightInMeters / 2, 0),
                    new Vector3(-widthInMeters / 2,  heightInMeters / 2, 0),
                    new Vector3( widthInMeters / 2,  heightInMeters / 2, 0)
            };

            List<int> triangles = new List<int>()
            {
                1, 3, 0,
                3, 2, 0
            };

            List<Vector2> uvs = new List<Vector2>()
            {
                new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
            };

            Mesh unityMesh = new Mesh();
            unityMesh.SetVertices(vertices);
            unityMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
            unityMesh.SetUVs(0, uvs);

            return unityMesh;
        }

        private Color? GetColor(SceneObjectKind kind)
        {
            switch (kind)
            {
                case SceneUnderstanding.SceneObjectKind.Background:
                    return ColorForBackgroundObjs;  // Pink'ish
                case SceneUnderstanding.SceneObjectKind.Wall:
                    return ColorForWallObjs;       // Orange'ish
                case SceneUnderstanding.SceneObjectKind.Floor:
                    return ColorForFloorObjs;      // Green'ish
                case SceneUnderstanding.SceneObjectKind.Ceiling:
                    return ColorForCeilingObjs;   // Purple'ish
                case SceneUnderstanding.SceneObjectKind.Platform:
                    return ColorForPlatformsObjs; // Blue'ish
                case SceneUnderstanding.SceneObjectKind.Unknown:
                    return ColorForUnknownObjs;  // White
                case SceneUnderstanding.SceneObjectKind.CompletelyInferred:
                    return ColorForInferredObjs;  // Gray
                case SceneUnderstanding.SceneObjectKind.World:
                    return ColorForWorldObjs;  // Gray
                default:
                    return null;
            }
        }

        private void AddMeshToUnityObj(GameObject unityObj, Mesh mesh, Color? color, Material material)
        {
            if(unityObj == null || mesh == null || material == null)
            {
                Debug.Log("SceneUnderstandingManager.AddMeshToUnityObj: One or more arguments are null");
            }

            MeshFilter mf = unityObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            Material tempMaterial;
            if(isInGhostMode)
            {
                tempMaterial = Instantiate(TransparentOcclussion);
            }
            else
            {
                tempMaterial = Instantiate(material);
            }
            
            if(color != null)
            {
                tempMaterial.color = color.Value;
                tempMaterial.SetColor("_WireColor", color.Value);
            }

            MeshRenderer mr = unityObj.AddComponent<MeshRenderer>();
            mr.material = tempMaterial;

        }

        private void ApplyQuadRegionMask(SceneUnderstanding.SceneQuad quad, GameObject obj, Color color)
        {
            if (quad == null || obj == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.ApplyQuadRegionMask: One or more arguments are null.");
                return;
            }

            // Resolution of the mask.
            ushort width = 256;
            ushort height = 256;

            byte[] mask = new byte[width * height];
            quad.GetSurfaceMask(width, height, mask);

            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.HasProperty("_MainTex") == false)
            {
                Debug.LogWarning("SceneUnderstandingManager.ApplyQuadRegionMask: Mesh renderer component is null or does not have a valid material.");
                return;
            }

            // Create a new texture.
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // Transfer the invalidation mask onto the texture.
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; ++i)
            {
                byte value = mask[i];

                if (value == (byte)SceneUnderstanding.SceneRegionSurfaceKind.NotSurface)
                {
                    pixels[i] = Color.clear;
                }
                else
                {
                    pixels[i] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);

            // Set the texture on the material.
            meshRenderer.sharedMaterial.mainTexture = texture;
        }

        private void AddLabel(GameObject obj, string label)
        {
            if (obj == null || label == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.AddLabel: One or more arguments are null.");
                return;
            }

            // Create the parent container and give it a name
            GameObject textGO = new GameObject("Label");

            // Set it as a child of the game object
            textGO.transform.SetParent(obj.transform, worldPositionStays: false);

            // Move it slightly in front of the game object
            textGO.transform.Translate(0, 0, -0.003f);

            // Create a TextMeshPro object for our text
            TextMeshPro tmpro = textGO.AddComponent<TextMeshPro>();

            // Align middle, center
            tmpro.alignment = TextAlignmentOptions.Center;

            // This width, height and scale seems to be about right for HoloLens
            tmpro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 20f);
            tmpro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 6f);
            tmpro.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

            // And finally assign the label text
            tmpro.text = label;
        }

        #endregion

        #region Utility Functions

        private void DestroyAllGameObjectsUnderParent(Transform parentTransform)
        {
            if (parentTransform == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.DestroyAllGameObjectsUnderParent: Parent is null.");
                return;
            }

            foreach (Transform child in parentTransform)
            {
                Destroy(child.gameObject);
            }
        }

        private System.Numerics.Matrix4x4 GetSceneToUnityTransformAsMatrix4x4(SceneUnderstanding.Scene scene)
        {
            System.Numerics.Matrix4x4? sceneToUnityTransform = System.Numerics.Matrix4x4.Identity;

            if(RunOnDevice)
            {
                Windows.Perception.Spatial.SpatialCoordinateSystem sceneCoordinateSystem = Microsoft.Windows.Perception.Spatial.Preview.SpatialGraphInteropPreview.CreateCoordinateSystemForNode(scene.OriginSpatialGraphNodeId);
                HolograhicFrameData holoFrameData =  Marshal.PtrToStructure<HolograhicFrameData>(UnityEngine.XR.XRDevice.GetNativePtr());
                Windows.Perception.Spatial.SpatialCoordinateSystem unityCoordinateSystem = Microsoft.Windows.Perception.Spatial.SpatialCoordinateSystem.FromNativePtr(holoFrameData.ISpatialCoordinateSystemPtr);

                sceneToUnityTransform = sceneCoordinateSystem.TryGetTransformTo(unityCoordinateSystem);

                if(sceneToUnityTransform != null)
                {
                    sceneToUnityTransform = ConvertRightHandedMatrix4x4ToLeftHanded(sceneToUnityTransform.Value);
                }
                else
                {
                    Debug.LogWarning("SceneUnderstandingManager.GetSceneToUnityTransform: Scene to Unity transform is null.");
                }
            }

            return sceneToUnityTransform.Value;
        }

        private System.Numerics.Matrix4x4 ConvertRightHandedMatrix4x4ToLeftHanded(System.Numerics.Matrix4x4 matrix)
        {
            matrix.M13 = -matrix.M13;
            matrix.M23 = -matrix.M23;
            matrix.M43 = -matrix.M43;

            matrix.M31 = -matrix.M31;
            matrix.M32 = -matrix.M32;
            matrix.M34 = -matrix.M34;

            return matrix;
        }

        private void SetUnityTransformFromMatrix4x4(Transform targetTransform, System.Numerics.Matrix4x4 matrix, bool updateLocalTransformOnly = false)
        {
            if(targetTransform == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.SetUnityTransformFromMatrix4x4: Unity transform is null.");
                return;
            }

            Vector3 unityTranslation;
            Quaternion unityQuat;
            Vector3 unityScale;

            System.Numerics.Vector3 vector3;
            System.Numerics.Quaternion quaternion;
            System.Numerics.Vector3 scale;

            System.Numerics.Matrix4x4.Decompose(matrix, out scale, out quaternion, out vector3);

            unityTranslation = new Vector3(vector3.X, vector3.Y, vector3.Z);
            unityQuat = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
            unityScale = new Vector3(scale.X, scale.Y, scale.Z);

            if(updateLocalTransformOnly)
            {
                targetTransform.localPosition = unityTranslation;
                targetTransform.localRotation = unityQuat;
            }
            else
            {
                targetTransform.SetPositionAndRotation(unityTranslation, unityQuat);
            }
        }

        private void OrientSceneForPC(GameObject sceneRoot, SceneUnderstanding.Scene suScene)
        {
            if(suScene == null)
            {
                Debug.Log("SceneUnderstandingManager.OrientSceneForPC: Scene Understanding Scene Data is null.");
            }

            IEnumerable<SceneUnderstanding.SceneObject> sceneObjects = suScene.SceneObjects;

            float largestFloorAreaFound = 0.0f;
            SceneUnderstanding.SceneObject suLargestFloorObj = null;
            SceneUnderstanding.SceneQuad suLargestFloorQuad = null;
            foreach(SceneUnderstanding.SceneObject sceneObj in sceneObjects)
            {
                if(sceneObj.Kind == SceneUnderstanding.SceneObjectKind.Floor)
                {
                    IEnumerable<SceneUnderstanding.SceneQuad> quads = sceneObj.Quads;

                    if(quads != null)
                    {
                        foreach(SceneUnderstanding.SceneQuad quad in quads)
                        {
                            float quadArea = quad.Extents.X * quad.Extents.Y;

                            if(quadArea > largestFloorAreaFound)
                            {
                                largestFloorAreaFound = quadArea;
                                suLargestFloorObj = sceneObj;
                                suLargestFloorQuad = quad;
                            }
                        }
                    }
                }
            }

            if(suLargestFloorQuad != null)
            {
                float quadWith = suLargestFloorQuad.Extents.X;
                float quadHeight = suLargestFloorQuad.Extents.Y;

                System.Numerics.Vector3 p1 = new System.Numerics.Vector3(-quadWith / 2, -quadHeight / 2, 0);
                System.Numerics.Vector3 p2 = new System.Numerics.Vector3( quadWith / 2, -quadHeight / 2, 0);
                System.Numerics.Vector3 p3 = new System.Numerics.Vector3(-quadWith / 2,  quadHeight / 2, 0);

                System.Numerics.Matrix4x4 floorTransform = suLargestFloorObj.GetLocationAsMatrix();
                floorTransform = ConvertRightHandedMatrix4x4ToLeftHanded(floorTransform);

                System.Numerics.Vector3 tp1 = System.Numerics.Vector3.Transform(p1, floorTransform);
                System.Numerics.Vector3 tp2 = System.Numerics.Vector3.Transform(p2, floorTransform);
                System.Numerics.Vector3 tp3 = System.Numerics.Vector3.Transform(p3, floorTransform);

                System.Numerics.Vector3 p21 = tp2 - tp1;
                System.Numerics.Vector3 p31 = tp3 - tp1;

                System.Numerics.Vector3 floorNormal = System.Numerics.Vector3.Cross(p31, p21);

                Vector3 floorNormalUnity = new Vector3(floorNormal.X, floorNormal.Y, floorNormal.Z);

                Quaternion rotation = Quaternion.FromToRotation(floorNormalUnity, Vector3.up);
                SceneRoot.transform.rotation = rotation;
            }
        }



        #endregion

        #region Out of PlayMode Functions

        public void BakeScene()
        {
            Debug.Log("[IN EDITOR] SceneUnderStandingManager.BakeScene: Bake Started");
            DestroyImmediate(SceneRoot.gameObject);
            if(!RunOnDevice)
            {
                SceneRoot = SceneRoot == null ? new GameObject("Root") : SceneRoot;

                List<SceneUnderstanding.Scene> suScenes = new List<SceneUnderstanding.Scene>();

                foreach(TextAsset serializedScene in SUSerializedScenePaths)
                {
                    if(serializedScene)
                    {
                        byte[] sceneData = serializedScene.bytes;
                        suScenes.Add(SceneUnderstanding.Scene.Deserialize(sceneData));
                    }
                }

                foreach (SceneUnderstanding.Scene suScene in suScenes)
                {
                    if(suScene != null)
                    {
                        System.Numerics.Matrix4x4 sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(suScene);

                        if(sceneToUnityTransformAsMatrix4x4 != null)
                        {
                            SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4, RunOnDevice);

                            if(!RunOnDevice)
                            {
                                OrientSceneForPC(SceneRoot, suScene);
                            }

                            IEnumerable<SceneUnderstanding.SceneObject> sceneObjects = suScene.SceneObjects;
                            foreach (SceneUnderstanding.SceneObject sceneObject in sceneObjects)
                            {
                                DisplaySceneObject(sceneObject);
                            }
                        }
                    }
                }

                Debug.Log("[IN EDITOR] SceneUnderStandingManager.BakeScene: Display Completed");
            }
        }

        #endregion

        #region Save To Disk Functions

        // Await is conditionally compiled out based on platform but needs to be awaitable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task SaveBytesToDiskAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            DateTime currentDate = DateTime.Now;
            int year = currentDate.Year;
            int month = currentDate.Month;
            int day = currentDate.Day;
            int hour = currentDate.Hour;
            int min = currentDate.Minute;
            int sec = currentDate.Second;

            if(RunOnDevice)
            {
                string fileName = string.Format("SU_{0}-{1}-{2}_{3}-{4}-{5}.bytes",
                                                year, month, day, hour, min, sec);

                byte[] OnDeviceBytes = GetLatestSUScene();

                #if WINDOWS_UWP
                var folder = WindowsStorage.ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(fileName, WindowsStorage.CreationCollisionOption.GenerateUniqueName);
                await WindowsStorage.FileIO.WriteBytesAsync(file, OnDeviceBytes);
                #else
                Debug.Log("Save on Device is only supported in Universal Windows Applications");
                #endif
            }
            else
            {
                int fragmentNumber = 0;
                foreach(TextAsset serializedScene in SUSerializedScenePaths)
                {
                    byte[] fragmentBytes = serializedScene.bytes;

                    string fileName = string.Format("SU_Frag{0}-{1}-{2}-{3}_{4}-{5}-{6}.bytes",
                                                    fragmentNumber++, year, month, day, hour, min, sec);

                    string folder = Path.GetTempPath();
                    string file = Path.Combine(folder, fileName);
                    File.WriteAllBytes(file, fragmentBytes);
                    Debug.Log("SceneUnderstandingManager.SaveBytesToDisk: Scene Fragment saved at " + file);
                }

            }
        }

        public async Task SaveObjsToDiskAsync()
        {
            DateTime currentDate = DateTime.Now;
            int year = currentDate.Year;
            int month = currentDate.Month;
            int day = currentDate.Day;
            int hour = currentDate.Hour;
            int min = currentDate.Minute;
            int sec = currentDate.Second;

            // List of all SceneObjectKind enum values.
            List<SceneUnderstanding.SceneObjectKind> sceneObjectKinds = new List<SceneObjectKind>();
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Background);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Ceiling);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.CompletelyInferred);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Floor);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Platform);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Unknown);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.Wall);
            sceneObjectKinds.Add(SceneUnderstanding.SceneObjectKind.World);

            List<Task> tasks = new List<Task>();

            if(RunOnDevice)
            {
                byte[] OnDeviceBytes = GetLatestSUScene();
                if (OnDeviceBytes == null)
                {
                    Debug.LogWarning("SceneUnderstandingManager.SaveObjsToDisk: Nothing to save.");
                    return;
                }

                // Deserialize the scene.
                SceneUnderstanding.Scene scene = SceneUnderstanding.Scene.Deserialize(OnDeviceBytes);
                foreach (SceneUnderstanding.SceneObjectKind soKind in sceneObjectKinds)
                {
                    List<SceneUnderstanding.SceneObject> allObjectsOfAKind = new List<SceneObject>();
                    foreach(SceneUnderstanding.SceneObject sceneObject in scene.SceneObjects)
                    {
                        if(sceneObject.Kind == soKind)
                        {
                            allObjectsOfAKind.Add(sceneObject);
                        }
                    }

                    string fileName = string.Format("SU_{0}_{1}-{2}-{3}_{4}-{5}-{6}.obj",
                                                soKind.ToString(), year, month, day, hour, min, sec);

                    tasks.Add(SaveAllSceneObjectsOfAKindAsOneObj(allObjectsOfAKind, GetColor(soKind), fileName));
                }
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (SceneUnderstanding.SceneObjectKind soKind in sceneObjectKinds)
                {
                    List<SceneUnderstanding.SceneObject> allObjectsOfAKind = new List<SceneObject>();
                    foreach(TextAsset serializedScene in SUSerializedScenePaths)
                    {
                        byte[] fragmentBytes = serializedScene.bytes;
                        SceneUnderstanding.Scene scene = SceneUnderstanding.Scene.Deserialize(fragmentBytes);

                        foreach(SceneUnderstanding.SceneObject sceneObject in scene.SceneObjects)
                        {
                            if(sceneObject.Kind == soKind)
                            {
                                allObjectsOfAKind.Add(sceneObject);
                            }
                        }
                    }

                    string fileName = string.Format("SU_{0}_{1}-{2}-{3}_{4}-{5}-{6}.obj",
                                                soKind.ToString(), year, month, day, hour, min, sec);

                    tasks.Add(SaveAllSceneObjectsOfAKindAsOneObj(allObjectsOfAKind, GetColor(soKind), fileName));
                }
            }

        }

        private async Task SaveAllSceneObjectsOfAKindAsOneObj(List<SceneUnderstanding.SceneObject> sceneObjects, Color? color, string fileName)
        {
            if (sceneObjects == null)
            {
                return;
            }
            
            List<System.Numerics.Vector3> combinedMeshVertices = new List<System.Numerics.Vector3>();
            List<uint> combinedMeshIndices = new List<uint>();
            
            // Go through each scene object, retrieve its meshes and add them to the combined lists, defined above.
            foreach (SceneUnderstanding.SceneObject so in sceneObjects)
            {
                if (so == null)
                {
                    continue;
                }

                IEnumerable<SceneUnderstanding.SceneMesh> meshes = so.Meshes;
                if (meshes == null)
                {
                    continue;
                }
                
                foreach (SceneUnderstanding.SceneMesh mesh in meshes)
                {
                    // Get the mesh vertices.
                    var mvList = new System.Numerics.Vector3[mesh.VertexCount];
                    mesh.GetVertexPositions(mvList);

                    // Transform the vertices using the transformation matrix.
                    TransformVertices(so.GetLocationAsMatrix(), mvList);
                    
                    // Store the current set of vertices in the combined list. As we add indices, we'll offset it by this value.
                    uint indexOffset = (uint)combinedMeshVertices.Count;
                    
                    // Add the new set of mesh vertices to the existing set.
                    combinedMeshVertices.AddRange(mvList);

                    // Get the mesh indices.
                    uint[] mi = new uint[mesh.TriangleIndexCount];
                    mesh.GetTriangleIndices(mi);

                    // Add the new set of mesh indices to the existing set.
                    for (int i = 0; i < mi.Length; ++i)
                    {
                        combinedMeshIndices.Add((uint)(mi[i] + indexOffset));
                    }
                }
            }

            // Write as string to file.
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < combinedMeshVertices.Count; ++i)
            {
                sb.Append(string.Format("v {0} {1} {2} {3} {4} {5}\n", combinedMeshVertices[i].X, combinedMeshVertices[i].Y, combinedMeshVertices[i].Z, color.Value.r, color.Value.g, color.Value.b));
            }

            for (int i = 0; i < combinedMeshIndices.Count; i += 3)
            {
                // Indices start at index 1 (as opposed to 0) in objs.
                sb.Append(string.Format("f {0} {1} {2}\n", combinedMeshIndices[i] + 1, combinedMeshIndices[i + 1] + 1, combinedMeshIndices[i + 2] + 1));
            }

            await SaveStringToDiskAsync(sb.ToString(), fileName);
        }

// Await is conditionally compiled out based on platform but needs to be awaitable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task SaveStringToDiskAsync(string data, string fileName)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("SceneUnderstandingManager.SaveStringToDiskAsync: Nothing to save.");
                return;
            }

            if(RunOnDevice)
            {
                #if WINDOWS_UWP
                var folder = WindowsStorage.ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(fileName, WindowsStorage.CreationCollisionOption.GenerateUniqueName);
                await WindowsStorage.FileIO.WriteBytesAsync(file, data);
                #else
                Debug.Log("Save on Device is only supported in Universal Windows Applications");
                #endif
            }
            else
            {
                string folder = Path.GetTempPath();
                string file = Path.Combine(folder, fileName);
                File.WriteAllText(file, data);
                Debug.Log("SceneUnderstandingManager.SaveStringToDiskAsync: Scene Objects saved at " + file);
            }
        }
        
        private void TransformVertices(System.Numerics.Matrix4x4 transformationMatrix, System.Numerics.Vector3[] vertices)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = System.Numerics.Vector3.Transform(vertices[i], transformationMatrix);
            }
        }

        #endregion

    }
}