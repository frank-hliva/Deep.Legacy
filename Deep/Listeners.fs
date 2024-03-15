namespace Deep

type ListenerResult =
| End
| Next
| Error of exn

type Listener = Request -> Response -> IKernel -> exn option -> Async<ListenerResult>
type ListenerSync = Request -> Response -> IKernel -> exn option -> ListenerResult

type IListener = 
    abstract Listen : Request -> Response -> IKernel -> exn option -> Async<ListenerResult>

type ListenerContainer(listeners : Listener list) =

    let rec loop (request : Request) (response : Response) (kernel : IKernel) (e : exn option) (listeners : Listener list) = async {
        match listeners with
        | (listener : Listener) :: xs -> 
            let! result = listener request response kernel e
            match result with
            | Next ->
                do! xs |> loop request response kernel e
            | Error exn ->
                do! xs |> loop request response kernel (Some exn)
            | End -> ()
        | _ -> () }

    member c.Use(listener : #IListener) =
        new ListenerContainer(listeners @ [listener.Listen])

    member c.Use(listener : Listener) =
        new ListenerContainer(listeners @ [listener])

    member c.Use(listener : ListenerSync) =
        new ListenerContainer(listeners @ [fun request response kernel e -> async { return listener request response kernel e }])

    member c.Apply(kernel : IKernel) =
        let request = kernel.Resolve<Request>()
        let response = kernel.Resolve<Response>()
        listeners |> loop request response kernel None

    new() = ListenerContainer(List.empty)