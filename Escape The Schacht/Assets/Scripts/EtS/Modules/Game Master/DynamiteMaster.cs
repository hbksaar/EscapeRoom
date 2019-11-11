using EscapeRoomFramework;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeTheSchacht.GameMaster {

    public class DynamiteMaster : MbModule<IEtsInterface> {

        public new AudioSystem audio;

        private DynamiteGame game;
        private LightingDirector lighting;

        private int[] instructionsVoiceClips = { 132, 133, 134, 111, 112, 113 };

        protected override void Setup() {
            lighting = Room.GetModule<LightingDirector>();
            game = Room.GetGame<DynamiteGame>();
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (sender != game)
                return;

            StopAllCoroutines();
            audio.StopAllVoice();
            audio.StopAllSounds();

            switch (e.NewState) {
            case GameState.Initialized:
                StartCoroutine(GameInitialized(game));
                break;

            case GameState.Running:
                StartCoroutine(GameRunning(game));
                break;

            case GameState.Completed:
                StartCoroutine(GameCompleted(game));
                break;

            case GameState.Aborted:
                StartCoroutine(GameAborted());
                break;

            case GameState.Uninitialized:
            case GameState.Error:
                break;

            default:
                throw new NotImplementedException("GameState not handled: " + e.NewState);
            }
        }

        #region Coroutines
        private IEnumerator GameInitialized(DynamiteGame game) {
            lighting.SwitchToLights(Light.Dynamite);

            audio.PlayVoice(131); // "Die Wand hier..."
            yield return new WaitForSeconds(audio.GetVoiceLength(131));

            int instructionsVoiceClip = instructionsVoiceClips[game.ScenarioNumber - 1];
            audio.PlayVoice(instructionsVoiceClip); // "Benutzt Sprengtafel..."
            yield return new WaitForSeconds(audio.GetVoiceLength(instructionsVoiceClip));

            audio.PlayVoice(136); // "Wenn alles richtig platziert ist..."
            yield return new WaitForSeconds(audio.GetVoiceLength(136));

            game.Start();
        }

        private IEnumerator GameRunning(DynamiteGame game) {
            yield return new WaitForSeconds(220f);
            if (game.State != GameState.Running)
                yield break;

            audio.PlayVoice(2); // "Beeilt euch!"

            // wait for timeout
            WaitForSeconds interval = new WaitForSeconds(.5f);
            while (game.State == GameState.Running && game.ElapsedTime <= RoomMaster.MaxTimeDynamite)
                yield return interval;

            if (game.State == GameState.Running) {
                Debug.Assert(game.ElapsedTime > RoomMaster.MaxTimeDynamite, "Timeout not reached.");
                game.Abort();
            }
        }

        private IEnumerator GameAborted() {
            audio.PlayVoice(160); // "Verdammt!"
            yield return new WaitForSeconds(audio.GetVoiceLength(160));
            yield return new WaitForSeconds(3f);

            Ets.Room.GetGame<TriggersGame>().Initialize(GameDifficulty.Easy);
        }

        private IEnumerator GameCompleted(DynamiteGame game) {
            audio.PlayVoice(142); // "Alles klar."
            yield return new WaitForSeconds(audio.GetVoiceLength(142));

            if (game.ElapsedTime <= 300f)
                Ets.Room.GetGame<TriggersGame>().Initialize(GameDifficulty.Hard);
            else
                Ets.Room.GetGame<TriggersGame>().Initialize(GameDifficulty.Medium);
        }
        #endregion Coroutines

    }

}