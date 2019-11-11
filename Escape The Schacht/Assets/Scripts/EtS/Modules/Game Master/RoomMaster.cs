using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeTheSchacht.GameMaster {

    public class RoomMaster : MbModule<IEtsInterface> {

        public static readonly float MaxTimePipes = 5 * 60;
        public static readonly float MaxTimeCrates = 6 * 60;
        public static readonly float MaxTimeDynamite = 7 * 60;
        public static readonly float MaxTimeTriggers = 5 * 60;

        private static readonly float DelayUntilRoomReset = 30f;

        public new AudioSystem audio;

        private LightingDirector lighting;

        protected override void Setup() {
            lighting = Room.GetModule<LightingDirector>();
        }

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            switch (e.NewState) {
            case RoomState.Uninitialized:
                break;

            case RoomState.Initialized:
                break;

            case RoomState.Running:
                StopAllCoroutines();
                audio.StopAllVoice();
                audio.StopAllSounds();
                StartCoroutine(StartRoom());
                break;

            case RoomState.Completed:
                StopAllCoroutines();
                audio.StopAllVoice();
                audio.StopAllSounds();
                StartCoroutine(CompleteRoomEffects());
                StartCoroutine(CompleteRoomVoiceover());
                LogTimes();
                break;

            case RoomState.Aborted:
                StopAllCoroutines();
                audio.StopAllVoice();
                audio.StopAllSounds();
                StartCoroutine(AbortRoom());
                LogTimes();
                break;

            case RoomState.Error:
                StopAllCoroutines();
                break;

            default:
                throw new NotImplementedException("RoomState not handled: " + e.NewState);
            }
        }

        private void LogTimes() {
            float totalGamesTime = 0;
            Log.Info("Times for room in state {0}:", Room.State);
            foreach (Game<IEtsInterface> game in Room.GetGames()) {
                Log.Info("{0} in state {1} after {2} s", game.Id, game.State, game.ElapsedTime);
                totalGamesTime += game.ElapsedTime;
            }
            Log.Info("Total time in games: {0} s", totalGamesTime);
            Log.Info("Total room time: {0} s", Room.ElapsedTime);
        }

        #region Coroutines
        private IEnumerator StartRoom() {
            audio.PlaySound(1); // collapse
            yield return new WaitForSeconds(audio.GetSoundLength(1));

            audio.PlayVoice(35); // "Hallo?"
            yield return new WaitForSeconds(audio.GetVoiceLength(35));

            // initialize pipes
            lighting.SwitchToLights(Light.PipesLeft, Light.PipesRight);
            Room.GetGame<PipesGame>().Initialize(GameDifficulty.Medium);
        }

        private IEnumerator CompleteRoomEffects() {
            audio.PlaySound(43); // alarm 1
            audio.ChangeSoundVolume(43, .5f);
            yield return new WaitForSeconds(audio.GetSoundLength(43) + 3f); // ~3.8s + 2s = ~5.8s

            audio.PlaySound(44); // alarm 2
            audio.ChangeSoundVolume(44, .5f);
            yield return new WaitForSeconds(audio.GetSoundLength(44) + 1f); // + ~3.25s + 1s = ~10s

            audio.PlaySound(45); // explosion
            yield return new WaitForSeconds(audio.GetSoundLength(45) + 3f); // + ~8.5s + 3s = ~20.5s (start voice at 3.5s)

            lighting.SwitchAllLights(LightSetting.On);

            // reset room after some time
            yield return new WaitForSeconds(DelayUntilRoomReset);
            Room.Initialize();
        }

        private IEnumerator CompleteRoomVoiceover() {
            // start voice delayed
            yield return new WaitForSeconds(1.5f);

            audio.PlayVoice(170); // "Ihr habt es geschafft!"
            yield return new WaitForSeconds(audio.GetVoiceLength(170) + 7f); // + ~8s + 7s

            audio.PlayVoice(190); // "Ab in die Rettungskapsel!"
        }

        private IEnumerator AbortRoom() {
            audio.PlayVoice(177); // "Ihr schafft es nicht..."
            yield return new WaitForSeconds(audio.GetVoiceLength(177));

            audio.PlaySound(99); // collapse
            yield return new WaitForSeconds(audio.GetSoundLength(99));

            // turn lights on after a short while
            yield return new WaitForSeconds(5f);
            lighting.SwitchAllLights(LightSetting.On);

            // reset room after some time
            yield return new WaitForSeconds(DelayUntilRoomReset - 5f);
            Room.Initialize();
        }
        #endregion Coroutines

    }

}