using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Pipes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapeTheSchacht.GameMaster {

    public class PipesMaster : MbModule<IEtsInterface> {

        public new AudioSystem audio;
        private PipesGame game;
        private Dictionary<int, bool> solvedSubsystems; // keeps track of subsystems (identified by index from 0 to 3) that have reached a solved state once throughout the playthrough

        protected override void Setup() {
            // retrieve the pipes game from the room and register for fan events
            game = Room.GetGame<PipesGame>();
            game.OnFanTriggered += OnFanTriggered;
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            // this method is registered with the pipes game's OnGameStateChanged event.
            if (sender != game)
                return;

            // stop all coroutines to prevent timing errors and stop sound playback to avoid overlapping audio
            StopAllCoroutines();
            audio.StopAllVoice();
            audio.StopAllSounds();

            switch (e.NewState) {
            case GameState.Initialized:
                // if the game has been initialized, reset what we know about the subsystems from the last playthrough
                solvedSubsystems = new Dictionary<int, bool>();
                foreach (PipeSystem ps in game.PipeSystems())
                    solvedSubsystems[ps.Index] = false;

                StartCoroutine(GameInitialized()); // then explain the rules
                break;

            case GameState.Running:
                // if the game has been started, start the coroutine that keeps track of the player's progress
                StartCoroutine(GameRunning(game));
                break;

            case GameState.Completed:
                // if the game has been completed, handle the players' success
                StartCoroutine(GameCompleted());
                break;

            case GameState.Aborted:
                // if the game has been aborted (due to timeout), handle the player's failure or partial success
                StartCoroutine(GameAborted());
                break;

            case GameState.Uninitialized:
            case GameState.Error:
                break;

            default:
                throw new NotImplementedException("GameState not handled: " + e.NewState);
            }
        }

        private void OnFanTriggered(PipesGame sender, FanTriggeredEventArgs e) {
            // whenever a fan starts or stops, we check if the subsystem it belongs to has been solved by this very state change
            // if this is the case and the subsystem had not reached a solved state previously, some audio feedback (e.g. "well done!") should be played
            if (e.System.RunningFansCount >= e.System.FanCount && !solvedSubsystems[e.System.Index]) {
                solvedSubsystems[e.System.Index] = true;

                // if two subsystem are solved in quick succession, another voice sample might still be playing which we need to stop first
                audio.StopVoice(49, 50, 51);

                // select and play a voice sample depending on how many subsystems have been solved
                switch (e.System.Index) {
                case 0:
                    audio.PlayVoice(49);
                    break;
                case 1:
                    audio.PlayVoice(50);
                    break;
                case 2:
                    audio.PlayVoice(51);
                    break;
                }
            }
        }

        #region Coroutines
        private IEnumerator GameInitialized() { // called when the game has been initialized
            audio.PlayVoice(36); // explanation of game mechanics
            yield return new WaitForSeconds(audio.GetVoiceLength(36)); // wait for the explanation to be finished
            game.Start(); // then start the game
        }

        private IEnumerator GameRunning(PipesGame game) { // called when the game has been started
            // check regularly if the game is still running or the timeout has been reached
            WaitForSeconds interval = new WaitForSeconds(.5f);
            while (game.State == GameState.Running && game.ElapsedTime <= RoomMaster.MaxTimePipes)
                yield return interval;

            // if the game is running, this means the timeout has been reached, so we abort the game
            if (game.State == GameState.Running) {
                Debug.Assert(game.ElapsedTime > RoomMaster.MaxTimePipes, "Timeout not reached.");
                game.Abort();
            }
        }

        private IEnumerator GameAborted() { // called when the game has been aborted, i.e. time is up
            // play a voice sample to inform the players about their failure
            audio.PlayVoice(57); 
            yield return new WaitForSeconds(audio.GetVoiceLength(57));

            // evaluate the players' performance
            int solvedSystems = 0;
            foreach (bool solved in solvedSubsystems.Values)
                if (solved)
                    solvedSystems++;

            // initialize the next game on medium or easy difficulty, depending on the number of solved subsystems
            if (solvedSystems >= 3)
                Ets.Room.GetGame<CratesGame>().Initialize(GameDifficulty.Medium);
            else
                Ets.Room.GetGame<CratesGame>().Initialize(GameDifficulty.Easy);
        }

        private IEnumerator GameCompleted() { // called when the game has been completed, i.e. won
            audio.PlayVoice(70); // inform the players about their success
            yield return new WaitForSeconds(audio.GetVoiceLength(70));

            // initialize the next game on hard difficulty
            Ets.Room.GetGame<CratesGame>().Initialize(GameDifficulty.Hard);
        }
        #endregion Coroutines

    }

}
