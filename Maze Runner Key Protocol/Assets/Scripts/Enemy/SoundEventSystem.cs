using UnityEngine;
using System;

public enum SoundType { Gunshot, Footstep }

/// <summary>
/// Static broadcast system for sound events. Enemies subscribe to hear gunshots/footsteps.
/// </summary>
public static class SoundEventSystem
{
    public static event Action<Vector3, float, SoundType> OnSoundBroadcast;

    public static void BroadcastSound(Vector3 origin, float radius, SoundType type)
    {
        OnSoundBroadcast?.Invoke(origin, radius, type);
    }
}
