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
        bool highlightGlowUp;
        float fTimeStamp;
        float fTimeGlowing;
        public bool isPlacing;

        public Material placeableObjectsMaterial;

        private GameObject goCurrentObjectToPlace = null;

        private Vector3 hoverPostion;

        /// <summary>
        /// Initialization.
        /// </summary>
        void Start()
        {
            SUUtils = SUUtils == null ? gameObject.GetComponent<SceneUnderstandingUtils>() : SUUtils;
            SUDataProvider = SUDataProvider == null ? gameObject.GetComponent<SceneUnderstandingDataProvider>() : SUDataProvider;
            mainCamera = mainCamera == null ? Camera.main : mainCamera;
            isPlacing = false;
            fTimeGlowing = 0.5f;
        }

        void Update()
        {
            if(SUDataProvider.RunOnDevice)
            {
                Logger.LogError("Placement on device not implemented when running on device");
                return;
            }
            else 
            {
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward), out raycastHit, Mathf.Infinity))
                {
                    Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * raycastHit.distance, Color.yellow);
                    DrawLine(mainCamera.transform.position,raycastHit.point);
                    
                    if(goCurrentHighlight != raycastHit.transform.gameObject)
                    {
                        //This means a different gameobject is being highlighted

                        //Reset color of the old highlighted object, if there's any
                        if(goCurrentHighlight != null)
                        {
                            goCurrentHighlight.GetComponent<MeshRenderer>().material.color = currentHighlightBaseColor;
                        }

                        //Set the new object as the highlighted object
                        currentHighlightBaseColor = raycastHit.transform.GetComponent<MeshRenderer>().material.color;
                        goCurrentHighlight = raycastHit.transform.gameObject;
                        fTimeStamp = Time.time;
                        highlightGlowUp = true;
                    }
                    
                    hoverPostion = raycastHit.point;
                }
                else
                {
                    Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                    DrawLine(mainCamera.transform.position,mainCamera.transform.TransformDirection(Vector3.forward) * 1000);

                    //if you are not hitting anything no object is being highlighted at the moment
                    goCurrentHighlight = null;

                    hoverPostion = mainCamera.transform.position + (mainCamera.transform.forward * 1.0f);
                }
            }

            CheckforHighlight();
            CheckforPlaceableObjectPosition(hoverPostion);
        }

        void DrawLine(Vector3 vc3Start, Vector3 vc3End)
        {
            LineRenderer line = goLine.GetComponent<LineRenderer>();
            line.startWidth = 0.25f;
            line.endWidth = 0.25f;
            line.positionCount = 2;
            line.SetPosition(0, vc3Start - (Vector3.up * 0.2f));
            line.SetPosition (1, vc3End);
        }

        void CheckforHighlight()
        {
            if(goCurrentHighlight == null)
            {
                return;
            }

            if(highlightGlowUp)
            {
                GlowUp(goCurrentHighlight);
            }
            else 
            {
                GlowDown(goCurrentHighlight);
            }

            if(Time.time > fTimeStamp + fTimeGlowing)
            {
                highlightGlowUp = !highlightGlowUp;
                fTimeStamp = Time.time;
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

        public void StartCubePlacement()
        {   
            isPlacing = true;
            goCurrentObjectToPlace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(goCurrentObjectToPlace.GetComponent<BoxCollider>());
            goCurrentObjectToPlace.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
            goCurrentObjectToPlace.GetComponent<MeshRenderer>().material = placeableObjectsMaterial;
        }

        void CheckforPlaceableObjectPosition(Vector3 pos)
        {
            if(goCurrentObjectToPlace == null || !isPlacing)
            {
                return;
            }

            goCurrentObjectToPlace.transform.position = pos;
        }

        public void FinishCubePlacement()
        {
            goCurrentObjectToPlace = null;
            isPlacing = false;
        }
    }

}