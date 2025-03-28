﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-mb-billboardorientation.html")]
    public class BillboardOrientation : MonoBehaviour
    {
        [SerializeField, Tooltip("The billboard surface normal direction (this will be turned towards the camera)")]
        private BillboardNormal m_BillboardNormal = BillboardNormal.Back;

        private Transform m_Target = null;
        private Transform m_LocalTransform = null;

        public enum BillboardNormal
        {
            Forward,
            Back,
            Left,
            Right,
            Up,
            Down
        }

        protected void Start()
        {
            m_LocalTransform = transform;
            FirstPersonCameraBase.onCurrentCameraChanged += OnCurrentCameraChanged;
            OnCurrentCameraChanged(FirstPersonCameraBase.current);
        }

        protected void OnDestroy()
        {
            FirstPersonCameraBase.onCurrentCameraChanged -= OnCurrentCameraChanged;
        }

        protected void LateUpdate()
        {
            if (m_Target != null)
            {
                var direction = (m_Target.position - m_LocalTransform.position).normalized;
                switch(m_BillboardNormal)
                {
                    case BillboardNormal.Forward:
                        transform.rotation = Quaternion.FromToRotation(Vector3.forward, direction);
                        break;
                    case BillboardNormal.Back:
                        transform.rotation = Quaternion.FromToRotation(Vector3.back, direction);
                        break;
                    case BillboardNormal.Left:
                        transform.rotation = Quaternion.FromToRotation(Vector3.left, direction);
                        break;
                    case BillboardNormal.Right:
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
                        break;
                    case BillboardNormal.Up:
                        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                        break;
                    case BillboardNormal.Down:
                        transform.rotation = Quaternion.FromToRotation(Vector3.down, direction);
                        break;
                }
            }
        }

        void OnCurrentCameraChanged(FirstPersonCameraBase cam)
        {
            if (cam == null)
            {
                var main = Camera.main;
                if (main != null)
                    m_Target = main.transform;
                else
                    m_Target = null;
            }
            else
                m_Target = cam.cameraTransform;
        }
    }
}