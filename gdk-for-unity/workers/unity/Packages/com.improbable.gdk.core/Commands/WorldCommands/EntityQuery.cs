using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Improbable.Gdk.Core.Commands
{
    public static partial class WorldCommands
    {
        public static class EntityQuery
        {
            /// <summary>
            ///     An object that is a EntityQuery command request.
            /// </summary>
            public struct Request : ICommandRequest
            {
                public Improbable.Worker.CInterop.Query.EntityQuery EntityQuery;
                public uint? TimeoutMillis;
                public object Context;

                /// <summary>
                ///     Method to create an EntityQuery command request payload.
                /// </summary>
                /// <param name="entityQuery">The EntityQuery object defining the constraints and query type.</param>
                /// <param name="timeoutMillis">
                ///     (Optional) The command timeout in milliseconds. If not specified, will default to 5 seconds.
                /// </param>
                /// <param name="context">
                ///    (Optional) A context object that will be returned with the command response.
                /// </param>
                /// <returns></returns>
                public Request(Improbable.Worker.CInterop.Query.EntityQuery entityQuery,
                    uint? timeoutMillis = null, object context = null)
                {
                    EntityQuery = entityQuery;
                    TimeoutMillis = timeoutMillis;
                    Context = context;
                }
            }

            /// <summary>
            ///     An object that is the response of an EntityQuery command from the SpatialOS runtime.
            /// </summary>
            public readonly struct ReceivedResponse : IReceivedCommandResponse
            {
                public readonly Entity SendingEntity;

                /// <summary>
                ///     The status code of the command response. If equal to <see cref="StatusCode"/>.Success then
                ///     the command succeeded.
                /// </summary>
                public readonly StatusCode StatusCode;

                /// <summary>
                ///     The failure message of the command. Will only be non-null if the command failed.
                /// </summary>
                public readonly string Message;

                /// <summary>
                ///     A dictionary that represents the results of a <see cref="SnapshotResultType"/> entity query.
                ///     This is null for <see cref="CountResultType"/> entity queries.
                /// </summary>
                public readonly Dictionary<EntityId, EntityQuerySnapshot> Result;

                /// <summary>
                ///     The number of entities that matched the entity query constraints.
                /// </summary>
                public readonly int ResultCount;

                /// <summary>
                ///     The request payload that was originally sent with this command.
                /// </summary>
                public readonly Request RequestPayload;

                /// <summary>
                ///     The context object that was provided when sending the command.
                /// </summary>
                public readonly object Context;

                /// <summary>
                ///     The unique request ID of this command. Will match the request ID in the corresponding request.
                /// </summary>
                public readonly long RequestId;

                internal ReceivedResponse(EntityQueryResponseOp op, Entity sendingEntity, Request req, long requestId)
                {
                    SendingEntity = sendingEntity;
                    StatusCode = op.StatusCode;
                    Message = op.Message;
                    ResultCount = op.ResultCount;
                    RequestPayload = req;
                    Context = req.Context;
                    RequestId = requestId;

                    if (op.Result == null)
                    {
                        Result = null;
                        return;
                    }

                    Result = new Dictionary<EntityId, EntityQuerySnapshot>();
                    foreach (var entityIdToEntity in op.Result)
                    {
                        Result.Add(new EntityId(entityIdToEntity.Key), new EntityQuerySnapshot(entityIdToEntity.Value));
                    }
                }

                long IReceivedCommandResponse.GetRequestId()
                {
                    return RequestId;
                }
            }

            /// <summary>
            ///     ECS component is for sending EntityQuery command requests to the SpatialOS runtime.
            /// </summary>
            public struct CommandSender : IComponentData
            {
                internal uint Handle;

                /// <summary>
                ///     The list of pending EntityQuery command requests.
                ///     To send a command request, add an element to this list.
                /// </summary>
                public List<Request> RequestsToSend
                {
                    get => RequestsProvider.Get(Handle);
                    set => RequestsProvider.Set(Handle, value);
                }
            }

            /// <summary>
            ///     ECS component contains a list of CreateEntity command responses received this frame.
            /// </summary>
            public struct CommandResponses : IComponentData
            {
                internal uint Handle;

                /// <summary>
                ///     The list of EntityQuery command responses.
                /// </summary>
                public List<ReceivedResponse> Responses
                {
                    get => ResponsesProvider.Get(Handle);
                    set => ResponsesProvider.Set(Handle, value);
                }
            }

            internal static class RequestsProvider
            {
                private static readonly Dictionary<long, List<Request>> Storage = new Dictionary<long, List<Request>>();
                private static readonly Dictionary<uint, World> WorldMapping = new Dictionary<uint, World>();

                private static uint nextHandle = 0;

                public static uint Allocate(World world)
                {
                    var handle = GetNextHandle();
                    Storage.Add(handle, default(List<Request>));
                    WorldMapping.Add(handle, world);

                    return handle;
                }

                public static List<Request> Get(uint handle)
                {
                    if (!Storage.TryGetValue(handle, out var value))
                    {
                        throw new ArgumentException($"RequestsProvider does not contain handle {handle}");
                    }

                    return value;
                }

                public static void Set(uint handle, List<Request> value)
                {
                    if (!Storage.ContainsKey(handle))
                    {
                        throw new ArgumentException($"RequestsProvider does not contain handle {handle}");
                    }

                    Storage[handle] = value;
                }

                public static void Free(uint handle)
                {
                    Storage.Remove(handle);
                    WorldMapping.Remove(handle);
                }

                public static void CleanDataInWorld(World world)
                {
                    var handles = WorldMapping.Where(pair => pair.Value == world).Select(pair => pair.Key).ToList();

                    foreach (var handle in handles)
                    {
                        Free(handle);
                    }
                }

                private static uint GetNextHandle()
                {
                    nextHandle++;

                    while (Storage.ContainsKey(nextHandle))
                    {
                        nextHandle++;
                    }

                    return nextHandle;
                }
            }

            internal static class ResponsesProvider
            {
                private static readonly Dictionary<uint, List<ReceivedResponse>> Storage =
                    new Dictionary<uint, List<ReceivedResponse>>();

                private static readonly Dictionary<uint, World> WorldMapping = new Dictionary<uint, World>();

                private static uint nextHandle = 0;

                public static uint Allocate(World world)
                {
                    var handle = GetNextHandle();
                    Storage.Add(handle, default(List<ReceivedResponse>));
                    WorldMapping.Add(handle, world);

                    return handle;
                }

                public static List<ReceivedResponse> Get(uint handle)
                {
                    if (!Storage.TryGetValue(handle, out var value))
                    {
                        throw new ArgumentException($"ResponsesProvider does not contain handle {handle}");
                    }

                    return value;
                }

                public static void Set(uint handle, List<ReceivedResponse> value)
                {
                    if (!Storage.ContainsKey(handle))
                    {
                        throw new ArgumentException($"ResponsesProvider does not contain handle {handle}");
                    }

                    Storage[handle] = value;
                }

                public static void Free(uint handle)
                {
                    Storage.Remove(handle);
                    WorldMapping.Remove(handle);
                }

                public static void CleanDataInWorld(World world)
                {
                    var handles = WorldMapping.Where(pair => pair.Value == world).Select(pair => pair.Key).ToList();

                    foreach (var handle in handles)
                    {
                        Free(handle);
                    }
                }

                private static uint GetNextHandle()
                {
                    nextHandle++;

                    while (Storage.ContainsKey(nextHandle))
                    {
                        nextHandle++;
                    }

                    return nextHandle;
                }
            }

            public class CommandComponentManager : IReactiveCommandComponentManager
            {
                public void PopulateReactiveCommandComponents(CommandSystem commandSystem, EntityManager entityManager,
                    WorkerSystem workerSystem, World world)
                {
                    var receivedResponses = commandSystem.GetResponses<ReceivedResponse>();
                    // todo Not efficient if it keeps jumping all over entities but don't care right now
                    for (int i = 0; i < receivedResponses.Count; ++i)
                    {
                        ref readonly var response = ref receivedResponses[i];
                        if (response.SendingEntity == Unity.Entities.Entity.Null ||
                            !entityManager.Exists(response.SendingEntity))
                        {
                            continue;
                        }

                        List<ReceivedResponse> responses;
                        if (entityManager.HasComponent<CommandResponses>(
                            response.SendingEntity))
                        {
                            responses = entityManager
                                .GetComponentData<CommandResponses>(response.SendingEntity).Responses;
                        }
                        else
                        {
                            var data = new CommandResponses
                            {
                                Handle = ResponsesProvider.Allocate(world)
                            };
                            data.Responses = new List<ReceivedResponse>();
                            responses = data.Responses;
                            entityManager.AddComponentData(response.SendingEntity, data);
                        }

                        responses.Add(response);
                    }
                }

                public void Clean(World world)
                {
                    RequestsProvider.CleanDataInWorld(world);
                    ResponsesProvider.CleanDataInWorld(world);
                }
            }
        }
    }
}
