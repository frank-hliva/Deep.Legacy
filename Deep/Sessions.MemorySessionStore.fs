namespace Deep

open System
open System.Collections.Generic
open System.Timers

type private Sessions = Map<string, obj>
type private SessionsMap = Map<string, Sessions>
type private ExpirationMap = Map<string, DateTime>

type private SessionStoreMessage =
| TryGetItem of id : string * key : string * replyChannel : AsyncReplyChannel<obj option>
| GetItem of id : string * key : string * replyChannel : AsyncReplyChannel<obj>
| SetItem of id : string * key : string * value : obj * replyChannel : AsyncReplyChannel<unit>
| ContainsKey of id : string * key : string * replyChannel : AsyncReplyChannel<bool>
| RemoveItem of id : string * key : string * replyChannel : AsyncReplyChannel<unit>
| GetItems of id : string * replyChannel : AsyncReplyChannel<IDictionary<string, obj>>
| RemoveExpiredItems of AsyncReplyChannel<unit>

type MemorySessionOptions = { Expiration : TimeSpan }

type MemorySessionConfig(config : Config) =
    interface IConfigSection
    member c.GetOptions() =
        let expiration = config.SelectAs<int>("MemorySession.Expiration")
        { Expiration = expiration |> float |> TimeSpan.FromSeconds }

type MemorySessionStore(options : MemorySessionOptions) as s =

    let updateExpiration (id : string) (expirations : ExpirationMap) =
        expirations.Add(id, DateTime.Now.Add(options.Expiration))

    let mbox =
        MailboxProcessor.Start(fun inbox ->
            let rec loop (map : SessionsMap) (expirations : ExpirationMap) = async {
                let! message = inbox.Receive()
                match message with
                | TryGetItem (id, key, replyChannel) ->
                    let sessions =
                        match map.TryFind id with
                        | Some sessions ->
                            key |> sessions.TryFind |> replyChannel.Reply
                        | _ -> None |> replyChannel.Reply
                    return! loop map (expirations |> updateExpiration id)
                | GetItem (id, key, replyChannel) ->
                    let sessions =
                        match map.TryFind id with
                        | Some sessions ->
                             sessions.[key] |> replyChannel.Reply
                        | _ -> None |> replyChannel.Reply
                    return! loop map (expirations |> updateExpiration id)
                | SetItem (id, key, value, replyChannel) ->
                    let sessions =
                        match map.TryFind id with
                        | Some sessions ->
                            sessions.Add(key, value)
                        | _ ->
                            Map.empty.Add(key, value)
                    let newMap = map.Add(id, sessions)
                    replyChannel.Reply()
                    return! loop newMap (expirations |> updateExpiration id)
                | ContainsKey (id, key, replyChannel) ->
                    let sessions =
                        match map.TryFind id with
                        | Some sessions ->
                             key |> sessions.ContainsKey |> replyChannel.Reply
                        | _ -> false |> replyChannel.Reply
                    return! loop map (expirations |> updateExpiration id)
                | RemoveItem (id, key, replyChannel) ->
                    let sessions =
                        match map.TryFind id with
                        | Some sessions ->
                            sessions.Remove(key)
                        | _ -> Map.empty
                    let newMap = map.Add(id, sessions)
                    replyChannel.Reply()
                    return! loop newMap (expirations |> updateExpiration id)
                | GetItems (id, replyChannel) ->
                    match map.TryFind id with
                    | Some sessions -> sessions
                    | _ -> Map.empty
                    :> IDictionary<string, obj>
                    |> replyChannel.Reply
                    return! loop map (expirations |> updateExpiration id)
                | RemoveExpiredItems (replyChannel) ->
                    let expired, noExpired = expirations |> Map.partition(fun k expires -> expires < DateTime.Now)
                    let newMap = map |> Map.filter(fun k v -> expired.ContainsKey k |> not)
                    return! loop newMap noExpired
            }
            loop Map.empty Map.empty)

    do s.AutoRemoveExpiredItems()

    interface ISessionStore with
        member s.TryGetItem<'t> id key = async { 
            let! value = mbox.PostAndAsyncReply(fun replyChannel -> TryGetItem(id, key, replyChannel))
            match value with
            | Some value -> return value :?> 't |> Some
            | _ -> return None }
        member s.GetItem<'t> id key = async {
            let! value = mbox.PostAndAsyncReply(fun replyChannel -> GetItem(id, key, replyChannel))
            return value :?> 't }
        member s.SetItem id key value = mbox.PostAndAsyncReply(fun replyChannel -> SetItem(id, key, value, replyChannel))
        member s.ContainsKey id  key = mbox.PostAndAsyncReply(fun replyChannel -> ContainsKey(id, key, replyChannel))
        member s.RemoveItem id key = mbox.PostAndAsyncReply(fun replyChannel -> RemoveItem(id, key, replyChannel))
        member s.GetItems id = mbox.PostAndAsyncReply(fun replyChannel -> GetItems(id, replyChannel))
        member s.Expiration = options.Expiration
    member s.RemoveExpiredItems() = mbox.PostAndAsyncReply(fun replyChannel -> RemoveExpiredItems(replyChannel))
    member private s.AutoRemoveExpiredItems() =
        let timer = new Timer(5000.0)
        timer.Elapsed.Add(fun _ -> s.RemoveExpiredItems() |> Async.Start)
        timer.Start()

    new(config : MemorySessionConfig) = MemorySessionStore(config.GetOptions())