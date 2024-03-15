namespace Deep

open System
open System.Net

type SessionManager(store : ISessionStore, id : string) =
    interface ISessionManager with
        member s.TryGetItem<'t>(key) = store.TryGetItem<'t> id key
        member s.GetItem<'t>(key) = store.GetItem<'t> id key
        member s.GetItemOrDefault<'t>(key) = async {
            let! value = (s :> ISessionManager).TryGetItem<'t> key
            match value with
            | Some value -> return value
            | None -> return Unchecked.defaultof<'t> }
        member s.TryGetItem(key) = store.TryGetItem<obj> id key
        member s.GetItem(key) = store.GetItem<obj> id key
        member s.GetItemOrDefault(key) =
            (s :> ISessionManager).GetItemOrDefault<obj> key
        member s.SetItem(key, value) = store.SetItem id key value
        member s.ContainsKey(key) = store.ContainsKey id key
        member s.RemoveItem(key) = store.RemoveItem id key
        member s.Items = store.GetItems id
    new(store : ISessionStore, request : Request, response : Response) =
        let cookieName = "DEEP_SESSION_ID"
        let expires =
            if store.Expiration = (0.0 |> TimeSpan.FromSeconds)
            then DateTime.MinValue
            else DateTime.Now.Add(store.Expiration)
        let deepSessionId = 
            match request.Cookies.[cookieName] with
            | null -> 
                let deepSessionId = Guid.NewGuid().ToString()
                new Cookie(
                    Domain = request.Url.Host,
                    Path = "/",
                    Name = cookieName,
                    Value = deepSessionId,
                    Expires = expires,
                    Expired = false 
                ) |> response.Cookies.Add
                deepSessionId
            | deepSessionCookie ->
                deepSessionCookie.Expires <- expires
                deepSessionCookie |> response.Cookies.Add
                deepSessionCookie.Value
        SessionManager(store, deepSessionId)