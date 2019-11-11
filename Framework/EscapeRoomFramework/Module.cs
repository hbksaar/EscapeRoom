namespace EscapeRoomFramework {

    /// <summary>
    /// This is an abstract base implementation of the <see cref="IModule{TPhysicalInterface}"/> interface that 
    /// binds the arguments of <see cref="Setup(EscapeRoom{TPhysicalInterface}, TPhysicalInterface)"/> 
    /// to protected properties for use in child types, and provides empty virtual implementations of 
    /// the interface methods, making overrides optional.
    /// </summary>
    public abstract class Module<TPhysicalInterface> : IModule<TPhysicalInterface> where TPhysicalInterface : IPhysicalInterface {
        
        /// <summary>
        /// The escape room this module is registered with.
        /// </summary>
        protected internal EscapeRoom<TPhysicalInterface> Room { get; private set; }

        /// <summary>
        /// The physical interface of the room this module is registered with.
        /// </summary>
        protected internal TPhysicalInterface Physical { get; private set; }

        /// <inheritdoc />
        public void Setup(EscapeRoom<TPhysicalInterface> room, TPhysicalInterface physical) {
            Room = room;
            Physical = physical;
            Setup();
        }

        /// <summary>
        /// This method is called when the module is registered with the <see cref="EscapeRoom{TPhysicalInterface}"/> 
        /// to allow initialization work to be done. Use the properties <see cref="Room"/> and <see cref="Physical"/> as needed.
        /// </summary>
        protected virtual void Setup() { }

        /// <inheritdoc/>
        public virtual void OnRoomStateChanged(EscapeRoom<TPhysicalInterface> sender, RoomStateChangedEventArgs e) { }

        /// <inheritdoc/>
        public virtual void OnGameStateChanged(Game<TPhysicalInterface> sender, GameStateChangedEventArgs e) { }

        /// <inheritdoc/>
        public virtual void OnDiagnosticsReportCreated(TPhysicalInterface sender, DiagnosticsReportCreatedEventArgs e) { }

        /// <inheritdoc/>
        public virtual void Update(float deltaTime) { }

    }

}