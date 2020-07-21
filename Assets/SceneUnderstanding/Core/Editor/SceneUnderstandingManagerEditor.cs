namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(SceneUnderstandingManager))]
    public class SceneUnderstandingManagerEditor : Editor
    {
        SceneUnderstandingManager SUManager;
        SerializedProperty serializedRunOnDevice;
        SerializedProperty serializedSUScene;
        SerializedProperty serializedRootGameObject;
        SerializedProperty serializedBoudingSphereRadiousInMeters;
        SerializedProperty serializedAutoRefreshData;
        SerializedProperty serializedAutoRefreshIntervalInSeconds;
        SerializedProperty serializedRenderMode;
        SerializedProperty serializedMeshMaterial;
        SerializedProperty serializedQuadMaterial;
        SerializedProperty serializedWireFrameMaterial;
        SerializedProperty serializedInvisibleMaterial;
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
        SerializedProperty serializedisInGhostMode;
        SerializedProperty serializedAddColliders;
        SerializedProperty serializedOnLoadStartedCallback;
        SerializedProperty serializedOnLoadFinishedCallback;

        private void OnEnable()
        {
            SUManager = this.target as SceneUnderstandingManager;
            serializedRunOnDevice = serializedObject.FindProperty("RunOnDevice");

            serializedSUScene = serializedObject.FindProperty("SUSerializedScenePaths");
            serializedRootGameObject = serializedObject.FindProperty("SceneRoot");

            serializedBoudingSphereRadiousInMeters = serializedObject.FindProperty("BoundingSphereRadiusInMeters");
            serializedAutoRefreshData = serializedObject.FindProperty("AutoRefresh");
            serializedAutoRefreshIntervalInSeconds = serializedObject.FindProperty("AutoRefreshIntervalInSeconds");

            serializedRenderMode = serializedObject.FindProperty("SceneObjectRenderMode");
            serializedRenderQuality = serializedObject.FindProperty("RenderQuality");


            serializedMeshMaterial = serializedObject.FindProperty("SceneObjectMeshMaterial");
            serializedQuadMaterial = serializedObject.FindProperty("SceneObjectQuadMaterial");
            serializedWireFrameMaterial = serializedObject.FindProperty("SceneObjectWireframeMaterial");
            serializedInvisibleMaterial = serializedObject.FindProperty("TransparentOcclussion");

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

            serializedisInGhostMode = serializedObject.FindProperty("isInGhostMode");

            serializedAddColliders = serializedObject.FindProperty("AddColliders");

            serializedOnLoadStartedCallback = serializedObject.FindProperty("OnLoadStarted");
            serializedOnLoadFinishedCallback = serializedObject.FindProperty("OnLoadFinished");
            
        }

        public override void OnInspectorGUI()
        {
            //Load Latest, before any changes
            serializedObject.Update();

            //Data Loader Mode (Run On device flag)
            EditorGUILayout.PropertyField(serializedRunOnDevice);

            //PC path
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
                EditorGUILayout.PropertyField(serializedSUScene, false);
                for (int i = 0; i < serializedSUScene.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(serializedSUScene.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel -= 1;
            }
            GUILayout.Space(4.0f);

            //Scene Root
            EditorGUILayout.PropertyField(serializedRootGameObject);
            GUILayout.Space(4.0f);
            
            //Data Loader Params
            if(SUManager.RunOnDevice)
            {
                GUILayout.Label("Data Loader Parameters", EditorStyles.boldLabel);
                GUIContent BoundingSphereRadiousInMetersContent = new GUIContent("Bounding Sphere Radious In Meters", "Radius of the sphere around the camera, which is used to query the environment.");
                serializedBoudingSphereRadiousInMeters.floatValue = EditorGUILayout.Slider(BoundingSphereRadiousInMetersContent, serializedBoudingSphereRadiousInMeters.floatValue, 5.0f, 100.0f);

                EditorGUILayout.PropertyField(serializedAutoRefreshData);
                
                if(SUManager.AutoRefresh)
                {
                    GUIContent AutoRefreshIntervalInSeconds = new GUIContent("Auto Refresh Interval In Seconds", "Interval to use for auto refresh, in seconds.");
                    serializedAutoRefreshIntervalInSeconds.floatValue = EditorGUILayout.Slider(AutoRefreshIntervalInSeconds, serializedAutoRefreshIntervalInSeconds.floatValue, 1.0f, 60.0f);
                }
                GUILayout.Space(4.0f);
            }

            //Render Mode
            EditorGUILayout.PropertyField(serializedRenderMode);
            EditorGUILayout.PropertyField(serializedRenderQuality);
            GUILayout.Space(4.0f);

            //Colors
            EditorGUILayout.PropertyField(serializedRenderColorBackGrounds);
            EditorGUILayout.PropertyField(serializedRenderColorWall);
            EditorGUILayout.PropertyField(serializedRenderColorFloor);
            EditorGUILayout.PropertyField(serializedRenderColorCeiling);
            EditorGUILayout.PropertyField(serializedRenderColorPlatform);
            EditorGUILayout.PropertyField(serializedRenderColorUnknown);
            EditorGUILayout.PropertyField(serializedRenderColorCompletelyInferred);
            EditorGUILayout.PropertyField(serializedRenderColorWorld);
            GUILayout.Space(4.0f);

            //Materials
            EditorGUILayout.PropertyField(serializedMeshMaterial);
            EditorGUILayout.PropertyField(serializedQuadMaterial);
            EditorGUILayout.PropertyField(serializedWireFrameMaterial);
            EditorGUILayout.PropertyField(serializedInvisibleMaterial);
            GUILayout.Space(4.0f);

            //Render Filters
            EditorGUILayout.PropertyField(serializedRenderSceneObjects);
            EditorGUILayout.PropertyField(serializedDisplayTextLabels);
            EditorGUILayout.PropertyField(serializedRenderPlatformsObjects);
            EditorGUILayout.PropertyField(serializedRenderBackgroundObjects);
            EditorGUILayout.PropertyField(serializedRenderUnknownObjects);
            EditorGUILayout.PropertyField(serializedRenderWorldMesh);
            EditorGUILayout.PropertyField(serializedRequestInferredRegions);
            if(SUManager.RequestInferredRegions)
            {
                EditorGUILayout.PropertyField(serializedRenderCompletelyInferredSceneObjects);
            }
            GUILayout.Space(4.0f);

            //Ghost Mode
            EditorGUILayout.PropertyField(serializedisInGhostMode);
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedAddColliders);
            GUILayout.Space(4.0f);

            //Callbacks
            EditorGUILayout.PropertyField(serializedOnLoadStartedCallback);
            GUILayout.Space(4.0f);

            EditorGUILayout.PropertyField(serializedOnLoadFinishedCallback);
            GUILayout.Space(4.0f);

            //On Editor only
            if(!SUManager.RunOnDevice)
            {
                GUILayout.Label("Actions", EditorStyles.boldLabel);
                if(GUILayout.Button("Bake Scene", GUILayout.Width(90)))
                {
                    SUManager.BakeScene();
                }
            }

            //Apply Changes
            serializedObject.ApplyModifiedProperties();
        }
    }

}