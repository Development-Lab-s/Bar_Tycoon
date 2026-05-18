using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;

public sealed class StoryTransitionSamplerCameraShakeRemovalTests
{
    [Test]
    public void CollectCameraShakeKeysBetween_IgnoresLegacyShakeKeys()
    {
        var track = new StoryCameraTrackData
        {
            keyframes = new List<StoryActorKeyframeData>
            {
                new()
                {
                    property = StoryActorKeyframeProperty.CameraShake,
                    timeSeconds = 0.2f,
                    cameraShakeStrength = 1f,
                    cameraShakeDuration = 0.25f,
                    cameraShakeFrequency = 2f
                }
            }
        };

        var results = new List<StoryActorKeyframeData>();
        StoryTransitionSampler.CollectCameraShakeKeysBetween(track, 0f, 1f, results);

        Assert.That(results, Is.Empty);
    }
}
