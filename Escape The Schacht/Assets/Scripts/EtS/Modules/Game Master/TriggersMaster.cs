using EscapeRoomFramework;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeTheSchacht.GameMaster {

    public class TriggersMaster : MbModule<IEtsInterface> {

        public new AudioSystem audio;

        private TriggersGame game;
        private LightingDirector lighting;

        private bool firstButtonFound, laterButtonWrong;

        protected override void Setup() {
            lighting = Room.GetModule<LightingDirector>();
            game = Room.GetGame<TriggersGame>();
            game.OnButtonPressed += OnButtonPressed;
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (sender != game)
                return;

            StopAllCoroutines();
            audio.StopAllVoice();
            audio.StopAllSounds();

            switch (e.NewState) {
            case GameState.Initialized:
                firstButtonFound = false;
                laterButtonWrong = false;

                StartCoroutine(GameInitialized(game));
                break;

            case GameState.Running:
                StartCoroutine(GameRunning(game));
                break;

            case GameState.Completed:
                if (Room.State.CanTransition(RoomState.Completed)) // room needs to be manually completed
                    Ets.Room.Complete();
                break;

            case GameState.Aborted:
                Ets.Room.Abort(); // room needs to be manually aborted 
                break;

            case GameState.Uninitialized:
            case GameState.Error:
                break;

            default:
                throw new NotImplementedException("GameState not handled: " + e.NewState);
            }
        }

        private void OnButtonPressed(TriggersGame sender, ButtonPressedEventArgs e) {
            if (e.SequenceIndex == 0 && e.Correct && !firstButtonFound) {
                audio.StopAllVoice();
                audio.PlayVoice(165);
                firstButtonFound = true;
            }

            if (e.SequenceIndex > 0 && !e.Correct && !laterButtonWrong) {
                audio.StopAllVoice();
                audio.PlayVoice(168);
                laterButtonWrong = true;
            }
        }

        #region Coroutines
        private IEnumerator GameInitialized(TriggersGame game) {
            audio.PlayVoice(163); // "Jetzt der letzte Schritt."
            yield return new WaitForSeconds(audio.GetVoiceLength(163));

            lighting.SwitchToLights(Light.Trigger);

            audio.PlayVoice(164); // "Seht ihr den Sprengkasten..."
            yield return new WaitForSeconds(audio.GetVoiceLength(164));

            if (game.Difficulty == GameDifficulty.Hard) {
                audio.PlayVoice(166); // "Es kann vorkommen..."
                yield return new WaitForSeconds(audio.GetVoiceLength(166));
            }

            game.Start();
        }

        private IEnumerator GameRunning(TriggersGame game) {
            // wait for timeout
            WaitForSeconds interval = new WaitForSeconds(.5f);
            while (game.State == GameState.Running && game.ElapsedTime <= RoomMaster.MaxTimeTriggers)
                yield return interval;

            if (game.State == GameState.Running) {
                Debug.Assert(game.ElapsedTime > RoomMaster.MaxTimeTriggers, "Timeout not reached.");
                game.Abort();
            }
        }
        #endregion Coroutines

    }

}