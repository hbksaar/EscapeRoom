namespace EscapeRoomFramework {

    /// <summary>
    /// <para>
    /// This interface defines the basic structure of a module. Modules need to be registered with the <see cref="EscapeRoom{TPhysicalInterface}"/> 
    /// after all games have been registered. 
    /// </para>
    /// 
    /// <para>
    /// Their <see cref="IModule{TPhysicalInterface}.OnRoomStateChanged(EscapeRoom{TPhysicalInterface}, RoomStateChangedEventArgs)"/>, 
    /// <see cref="IModule{TPhysicalInterface}.OnGameStateChanged(Game{TPhysicalInterface}, GameStateChangedEventArgs)"/> and 
    /// <see cref="IModule{TPhysicalInterface}.OnDiagnosticsReportCreated(TPhysicalInterface, DiagnosticsReportCreatedEventArgs)"/> are 
    /// automatically subscribed to the respective events of the room and all games. Also bindings to the room and the physical computing 
    /// interface are provided.
    /// </para>
    /// 
    /// <para>
    /// Similar to games modules have <see cref="IModule{TPhysicalInterface}.Update(float)"/> methods which are called after the games' update 
    /// methods. This is done in the same order as the modules have been registered with the room.
    /// </para>
    /// </summary>
    public interface IModule<TPhysicalInterface> where TPhysicalInterface : IPhysicalInterface {

        /// <summary>
        /// This method is called when the module is registered with the <see cref="EscapeRoom{TPhysicalInterface}"/> 
        /// to allow initialization work to be done using (and storing) the provided room and physical computing interface.
        /// </summary>
        /// <param name="room">the room this module is registered with</param>
        /// <param name="physical">the physical interface of the room</param>
        void Setup(EscapeRoom<TPhysicalInterface> room, TPhysicalInterface physical);

        /// <inheritdoc cref="EscapeRoom{TPhysicalInterface}.OnRoomStateChanged"/>
        void OnRoomStateChanged(EscapeRoom<TPhysicalInterface> sender, RoomStateChangedEventArgs e);

        /// <inheritdoc cref="Game{TPhysicalInterface}.OnGameStateChanged"/>
        void OnGameStateChanged(Game<TPhysicalInterface> sender, GameStateChangedEventArgs e);

        /// <inheritdoc cref="EscapeRoom{TPhysicalInterface}.OnDiagnosticsReportCreated"/>
        void OnDiagnosticsReportCreated(TPhysicalInterface sender, DiagnosticsReportCreatedEventArgs e);
        
        /// <summary>
        /// Called in every frame after all games have been updated and all game-related events have been raised. This 
        /// is the place to do work that is either independent from game events or dependent on multiple game events 
        /// and thus not doable in reaction to a single event.
        /// </summary>
        /// <param name="deltaTime">the time that passed since the last update cycle</param>
        void Update(float deltaTime);

    }

}