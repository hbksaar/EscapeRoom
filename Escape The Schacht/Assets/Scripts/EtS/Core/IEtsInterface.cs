using EscapeRoomFramework;
using System;
using UnityEngine;

namespace EscapeTheSchacht {

    public enum Light {
        PipesLeft = 1,
        PipesRight = 3,
        CraneLeft = 2,
        CraneRight = 0,
        Trigger = 4,
        Dynamite = 5,
    }

    public enum LightSetting {
        None = 0,
        On = 1,
        Off = -1
    }

    public interface IEtsInterface : IPhysicalInterface {

        #region Room
		bool OverrideLighting { get; set; }

        bool GetLightState(Light light);

        void SetLightState(Light light, bool on);

        bool[] GetLightStates();

        void SetLightStates(bool[] states);

        void ChangeLightStates(Light[] lights, LightSetting[] settings);

        /// <summary>
        /// Returns true iff the control button is currently pressed.
        /// </summary>
        bool IsControlButtonDown();

        /// <summary>
        /// Returns true iff the control button has been pressed since the last frame.
        /// </summary>
        bool WasControlButtonPressed();
        #endregion Room

        #region Pipes
        /// <summary>
        /// Forces an immediate update of the physical state of the devices relevant for the pipes game to ensure 
        /// the respective getters return up-to-date information.
        /// </summary>
        void ForcePipesUpdate();

        /// <summary>
        /// Returns the state of the valve at the given position. The state is either true or false but the 
        /// values don't correspond to "open" or "closed". Interpretation is left for a logical 
        /// implementation.
        /// </summary>
        /// <param name="row">the row of the valve</param>
        /// <param name="position">the position of the valve in the row (e.g. to access the second valve in the 
        /// row)</param>
        bool GetValveState(int row, int position);

        /// <summary>
        /// Returns true iff the valve at the given position is broken. This means a hardware failure is detected,
        /// e.g. the Arduino is not responding.
        /// </summary>
        /// <param name="row">the row of the valve</param>
        /// <param name="position">the position of the valve in the row (e.g. to access the second valve in the
        /// row)</param>
        bool IsValveBroken(int row, int position);

        /// <summary>
        /// Returns true iff the valve at the given position is being turned by someone.
        /// </summary>
        /// <param name="row">the row of the valve</param>
        /// <param name="position">the position of the valve in the row (e.g. to access the second valve in the 
        /// row)</param>
        bool IsValveRotating(int row, int position);

        /// <summary>
        /// Returns true iff any valve is being turned by someone.
        /// </summary>
        bool IsAnyValveRotating();

        /// <summary>
        /// Returns true iff the fan at the given position is turned on.
        /// </summary>
        /// <param name="row">the row of the fan</param>
        /// <param name="position">the position of the fan in the row (e.g. to access the second fan in the 
        /// row)</param>
        bool GetFanState(int row, int position);

        /// <summary>
        /// Enables or disables the fan at the given position.
        /// </summary>
        /// <param name="row">the row of the fan</param>
        /// <param name="position">the position of the fan in the row (e.g. to access the second fan in the 
        /// row)</param>
        /// <param name="active">true to switch the fan on, false to switch it off</param>
        void SetFanState(int row, int position, bool active);
        #endregion Pipes

        #region Crates
        /// <summary>
        /// Forces an immediate update of the physical state of the devices relevant for the crane game to ensure 
        /// the respective getters return up-to-date information.
        /// </summary>
        void ForceCraneUpdate();

        /// <summary>
        /// Returns true iff the crane button with the given index is currently pressed.
        /// </summary>
        bool IsCraneButtonDown(int buttonIndex);

        /// <summary>
        /// Returns true iff the crane button with the given index has been pressed since the last frame.
        /// </summary>
        bool WasCraneButtonPressed(int buttonIndex);
        #endregion Crates

        #region Dynamite
        /// <summary>
        /// Forces an immediate update of the physical state of the devices relevant for the dynamite game to ensure 
        /// the respective getters return up-to-date information.
        /// </summary>
        void ForceDynamiteUpdate();

        /// <summary>
        /// Returns the state of the hole with the given index. If the hole is empty, -1 is returned.
        /// Otherwise the return value is the id of the stick inside the hole.
        /// </summary>
        int GetHoleState(int holeIndex);
        #endregion Dyanmite

        #region Trigger
        /// <summary>
        /// Forces an immediate update of the physical state of the devices relevant for the trigger game to ensure 
        /// the respective getters return up-to-date information.
        /// </summary>
        void ForceTriggerUpdate();

        /// <summary>
        /// Returns true iff the sequence button with the given index has been pressed since the last frame.
        /// </summary>
        bool WasSequenceButtonPressed(int buttonIndex);

        /// <summary>
        /// Set the color of the LED with the given index to the specified value. <see cref="Color.black"/> 
        /// turns the LED off.
        /// </summary>
        void SetLEDColor(int ledIndex, Color color);

        /// <summary>
        /// Set the colors of all LEDs to the specified values. Each array index is mapped to the corresponding 
        /// LED index. <see cref="Color.black"/> turns the LED off.
        /// </summary>
        void SetLEDColors(Color[] colors);
        #endregion Trigger

    }

}