﻿#if !NEOFPS_FORCE_QUALITY && (UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || (UNITY_WSA && NETFX_CORE) || NEOFPS_FORCE_LIGHTWEIGHT)
#define NEOFPS_LIGHTWEIGHT
#endif

using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoSaveGames.Serialization;
using UnityEngine.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Airborne/Falling", "Falling")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-fallingstate.html")]
    public class FallingState : MotionGraphState
    {
        [SerializeField, Tooltip("The input driven acceleration while falling.")]
        private FloatDataReference m_HorizontalAcceleration = new FloatDataReference(50f);

        [SerializeField, FormerlySerializedAs("m_HorizontalAcceleration"), Tooltip("The input driven deceleration while falling (if input is zero or less than previous).")]
        private FloatDataReference m_HorizontalDeceleration = new FloatDataReference(50f);

        [SerializeField, Tooltip("The top horizontal movement speed (for keyboard input or max analog input)")]
        private FloatDataReference m_TopSpeed = new FloatDataReference(5f);

        [SerializeField, Tooltip("The multiplier applied to the max movement speed when strafing")]
        private FloatDataReference m_StrafeMultiplier = new FloatDataReference(0.75f);

        [SerializeField, Tooltip("The multiplier applied to the max movement speed when moving in reverse")]
        private FloatDataReference m_ReverseMultiplier = new FloatDataReference(0.5f);

        [SerializeField, Tooltip("A drag acceleration applied to horizontal movement")]
        private FloatDataReference m_HorizontalDrag = new FloatDataReference(0f);

        [SerializeField, HideInInspector]
        private bool m_ClampSpeed = false;

        [SerializeField, Tooltip("Should the speed of the character decelerate to top speed")]
        private MomentumConservation m_MomentumConservation = MomentumConservation.MaintainSpeed;

        [SerializeField, Range(0f, 1f), Tooltip("The amount of damping to apply when changing direction")]
        private float m_Damping = 0.25f;

        enum MomentumConservation
        {
            MaintainSpeed,
            ClampSpeed,
            ClampToInput
        }

        private const float k_TinyValue = 0.001f;

        private Vector3 m_MotorAcceleration = Vector3.zero;
        private Vector3 m_OutVelocity = Vector3.zero;

        protected Vector3 fallVelocity
        {
            get { return m_OutVelocity; }
        }

        public override bool applyGroundingForce
        {
            get { return false; }
        }

        public override Vector3 moveVector
        {
            get { return m_OutVelocity * Time.deltaTime; }
        }

        public override bool ignorePlatformMove
        {
            get { return true; }
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_HorizontalAcceleration.ClampValue(0f, 1000f);
            m_TopSpeed.ClampValue(0f, 1f);
            m_StrafeMultiplier.ClampValue(0f, 1f);
            m_ReverseMultiplier.ClampValue(0f, 1f);

            if (m_ClampSpeed)
            {
                m_MomentumConservation = MomentumConservation.ClampSpeed;
                m_ClampSpeed = false;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            m_MotorAcceleration = Vector3.zero;
            m_OutVelocity = characterController.velocity;
        }

        public override void OnExit()
        {
            base.OnExit();
            m_OutVelocity = Vector3.zero;
        }

        public override void Update()
        {
            base.Update();

            // Update the current velocity (Note: The y value is handled by the standard gravity calculations in the parent mover)
            Vector3 up = characterController.up;
            Vector3 upVelocity = up * Vector3.Dot(characterController.velocity, up);
            Vector3 hVelocity = characterController.velocity - upVelocity;
            
            float topSpeed = m_TopSpeed.value;
            float directionMultiplier = 1f;
            Vector3 targetVelocity = hVelocity;
            bool decelerating = false;

            if (controller.inputMoveScale < k_TinyValue)
            {
                switch(m_MomentumConservation)
                {
                    case MomentumConservation.ClampSpeed:
                        {
                            float beforeSqrMag = targetVelocity.sqrMagnitude;
                            targetVelocity = Vector3.ClampMagnitude(targetVelocity, topSpeed);
                            float afterSqrMag = targetVelocity.sqrMagnitude;
                            if (beforeSqrMag > afterSqrMag)
                                decelerating = true;
                        }
                        break;
                    case MomentumConservation.ClampToInput:
                        {
                            targetVelocity = Vector3.zero;
                            decelerating = true;
                        }
                        break;
                }
            }
            else
            {
                // Calculate speed based on move direction
                if (controller.inputMoveDirection.y < 0f)
                    directionMultiplier *= Mathf.Lerp(1f, m_ReverseMultiplier.value, -controller.inputMoveDirection.y);
                directionMultiplier *= Mathf.Lerp(1f, m_StrafeMultiplier.value, Mathf.Abs(controller.inputMoveDirection.x));

                // Get the input based move direction
                Vector3 direction = characterController.forward * controller.inputMoveDirection.y;
                direction += characterController.right * controller.inputMoveDirection.x;

                // Get the target speed based on the input & current speed in the desired direction
                float inputSpeed = topSpeed * controller.inputMoveScale * directionMultiplier;
                float alignedSpeed = Vector3.Dot(hVelocity, direction);

                // Clamp top speed
                if (m_MomentumConservation != MomentumConservation.MaintainSpeed && alignedSpeed > topSpeed)
                {
                    decelerating = true;
                    alignedSpeed = topSpeed;
                }

                // Get the target vector
                targetVelocity = direction * Mathf.Max(inputSpeed, alignedSpeed);
            }

            // Apply drag
            if (m_HorizontalDrag.value > k_TinyValue)
            {
                float dragMultiplier = Mathf.Clamp01(1f - (m_HorizontalDrag.value * Time.deltaTime));
                targetVelocity *= dragMultiplier;
            }

            // Change velocity
            float hAcceleration = decelerating ? m_HorizontalDeceleration.value : m_HorizontalAcceleration.value;
            if (hAcceleration < k_TinyValue)
            {
                // Don't use acceleration (instant)
                m_OutVelocity = targetVelocity;
            }
            else
            {
                // Accelerate if required
                if (targetVelocity != hVelocity)
                {
                    // Get maximum acceleration
                    float maxAccel = hAcceleration * directionMultiplier;
                    // Accelerate the velocity
                    m_OutVelocity = Vector3.SmoothDamp(hVelocity, targetVelocity, ref m_MotorAcceleration, Mathf.Lerp(0.05f, 0.25f, m_Damping), maxAccel);
                }
                else
                    m_OutVelocity = targetVelocity;
            }
            
            // Set the local vertical to match the previous velocity
            m_OutVelocity = Vector3.ProjectOnPlane(m_OutVelocity, up);
            m_OutVelocity += upVelocity;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            base.CheckReferences(map);
            m_TopSpeed.CheckReference(map);
            m_StrafeMultiplier.CheckReference(map);
            m_ReverseMultiplier.CheckReference(map);
            m_HorizontalAcceleration.CheckReference(map);
            m_HorizontalDeceleration.CheckReference(map);
        }

        #region SAVE / LOAD

        private static readonly NeoSerializationKey k_AccelerationKey = new NeoSerializationKey("acceleration");
        private static readonly NeoSerializationKey k_VelocityKey = new NeoSerializationKey("velocity");

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);
            writer.WriteValue(k_AccelerationKey, m_MotorAcceleration);
            writer.WriteValue(k_VelocityKey, m_OutVelocity);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);
            reader.TryReadValue(k_AccelerationKey, out m_MotorAcceleration, m_MotorAcceleration);
            reader.TryReadValue(k_VelocityKey, out m_OutVelocity, m_OutVelocity);
        }

        #endregion
    }
}