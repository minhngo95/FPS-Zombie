﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-solocharactertriggerzonepersistant.html")]
	public class SoloCharacterTriggerZonePersistant : MonoBehaviour
	{
        [SerializeField, Tooltip("The event that is fired when a character enters the trigger collider.")]
        private CharacterEvent m_OnTriggerEnter = new CharacterEvent();

        [SerializeField, Tooltip("The event that is fired when a character exits the trigger collider.")]
        private CharacterEvent m_OnTriggerExit = new CharacterEvent();

        [SerializeField, Tooltip("The event that is fired each frame a character stays inside the trigger collider.")]
        private CharacterEvent m_OnTriggerStay = new CharacterEvent();
		
		[Serializable]
		public class CharacterEvent : UnityEvent<FpsSoloCharacter> {}

        private List<FpsSoloCharacter> m_Characters = new List<FpsSoloCharacter>();

        protected void OnTriggerEnter (Collider other)
		{
			if (other.CompareTag ("Player"))
			{
				FpsSoloCharacter c = other.GetComponentInParent<FpsSoloCharacter>();
				if (c != null)
                {
                    m_Characters.Add(c);
                    OnCharacterEntered(c);
                }
			}
		}

        protected void OnTriggerExit (Collider other)
		{
			if (other.CompareTag ("Player"))
			{
				FpsSoloCharacter c = other.GetComponentInParent<FpsSoloCharacter>();
				if (c != null)
                {
                    m_Characters.Remove(c);
                    OnCharacterExited(c);
                }
			}
		}

        protected void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                FpsSoloCharacter c = other.GetComponentInParent<FpsSoloCharacter>();
                if (c != null)
					OnCharacterStay(c);
            }
        }

        protected void OnDisable()
        {
			foreach (var c in m_Characters)
				OnCharacterExited(c);
			m_Characters.Clear();
        }

		protected virtual void OnCharacterEntered (FpsSoloCharacter c)
		{
			if (m_OnTriggerEnter != null)
				m_OnTriggerEnter.Invoke (c);
		}

		protected virtual void OnCharacterExited(FpsSoloCharacter c)
		{
			if (m_OnTriggerExit != null)
				m_OnTriggerExit.Invoke (c);
		}

		protected virtual void OnCharacterStay(FpsSoloCharacter c)
        {
            if (m_OnTriggerStay != null)
                m_OnTriggerStay.Invoke(c);
        }
    }
}