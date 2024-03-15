namespace Deep

open System

type IApplication =
    abstract Run : string seq -> unit

[<AllowNullLiteral>]
[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type RouteAttribute(httpMethod : string, routePattern : string, priority : int) =
    inherit Attribute()
    member a.HttpMethod = httpMethod
    member a.RoutePattern = routePattern
    member a.Priority = priority

[<RequireQualifiedAccess>]
module HttpMethods =
    let [<Literal>] Any = "ANY"
    let [<Literal>] Get = "GET"
    let [<Literal>] Head = "HEAD"
    let [<Literal>] Post = "POST"
    let [<Literal>] Put = "PUT"
    let [<Literal>] Delete = "DELETE"
    let [<Literal>] Connect = "CONNECT"
    let [<Literal>] Options = "OPTIONS"
    let [<Literal>] Trace = "TRACE"
    let [<Literal>] Patch = "PATCH"

[<RequireQualifiedAccess>]
module ContentTypes =
    let html = "text/html"
    let plainText = "text/plain"
    let xml = "application/xml"
    let json = "application/json"

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type IgnoreActionAttribute() = inherit Attribute()

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type AnyAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Any, routePattern, priority)
    new(routePattern) = AnyAttribute(routePattern, 0)
    new() = AnyAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type GetAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Get, routePattern, priority)
    new(routePattern) = GetAttribute(routePattern, 0)
    new() = GetAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type HeadAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Head, routePattern, priority)
    new(routePattern) = HeadAttribute(routePattern, 0)
    new() = HeadAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type PostAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Post, routePattern, priority)
    new(routePattern) = PostAttribute(routePattern, 0)
    new() = PostAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type PutAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Put, routePattern, priority)
    new(routePattern) = PutAttribute(routePattern, 0)
    new() = PutAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type DeleteAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Delete, routePattern, priority)
    new(routePattern) = DeleteAttribute(routePattern, 0)
    new() = DeleteAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type ConnectAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Connect, routePattern, priority)
    new(routePattern) = ConnectAttribute(routePattern, 0)
    new() = ConnectAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type OptionsAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Options, routePattern, priority)
    new(routePattern) = OptionsAttribute(routePattern, 0)
    new() = OptionsAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type TraceAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Trace, routePattern, priority)
    new(routePattern) = TraceAttribute(routePattern, 0)
    new() = TraceAttribute("")

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)>]
type PatchAttribute(routePattern : string, priority : int) =
    inherit RouteAttribute(HttpMethods.Patch, routePattern, priority)
    new(routePattern) = PatchAttribute(routePattern, 0)
    new() = PatchAttribute("")

[<AutoOpen>]
module Operators =
    let (=>) a b = a, box b

exception HttpException of int * string

type IAutoDisposable =
    inherit IDisposable
    abstract IsDisposed : bool with get

module TypeConversion =

    let changeType (conversion : Type) (value : obj) =
        match value with
        | null -> null
        | _ ->
            (if conversion.IsGenericType && conversion.GetGenericTypeDefinition().Equals(typedefof<Nullable<_>>)
            then Nullable.GetUnderlyingType(conversion)
            else conversion) |> fun conversion -> Convert.ChangeType(value, conversion)

    let change<'t> (value : obj) =
        let conversion = typedefof<'t>
        match value with
        | null -> Unchecked.defaultof<'t>
        | _ ->
            (if conversion.IsGenericType && conversion.GetGenericTypeDefinition().Equals(typedefof<Nullable<_>>)
            then Nullable.GetUnderlyingType(conversion)
            else conversion) |> fun conversion -> Convert.ChangeType(value, conversion) :?> 't


module TimeSpan =

    let tryParse (input : string) =
        let items = input.Split([|':'|])
        let input =
            match items.Length with
            | 2 -> sprintf "%s:00" input
            | _ -> input
        let success, timeSpan = input |> TimeSpan.TryParse
        if success then Some timeSpan
        else None