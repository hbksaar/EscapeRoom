using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using System.Collections;
using UnityEngine;
using System;

namespace EscapeTheSchacht.GameMaster {

    public class CratesMaster : MbModule<IEtsInterface> {

        public new AudioSystem audio;

        private CratesGame game;
        private LightingDirector lighting;

        private bool firstCratePickedUp, firstCrateInDropzone;

        protected override void Setup() {
            lighting = Room.GetModule<LightingDirector>();
            game = Room.GetGame<CratesGame>();
            game.OnCratePickedUp += OnCratePickedUp;
            game.OnCrateDropped += OnCrateDropped;
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (sender != game)
                return;

            StopAllCoroutines();
            audio.StopAllVoice();
            audio.StopAllSounds();

            switch (e.NewState) {
            case GameState.Initialized:
                lighting.SwitchAllLights(LightSetting.Off);
                firstCratePickedUp = false;
                firstCrateInDropzone = false;

                StartCoroutine(GameInitialized());
                break;

            case GameState.Running:
                StartCoroutine(GameRunning(game));
                break;

            case GameState.Completed:
                StartCoroutine(GameCompleted());
                break;

            case GameState.Aborted:
                StartCoroutine(GameAborted(game));
                break;
            
            case GameState.Uninitialized:
            case GameState.Error:
                break;

            default:
                throw new NotImplementedException("GameState not handled: " + e.NewState);
            }
        }

        private void OnCratePickedUp(CratesGame sender, CrateEventArgs e) {
            if (!firstCratePickedUp) {
                audio.PlayVoice(86);
                firstCratePickedUp = true;
            }
        }

        private void OnCrateDropped(CratesGame sender, CrateEventArgs e) {
            if (!firstCrateInDropzone && e.InDropzone) {
                audio.PlayVoice(88);
                firstCrateInDropzone = true;
            }
        }

        #region Coroutines
        private IEnumerator GameInitialized() {
            audio.PlayVoice(84); // introduction
            yield return new WaitForSeconds(audio.GetVoiceLength(84));

            lighting.SwitchToLights(Light.CraneLeft, Light.CraneRight);

            audio.PlayVoice(85); // explanation of game mechanics
            yield return new WaitForSeconds(audio.GetVoiceLength(85));

            game.Start();
        }

        private IEnumerator GameRunning(CratesGame game) {
            yield return new WaitForSeconds(60f);

            if (game.State != GameState.Running)
                yield break;

            if (game.CratesInDropzone == 0 && !firstCratePickedUp)
                audio.PlayVoice(104); // "Noch keine Kiste aufgehoben?"

            yield return new WaitForSeconds(60f);

            if (game.CratesInDropzone == 0)
                audio.PlayVoice(110);

            // wait for timeout
            WaitForSeconds interval = new WaitForSeconds(.5f);
            while (game.State == GameState.Running && game.ElapsedTime <= RoomMaster.MaxTimeCrates)
                yield return interval;

            if (game.State == GameState.Running) {
                Debug.Assert(game.ElapsedTime > RoomMaster.MaxTimeCrates, "Timeout not reached.");
                game.Abort();
            }
        }

        private IEnumerator GameAborted(CratesGame game) {
            audio.PlayVoice(122); // "Das klappt nicht."
            yield return new WaitForSeconds(audio.GetVoiceLength(122));

            if (game.CratesInDropzone == 0)
                Room.GetGame<DynamiteGame>().Initialize(GameDifficulty.Easy);
            else
                Room.GetGame<DynamiteGame>().Initialize(GameDifficulty.Medium);
        }

        private IEnumerator GameCompleted() {
            audio.PlayVoice(121); // "Oki, so geht das."
            yield return new WaitForSeconds(audio.GetVoiceLength(121));

            Room.GetGame<DynamiteGame>().Initialize(GameDifficulty.Hard);
        }
        #endregion Coroutines

    }

}