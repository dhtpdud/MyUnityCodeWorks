using System;
using JSchool.Common.Script.BaseFramework.Lecture.Addressable.Sound;
using UnityEngine;
using UnityEngine.Audio;

namespace JSchool.Modules.Common.OSY
{
    public class SYAudioSpectrum : MonoBehaviour
    {
        public AudioSource audioSource;
        [SerializeField] AudioMixerGroup microphoneMixer;
        public float[] SpectrumData { get; protected set; }
        [SerializeField] private FFTWindow type = FFTWindow.Rectangular;

        private void Awake()
        {
            SpectrumData = new float[64]; 
        }

        // Update is called once per frame
        void Update()
        {
            if(audioSource)
                audioSource.GetSpectrumData(SpectrumData, 0, type);
            else
            {
                audioSource = SoundLoader.GetInstantAudioSource(true);
                audioSource.outputAudioMixerGroup = microphoneMixer;
            }
        }

        public void SetMicOn(bool val)
        {
            if(val)
                audioSource.outputAudioMixerGroup = microphoneMixer;
            else
                audioSource.outputAudioMixerGroup = null;
        }
    }

}