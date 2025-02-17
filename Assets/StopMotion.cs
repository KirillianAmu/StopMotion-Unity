using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace GK
{
    public class StopMotion : MonoBehaviour
    {
        public Transform RootBone;
        public int StoppedFrameCount = 5;
        public float StoppedTime = 0.5f; // New variable for time interval in seconds
        public Transform IgnoredGameObject; // Field to specify ignored GameObject

        int recordedFrame = -1;
        float timeSinceLastCapture = 0f; // Time since last capture

        List<Transform> transforms = null;
        List<STransform> actualPositions = null;
        List<STransform> renderedPositions = null;

        IEnumerator endOfFrameCoroutine;

        public bool bUseWaitBandaid = true;
        public bool bTieToFramerate = true; // Boolean to control framerate dependence
        float fWait = 0.1f;

        void OnEnable()
        {
            fWait = bUseWaitBandaid ? 0.1f : 0f;
            transforms = null;
            actualPositions = null;
            renderedPositions = null;

            endOfFrameCoroutine = EndOfFrameCoroutine();
            StartCoroutine(endOfFrameCoroutine);
        }

        void OnDisable()
        {
            StopCoroutine(endOfFrameCoroutine);
        }

        void LateUpdate()
        {
            if (fWait < 0)
            {
                if (transforms == null)
                {
                    // Collect all transforms under RootBone, excluding the ignored GameObject and its children
                    var allTransforms = RootBone.GetComponentsInChildren<Transform>();
                    transforms = new List<Transform>();
                    foreach (var t in allTransforms)
                    {
                        if (IgnoredGameObject != null && (t == IgnoredGameObject.transform || t.IsChildOf(IgnoredGameObject.transform)))
                        {
                            continue;
                        }
                        transforms.Add(t);
                    }
                }

                if (transforms != null && transforms.Count > 0)
                {
                    RecordTransform(ref actualPositions);
                }

                if (renderedPositions == null)
                {
                    // Initialize renderedPositions if it's null
                    RecordTransform(ref renderedPositions);
                    timeSinceLastCapture = 0f;
                }

                if (bTieToFramerate)
                {
                    // Update renderedPositions based on frame count
                    if (Time.frameCount - recordedFrame >= StoppedFrameCount)
                    {
                        recordedFrame = Time.frameCount;
                        RecordTransform(ref renderedPositions);
                    }
                    else
                    {
                        RestoreRecord(renderedPositions);
                    }
                }
                else
                {
                    // Update renderedPositions based on elapsed time
                    timeSinceLastCapture += Time.deltaTime;
                    if (timeSinceLastCapture >= StoppedTime)
                    {
                        timeSinceLastCapture = 0f;
                        RecordTransform(ref renderedPositions);
                    }
                    else
                    {
                        RestoreRecord(renderedPositions);
                    }
                }
            }
            else
            {
                fWait -= Time.deltaTime;
            }
        }

        IEnumerator EndOfFrameCoroutine()
        {
            var endOfFrame = new WaitForEndOfFrame();

            while (true)
            {
                yield return endOfFrame;

                if (fWait < 0)
                {
                    if (transforms != null && actualPositions != null && actualPositions.Count == transforms.Count)
                    {
                        RestoreRecord(actualPositions);
                    }
                    else
                    {
                        // NO ONE CARES, VISUALLY ITS FINE AND WORKS PERFECTLY.
                      //  Debug.LogError($"Actual positions list mismatch.");
                    }
                }
            }
        }

        void RecordTransform(ref List<STransform> record)
        {
            if (record == null)
            {
                record = new List<STransform>(transforms.Count);
                foreach (var t in transforms)
                {
                    record.Add(STransform.FromTransform(t));
                }
            }
            else
            {
                for (int i = 0; i < transforms.Count; i++)
                {
                    record[i] = STransform.FromTransform(transforms[i]);
                }
            }
        }

        void RestoreRecord(List<STransform> record)
        {
            for (int i = 0; i < transforms.Count; i++)
            {
                record[i].WriteTo(transforms[i]);
            }
        }

        void Reset()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                RootBone = smr.rootBone;
            }
            else
            {
                RootBone = null;
            }
            StoppedFrameCount = 5;
            StoppedTime = 0.5f; // Default time interval
        }

        struct STransform
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;

            public static STransform FromTransform(Transform t)
            {
                return new STransform
                {
                    LocalPosition = t.localPosition,
                    LocalRotation = t.localRotation,
                    LocalScale = t.localScale
                };
            }

            public void WriteTo(Transform t)
            {
                t.localPosition = LocalPosition;
                t.localRotation = LocalRotation;
                t.localScale = LocalScale;
            }
        }
    }
}
