namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(SceneUnderstandingManager))]
    public class SceneUnderstandingManagerEditor : Editor
    {
        SceneUnderstandingManager SUManager;
        SerializedProperty serializedSUScene;
        SerializedProperty serializedRootGameObject;
        SerializedProperty serializedRenderMode;
        SerializedProperty serializedMeshMaterial;
        SerializedProperty serializedQuadMaterial;
        SerializedProperty serializedWireFrameMaterial;
        SerializedProperty serializedRenderSceneObjects;
        SerializedProperty serializedDisplayTextLabels;
        SerializedProperty serializedRenderPlatformsObjects;
        SerializedProperty serializedRenderBackgroundObjects;
        SerializedProperty serializedRenderUnknownObjects;
        SerializedProperty serializedRenderWorldMesh;
        SerializedProperty serializedRequestInferredRegions;
        SerializedProperty serializedRenderCompletelyInferredSceneObjects;
        SerializedProperty serializedRenderQuality;
        SerializedProperty serializedRenderColorBackGrounds;
        SerializedProperty serializedRenderColorWall;
        SerializedProperty serializedRenderColorFloor;
        SerializedProperty serializedRenderColorCeiling;
        SerializedProperty serializedRenderColorPlatform;
        SerializedProperty serializedRenderColorUnknown;
        SerializedProperty serializedRenderColorCompletelyInferred;
        SerializedProperty serializedRenderColorWorld;
        SerializedProperty serializedOnLoadCallback;

        private void OnEnable()
        {
            SUManager = this.target as SceneUnderstandingManager;
            serializedSUScene = serializedObject.FindProperty("SUSerializedScenePaths");
            serializedRootGameObject = serializedObject.FindProperty("SceneRoot");
            serializedRenderMode = serializedObject.FindProperty("SceneObjectRenderMode");
            serializedMeshMaterial = serializedObject.FindProperty("SceneObjectMeshMaterial");
            serializedQuadMaterial = serializedObject.FindProperty("SceneObjectQuadMaterial");
            serializedWireFrameMaterial = serializedObject.FindProperty("SceneObjectWireframeMaterial");
            serializedRenderQuality = serializedObject.FindProperty("RenderQuality");
            serializedRenderSceneObjects = serializedObject.FindProperty("RenderSceneObjects");
            serializedDisplayTextLabels = serializedObject.FindProperty("DisplayTextLabels");
            serializedRenderPlatformsObjects = serializedObject.FindProperty("RenderPlatformSceneObjects");
            serializedRenderBackgroundObjects = serializedObject.FindProperty("RenderBackgroundSceneObjects");
            serializedRenderUnknownObjects = serializedObject.FindProperty("RenderUnknownSceneObjects");
            serializedRenderWorldMesh = serializedObject.FindProperty("RenderWorldMesh");
            serializedRequestInferredRegions = serializedObject.FindProperty("RequestInferredRegions");
            serializedRenderCompletelyInferredSceneObjects = serializedObject.FindProperty("RenderCompletelyInferredSceneObjects");
            serializedRenderColorBackGrounds = serializedObject.FindProperty("ColorForBackgroundObjs");
            serializedRenderColorWall = serializedObject.FindProperty("ColorForWallObjs");
            serializedRenderColorFloor = serializedObject.FindProperty("ColorForFloorObjs");
            serializedRenderColorCeiling = serializedObject.FindProperty("ColorForCeilingObjs");
            serializedRenderColorPlatform = serializedObject.FindProperty("ColorForPlatformsObjs");
            serializedRenderColorUnknown = serializedObject.FindProperty("ColorForUnknownObjs");
            serializedRenderColorCompletelyInferred = serializedObject.FindProperty("ColorForInferredObjs");
            serializedRenderColorWorld = serializedObject.FindProperty("ColorForWorldObjs");
            serializedOnLoadCallback = serializedObject.FindProperty("OnLoad");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label("Data Loader Mode", EditorStyles.boldLabel);
            GUIContent RunOnDeviceContent = new GUIContent("Run On Device", "When enabled, the scene will run using a device (e.g Hololens). Otherwise, a previously saved, serialized scene will be loaded and served from your PC.");
            SUManager.RunOnDevice = EditorGUILayout.Toggle(RunOnDeviceContent,SUManager.RunOnDevice);
            if(!SUManager.RunOnDevice)
            {
                GUILayout.Label("Scene Fragments: ", EditorStyles.boldLabel);
                if(GUILayout.Button("Add Item", GUILayout.Width(90)))
                {
                    SUManager.SUSerializedScenePaths.Add(null);
                }

                if(GUILayout.Button("Remove Item", GUILayout.Width(90)))
                {
                    if(SUManager.SUSerializedScenePaths.Count >= 1)
                    {
                        SUManager.SUSerializedScenePaths.RemoveAt(SUManager.SUSerializedScenePaths.Count -1);
                    }
                }

                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(serializedSUScene,false);
                for (int i = 0; i < serializedSUScene.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(serializedSUScene.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel -= 1;
            }
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedRootGameObject);
            GUILayout.Space(4.0f);

            if(SUManager.RunOnDevice)
            {
                GUILayout.Label("Data Loader Parameters", EditorStyles.boldLabel);
                GUIContent BoundingSphereRadiousInMetersContent = new GUIContent("Bounding Sphere Radious In Meters", "Radius of the sphere around the camera, which is used to query the environment.");
                SUManager.BoundingSphereRadiusInMeters = EditorGUILayout.Slider(BoundingSphereRadiousInMetersContent, SUManager.BoundingSphereRadiusInMeters, 5.0f, 100.0f);

                GUIContent AutoRefreshContent = new GUIContent("Auto Refresh Data", "When enabled, the latest data from Scene Understanding data provider will be displayed periodically (controlled by the AutoRefreshIntervalInSeconds float).");
                SUManager.AutoRefresh = EditorGUILayout.Toggle(AutoRefreshContent,SUManager.AutoRefresh);

                if(SUManager.AutoRefresh)
                {
                    GUIContent AutoRefreshIntervalInSeconds = new GUIContent("Auto Refresh Interval In Seconds", "Interval to use for auto refresh, in seconds.");
                    SUManager.AutoRefreshIntervalInSeconds = EditorGUILayout.Slider(AutoRefreshIntervalInSeconds, SUManager.AutoRefreshIntervalInSeconds, 1.0f, 60.0f);
                }
                GUILayout.Space(4.0f);
            }

            EditorGUILayout.PropertyField(serializedRenderMode);
            EditorGUILayout.PropertyField(serializedRenderQuality);
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedRenderColorBackGrounds);
            EditorGUILayout.PropertyField(serializedRenderColorWall);
            EditorGUILayout.PropertyField(serializedRenderColorFloor);
            EditorGUILayout.PropertyField(serializedRenderColorCeiling);
            EditorGUILayout.PropertyField(serializedRenderColorPlatform);
            EditorGUILayout.PropertyField(serializedRenderColorUnknown);
            EditorGUILayout.PropertyField(serializedRenderColorCompletelyInferred);
            EditorGUILayout.PropertyField(serializedRenderColorWorld);
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedMeshMaterial);
            EditorGUILayout.PropertyField(serializedQuadMaterial);
            EditorGUILayout.PropertyField(serializedWireFrameMaterial);
            GUILayout.Space(4.0f);

            GUILayout.Label("Render Filters", EditorStyles.boldLabel);
            GUIContent RenderSceneObjectsContent = new GUIContent("Render Scene Objects", "Toggles display of all scene objects, except for the world mesh.");
            SUManager.RenderSceneObjects = EditorGUILayout.Toggle(RenderSceneObjectsContent, SUManager.RenderSceneObjects);

            GUIContent DisplayTextLabelsContent = new GUIContent("Display Text Labels", "Display text labels for the scene objects.");
            SUManager.DisplayTextLabels = EditorGUILayout.Toggle(DisplayTextLabelsContent, SUManager.DisplayTextLabels);

            GUIContent RenderPlatformsObjectsContent = new GUIContent("Render Platforms Objects", "Toggles display of large, horizontal scene objects, aka 'Platform'.");
            SUManager.RenderPlatformSceneObjects = EditorGUILayout.Toggle(RenderPlatformsObjectsContent, SUManager.RenderPlatformSceneObjects);

            GUIContent RenderBackgroundObjectsContent = new GUIContent("Render Background Objects", "Toggles the display of background scene objects.");
            SUManager.RenderBackgroundSceneObjects = EditorGUILayout.Toggle(RenderBackgroundObjectsContent, SUManager.RenderBackgroundSceneObjects);

            GUIContent RenderUnknownObjectsContent = new GUIContent("Render Unknown Objects", "Toggles the display of unknown scene objects.");
            SUManager.RenderUnknownSceneObjects = EditorGUILayout.Toggle(RenderUnknownObjectsContent, SUManager.RenderUnknownSceneObjects);

            GUIContent RenderWorldMeshContent = new GUIContent("Render World Mesh", "Toggles the display of the world mesh.");
            SUManager.RenderWorldMesh = EditorGUILayout.Toggle(RenderWorldMeshContent, SUManager.RenderWorldMesh);

            GUIContent RequestInferredRegionsContent = new GUIContent("Request Inferred Regions", "When enabled, requests observed and inferred regions for scene objects. When disabled, requests only the observed regions for scene objects.");
            SUManager.RequestInferredRegions = EditorGUILayout.Toggle(RequestInferredRegionsContent, SUManager.RequestInferredRegions);

            if(SUManager.RequestInferredRegions)
            {
                GUIContent RenderCompletelyInferredSceneObjectsContent = new GUIContent("Render Completely Inferred Scene Objects", "Toggles the display of completely inferred scene objects.");
                SUManager.RenderCompletelyInferredSceneObjects = EditorGUILayout.Toggle(RenderCompletelyInferredSceneObjectsContent,SUManager.RenderCompletelyInferredSceneObjects);
            }
            GUILayout.Space(4.0f);

            GUILayout.Label("Physics", EditorStyles.boldLabel);
            GUIContent AddCollidersContent = new GUIContent("Add Colliders", "Toggles the creation of objects with collider components.");
            SUManager.AddColliders = EditorGUILayout.Toggle(AddCollidersContent, SUManager.AddColliders);
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedOnLoadCallback);
            GUILayout.Space(4.0f);

            if(!SUManager.RunOnDevice)
            {
                GUILayout.Label("Actions", EditorStyles.boldLabel);
                if(GUILayout.Button("Bake Scene", GUILayout.Width(90)))
                {
                    SUManager.BakeScene();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}