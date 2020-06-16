// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

/// <summary>
/// When running on device (Hololens 2), this class will interact with gesture APis to assist in object placement logic integrated with a Scene Understanding
/// deserialized scene 
/// When running on PC, this class will use the main camera to simulate object placement and texture changes on Scene Understanding objects
/// </summary>
    public class ObjectPlacementManager : MonoBehaviour
    {
        /// <summary>
        /// Scene Understanding utilities component.
        /// </summary>
        [Tooltip("Scene Understanding utilities component.")]
        public SceneUnderstandingUtils SUUtils = null;
        
        /// <summary>
        /// Scene Understanding data provider component.
        /// </summary>
        [Tooltip("Scene Understanding Data provider component.")]
        public SceneUnderstandingDataProvider SUDataProvider = null;

        /// <summary>
        /// Scene Understanding display manager component.
        /// </summary>
        [Tooltip("Scene Understanding display manager component.")]
        public SceneUnderstandingDisplayManager SUDisplayManager = null;

        /// <summary>
        /// Reference to the Unity Main Camera used when not running on device
        /// </summary>
        [Tooltip("Reference to the Unity Main Camera used when not running on device")]
        public Camera mainCamera = null;

        /// <summary>
        /// This Gameobject contains a LineRender that helps the user where they are pointing at when
        /// placing objects or textures
        /// </summary>
        [Tooltip("This Gameobject contains a LineRender that helps the user where they are pointing at when placing objects or textures")]
        public GameObject goLine = null;

        /// <summary>
        /// Reference to the Raycast object used to know where the user is looking at
        /// </summary>
        [Tooltip("Reference to the Raycast object used to know where the user is looking at")]
        private RaycastHit raycastHit;

        /// <summary>
        /// Reference to the gameobject that is being selected for something to be placed in it
        /// or to change its texture
        /// </summary>
        [Tooltip("Reference to the gameobject that is being selected for something to be placed in it or to change its texture")]
        private GameObject goCurrentSelected = null;

        /// <summary>
        /// This variable is used to store the original value for a selected object's texture
        /// </summary>
        [Tooltip("This variable is used to store the original value for a selected object's texture")]
        private Texture currentSelectedBaseTexture = null;

        /// <summary>
        /// This variable is used to store the original value for a selected object's base color
        /// </summary>
        [Tooltip("This variable is used to store the original value for a selected object's texture")]
        private Color currentSelectedBaseColor;

        /// <summary>
        /// When an object is selected it will glow brighter or dimmer, this flag is to keep track of that
        /// </summary>
        [Tooltip("When an object is selected it will glow brighter or dimmer, this flag is to keep track of that")]
        private bool isSelectedObjGlowingUp;

        /// <summary>
        /// This is the amount of time a selected object will grow brighter before it starts going dimmer and viseversa
        /// </summary>
        [Tooltip("This is the amount of time a selected object will grow brighter before it starts going dimmer and viseversa")]
        private float fTimeGlowingUpOrDown;
        
        /// <summary>
        /// This is a timestamp, in-game time to know when was the moment a selected object started glowing brighter or dimmer.
        /// This variable, together with fTimeGlowingUpOrDown, will help us keep a glowing effect for selected objects in a loop
        /// using the standard unity update
        /// </summary>
        [Tooltip("This is a timestamp, in-game time to know when was the moment a selected object started glowing brighter or dimmer." + 
                 "This variable, together with fTimeGlowingUpOrDown, will help us keep a glowing effect for selected objects" + 
                 "in a loop using the standard unity update")]
        private float fTimeStampLastGlowUpOrDown;

        /// <summary>
        /// Flag to know if user is currently placing an object
        /// </summary>
        [Tooltip("Flag to know if user is currently placing an object")]
        [HideInInspector]
        public bool isPlacingObject;

        /// <summary>
        /// Flag to know if user is currently placing a texture
        /// </summary>
        [Tooltip("Flag to know if user is currently placing a texture")]
        [HideInInspector]
        public bool isPlacingTexture;

        /// <summary>
        /// This is the default material for objects in a Scene Understanding scene
        /// </summary>
        [Tooltip("This is the default material for objects in a Scene Understanding scene")]
        public Material placeableObjectsMaterial;

        /// <summary>
        /// This is a reference for the object that is currently being placed in the scene
        /// </summary>
        [Tooltip("This is a reference for the object that is currently being placed in the scene")]
        [HideInInspector]
        public GameObject goCurrentObjectToPlace = null;

        /// <summary>
        /// This is a reference for the type of object that is currently being placed in the scene
        /// </summary>
        [Tooltip("This is a reference for the type of object that is currently being placed in the scene")]
        private PrimitiveType goCurrentObjectToPlaceType;

        /// <summary>
        /// This is a reference to the position of an object in the process of being placed
        /// </summary>
        [Tooltip("This is a reference to the position of an object in the process of being placed")]
        private Vector3 v3CurrentObjToPlaceHoverPosition;

        /// <summary>
        /// This is a direction, simulates a selected object's (wall, floor, platform) normal vector
        /// </summary>
        [Tooltip("This is a direction, simulates a selected object's (wall, floor, platform) normal vector")]
        private Vector3 selectedObjFacingTowards;

        /// <summary>
        /// This is an empty GameObject to contain all placed objects that are not part of the orignal scene,
        /// use this to not polute the hierarchy in the unity inspector
        /// </summary>
        [Tooltip("This is an empty GameObject to contain all placed objects that are not part of the orignal scene, use this to not polute the hierarchy in the unity inspector")]
        [HideInInspector]
        public GameObject goPlaceableObjectsContainer;

        /// <summary>
        /// This is a reference to the custom texture that can be placed on scene objects
        /// </summary>
        [Tooltip("This is a reference to the custom texture that can be placed on scene objects")]
        [HideInInspector]
        public Texture textureRug;

        void Start()
        {
            mainCamera = mainCamera == null ? Camera.main : mainCamera;
            isPlacingObject = false;
            isPlacingTexture = false;
            fTimeGlowingUpOrDown = 0.5f;
            goPlaceableObjectsContainer = SUUtils.CreateGameObject("PlaceableObjectContainer", null);
        }

        void Update()
        {
            if (isPlacingObject || isPlacingTexture)
            {
                CastRaycastAndCheckForHit();
            } 
            else if (goCurrentSelected != null || goCurrentObjectToPlace != null)
            {
                goCurrentSelected.GetComponent<MeshRenderer>().material.color = currentSelectedBaseColor;
                goCurrentSelected = null;
                goLine.SetActive(false);
                goCurrentObjectToPlace = null;
            }
            
            CheckforHighlight();
            CheckforPlaceableObjectPosition();
            CheckForTextureOnHighlightedObject();
        }


        void CastRaycastAndCheckForHit()
        {
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
            {
                if(!SUDataProvider.RunOnDevice)
                {
                    DrawLine(mainCamera.transform.position, raycastHit.point);
                }

                //This means a different gameobject is being highlighted
                if (goCurrentSelected != raycastHit.transform.gameObject)
                {
                    //Reset color of the old highlighted object, if there's any
                    if (goCurrentSelected != null && SUDisplayManager.SceneObjectVisualizationMode != VisualizationMode.Wireframe)
                    {
                        goCurrentSelected.GetComponent<MeshRenderer>().material.color = currentSelectedBaseColor;
                        goCurrentSelected.GetComponent<MeshRenderer>().material.mainTexture = currentSelectedBaseTexture;
                    }

                    //Set the new object as the highlighted object
                    if(SUDisplayManager.SceneObjectVisualizationMode != VisualizationMode.Wireframe)
                    {
                        currentSelectedBaseColor = raycastHit.transform.GetComponent<MeshRenderer>().material.color;
                        currentSelectedBaseTexture = raycastHit.transform.GetComponent<MeshRenderer>().material.mainTexture;
                    }

                    //Set the new Object that you are hitting with raycast as new highlighted object
                    goCurrentSelected = raycastHit.transform.gameObject;
                    fTimeStampLastGlowUpOrDown = Time.time;
                    isSelectedObjGlowingUp = true;

                    selectedObjFacingTowards = -goCurrentSelected.transform.forward.normalized;
                }

                if(isPlacingObject)
                {
                    //figure out whether you are facing the object from the front or from the back
                    v3CurrentObjToPlaceHoverPosition = Vector3.Dot(mainCamera.transform.TransformDirection(Vector3.forward), selectedObjFacingTowards) < 0 ? raycastHit.point + (selectedObjFacingTowards * 0.2f) : raycastHit.point - (selectedObjFacingTowards * 0.2f);
                }
            }
            else
            {
                //if you are not hitting anything no object is being highlighted at the moment
                goLine.SetActive(false);
                goCurrentSelected = null;
                v3CurrentObjToPlaceHoverPosition = mainCamera.transform.position + (mainCamera.transform.forward * 2.0f);
            }
        }

        void CheckForTextureOnHighlightedObject()
        {
            if(goCurrentSelected != null && isPlacingTexture)
            {
                if(goCurrentSelected.GetComponent<MeshRenderer>().material.mainTexture == currentSelectedBaseTexture)
                {
                    goCurrentSelected.GetComponent<MeshRenderer>().material.mainTexture = textureRug;
                }
            }
        }

        void DrawLine(Vector3 vc3Start, Vector3 vc3End)
        {
            goLine.SetActive(true);
            LineRenderer line = goLine.GetComponent<LineRenderer>();
            line.startWidth = 0.15f;
            line.endWidth = 0.15f;
            line.positionCount = 2;
            line.SetPosition(0, vc3Start - (Vector3.up * 0.2f));
            line.SetPosition (1, vc3End);
        }

        void CheckforHighlight()
        {
            if (goCurrentSelected != null && SUDisplayManager.SceneObjectVisualizationMode != VisualizationMode.Wireframe)
            {
                if (isSelectedObjGlowingUp)
                {
                    GlowUp(goCurrentSelected);
                }
                else
                {
                    GlowDown(goCurrentSelected);
                }

                if (Time.time > fTimeStampLastGlowUpOrDown + fTimeGlowingUpOrDown)
                {
                    isSelectedObjGlowingUp = !isSelectedObjGlowingUp;
                    fTimeStampLastGlowUpOrDown = Time.time;
                }
            }
        }

        void GlowUp(GameObject target)
        {
            Color currentColor = target.GetComponent<MeshRenderer>().material.color;
            float missingValueToMaxRed = 1.0f - currentColor.r;
            float missingValueToMaxGreen = 1.0f - currentColor.g;
            float missingValueToMaxBlue = 1.0f - currentColor.b;
            float IncreaseRed = missingValueToMaxRed * (Time.deltaTime / fTimeGlowingUpOrDown);
            float IncreaseBlue = missingValueToMaxGreen * (Time.deltaTime / fTimeGlowingUpOrDown);
            float IncreaseGreen = missingValueToMaxBlue * (Time.deltaTime / fTimeGlowingUpOrDown);
            currentColor.r += IncreaseRed;
            currentColor.g += IncreaseBlue;
            currentColor.b += IncreaseGreen;
            target.GetComponent<MeshRenderer>().material.color = currentColor;
        }

        void GlowDown(GameObject target)
        {
            Color currentColor = target.GetComponent<MeshRenderer>().material.color;
            float missingValueToMaxRed = 1.0f - currentColor.r;
            float missingValueToMaxGreen = 1.0f - currentColor.g;
            float missingValueToMaxBlue = 1.0f - currentColor.b;
            float IncreaseRed = missingValueToMaxRed * (Time.deltaTime / fTimeGlowingUpOrDown);
            float IncreaseBlue = missingValueToMaxGreen * (Time.deltaTime / fTimeGlowingUpOrDown);
            float IncreaseGreen = missingValueToMaxBlue * (Time.deltaTime / fTimeGlowingUpOrDown);
            currentColor.r -= IncreaseRed;
            currentColor.g -= IncreaseBlue;
            currentColor.b -= IncreaseGreen;
            target.GetComponent<MeshRenderer>().material.color = currentColor;
        }

        void CheckforPlaceableObjectPosition()
        {
            if (goCurrentObjectToPlace != null && isPlacingObject)
            {
                goCurrentObjectToPlace.transform.position = v3CurrentObjToPlaceHoverPosition;
            }
        }

        public void StartObjectPlacement(KeyCode key)
        {   
            if(goCurrentObjectToPlace == null && !isPlacingObject)
            {
                isPlacingObject = true;
                switch(key)
                {
                    case KeyCode.C:
                        goCurrentObjectToPlace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        goCurrentObjectToPlaceType = PrimitiveType.Cube;
                        break;
                    case KeyCode.V:
                        goCurrentObjectToPlace = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        goCurrentObjectToPlaceType = PrimitiveType.Sphere;
                        break;
                    default:
                        Logger.LogError("ObjectPlacementManager.StartObjectPlacement: Object Type unidentified");
                    break;
                }
                
                Destroy(goCurrentObjectToPlace.GetComponent<Collider>());
                goCurrentObjectToPlace.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
                goCurrentObjectToPlace.GetComponent<MeshRenderer>().material = placeableObjectsMaterial;
                goCurrentObjectToPlace.transform.parent = goPlaceableObjectsContainer.transform;
            }
        }

        public void FinishObjectPlacement()
        {
            if (goCurrentObjectToPlace != null && isPlacingObject)
            {
                goCurrentObjectToPlace.AddComponent<Rigidbody>();
                switch(goCurrentObjectToPlaceType)
                {
                    case PrimitiveType.Cube:
                        goCurrentObjectToPlace.AddComponent<BoxCollider>();
                    break;
                    case PrimitiveType.Sphere:
                        goCurrentObjectToPlace.AddComponent<SphereCollider>();
                    break;
                    default:
                        Logger.LogError("ObjectPlacementManager.FinishObjectPlacement: Object Type unidentified");
                    break;
                }
                goCurrentObjectToPlace = null;
                isPlacingObject = false;
            }
        }

        public void StartTexturePlacement()
        {
            if(!isPlacingTexture)
            {
                isPlacingTexture = true;
            }
        }

        public void FinishTexturePlacement()
        {
            if(isPlacingTexture)
            {
                isPlacingTexture = false;
            }
        }

    }
}