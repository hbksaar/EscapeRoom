using System.Collections.Generic;
using UnityEngine;
using System;

namespace EscapeTheSchacht {

    public class AudioSystem : MonoBehaviour {
        
        public static readonly string SoundsPath = "Sounds";
        public static readonly string VoicePath = "Voice";

        private Dictionary<int, AudioSource> sounds = new Dictionary<int, AudioSource>();
        private Dictionary<int, AudioSource> voice = new Dictionary<int, AudioSource>();

        void Awake() {
            // load and register sound files
            Dictionary<int, AudioClip> soundClips = new Dictionary<int, AudioClip>();
            int soundFiles = loadAudioClips(SoundsPath, soundClips);
            Log.Verbose("AudioDirector: Registered " + soundClips.Count + " of " + soundFiles + " loaded sound files.");

            foreach (KeyValuePair<int, AudioClip> clip in soundClips) {
                sounds[clip.Key] = gameObject.AddComponent<AudioSource>();
                sounds[clip.Key].clip = clip.Value;
            }

            // load and register voice files
            Dictionary<int, AudioClip> voiceClips = new Dictionary<int, AudioClip>();
            int voiceFiles = loadAudioClips(VoicePath, voiceClips);
            Log.Verbose("AudioDirector: Registered " + voiceClips.Count + " of " + voiceFiles + " loaded voice files.");

            foreach (KeyValuePair<int, AudioClip> clip in voiceClips) {
                voice[clip.Key] = gameObject.AddComponent<AudioSource>();
                voice[clip.Key].clip = clip.Value;
            }
        }

        private int loadAudioClips(string path, Dictionary<int, AudioClip> storage) {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
            foreach (AudioClip clip in clips) {
                try {
                    string[] name = clip.name.Split('_');
                    int number = int.Parse(name[0]);
                    if (!storage.ContainsKey(number))
                        storage[number] = clip;
                    else
                        Log.Warn("Audio clip with number " + number + " already exists, skipping " + clip.name);
                } catch (Exception e) {
                    Log.Error(e);
                }
            }
            return clips.Length;
        }

        #region Sounds
        /// <summary>
        /// Returns the length in seconds of a sound clip.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        /// <returns></returns>
        public float GetSoundLength(int clipNumber) {
            if (!sounds.ContainsKey(clipNumber))
                throw new ArgumentException("No sound clip with number " + clipNumber);

            return sounds[clipNumber].clip.length;
        }

        /// <summary>
        /// Returns true iff a sound clip is currently playing.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        /// <returns></returns>
        public bool IsSoundPlaying(int clipNumber) {
            if (!sounds.ContainsKey(clipNumber))
                throw new ArgumentException("No sound clip with number " + clipNumber);

            return sounds[clipNumber].isPlaying;
        }

        /// <summary>
        /// Starts playback of a sound clip. The clip is played once and at full volume. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        public void PlaySound(int clipNumber) {
            PlaySound(clipNumber, false, 1f);
        }

        /// <summary>
        /// Starts playback of a sound clip. The clip is played at full volume. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        /// <param name="loop">if true, the clip will loop</param>
        public void PlaySound(int clipNumber, bool loop) {
            PlaySound(clipNumber, loop, 1f);
        }

        /// <summary>
        /// Starts playback of a sound clip. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        /// <param name="loop">if true, the clip will loop</param>
        /// <param name="volume">the volume (0.0 to 1.0)</param>
        public void PlaySound(int clipNumber, bool loop, float volume) {
            if (!sounds.ContainsKey(clipNumber))
                throw new ArgumentException("No sound clip with number " + clipNumber);

            AudioSource player = sounds[clipNumber];
            player.loop = loop;
            player.volume = volume;
            player.Play();
        }

        /// <summary>
        /// Stops playback of a sound clip.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        public void StopSound(int clipNumber, params int[] moreClipNumbers) {
            if (!sounds.ContainsKey(clipNumber))
                throw new ArgumentException("No sound clip with number " + clipNumber);

            sounds[clipNumber].Stop();

