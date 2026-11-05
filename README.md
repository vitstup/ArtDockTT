
# README

A multiplayer project featuring a server-authoritative architecture, networked physics, an item interaction system, and basic client-side prediction.

## Running the Project

To run the project, simply open it in Unity Editor or use the provided build.

1. In Unity Editor, open the `Menu` scene and start the project from there.
2. In the menu, select the connection mode: **Host** or **Client**.
3. Enter the host's IP address to establish a connection.

**Important:**
- When running the project in Unity Editor, it must be started from the `Menu` scene. Starting directly from the gameplay scene is not supported.
- Connections outside of localhost have not been tested.
- All available keyboard shortcuts are displayed in the bottom-right corner of the gameplay scene.

## Network Architecture

### Connection Management

The core networking logic is divided between two classes:

- `NetworkSession` — handles network session management, client connections and disconnections, as well as connection approval.
- `NetworkSceneController` — manages scene states, scene transitions, and lobby logic.

This separation keeps the network session lifecycle independent from scene management logic.

### NetworkManager and Transport

Network settings and transport configuration are managed through the `NetworkManager` prefab.

**Limitation:** The current implementation of `NetworkSession` directly depends on the specific transport being used. As a result, switching to a different transport requires code changes and cannot be done through configuration alone.

Ideally, this dependency could be abstracted to support different transport implementations in the future. However, such an abstraction was not implemented within the scope of this project.

### Dependency Injection

Wrapper classes are used to register and expose network-related entities through Dependency Injection (DI).

Examples include:
- `PingService`
- `LocalPlayerProvider`

These wrappers allow other components to access the required networking dependencies through DI.

## Player Networking

### Server Authority

The player controller follows a **server-authoritative** approach.

The server acts as the source of truth for player states and physical interactions.

This approach was chosen to ensure consistent physical interactions between players and objects, while minimizing simulation inconsistencies and related issues.

### Client-Side Prediction

A basic **client-side prediction** system has been implemented.

It allows clients to locally predict the results of player inputs without waiting for every server update, reducing perceived input latency and improving responsiveness.

## Networked Item Physics

Items also follow a server-authoritative model.

- **Server:** Handles the physical simulation of items.
- **Clients:** Items use kinematic physics and receive their updated states from the server.
- **Held items:** Physics is switched to kinematic mode while an item is being held.

This ensures that physical simulation is performed on the server, while clients display the synchronized state.

### Item Interactions

Item interactions, including picking up and placing objects, are validated on the server.

The server verifies whether an action is allowed and handles the corresponding item state changes.

This provides a single source of truth for interactions and prevents clients from independently modifying item states.

## Known Limitations

- Connections outside of localhost have not been tested.
- Starting the project directly from the gameplay scene is not supported.
- The network transport is tightly coupled to `NetworkSession` and cannot be changed through configuration alone.

## Development Time

Approximate development time: **20 hours**.

## Links

- **Demo Video:** https://drive.google.com/file/d/1Gxz3MjlqZ156mML06Llj4ba7wfuPjLGW/view?usp=sharing
- **Build:** https://drive.google.com/file/d/1_RNHQqQ1HSpeZXh7G1KuybK5tyBLUlo3/view?usp=sharing
