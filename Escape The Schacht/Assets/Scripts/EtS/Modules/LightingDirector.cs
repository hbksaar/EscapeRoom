using EscapeRoomFramework;
using EscapeTheSchacht.Crates;
using EscapeTheSchacht.Dynamite;
using EscapeTheSchacht.GameMaster;
using EscapeTheSchacht.Pipes;
using EscapeTheSchacht.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapeTheSchacht {

    public class LightingDirector : MbModule<IEtsInterface> {

        private Color LedColorNeutral = Color.white;
        private Color LedColorCorrect = Color.green;
        private Color LedColorWrong = Color.red;
        private Color LedColorOff = Color.black;

        public Ets unityEtsAdapter;

        private bool gameMasterActive;

        public void SwitchAllLights(LightSetting setting) {
            Light[] allLights = (Light[]) Enum.GetValues(typeof(Light));
            SwitchLights(setting, allLights);
        }

        public void SwitchToLights(params Light[] lights) {
            Light[] allLights = (Light[]) Enum.GetValues(typeof(Light));
            LightSetting[] settings = new LightSetting[EtsInterface.LightsCount];
            for (int i = 0; i < settings.Length; i++)
                settings[i] = LightSetting.Off;
            for (int i = 0; i < lights.Length; i++)
                settings[(int) lights[i]] = LightSetting.On;
            Physical.ChangeLightStates(allLights, settings);
        }

        public void SwitchLights(LightSetting setting, params Light[] lights) {
            LightSetting[] settings = new LightSetting[lights.Length];
            for (int i = 0; i < settings.Length; i++)
                settings[i] = setting;
            Physical.ChangeLightStates(lights, settings);
        }

        protected override void Setup() {
            gameMasterActive = unityEtsAdapter.enableGameMasters;
            Room.GetGame<TriggersGame>().OnButtonPressed += OnTriggerButtonPressed;
        }

        public override void OnRoomStateChanged(EscapeRoom<IEtsInterface> sender, RoomStateChangedEventArgs e) {
            if (e.NewState == RoomState.Initialized) {
                SwitchAllLights(LightSetting.Off);

                foreach (Fan fan in Room.GetGame<PipesGame>().Fans())
                    Physical.SetFanState(fan.Row, fan.PositionInRow, false);

                Color[] ledsOff = new Color[TriggersGame.LedCount];
                Physical.SetLEDColors(ledsOff);
            }
        }

        public override void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (sender is PipesGame && e.NewState == GameState.Initialized)
                if (!gameMasterActive)
                    SwitchToLights(Light.PipesLeft, Light.PipesRight);

            if (sender is CratesGame && e.NewState == GameState.Initialized)
                if (!gameMasterActive)
                    SwitchToLights(Light.CraneLeft, Light.CraneRight);

            if (sender is DynamiteGame && e.NewState == GameState.Initialized)
                if (!gameMasterActive)
                    SwitchToLights(Light.Dynamite);

            if (sender is DynamiteGame && e.NewState == GameState.Completed) {
                // activate trigger LEDs to visually signal completion
                Color[] colors = new Color[TriggersGame.LedCount];
                for (int i = 0; i < TriggersGame.LedCount; i++)
                    colors[i] = LedColorNeutral;
                Physical.SetLEDColors(colors);
            }

            if (sender is TriggersGame && e.NewState == GameState.Initialized) {
                if (!gameMasterActive)
                    SwitchToLights(Light.Trigger);

                if (e.NewState == GameState.Initialized) {
                    // activate LEDs
                    Color[] colors = new Color[TriggersGame.LedCount];
                    for (int i = 0; i < TriggersGame.LedCount; i++)
                        colors[i] = LedColorNeutral;

                    Physical.SetLEDColors(colors);
                }
            }
        }

        private void OnTriggerButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!e.Correct) {
                Color[] colors = new Color[TriggersGame.LedCount];
                for (int i = 0; i < TriggersGame.LedCount; i++)
                    colors[i] = LedColorWrong;

                Room.Physical.SetLEDColors(colors);
            }

            if (e.Correct) {
                Color[] colors = new Color[TriggersGame.LedCount];
                for (int i = 0; i <= e.SequenceIndex; i++)
                    colors[i] = LedColorCorrect;
                for (int i = e.SequenceIndex + 1; i < TriggersGame.LedCount; i++)
                    colors[i] = LedColorOff;

                Room.Physical.SetLEDColors(colors);
            }
        }

    }

}
