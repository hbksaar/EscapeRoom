using EscapeRoomFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapeTheSchacht {

    /// <summary>
    /// <para>
    /// An abstract base implementation of the <see cref="IModule{TPhysicalInterface}"/> that is derived from <see cref="MonoBehaviour"/>. 
    /// The implementation is identical to <see cref="Module{TPhysicalInterface}"/> (see also there). The main motivation for this type is 
    /// making Unity's coroutines accessible for the game master and other classes that deem this functionality useful.
    /// </para>
    /// 
    /// <para>
    /// Using the Update method called by the Unity engine in child classes is discouraged as the script execution order is not guaranteed.
    /// To avoid confusion and unnecessary hassle, use <see cref="IModule{TPhysicalInterface}.Update(float)"/> which usually is called through 
    /// another one of Unity's update methods that drives the <see cref="EscapeRoom{TPhysicalInterface}"/>, anyway).
    /// </para>
    /// </summary>
    /// <typeparam name="TPhysicalInterface"></typeparam>
    public abstract class MbModule<TPhysicalInterface> : MonoBehaviour, IModule<TPhysicalInterface> where TPhysicalInterface : IPhysicalInterface {

        /// <inheritdoc cref="Module{TPhysicalInterface}.Room"/>
        protected internal EscapeRoom<TPhysicalInterface> Room { get; private set; }

        /// <inheritdoc cref="Module{TPhysicalInterface}.Physical"/>
        protected internal TPhysicalInterface Physical { get; private set; }

        /// <inheritdoc/>
        public void Setup(EscapeRoom<TPhysicalInterface> room, TPhysicalInterface physical) {
            Room = room;
            Physical = physical;
            Setup();
        }

        /// <inheritdoc cref="Module{TPhysicalInterface}.Setup"/>
        protected virtual void Setup() { }

        /// <inheritdoc/>
        public virtual void OnRoomStateChanged(EscapeRoom<TPhysicalInterface> sender, RoomStateChangedEventArgs e) { }

        /// <inheritdoc/>
        public virtual void OnGameStateChanged(Game<TPhysicalInterface> sender, GameStateChangedEventArgs e) { }

        /// <inheritdoc/>
        void IModule<TPhysicalInterface>.Update(float deltaTime) {
            UbmUpdate(deltaTime);
        }

        /// <inheritdoc cref="IModule{TPhysicalInterface}.Update(float)"/>
        public virtual void UbmUpdate(float deltaTime) { }

        public virtual void OnDiagnosticsReportCreated(TPhysicalInterface sender, DiagnosticsReportCreatedEventArgs e) { }
        
    }

}
