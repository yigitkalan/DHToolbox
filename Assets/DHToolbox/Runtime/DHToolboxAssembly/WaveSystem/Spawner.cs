using System;
using System.Linq;
using DHToolbox.Runtime.DHToolboxAssembly.Utils.Extensions;
using UniRx;
using UnityEngine;

namespace DHToolbox.Runtime.DHToolboxAssembly.WaveSystem
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private SpawnTransform[] spawnTransforms;

        private Subject<Unit> stop = new();
        private bool paused;

        public IObservable<GameObject> StartSpawning(WaveManager.WaveSetup waveSetup)
        {
            var waveDifficultySetup = waveSetup.WaveDifficultySetup;

            var prefabSetups = IEnumerableExtensions.Shuffle(waveDifficultySetup.WavePrefabs).ToList();
            var totalCount = prefabSetups.Sum(setup => setup.Amount);

            var frequency = TimeSpan.FromSeconds(waveDifficultySetup.Duration / totalCount);

            int countFromPrefabSetup = 0;
            return Observable.Interval(frequency)
                .Where(_ => !paused)
                .Take(totalCount)
                .Select(_ =>
                {
                    var newPrefab = Instantiate(prefabSetups[^1].Prefabs.GetRandom(),
                        spawnTransforms.GetRandom().RandomPosition,
                        Quaternion.identity);
                    if (++countFromPrefabSetup >= prefabSetups[^1].Amount)
                    {
                        prefabSetups.Remove(prefabSetups[^1]);
                        countFromPrefabSetup = 0;
                    }

                    return newPrefab;
                })
                .TakeUntil(stop);
        }

        public void Stop() => stop.OnNext(Unit.Default);

        public void Pause() => paused = true;

        public void Unpause() => paused = false;
    }
}