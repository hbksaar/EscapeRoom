namespace EscapeRoomFramework {

    /// <summary>
    /// <para>
    /// This is the base class for the physical computing interface that is responsible for connecting the <see cref="EscapeRoom{TPhysicalInterface}"/> 
    /// control program and the <see cref="Game{TPhysicalInterface}"/> implementations with the physical world, e.g. any Arduino devices that 
    /// are connected to the computer and provide sensor data or means to manipulate actuators. 
    /// </para>
    /// 
    /// <para>
    /// The physical interface is opened in the initialization of the <see cref="EscapeRoom{TPhysicalInterface}"/> and closed after a run is 
    /// terminated. Between escape room runs, it is reset by closing and reopening it. Any implementation should be aware
    /// of this and handle any resources accordingly in the respective methods <see cref="IPhysicalInterface.Open"/> and <see cref="IPhysicalInterface.Close"/>.
    /// </para>
    /// 
    /// <para>
    /// Every time after the physical interface has been opened, <see cref="IPhysicalInterface.RunDiagnostics(DiagnosticsReport)"/> is immediately 
    /// called which is intended to report any technical failures that can be determined. If this functionality 
    /// is not supported by the escape room, the implementation of the method can be left empty.
    /// </para>
    /// </summary>
    public interface IPhysicalInterface {

        /// <summary>
        /// Returns <code>true</code> iff the interface has been opened recently by calling <see cref="IPhysicalInterface.Open"/>.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Opens all necessary connections to physical devices. This is the place where any resources should be 
        /// allocated, external processes be started and serial and network connections be opened.
        /// </summary>
        void Open();

        /// <summary>
        /// Runs a diagnostics check and fills the given <see cref="DiagnosticsReport"/> with any useful information on 
        /// failed devices that can be provided.
        /// </summary>
        /// <param name="result">the resulting diagnostics report</param>
        void RunDiagnostics(DiagnosticsReport result);

        /// <summary>
        /// This is the place where the connected devices should be queried for any sensor values to ensure the currently 
        /// running games receive up-to-date information. This <see cref="IPhysicalInterface.PhysicalUpdate(float)"/> 
        /// method is guaranteed to be called before any respective methods in <see cref="Game{TPhysicalInterface}"/> or 
        /// <see cref="IModule{TPhysicalInterface}"/> implementations.
        /// </summary>
        void PhysicalUpdate(float deltaTime);

        /// <summary>
        /// Closes all previously opened connections to physical devices. This is the place where any resources 
        /// should be freed, external processes be terminated and serial and network connections be closed.
        /// </summary>
        void Close();

    }

}