            for (int i = 0; i < moreClipNumbers.Length; i++) {
                if (!sounds.ContainsKey(moreClipNumbers[i]))
                    throw new ArgumentException("No sound clip with number " + moreClipNumbers[i]);

                sounds[moreClipNumbers[i]].Stop();
            }
        }

        public void StopAllSounds() {
            foreach (AudioSource player in sounds.Values)
                player.Stop();
        }

        /// <summary>
        /// Adjusts the volume of a sound clip, whether it's currently playing or not.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the sound clip</param>
        /// <param name="volume">the volume (0.0 to 1.0)</param>
        public void ChangeSoundVolume(int clipNumber, float volume) {
            if (!sounds.ContainsKey(clipNumber))
                throw new ArgumentException("No sound clip with number " + clipNumber);

            sounds[clipNumber].volume = volume;
        }
        #endregion Sounds

        #region Voice
        /// <summary>
        /// Returns the length in seconds of a voice clip.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        /// <returns></returns>
        public float GetVoiceLength(int clipNumber) {
            if (!voice.ContainsKey(clipNumber))
                throw new ArgumentException("No voice clip with number " + clipNumber);

            return voice[clipNumber].clip.length;
        }

        /// <summary>
        /// Returns true iff any voice clip is currently playing.
        /// </summary>
        /// <returns></returns>
        public bool IsVoicePlaying() {
            foreach (AudioSource player in voice.Values)
                if (player.isPlaying)
                    return true;

            return false;
        }

        /// <summary>
        /// Returns true iff a voice clip is currently playing.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        /// <returns></returns>
        public bool IsVoicePlaying(int clipNumber) {
            if (!voice.ContainsKey(clipNumber))
                throw new ArgumentException("No voice clip with number " + clipNumber);

            return voice[clipNumber].isPlaying;
        }

        /// <summary>
        /// Starts playback of a voice clip. The clip is played once and at full volume. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        public void PlayVoice(int clipNumber) {
            PlayVoice(clipNumber, false, 1f);
        }

        /// <summary>
        /// Starts playback of a voice clip. The clip is played at full volume. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        /// <param name="loop">if true, the clip will loop</param>
        public void PlayVoice(int clipNumber, bool loop) {
            PlayVoice(clipNumber, loop, 1f);
        }

        /// <summary>
        /// Starts playback of a voice clip. Multiple clips can be played simultaneously.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        /// <param name="loop">if true, the voice will loop</param>
        /// <param name="volume">the volume (0.0 to 1.0)</param>
        public void PlayVoice(int clipNumber, bool loop, float volume) {
            if (!voice.ContainsKey(clipNumber))
                throw new ArgumentException("No voice clip with number " + clipNumber);

            AudioSource player = voice[clipNumber];
            player.loop = loop;
            player.volume = volume;
            player.Play();
        }

        /// <summary>
        /// Stops playback of a voice clip.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        public void StopVoice(int clipNumber, params int[] moreClipNumbers) {
            if (!voice.ContainsKey(clipNumber))
                throw new ArgumentException("No voice clip with number " + clipNumber);

            voice[clipNumber].Stop();

            for (int i = 0; i < moreClipNumbers.Length; i++) {
                if (!voice.ContainsKey(moreClipNumbers[i]))
                    throw new ArgumentException("No voice clip with number " + moreClipNumbers[i]);

                voice[moreClipNumbers[i]].Stop();
            }
        }

        public void StopAllVoice() {
            foreach (AudioSource player in voice.Values)
                player.Stop();
        }

        /// <summary>
        /// Adjusts the volume of a voice clip, whether it's currently playing or not.
        /// </summary>
        /// <param name="clipNumber">the number that identifies the voice clip</param>
        /// <param name="volume">the volume (0.0 to 1.0)</param>
        public void ChangeVoiceVolume(int clipNumber, float volume) {
            if (!voice.ContainsKey(clipNumber))
                throw new ArgumentException("No voice clip with number " + clipNumber);

            voice[clipNumber].volume = volume;
        }
        #endregion Voice

    }

}