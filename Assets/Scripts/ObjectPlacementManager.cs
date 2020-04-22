// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.MixedReality.SceneUnderstanding.Samples.Unity
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

/// <summary>
/// When running on device (Hololens 2), this class will interact with gesture APis to assist in object placement logic integrated with a Scene Understanding
/// deserialized scene 
/// When running on PC, this class will use the main camera to simulate object placement
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
        public SceneUnderstandingDataProvider SUDataProvider = null;

        /// <summary>
        /// Reference to the Main Camera when not running on device
        /// </summary>
        [Tooltip("Reference to the Main Camera when not running on device")]
        public Camera mainCamera = null;

        /// <summary>
        /// Rendered line to guide placement
        /// </summary>
        public GameObject goLine = null;
        private RaycastHit raycastHit;
        private GameObject goCurrentHighlight = null;
        private Color currentHighlightBaseColor;
        bool isHighlightedObjGlowingUp;
        float fTimeStamp;
        float fTimeGlowing;

        [HideInInspector]
        public bool isPlacing;
        public Material placeableObjectsMaterial;

        [HideInInspector]
        public GameObject goCurrentObjectToPlace = null;
        private PrimitiveType goCurrentObjectToPlaceType;
        private Vector3 hoverPostion;
        private Vector3 highlightedObjFacingTowards;

        [HideInInspector]
        public GameObject goPlaceableObjectsContainer;

        /// <summary>
        /// Initialization.
        /// </summary>
        void Start()
        {
            SUDataProvider = SUDataProvider == null ? gameObject.GetComponent<SceneUnderstandingDataProvider>() : SUDataProvider;
            SUUtils = SUUtils == null ? gameObject.GetComponent<SceneUnderstandingUtils>() : SUUtils;
            mainCamera = mainCamera == null ? Camera.main : mainCamera;
            isPlacing = false;
            fTimeGlowing = 0.5f;

            goPlaceableObjectsContainer = SUUtils.CreateGameObject("PlaceableObjectContainer", null);
        }

        void Update()
        {
            

            if(SUDataProvider.RunOnDevice)
            {
                Logger.LogWarning("Object Placement not implemented when running on device");
                return;
            }
            else
            {
                if (isPlacing)
                {
                    if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
                    {
                        Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * raycastHit.distance, Color.yellow);
                        goLine.SetActive(true);
                        DrawLine(mainCamera.transform.position, raycastHit.point);

                        if (goCurrentHighlight != raycastHit.transform.gameObject)
                        {
                            //This means a different gameobject is being highlighted

                            //Reset color of the old highlighted object, if there's any
                            if (goCurrentHighlight != null)
                            {
                                goCurrentHighlight.GetComponent<MeshRenderer>().material.color = currentHighlightBaseColor;
                            }

                            //Set the new object as the highlighted object
                            currentHighlightBaseColor = raycastHit.transform.GetComponent<MeshRenderer>().material.color;
                            goCurrentHighlight = raycastHit.transform.gameObject;
                            fTimeStamp = Time.time;
                            isHighlightedObjGlowingUp = true;

                            highlightedObjFacingTowards = -goCurrentHighlight.transform.forward.normalized;
                        }

                        //figure out whether you are facing the object from the front or from the back
                        hoverPostion = Vector3.Dot(mainCamera.transform.TransformDirection(Vector3.forward), highlightedObjFacingTowards) < 0 ? raycastHit.point + (highlightedObjFacingTowards * 0.2f) : raycastHit.point - (highlightedObjFacingTowards * 0.2f);

                    }
                    else
                    {
                        Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                        goLine.SetActive(false);
                        //if you are not hitting anything no object is being highlighted at the moment
                        goCurrentHighlight = null;

                        hoverPostion = mainCamera.transform.position + (mainCamera.transform.forward * 2.0f);
                    }
                }
                else if (goCurrentHighlight != null || goCurrentObjectToPlace != null)
                {
                    goCurrentHighlight.GetComponent<MeshRenderer>().material.color = currentHighlightBaseColor;
                    goCurrentHighlight = null;
                    goLine.SetActive(false);
                    goCurrentObjectToPlace = null;
                }
            }

            CheckforHighlight();
            CheckforPlaceableObjectPosition(hoverPostion);
        }

        void DrawLine(Vector3 vc3Start, Vector3 vc3End)
        {
            LineRenderer line = goLine.GetComponent<LineRenderer>();
            line.startWidth = 0.15f;
            line.endWidth = 0.15f;
            line.positionCount = 2;
            line.SetPosition(0, vc3Start - (Vector3.up * 0.2f));
            line.SetPosition (1, vc3End);
        }

        void CheckforHighlight()
        {
            if (goCurrentHighlight != null)
            {
                if (isHighlightedObjGlowingUp)
                {
                    GlowUp(goCurrentHighlight);
                }
                else
                {
                    GlowDown(goCurrentHighlight);
                }

                if (Time.time > fTimeStamp + fTimeGlowing)
                {
                    isHighlightedObjGlowingUp = !isHighlightedObjGlowingUp;
                    fTimeStamp = Time.time;
                }
            }
        }

        void GlowUp(GameObject target)
        {
            Color currentColor = target.GetComponent<MeshRenderer>().material.color;
            float missingValueToMaxRed = 1.0f - currentColor.r;
            float missingValueToMaxGreen = 1.0f - currentColor.g;
            float missingValueToMaxBlue = 1.0f - currentColor.b;
            float IncreaseRed = missingValueToMaxRed * (Time.deltaTime / fTimeGlowing);
            float IncreaseBlue = missingValueToMaxGreen * (Time.deltaTime / fTimeGlowing);
            float IncreaseGreen = missingValueToMaxBlue * (Time.deltaTime / fTimeGlowing);
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
            float IncreaseRed = missingValueToMaxRed * (Time.deltaTime / fTimeGlowing);
            float IncreaseBlue = missingValueToMaxGreen * (Time.deltaTime / fTimeGlowing);
            float IncreaseGreen = missingValueToMaxBlue * (Time.deltaTime / fTimeGlowing);
            currentColor.r -= IncreaseRed;
            currentColor.g -= IncreaseBlue;
            currentColor.b -= IncreaseGreen;
            target.GetComponent<MeshRenderer>().material.color = currentColor;
        }

        void CheckforPlaceableObjectPosition(Vector3 pos)
        {
            if (goCurrentObjectToPlace != null && isPlacing)
            {
                goCurrentObjectToPlace.transform.position = pos;
            }
        }

        public void StartObjectPlacement(KeyCode key)
        {   
            if(goCurrentObjectToPlace == null && !isPlacing)
            {
                isPlacing = true;
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
            if (goCurrentObjectToPlace != null && isPlacing)
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
                isPlacing = false;
            }
        }

    }
}