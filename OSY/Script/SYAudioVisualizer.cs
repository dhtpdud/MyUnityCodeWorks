using UnityEngine;

namespace JSchool.Modules.Common.OSY
{
    public class SYAudioVisualizer : MonoBehaviour
    {
        public SYAudioSpectrum spectrum;
        public Transform[] targets;
        [SerializeField] private float[] maxHeights;
        [SerializeField] private float power = 20;
        [SerializeField] private float updateSpeed = 5;


        private void Awake()
        {
            maxHeights = new float[targets.Length];
            for (var i = 0; i < maxHeights.Length; i++)
                maxHeights[i] = maxHeights[i].Equals(0) ? targets[i].localScale.y : maxHeights[i];
        }

        // Update is called once per frame
        void Update()
        {
            if (spectrum.SpectrumData == null) return;
            for (var i = 0; i < targets.Length; i++)
            {
                Vector2 size = targets[i].localScale;
                size.y = Mathf.Clamp(
                    Mathf.Lerp(size.y, spectrum.SpectrumData[i] * power * maxHeights[i], updateSpeed * Time.deltaTime),
                    0,
                    maxHeights[i]);
                targets[i].localScale = size;
            }
        }
    }
}
