# Changelog

## Unreleased

## `0.2.0` - 2019-03-18

### Breaking Changes

- Changed the format of the BuildConfiguration asset. Please recreate, or copy it from `workers/unity/Playground/Assets/Config/BuildConfiguration.asset`.
- Command request and responses are no longer constructed from static methods `CreateRequest` and `CreateResponse`. Instead, they are constructors that take the same arguments.
- The `Require` attribute has moved from the `Improbable.Gdk.GameObjectRepresentation` namespace to the `Improbable.Gdk.Subscriptions` namespace.
- The generated Readers have been renamed from `{COMPONENT_NAME}.Requirable.Reader` to `{COMPONENT_NAME}Reader`.
- The Reader callback events' names have changed.
    - `On{EVENT_NAME}` is now `On{EVENT_NAME}Event`.
    - `{FIELD_NAME}Updated` is now `On{FIELD_NAME}Update`.
- The generated Writers have been renamed from` {COMPONENT_NAME}.Requirable.Writer` to `{COMPONENT_NAME}Writer`.
- The Writer send method names have changed.
    - `Send{EVENT_NAME}` is now `Send{EVENT_NAME}Event`.
    - `Send` is now `SendUpdate`.
 - The generated command senders in MonoBehaviours have also changed.
    - `{COMPONENT_NAME}.Requirable.CommandRequestSender` and `{COMPONENT_NAME}.Requirable.CommandResponseHandler` have been combined and are now called `{COMPONENT_NAME}CommandSender`.
    - `{COMPONENT_NAME}.Requirable.CommandRequestHandler` is now called `{COMPONENT_NAME}CommandReceiver`.
- When creating GameObjects, the `IEntityGameObjectCreator.OnEntityCreated` signature has changed from `GameObject OnEntityCreated(SpatialOSEntity entity)` to `void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)`.
- The signature of `IEntityGameObjectCreator.OnEntityCreated` has changed from `void OnEntityRemoved(EntityId entityId, GameObject linkedGameObject)` to `void OnEntityRemoved(EntityId entityId)`.
    - All linked `GameObject` instances are still unlinked before this is called, however it is now your responsibility to track if a `GameObject` was created when the entity was added.
    - You should now call `linker.LinkGameObjectToSpatialOSEntity()` to link the `GameObject` to the SpatialOS entity.
    - You should also pass-in a list of `ComponentType` to `LinkGameObjectToSpatialOSEntity` which you wish to be copied from the `GameObject` to the ECS entity associated with the `GameObject`.
        - Note that for the Transform Synchronization feature module to work correctly, you need to set up a linked Transform Component on your GameObject. You also need to link any Rigidbody Component on your GameObject.
    - There is no limit on the number of GameObject instances that you can link to a SpatialOS entity. However, you cannot add a component type to a linked GameObject instance more than once.
    - Deleting a linked GameObject unlinks it from the SpatialOS entity automatically.
- `SpatialOSComponent` has been renamed to `LinkedEntityComponent`.
    - The field `SpatialEntityId` on the `LinkedEntityComponent` has been renamed to `EntityId`.
    - The field `Entity` has been removed.
- The `Improbable.Gdk.Core.Dispatcher` class has been removed.

### Added

- All generated schema types, enums, and types which implement `ISpatialComponentSnapshot` are now marked as `Serializable`.
    - Note that generated types that implement `ISpatialComponentData` are not marked as `Serializable`.
- Added the `DynamicConverter` class for converting a `ISpatialComponentSnapshot` to an `ISpatialComponentUpdate`.
- Added a generated ECS shared component called `{COMPONENT_NAME}.ComponentAuthority` for each SpatialOS component.
    - This component contains a single boolean which denotes whether a server-worker instance has write access authority over that component.
    - The component does not tell you about soft-handover (`AuthorityLossImminent`).
- You may now `[Require]` an `EntityId`, `Entity`, `World`, `ILogDispatcher`, and `WorldCommandSender` in MonoBehaviours.
- Added constructors for all generated component snapshot types.
- Added the ability to send arbitrary serialized data in a player creation request.
    - Replaced `Vector3f` position in `CreatePlayerRequestType` with a `bytes` field for sending arbitrary serialized data.
