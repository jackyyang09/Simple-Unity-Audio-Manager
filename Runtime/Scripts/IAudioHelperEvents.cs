using UnityEngine.SceneManagement;

namespace JSAM
{
    public interface IAudioHelperEvents
    {
        void SceneUnloaded(Scene scene);
        void TimeScaleChanged(float prevTimeScale);
        void VolumeChanged(float channelVolume, float realVolume);
        void Spatialize();
    }
}