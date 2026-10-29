using UnityEngine;

namespace SlimeHub.Core
{
    public interface IInteractable
    {
        void OnPress(Vector3 worldPoint, Vector3 worldVelocity);
        void OnDrag(Vector3 worldPoint, Vector3 worldVelocity);
        void OnRelease();
    }

    public interface IDamageable
    {
        void Damage(float amount, Vector3 point, Vector3 normal);
    }

    public interface IAudioService
    {
        void Play(Sfx sfx, float pitch = 1f, float volume = 1f);
        void PlayGiggle(float pitch);
        void SetStretchLoop(bool active, float pitch);
        void SetMuted(bool muted);
        bool Muted { get; }
    }

    public interface IHapticsService
    {
        void Light();
        void Medium();
        void Heavy();
        bool Enabled { get; set; }
    }

    public interface ILeaderboardService
    {
        int GetBest(string boardId);
        bool Submit(string boardId, int score);
    }
}