- Added `RequestPlayerCreation` to manually request for player creation in `SendCreatePlayerRequestSystem`.
- Added a menu item, navigate to **SpatialOS** > **Generate Dev Authentication Token**, to generate a TextAsset containing the [Development Authentication Token](https://docs.improbable.io/reference/latest/shared/auth/development-authentication).
- Added the ability to mark a build target as `Required` which will cause builds to fail in the Editor if the prerequisite build support is not installed.

### Changed

- Upgraded the Worker SDK version to `13.6.2`.
- Improved the UX of the BuildConfiguration inspector.
- Improved the UX of the GDK’s Tools Configuration window.
- Deleting a GameObject now automatically unlinks it from its ECS entity. Note that the ECS entity and the SpatialOS entity are _not_ also deleted.
- Changed the format of the BuildConfiguration asset. Please recreate, or copy it from `workers/unity/Playground/Assets/Config/BuildConfiguration.asset`.
- Building workers will not change the active build target anymore. The build target will be set back to whatever was set before starting the build process.

### Fixed

- Fixed a bug where, from the SpatialOS menu in the Unity Editor, running **SpatialOS ** > **Generate code** would always regenerate code, even if no files had changed.
- Fixed a bug where building all workers in our sample projects would fail if you have Android build support installed but didn't set the path to the Android SDK.
 - Fixed a bug where some prefabs would not be processed correctly, causing `NullReferenceExceptions` in `OnEnable`.

### Internal

- Changed the code generator to use the schema bundle JSON rather than AST JSON.
    - If you have forked the code generator, this may be a breaking change.
- Exposed annotations in the code generator model.
- Added a `MockConnectionHandler` implementation for testing code which requires the world to be populated with SpatialOS entities.
- Added tests for `StandardSubscriptionManagers` and `AggregateSubscription`.
 - Re-added tests for Reader/Writer injection criteria and MonoBehaviour enabling.
- Reactive components have been isolated and can be disabled.
- Subscriptions API has been added, this allows you to subscribe anything for which a manager has been defined.
     - This now backs the `Require` API in MonoBehaviours.
- Low-level APIs have been changed significantly.
- Added a View separate from the Unity ECS.
- Removed unnecessary `KcpNetworkParameters` overrides in `MobileWorkerConnector` where it matched the default values.


## `0.1.5` - 2019-02-18

### Changed

- Changed `RedirectedProcess` to have Builder-like API.
- Upgraded the project to be compatible with `2018.3.5f1`.

### Fixed

- Fixed a bug where launching on Android from the Unity Editor would break if you have spaces in your project path.
- Fixed a bug where a Unity package with no dependencies field in its `package.json` would cause code generation to throw exceptions.
- Fixed a bug where protocol logging would crash Linux workers.

## `0.1.4` - 2019-01-28

### Added

- Added support for the Alpha Locator flow.
- Added support for connecting mobile devices to cloud deployments via the anonymous authentication flow.
- Added option to build workers out via IL2CPP in the cmd.
- Added an example of handling disconnect for mobile workers.
- Added support for launching an Android client from the Editor over ADB.

### Changed

- Upgraded the Worker SDK version to `13.5.1`. This is a stable Worker SDK release! :tada:
- `Improbable.Gdk.EntityTemplate` is now mutable and exposes a set of APIs to add, remove, and replace component snapshots
    - This replaces the `Improbable.Gdk.Core.EntityBuilder` class.
    - These changes also allow you to reuse an `EntityTemplate` more than once.
- Upgraded the project to be compatible with `2018.3.2f1`.
- Upgraded the entities package to `0.0.12-preview.21`
- Disabled protocol logging on Linux workers to prevent crashes. This will be reverted once the underlying issue is fixed.
- Updated the `MobileWorkerConnector` to use the KCP network protocol by default.
- Changed the `mobile_launch.json` config to use the new Runtime.
- Updated all the launch configs to use the new Runtime.
- Changed the build process in the Editor such that it skips builds that don't have build support rather than canceling the entire build process.
    - Note that building via the `Improbable.Gdk.BuildSystem.WorkerBuilder.Build` static method is unchanged.

### Fixed

- `Clean all workers` now cleans worker configs in addition to built-out workers.
- Fixed a bug where you could start each built-out worker only once on OSX.
- Code generation now captures nested package dependencies, so the generated schema contains schema components from all required packages. Previously, code generation only generated schema for top-level dependencies, skipping nested packages.
- Fixed a bug where spaces in the path would cause code generation to fail on OSX.
- Fixed an issue in the TransformSynchronization module where an integer underflow would cause a memory crash.
- Fixed a bug where using `Coordinates`, `Vector3f`, or `Vector3d` in a command definition would cause the Code Generator to crash.

### Removed

- Removed the `Improbable.Gdk.Core.EntityBuilder` class as it was superceded by the updated functionality in `Improbable.Gdk.Core.EntityTemplate`.
    - Removed `CreateSchemaComponentData` from each generated component as it is no longer required by the `EntityBuilder`.
- Removed `com.unity.incrementalcompiler` package as a dependency of the `Core` package.

## `0.1.3` - 2018-11-26

### Added

- Added Frames Per Second (FPS) and Unity heap usage as metrics sent by `MetricSendSystem.cs`.
- Added a warning message to the top of schema files copied into the `from_gdk_packages` directory.
- Added an `ISnapshottable<T>` interface to all generated components. This allows you to convert a component to a snapshot.
- Added an `EntityId` property on the Readers/Writers to access the `EntityId` of the underlying SpatialOS entity.
- Added a `HasEntity` method to the `WorkerSystem`. This allows you to check if an entity is checked out on your worker.
- Added operators and conversion methods to `Coordinates`, `Vector3d`, and `Vector3f` in code generation.
    - This supercedes the `StandardLibraryUtils` feature module which was removed as a consequence.

### Changed

- Improved the method of calculating load and FPS.
- Updated test project Unity version to `2018.2.14f`.
- Upgraded the Worker SDK snapshot version. This entails the following changes:
    - `EntityId` is now in the `Improbable.Gdk.Core` namespace. (Previously `Improbable.Worker`).
    - `Dispatcher` is now in the `Improbable.Gdk.Core` namespace. (Previously `Improbable.Worker`).
    - The `Improbable.Worker.Core` namespace is now `Improbable.Worker.CInterop`.

### Fixed

- Fixed a bug where schema components with a field named `value` would generate invalid code.

### Removed

- Removed the `StandardLibraryUtils` feature module as it was superceded by inserting the methods during code generation.

## `0.1.2` - 2018-11-01

### Added

- Added the ability to acknowledge `AuthorityLossImminent` messages.
- Added an `Open Inspector` button to the `SpatialOS` menu in the Unity Editor.
- Added support for local mobile development.
- Added a changelog.
- Added field level dirty markers in components. This allows for partial automatic component updates to be sent.
- Added full support for `EntityQuery` world commands.
    - Added `Improbable.Gdk.Core.EntityQuerySnapshot` to hold the result of a single entity from a snapshot query.
    - Added `Improbable.Gdk.Core.ISpatialComponentSnapshot` to differentiate between a snapshot of component state and component data.

### Changed

- Changed the allocation type used internally for Unity ECS chunk iteration from `Temp` to `TempJob`
- Running a build in the Editor no longer automatically selects all scenes in the Unity build configuration
- `Improbable.Gdk.Core.Snapshot.AddEntity` now returns the `EntityId` assigned in the snapshot.
- Changed the `WorkerConnector` to be more generic and have an explicit `StandaloneWorkerConnector` for any workers running on OSX/Linux/Windows.
- Updated the default Unity version to `2018.2.14f1`.

### Fixed

- Fixed a bug where deserialising multiple events in a single component update only returned N copies of the last event received, where N is the number of events in the update.
- Fixed a broken link to the setup guide in an error message.

## `0.1.1` - 2018-10-19

### Added

- Better error messages when missing build support for a target platform.
- Better error messages for common problems when downloading the Worker SDK.

### Changed

- Position updates are now sent after all other updates.
- Simplified the heartbeating system in the `PlayerLifecycle` feature module.
- Updated the `README` and "Get Started" guide.

### Fixed

- The `GameLogic` worker is run in headless mode.
- The `Clean All Workers` menu item now works.

## `0.1.0` - 2018-10-10

The initial alpha release of the SpatialOS GDK for Unity.
