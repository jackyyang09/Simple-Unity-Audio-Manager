using UnityEngine;

namespace JSAM
{
    public interface IAudioHelperEvents
    {
        void TimeScaleChanged(float prevTimeScale);
        void VolumeChanged(float channelVolume, float realVolume);
        void Spatialize();
    }
}