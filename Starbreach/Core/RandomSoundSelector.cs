// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Audio;
using Stride.Engine;

namespace Starbreach.Core
{
    public class RandomSoundSelector
    {
        private AudioEmitterComponent emitter;
        private Random random = new Random();

        public AudioEmitterSoundController[] Sounds { get; private set; }

        /// <summary>
        /// Takes all sounds starting with <see cref="baseName"/>
        /// </summary>
        public RandomSoundSelector(AudioEmitterComponent emitter, string baseName)
        {
            this.emitter = emitter;
            Sounds = emitter.Sounds.Where(x => x.Key.StartsWith(baseName)).Select(x => emitter[x.Key]).ToArray();
        }

        /// <summary>
        /// Plays a random sound
        /// </summary>
        public AudioEmitterSoundController PlayAndForget()
        {
            if (Sounds.Length == 0)
                return null;

            int soundToPlay = random.Next(0, Sounds.Length - 1);
            var soundController = Sounds[soundToPlay];
            soundController.PlayAndForget();
            return soundController;
        }

        /// <summary>
        /// Stop all sounds
        /// </summary>
        public void StopAll()
        {
            foreach (var sound in Sounds)
            {
                sound.Stop();
            }
        }
    }
}