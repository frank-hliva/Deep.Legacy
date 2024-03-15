namespace Deep

open System
open System.Collections.Generic

type ISessionStore =
    abstract TryGetItem<'t> : id : string -> key : string -> Async<'t option>
    abstract GetItem<'t> : id : string -> key : string -> Async<'t>
    abstract SetItem : id : string -> key : string -> value : obj -> Async<unit>
    abstract ContainsKey : id : string -> key : string -> Async<bool>
    abstract RemoveItem : id : string -> key : string -> Async<unit>
    abstract GetItems : id : string -> Async<IDictionary<string, obj>>
    abstract Expiration : TimeSpan

type ISessionManager =
    abstract TryGetItem<'t> : key : string -> Async<'t option>
    abstract GetItem<'t> : key : string -> Async<'t>
    abstract GetItemOrDefault<'t> : key : string -> Async<'t>
    abstract TryGetItem : key : string -> Async<obj option>
    abstract GetItem : key : string -> Async<obj>
    abstract GetItemOrDefault : key : string -> Async<obj>
    abstract SetItem : key : string * value : obj -> Async<unit>
    abstract ContainsKey : key : string -> Async<bool>
    abstract RemoveItem : key : string -> Async<unit>
    abstract Items : Async<IDictionary<string, obj>>